# 0.4.46 / protocol 6 validation — 2026-10-23

Implemented in the existing working tree, preserving the prior Cabin renderer and casserole fixes. This is a development test build, not a claim of full story or visual animation parity.

## Automated checks

- Release plugin build: no warnings or errors, using the installed game's local assemblies.
- `scripts/Test-SyncRegression.ps1`: 18 snapshot assertions, 14 log fixtures, and 194 pose/protocol/motion/limb/Steam-stream checks.
- Native helper: `SpacewarCoreTests` and `SpacewarOutputIsolation` passed.
- Managed regressions cover pose/Pong round trips, every truncated pose length, protocol 5 rejection, invalid floats/rotations/reach/flags, stale and duplicate sequences, rollover, scene/session mismatch, 1000 FPS motion sampling, teleport/gap reset, limb length preservation, partial stream reads, bounded frames, Steam chunk reassembly, wrong senders, a third lobby member, and peer departure.
- Steam stream tests use a fake helper. They do not simulate Valve's Internet routing or a second Steam account.

## Local game evidence

Game found at `C:\Users\tudor\OneDrive\Plocha\Fears.to.Fathom.Woodbury.Getaway\Fears to Fathom - Woodbury Getaway.exe`. Existing September 13 logs and decompiled `FirstPersonController` / `PlayerController` sources were inspected. MWCMP's pose selection, limb solver and isolated native helper supplied implementation references.

Headless local host/client runs entered `CabinScene`, session 1, generation 5:

| Check | Evidence |
| --- | --- |
| TCP only | `build/runtime-pose-20261023-final/summary.json`: passed; 5,766 applied host-state messages observed |
| TCP + UDP, final runtime code | `build/runtime-pose-20261023-udp/summary.json`: passed; 2,207 applied host-state messages observed |
| Player poses | Both roles logged accepted poses in the same Live gameplay session, with head, torso, left arm and right arm mapped |
| Scene transition repair | Initial run reproduced `RemoteAvatar.Root` throwing during voice anchor lookup after Unity destroyed the old scene object; later runs did not reproduce that managed exception |
| Duplicate counter repair | Initial run exposed per-frame fallback application logs for duplicate poses; fixed runs no longer produce those false application records |

Both checks used `-RequireTraffic -RequireGameplay -RequirePlayerPoses`. Each run directory contains its launch configuration, session logs and Unity logs. Test game processes were stopped. The original plugin DLL and exported PlayerPrefs are backed up under `build/runtime-pose-20261023`; the exported preferences were imported after testing.

Final runtime-tested plugin SHA-256:

`0d1262be1fdd802d8f1d938e5c1771a63b7a723625bd13729e1131af32716fdb`

## Native Spacewar probe

The first probe failed clearly because Steam was stopped. After starting the installed Steam client, `SyncRegression.exe --probe` successfully initialized App ID 480, created a Woodbury lobby, authenticated the local Steam identity and reported relay availability/configuration as ready (`100`). The probe then closed its helper and lobby normally. No second Steam account was available for joining or exchanging game packets across the Internet.

## Remaining limits

- These runs verify scene entry, snapshot application, pose traffic and rig binding. They do not visually verify walking, crouching, jumping, seated transitions, item-specific grips, finger animation, or all avatar models. The game has no universal full-body local animation stream; missing locomotion controls use procedural bone poses, and held-item anchors drive arm IK.
- Headless tests did not play the full story, Board Game sequence, death/ending flow, persistent save/resume, or reconnect recovery. Spacewar currently carries all traffic reliably; congested links can delay poses behind snapshot traffic. Restarting a host session can create a new lobby ID.
- The installed game's own 14,336-byte `..._Data/Plugins/x86_64/steam_api64.dll` still reports missing `SteamAPI_ManualDispatch_Init` (and historical shutdown) entry points. That is separate from the mod's isolated, successfully initialized helper. The mod did not replace that game library; the appropriate source for repairing it is the game's official Steam installation. This native game error prevents calling the entire installation error-free.
- No Git commit, push or public site/release deployment was performed.
