[CmdletBinding()]
param(
    [string]$GameDir,
    [string]$RecheckArchive,
    [string[]]$Scenes = @('OfficeLayout','Typing Shooter Final scene','OfficeLayout','ParkingLotScene','RoadTripLoop','Pizzeria','CabinScene','CabinSceneDark','CreditsScene','MainMenu'),
    [bool]$UdpEnabled = $true,
    [ValidateRange(25,180)][int]$SceneSeconds = 45,
    [ValidateRange(120,3600)][int]$TimeoutSeconds = 720,
    [int]$HostPort = 27155,
    [int]$UdpPort = 27156,
    [string]$ArchiveRoot = (Join-Path $PSScriptRoot '../build/all-scene-smoke'),
    [switch]$NoBuild,
    [switch]$ProbeMenuAndAnimations,
    [switch]$ProbeStoryParity,
    [switch]$ProbeCharacterAppearance,
    [switch]$CaptureGraphics
)
$ErrorActionPreference = 'Stop'
$requireFullBuildCoverage = -not $PSBoundParameters.ContainsKey('Scenes')
if ($RecheckArchive) {
    $runDir = (Resolve-Path -LiteralPath $RecheckArchive).Path
    $previous = Get-Content -LiteralPath (Join-Path $runDir 'summary.json') -Raw | ConvertFrom-Json
    $launch = Get-Content -LiteralPath (Join-Path $runDir 'launch.json') -Raw | ConvertFrom-Json
    $GameDir = $launch.GameDir; $Scenes = $launch.SceneDumpCrawlerScenes.Split(',')
    $completed = $previous.CrawlerCompleted; $failure = $previous.RunFailure
    $pluginSha256 = $previous.PluginSha256; $UdpEnabled = $previous.UdpEnabled
    $ProbeMenuAndAnimations = [bool]$previous.ProbeMenuAndAnimations
    $ProbeStoryParity = [bool]$previous.ProbeStoryParity
    $ProbeCharacterAppearance = [bool]$previous.ProbeCharacterAppearance
    $requireFullBuildCoverage = if ($previous.PSObject.Properties['FullBuildSweep']) { $previous.FullBuildSweep } else {
        @('OfficeLayout','Typing Shooter Final scene','ParkingLotScene','RoadTripLoop','Pizzeria','CabinScene','CabinSceneDark','CreditsScene','MainMenu' | Where-Object { $Scenes -notcontains $_ }).Count -eq 0
    }
    Copy-Item -LiteralPath (Join-Path $runDir 'summary.json') -Destination (Join-Path $runDir ('summary-before-recheck-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '.json'))
} else {
if (-not $GameDir) { throw 'Provide GameDir or RecheckArchive.' }
# All Unity instances of this game share PlayerPrefs, even in separate folders.
if (Get-Process | Where-Object { $_.ProcessName -eq 'Fears to Fathom - Woodbury Getaway' }) {
    throw 'A Woodbury game instance is already running. Close it before the automated sweep so its preferences and play session remain untouched.'
}
$runDir = Join-Path $ArchiveRoot (Get-Date -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
$runDir = (Resolve-Path -LiteralPath $runDir).Path
$prefsKey = 'HKCU\Software\Rayll Studios\Fears to Fathom - Woodbury Getaway'
$prefsBackup = Join-Path $runDir 'playerprefs-before.reg'
$hadPrefs = Test-Path 'HKCU:\Software\Rayll Studios\Fears to Fathom - Woodbury Getaway'
if ($hadPrefs) {
    & reg.exe export $prefsKey $prefsBackup /y | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Could not back up game PlayerPrefs.' }
}
$launch = $null
$completed = $false
$failure = ''
$pluginSha256 = ''
$probeEnvironmentBefore = @{}
try {
    foreach ($key in @('WSS_MENU_PROBE','WSS_MENU_CONNECT','WSS_ANIMATION_PROBE','WSS_CAPTURE_MENU','WSS_CAPTURE_AVATARS','WSS_PARITY_PROBE','WSS_CHARACTER_PROBE')) {
        $probeEnvironmentBefore[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
        if ($ProbeMenuAndAnimations -and $key -in @('WSS_MENU_PROBE','WSS_MENU_CONNECT','WSS_ANIMATION_PROBE')) { [Environment]::SetEnvironmentVariable($key, '1', 'Process') }
        if ($ProbeStoryParity -and $key -eq 'WSS_PARITY_PROBE') { [Environment]::SetEnvironmentVariable($key, '1', 'Process') }
        if ($ProbeCharacterAppearance -and $key -eq 'WSS_CHARACTER_PROBE') { [Environment]::SetEnvironmentVariable($key, '1', 'Process') }
        if ($CaptureGraphics -and $key -like 'WSS_CAPTURE_*') { [Environment]::SetEnvironmentVariable($key, (Join-Path $runDir 'images'), 'Process') }
    }
    & (Join-Path $PSScriptRoot 'Deploy-Coop.ps1') -GameDir $GameDir -Build:(-not $NoBuild) | Out-Null
    $pluginSha256 = (Get-FileHash -LiteralPath (Join-Path $GameDir 'BepInEx/plugins/WoodburySpectatorSync.dll') -Algorithm SHA256).Hash.ToLowerInvariant()
    $launch = & (Join-Path $PSScriptRoot 'Launch-CoopPair.ps1') -GameDir $GameDir `
        -HostConfig (Join-Path $runDir 'host.cfg') -ClientConfig (Join-Path $runDir 'client.cfg') `
        -HostPort $HostPort -UdpPort $UdpPort -UdpEnabled $UdpEnabled -StartupDelaySeconds 3 `
        -HostDisplayName 'Host' -ClientDisplayName 'Client' `
        -GraphicsBatch:$CaptureGraphics -WindowWidth 1280 -WindowHeight 720 `
        -AutoStartHost:(-not $ProbeMenuAndAnimations) -AutoConnectClient:(-not $ProbeMenuAndAnimations) -EnableSessionLog -Headless:(-not $CaptureGraphics) -ExperimentalSceneDumpCrawler `
        -SceneDumpCrawlerScenes ($Scenes -join ',') -SceneDumpCrawlerStartDelaySeconds 8 `
        -SceneDumpCrawlerSceneSeconds $SceneSeconds -SceneDiscoveryDumpIntervalSeconds 20
    $launch | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $runDir 'launch.json') -Encoding UTF8
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        foreach ($processId in @($launch.HostPid,$launch.ClientPid)) {
            if (-not (Get-Process -Id $processId -ErrorAction SilentlyContinue)) { throw "Test process exited early: $processId" }
        }
        if (Test-Path -LiteralPath $launch.HostSessionLog) {
            if (Select-String -LiteralPath $launch.HostSessionLog -Pattern 'SceneDiscoveryDumpCrawler: completed scenes=' -Quiet -SimpleMatch) { $completed = $true; break }
        }
        Start-Sleep -Seconds 5
    }
    if (-not $completed) { $failure = 'Crawler timed out before completing the route.' }
}
catch { $failure = $_.Exception.Message }
finally {
    foreach ($key in $probeEnvironmentBefore.Keys) { [Environment]::SetEnvironmentVariable($key, $probeEnvironmentBefore[$key], 'Process') }
    if ($launch) {
        foreach ($processId in @($launch.HostPid,$launch.ClientPid)) {
            $proc = Get-Process -Id $processId -ErrorAction SilentlyContinue
            if ($proc -and $proc.Path -eq $launch.ExePath) { Stop-Process -Id $processId -Force }
        }
        foreach ($path in @($launch.HostSessionLog,$launch.ClientSessionLog,$launch.HostUnityLog,$launch.ClientUnityLog)) {
            if ($path -and (Test-Path -LiteralPath $path)) { Copy-Item -LiteralPath $path -Destination $runDir }
        }
    }
    if ($hadPrefs) {
        & reg.exe import $prefsBackup | Out-Null
        if ($LASTEXITCODE -ne 0) { $failure += ' PlayerPrefs restore failed.' }
    }
}
if (-not $launch) { throw "All-scene launch failed: $failure. Archive: $runDir" }
}
$logs = @($launch.HostSessionLog,$launch.ClientSessionLog | ForEach-Object { Join-Path $runDir (Split-Path $_ -Leaf) })
$summary = & (Join-Path $PSScriptRoot 'Check-CoopLogs.ps1') -GameDir $GameDir -LogPaths $logs `
    -RequireTraffic -RequireGameplay -RequirePlayerPoses -RequiredScenes $Scenes -NoFail
# Keep native installation faults separate from mod/managed runtime errors, never silently discard them.
$classified = & (Join-Path $PSScriptRoot 'Classify-UnityLogErrors.ps1') -LogPaths @($launch.HostUnityLog,$launch.ClientUnityLog | ForEach-Object { Join-Path $runDir (Split-Path $_ -Leaf) })
$environmentErrors = @($classified.EnvironmentErrors)
$runtimeErrors = @($classified.RuntimeErrors)
$summary | Add-Member CrawlerCompleted $completed
$summary | Add-Member RunFailure $failure
$summary | Add-Member EnvironmentErrors @($environmentErrors | Select-Object -Unique)
$summary | Add-Member RuntimeErrors @($runtimeErrors | Select-Object -Unique)
$summary | Add-Member UdpEnabled $UdpEnabled
$summary | Add-Member PluginSha256 $pluginSha256
$summary | Add-Member FullBuildSweep $requireFullBuildCoverage
$summary | Add-Member ProbeMenuAndAnimations ([bool]$ProbeMenuAndAnimations)
$summary | Add-Member ProbeStoryParity ([bool]$ProbeStoryParity)
$summary | Add-Member ProbeCharacterAppearance ([bool]$ProbeCharacterAppearance)
$activityProblems = @()
$clientText = [IO.File]::ReadAllText($logs[1])
if ($Scenes -contains 'Typing Shooter Final scene' -and $clientText -notmatch 'ScenePresentation role=client scene=Typing Shooter Final scene nodes=\d+ words=[1-9]\d* missing=0') {
    $activityProblems += 'Typing word/progress fixture did not reach the client.'
}
if ($Scenes -contains 'RoadTripLoop') {
    foreach ($player in @(0,1)) {
        $text = if ($player -eq 0) { $clientText } else { [IO.File]::ReadAllText($logs[0]) }
        if ($text -notmatch "Remote player pose received: player=$player [^\r\n]*flags=[^\r\n]*Seated scene=RoadTripLoop [^\r\n]*head=True torso=True leftArm=True rightArm=True") {
            $activityProblems += "RoadTrip seated human pose was not applied for player $player."
        }
    }
}
$summary | Add-Member ActivityProblems $activityProblems
if ($ProbeMenuAndAnimations) {
    $probe = & (Join-Path $PSScriptRoot 'Check-MenuAnimationLogs.ps1') -LogPaths $logs -RequiredScenes $Scenes
    $summary | Add-Member MenuAnimationProblems @($probe.Problems)
    $summary.Passed = $summary.Passed -and $probe.Passed
}
$buildScenes = @()
if ($ProbeCharacterAppearance) {
    $appearance = & (Join-Path $PSScriptRoot 'Check-CharacterAppearanceLogs.ps1') -LogPaths $logs -RequiredScenes $Scenes -RequireMenu:$ProbeMenuAndAnimations
    $summary | Add-Member CharacterAppearanceProblems @($appearance.Problems)
    $summary.Passed = $summary.Passed -and $appearance.Passed
}
if ($ProbeStoryParity) {
    $parity = & (Join-Path $PSScriptRoot 'Check-StoryParityLogs.ps1') -LogPaths $logs -RequiredScenes $Scenes
    $summary | Add-Member StoryParityProblems @($parity.Problems)
    $summary.Passed = $summary.Passed -and $parity.Passed
}
$inventoryProblems = @()
$inventoryText = [IO.File]::ReadAllText($logs[0])
foreach ($match in [regex]::Matches($inventoryText, 'SceneBuild index=(\d+) scene=([^\r\n]+?) adapter=([^\r\n]*)')) {
    $scene = $match.Groups[2].Value
    $row = @($summary.SceneCoverage | Where-Object { $_.Scene -ceq $scene })
    $kind = if ($scene -eq 'Disclaimer') { 'bootstrap' } elseif ($scene -eq 'Typing Shooter Final scene') { 'additive presentation' } elseif ($scene -in @('MainMenu','CreditsScene')) { 'utility' } else { 'gameplay' }
    $covered = if ($scene -eq 'Disclaimer') {
        # Disclaimer automatically hands off before the network handshake; validate both boot instances.
        @($logs | Where-Object { [IO.File]::ReadAllText($_) -match 'SceneManifest (host|client) scene=Disclaimer ' }).Count -eq 2
    } else { $row.Count -gt 0 -and $row[0].Passed }
    $buildScenes += [pscustomobject]@{ Index=[int]$match.Groups[1].Value; Scene=$scene; Kind=$kind; Covered=$covered }
    if ($requireFullBuildCoverage -and -not $covered) { $inventoryProblems += "Build scene was not covered: $scene" }
}
if ($requireFullBuildCoverage -and $buildScenes.Count -eq 0) { $inventoryProblems += 'No build scene inventory was recorded.' }
$summary | Add-Member BuildSceneCoverage $buildScenes
$summary | Add-Member InventoryProblems $inventoryProblems
$summary.Passed = $summary.Passed -and $completed -and $failure -eq '' -and $runtimeErrors.Count -eq 0
$summary.Passed = $summary.Passed -and $inventoryProblems.Count -eq 0
$summary.Passed = $summary.Passed -and $activityProblems.Count -eq 0
$summary | ConvertTo-Json -Depth 12 | Set-Content (Join-Path $runDir 'summary.json') -Encoding UTF8
$summary.SceneCoverage | Format-Table Scene,Passed,Adapter
Write-Host "All-scene smoke passed=$($summary.Passed); installationErrors=$($environmentErrors.Count); archive=$runDir"
if (-not $summary.Passed) { throw "All-scene smoke failed. Inspect $runDir/summary.json" }
