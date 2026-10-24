param(
    [string]$GameDir = "C:\Games\Fears to Fathom - Woodbury Getaway",
    [int]$MaxSessionLogs = 2,
    [datetime]$Since = [datetime]::MinValue,
    [string]$OutputJson = "",
    [switch]$RequireTraffic,
    [switch]$RequireGameplay,
    [switch]$RequirePlayerPoses,
    [string[]]$RequiredScenes = @(),
    [string[]]$LogPaths = @(),
    [switch]$NoFail
)

$ErrorActionPreference = "Stop"

function Get-RecentLogs {
    param(
        [string]$Root,
        [string]$Filter,
        [int]$MaxCount,
        [datetime]$Since
    )

    if (-not (Test-Path -LiteralPath $Root)) {
        return @()
    }

    $items = Get-ChildItem -LiteralPath $Root -Filter $Filter -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending
    if ($Since -gt [datetime]::MinValue) {
        $items = @($items | Where-Object { $_.LastWriteTime -ge $Since })
    }

    return @($items | Select-Object -First $MaxCount)
}

function Get-MaxRegexValue {
    param(
        [string]$Text,
        [string]$Pattern
    )

    if ([string]::IsNullOrEmpty($Text)) {
        return 0
    }

    $matches = [regex]::Matches($Text, $Pattern)
    if ($matches.Count -eq 0) {
        return 0
    }

    $max = 0
    foreach ($m in $matches) {
        $value = 0
        if ([int]::TryParse($m.Groups[1].Value, [ref]$value)) {
            if ($value -gt $max) {
                $max = $value
            }
        }
    }
    return $max
}

function Test-IgnoreLine {
    param([string]$Line)

    $ignorePatterns = @(
        '(?i)WSACancelBlockingCall',
        '(?i)forcibly closed',
        '(?i)transport connection',
        '(?i)SocketError\.OperationAborted',
        '(?i)SocketError\.ConnectionReset',
        '(?i)SocketError\.ConnectionAborted',
        '(?i)SocketError\.NotConnected'
    )

    foreach ($pattern in $ignorePatterns) {
        if ($Line -match $pattern) {
            return $true
        }
    }

    return $false
}

$sessionRoot = Join-Path $GameDir "BepInEx\logs"
$bepRoot = Join-Path $GameDir "BepInEx"

$sessionLogs = Get-RecentLogs -Root $sessionRoot -Filter "WoodburySpectatorSync_session_*.log" -MaxCount $MaxSessionLogs -Since $Since
$bepLogs = Get-RecentLogs -Root $bepRoot -Filter "LogOutput.log*" -MaxCount 4 -Since $Since

$allLogs = @()
$allLogs += $sessionLogs
$allLogs += $bepLogs
$allLogs = @($allLogs | Sort-Object FullName -Unique)
if ($LogPaths.Count -gt 0) {
    $allLogs = @($LogPaths | ForEach-Object { Get-Item -LiteralPath $_ -ErrorAction Stop })
    $sessionLogs = @($allLogs | Where-Object { $_.Name -like 'WoodburySpectatorSync_session_*' })
    $bepLogs = @()
}

if ($allLogs.Count -eq 0) {
    throw "No logs found under $GameDir"
}

$problemPatterns = @(
    '(?i)\bNullReferenceException\b',
    '(?i)\bIndexOutOfRangeException\b',
    '(?i)\bInvalidCastException\b',
    '(?i)\bSerializationException\b',
    '(?i)\bUnknown message type\b',
    '(?i)\bProtocol\b.*\b(error|fail|mismatch)\b',
    '(?i)\bpayload too large\b',
    '(?i)\bfailed to parse\b',
    '(?i)\bfailed to decode\b',
    '(?i)SnapshotAck (reported failure|incomplete|send failed)',
    '(?i)SnapshotEnd ApplyPendingStates threw'
    '(?i)SceneDiscoveryDumpCrawler: load-failed'
    '(?i)\bMissingReferenceException\b'
)

