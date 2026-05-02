param(
    [string]$GameDir = "C:\Games\Fears to Fathom - Woodbury Getaway",
    [string]$ExePath = "",
    [string]$HostConfig = "",
    [string]$ClientConfig = "",
    [string]$HostIp = "127.0.0.1",
    [int]$HostPort = 27055,
    [int]$UdpPort = 27056,
    [bool]$UdpEnabled = $true,
    [int]$StartupDelaySeconds = 35,
    [int]$HostReadyTimeoutSeconds = 45,
    [switch]$EnableSessionLog,
    [switch]$NoPrepareConfigs,
    [switch]$AutoStartHost,
    [switch]$AutoConnectClient,
    [switch]$NoWindowed,
    [int]$WindowWidth = 1440,
    [int]$WindowHeight = 900,
    [string]$RemotePlayerPrefabPath = "",
    [string]$RemotePlayerRig = "Auto",
    [string]$RemotePlayerAvatarSource = "AssetBundle",
    [string]$RemotePlayerAvatarBundlePath = "BepInEx/plugins/WoodburySpectatorSync/avatars/woodbury_avatars.bundle",
    [string]$RemotePlayerAvatarId = "quaternius_regular_male",
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

function Wait-ForUnlockedFile {
    param(
        [string]$Path,
        [int]$TimeoutSeconds = 20
    )

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path)) {
        return
    }

    $deadline = (Get-Date).AddSeconds([Math]::Max(1, $TimeoutSeconds))
    $lastError = $null
    while ((Get-Date) -lt $deadline) {
        try {
            $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
            $stream.Close()
            return
        }
        catch {
            $lastError = $_.Exception.Message
            Start-Sleep -Milliseconds 250
        }
    }

    throw "Timed out waiting for file lock to clear: $Path ($lastError)"
}

