param(
    [string]$GameDir = "C:\Games\Fears to Fathom - Woodbury Getaway",
    [string]$ExePath = "",
    [string]$HostConfig = "",
    [string]$ClientConfig = "",
    [string]$HostIp = "127.0.0.1",
    [int]$HostPort = 27055,
    [int]$UdpPort = 27056,
    [bool]$UdpEnabled = $true,
    [int]$StartupDelaySeconds = 3,
    [switch]$NoPrepareConfigs,
    [switch]$AutoStartHost,
    [switch]$AutoConnectClient,
    [switch]$NoWindowed,
    [int]$WindowWidth = 1440,
    [int]$WindowHeight = 900,
    [string]$RemotePlayerPrefabPath = "",
    [string]$RemotePlayerRig = "Auto",
    [string]$RemotePlayerAvatarSource = "Auto",
    [string]$RemotePlayerAvatarBundlePath = "BepInEx/plugins/WoodburySpectatorSync/avatars/woodbury_avatars.bundle",
    [string]$RemotePlayerAvatarId = "woodbury_scene_auto",
    [float]$RemotePlayerAvatarScale = 1.0,
    [float]$RemotePlayerAvatarYOffset = 0.0,
    [switch]$ForceStopExisting
)

$ErrorActionPreference = "Stop"

function Resolve-GameExe {
    param(
        [string]$Root,
        [string]$ExplicitPath
    )

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        if (-not (Test-Path -LiteralPath $ExplicitPath)) {
            throw "ExePath does not exist: $ExplicitPath"
        }
        return (Resolve-Path -LiteralPath $ExplicitPath).Path
    }

    $preferred = Join-Path $Root "Fears to Fathom - Woodbury Getaway.exe"
    if (Test-Path -LiteralPath $preferred) {
        return (Resolve-Path -LiteralPath $preferred).Path
    }

    $candidate = Get-ChildItem -LiteralPath $Root -Filter "*.exe" -File |
        Where-Object { $_.Name -notlike "UnityCrashHandler*" } |
        Select-Object -First 1
    if ($candidate -eq $null) {
        throw "Could not locate game executable under: $Root"
    }

    return $candidate.FullName
}

function Get-RunningGameProcesses {
    param(
        [string]$ResolvedExePath
    )

    return @(Get-CimInstance Win32_Process |
        Where-Object { $_.ExecutablePath -eq $ResolvedExePath } |
        Sort-Object CreationDate)
}

function Format-InvariantFloat {
    param(
        [float]$Value
    )

    return $Value.ToString([System.Globalization.CultureInfo]::InvariantCulture)
}

