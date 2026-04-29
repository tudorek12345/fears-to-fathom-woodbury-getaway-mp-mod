param(
    [string]$GameDir = "C:\Games\Fears to Fathom - Woodbury Getaway",
    [int]$MaxSessionLogs = 2,
    [datetime]$Since = [datetime]::MinValue,
    [string]$OutputJson = "",
    [switch]$RequireTraffic,
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
        $recent = $items | Where-Object { $_.LastWriteTime -ge $Since }
        if ($recent.Count -gt 0) {
            $items = $recent
        }
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
    '(?i)\bfailed to decode\b'
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
    HostStateAppliedMax = Get-MaxRegexValue -Text $allText -Pattern 'HostState:\s*read=\d+,\s*enq=\d+,\s*applied=(\d+)'
    HostAppliedCountMax = Get-MaxRegexValue -Text $allText -Pattern 'HostApplied:\s*count=(\d+)'
    HostRxCountMax = Get-MaxRegexValue -Text $allText -Pattern 'HostRx:\s*count=(\d+)'
    HostTxCountMax = Get-MaxRegexValue -Text $allText -Pattern 'HostTx:.*count=(\d+)'
    DuplicateReadyIgnored = [regex]::Matches($allText, 'duplicate SceneReady ignored').Count
    SceneReadyAccepted = [regex]::Matches($allText, 'Co-op scene ready:').Count
    PendingRetryWarnings = [regex]::Matches($allText, 'Pending retry age:').Count
}

$trafficSignal = ($counters.HostStateAppliedMax -gt 0 -or $counters.HostAppliedCountMax -gt 0 -or $counters.HostTxCountMax -gt 0 -or $counters.HostRxCountMax -gt 0)
$trafficFailed = $RequireTraffic.IsPresent -and -not $trafficSignal

if ($trafficFailed) {
    $problemLines.Add([pscustomobject]@{
            File = "-"
            Line = 0
            Text = "No sync traffic counters were observed in selected logs."
        })
}

$summary = [pscustomobject]@{
    Passed = ($problemLines.Count -eq 0)
    Since = if ($Since -gt [datetime]::MinValue) { $Since } else { $null }
    SessionLogCount = $sessionLogs.Count
    BepInExLogCount = $bepLogs.Count
    SelectedLogs = @($allLogs | ForEach-Object { $_.FullName })
    Counters = $counters
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
