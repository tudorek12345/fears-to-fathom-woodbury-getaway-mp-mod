[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$source = Get-Content -LiteralPath (Join-Path $repoRoot 'src/WoodburySpectatorSync/Coop/SceneHandshakeState.cs') -Raw
if (-not ('WoodburySyncRegression' -as [type])) {
    Add-Type -TypeDefinition ($source + @'

public static class WoodburySyncRegression
{
    public static int Run()
    {
        int checks = 0;
        System.Action<bool, string> expect = (value, name) => {
            if (!value) throw new System.Exception(name);
            checks++;
        };
        var state = new WoodburySpectatorSync.Coop.SceneHandshakeState();
        state.Begin(5, "CabinSceneDark", true, 0);
        expect(!state.AcceptReady("OfficeLayout", 1), "Wrong scene must not become ready");
        expect(state.AcceptReady("CabinSceneDark", 1), "Matching scene becomes ready");
        expect(!state.AcknowledgeSnapshot(5, true), "Unsent snapshot must not acknowledge");
        state.MarkSnapshotBegin(5);
        expect(!state.AcknowledgeSnapshot(5, true), "Missing SnapshotEnd must not acknowledge");
        state.MarkSnapshotEnd(5);
        expect(!state.AcknowledgeSnapshot(4, true), "Stale generation must not acknowledge");
        expect(!state.AcknowledgeSnapshot(5, false), "Failed application must not acknowledge");
        expect(!state.IsSnapshotAcknowledged(), "Failure must leave retry enabled");
        expect(!WoodburySpectatorSync.Coop.SceneHandshakeState.CanCompleteSnapshot(false, 0, 0, 2, 2, 1, 1, 3, 3), "Application exception must fail even with empty pending state");
        expect(!WoodburySpectatorSync.Coop.SceneHandshakeState.CanCompleteSnapshot(true, 1, 0, 2, 2, 1, 1, 3, 3), "Pending objects must fail");
        expect(!WoodburySpectatorSync.Coop.SceneHandshakeState.CanCompleteSnapshot(true, 0, 1, 2, 2, 1, 1, 3, 3), "Missing objects must fail");
        expect(!WoodburySpectatorSync.Coop.SceneHandshakeState.CanCompleteSnapshot(true, 0, 0, 1, 2, 1, 1, 3, 3), "Missing door must fail");
        expect(!WoodburySpectatorSync.Coop.SceneHandshakeState.CanCompleteSnapshot(true, 0, 0, 2, 2, 0, 1, 3, 3), "Missing holdable must fail");
        expect(!WoodburySpectatorSync.Coop.SceneHandshakeState.CanCompleteSnapshot(true, 0, 0, 2, 2, 1, 1, 2, 3), "Missing NPC must fail");
        bool complete = WoodburySpectatorSync.Coop.SceneHandshakeState.CanCompleteSnapshot(true, 0, 0, 2, 2, 1, 1, 3, 3);
        expect(complete && state.AcknowledgeSnapshot(5, complete), "Complete retry must acknowledge");
        expect(state.IsSnapshotAcknowledged(), "Complete retry opens acknowledgement gate");
        expect(!state.AcknowledgeSnapshot(5, true), "Duplicate acknowledgement is ignored");
        state.Begin(6, "OfficeLayout", true, 10);
        expect(!state.IsSnapshotAcknowledged(), "Scene change clears acknowledgement");
        expect(!state.AcknowledgeSnapshot(5, true), "Previous scene acknowledgement is rejected");
        return checks;
    }
}
'@)
}
$checks = [WoodburySyncRegression]::Run()
$fixtureRoot = Join-Path $repoRoot ('build/sync-regression-' + [guid]::NewGuid().ToString('N'))
$checkScript = Join-Path $PSScriptRoot 'Check-CoopLogs.ps1'
function Test-LogCase {
    param([string]$Name, [string]$HostText, [string]$ClientText, [bool]$Expected, [bool]$RequirePoses = $false, [string[]]$Scenes = @())
    $caseRoot = Join-Path $fixtureRoot $Name
    $logs = Join-Path $caseRoot 'BepInEx/logs'
    New-Item -ItemType Directory -Path $logs -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $logs 'WoodburySpectatorSync_session_CoopHost.log') -Value $HostText
    Set-Content -LiteralPath (Join-Path $logs 'WoodburySpectatorSync_session_CoopClient.log') -Value $ClientText
    $result = & $checkScript -GameDir $caseRoot -RequireTraffic -RequireGameplay -RequirePlayerPoses:$RequirePoses -RequiredScenes $Scenes -NoFail 6>$null
    if ($result.Passed -ne $Expected) { throw "Log regression failed: $Name" }
}
$hostLive = 'Co-op session host: SnapshotApplying -> Live reason=snapshot-ack scene=CabinSceneDark gen=5 sid=1'
$clientLive = 'Co-op session client: SnapshotApplying -> Live reason=snapshot-end-applied scene=CabinSceneDark gen=5 sid=1'
$applied = 'Sync: state 10/10/10 latch 5>5 net=1ms'
Test-LogCase 'healthy' $hostLive ($clientLive + "`n" + $applied) $true
Test-LogCase 'legacy-counters' $hostLive ($clientLive + "`nHostState: read=10, enq=10, applied=10") $true
Test-LogCase 'receive-only' $hostLive ($clientLive + "`nHostRx: count=3028") $false
Test-LogCase 'menu-only' ($hostLive.Replace('CabinSceneDark','MainMenu')) ($clientLive.Replace('CabinSceneDark','MainMenu') + "`n" + $applied) $false
Test-LogCase 'different-generation' $hostLive ($clientLive.Replace('gen=5','gen=6') + "`n" + $applied) $false
Test-LogCase 'missing-host' '' ($clientLive + "`n" + $applied) $false
Test-LogCase 'failed-snapshot' ($hostLive + "`nCo-op session host: SnapshotAck reported failure reason=pending=1") ($clientLive + "`n" + $applied) $false
Test-LogCase 'stalled-objects' $hostLive ($clientLive + "`n" + $applied + "`nPending retry age: max=18.0s doors=0 pizzeria=1") $false
Test-LogCase 'empty-pending' $hostLive ($clientLive + "`n" + $applied + "`nPending retry age: max=0.0s doors=0 pizzeria=0") $true
$rig = ' head=True torso=True leftArm=True rightArm=True procedural=True'
$hostPose = "`nRemote player pose received: player=1 seq=20 flags=Valid scene=CabinSceneDark gen=5 sid=1$rig"
$clientPose = "`nRemote player pose received: player=0 seq=20 flags=Valid scene=CabinSceneDark gen=5 sid=1$rig"
Test-LogCase 'poses-both' ($hostLive + $hostPose) ($clientLive + "`n" + $applied + $clientPose) $true $true
Test-LogCase 'poses-one' $hostLive ($clientLive + "`n" + $applied + $clientPose) $false $true
Test-LogCase 'poses-wrong-scene' ($hostLive + $hostPose.Replace('gen=5','gen=4')) ($clientLive + "`n" + $applied + $clientPose) $false $true
Test-LogCase 'poses-no-bones' ($hostLive + $hostPose.Replace('leftArm=True','leftArm=False')) ($clientLive + "`n" + $applied + $clientPose) $false $true
$hostWorld = "`nSceneAdapter role=host scene=CabinSceneDark adapter=Cabin`nSceneSnapshot role=host scene=CabinSceneDark gen=5 sid=1 story=30 doors=2 holdables=4 ai=5 custom=1 total=42"
$clientWorld = "`nSceneAdapter role=client scene=CabinSceneDark adapter=Cabin`nSceneSnapshot role=client scene=CabinSceneDark gen=5 sid=1 story=30 doors=2 holdables=4 ai=5 custom=1 pending=0 missing=0 success=True"
$h = $hostLive + $hostPose + $hostWorld
$c = $clientLive + "`n" + $applied + $clientPose + $clientWorld
Test-LogCase 'scene-complete' $h $c $true $true @('CabinSceneDark')
Test-LogCase 'scene-skipped' $h $c $false $true @('CabinSceneDark','OfficeLayout')
Test-LogCase 'scene-adapter-mismatch' $h ($c.Replace('adapter=Cabin','adapter=Office')) $false $true @('CabinSceneDark')
Test-LogCase 'scene-stale-snapshot' $h ($c.Replace('role=client scene=CabinSceneDark gen=5','role=client scene=CabinSceneDark gen=4')) $false $true @('CabinSceneDark')
Test-LogCase 'scene-no-world' $h ($c.Replace('story=30','story=0')) $false $true @('CabinSceneDark')
Test-LogCase 'scene-missing-ai' $h ($c.Replace('ai=5','ai=0')) $false $true @('CabinSceneDark')
Test-LogCase 'scene-coalesced-type-buckets' ($h.Replace('story=30','story=31').Replace('ai=5','ai=10')) $c $true $true @('CabinSceneDark')
Test-LogCase 'scene-incomplete-snapshot' $h ($c.Replace('holdables=4','holdables=3')) $false $true @('CabinSceneDark')
Test-LogCase 'scene-unresolved' $h ($c.Replace('pending=0','pending=1')) $false $true @('CabinSceneDark')
Test-LogCase 'scene-no-pose' $h ($c.Replace('head=True','head=False')) $false $true @('CabinSceneDark')
Test-LogCase 'scene-load-failure' ($h + "`nSceneDiscoveryDumpCrawler: load-failed scene=OfficeLayout") $c $false $true @('CabinSceneDark')
$h2 = $h.Replace('CabinSceneDark','OfficeLayout').Replace('adapter=Cabin','adapter=OfficeLayout').Replace('gen=5','gen=6')
$c2 = $c.Replace('CabinSceneDark','OfficeLayout').Replace('adapter=Cabin','adapter=OfficeLayout').Replace('gen=5','gen=6')
Test-LogCase 'two-scenes-complete' ($h + "`n" + $h2) ($c + "`n" + $c2) $true $true @('CabinSceneDark','OfficeLayout')
Test-LogCase 'return-visit-missing' $h $c $false $true @('CabinSceneDark','CabinSceneDark')
Test-LogCase 'return-visit-complete' ($h + "`n" + $h.Replace('gen=5','gen=6')) ($c + "`n" + $c.Replace('gen=5','gen=6')) $true $true @('CabinSceneDark','CabinSceneDark')
$miniH = $h.Replace('CabinSceneDark','Typing Shooter Final scene').Replace('adapter=Cabin','adapter=TypingShooter')
$miniH += "`nScenePresentationAuthority role=host scene=Typing Shooter Final scene enabled=True gen=5 sid=1"
$miniC = $c.Replace('CabinSceneDark','Typing Shooter Final scene').Replace('adapter=Cabin','adapter=TypingShooter')
$presentation = "`nScenePresentation role=client scene=Typing Shooter Final scene nodes=83 words=2 missing=0 gen=5 sid=1"
Test-LogCase 'additive-menu-without-mirror' ($h + "`n" + $miniH) ($c + "`n" + $miniC) $false $true @('CabinSceneDark','Typing Shooter Final scene')
Test-LogCase 'additive-mirror' ($h + "`n" + $miniH) ($c + "`n" + $miniC + $presentation) $true $true @('CabinSceneDark','Typing Shooter Final scene')
Test-LogCase 'additive-stale-mirror' ($h + "`n" + $miniH) ($c + "`n" + $miniC + $presentation.Replace('gen=5','gen=4')) $false $true @('CabinSceneDark','Typing Shooter Final scene')
Test-LogCase 'additive-missing-node' ($h + "`n" + $miniH) ($c + "`n" + $miniC + $presentation.Replace('missing=0','missing=1')) $false $true @('CabinSceneDark','Typing Shooter Final scene')
Test-LogCase 'additive-host-suppressed' ($h + "`n" + $miniH.Replace('enabled=True','enabled=False')) ($c + "`n" + $miniC + $presentation) $false $true @('CabinSceneDark','Typing Shooter Final scene')
$rejected = $false
try {
    & $checkScript -GameDir (Join-Path $fixtureRoot 'healthy') -Since ((Get-Date).AddMinutes(1)) -NoFail 6>$null | Out-Null
} catch {
    if ($_.Exception.Message -notlike 'No logs found*') { throw }
    $rejected = $true
}
if (-not $rejected) { throw 'Stale logs were selected after the requested cutoff.' }
Write-Host "PASS: $checks snapshot checks and 33 log-check regressions."

$unityFixture = Join-Path $fixtureRoot 'unity-errors.log'
$classifier = Join-Path $PSScriptRoot 'Classify-UnityLogErrors.ps1'
$native = 'System.EntryPointNotFoundException: SteamAPI_ManualDispatch_Init assembly:<unknown>'
$achievement = "NullReferenceException: Object reference not set to an instance of an object`n  at Steamworks.Data.Achievement.Trigger (System.Boolean apply)"
foreach ($case in @(
    @{ Text=$native; Runtime=0; Environment=1 },
    @{ Text="$native`n$achievement"; Runtime=0; Environment=2 },
    @{ Text=$achievement; Runtime=1; Environment=0 },
    @{ Text="$native`nNullReferenceException: other fault`n  at BlindCrouchHeight.OnTriggerEnter ()"; Runtime=1; Environment=1 },
    @{ Text="$native`nA crash has been intercepted by the crash handler."; Runtime=1; Environment=1 },
    @{ Text='Could not allocate memory: System out of memory!'; Runtime=1; Environment=0 },
    @{ Text="SkinnedMeshRenderer: Rendering stopped because the data for mesh 'Woodbury traveler skin' does not match"; Runtime=1; Environment=0 }
)) {
    Set-Content -LiteralPath $unityFixture -Value $case.Text -Encoding UTF8
    $classified = & $classifier -LogPaths @($unityFixture)
    if ($classified.RuntimeErrors.Count -ne $case.Runtime -or $classified.EnvironmentErrors.Count -ne $case.Environment) { throw 'Unity error classification regression.' }
}
Write-Host 'PASS: 7 Unity error classification regressions.'

$menuFixtureHost = Join-Path $fixtureRoot 'menu-animation-host.log'
$menuFixtureClient = Join-Path $fixtureRoot 'menu-animation-client.log'
$goodProbe = "NativeMultiplayerMenu probe=PASS`nNativeMultiplayerMenu connection=PASS`nCharacterAnimationProbe scene=OfficeLayout result=PASS walk/run/strafe/backward/crouch/sit/jump/head/hand/restore"
foreach ($case in @(
    @{ Text=$goodProbe; Expected=$true },
    @{ Text=$goodProbe.Replace('probe=PASS','probe=FAIL'); Expected=$false },
    @{ Text=$goodProbe.Replace('connection=PASS','connection=FAIL'); Expected=$false },
    @{ Text=$goodProbe.Replace('result=PASS','result=SKIP'); Expected=$false },
    @{ Text=$goodProbe.Replace('OfficeLayout','CabinScene'); Expected=$false },
    @{ Text=$goodProbe + "`nCharacterAnimationProbe scene=OfficeLayout result=FAIL drift"; Expected=$false }
)) {
    Set-Content -LiteralPath $menuFixtureHost -Value $goodProbe -Encoding UTF8
    Set-Content -LiteralPath $menuFixtureClient -Value $case.Text -Encoding UTF8
    $probe = & (Join-Path $PSScriptRoot 'Check-MenuAnimationLogs.ps1') -LogPaths @($menuFixtureHost,$menuFixtureClient) -RequiredScenes @('OfficeLayout','MainMenu')
    if ($probe.Passed -ne $case.Expected) { throw 'Native menu / animation log regression.' }
}
Write-Host 'PASS: 6 native menu / animation log regressions.'

$goodParity = "StoryParity role=client scene=CabinScene result=PASS fields=15 hash=-44`nStoryParity role=client scene=CabinSceneDark result=PASS fields=30 hash=55`nSceneLookup requests=50 searches=10 reused=40"
foreach ($case in @(
    @{Text=$goodParity;Expected=$true},
    @{Text=$goodParity.Replace('result=PASS','result=FAIL');Expected=$false},
    @{Text=$goodParity.Replace('CabinSceneDark','WrongScene');Expected=$false},
    @{Text=$goodParity.Replace('fields=30','fields=0');Expected=$false},
    @{Text=$goodParity.Replace('reused=40','reused=0');Expected=$false},
    @{Text=$goodParity.Replace('hash=55','hash=999');Expected=$false}
)) {
    Set-Content -LiteralPath $menuFixtureHost -Value $goodParity.Replace('role=client','role=host') -Encoding UTF8
    Set-Content -LiteralPath $menuFixtureClient -Value $case.Text -Encoding UTF8
    $probe = & (Join-Path $PSScriptRoot 'Check-StoryParityLogs.ps1') -LogPaths @($menuFixtureHost,$menuFixtureClient) -RequiredScenes @('CabinScene','CabinSceneDark','OfficeLayout')
    if ($probe.Passed -ne $case.Expected) { throw 'Story parity / lookup log regression.' }
}
Write-Host 'PASS: 6 story parity / lookup log regressions.'

$appearanceHost = "Co-op session host: Connected -> Live`nCharacterMenu probe=PASS draft/discard/apply/reopen/close scene=MainMenu`nCharacterAppearance received role=host sid=1 revision=1 profile=2:4:2:2:2:4:3:3:1`nCharacterAppearance received role=host sid=1 revision=2 profile=2:4:0:2:0:6:3:3:2`nCharacterAppearance applied scene=OfficeLayout profile=2:4:0:2:0:6:3:3:2 vertices=4000"
$appearanceClient = "Co-op session client: Connected -> Live`nCharacterMenu probe=PASS draft/discard/apply/reopen/close scene=MainMenu`nCharacterAppearance received role=client sid=1 revision=1 profile=1:2:3:0:1:1:2:2:2`nCharacterAppearance received role=client sid=1 revision=2 profile=1:2:4:0:2:7:2:2:3`nCharacterAppearance applied scene=OfficeLayout profile=1:2:4:0:2:7:2:2:3 vertices=4000"
foreach ($case in @(
    @{Text=$appearanceClient;Expected=$true},
    @{Text=$appearanceClient.Replace('probe=PASS','probe=FAIL');Expected=$false},
    @{Text=$appearanceClient.Replace('revision=2','revision=0');Expected=$false},
    @{Text=$appearanceClient.Replace('1:2:4:0:2:7:2:2:3','0:1:1:1:0:0:1:0:0');Expected=$false},
    @{Text=$appearanceClient.Replace('OfficeLayout','WrongScene');Expected=$false},
    @{Text=$appearanceClient.Replace('vertices=4000','vertices=0');Expected=$false}
)) {
    Set-Content -LiteralPath $menuFixtureHost -Value $appearanceHost -Encoding UTF8
    Set-Content -LiteralPath $menuFixtureClient -Value $case.Text -Encoding UTF8
    $probe = & (Join-Path $PSScriptRoot 'Check-CharacterAppearanceLogs.ps1') -LogPaths @($menuFixtureHost,$menuFixtureClient) -RequiredScenes @('OfficeLayout','MainMenu') -RequireMenu
    if ($probe.Passed -ne $case.Expected) { throw 'Character appearance log regression.' }
}
Set-Content -LiteralPath $menuFixtureHost -Value $appearanceHost -Encoding UTF8
Set-Content -LiteralPath $menuFixtureClient -Value $appearanceClient -Encoding UTF8
$probe = & (Join-Path $PSScriptRoot 'Check-CharacterAppearanceLogs.ps1') -LogPaths @($menuFixtureHost,$menuFixtureClient) -RequiredScenes @('OfficeLayout','Typing Shooter Final scene') -RequireMenu
if ($probe.Passed) { throw 'Missing additive avatar coverage must fail.' }
Add-Content -LiteralPath $menuFixtureHost -Value $appearanceHost.Replace('OfficeLayout','Typing Shooter Final scene') -Encoding UTF8
Add-Content -LiteralPath $menuFixtureClient -Value $appearanceClient.Replace('OfficeLayout','Typing Shooter Final scene') -Encoding UTF8
$probe = & (Join-Path $PSScriptRoot 'Check-CharacterAppearanceLogs.ps1') -LogPaths @($menuFixtureHost,$menuFixtureClient) -RequiredScenes @('OfficeLayout','Typing Shooter Final scene') -RequireMenu
if (-not $probe.Passed) { throw 'Complete additive avatar coverage must pass.' }
Write-Host 'PASS: 8 character appearance log regressions.'

# Compile the actual wire, sequencing, limb solver and Steam stream without game assemblies.
& (Join-Path $PSScriptRoot 'Test-PlayerPoseRegression.ps1')