$exceptionPattern = '(?i)\bException\b'
$problemLines = New-Object System.Collections.Generic.List[object]
$allTextBuilder = New-Object System.Text.StringBuilder

foreach ($log in $allLogs) {
    $lineNumber = 0
    foreach ($line in Get-Content -LiteralPath $log.FullName -ErrorAction SilentlyContinue) {
        $lineNumber++
        [void]$allTextBuilder.AppendLine($line)

        if (Test-IgnoreLine -Line $line) {
            continue
        }

        $isProblem = $false
        foreach ($pattern in $problemPatterns) {
            if ($line -match $pattern) {
                $isProblem = $true
                break
            }
        }

        if (-not $isProblem -and $line -match $exceptionPattern) {
            $isProblem = $true
        }

        if ($line -match 'Pending retry age: max=([\d.]+)s' -and
            [double]::Parse($Matches[1], [Globalization.CultureInfo]::InvariantCulture) -ge 15 -and
            $line -match '\b(doors|holdables|ai|npc|cabinHouse|cabinGame|pizzeria|roadTrip|office|parkingLot)=[1-9]\d*') {
            $isProblem = $true
        }

        if ($isProblem) {
            $problemLines.Add([pscustomobject]@{
                    File = $log.FullName
                    Line = $lineNumber
                    Text = $line
                })
        }
    }
}

$allText = $allTextBuilder.ToString()
$counters = [ordered]@{
    HostStateAppliedMax = Get-MaxRegexValue -Text $allText -Pattern '(?:HostState:\s*read=\d+,\s*enq=\d+,\s*applied=|Sync:\s*state\s+\d+/\d+/)(\d+)'
    HostAppliedCountMax = Get-MaxRegexValue -Text $allText -Pattern '(?:HostApplied|Applied):\s*count=(\d+)'
    HostRxCountMax = Get-MaxRegexValue -Text $allText -Pattern 'HostRx:\s*count=(\d+)'
    HostTxCountMax = Get-MaxRegexValue -Text $allText -Pattern 'HostTx:.*count=(\d+)'
    DuplicateReadyIgnored = [regex]::Matches($allText, 'duplicate SceneReady ignored').Count
    SceneReadyAccepted = [regex]::Matches($allText, 'Co-op scene ready:').Count
    PendingRetryReports = [regex]::Matches($allText, 'Pending retry age:').Count
    PendingRetryWarnings = [regex]::Matches($allText, 'Pending retry age: max=(?!0(?:\.0+)?s)\d+(?:\.\d+)?s').Count
}

$trafficSignal = ($counters.HostStateAppliedMax -gt 0 -or $counters.HostAppliedCountMax -gt 0)
$trafficFailed = ($RequireTraffic.IsPresent -or $RequireGameplay.IsPresent) -and -not $trafficSignal

if ($trafficFailed) {
    $problemLines.Add([pscustomobject]@{
            File = "-"
            Line = 0
            Text = "No applied host-state counters were observed; receiving or sending packets alone does not prove sync."
        })
}

$gameplayRoles = @{}
foreach ($match in [regex]::Matches($allText, 'Co-op session (host|client): \S+ -> Live [^\r\n]*?scene=([^\r\n]+?) gen=(\d+) sid=(\d+)')) {
    $sceneName = $match.Groups[2].Value
    $key = $sceneName + '|' + $match.Groups[3].Value + '|' + $match.Groups[4].Value
    if (-not $gameplayRoles.ContainsKey($key)) { $gameplayRoles[$key] = @{} }
    $gameplayRoles[$key][$match.Groups[1].Value] = $true
}
$liveSessions = @($gameplayRoles.Keys | Where-Object {
    $gameplayRoles[$_].ContainsKey('host') -and $gameplayRoles[$_].ContainsKey('client')
})
$gameplaySessions = @($liveSessions | Where-Object { $_.Split('|')[0] -match '^(CabinScene|CabinSceneDark|CabinDarkScene|Pizzeria|RoadTripLoop|OfficeLayout|ParkingLotScene)$' })
if ($RequireGameplay.IsPresent -and $gameplaySessions.Count -eq 0) {
    $problemLines.Add([pscustomobject]@{
        File = '-'; Line = 0
        Text = 'Host and client did not both reach Live in the same gameplay scene, generation, and session.'
    })
}