function Write-WssConfig {
    param(
        [string]$Path,
        [string]$Mode,
        [bool]$AutoStartHost,
        [bool]$AutoConnectClient,
        [string]$RemotePlayerPrefabPath,
        [string]$RemotePlayerRig,
        [string]$RemotePlayerAvatarSource,
        [string]$RemotePlayerAvatarBundlePath,
        [string]$RemotePlayerAvatarId,
        [float]$RemotePlayerAvatarScale,
        [float]$RemotePlayerAvatarYOffset,
        [string]$HostIp,
        [int]$HostPort,
        [int]$UdpPort,
        [bool]$UdpEnabled
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    $content = @"
[General]
Mode = $Mode

[Host]
HostBindIP = 0.0.0.0
HostPort = $HostPort

[Spectator]
SpectatorHostIP = $HostIp

[Network]
UdpEnabled = $($UdpEnabled.ToString().ToLowerInvariant())
UdpPort = $UdpPort

[Debug]
VerboseLogging = false

[UI]
OverlayEnabled = true

[Coop]
RouteInteractionsToHost = true
UseLocalPlayerController = true
SnapToHostOnSceneLoad = true
AutoStartHost = $($AutoStartHost.ToString().ToLowerInvariant())
AutoConnectClient = $($AutoConnectClient.ToString().ToLowerInvariant())
RemotePlayerPrefabPath = $RemotePlayerPrefabPath
RemotePlayerRig = $RemotePlayerRig
RemotePlayerAvatarSource = $RemotePlayerAvatarSource
RemotePlayerAvatarBundlePath = $RemotePlayerAvatarBundlePath
RemotePlayerAvatarId = $RemotePlayerAvatarId
RemotePlayerAvatarScale = $(Format-InvariantFloat -Value $RemotePlayerAvatarScale)
RemotePlayerAvatarYOffset = $(Format-InvariantFloat -Value $RemotePlayerAvatarYOffset)
ForceCabinStartSequence = true
CabinStartSequence = StartAfterShower
"@

    Set-Content -LiteralPath $Path -Value $content -Encoding UTF8
}

if ([string]::IsNullOrWhiteSpace($HostConfig)) {
    $HostConfig = Join-Path $GameDir "BepInEx\config\com.woodbury.spectatorsync.host.cfg"
}

if ([string]::IsNullOrWhiteSpace($ClientConfig)) {
    $ClientConfig = Join-Path $GameDir "BepInEx\config\com.woodbury.spectatorsync.client.cfg"
}

$resolvedExe = Resolve-GameExe -Root $GameDir -ExplicitPath $ExePath

$runningBeforeLaunch = Get-RunningGameProcesses -ResolvedExePath $resolvedExe
if ($runningBeforeLaunch.Count -gt 0) {
    if ($ForceStopExisting.IsPresent) {
        foreach ($proc in $runningBeforeLaunch) {
            Stop-Process -Id $proc.ProcessId -Force -ErrorAction SilentlyContinue
        }
        Start-Sleep -Seconds 1
        $runningBeforeLaunch = Get-RunningGameProcesses -ResolvedExePath $resolvedExe
    }

    if ($runningBeforeLaunch.Count -gt 0) {
        throw "Found $($runningBeforeLaunch.Count) running game instance(s). Close existing windows first or use -ForceStopExisting."
    }
}

if (-not $NoPrepareConfigs.IsPresent) {
    Write-WssConfig -Path $HostConfig -Mode "CoopHost" -AutoStartHost $AutoStartHost.IsPresent -AutoConnectClient $false -RemotePlayerPrefabPath $RemotePlayerPrefabPath -RemotePlayerRig $RemotePlayerRig -RemotePlayerAvatarSource $RemotePlayerAvatarSource -RemotePlayerAvatarBundlePath $RemotePlayerAvatarBundlePath -RemotePlayerAvatarId $RemotePlayerAvatarId -RemotePlayerAvatarScale $RemotePlayerAvatarScale -RemotePlayerAvatarYOffset $RemotePlayerAvatarYOffset -HostIp $HostIp -HostPort $HostPort -UdpPort $UdpPort -UdpEnabled $UdpEnabled
    Write-WssConfig -Path $ClientConfig -Mode "CoopClient" -AutoStartHost $false -AutoConnectClient $AutoConnectClient.IsPresent -RemotePlayerPrefabPath $RemotePlayerPrefabPath -RemotePlayerRig $RemotePlayerRig -RemotePlayerAvatarSource $RemotePlayerAvatarSource -RemotePlayerAvatarBundlePath $RemotePlayerAvatarBundlePath -RemotePlayerAvatarId $RemotePlayerAvatarId -RemotePlayerAvatarScale $RemotePlayerAvatarScale -RemotePlayerAvatarYOffset $RemotePlayerAvatarYOffset -HostIp $HostIp -HostPort $HostPort -UdpPort $UdpPort -UdpEnabled $UdpEnabled
}

$hostProc = $null
$clientProc = $null
$launchArgs = @()
if (-not $NoWindowed.IsPresent) {
    $launchArgs += @(
        "-screen-fullscreen", "0",
        "-screen-width", "$WindowWidth",
        "-screen-height", "$WindowHeight"
    )
}

try {
    $env:WSS_MODE = "CoopHost"
    $env:WSS_CONFIG = $HostConfig
    $env:WSS_HOST_IP = $HostIp
    $env:WSS_HOST_PORT = "$HostPort"
    $env:WSS_UDP_PORT = "$UdpPort"
    $env:WSS_UDP = $UdpEnabled.ToString().ToLowerInvariant()
    $hostProc = Start-Process -FilePath $resolvedExe -WorkingDirectory $GameDir -PassThru -ArgumentList $launchArgs

    Start-Sleep -Seconds ([Math]::Max(1, $StartupDelaySeconds))

    $env:WSS_MODE = "CoopClient"
    $env:WSS_CONFIG = $ClientConfig
    $env:WSS_HOST_IP = $HostIp
    $env:WSS_HOST_PORT = "$HostPort"
    $env:WSS_UDP_PORT = "$UdpPort"
    $env:WSS_UDP = $UdpEnabled.ToString().ToLowerInvariant()
    $clientProc = Start-Process -FilePath $resolvedExe -WorkingDirectory $GameDir -PassThru -ArgumentList $launchArgs
}
finally {
    Remove-Item Env:WSS_MODE -ErrorAction SilentlyContinue
    Remove-Item Env:WSS_CONFIG -ErrorAction SilentlyContinue
    Remove-Item Env:WSS_HOST_IP -ErrorAction SilentlyContinue
    Remove-Item Env:WSS_HOST_PORT -ErrorAction SilentlyContinue
    Remove-Item Env:WSS_UDP_PORT -ErrorAction SilentlyContinue
    Remove-Item Env:WSS_UDP -ErrorAction SilentlyContinue
}

if ($hostProc -eq $null -or $clientProc -eq $null) {
    throw "Failed to launch both host and client instances."
}

Start-Sleep -Seconds 1
$runningAfterLaunch = Get-RunningGameProcesses -ResolvedExePath $resolvedExe
if ($runningAfterLaunch.Count -gt 2) {
    $ids = @($runningAfterLaunch | ForEach-Object { $_.ProcessId }) -join ", "
    throw "Launch exceeded instance cap (running=$($runningAfterLaunch.Count), pids=$ids)."
}

Write-Host "Host PID  : $($hostProc.Id)"
Write-Host "Client PID: $($clientProc.Id)"
Write-Host "Host cfg  : $HostConfig"
Write-Host "Client cfg: $ClientConfig"
Write-Host "AutoStartHost   : $($AutoStartHost.IsPresent)"
Write-Host "AutoConnectClient: $($AutoConnectClient.IsPresent)"
Write-Host "Windowed launch : $($NoWindowed.IsPresent -eq $false) ($WindowWidth x $WindowHeight)"
Write-Host "RemotePlayerPrefabPath: $RemotePlayerPrefabPath"
Write-Host "RemotePlayerRig : $RemotePlayerRig"
Write-Host "RemotePlayerAvatarSource: $RemotePlayerAvatarSource"
Write-Host "RemotePlayerAvatarBundlePath: $RemotePlayerAvatarBundlePath"
Write-Host "RemotePlayerAvatarId: $RemotePlayerAvatarId"
Write-Host "RemotePlayerAvatarScale: $(Format-InvariantFloat -Value $RemotePlayerAvatarScale)"
Write-Host "RemotePlayerAvatarYOffset: $(Format-InvariantFloat -Value $RemotePlayerAvatarYOffset)"

[pscustomobject]@{
    StartedAt = Get-Date
    GameDir = $GameDir
    ExePath = $resolvedExe
    HostPid = $hostProc.Id
    ClientPid = $clientProc.Id
    HostConfig = $HostConfig
    ClientConfig = $ClientConfig
    HostIp = $HostIp
    HostPort = $HostPort
    UdpPort = $UdpPort
    UdpEnabled = $UdpEnabled
    AutoStartHost = $AutoStartHost.IsPresent
    AutoConnectClient = $AutoConnectClient.IsPresent
    Windowed = (-not $NoWindowed.IsPresent)
    WindowWidth = $WindowWidth
    WindowHeight = $WindowHeight
    RemotePlayerPrefabPath = $RemotePlayerPrefabPath
    RemotePlayerRig = $RemotePlayerRig
    RemotePlayerAvatarSource = $RemotePlayerAvatarSource
    RemotePlayerAvatarBundlePath = $RemotePlayerAvatarBundlePath
    RemotePlayerAvatarId = $RemotePlayerAvatarId
    RemotePlayerAvatarScale = $RemotePlayerAvatarScale
    RemotePlayerAvatarYOffset = $RemotePlayerAvatarYOffset
}
