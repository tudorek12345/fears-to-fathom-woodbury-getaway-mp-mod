# Changelog

## Unreleased
- Client: forcibly release dialogue camera locks during Mike conversations (StopConversation + ResumeCameraControl + reset player state).
- Co-op: HostUpdateAge now reports time since applied host transform; HostRxAge remains for receive timing.
- Co-op: host avatar now recreates safely after scene changes to prevent ApplyHostTransform failures.
- Notes: door and fridge interactions mirror on both host and client in Cabin (Board Game flow).

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
