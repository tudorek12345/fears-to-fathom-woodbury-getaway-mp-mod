# Woodbury Spectator Sync - Status and Roadmap

## Current status

- Primary focus is co-op. Cabin is the first scene being stabilized.
- Entry path: use the "Board game" episode in the menu to reach the Cabin flow.
- Co-op is host-authoritative with SceneReady handshake and full snapshot on connect.
- Client interactions can be routed to host (RouteInteractionsToHost).
- Sync covers player transforms, door states, holdables, basic AI transforms, and story flags.
- UDP handles high-frequency transforms; TCP carries scene and world state.
- Priority queue and UDP drain budgeting reduce transform starvation.
- Dialogue events are transmitted; UI mirroring is still incomplete.

## Limitations

- Not a full second player prefab yet; client uses local controller or freecam.
- Dialogue UI can flicker or appear briefly on the client.
- Item ownership, hand attachment, and physics replication are incomplete.
- AI sync is transform-only; behavior state is not replicated.
- Other scenes are not validated beyond Cabin.

## Future features

- Spawn a real second player prefab per scene (Cabin, Pizzeria, RoadTrip).
- Networked input and animation state replication.
- Proper item ownership, pickup/throw authority, and hand attachment sync.
- Server-side gating for story triggers and cutscene synchronization.
- AI behavior state replication (not only transform).
- Multi-client support (more than one client).

## Next steps to take

1. Validate the Cabin co-op flow end-to-end (doors, simple items, triggers).
2. Map player prefab spawn points and camera setup for Cabin.
3. Implement client-held item ownership (host confirms, client mirrors).
4. Add host-side checks for interaction distance and line of sight.
5. Extend sync to additional scenes after Cabin stabilizes.

## TODO

- Add player prefab spawn + true second controller for Cabin.
- Sync held items (left/right hand) and throw events.
- Sync door audio and lock state consistently.
- Expand story flag coverage beyond PlayerPrefs (in-memory flags).
- Add UDP stats (drop rate, last packet time) to overlay.
