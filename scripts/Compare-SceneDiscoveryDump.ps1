param(
    [Parameter(Mandatory = $true)]
    [string]$HostLog,

    [Parameter(Mandatory = $true)]
    [string]$ClientLog,

    [string]$Scene = "",

    [ValidateSet("Auto", "Manual", "Timed", "Crawler", "Both")]
    [string]$DumpKind = "Both",

    [int]$Top = 200,

    [int]$MaxValueLength = 220,

    [switch]$IncludeVolatile,

    [switch]$FullValues,

    [switch]$LatestOnly,

    [switch]$PassThru,

    [string]$OutputJson = ""
)

$ErrorActionPreference = "Stop"

function Unescape-DumpValue {
    param([string]$Value)

    if ($null -eq $Value) {
        return ""
    }

    try {
        return [uri]::UnescapeDataString($Value)
    }
    catch {
        return $Value
    }
}

function Split-DumpLine {
    param([string]$Line)

    $markerIndex = $Line.IndexOf("SceneDiscoveryDump", [StringComparison]::Ordinal)
    if ($markerIndex -lt 0) {
        return $null
    }

    $payload = $Line.Substring($markerIndex)
    $parts = $payload -split '\|'
    if ($parts.Count -lt 2) {
        return $null
    }

    $record = [ordered]@{
        RecordType = $parts[0]
    }

    for ($i = 1; $i -lt $parts.Count; $i++) {
        $eq = $parts[$i].IndexOf("=")
        if ($eq -le 0) {
            continue
        }

        $name = $parts[$i].Substring(0, $eq)
        $value = $parts[$i].Substring($eq + 1)
        $record[$name] = Unescape-DumpValue $value
    }

    return [pscustomobject]$record
}

function Normalize-DumpValue {
    param([string]$Value)

    if ($null -eq $Value) {
        return ""
    }

    $normalized = $Value
    $normalized = $normalized -replace 'id=-?\d+', 'id=<id>'
    $normalized = $normalized -replace 'pid\d+', 'pid<pid>'
    return $normalized
}

function Test-VolatileField {
    param([object]$Record)

    $name = "$($Record.fieldName)"
    $type = "$($Record.fieldType)"
    $path = "$($Record.componentPath)"

    $volatilePatterns = @(
        '(?i)mouse',
        '(?i)cursor',
        '(?i)input',
        '(?i)delta',
        '(?i)elapsed',
        '(?i)timer',
        '(?i)time',
        '(?i)last',
        '(?i)audio',
        '(?i)clip',
        '(?i)volume',
        '(?i)pitch',
        '(?i)camera',
        '(?i)velocity',
        '(?i)speed'
    )

    foreach ($pattern in $volatilePatterns) {
        if ($name -match $pattern -or $type -match $pattern -or $path -match $pattern) {
            return $true
        }
    }

    return $false
}

function Get-DumpKind {
    param([string]$RecordType)

    if ($RecordType -like "SceneDiscoveryDumpManual*") {
        return "Manual"
    }

    if ($RecordType -like "SceneDiscoveryDumpTimed*") {
        return "Timed"
    }

    if ($RecordType -like "SceneDiscoveryDumpCrawler*") {
        return "Crawler"
    }

    return "Auto"
}

