param(
    [string]$GameDir = "C:\Games\Fears to Fathom - Woodbury Getaway",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [int]$RuntimeSeconds = 90,
    [int]$StartupDelaySeconds = 3,
    [bool]$UdpEnabled = $true,
    [string]$ArchiveRoot = (Join-Path $PSScriptRoot "..\sessionlogsbackup\smoke"),
    [switch]$KeepProcesses
)

$ErrorActionPreference = "Stop"

$buildScript = Join-Path $PSScriptRoot "Build.ps1"
$deployScript = Join-Path $PSScriptRoot "Deploy-Coop.ps1"
$launchScript = Join-Path $PSScriptRoot "Launch-CoopPair.ps1"
$checkScript = Join-Path $PSScriptRoot "Check-CoopLogs.ps1"

$startedAt = Get-Date
Write-Host "Smoke start: $startedAt"
Write-Host "Game dir    : $GameDir"
Write-Host "UDP enabled : $UdpEnabled"

& $buildScript
$deployInfo = & $deployScript -GameDir $GameDir -Configuration $Configuration

$launchInfo = $null
try {
    $launchInfo = & $launchScript -GameDir $GameDir -StartupDelaySeconds $StartupDelaySeconds -UdpEnabled $UdpEnabled -AutoStartHost -AutoConnectClient -ForceStopExisting
    Write-Host "Runtime window: $RuntimeSeconds seconds"
    Start-Sleep -Seconds ([Math]::Max(10, $RuntimeSeconds))
}
finally {
    if (-not $KeepProcesses.IsPresent -and $launchInfo -ne $null) {
        foreach ($pid in @($launchInfo.HostPid, $launchInfo.ClientPid)) {
            if ($pid -le 0) { continue }
            try {
                $proc = Get-Process -Id $pid -ErrorAction SilentlyContinue
                if ($proc -ne $null) {
                    Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
                }
            }
            catch {
            }
        }
    }
}

Start-Sleep -Seconds 3

$runStamp = Get-Date -Format "yyyyMMdd_HHmmss"
$runDir = Join-Path $ArchiveRoot $runStamp
New-Item -ItemType Directory -Force -Path $runDir | Out-Null

$summaryJson = Join-Path $runDir "summary.json"
$check = & $checkScript -GameDir $GameDir -Since $startedAt -MaxSessionLogs 2 -RequireTraffic -OutputJson $summaryJson -NoFail | Select-Object -First 1

$logIndex = 0
foreach ($logPath in $check.SelectedLogs) {
    if (-not (Test-Path -LiteralPath $logPath)) { continue }
    $name = ("{0:D2}_" -f $logIndex) + (Split-Path -Leaf $logPath)
    Copy-Item -LiteralPath $logPath -Destination (Join-Path $runDir $name) -Force
    $logIndex++
}

if ($launchInfo -ne $null) {
    if (Test-Path -LiteralPath $launchInfo.HostConfig) {
        Copy-Item -LiteralPath $launchInfo.HostConfig -Destination (Join-Path $runDir "host.cfg") -Force
    }
    if (Test-Path -LiteralPath $launchInfo.ClientConfig) {
        Copy-Item -LiteralPath $launchInfo.ClientConfig -Destination (Join-Path $runDir "client.cfg") -Force
    }
}

$deployInfo | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $runDir "deploy.json") -Encoding UTF8

if ($check.Passed) {
    Write-Host "SMOKE PASS"
    Write-Host "Archive: $runDir"
    exit 0
}

Write-Host "SMOKE FAIL"
Write-Host "Archive: $runDir"
exit 1