function Wait-ForTcpPort {
    param(
        [string]$HostIp,
        [int]$Port,
        [int]$ProcessId,
        [int]$TimeoutSeconds = 45
    )

    $deadline = (Get-Date).AddSeconds([Math]::Max(1, $TimeoutSeconds))
    $lastError = $null
    while ((Get-Date) -lt $deadline) {
        if ($ProcessId -gt 0 -and -not (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue)) {
            throw "Host process exited before TCP port was ready (pid=$ProcessId, port=$Port)."
        }

        try {
            $listeners = [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners()
            foreach ($listener in $listeners) {
                if ($listener.Port -eq $Port) {
                    Write-Host "Host TCP port ready: $HostIp`:$Port"
                    return
                }
            }
            $lastError = "port not listening yet"
        }
        catch {
            $lastError = $_.Exception.Message
            try {
                $tcpRows = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
                if ($tcpRows.Count -gt 0) {
                    Write-Host "Host TCP port ready: $HostIp`:$Port"
                    return
                }
            }
            catch {
                $lastError = $_.Exception.Message
            }
        }

        Start-Sleep -Milliseconds 500
    }

    throw "Timed out waiting for host TCP port $HostIp`:$Port ($lastError)"
}

function Wait-ForSessionLog {
    param(
        [string]$Root,
        [string]$ModeLabel,
        [int]$ProcessId,
        [datetime]$Since,
        [int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds([Math]::Max(1, $TimeoutSeconds))
    $sessionRoot = Join-Path $Root "BepInEx\logs"
    $filter = "WoodburySpectatorSync_session_*_$($ModeLabel)_pid$($ProcessId).log"

    while ((Get-Date) -lt $deadline) {
        $running = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
        if ($running -eq $null) {
            throw "$ModeLabel process exited before the plugin created a session log (pid=$ProcessId)."
        }

        if (Test-Path -LiteralPath $sessionRoot) {
            $log = Get-ChildItem -LiteralPath $sessionRoot -Filter $filter -File -ErrorAction SilentlyContinue |
                Where-Object { $_.LastWriteTime -ge $Since.AddSeconds(-2) } |
                Sort-Object LastWriteTime -Descending |
                Select-Object -First 1
            if ($log -ne $null) {
                return $log.FullName
            }
        }

        Start-Sleep -Milliseconds 500
    }

    throw "$ModeLabel plugin did not create a session log within $TimeoutSeconds seconds (pid=$ProcessId)."
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

$runningBeforeLaunch = @(Get-RunningGameProcesses -ResolvedExePath $resolvedExe)
if ($runningBeforeLaunch.Count -gt 0) {
    if ($ForceStopExisting.IsPresent) {
        foreach ($proc in $runningBeforeLaunch) {
            Stop-Process -Id $proc.ProcessId -Force -ErrorAction SilentlyContinue
        }
        Start-Sleep -Seconds 3
        $runningBeforeLaunch = @(Get-RunningGameProcesses -ResolvedExePath $resolvedExe)
    }

    if ($runningBeforeLaunch.Count -gt 0) {
        throw "Found $($runningBeforeLaunch.Count) running game instance(s). Close existing windows first or use -ForceStopExisting."
    }
}

$bepInExConfig = Join-Path $GameDir "BepInEx\config\BepInEx.cfg"
Wait-ForUnlockedFile -Path $bepInExConfig -TimeoutSeconds 20

if (-not $NoPrepareConfigs.IsPresent) {
    Write-WssConfig -Path $HostConfig -Mode "CoopHost" -AutoStartHost $AutoStartHost.IsPresent -AutoConnectClient $false -RemotePlayerPrefabPath $RemotePlayerPrefabPath -RemotePlayerRig $RemotePlayerRig -RemotePlayerAvatarSource $RemotePlayerAvatarSource -RemotePlayerAvatarBundlePath $RemotePlayerAvatarBundlePath -RemotePlayerAvatarId $RemotePlayerAvatarId -RemotePlayerAvatarScale $RemotePlayerAvatarScale -RemotePlayerAvatarYOffset $RemotePlayerAvatarYOffset -HostIp $HostIp -HostPort $HostPort -UdpPort $UdpPort -UdpEnabled $UdpEnabled
    Write-WssConfig -Path $ClientConfig -Mode "CoopClient" -AutoStartHost $false -AutoConnectClient $AutoConnectClient.IsPresent -RemotePlayerPrefabPath $RemotePlayerPrefabPath -RemotePlayerRig $RemotePlayerRig -RemotePlayerAvatarSource $RemotePlayerAvatarSource -RemotePlayerAvatarBundlePath $RemotePlayerAvatarBundlePath -RemotePlayerAvatarId $RemotePlayerAvatarId -RemotePlayerAvatarScale $RemotePlayerAvatarScale -RemotePlayerAvatarYOffset $RemotePlayerAvatarYOffset -HostIp $HostIp -HostPort $HostPort -UdpPort $UdpPort -UdpEnabled $UdpEnabled
}

$hostProc = $null
$clientProc = $null
$commonLaunchArgs = @()
if (-not $NoWindowed.IsPresent) {
    $commonLaunchArgs += @(
        "-screen-fullscreen", "0",
        "-screen-width", "$WindowWidth",
        "-screen-height", "$WindowHeight"
    )
}
$unityLogDir = Join-Path $GameDir "BepInEx\logs"
New-Item -ItemType Directory -Force -Path $unityLogDir | Out-Null
$hostUnityLog = Join-Path $unityLogDir ("unity_host_" + (Get-Date -Format "yyyyMMdd_HHmmss") + ".log")
$clientUnityLog = Join-Path $unityLogDir ("unity_client_" + (Get-Date -Format "yyyyMMdd_HHmmss") + ".log")
$hostLaunchArgs = @($commonLaunchArgs + @("-logFile", $hostUnityLog))
$clientLaunchArgs = @($commonLaunchArgs + @("-logFile", $clientUnityLog))

try {
    $env:WSS_MODE = "CoopHost"
    $env:WSS_CONFIG = $HostConfig
    $env:WSS_HOST_IP = $HostIp
    $env:WSS_HOST_PORT = "$HostPort"
    $env:WSS_UDP_PORT = "$UdpPort"
    $env:WSS_UDP = $UdpEnabled.ToString().ToLowerInvariant()
    if ($EnableSessionLog.IsPresent) {
        $env:WSS_SESSION_LOG = "true"
    }
    $hostLaunchTime = Get-Date
    $hostProc = Start-Process -FilePath $resolvedExe -WorkingDirectory $GameDir -PassThru -ArgumentList $hostLaunchArgs

    if ($EnableSessionLog.IsPresent) {
        $hostSessionLog = Wait-ForSessionLog -Root $GameDir -ModeLabel "CoopHost" -ProcessId $hostProc.Id -Since $hostLaunchTime -TimeoutSeconds $HostReadyTimeoutSeconds
    }
    Start-Sleep -Seconds ([Math]::Max(1, $StartupDelaySeconds))
    Wait-ForUnlockedFile -Path $bepInExConfig -TimeoutSeconds 20
    Wait-ForTcpPort -HostIp $HostIp -Port $HostPort -ProcessId $hostProc.Id -TimeoutSeconds $HostReadyTimeoutSeconds

    $env:WSS_MODE = "CoopClient"
    $env:WSS_CONFIG = $ClientConfig
    $env:WSS_HOST_IP = $HostIp
    $env:WSS_HOST_PORT = "$HostPort"
    $env:WSS_UDP_PORT = "$UdpPort"
    $env:WSS_UDP = $UdpEnabled.ToString().ToLowerInvariant()
    if ($EnableSessionLog.IsPresent) {
        $env:WSS_SESSION_LOG = "true"
    }
    $clientLaunchTime = Get-Date
    $clientProc = Start-Process -FilePath $resolvedExe -WorkingDirectory $GameDir -PassThru -ArgumentList $clientLaunchArgs
    if ($EnableSessionLog.IsPresent) {
        $clientSessionLog = Wait-ForSessionLog -Root $GameDir -ModeLabel "CoopClient" -ProcessId $clientProc.Id -Since $clientLaunchTime -TimeoutSeconds $HostReadyTimeoutSeconds
    }
}
finally {
    Remove-Item Env:WSS_MODE -ErrorAction SilentlyContinue
    Remove-Item Env:WSS_CONFIG -ErrorAction SilentlyContinue
    Remove-Item Env:WSS_HOST_IP -ErrorAction SilentlyContinue
    Remove-Item Env:WSS_HOST_PORT -ErrorAction SilentlyContinue
    Remove-Item Env:WSS_UDP_PORT -ErrorAction SilentlyContinue
    Remove-Item Env:WSS_UDP -ErrorAction SilentlyContinue
    Remove-Item Env:WSS_SESSION_LOG -ErrorAction SilentlyContinue
}

if ($hostProc -eq $null -or $clientProc -eq $null) {
    throw "Failed to launch both host and client instances."
}

Start-Sleep -Seconds 1
$runningAfterLaunch = @(Get-RunningGameProcesses -ResolvedExePath $resolvedExe)
if ($runningAfterLaunch.Count -lt 2) {
    $ids = @($runningAfterLaunch | ForEach-Object { $_.ProcessId }) -join ", "
    throw "Launch produced fewer than two running game instances (running=$($runningAfterLaunch.Count), pids=$ids)."
}

if ($runningAfterLaunch.Count -gt 2) {
    $ids = @($runningAfterLaunch | ForEach-Object { $_.ProcessId }) -join ", "
    throw "Launch exceeded instance cap (running=$($runningAfterLaunch.Count), pids=$ids)."
}

Write-Host "Host PID  : $($hostProc.Id)"
Write-Host "Client PID: $($clientProc.Id)"
Write-Host "Host cfg  : $HostConfig"
Write-Host "Client cfg: $ClientConfig"
Write-Host "Host Unity log  : $hostUnityLog"
Write-Host "Client Unity log: $clientUnityLog"
if ($EnableSessionLog.IsPresent) {
    Write-Host "Host log  : $hostSessionLog"
    Write-Host "Client log: $clientSessionLog"
}
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
    HostUnityLog = $hostUnityLog
    ClientUnityLog = $clientUnityLog
    HostSessionLog = if ($EnableSessionLog.IsPresent) { $hostSessionLog } else { $null }
    ClientSessionLog = if ($EnableSessionLog.IsPresent) { $clientSessionLog } else { $null }
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
