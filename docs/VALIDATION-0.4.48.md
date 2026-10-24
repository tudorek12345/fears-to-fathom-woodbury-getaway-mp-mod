# 0.4.48 validation

Plugin 0.4.48, protocol 6. Final DLL SHA256: `ceb5d2004eef613f63378acd3e2e325713545eb76552c07f3ce5e558a92d7cf2`.

## Implemented

- Native `Multiplayer` text above Episodes, using the game's font, with a Canvas tab for LAN/Spacewar, Host/Join, player name, IP/port/lobby ID, Disconnect, Copy lobby ID, Back/Escape and native Start story. F11 opens the tab in MainMenu and the advanced panel in gameplay. F8 enables main-menu diagnostics explicitly; the waiting overlay stays out of the menu.
- Neutral rig calibration and directional idle/walk/run/backward/strafe/crouch/airborne/seated animation, including arms, elbows and ankles. Native NPC Animators are disabled on procedural player clones. Head/torso aim and held-item hand IK/wrist orientation remain driven by the validated pose packets.
- Grounded foot correction, stale-pose relaxation, scene/session/teleport blend resets and movement-backed airborne detection. Inactive cinematic controllers no longer produce a false jumping pose. Janitor broom/mop props are hidden on player clones.
- Native menu action probes and skinned-bone animation probes run for each peer; the normal scene/snapshot/full-rig checks remain required. Unknown managed exceptions, native crash markers and out-of-memory reports fail validation.
- Thirty explicit Cabin arrival, toilet, intro and late-story fields now mirror through reliable story messages. The client checks a digest of actual native field values. House entry/toilet completion differences were observed in the earlier local UDP logs; local player movement and dialogue-camera fields are deliberately independent.
- Office/Parking/RoadTrip/Pizzeria adapters share object lookups within one coordinator update. Scopes clear after the update, including on exceptions; object inventory changes invalidate the cache. This reduces repeated Unity searches without retaining stale results between frames.

## Automated checks

`build/regression-048-parity.log` records **688 passing checks**: 18 snapshot assertions, 33 log fixtures, 6 Unity-error classifications, 6 native-menu/animation log fixtures, 6 story-parity/lookup log fixtures and 619 managed checks. Managed tests include the actual production client/server and Spacewar stream, real loopback TCP replacement sessions, delayed/missing Steam packets, helper errors, wire serialization, scalar validation and lookup scope invalidation. Release compilation has zero warnings/errors.

The package build reran all 688 checks successfully (`build/package-048-final.log`). The final archive is `site/downloads/woodbury-coop-latest.7z` (8,286,412 bytes), SHA256 `daa8fc50172552eae649301a69f75c4098f37c48ddb4c23d1a3cc317eda5f961`. Independent extraction verified all 30 listed file checksums, both runtime report entries and agreement between built, installed and packaged DLL hashes (`build/package-048-verification.json`). The real native helper matches its build output; no game reference assemblies are packaged. Local status/site metadata was refreshed from the manifest. Nothing was committed, pushed or publicly deployed.

Both native helper suites pass (`SpacewarCoreTests` and `SpacewarOutputIsolation`). The real signed-in Steam probe initialized App ID 480, created a lobby and reported relay readiness; output is in `build/spacewar-048-native-probe.log`.

## Tester Spacewar join report

The tester reported joining then being removed after roughly one second, in either hosting direction, with no visible error on the previously supplied build. The user confirmed Spacewar. Neither tester's actual logs have been provided, so that specific cause is not confirmed or claimed fixed by a same-PC LAN test.

Confirmed code repairs: lobby membership no longer reports Connected until peer packets arrive; a game Ping initiates route negotiation. Helper/protocol failures remain visible after cleanup. Expired Steam lobbies are not silently retried or offered for copying. The menu selects the error for the current Host/Join role. Host cleanup is tied to its original stream so a late worker cannot kick the replacement peer; 12 repeated TCP replacement cases exercise this guard. The client waits for a previous connection worker to finish before starting another.

