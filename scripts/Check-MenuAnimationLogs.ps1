param([Parameter(Mandatory=$true)][string[]]$LogPaths, [Parameter(Mandatory=$true)][string[]]$RequiredScenes)
$ErrorActionPreference = 'Stop'
$problems = @()
$gameplay = @('OfficeLayout','ParkingLotScene','RoadTripLoop','Pizzeria','CabinScene','CabinSceneDark')
foreach ($path in $LogPaths) {
    $text = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $path))
    foreach ($marker in @('NativeMultiplayerMenu probe=PASS','NativeMultiplayerMenu connection=PASS')) {
        if (-not $text.Contains($marker)) { $problems += "$path missing $marker" }
    }
    if ($text -match 'NativeMultiplayerMenu (probe|connection)=FAIL|CharacterAnimationProbe [^\r\n]*result=FAIL') { $problems += "$path contains a failed menu/animation probe" }
    foreach ($scene in @($RequiredScenes | Select-Object -Unique)) {
        if ($gameplay -contains $scene -and $text -notmatch ('CharacterAnimationProbe scene=' + [regex]::Escape($scene) + ' result=PASS ')) {
            $problems += "$path missing complete animation probe for $scene"
        }
    }
}
[pscustomobject]@{ Passed=$LogPaths.Count -eq 2 -and $problems.Count -eq 0; Problems=$problems }
