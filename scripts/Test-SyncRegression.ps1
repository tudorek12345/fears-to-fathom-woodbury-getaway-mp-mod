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
    param([string]$Name, [string]$HostText, [string]$ClientText, [bool]$Expected)
    $caseRoot = Join-Path $fixtureRoot $Name
    $logs = Join-Path $caseRoot 'BepInEx/logs'
    New-Item -ItemType Directory -Path $logs -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $logs 'WoodburySpectatorSync_session_CoopHost.log') -Value $HostText
    Set-Content -LiteralPath (Join-Path $logs 'WoodburySpectatorSync_session_CoopClient.log') -Value $ClientText
    $result = & $checkScript -GameDir $caseRoot -RequireTraffic -RequireGameplay -NoFail 6>$null
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
$rejected = $false
try {
    & $checkScript -GameDir (Join-Path $fixtureRoot 'healthy') -Since ((Get-Date).AddMinutes(1)) -NoFail 6>$null | Out-Null
} catch {
    if ($_.Exception.Message -notlike 'No logs found*') { throw }
    $rejected = $true
}
if (-not $rejected) { throw 'Stale logs were selected after the requested cutoff.' }
Write-Host "PASS: $checks snapshot checks and 10 log-check regressions."