function Read-DumpFields {
    param(
        [string]$Path,
        [string]$ExpectedRole
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Log file not found: $Path"
    }

    $blocks = New-Object System.Collections.Generic.List[object]
    $currentBlock = $null
    $lineNumber = 0
    $blockSequence = 0

    foreach ($line in Get-Content -LiteralPath $Path -ErrorAction Stop) {
        $lineNumber++
        if ($line -notmatch 'SceneDiscoveryDump(Manual|Timed|Crawler)?(Begin|Field|End)\|') {
            continue
        }

        $record = Split-DumpLine -Line $line
        if ($null -eq $record) {
            continue
        }

        $isBegin = $record.RecordType -eq "SceneDiscoveryDumpBegin" -or
            $record.RecordType -eq "SceneDiscoveryDumpManualBegin" -or
            $record.RecordType -eq "SceneDiscoveryDumpTimedBegin" -or
            $record.RecordType -eq "SceneDiscoveryDumpCrawlerBegin"
        $isField = $record.RecordType -eq "SceneDiscoveryDumpField" -or
            $record.RecordType -eq "SceneDiscoveryDumpManualField" -or
            $record.RecordType -eq "SceneDiscoveryDumpTimedField" -or
            $record.RecordType -eq "SceneDiscoveryDumpCrawlerField"
        $isEnd = $record.RecordType -eq "SceneDiscoveryDumpEnd" -or
            $record.RecordType -eq "SceneDiscoveryDumpManualEnd" -or
            $record.RecordType -eq "SceneDiscoveryDumpTimedEnd" -or
            $record.RecordType -eq "SceneDiscoveryDumpCrawlerEnd"

        if (-not $isBegin -and -not $isField -and -not $isEnd) {
            continue
        }

        if (-not [string]::IsNullOrWhiteSpace($ExpectedRole) -and
            "$($record.role)" -ne $ExpectedRole) {
            continue
        }

        $kind = Get-DumpKind -RecordType $record.RecordType
        if ($DumpKind -ne "Both" -and $kind -ne $DumpKind) {
            continue
        }

        if (-not [string]::IsNullOrWhiteSpace($Scene) -and "$($record.scene)" -ne $Scene) {
            continue
        }

        if ($isBegin) {
            $blockSequence++
            $currentBlock = [pscustomobject][ordered]@{
                Sequence = $blockSequence
                DumpKind = $kind
                Scene = "$($record.scene)"
                BeginLine = $lineNumber
                EndLine = 0
                DeclaredComponents = if ($record.PSObject.Properties.Name -contains "components") { [int]$record.components } else { 0 }
                DeclaredFields = if ($record.PSObject.Properties.Name -contains "fields") { [int]$record.fields } else { 0 }
                Fields = @{}
                FieldCount = 0
            }
            $blocks.Add($currentBlock)
            continue
        }

        if ($isEnd) {
            if ($null -ne $currentBlock -and
                "$($currentBlock.DumpKind)" -eq $kind -and
                "$($currentBlock.Scene)" -eq "$($record.scene)") {
                $currentBlock.EndLine = $lineNumber
                $currentBlock = $null
            }
            continue
        }

        if (-not $IncludeVolatile.IsPresent -and (Test-VolatileField -Record $record)) {
            continue
        }

        if ($null -eq $currentBlock) {
            $blockSequence++
            $currentBlock = [pscustomobject][ordered]@{
                Sequence = $blockSequence
                DumpKind = $kind
                Scene = "$($record.scene)"
                BeginLine = 0
                EndLine = 0
                DeclaredComponents = 0
                DeclaredFields = 0
                Fields = @{}
                FieldCount = 0
            }
            $blocks.Add($currentBlock)
        }

        $currentBlock.FieldCount++
        $record | Add-Member -NotePropertyName DumpKind -NotePropertyValue $kind -Force
        $record | Add-Member -NotePropertyName SourceLog -NotePropertyValue $Path -Force
        $record | Add-Member -NotePropertyName SourceLine -NotePropertyValue $lineNumber -Force
        $record | Add-Member -NotePropertyName BlockSequence -NotePropertyValue $currentBlock.Sequence -Force
        $record | Add-Member -NotePropertyName BlockBeginLine -NotePropertyValue $currentBlock.BeginLine -Force
        $record | Add-Member -NotePropertyName NormalizedValue -NotePropertyValue (Normalize-DumpValue "$($record.fieldValue)") -Force

        $key = @(
            $kind,
            "$($record.scene)",
            "$($record.componentType)",
            "$($record.componentPath)",
            "$($record.fieldDeclaringType)",
            "$($record.fieldName)",
            "$($record.fieldType)"
        ) -join "|"

        $currentBlock.Fields[$key] = $record
    }

    $eligibleBlocks = @($blocks | Where-Object {
        $_.FieldCount -gt 0 -and
        ($DumpKind -eq "Both" -or $_.DumpKind -eq $DumpKind) -and
        ([string]::IsNullOrWhiteSpace($Scene) -or $_.Scene -eq $Scene)
    })

    $selectedBlocks = if ($LatestOnly.IsPresent) {
        @($eligibleBlocks | Sort-Object Sequence -Descending | Select-Object -First 1)
    } else {
        $eligibleBlocks
    }

    $fields = @{}
    $fieldCount = 0
    foreach ($block in $selectedBlocks) {
        foreach ($key in $block.Fields.Keys) {
            $fields[$key] = $block.Fields[$key]
            $fieldCount++
        }
    }

    return [pscustomobject]@{
        Path = $Path
        Role = $ExpectedRole
        FieldCount = $fieldCount
        BlockCount = $eligibleBlocks.Count
        SelectedBlocks = $selectedBlocks
        Fields = $fields
    }
}

function New-DiffRecord {
    param(
        [string]$Kind,
        [object]$HostRecord,
        [object]$ClientRecord
    )

    $source = if ($null -ne $HostRecord) { $HostRecord } else { $ClientRecord }
    [pscustomobject]@{
        Kind = $Kind
        DumpKind = "$($source.DumpKind)"
        Scene = "$($source.scene)"
        ComponentType = "$($source.componentType)"
        ComponentPath = "$($source.componentPath)"
        FieldDeclaringType = "$($source.fieldDeclaringType)"
        FieldName = "$($source.fieldName)"
        FieldType = "$($source.fieldType)"
        HostValue = if ($null -ne $HostRecord) { "$($HostRecord.fieldValue)" } else { "" }
        ClientValue = if ($null -ne $ClientRecord) { "$($ClientRecord.fieldValue)" } else { "" }
        HostLine = if ($null -ne $HostRecord) { $HostRecord.SourceLine } else { 0 }
        ClientLine = if ($null -ne $ClientRecord) { $ClientRecord.SourceLine } else { 0 }
    }
}

