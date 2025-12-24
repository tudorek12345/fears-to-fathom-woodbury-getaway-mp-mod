# Woodbury Spectator Sync - Status and Roadmap

## Current status

- MVP spectator mode is implemented (Host/Spectator, scene sync, camera sync, progress marker).
- Co-op is experimental and host-authoritative:
  - CoopHost and CoopClient modes added.
  - Scene sync for co-op client (auto loads host scene).
  - Client interactions relay to host via Iinteractable.Clicked.
  - Host replicates door state (CabinDoor + NOTLonely_Door.DoorScript), holdables (position/active), AI transforms (NavmeshPathAgent), and story flags (PlayerPrefs keys).
  - Remote avatar capsules show host/client positions (not full player prefab).
- Build target: net472 (Mono). BepInEx 5 Mono path is active. IL2CPP remains TODO.

## Limitations

- Spectator mode:
  - No co-op interaction or state sync (items, doors, AI, inventory).
  - No prevention of story triggers beyond minimal spectator lockdown.
  - One spectator only.
- Co-op mode:
  - Not a full second player prefab; client uses free camera and interaction ray.
  - Client gameplay systems are disabled locally to avoid story divergence.
  - Inventory ownership and held item handoff are not fully modeled.
  - Door and holdable sync is best-effort; physics state is not replicated.
  - AI sync is transform-only; behavior state is not replicated.
  - Focused on Cabin scene first; other scenes are not validated.

## Future features

- Spawn a real second player prefab per scene (Cabin/Pizzeria/RoadTrip).
- Networked input and animation state replication.
- Proper item ownership, pickup/throw authority, and hand attachment sync.
- Server-side gating for story triggers and cutscene synchronization.
- AI behavior state replication (not only transform).
- Multi-client support (more than one client).
- Smoother state sync (snapshots, interpolation, rollback).

## Next steps to take

1. Validate Cabin co-op flow end-to-end (doors, simple items, triggers).
2. Identify and map player prefab spawn points and camera setup for Cabin.
3. Implement client-held item ownership (host confirms, client mirrors).
4. Add host-side checks for interaction distance and line of sight.
5. Extend sync to additional scenes after Cabin stabilizes.

## TODO

- Add player prefab spawn + true second controller for Cabin.
- Sync held items (left/right hand) and throw events.
- Sync door audio and lock state consistently.
- Expand story flag coverage beyond PlayerPrefs (in-memory flags).
- Add a reconnect/resync full snapshot on client join.
- Add debug overlay: ping, last sync time, entity counts.

## Chat prompt log (user requests)

- Create a production-quality MVP spectator mod with TCP sync, camera follow, scene sync, and progress marker.
- Report mod state.
- Add build and lib wiring; build the project.
- Install BepInEx 5 (Mono), run the game once to generate config.
- Wire full co-op interaction and state sync (items, doors, AI, inventory, story flags) and prevent story triggers.
- Confirm Mono/IL2CPP, enable true co-op with both players interacting, and reverse-engineer game systems.
- Use a My Summer Car Online style approach (two players).
- Provide a recommendation and start with Cabin scene.
- Launch host and client builds; launch two instances at 720p.
- Diagnose client stuck on MainMenu/black screen and fix co-op scene sync/camera.
- Launch host and client instances again.
- Create this status/roadmap/readme document with limitations, future steps, prompt log, and TODO.
