using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using WoodburySpectatorSync.Coop;
using WoodburySpectatorSync.Config;
using WoodburySpectatorSync.Diagnostics;
using WoodburySpectatorSync.Net;
using WoodburySpectatorSync.Sync;
using WoodburySpectatorSync.UI;

namespace WoodburySpectatorSync
{
    // TODO (IL2CPP): Swap to BepInEx IL2CPP chainloader and update project references.
[BepInPlugin("com.woodbury.spectatorsync", "Woodbury Spectator Sync", "0.2.30")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private Settings _settings;
        private HostServer _hostServer;
        private SpectatorClient _spectatorClient;
        private CameraFollower _cameraFollower;
        private SceneSync _sceneSync;
        private Overlay _overlay;
        private RemoteDialogueOverlay _remoteDialogueOverlay;
        private SessionLog _sessionLog;
        private CoopServer _coopServer;
        private CoopClient _coopClient;
        private CoopHostCoordinator _coopHost;
        private CoopClientCoordinator _coopClientCoordinator;
        private bool _autoHostStarted;
        private bool _autoClientStarted;
        private string _lastOverlaySnapshot = string.Empty;
        private float _nextOverlayLogTime;
        private string _lastStatusSnapshot = string.Empty;
        private float _nextStatusLogTime;
        private float _nextHeartbeatLogTime;

        private string _currentProgressMarker = string.Empty;
        private int _progressNoteIndex;
        private readonly string[] _progressNotes = new[] { "checkpoint", "cutscene", "door", "phone", "misc" };

        private void Awake()
        {
            var configFile = CreateConfigFile();
            _settings = Settings.Bind(configFile);
            ApplyRuntimeOverrides(_settings);
            Application.runInBackground = true;
            _overlay = new Overlay(_settings);
            _remoteDialogueOverlay = new RemoteDialogueOverlay();
            _sessionLog = new SessionLog(Logger);
            _cameraFollower = new CameraFollower(_settings);
            _sceneSync = new SceneSync();
            _hostServer = new HostServer(Logger, _settings);
            _spectatorClient = new SpectatorClient(Logger, _settings);
            _coopServer = new CoopServer(Logger, _settings);
            _coopClient = new CoopClient(Logger, _settings);
            _coopHost = new CoopHostCoordinator(Logger, _settings, _coopServer);
            _coopClientCoordinator = new CoopClientCoordinator(Logger, _settings, _coopClient);

            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            Logger.LogInfo("Woodbury Spectator Sync loaded (runInBackground enabled)");
            _sessionLog?.Write("Plugin loaded");
            _sessionLog?.Write("Config: " + (configFile != null ? configFile.ConfigFilePath : "default"));
        }

        private ConfigFile CreateConfigFile()
        {
            var overridePath = Environment.GetEnvironmentVariable("WSS_CONFIG");
            if (string.IsNullOrWhiteSpace(overridePath))
            {
                return Config;
            }

            try
            {
                var directory = Path.GetDirectoryName(overridePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                Logger.LogInfo("Using override config: " + overridePath);
                return new ConfigFile(overridePath, true);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Config override failed, falling back to default: " + ex.Message);
                return Config;
            }
        }

        private void ApplyRuntimeOverrides(Settings settings)
        {
            var modeOverride = Environment.GetEnvironmentVariable("WSS_MODE");
            if (!string.IsNullOrWhiteSpace(modeOverride) &&
                Enum.TryParse(modeOverride, true, out Mode parsedMode))
            {
                settings.ModeSetting.Value = parsedMode;
                _sessionLog?.Write("Override: Mode=" + parsedMode);
            }

            var udpOverride = Environment.GetEnvironmentVariable("WSS_UDP");
            if (!string.IsNullOrWhiteSpace(udpOverride) &&
                bool.TryParse(udpOverride, out var udpEnabled))
            {
                settings.UdpEnabled.Value = udpEnabled;
                _sessionLog?.Write("Override: UdpEnabled=" + udpEnabled);
            }

            var hostIpOverride = Environment.GetEnvironmentVariable("WSS_HOST_IP");
            if (!string.IsNullOrWhiteSpace(hostIpOverride))
            {
                settings.SpectatorHostIP.Value = hostIpOverride;
                _sessionLog?.Write("Override: SpectatorHostIP=" + hostIpOverride);
            }

            var hostPortOverride = Environment.GetEnvironmentVariable("WSS_HOST_PORT");
            if (!string.IsNullOrWhiteSpace(hostPortOverride) &&
                int.TryParse(hostPortOverride, out var hostPort))
            {
                settings.HostPort.Value = hostPort;
                _sessionLog?.Write("Override: HostPort=" + hostPort);
            }

            var udpPortOverride = Environment.GetEnvironmentVariable("WSS_UDP_PORT");
            if (!string.IsNullOrWhiteSpace(udpPortOverride) &&
                int.TryParse(udpPortOverride, out var udpPort))
            {
                settings.UdpPort.Value = udpPort;
                _sessionLog?.Write("Override: UdpPort=" + udpPort);
            }
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            _hostServer.Stop();
            _spectatorClient.Disconnect();
            _coopServer.Stop();
            _coopClient.Disconnect();
            _coopHost.Shutdown();
            _coopClientCoordinator.Shutdown();
            _sessionLog?.Write("Plugin shutdown");
            _sessionLog?.Dispose();
            _sessionLog = null;
        }

        private void Update()
        {
            HandleHotkeys();
            HandleAutoStart();

            if (_settings.ModeSetting.Value == Mode.Host)
            {
                SampleHostCamera();
            }
            else if (_settings.ModeSetting.Value == Mode.Spectator)
            {
                DrainSpectatorMessages();
                _sceneSync.Update();
                if (!_spectatorClient.IsConnected)
                {
                    _cameraFollower.ResetTarget();
                    return;
                }

                if (!_sceneSync.IsLoading)
                {
                    _cameraFollower.Update(true);
                }
            }
            else if (_settings.ModeSetting.Value == Mode.CoopHost)
            {
                _coopHost.Update();
            }
            else if (_settings.ModeSetting.Value == Mode.CoopClient)
            {
                _coopClientCoordinator.Update();
            }

            MaybeLogHeartbeat();
        }

        private void OnGUI()
        {
            var modeLabel = _settings.ModeSetting.Value.ToString();
            var status = _settings.ModeSetting.Value == Mode.Host
                ? (_hostServer.IsRunning ? (_hostServer.IsClientConnected ? "Hosting (client connected)" : "Hosting (waiting)") : "Host idle")
                : (_settings.ModeSetting.Value == Mode.Spectator ? _spectatorClient.Status
                    : _settings.ModeSetting.Value == Mode.CoopHost
                        ? (_coopServer.IsRunning ? (_coopServer.IsClientConnected ? "Co-op hosting (client connected)" : "Co-op hosting (waiting)") : "Co-op host idle")
                        : _coopClient.Status);
            var extra = BuildOverlayExtras();
            var overlayText = _overlay.Draw(modeLabel, status, SceneManager.GetActiveScene().name, extra);
            MaybeLogOverlay(overlayText);
            MaybeLogStatus(modeLabel, status, SceneManager.GetActiveScene().name);

            if (_settings.ModeSetting.Value == Mode.CoopClient && _coopClientCoordinator != null)
            {
                if (_coopClientCoordinator.TryGetRemoteDialogue(out var speaker, out var text, out var kind))
                {
                    _remoteDialogueOverlay.Draw(speaker, text);
                }
            }
        }

        private void HandleHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                _overlay.Toggle();
            }

            if (_settings.ModeSetting.Value == Mode.Host)
            {
                if (Input.GetKeyDown(KeyCode.F6))
                {
                    if (_hostServer.IsRunning) _hostServer.Stop(); else _hostServer.Start();
                    _sessionLog?.Write("Hotkey F6: host " + (_hostServer.IsRunning ? "started" : "stopped"));
                }

                if (Input.GetKeyDown(KeyCode.F9))
                {
                    CycleProgressMarker();
                }
            }
            else if (_settings.ModeSetting.Value == Mode.Spectator)
            {
                if (Input.GetKeyDown(KeyCode.F7))
                {
                    if (_spectatorClient.IsConnected) _spectatorClient.Disconnect(); else _spectatorClient.Connect();
                    _sessionLog?.Write("Hotkey F7: spectator " + (_spectatorClient.IsConnected ? "connected" : "connecting"));
                }
            }
            else if (_settings.ModeSetting.Value == Mode.CoopHost)
            {
                if (Input.GetKeyDown(KeyCode.F6))
                {
                    if (_coopServer.IsRunning) _coopServer.Stop(); else _coopServer.Start();
                    _sessionLog?.Write("Hotkey F6: coop host " + (_coopServer.IsRunning ? "started" : "stopped"));
                }
            }
            else if (_settings.ModeSetting.Value == Mode.CoopClient)
            {
                if (Input.GetKeyDown(KeyCode.F7))
                {
                    if (_coopClient.IsConnected) _coopClient.Disconnect(); else _coopClient.Connect();
                    _sessionLog?.Write("Hotkey F7: coop client " + (_coopClient.IsConnected ? "connected" : "connecting"));
                }
            }
        }

