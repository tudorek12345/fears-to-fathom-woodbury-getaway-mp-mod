# Woodbury Spectator Sync (MVP)

A BepInEx 5 (Mono) mod that adds a LAN spectator mode to "Fears to Fathom: Woodbury Getaway".

TODO (IL2CPP): If the game is IL2CPP, swap to BepInEx IL2CPP and update the project references and bootstrapper.

## Features (MVP)

- One host, one spectator client.
- Scene sync (spectator loads host scene).
- Camera sync (position/rotation/FOV) at configurable rate.
- Progress marker string sync (host sets via hotkey).

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

### Host

1. Set `Mode = Host` in the config.
2. Launch the game.
3. Press `F6` to start the host server.
4. Press `F9` to set the progress marker string.

### Spectator

1. Set `Mode = Spectator` in the config.
2. Set `SpectatorHostIP` to the host's LAN IP.
3. Launch the game.
4. Press `F7` to connect.

### Co-op (experimental)

This mode is host-authoritative and currently focused on the Cabin scene.

#### Co-op Host

1. Set `Mode = CoopHost` in the config.
2. Launch the game.
3. Press `F6` to start the co-op server.

#### Co-op Client

1. Set `Mode = CoopClient` in the config.
2. Set `SpectatorHostIP` to the host's LAN IP.
3. Launch the game.
4. Press `F7` to connect.

Notes:
- Client uses a free camera controller to avoid local story triggers.
- Interactions are sent to the host and applied there.

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

## Known limitations

- Spectator mode: no co-op interaction or state sync (items, doors, AI, inventory).
- Spectator mode: no prevention of story triggers beyond minimal spectator lockdown.
- One spectator only.
- Co-op mode is experimental and currently focused on Cabin scene only.
- Co-op uses host-authoritative state; client item ownership and inventory handoff are limited.

## Troubleshooting

- Firewall: allow the host's port (`HostPort`) on LAN.
- If you see a black screen on spectator, ensure the correct scene is loaded and the camera is found.
- If input feels locked on spectator, that is expected; spectator is camera-only.

## Test plan

- Host and spectator on same PC (`127.0.0.1`).
- Host and spectator on two PCs over LAN.
- Trigger a scene change and verify spectator loads the same scene.
- Disconnect/reconnect spectator (F7) while host is running.

## Additional methods to explore (future)

- UDP for camera stream (lower latency), keep TCP for scene/progress.
- Snapshot buffers + time-based interpolation for smoother motion.
- Game-specific camera binding to cutscene cameras instead of `Camera.main`.
- Real story flag sync instead of manual progress markers.
