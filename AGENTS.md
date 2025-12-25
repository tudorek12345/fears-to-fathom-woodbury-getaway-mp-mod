# Woodbury Spectator Sync - Agent Guide

## Purpose
This project adds LAN co-op and spectator features to "Fears to Fathom: Woodbury Getaway" using BepInEx 5 (Mono).
Primary focus is co-op, with the Cabin scene stabilized first.

## Current focus
- Co-op host/client flow in the Cabin scene.
- Entry path: select "Board game" in the episode list to reach the Cabin flow.
- Host-authoritative sync with SceneReady handshake and full snapshot.

## Key constraints
- Target: BepInEx 5 Mono, net472.
- No Unity API calls from background threads.
- One host and one client for now.
- Keep files in ASCII unless already using Unicode.

## Repo map
- `src/WoodburySpectatorSync/Plugin.cs`: BepInEx entry point.
- `src/WoodburySpectatorSync/Config/Settings.cs`: config keys (Mode, UDP, co-op options).
- `src/WoodburySpectatorSync/Coop/CoopHostCoordinator.cs`: host logic, handshake, state send.
- `src/WoodburySpectatorSync/Coop/CoopClientCoordinator.cs`: client logic, scene load, state apply, local locks.
- `src/WoodburySpectatorSync/Coop/CoopClient.cs`: TCP/UDP IO, queues, UDP budget, transform drop.
- `src/WoodburySpectatorSync/Coop/CoopServer.cs`: server transport and UDP info.
- `src/WoodburySpectatorSync/Coop/CoopClientInteractor.cs`: client raycast -> InteractRequest.
- `src/WoodburySpectatorSync/Coop/NetPath.cs`: path mapping for objects.
- `src/WoodburySpectatorSync/Net/Protocol.cs`: message framing and serialization.
- `src/WoodburySpectatorSync/UI/Overlay.cs`: overlay UI and debug data.

## Co-op runtime flow
1. Host loads into Cabin (via "Board game").
2. Host starts server (F6).
3. Client stays on main menu, connects (F7), and auto-loads host scene.
4. Host resends SceneChange until client sends SceneReady.
5. Host sends full snapshot; client applies.

## Interaction routing
- `RouteInteractionsToHost = true` sends client clicks to host and disables local interactables.
- Keep colliders enabled so raycasts still hit.
- Use `UseLocalPlayerController = true` to move a real player locally.

## Networking model
- TCP: scene changes, doors, holdables, story flags, dialogue events.
- UDP: high-frequency transforms.
- Priority queue ensures state is processed before transforms.
- Transform backlog can be dropped to keep updates fresh.

## Debugging
- Overlay: check SceneReady/awaiting, ping, UDP lastRx, host state counters.
- Logs: `BepInEx/LogOutput.log` and `BepInEx/logs/`.
- Common issue: HostUpdateAge rising while transforms still flow. Use counters to confirm receive vs apply.

## Known issues
- Dialogue UI mirroring is incomplete; only event data is sent.
- Item ownership and physics replication are incomplete.
- AI sync is transform-only.
- Other scenes are unverified.

## Build
```powershell
.\scripts\Build.ps1
```
DLL output: `src/WoodburySpectatorSync/bin/Release/net472/WoodburySpectatorSync.dll`

## Do not change without review
- Threading model (Unity calls must stay on main thread).
- Message framing (length-prefixed TCP).
- Scene handshake logic (SceneChange + SceneReady).
