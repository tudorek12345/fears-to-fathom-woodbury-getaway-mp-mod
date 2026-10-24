param([Parameter(Mandatory=$true)][string[]]$LogPaths, [Parameter(Mandatory=$true)][string[]]$RequiredScenes)
$ErrorActionPreference='Stop'
$problems=@()
$digests=@(@{},@{})
for($index=0;$index -lt $LogPaths.Count;$index++) {
    $path=$LogPaths[$index]
    $role=if($index -eq 0){'host'}else{'client'}
    $text=[IO.File]::ReadAllText((Resolve-Path -LiteralPath $path))
    if($text -match 'StoryParity [^\r\n]*result=FAIL') { $problems += "$path contains a story field digest failure" }
    foreach($scene in @('CabinScene','CabinSceneDark')) {
        if($RequiredScenes -contains $scene -and $text -notmatch ('StoryParity role='+$role+' scene='+$scene+' result=PASS fields=[1-9]\d* hash=-?\d+')) {
            $problems += "$path missing native story digest for $scene"
        }
    }
    if($index -lt 2) {
        foreach($match in [regex]::Matches($text,'StoryParity role='+$role+' scene=(\w+) result=PASS fields=([1-9]\d*) hash=(-?\d+)')) {
            $digests[$index][($match.Groups[1].Value+'|'+$match.Groups[2].Value+'|'+$match.Groups[3].Value)]=$true
        }
    }
    if(@($RequiredScenes | Where-Object {$_ -in @('OfficeLayout','ParkingLotScene','RoadTripLoop','Pizzeria')}).Count -gt 0 -and
        $text -notmatch 'SceneLookup requests=\d+ searches=\d+ reused=[1-9]\d*') {
        $problems += "$path missing scene lookup reuse evidence"
    }
}
foreach($scene in @('CabinScene','CabinSceneDark')) {
    if($RequiredScenes -contains $scene -and @($digests[0].Keys | Where-Object { $_.StartsWith($scene+'|') -and $digests[1].ContainsKey($_) }).Count -eq 0) {
        $problems += "No matching host/client field count and digest for $scene"
    }
}
[pscustomobject]@{Passed=$LogPaths.Count -eq 2 -and $problems.Count -eq 0;Problems=$problems}