$poseRoles = @{}
foreach ($match in [regex]::Matches($allText, 'Remote player pose received: player=([01]) [^\r\n]*?scene=(\S+) gen=(\d+) sid=(\d+) head=True torso=True leftArm=True rightArm=True')) {
    $key = $match.Groups[2].Value + '|' + $match.Groups[3].Value + '|' + $match.Groups[4].Value
    if (-not $poseRoles.ContainsKey($key)) { $poseRoles[$key] = @{} }
    $poseRoles[$key][$match.Groups[1].Value] = $true
}
$poseSessions = @($gameplaySessions | Where-Object {
    $poseRoles.ContainsKey($_) -and $poseRoles[$_].ContainsKey('0') -and $poseRoles[$_].ContainsKey('1')
})
if ($RequirePlayerPoses -and $poseSessions.Count -eq 0) {
    $problemLines.Add([pscustomobject]@{ File = '-'; Line = 0; Text = 'Both players did not receive full-rig poses in the same Live gameplay session.' })
}

# A pass in Cabin must never mask a failed or skipped scene elsewhere in the route.
$snapshotRoles = @{}
foreach ($match in [regex]::Matches($allText, 'SceneSnapshot role=(host|client) scene=([^\r\n]+?) gen=(\d+) sid=(\d+) story=(\d+) doors=(\d+) holdables=(\d+) ai=(\d+) custom=(\d+)([^\r\n]*)')) {
    $key = $match.Groups[2].Value + '|' + $match.Groups[3].Value + '|' + $match.Groups[4].Value
    if (-not $snapshotRoles.ContainsKey($key)) { $snapshotRoles[$key] = @{} }
    $snapshotRoles[$key][$match.Groups[1].Value] = [pscustomobject]@{
        Story = [int]$match.Groups[5].Value; Doors = [int]$match.Groups[6].Value
        Holdables = [int]$match.Groups[7].Value; Ai = [int]$match.Groups[8].Value
        Custom = [int]$match.Groups[9].Value
        Complete = $match.Groups[10].Value -match 'pending=0 missing=0 success=True'
    }
}
$sceneCoverage = @($RequiredScenes | Select-Object -Unique | ForEach-Object {
    $scene = $_
    $requiredVisits = @($RequiredScenes | Where-Object { $_ -ceq $scene }).Count
    $sessions = @($liveSessions | Where-Object { $_.Split('|')[0] -ceq $scene })
    $presentation = $scene -in @('CreditsScene','Typing Shooter Final scene')
    $utility = $presentation -or $scene -in @('MainMenu','Disclaimer')
    $valid = @($sessions | Where-Object {
        $key = $_
        if (-not $snapshotRoles.ContainsKey($key)) { return $false }
        $pair = $snapshotRoles[$key]
        if (-not $pair.ContainsKey('host') -or -not $pair.ContainsKey('client')) { return $false }
        $h = $pair.host; $c = $pair.client
        if ($utility) {
            $parts = $key.Split('|')
            $pattern = 'ScenePresentation role=client scene=' + [regex]::Escape($scene) + ' nodes=[1-9]\d* words=\d+ missing=0 gen=' + $parts[1] + ' sid=' + $parts[2] + '\b'
            $authority = 'ScenePresentationAuthority role=host scene=' + [regex]::Escape($scene) + ' enabled=True gen=' + $parts[1] + ' sid=' + $parts[2] + '\b'
            return $c.Complete -and (-not $presentation -or ($allText -match $pattern -and $allText -match $authority))
        }
        # These two diagnostic buckets differ: host counts emitted messages by phase,
        # client coalesces keys and puts typed vehicle/event messages in their own categories.
        # Compare counts only for the one-entry-per-key categories used by the runtime gate.
        return $c.Complete -and $h.Story -gt 0 -and $c.Story -gt 0 -and
            $h.Ai -gt 0 -and $c.Ai -gt 0 -and $c.Doors -ge $h.Doors -and
            $c.Holdables -ge $h.Holdables -and $c.Custom -ge $h.Custom -and
            (-not $RequirePlayerPoses -or $poseSessions -contains $key)
    })
    $escaped = [regex]::Escape($scene)
    $hostAdapter = [regex]::Match($allText, "SceneAdapter role=host scene=$escaped adapter=(\S+)").Groups[1].Value
    $clientAdapter = [regex]::Match($allText, "SceneAdapter role=client scene=$escaped adapter=(\S+)").Groups[1].Value
    $wired = ($hostAdapter -ne '' -and $hostAdapter -ceq $clientAdapter) -or $scene -in @('MainMenu','Disclaimer')
    $passed = $wired -and $valid.Count -ge $requiredVisits
    if (-not $passed) {
        $problemLines.Add([pscustomobject]@{ File = '-'; Line = 0
            Text = "Scene coverage incomplete: $scene (matchingAdapters=$wired, liveSessions=$($sessions.Count), completeSnapshotsWithRequiredPoses=$($valid.Count))." })
    }
    [pscustomobject]@{ Scene = $scene; Passed = $passed; Adapter = $hostAdapter; RequiredVisits = $requiredVisits
        MatchingAdapters = $wired; LiveSessions = $sessions; VerifiedSessions = $valid
        Snapshots = @($valid | ForEach-Object { [pscustomobject]@{ Session = $_; Host = $snapshotRoles[$_].host; Client = $snapshotRoles[$_].client } }) }
})

