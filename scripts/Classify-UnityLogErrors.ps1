[CmdletBinding()]
param([Parameter(Mandatory=$true)][string[]]$LogPaths)
$environmentErrors = @()
$runtimeErrors = @()
foreach ($path in $LogPaths) {
    $lines = @(Get-Content -LiteralPath $path)
    $failedSteamInit = @($lines | Where-Object { $_ -match 'EntryPointNotFoundException: SteamAPI_ManualDispatch_Init\b' }).Count -gt 0
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match 'A crash has been intercepted by the crash handler|^Crash!!!|^Fatal error|Could not allocate memory: System out of memory|SkinnedMeshRenderer: Rendering stopped') { $runtimeErrors += $line; continue }
        if ($line -notmatch '(?:\w+Exception\b|SceneDiscoveryDumpError|SceneDiscoveryDumpTimedError)') { continue }
        if ($line -match 'EntryPointNotFoundException: SteamAPI_(ManualDispatch_Init|Shutdown)\b') { $environmentErrors += $line }
        elseif ($failedSteamInit -and $line -match '^NullReferenceException:' -and $i + 1 -lt $lines.Count -and
            $lines[$i + 1] -match '^\s+at Steamworks\.Data\.Achievement\.Trigger\s*\(') {
            $environmentErrors += $line + ' ' + $lines[$i + 1].Trim() + ' [same process failed Steam initialization]'
        }
        else { $runtimeErrors += $line }
    }
}
[pscustomobject]@{ EnvironmentErrors=@($environmentErrors | Select-Object -Unique); RuntimeErrors=@($runtimeErrors | Select-Object -Unique) }
