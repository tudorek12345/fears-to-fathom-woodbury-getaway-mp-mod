# Woodbury Co-op Mod (WIP)

A BepInEx 5 (Mono) LAN co-op mod for "Fears to Fathom: Woodbury Getaway".
Primary focus is co-op; spectator mode remains but is secondary.
Current work targets the Cabin scene first. In the episode list, select "Board game" to reach the Cabin flow.


## Scope

- Co-op first, host-authoritative.
- Cabin scene is the first stabilized target.
- Spectator mode is supported but not the main focus.

## Current status (WIP)

- Co-op host/client connects and syncs on LAN.
- Scene handshake uses SceneReady; host resends SceneChange until client acks, then sends a full snapshot.
- Sync includes player transforms, door states, holdables, basic AI transforms, and story flags.
- Interaction routing is available: client clicks are sent to the host and applied there.
- Door interactions (including the cabin fridge door) now mirror on both host and client.
- Client no longer gets camera-locked during Mike dialogue; free movement is preserved.
- CabinHouse progression flags are replicated (WIP; dialogue/sequence triggers).
- CabinGameManager state flags (CurrentSequence/currentPlayerState/inConversation) are replicated (WIP).
- UDP is used for high-frequency transforms; TCP carries scene and world state.
- Transform backlog control and UDP drain budgeting are in place to reduce starvation.
- Dialogue events are transmitted, but full dialogue UI mirroring is not complete.

Known issues (latest build):
- Story progression beyond the initial Cabin/Board Game flow is still unstable; CabinHouse/CabinGame flags now sync but full sequence control is WIP.
- Dialogue UI can flicker or appear briefly on the client instead of staying in sync.
- Item ownership and physics replication are incomplete.
- AI behavior/animation sync is transform-only (no full state/brain sync).
- Other scenes are not yet validated beyond Cabin.

Latest session observations (Dec 26, 2025):
- HostUpdateAge stays near 0.0s while HostApplied count increases (transform apply confirmed).
- HostState enqueued/sent on host and read/applied on client are tracking correctly.

## Install (BepInEx Mono)

1. Install BepInEx 5 (Mono) into the game folder.
2. Build the project to produce `WoodburySpectatorSync.dll`.
3. Copy the DLL into `BepInEx/plugins`.
4. Launch the game once to generate config.

### Build notes

The project references Unity, the game assembly, and BepInEx from `lib/`:

- `lib/BepInEx.dll`
- `lib/Assembly-CSharp.dll`
- `lib/UnityEngine.dll`
- `lib/UnityEngine.CoreModule.dll`
- `lib/UnityEngine.InputLegacyModule.dll`
- `lib/UnityEngine.IMGUIModule.dll`
- `lib/UnityEngine.AudioModule.dll`
- `lib/UnityEngine.PhysicsModule.dll`
- `lib/UnityEngine.AnimationModule.dll`
- `lib/UnityEngine.UIModule.dll`
- `lib/UnityEngine.SceneManagementModule.dll`
- `lib/UnityEngine.UnityWebRequestModule.dll`

Some Unity versions do not ship module DLLs; in that case only `UnityEngine.dll` and `UnityEngine.CoreModule.dll` are required.
Copy these from your game/BepInEx install into `lib/` if needed.

### Quick setup (scripts)

1. Copy Unity and BepInEx assemblies into `lib/`:

```powershell
.\scripts\CopyLibs.ps1 -GameDir "C:\Path\To\Fears to Fathom Woodbury Getaway"
```

2. Build the DLL:

```powershell
.\scripts\Build.ps1
```

## Usage

### Co-op Host (primary focus)

1. Set `Mode = CoopHost` in the config.
2. Launch the game.
3. From the main menu, select the "Board game" episode (Woodbury Getaway) to reach the Cabin flow.
4. Wait until the host is fully inside the Cabin scene.
5. Press `F6` to start the co-op server.

### Co-op Client