$summary = [pscustomobject]@{
    Passed = ($problemLines.Count -eq 0)
    Since = if ($Since -gt [datetime]::MinValue) { $Since } else { $null }
    SessionLogCount = $sessionLogs.Count
    BepInExLogCount = $bepLogs.Count
    SelectedLogs = @($allLogs | ForEach-Object { $_.FullName })
    Counters = $counters
    GameplaySessions = $gameplaySessions
    LiveSessions = $liveSessions
    PlayerPoseSessions = $poseSessions
    SceneCoverage = $sceneCoverage
    ProblemCount = $problemLines.Count
    Problems = @($problemLines | Select-Object -First 80)
}

Write-Host "Selected logs:"
foreach ($path in $summary.SelectedLogs) {
    Write-Host " - $path"
}

Write-Host "Counters:"
Write-Host " - HostStateAppliedMax: $($counters.HostStateAppliedMax)"
Write-Host " - HostAppliedCountMax: $($counters.HostAppliedCountMax)"
Write-Host " - HostRxCountMax: $($counters.HostRxCountMax)"
Write-Host " - HostTxCountMax: $($counters.HostTxCountMax)"
Write-Host " - SceneReadyAccepted: $($counters.SceneReadyAccepted)"
Write-Host " - DuplicateReadyIgnored: $($counters.DuplicateReadyIgnored)"
Write-Host " - PendingRetryWarnings: $($counters.PendingRetryWarnings)"

if ($summary.ProblemCount -gt 0) {
    Write-Host "Problems:"
    foreach ($problem in $summary.Problems) {
        Write-Host " - $($problem.File):$($problem.Line) $($problem.Text)"
    }
}

if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    $outDir = Split-Path -Parent $OutputJson
    if (-not [string]::IsNullOrWhiteSpace($outDir)) {
        New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    }
    $summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutputJson -Encoding UTF8
}

$summary

if (-not $NoFail.IsPresent -and -not $summary.Passed) {
    exit 1
}
