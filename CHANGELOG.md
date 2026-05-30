# Changelog

## Unreleased

> Plugin 0.3.0 → 0.4.8 · wire protocol 3 → 4.
> Old < 0.3 / proto < 2 clients/hosts are rejected cleanly via Hello/HelloAck.

### Co-op Cabin

- NPC brains: host-authoritative `NpcBrainState` sync for Mike variants (MikeCabin, MikeFishing, MikePostEating, MikeAfterHiding, MikeCabinCookController), hiker/window (CabinHiker + HikerCabinController + HostFixingSink), Nora, and cat — registry-based actor IDs, client-side local brain suppression, snapshot buffering, stale-sequence drops, throttled diagnostics.
- Hiker window: state enums + go/moving/reachedPos/followingHost flags + sinisterAudioTrigger/closetLight/hikerConvoTrigger active states. Lock window gated on `CurrentSequence` (HikerSequence / HostAtDoor / HostHittingDoor) instead of `gameObject.activeSelf` to stop client clumping.
- Mini-games (Jenga + Ouija): full controller state, piece/drag mini-game state, audio, initial position, planchette/table transform correction, dining-table colliders as stable mask, basement/Ouija active objects, spectator camera state. Client local mini-game brains suppressed. Compact overlay line. Mike's held Ouija table/tablet setup mirrored. False `cabinGame=5` pending/missing fixed by removing invalid Jenga collider keys, accepting diagnostic Jenga piece counts, accepting inactive mini-game level visuals, logging unresolved Cabin game keys with values/ages.
- Cooking/eating: casserole/oven prop sync via story flags + transform correction, Mike cook/eating visuals, plate masks, eating rig weights, living-room TV clip identity + playback time. Pre-Eating leak visuals suppressed on clients.
- Ambient: light switches (basement/closet/outside), bedroom TV playback, flashlight light, sink/dishwashing visuals, seated-eating player/plate visuals.
- Opening RV/truck driving intro: RV state, lights, brake/turn values, bobblehead speed, radio (clip + playback time), bag/door/light actives, truck visual transforms.
- Randomized traffic pool synced + AiTransform correction so pools no longer diverge silently.
- Seated proxies: remote player transform sending follows active seated bodies (couch, Ouija table, board-game seats, bed) instead of dropping with the disabled first-person controller.
- Post-eating/hiding: MikePostEating, shed/understairs hiding flags, key active triggers, toolshed crash breadcrumbs. Mike target selection prefers `Mike Post Eating` during hiding while game still reports broad `Eating` sequence.
- Mike animation event replication (`CabinGM.MikeAnim.StateHash/Loop/Phase10/Transition/NextStateHash`).

### Co-op Pizzeria

- `MikeDrivingInPizzeriaScene` opening drive: vehicle/path state, engine/key/handbrake audio, headlights/panel/bobblehead, transform correction, client-side driving brain suppression.
- Manager + story progression: current player mode, first-conversation timers, pizza/burp gates, phone UI, text-reply unlocks, soda-can seating, doors, light triggers, phone canvas/network, truck triggers, keys UI + one-shot audio.
- Camera/control: driving freeze, sitting/global FOV zoom, dialogue camera, zoom transitions, main-camera FOV, look-target diagnostics, trash/truck layer switching, editor-start music. Player seat/drive/camera prop state, pizza-on-table visibility, boundary collider masks, out-of-play-zone trigger state.
- Props / media: pizza box/slice/lid + folding-worker prop (with local folding brain suppression), TV/radio media (advert/news clip selection + playback time + radio track), DontDestroyRoadTripMusic fade/playback handoff from RoadTrip.
- Specialised interactables: PizzeriaTVAudioProximity (range + inside-pizzeria volume), PizzeriaChair (interactability/layer through pickup/sitting/eating/burp/get-up), generic triggers (`OnTriggerSub` / `OnTriggerDisplaySub` / `OnTrigger` / `TriggerEventInvoker`), traffic trigger active/enabled + pool state.

### Co-op RoadTrip

