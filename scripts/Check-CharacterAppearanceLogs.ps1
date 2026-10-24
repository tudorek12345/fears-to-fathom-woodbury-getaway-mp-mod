[CmdletBinding()]
param([Parameter(Mandatory)][string[]]$LogPaths, [string[]]$RequiredScenes, [switch]$RequireMenu)
$ErrorActionPreference = 'Stop'
$problems = [Collections.Generic.List[string]]::new()
$avatarScenes = @('OfficeLayout','Typing Shooter Final scene','ParkingLotScene','RoadTripLoop','Pizzeria','CabinScene','CabinSceneDark')
$looks = @{
    host = @('2:4:2:2:2:4:3:3:1','2:4:0:2:0:6:3:3:2')
    client = @('1:2:3:0:1:1:2:2:2','1:2:4:0:2:7:2:2:3')
}
foreach ($role in @('host','client')) {
    $roleLogs = @($LogPaths | Where-Object { [IO.File]::ReadAllText($_) -match "Co-op session ${role}:" })
    if ($roleLogs.Count -ne 1) { $problems.Add("Expected one $role log."); continue }
    $text = [IO.File]::ReadAllText($roleLogs[0])
    if ($RequireMenu -and $text -notmatch 'CharacterMenu probe=PASS draft/discard/apply/reopen/close scene=MainMenu') { $problems.Add("$role character menu controls were not validated.") }
    foreach ($look in $looks[$role]) {
        if ($text -notmatch "CharacterAppearance received role=$role sid=[1-9]\d* revision=[1-9]\d* profile=$([regex]::Escape($look))") { $problems.Add("$role did not receive partner appearance $look") }
    }
    foreach ($scene in @($RequiredScenes | Select-Object -Unique | Where-Object { $_ -in $avatarScenes })) {
        $look = [regex]::Escape($looks[$role][1]); $scenePattern = [regex]::Escape($scene)
        if ($text -notmatch "CharacterAppearance applied scene=$scenePattern profile=$look vertices=[1-9]\d*") { $problems.Add("$role did not render changed partner appearance in $scene") }
    }
    if ($text -match 'CharacterMenu probe=FAIL') { $problems.Add("$role character menu reported a failure.") }
}
[pscustomobject]@{ Passed=$problems.Count -eq 0; Problems=@($problems) }