[Valve's Messages API documentation](https://partner.steamgames.com/doc/api/ISteamnetworkingMessages?language=english) distinguishes implicit asynchronous session establishment from successfully exchanged messages and recommends a peer reply to detect success. The transport probe follows that distinction. It does not establish that both testers can reach Steam's network.

The complete package includes `Collect-CoopDiagnostics.ps1` for both PCs. It copies recent BepInEx/Unity logs with capture times and installed plugin/helper hashes into a local ZIP; it does not upload logs. A real two-account retest remains necessary.

## Final scene sweeps

Both fresh headless sweeps pass on the final DLL above. Each starts through the native Host/Join actions and checks all ten build scenes, including Disclaimer bootstrap, both Office visits, the additive typing game, six gameplay rigs, RoadTrip seating, credits and menu return. Both require actual skinned-bone animation probes and matching native Cabin story digests. No complete story playthrough is claimed.

| Transport | Report | Result | Applied host-state counter | Mod runtime errors | Installation error categories |
| --- | --- | --- | ---: | ---: | ---: |
| LAN TCP only | `build/release-048-tcp/20261024-003045-061/summary.json` | PASS | 7,836 | 0 | 2 |
| LAN TCP + UDP | `build/release-048-udp/20261024-003719-823/summary.json` | PASS | 7,902 | 0 | 2 |

Both reports have no menu/animation, story-digest, inventory or activity problems, and zero nonzero-age pending-retry warnings. CabinScene matches 15 fields on available roots; CabinSceneDark matches all 30. The two installation error categories are documented below and remain in each report and package manifest.

Final client lookup counters: TCP used 629 scene searches for 8,947 requests (8,318 reused); UDP used 628 searches for 8,947 requests (8,319 reused). These measure avoided repeated searches, not an FPS improvement or total frame-time benchmark.

Completed local logs and installed hashes were collected at **2026-10-24 00:43:51 Europe/Prague** into `build/diagnostics-048-final/20261024-004351-243.zip`. The ZIP passes its integrity check and contains both final UDP session logs, earlier recent logs and the installed final DLL hash. It is not a capture from either tester's remote PC.

The focused Cabin/CabinDark/menu route passes at `build/parity-048-focused/20261024-001410-953/summary.json`, with 15 available story fields matching in CabinScene and all 30 in CabinSceneDark. That focused DLL preceded the previous-worker join guard, unconditional schema-binding validation and final menu status selection; final full sweeps use the hash above.

## Rendered review

The focused Office/MainMenu graphics run passed:
`build/menu-animation-final-visual/20261023-233552-250/summary.json`.
Its DLL was `40994849c88f11adadd3bbfa8ea6c4d5a922d52e4d632c4e0ab2341cd787975a`. The menu layout and animation curves are unchanged since these captures. Later changes reset diagnostic capture flags, measure joint rotation in the probe, preserve connection failure text, add story fields and cache scene searches. The final scene sweeps above use the final packaged DLL hash.

Reviewed game-rendered UI and actual avatar frames:

- `build/menu-animation-final-visual/20261023-233552-250/images/9596-menu.png`: matching Multiplayer text, separated above Episodes; the Options overlap from the user screenshot is gone.
- `build/menu-animation-final-visual/20261023-233552-250/images/9596-multiplayer.png`: native Canvas connection tab.
- `build/menu-animation-final-visual/20261023-233552-250/images/OfficeLayout-9596-0.png`: walking pose.
- `build/menu-animation-final-visual/20261023-233552-250/images/OfficeLayout-9596-4.png`: crouching pose.
- `build/menu-animation-final-visual/20261023-233552-250/images/OfficeLayout-9596-5.png`: seated pose.

The menu capture renders the UI through a diagnostic camera; its black background is not a change to the game's menu background. Avatar captures use an isolated light/camera and wait until LateUpdate applies the pose. These samples verify layout and visible deformation; they are not a full visual quality review of every scene or a two-person story session.

## Preserved failures and limits

The first 0.4.48 TCP route passed all scene/snapshot checks but failed its Pizzeria animation probe because that probe required foot translation while the new grounded crouch kept the feet planted. The original failed report is preserved at `build/all-scenes-048-tcp/20261023-233906-195/summary.json`. The revised probe verifies thigh/arm rotation, skinned-bone bindings, head/hand posing and transform restoration; final sweeps are fresh runs.

A graphics-enabled full sweep stopped at Pizzeria when the client process ran out of memory. The raw log reports `Could not allocate memory: System out of memory!` and an FMODSample allocation of 16,662,704 bytes before the Unity native crash. That run is failed, not counted as all-scene proof:
`build/all-scenes-animation-udp/20261023-232704-428/summary.json`.
The original crash dump remains under the game's Unity crash directory, `Crash_2026-10-23_213105063`. Final full sweeps use headless processes; the focused graphics run above passes.

The installed game's existing missing `SteamAPI_ManualDispatch_Init` entry point, and the credits achievement failure following that failed initialization, remain separately recorded environment errors. No game Steam DLL was replaced. The isolated mod Spacewar helper remains packaged; no two-account Internet gameplay claim is made.

Full story progression, all ending branches, save/resume parity, reconnect recovery, all-scene visual calibration, facial/finger animation and two-account Spacewar gameplay remain unverified or outside this animation pass. Native scene-entry/snapshot/rig coverage is not a completion percentage.