1. Set `Mode = CoopClient` in the config.
2. Set `SpectatorHostIP` to the host's LAN IP.
3. Launch the game and stay on the main menu.
4. Press `F7` to connect and wait for scene load.
5. Do not select an episode on the client.

Notes:
- `RouteInteractionsToHost = true` routes clicks to the host (prevents local story triggers).
- `UseLocalPlayerController = true` uses the local first-person controller; set to `false` for freecam.
- UDP (if enabled) carries high-frequency transforms; TCP carries scene and world state.
- Host waits for SceneReady before sending the full co-op snapshot.
- Host and client must run the same mod build/version.

### Spectator (secondary)

1. Set `Mode = Spectator` in the config.
2. Set `SpectatorHostIP` to the host's LAN IP.
3. Launch the game.
4. Press `F7` to connect.

### Host + client on the same PC (no auto-connect)

Use separate config files so each instance keeps its own mode:

```powershell
$exe = "C:\Games\Fears to Fathom - Woodbury Getaway\Fears to Fathom - Woodbury Getaway.exe"
$wd = "C:\Games\Fears to Fathom - Woodbury Getaway"
$env:WSS_MODE = "CoopHost"
$env:WSS_CONFIG = "C:\Games\Fears to Fathom - Woodbury Getaway\BepInEx\config\com.woodbury.spectatorsync.host.cfg"
Start-Process -FilePath $exe -WorkingDirectory $wd
Start-Sleep -Seconds 2
$env:WSS_MODE = "CoopClient"
$env:WSS_CONFIG = "C:\Games\Fears to Fathom - Woodbury Getaway\BepInEx\config\com.woodbury.spectatorsync.client.cfg"
Start-Process -FilePath $exe -WorkingDirectory $wd
Remove-Item Env:WSS_MODE, Env:WSS_CONFIG -ErrorAction SilentlyContinue
```

Then:
- Host window: enter Cabin, press `F6`.
- Client window: stay on main menu, press `F7`.

## Config

- `Mode` = Host | Spectator | CoopHost | CoopClient
- `HostBindIP` = 0.0.0.0
- `HostPort` = 27055
- `SpectatorHostIP` = 127.0.0.1
- `SendHz` = 20
- `SmoothingPosition` = 0.15
- `SmoothingRotation` = 0.15
- `OverlayEnabled` = true
- `VerboseLogging` = false
- `UdpEnabled` = true
- `UdpPort` = 27056
- `TeleportDistance` = 25
- `TeleportCooldownSeconds` = 3
- `TeleportOnStaleSeconds` = 6
- `SnapToHostOnSceneLoad` = true
- `UseLocalPlayerController` = true
- `RouteInteractionsToHost` = true
- `AutoStartHost` = false
- `AutoConnectClient` = false
- `ForceCabinStartSequence` = true
- `CabinStartSequence` = StartAfterShower

## Networking notes

- Host state (scene, doors, dialogue, story flags) is prioritized over transforms.
- UDP has a per-frame drain budget to avoid starving TCP state.
- Old transform messages can be dropped to keep the stream fresh.

## Troubleshooting

- Client stuck on main menu:
  - Start the host server only after the host is inside the Cabin scene.
  - Client should stay on main menu and press `F7`, not pick an episode.
- Wrong scene on client:
  - Host must select "Board game" in the episode list before starting the server.
- UDP updates missing:
  - Allow `UdpPort` in firewall or set `UdpEnabled = false`.
- Overlay missing:
  - Press `F8` to toggle overlay.

## Test plan

- Co-op host and client on the same PC (`127.0.0.1`).
- Co-op host and client on two PCs over LAN.
- Scene handshake with SceneReady and full snapshot.
- Door interaction routed to host and mirrored on client.
- UDP on/off validation.

## Additional methods to explore (future)

- Full dialogue UI mirroring and drift correction.
- Real item ownership and hand attachment sync.
- Expanded scene coverage beyond Cabin (Pizzeria, RoadTrip).
- More robust AI behavior state sync.
