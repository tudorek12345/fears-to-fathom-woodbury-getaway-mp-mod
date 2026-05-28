param(
    [Parameter(Mandatory = $true)]
    [string]$HostLog,

    [Parameter(Mandatory = $true)]
    [string]$ClientLog,

    [string]$Scene = "",

    [ValidateSet("Auto", "Manual", "Both")]
    [string]$DumpKind = "Both",

    [int]$Top = 200,

    [int]$MaxValueLength = 220,

    [switch]$IncludeVolatile,

    [switch]$FullValues,

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

    $fields = @{}
    $lineNumber = 0
    $fieldCount = 0

    foreach ($line in Get-Content -LiteralPath $Path -ErrorAction Stop) {
        $lineNumber++
        if ($line -notmatch 'SceneDiscoveryDump(Manual)?Field\|') {
            continue
        }

        $record = Split-DumpLine -Line $line
        if ($null -eq $record) {
            continue
        }

        if ($record.RecordType -ne "SceneDiscoveryDumpField" -and
            $record.RecordType -ne "SceneDiscoveryDumpManualField") {
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

        if (-not $IncludeVolatile.IsPresent -and (Test-VolatileField -Record $record)) {
            continue
        }

        $fieldCount++
        $record | Add-Member -NotePropertyName DumpKind -NotePropertyValue $kind -Force
        $record | Add-Member -NotePropertyName SourceLog -NotePropertyValue $Path -Force
        $record | Add-Member -NotePropertyName SourceLine -NotePropertyValue $lineNumber -Force
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

        $fields[$key] = $record
    }

    return [pscustomobject]@{
        Path = $Path
        Role = $ExpectedRole
        FieldCount = $fieldCount
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
    HostFieldCount = $hostDump.FieldCount
    ClientFieldCount = $clientDump.FieldCount
    DiffCount = $orderedDiffs.Count
    ValueMismatchCount = @($orderedDiffs | Where-Object { $_.Kind -eq "ValueMismatch" }).Count
    MissingOnHostCount = @($orderedDiffs | Where-Object { $_.Kind -eq "MissingOnHost" }).Count
    MissingOnClientCount = @($orderedDiffs | Where-Object { $_.Kind -eq "MissingOnClient" }).Count
    Diffs = $shownDiffs
}

Write-Host "SceneDiscoveryDump diff summary:"
Write-Host " - Host fields: $($summary.HostFieldCount)"
Write-Host " - Client fields: $($summary.ClientFieldCount)"
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