- Camera + manager timers: dialogue camera activation, car camera rotation/freeze, FOV flags, auto-conversation timers, phone state, road-bump timers, text-reply unlocks. Camera look-target sync for Mike/bus/deer cinematic focus.
- Vehicles + bobblehead: typed `PathVehicleState` smoothing for MikeInCar + MikeTruckInLoopScene/GoingToNora/InParking. Bobblehead active/layer-switched + animator state/speed + transform correction. Car radio playback, school bus active, horror audio, RoadTripTruck brake/deer-run + truck/deer audio + deer animation + deer/school-bus transform correction. Mike-in-car hidden conversation flags.
- Triggers + traffic: generic triggers (`OnTriggerSub` / `OnTriggerDisplaySub` / `OnTrigger` / `TriggerEventInvoker`), traffic pool/active-state + AiTransform correction + one-shot/speed/spline trigger settings.

### Co-op Office

- Scene + player state: OfficeLayoutGameManager scene/player mode, first-person enablement, end-scene yellow intro/audio, coffee-sip hand animation, phone/restroom triggers, throw permission, camera FOV.
- Yellow intro + worker monitors: `YellowIntroManager` timer/index/text-visibility mask with local intro suppression. `OfficeWorkerMonitor` video sync (clip identity + playback time + material/renderer) with monitor brain suppression.
- Subcontrollers + appliances: TableManager / ComputerManager / CoffeeMachineManager / OfficePhone / TypeMasterManager / TypingShooterComputerManager. OfficeRadio (track/time/button). OfficeFridge + OfficeMicrowave (door/light/audio). OfficeToiletManager (stall/pee/lid/seat). OfficeJanitor (jumpscare/cleaning). OfficeWorker conversation visual + audio. OfficeLayoutUIManager + peeing UI. Transform correction for actors + movable appliance/toilet parts.
- Boundaries + triggers + vending: `OnTriggerOfficeBoundsSub` reminder triggers, generic trigger suite, shared `VendingMachineManager` (cameras, triggers, slot/sliding colliders, soda-can state, audio, player-camera FOV) with client-side vending brain suppression.

### Co-op ParkingLot

- Game/player state: ParkingLotGameManager call timing, intro state, phone ringing, talking/hugging mode, throw permission, first-person, camera FOV. UI/intro/phone flags. Stranger / Mike / suitcase / truck / car subcontroller state with actor + vehicle transform correction.
- Walking cop + elevator: `WalkingCopController` NavMesh brain suppression + animator/target mirroring + transform correction. `ParkingLotElevatorManager` travel/door/audio/anti-throw state + elevator door transforms + truck tailgate transform correction. Anti-throw zones.
- Generic triggers (`OnTriggerSub` / `OnTriggerDisplaySub` / `OnTrigger` / `TriggerEventInvoker`) suppressing client-local trigger brains while mirroring active/entered/triggered/one-shot state.

### Co-op avatars

- Source / fallback path: Auto → GameModel → AssetBundle → Capsule. Auto prefers safe non-Mike scene humans; Capsule explicit-only; Auto/GameModel stays invisible rather than rendering procedural egg unless capsule explicitly configured. Render-only AssetBundle avatars with no AnimatorController rejected; wrapped avatars grounded to renderer bounds. Cabin House/Host sequence objects rejected from Auto/GameModel fallback.
- Nametags + display names: in-world HOST/CLIENT nametags for remote proxies; fallback role labels deduplicated; themed nametags hidden until a visible gameplay avatar is present. Host/client display names exchanged through Hello/HelloAck; launcher-selected names supported.
- Animation + grounding: scene-model animators frozen when their controller can't be driven by known movement params. Seated proxy raised positions preserved instead of forcing couch-sitting bodies to floor. Remote player animation driven from replicated transform motion. Buffered client-side AI interpolation for smoother Mike/NPC motion. Low-rate NPC brain corrections no longer fight Mike's high-frequency smoothing.
- AssetBundle tooling: manifest IDs, bundle/id/scale/y-offset config, exact fallback logging, launcher `-RemotePlayerAvatarId` support, Unity 2021.3 avatar bundle project + `Build-AvatarBundle.ps1` + humanoid rig/animation import + basic locomotion AnimatorController + render-only Quaternius bundle install.