        private void HandleAutoStart()
        {
            if (_settings.ModeSetting.Value == Mode.CoopHost && _settings.CoopAutoStartHost.Value)
            {
                if (!_autoHostStarted)
                {
                    _autoHostStarted = true;
                    if (!_coopServer.IsRunning)
                    {
                        _coopServer.Start();
                    }
                }
            }

            if (_settings.ModeSetting.Value == Mode.CoopClient && _settings.CoopAutoConnectClient.Value)
            {
                if (!_autoClientStarted)
                {
                    _autoClientStarted = true;
                    if (!_coopClient.IsConnected)
                    {
                        _coopClient.Connect();
                    }
                }
            }
        }

        private void SampleHostCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameras = UnityEngine.Object.FindObjectsOfType<Camera>();
                if (cameras.Length > 0) camera = cameras[0];
            }

            if (camera == null) return;

            var state = new CameraState
            {
                UnixTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Position = camera.transform.position,
                Rotation = camera.transform.rotation,
                Fov = camera.fieldOfView
            };

            _hostServer.SetLatestCamera(state);
        }

        private void DrainSpectatorMessages()
        {
            while (_spectatorClient.TryDequeue(out var message))
            {
                if (message is CameraStateMessage cam)
                {
                    _cameraFollower.SetTarget(cam.State);
                }
                else if (message is SceneChangeMessage scene)
                {
                    _sceneSync.RequestSceneLoad(scene.SceneName);
                }
                else if (message is ProgressMarkerMessage progress)
                {
                    _currentProgressMarker = progress.Marker;
                    _overlay.SetProgressMarker(_currentProgressMarker);
                }
            }
        }

        private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            if (_settings.ModeSetting.Value != Mode.Host) return;
            if (_hostServer.IsRunning)
            {
                _hostServer.QueueSceneChange(newScene.name, newScene.buildIndex);
            }
        }

        private void CycleProgressMarker()
        {
            var note = _progressNotes[_progressNoteIndex % _progressNotes.Length];
            _progressNoteIndex++;
            var marker = DateTime.Now.ToString("HH:mm:ss") + " - " + note;
            _currentProgressMarker = marker;
            _overlay.SetProgressMarker(marker);
            _sessionLog?.Write("Progress marker: " + marker);
            if (_hostServer.IsRunning)
            {
                _hostServer.QueueProgressMarker(marker);
            }
        }

        private string[] BuildOverlayExtras()
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (_settings.ModeSetting.Value == Mode.CoopHost)
            {
                var sequenceLabel = GetSequenceLabel();
                return new[]
                {
                    "SceneReady: " + (_coopHost.ClientSceneReady ? "yes" : "no") + ", awaiting=" + (_coopHost.AwaitingSceneReady ? "yes" : "no"),
                    "ClientScene: " + (string.IsNullOrEmpty(_coopHost.ClientSceneName) ? "-" : _coopHost.ClientSceneName),
                    "LastSceneReq: " + FormatAge(nowMs, _coopHost.LastSceneRequestMs) + ", ready: " + FormatAge(nowMs, _coopHost.LastSceneReadyMs),
                    "LastClientPkt: " + FormatAge(nowMs, _coopHost.LastClientTransformMs),
                    "HostTx: " + FormatAge(nowMs, _coopHost.LastHostTransformSendMs) +
                    ", tcpTx: " + FormatAge(nowMs, _coopHost.LastHostTransformSendTcpMs) +
                    ", udpTx: " + FormatAge(nowMs, _coopHost.LastHostTransformSendUdpMs) +
                    ", count=" + _coopHost.HostTransformSendCount,
                    "HostState: enq=" + _coopServer.HostStateEnqueued +
                    ", sent=" + _coopServer.HostStateSent +
                    ", last=" + FormatAge(nowMs, _coopServer.LastHostStateSentMs) +
                    ", type=" + _coopServer.LastHostStateType,
                    "Sequence: " + sequenceLabel,
                    BuildDialogueStatus("Dialogue", _coopHost.DialogueConversationId, _coopHost.DialogueEntryId, _coopHost.DialogueChoiceIndex, _coopHost.DialogueLastEventMs, nowMs),
                    BuildStoryStatus(_coopHost.LastStoryEventKey, _coopHost.LastStoryEventValue, _coopHost.LastStoryEventMs, nowMs),
                    BuildUdpLine(_coopServer.HasUdp, _coopServer.UdpLastReceiveMs, nowMs)
                };
            }

            if (_settings.ModeSetting.Value == Mode.CoopClient)
            {
                var progress = Mathf.Clamp01(_coopClientCoordinator.LoadingProgress) * 100f;
                var sceneState = _coopClientCoordinator.IsSceneLoading ? ("loading " + progress.ToString("0") + "%") : "ready";
                var pendingScene = string.IsNullOrEmpty(_coopClientCoordinator.PendingSceneName) ? "-" : _coopClientCoordinator.PendingSceneName;
                var sequenceLabel = GetSequenceLabel();
                var localDialogue = _coopClientCoordinator.TryGetLocalDialogueState(out var localConvo, out var localEntry)
                    ? "conv=" + localConvo + " entry=" + localEntry
                    : "conv=- entry=-";
                var hostId = _coopClientCoordinator.LastHostPlayerId == 255
                    ? "-"
                    : _coopClientCoordinator.LastHostPlayerId.ToString();
                var latestSeq = _coopClient.LatestHostTransformSeq;
                var consumedSeq = _coopClient.LastConsumedHostTransformSeq;
                var latestNetMs = Math.Max(_coopClient.LastTcpTransformMs, _coopClient.LastUdpTransformMs);
                return new[]
                {
                    "Ping: " + FormatPing(_coopClient.LastPingRttMs) + ", TCP lastRx: " + FormatAge(nowMs, _coopClient.LastTcpReceiveMs),
                    BuildUdpLine(_coopClient.HasUdp, _coopClient.UdpLastReceiveMs, nowMs),
                    "SceneSync: " + sceneState + ", pending=" + pendingScene,
                    "Pending queues: doors=" + _coopClientCoordinator.PendingDoorCount + ", holdables=" + _coopClientCoordinator.PendingHoldableCount + ", ai=" + _coopClientCoordinator.PendingAiCount,
                    "HostRx: " + FormatAge(nowMs, _coopClientCoordinator.LastHostTransformReceiveMs) + ", count=" + _coopClientCoordinator.HostTransformReceiveCount + ", id=" + hostId,
                    "HostNet: tcp=" + _coopClient.TcpTransformCount + " (" + FormatAge(nowMs, _coopClient.LastTcpTransformMs) + "), udp=" + _coopClient.UdpTransformCount + " (" + FormatAge(nowMs, _coopClient.LastUdpTransformMs) + "), last=" + _coopClient.LastTransformSource,
                    "HostState: read=" + _coopClient.HostStateReadCount + ", enq=" + _coopClient.HostStateEnqueuedCount + ", applied=" + _coopClientCoordinator.HostStateAppliedCount,
                    "HostLatch: seq=" + latestSeq + ", consumed=" + consumedSeq + ", lastNet=" + FormatAge(nowMs, latestNetMs),
                    "HostUpdateAge: " + _coopClientCoordinator.LastHostUpdateAgeSeconds.ToString("0.0") + "s",
                    "Sequence: " + sequenceLabel,
                    BuildDialogueStatus("DialogueHost", _coopClientCoordinator.HostDialogueConversationId, _coopClientCoordinator.HostDialogueEntryId, _coopClientCoordinator.HostDialogueChoiceIndex, _coopClientCoordinator.HostDialogueEventMs, nowMs),
                    "DialogueLocal: " + localDialogue,
                    BuildStoryStatus(_coopClientCoordinator.LastStoryEventKey, _coopClientCoordinator.LastStoryEventValue, _coopClientCoordinator.LastStoryEventMs, nowMs)
                };
            }

            return null;
        }

        private string BuildUdpLine(bool hasUdp, long lastReceiveMs, long nowMs)
        {
            if (!_settings.UdpEnabled.Value)
            {
                return "UDP: disabled";
            }

            var status = hasUdp ? "ready" : "pending";
            return "UDP: " + status + ", lastRx: " + FormatAge(nowMs, lastReceiveMs);
        }

        private static string FormatAge(long nowMs, long lastMs)
        {
            if (lastMs <= 0) return "n/a";
            var delta = nowMs - lastMs;
            if (delta < 0) delta = 0;
            if (delta >= 1000)
            {
                return (delta / 1000f).ToString("0.0") + "s";
            }
            return delta + "ms";
        }

        private static string FormatPing(long pingMs)
        {
            return pingMs > 0 ? pingMs + "ms" : "n/a";
        }

        private static string BuildDialogueStatus(string label, int conversationId, int entryId, int choiceIndex, long lastEventMs, long nowMs)
        {
            var convoLabel = conversationId >= 0 ? conversationId.ToString() : "-";
            var entryLabel = entryId >= 0 ? entryId.ToString() : "-";
            var choiceLabel = choiceIndex >= 0 ? choiceIndex.ToString() : "-";
            return label + ": conv=" + convoLabel + " entry=" + entryLabel + " choice=" + choiceLabel + " last=" + FormatAge(nowMs, lastEventMs);
        }

        private static string BuildStoryStatus(string key, int value, long lastEventMs, long nowMs)
        {
            var label = string.IsNullOrEmpty(key) ? "-" : key;
            return "StoryFlag: " + label + "=" + value + " last=" + FormatAge(nowMs, lastEventMs);
        }

        private static string GetSequenceLabel()
        {
            var cabin = UnityEngine.Object.FindObjectOfType<CabinGameManager>();
            if (cabin != null)
            {
                return cabin.CurrentSequence.ToString();
            }

            return "-";
        }

        private void MaybeLogOverlay(string overlayText)
        {
            if (_sessionLog == null) return;
            if (string.IsNullOrEmpty(overlayText)) return;

            var now = Time.realtimeSinceStartup;
            if (now < _nextOverlayLogTime) return;

            _lastOverlaySnapshot = overlayText;
            _nextOverlayLogTime = now + 5.0f;
            _sessionLog.Write("Overlay:");
            _sessionLog.Write(overlayText);
        }

        private void MaybeLogStatus(string modeLabel, string status, string sceneName)
        {
            if (_sessionLog == null) return;

            var now = Time.realtimeSinceStartup;
            var snapshot = modeLabel + "|" + status + "|" + sceneName;
            if (snapshot == _lastStatusSnapshot && now < _nextStatusLogTime) return;

            _lastStatusSnapshot = snapshot;
            _nextStatusLogTime = now + 5.0f;
            _sessionLog.Write("State: mode=" + modeLabel + " status=" + status + " scene=" + sceneName);
        }

        private void MaybeLogHeartbeat()
        {
            if (_sessionLog == null) return;

            var now = Time.realtimeSinceStartup;
            if (now < _nextHeartbeatLogTime) return;
            _nextHeartbeatLogTime = now + 5.0f;

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (_settings.ModeSetting.Value == Mode.CoopHost)
            {
                _sessionLog.Write(
                    "Heartbeat host: tcpRx=" + FormatAge(nowMs, _coopServer.LastTcpReceiveMs) +
                    " udpRx=" + FormatAge(nowMs, _coopServer.UdpLastReceiveMs) +
                    " hostSend=" + FormatAge(nowMs, _coopHost.LastHostTransformSendMs) +
                    " udpSend=" + FormatAge(nowMs, _coopHost.LastHostTransformSendUdpMs) +
                    " tcpSend=" + FormatAge(nowMs, _coopHost.LastHostTransformSendTcpMs) +
                    " sendCount=" + _coopHost.HostTransformSendCount +
                    " lastClientPkt=" + FormatAge(nowMs, _coopHost.LastClientTransformMs)
                );
            }
            else if (_settings.ModeSetting.Value == Mode.CoopClient)
            {
                _sessionLog.Write(
                    "Heartbeat client: tcpRx=" + FormatAge(nowMs, _coopClient.LastTcpReceiveMs) +
                    " udpRx=" + FormatAge(nowMs, _coopClient.UdpLastReceiveMs) +
                    " hostRx=" + FormatAge(nowMs, _coopClientCoordinator.LastHostTransformReceiveMs) +
                    " hostRxCount=" + _coopClientCoordinator.HostTransformReceiveCount +
                    " hostUpdateAge=" + _coopClientCoordinator.LastHostUpdateAgeSeconds.ToString("0.0") + "s" +
                    " tcpXform=" + _coopClient.TcpTransformCount +
                    " udpXform=" + _coopClient.UdpTransformCount +
                    " lastSrc=" + _coopClient.LastTransformSource
                );
            }
        }
    }
}