function Format-DisplayValue {
    param([string]$Value)

    if ($FullValues.IsPresent -or $MaxValueLength -le 0) {
        return $Value
    }

    if ($null -eq $Value) {
        return ""
    }

    if ($Value.Length -le $MaxValueLength) {
        return $Value
    }

    return $Value.Substring(0, $MaxValueLength) + "...<truncated>"
}

$hostDump = Read-DumpFields -Path $HostLog -ExpectedRole "host"
$clientDump = Read-DumpFields -Path $ClientLog -ExpectedRole "client"

$allKeys = New-Object System.Collections.Generic.HashSet[string]
foreach ($key in $hostDump.Fields.Keys) {
    [void]$allKeys.Add($key)
}
foreach ($key in $clientDump.Fields.Keys) {
    [void]$allKeys.Add($key)
}

$diffs = New-Object System.Collections.Generic.List[object]
foreach ($key in $allKeys) {
    $hostRecord = $null
    $clientRecord = $null
    if ($hostDump.Fields.ContainsKey($key)) {
        $hostRecord = $hostDump.Fields[$key]
    }
    if ($clientDump.Fields.ContainsKey($key)) {
        $clientRecord = $clientDump.Fields[$key]
    }

    if ($null -eq $hostRecord) {
        $diffs.Add((New-DiffRecord -Kind "MissingOnHost" -HostRecord $null -ClientRecord $clientRecord))
    }
    elseif ($null -eq $clientRecord) {
        $diffs.Add((New-DiffRecord -Kind "MissingOnClient" -HostRecord $hostRecord -ClientRecord $null))
    }
    elseif ("$($hostRecord.NormalizedValue)" -ne "$($clientRecord.NormalizedValue)") {
        $diffs.Add((New-DiffRecord -Kind "ValueMismatch" -HostRecord $hostRecord -ClientRecord $clientRecord))
    }
}

$orderedDiffs = @($diffs | Sort-Object Scene, ComponentType, ComponentPath, FieldName, Kind)
$shownDiffs = @($orderedDiffs | Select-Object -First $Top)

$summary = [pscustomobject]@{
    HostLog = $HostLog
    ClientLog = $ClientLog
    Scene = if ([string]::IsNullOrWhiteSpace($Scene)) { $null } else { $Scene }
    DumpKind = $DumpKind
    IncludeVolatile = $IncludeVolatile.IsPresent
    LatestOnly = $LatestOnly.IsPresent
    HostFieldCount = $hostDump.FieldCount
    ClientFieldCount = $clientDump.FieldCount
    HostBlockCount = $hostDump.BlockCount
    ClientBlockCount = $clientDump.BlockCount
    HostSelectedBlock = @($hostDump.SelectedBlocks | Select-Object -First 1 | ForEach-Object { "$($_.DumpKind):$($_.Scene):seq=$($_.Sequence):lines=$($_.BeginLine)-$($_.EndLine)" }) -join ","
    ClientSelectedBlock = @($clientDump.SelectedBlocks | Select-Object -First 1 | ForEach-Object { "$($_.DumpKind):$($_.Scene):seq=$($_.Sequence):lines=$($_.BeginLine)-$($_.EndLine)" }) -join ","
    DiffCount = $orderedDiffs.Count
    ValueMismatchCount = @($orderedDiffs | Where-Object { $_.Kind -eq "ValueMismatch" }).Count
    MissingOnHostCount = @($orderedDiffs | Where-Object { $_.Kind -eq "MissingOnHost" }).Count
    MissingOnClientCount = @($orderedDiffs | Where-Object { $_.Kind -eq "MissingOnClient" }).Count
    Diffs = $shownDiffs
}

Write-Host "SceneDiscoveryDump diff summary:"
Write-Host " - Host fields: $($summary.HostFieldCount)"
Write-Host " - Client fields: $($summary.ClientFieldCount)"
Write-Host " - Host blocks: $($summary.HostBlockCount)"
Write-Host " - Client blocks: $($summary.ClientBlockCount)"
if ($LatestOnly.IsPresent) {
    Write-Host " - Host selected: $($summary.HostSelectedBlock)"
    Write-Host " - Client selected: $($summary.ClientSelectedBlock)"
}
Write-Host " - Diffs: $($summary.DiffCount)"
Write-Host " - Value mismatches: $($summary.ValueMismatchCount)"
Write-Host " - Missing on host: $($summary.MissingOnHostCount)"
Write-Host " - Missing on client: $($summary.MissingOnClientCount)"

foreach ($diff in $shownDiffs) {
    Write-Host ("SceneDumpDiff|kind={0}|dump={1}|scene={2}|componentType={3}|componentPath={4}|field={5}|fieldType={6}|host={7}|client={8}" -f
        $diff.Kind,
        $diff.DumpKind,
        $diff.Scene,
        $diff.ComponentType,
        $diff.ComponentPath,
        $diff.FieldName,
        $diff.FieldType,
        (Format-DisplayValue $diff.HostValue),
        (Format-DisplayValue $diff.ClientValue))
}

if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    $outDir = Split-Path -Parent $OutputJson
    if (-not [string]::IsNullOrWhiteSpace($outDir)) {
        New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    }

    $summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutputJson -Encoding UTF8
}

if ($PassThru.IsPresent) {
    $summary
}
