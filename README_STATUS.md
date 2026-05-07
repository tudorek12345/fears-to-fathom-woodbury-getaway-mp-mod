# Woodbury Spectator Sync - Status and Roadmap

## Current status

- Primary focus is co-op scene-wide sync across RoadTrip, Pizzeria, and Cabin.
- Entry path: use the normal flow or launcher pair; Cabin "Board game" remains the deepest validated flow.
- Co-op is host-authoritative with SceneReady handshake and full snapshot on connect.
- Client interactions can be routed to host (RouteInteractionsToHost).
- Sync covers player transforms, door states, holdables, basic AI transforms, story flags, Cabin sequence flags, Pizzeria Mike/player flags, and RoadTrip Mike/truck flags.
- Visible remote players default to safe in-scene game-model clones (`woodbury_scene_auto`), can force CC0 AssetBundle avatars, and fall back to a compact non-colliding capsule.
- UDP handles high-frequency transforms; TCP carries scene and world state.
- Priority queue and UDP drain budgeting reduce transform starvation.
- Dialogue events are transmitted; UI mirroring is still incomplete.

## Limitations

- AssetBundle avatar support is runtime-wired; the installed Quaternius bundle now passes Unity 2021.3 load validation as a render-only/static avatar bundle.
- Visible avatars are display proxies, not a full second gameplay controller yet; client uses local controller or freecam.
- Dialogue UI can flicker or appear briefly on the client.
- Item ownership, hand attachment, and physics replication are incomplete.
- AI sync is transform-only; behavior state is not replicated.
- Pizzeria and RoadTrip sync coverage has been expanded but still needs a full end-to-end validation pass.
- Mid-game Jenga / Ouija detailed state (block selection, planchette position, letter stream) is not yet synced — sequence flag only.

## Future features

- Spawn a real second player prefab per scene (Cabin, Pizzeria, RoadTrip).
- Networked input and animation state replication.
- Proper item ownership, pickup/throw authority, and hand attachment sync.
- Server-side gating for story triggers and cutscene synchronization.
- AI behavior state replication (not only transform).
- Multi-client support (more than one client).

## Next steps to take

1. Launch host/client with `RemotePlayerAvatarSource=Auto` and verify Pizzeria shows a game-model avatar or compact capsule, not the tall fallback.
2. Run RoadTrip -> Pizzeria -> Cabin and inspect session logs for avatar diagnostics plus Pizzeria/RoadTrip scene flag application.
3. Sync mid-game Jenga / Ouija detailed state (block selection events, planchette position, letter stream) on top of the existing sequence-flag coverage.
4. Test each Quaternius avatar id with `RemotePlayerAvatarSource=AssetBundle`; animation can be reintroduced after runtime loading is confirmed in-game.
5. Implement client-held item ownership (host confirms, client mirrors).

## TODO

- Validate in-scene avatar choices per scene and tune per-avatar scale/y-offset.
- Reintroduce Quaternius locomotion clips after render-only AssetBundle loading is confirmed in-game.
- Add true second gameplay controller for Cabin.
- Sync held items (left/right hand) and throw events.
- Sync door audio and lock state consistently.
- Continue expanding story flag coverage beyond PlayerPrefs for scene-specific in-memory flags.
- Add UDP stats (drop rate, last packet time) to overlay.