### Co-op lifecycle / protocol

- Explicit `SessionState` machine + `Hello`/`HelloAck` negotiation + session/generation-aware `SceneChange`/`SceneReady` + `SnapshotBegin`/`SnapshotEnd`/`SnapshotAck`. UDP apply gated until `Live`. Bounded `SceneChange` retry + structured pre-Live drop logs.
- Scene-readiness probes for RoadTripLoop, CabinSceneDark, OfficeLayout, ParkingLotScene wait for the real scene managers instead of treating them as managerless. Client `SceneReady` waits up to 15s for scene-specific managers (CabinGameManager, PizzeriaGameManager, RoadTripGameManager) before falling back to `ReadyPartial`. Snapshot state buffered/applied before `Live`.
- Host snapshot emission bracketed; normal world/story/door/holdable/AI deltas gated until `SnapshotAck` moves to `Live`; blind 5-second full-state spam removed in favor of emergency/manual resync paths.
- Wire protocol bumped to 4 with typed `SceneActionIntent`, `UiMirrorState`, `CameraRigState`, `PathVehicleState`, `SceneEventState`. Plugin compatibility 0.3.0 → 0.4.8.

### Co-op UI

- F11 main-menu co-op setup panel: mode selection, host/client connect, LAN endpoint settings, display name, avatar source/id tuning, overlay toggle, SceneDiscoveryDump toggle — no longer need to edit `BepInEx/config/com.woodbury.spectatorsync.cfg` by hand.
- Overlay: `Session: <state> sid=<id> gen=<n>` line plus snapshot ack/retry or pending/missing counts. Compacted into styled two-column-sized panel; sync labels shortened. Always-on bottom-center `F2F WOODBURY CO:OP` brand mark. Expanded host/client connect logs (bind endpoint, session id, remote endpoint, retry reason, disconnect events).
- Dialogue: remote dialogue routed through the game's native subtitle UI when available; client UI conversation suppressed; drift detection retained. Dialogue camera locks forcibly released during Mike conversations.
- Host UI mirroring: Pizzeria + RoadTrip + Office UI managers (intro/fade canvases, dialogue cameras, phone pause/allow/canvas state, RoadTrip transition music, Office peeing UI).

### Tooling / diagnostics

- `scripts/Compare-SceneDiscoveryDump.ps1` diffs host/client SceneDiscoveryDump logs by scene/component/field and produces parseable `SceneDumpDiff` lines for identifying remaining unsynced state. F10 manual `SceneDiscoveryDump` hotkey gated by `[Debug] SceneDiscoveryDump`.
- `Run-CoopSmoke.ps1` end-to-end smoke flow + `Launch-CoopPair.ps1` per-instance Unity log files + instance-cap safety + default windowed launch args. Default to manual co-op startup unless `-AutoStartHost` / `-AutoConnectClient`.
- Session log filenames include mode + ms timestamp + pid so same-PC host/client runs don't collide. Throttled overlay/status logging. Mike-sync-target log throttled on `Transform.GetInstanceID()`. Client "runtime state held local" log bucketed per-reason. Shutdown retry noise quieted after intentional disconnects.
- Avatar diagnostics: source, fallback reason, renderer count, bounds, animator count, enabled-collider count in BepInEx + session logs.
- Restored missing `sharedassets5.assets.resS` / `sharedassets6.assets.resS` from cleanup backup after Unity logs showed missing streamed texture data.

## 0.2.30
- Co-op: enqueue TCP host transforms for main-thread apply and update applied latch timestamps from ApplyHostTransform.

## 0.2.29
- Co-op: apply host transforms when latch sequence advances and log apply failures (rate-limited).

## 0.2.28
- Co-op: add host-state counters (enqueued/sent/read/applied) and host-transform latch stats to overlay/logs for debugging.

