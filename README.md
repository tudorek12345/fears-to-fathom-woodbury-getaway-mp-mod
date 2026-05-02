# Woodbury Co-op Sync

Minimal BepInEx 5 LAN co-op tooling for **Fears to Fathom: Woodbury Getaway**.

The mod is host-authoritative and focused on keeping two local/LAN game instances aligned: scene loads, player transforms, doors, holdables, dialogue signals, story flags, and visible remote avatars.

> Status: WIP. Cabin is the deepest tested flow; Pizzeria/RoadTrip have partial sync coverage.

## What Works

- Co-op host/client over LAN or same PC.
- SceneReady handshake with full state snapshot after scene sync.
- TCP for reliable world/story state, UDP for high-frequency transforms.
- Client interaction routing to the host.
- Door, holdable, dialogue, story flag, and basic AI/Mike transform sync.
- Remote player proxy using AssetBundle avatars or safe in-scene game-model fallback.
- In-game overlay with connection, scene, queue, story, and Mike sync diagnostics.

## Known Gaps

- Full story parity is still being expanded scene by scene.
- Dialogue UI mirroring is not complete.
- Item ownership, hand attachment, physics, and true second-player gameplay are incomplete.
- Quaternius AssetBundle avatars currently require a valid Animator; otherwise the mod falls back to safe game-model avatars.

## Requirements

- Fears to Fathom: Woodbury Getaway
- BepInEx 5 Mono
- .NET SDK
- Unity/game/BepInEx reference DLLs copied into `lib/`

Copy references from a local game install:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\CopyLibs.ps1 -GameDir "C:\Path\To\Fears.to.Fathom.Woodbury.Getaway"
```

## Build

```powershell
dotnet build .\src\WoodburySpectatorSync\WoodburySpectatorSync.csproj -c Release
```

Output:

```text
src\WoodburySpectatorSync\bin\Release\net472\WoodburySpectatorSync.dll
```

Install it to:

```text
<GameDir>\BepInEx\plugins\WoodburySpectatorSync.dll
```

Avatar bundles, when used, live here:

```text
<GameDir>\BepInEx\plugins\WoodburySpectatorSync\avatars\
```

## Launch Two Instances

From repo root:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Launch-CoopPair.ps1 `
  -GameDir "C:\Path\To\Fears.to.Fathom.Woodbury.Getaway" `
  -AutoStartHost `
  -AutoConnectClient `
  -ForceStopExisting
```

Local test path used during development:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Launch-CoopPair.ps1 `
  -GameDir "C:\Users\tudor\OneDrive\Plocha\Fears.to.Fathom.Woodbury.Getaway" `
  -AutoStartHost `
  -AutoConnectClient `
  -ForceStopExisting
```

The launcher writes separate host/client configs and starts two windowed game instances.

## Manual Co-op Flow

1. Host: set `Mode = CoopHost`.
2. Client: set `Mode = CoopClient` and `SpectatorHostIP` to the host IP.
3. Host enters the target scene or episode flow.
4. Host presses `F6` to start hosting.
5. Client stays at menu and presses `F7` to connect.

Hotkeys:

```text
F6  host on/off
F7  connect client
F8  toggle overlay
F9  progress/debug action
```

## Key Config

```text
Mode                         CoopHost | CoopClient | Host | Spectator
HostPort                     27055
UdpEnabled                   true
UdpPort                      27056
UseLocalPlayerController     true
RouteInteractionsToHost      true
RemotePlayerAvatarSource     Auto | GameModel | AssetBundle | Capsule
RemotePlayerAvatarId         woodbury_scene_auto | quaternius_regular_male | ...
RemotePlayerAvatarYOffset    0
ForceCabinStartSequence      true
CabinStartSequence           StartAfterShower
```

## Avatar Notes

Default behavior should prefer a safe in-scene game model when an AssetBundle avatar is invalid. The fallback clone disables scripts, colliders, cameras, audio, rigidbodies, and agents so it is visual-only.

AssetBundle path:

```text
BepInEx\plugins\WoodburySpectatorSync\avatars\woodbury_avatars.bundle
```

The current Quaternius path expects a Unity 2021.3-built bundle with an AnimatorController. If the bundle is render-only, runtime logs will reject it with `reason=no Animator`.

## Logs

Game logs:

```text
<GameDir>\BepInEx\logs\
```

Useful strings:

```text
Woodbury Spectator Sync
Co-op scene ready
Mike sync target
Remote player avatar
Cabin client runtime state held local
```

## Current Focus

- Finish Cabin story parity through board game, Ouija, eating, hiding, hiker, and endgame.
- Keep Mike visible, grounded, and smooth on the client.
- Improve dialogue and choice synchronization.
- Expand validated coverage for RoadTrip and Pizzeria.
- Replace fallback avatars with a reliable animated bundle once the Unity build path is stable.

See `CHANGELOG.md` and `README_STATUS.md` for detailed history and current debugging notes.
