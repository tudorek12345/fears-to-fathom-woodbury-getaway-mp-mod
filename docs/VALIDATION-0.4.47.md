# 0.4.47 / protocol 6 validation — 2026-10-23

The local build expands scene coverage and repairs failures reproduced in the installed game. This report distinguishes scene/snapshot/rig tests from completing the story. No public release or Git push was performed.

## Automated checks

Release build: no warnings or errors. `scripts/Test-SyncRegression.ps1` passes **530 checks**: 18 snapshot assertions, 33 log fixtures, 4 Unity-error classification cases and 475 pose/protocol/presentation/limb/Steam-stream checks. The presentation codec is tested against every truncated payload length, invalid counts, versions, music state, progress and non-finite numbers. Log fixtures reject missing scenes, missing return visits, mismatched adapters, unresolved objects, absent rigs and disabled host presentation authority.

## Paired scene coverage

Two local game processes use the same DLL. The route waits for both peers to reach Live, then spends 25 seconds in each scheduled scene. It enters the typing game additively, injects a word/progress fixture, retains Office on return, and restores the pre-run PlayerPrefs after closing its own test processes.

| Build index | Scene | What is checked | TCP + UDP | TCP only |
| --- | --- | --- | --- | --- |
| 0 | `Disclaimer` | Both processes boot this auto-advancing scene; no gameplay handshake | Pass | Pass |
| 1 | `MainMenu` | Paired snapshot/Live and return from credits | Pass | Pass |
| 2 | `OfficeLayout` | Both visits, matching adapter, world snapshot, both full rigs | Pass | Pass |
| 3 | `ParkingLotScene` | Matching adapter, world snapshot and both full rigs | Pass | Pass |
| 4 | `RoadTripLoop` | Matching adapter, world snapshot and both full rigs; seated passenger poses | Pass | Pass |
| 5 | `Pizzeria` | Matching adapter, world snapshot and both full rigs | Pass | Pass |
| 6 | `CabinScene` | Matching adapter, world snapshot and both full rigs | Pass | Pass |
| 7 | `CabinSceneDark` | Matching adapter, world snapshot and both full rigs | Pass | Pass |
| 8 | `CreditsScene` | Host logic enabled, presentation snapshot/deltas and menu handoff | Pass | Pass |
| 9 | `Typing Shooter Final scene` | Host logic enabled, additive lifecycle, presentation and word fixture | Pass | Pass |

TCP-only archive: [`summary.json`](../build/all-scenes-release-tcp-20261023/20261023-223503-528/summary.json), with 7,969 applied host-state messages.

TCP + UDP archive: [`summary.json`](../build/all-scenes-release-udp-20261023/20261023-224139-270/summary.json), with 7,968 applied host-state messages.

Both final reports cover all ten inventory entries, all ten scheduled route stops and both Office visits, with no activity/inventory problems or unknown runtime exceptions. The game processes stopped after testing and backed-up PlayerPrefs values were imported. Session/Unity logs, launch configuration and registry backup are beside each report. Earlier 35-second sweeps under `build/all-scenes-pedestrians-*` also passed; final 25-second sweeps were rerun after aligning the loader and network hello version strings.

Final tested/installed/packaged DLL SHA-256:

`e631109b04c83ffbd634cb1e89854272ad26c993f5260fa0a3a1adecf3f52e6e`

## Repairs supported by this work

- Office and ParkingLot select native human rigs. RoadTrip selects the rendered human rig instead of an interaction object or the deer, resolves its moving passenger pivot without relying on the broken native singleton, and sends seated poses for both players.
- Native random pedestrian pools now mirror the host prefab/slot/count inventory before AI snapshots. The earlier TCP run stalled for 19.7 seconds on a missing Office pedestrian; the repaired run applies the additive snapshot on its first attempt, with all 19 AI entries and zero pending objects.
- Pizzeria folded pizza-box replicas keep the host clone name, resolving the previously missing transform paths.
- The typing game preserves Office across additive entry/exit. Client suppression is role-gated so the idle client coordinator cannot stop host logic. Client minigame buttons cannot independently unload it.
- Credits/minigame state uses an explicit bounded binary codec because Unity JSON on this installation omitted nested arrays. Changing transforms, activation, animation, text/color, cameras, audio, music selection, health and word progress are mirrored. Credits excludes its static cabin meshes from the packet.
- Native crouch callbacks are guarded before dereferencing a missing player; missing references are repaired where a local controller exists. Audio fade coroutines stop when their scene-owned source is destroyed. These are runtime patches through BepInEx Harmony; decompiled/game files were not edited.

## Evidence limits

Story and AI diagnostic totals are not directly comparable: the host counts emitted messages by phase, while the client coalesces keys and classifies typed vehicle/events differently. The checker requires non-empty story/AI state, successful snapshot application and zero pending/missing objects; count comparisons cover the one-entry-per-key door, holdable and NPC categories used by the runtime completion gate. The earlier `all-scenes-verified-udp-20261023` archive preserves the report from a stricter, invalid raw-counter comparison as `summary-before-recheck-*.json`. Final pedestrian-repair reports pass the corrected checker directly.

The game installation still fails `SteamAPI_ManualDispatch_Init` in its own native Steam DLL. Credits subsequently reaches `Steamworks.Data.Achievement.Trigger` with partially initialized Steam state. These exact faults remain listed as installation errors. The classifier only treats that achievement exception as related when the same log contains the native initialization failure; unrelated exceptions fail the suite. No Steam library was replaced.

These headless runs do not visually verify gait, crouching, item grips, fingers, camera framing or audio playback. They do not complete every story branch, death/ending path, reconnect, persistent save/resume or the typing game. The typing game remains controlled by the host; the second player receives its presentation. Dynamic death effects and full cinematic parity need gameplay validation. The installed optional avatar bundle is static and rejected; tested gameplay avatars use native scene rigs.

Spacewar remains included. Its native initialization/lobby/relay probe and helper tests are documented in [0.4.46 validation](VALIDATION-0.4.46.md); two-account Internet gameplay remains unverified.