## 0.2.27
- Co-op: apply host transforms when the latest TCP/UDP receive timestamp advances (fallback when latch consumption stalls).

## 0.2.26
- Co-op: consume latest host transform by sequence ID to avoid missing updates due to counter/seq race.

## 0.2.25
- Co-op: apply host transforms when network transform counters advance to avoid missed updates.

## 0.2.24
- Co-op: apply host transforms based on latest network timestamps, not only queued messages.

## 0.2.23
- Co-op: host transforms are now latched in the client receive loop and consumed on the main thread to avoid missed updates.

## 0.2.22
- Co-op client: accept host transforms regardless of player id and track last host id for debugging.
- Co-op overlay/logging: show per-channel transform receive stats (TCP/UDP) and last source.
- Co-op dialogue overlay: enforce a minimum display time and ignore premature clear messages.

## 0.2.21
- Co-op: host transform sending now runs in the server loop using the cached transform (TCP/UDP heartbeat at SendHz) to avoid stalls.

## 0.2.20
- Added env var overrides for config/mode/ports (WSS_CONFIG, WSS_MODE, WSS_UDP, WSS_HOST_IP, WSS_HOST_PORT, WSS_UDP_PORT).

## 0.2.19
- Co-op: if host can't sample a fresh transform on Ping, reply with cached transform.

## 0.2.18
- Co-op: stop using `TcpClient.Connected` for connection gating to avoid false disconnects.

## 0.2.17
- Co-op: handle Ping on host thread and reply with Pong carrying a fresh host transform.
- Force `Application.runInBackground` to keep host/client ticking when unfocused.

## 0.2.16
- Fix TCP framing reads for co-op/spectator streams and centralize the frame reader.
- Prioritize TCP messages over UDP to avoid starving reliable state updates.

## 0.2.15
- Co-op: piggyback host transform on Pong replies as a reliable fallback for host updates.

## 0.2.14
- Co-op AI sync: replicate NavMeshAgent transforms and disable local NavMeshAgent on clients.
- Client: spawn a full host proxy (not just a capsule) when possible.
- Added UnityEngine.AIModule reference for NavMeshAgent use.

## 0.2.13
- Client: suppress trigger scripts/colliders and interactables to reduce local story drift.
- Dialogue: added start/advance/choice/end events with drift detection and overlay instrumentation.
- Co-op host: keep sending transforms before SceneReady; freecam uses unscaled delta time.

## 0.2.12
- Added heartbeat logging for host/client send/receive timing in session logs.
- Throttled overlay snapshot logging to reduce log spam.

## 0.2.11
- Added per-session log file and overlay/status logging for easier debugging.

## 0.2.10
- Co-op: send TCP fallback player transforms and use best available camera during cutscenes.

## 0.2.9
- Co-op: mirror host dialogue/subtext lines to client and suppress local dialogue UI.

## 0.2.8
- Co-op client: force FROM_MENU when host provides START_SEQ to prevent driving intro.

## 0.2.7
- Co-op client: set Cabin start sequence prefs to skip driving intro reliably.

## 0.2.6
- Co-op client: force skip cabin intro if stuck in driving sequence and snap to host.

## 0.2.5
- Co-op scene sync: include build index and handle null async loads with fallback.

## 0.2.4
- Co-op client: defer gameplay lock while loading scenes and add load-timeout fallback.
- Co-op overlay: show scene load progress percent.

## 0.2.3
- Added optional co-op auto-start host and auto-connect client config entries.

## 0.2.2
- Co-op scene handshake (SceneReady + resend) and full snapshot after ready.
- Added co-op debug overlay lines (ping/UDP/scene sync/queues).

## 0.2.1
- Added UDP channel for high-frequency camera/transform updates with TCP fallback.

## 0.2.0
- Experimental co-op scaffolding: host/client modes, interaction relay, and world state sync.

## 0.1.0
- Initial MVP: host/spectator, scene sync, camera sync, progress markers.
