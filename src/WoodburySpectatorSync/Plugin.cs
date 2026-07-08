using System;
using System.Collections.Generic;
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
    [BepInPlugin("com.woodbury.spectatorsync", "Woodbury Spectator Sync", "0.4.43")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private Settings _settings;
        private HostServer _hostServer;
        private SpectatorClient _spectatorClient;
        private CameraFollower _cameraFollower;
        private SceneSync _sceneSync;
        private Overlay _overlay;
        private CoopMainMenuPanel _coopMenuPanel;
        private CoopBrandMark _brandMark;
        private WaitingSpinnerOverlay _waitingSpinnerOverlay;
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
        private string _cachedOverlayText;
        private int _cachedOverlayLineCount;
        private string _cachedOverlaySnapshot = string.Empty;
        private float _nextOverlayBuildTime;
        private float _nextSceneDiscoveryDumpTime;
        private string[] _sceneDiscoveryCrawlerScenes = new string[0];
        private int _sceneDiscoveryCrawlerIndex = -1;
        private bool _sceneDiscoveryCrawlerPrepared;
        private bool _sceneDiscoveryCrawlerCompleted;
        private float _nextSceneDiscoveryCrawlerActionTime;
        private float _nextSceneDiscoveryCrawlerStatusLogTime;

        private const float SessionSnapshotLogIntervalSeconds = 15f;
        private const float OverlayBuildIntervalSeconds = 0.25f;

        private string _currentProgressMarker = string.Empty;
        private int _progressNoteIndex;
        private readonly string[] _progressNotes = new[] { "checkpoint", "cutscene", "door", "phone", "misc" };

        private void Awake()
        {
            var configFile = CreateConfigFile();
            _settings = Settings.Bind(configFile);
            ApplySteamworksLaunchOverrides(_settings);
            ApplyRuntimeOverrides(_settings);
            Application.runInBackground = true;
            _overlay = new Overlay(_settings);
            _coopMenuPanel = new CoopMainMenuPanel(_settings);
            _brandMark = new CoopBrandMark();
            _waitingSpinnerOverlay = new WaitingSpinnerOverlay();
            _remoteDialogueOverlay = new RemoteDialogueOverlay();
            _sessionLog = ShouldEnableSessionLog(_settings)
                ? new SessionLog(Logger, _settings.ModeSetting.Value.ToString())
                : null;
            _cameraFollower = new CameraFollower(_settings);
            _sceneSync = new SceneSync();
            _hostServer = new HostServer(Logger, _settings);
            _spectatorClient = new SpectatorClient(Logger, _settings);
            _coopServer = new CoopServer(Logger, _settings);
            _coopClient = new CoopClient(Logger, _settings);
            _coopHost = new CoopHostCoordinator(
                Logger,
                _settings,
                _coopServer,
                _sessionLog != null ? new Action<string>(_sessionLog.Write) : null);
            _coopClientCoordinator = new CoopClientCoordinator(
                Logger,
                _settings,
                _coopClient,
                _sessionLog != null ? new Action<string>(_sessionLog.Write) : null);

            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            Logger.LogInfo("Woodbury Spectator Sync loaded (runInBackground enabled)");
            _sessionLog?.Write("Plugin loaded");
            _sessionLog?.Write("Config: " + (configFile != null ? configFile.ConfigFilePath : "default"));
        }

        private void ApplySteamworksLaunchOverrides(Settings settings)
        {
            var appIdText = ReadSteamworksAppIdOverride(settings);
            if (string.IsNullOrWhiteSpace(appIdText))
            {
                ClearManagedSteamworksAppIdOverride();
                return;
            }

            if (!uint.TryParse(appIdText.Trim(), out var appId) || appId == 0)
            {
                Logger.LogWarning("Steamworks launch compatibility: ignored invalid app id override '" + appIdText + "'");
                return;
            }

            try
            {
                var appIdString = appId.ToString();
                Environment.SetEnvironmentVariable("SteamAppId", appIdString);
                Environment.SetEnvironmentVariable("SteamGameId", appIdString);

                var gameRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var appIdPath = Path.Combine(gameRoot, "steam_appid.txt");
                var markerPath = Path.Combine(gameRoot, "WoodburySpectatorSync.steam_appid.managed");
                File.WriteAllText(appIdPath, appIdString);
                File.WriteAllText(markerPath, appIdString);
                Logger.LogInfo("Steamworks launch compatibility: app id override applied id=" + appIdString +
                    " file=" + appIdPath);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Steamworks launch compatibility: failed to apply app id override reason=" + ex.Message);
            }
        }

        private void ClearManagedSteamworksAppIdOverride()
        {
            try
            {
                var gameRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var appIdPath = Path.Combine(gameRoot, "steam_appid.txt");
                var markerPath = Path.Combine(gameRoot, "WoodburySpectatorSync.steam_appid.managed");
                if (!File.Exists(markerPath))
                {
                    if (File.Exists(appIdPath) &&
                        string.Equals(File.ReadAllText(appIdPath).Trim(), "480", StringComparison.Ordinal))
                    {
                        File.Delete(appIdPath);
                        Logger.LogInfo("Steamworks launch compatibility: removed previous test app id file=" + appIdPath);
                    }
                    return;
                }

                if (File.Exists(appIdPath))
                {
                    File.Delete(appIdPath);
                }
                File.Delete(markerPath);
                Logger.LogInfo("Steamworks launch compatibility: disabled managed app id override");
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Steamworks launch compatibility: failed to clear managed app id override reason=" + ex.Message);
            }
        }

        private static string ReadSteamworksAppIdOverride(Settings settings)
        {
            var value = Environment.GetEnvironmentVariable("WSS_STEAMWORKS_APP_ID");
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            value = Environment.GetEnvironmentVariable("WSS_STEAM_APP_ID");
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (string.IsNullOrWhiteSpace(arg))
                {
                    continue;
                }

                var equalsIndex = arg.IndexOf('=');
                if (equalsIndex > 0)
                {
                    var key = arg.Substring(0, equalsIndex);
                    if (IsSteamworksAppIdArgument(key))
                    {
                        return arg.Substring(equalsIndex + 1);
                    }
                }

                if (IsSteamworksAppIdArgument(arg) && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
            }

            if (settings != null && settings.SteamworksAppIdMode != null)
            {
                switch (settings.SteamworksAppIdMode.Value)
                {
                    case SteamworksAppIdMode.SteamworksTestApp:
                        return "480";
                    case SteamworksAppIdMode.Custom:
                        if (settings.SteamworksCustomAppId != null && settings.SteamworksCustomAppId.Value > 0)
                        {
                            return settings.SteamworksCustomAppId.Value.ToString();
                        }
                        break;
                }
            }

            return string.Empty;
        }

        private static bool IsSteamworksAppIdArgument(string arg)
        {
            return string.Equals(arg, "-WSSSteamAppId", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(arg, "--WSSSteamAppId", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(arg, "-SteamAppId", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(arg, "-steam_appid", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldEnableSessionLog(Settings settings)
        {
            var overrideValue = Environment.GetEnvironmentVariable("WSS_SESSION_LOG");
            if (!string.IsNullOrWhiteSpace(overrideValue) &&
                bool.TryParse(overrideValue, out var enabled))
            {
                return enabled;
            }

            return settings != null &&
                   settings.VerboseLogging != null &&
                   settings.VerboseLogging.Value;
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
            HandlePeriodicSceneDiscoveryDump();
            HandleSceneDiscoveryCrawler();

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
            var sceneName = SceneManager.GetActiveScene().name;
            var overlayText = DrawCachedOverlay(modeLabel, status, sceneName);
            MaybeLogStatus(modeLabel, status, sceneName);

            if (_settings.ModeSetting.Value == Mode.CoopClient && _coopClientCoordinator != null)
            {
                if (_coopClientCoordinator.TryGetRemoteDialogue(out var speaker, out var text, out var kind))
                {
                    _remoteDialogueOverlay.Draw(speaker, text, kind);
                }
            }

            DrawVoiceHud();
            DrawHostWaitIndicator();
            _brandMark?.Draw();
            _coopMenuPanel?.Draw(
                _coopServer,
                _coopClient,
                _overlay,
                _sessionLog != null ? new Action<string>(_sessionLog.Write) : null);
        }

        private string DrawCachedOverlay(string modeLabel, string status, string sceneName)
        {
            if (_overlay == null || !_overlay.IsVisible)
            {
                return null;
            }

            var now = Time.realtimeSinceStartup;
            var snapshot = modeLabel + "|" + status + "|" + sceneName;
            if (_cachedOverlayText == null ||
                snapshot != _cachedOverlaySnapshot ||
                now >= _nextOverlayBuildTime)
            {
                var extra = BuildOverlayExtras();
                _cachedOverlayText = _overlay.BuildText(modeLabel, status, sceneName, extra, out _cachedOverlayLineCount);
                _cachedOverlaySnapshot = snapshot;
                _nextOverlayBuildTime = now + OverlayBuildIntervalSeconds;
            }

            var overlayText = _overlay.DrawText(_cachedOverlayText, _cachedOverlayLineCount);
            MaybeLogOverlay(overlayText);
            return overlayText;
        }

        private void DrawHostWaitIndicator()
        {
            if (_settings == null ||
                _settings.ModeSetting.Value != Mode.CoopHost ||
                _coopHost == null ||
                !_coopHost.ShouldShowHostWaitIndicator)
            {
                return;
            }

            _waitingSpinnerOverlay?.Draw(_coopHost.HostWaitIndicatorTitle, _coopHost.HostWaitIndicatorDetail);
        }

        private void DrawVoiceHud()
        {
            if (_settings == null)
            {
                return;
            }

            if (_settings.ModeSetting.Value == Mode.CoopHost)
            {
                _coopHost?.DrawVoiceHud();
            }
            else if (_settings.ModeSetting.Value == Mode.CoopClient)
            {
                _coopClientCoordinator?.DrawVoiceHud();
            }
        }

        private void HandleHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                _overlay.Toggle();
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                DumpSceneDiscoveryNow();
            }

            if (Input.GetKeyDown(KeyCode.F11))
            {
                _coopMenuPanel?.Toggle();
                _sessionLog?.Write("Hotkey F11: co-op menu toggled");
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

        private void DumpSceneDiscoveryNow()
        {
            if (_settings == null ||
                _settings.SceneDiscoveryDump == null ||
                !_settings.SceneDiscoveryDump.Value)
            {
                return;
            }

            var scene = SceneManager.GetActiveScene();
            var role = GetSceneDiscoveryRole();
            Action<string> sessionWrite = _sessionLog != null ? _sessionLog.Write : (Action<string>)null;
            SceneDiscoveryDump.LogManualIfEnabled(_settings, role, scene, Logger, sessionWrite);
            _sessionLog?.Write("Hotkey F10: scene discovery manual dump role=" + role + " scene=" + scene.name);
        }

        private void HandlePeriodicSceneDiscoveryDump()
        {
            if (_settings == null ||
                _settings.SceneDiscoveryDump == null ||
                !_settings.SceneDiscoveryDump.Value ||
                _settings.SceneDiscoveryDumpIntervalSeconds == null)
            {
                _nextSceneDiscoveryDumpTime = 0f;
                return;
            }

            var interval = Mathf.Clamp(_settings.SceneDiscoveryDumpIntervalSeconds.Value, 0f, 600f);
            if (interval <= 0f)
            {
                _nextSceneDiscoveryDumpTime = 0f;
                return;
            }

            var now = Time.realtimeSinceStartup;
            if (_nextSceneDiscoveryDumpTime <= 0f)
            {
                _nextSceneDiscoveryDumpTime = now + interval;
                return;
            }

            if (now < _nextSceneDiscoveryDumpTime)
            {
                return;
            }

            _nextSceneDiscoveryDumpTime = now + interval;
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            var role = GetSceneDiscoveryRole();
            Action<string> sessionWrite = _sessionLog != null ? _sessionLog.Write : (Action<string>)null;
            SceneDiscoveryDump.LogTimedIfEnabled(_settings, role, scene, Logger, sessionWrite);
            _sessionLog?.Write("Timed scene discovery dump role=" + role + " scene=" + scene.name + " interval=" + interval.ToString("0.###"));
        }

        private void HandleSceneDiscoveryCrawler()
        {
            if (!IsSceneDiscoveryCrawlerEnabled())
            {
                ResetSceneDiscoveryCrawler();
                return;
            }

            if (!_sceneDiscoveryCrawlerPrepared)
            {
                PrepareSceneDiscoveryCrawler();
            }

            if (_sceneDiscoveryCrawlerCompleted || _sceneDiscoveryCrawlerScenes.Length == 0)
            {
                return;
            }

            string waitReason;
            if (!CanSceneDiscoveryCrawlerAdvance(out waitReason))
            {
                MaybeLogSceneDiscoveryCrawlerStatus("waiting " + waitReason);
                return;
            }

            var now = Time.realtimeSinceStartup;
            if (_nextSceneDiscoveryCrawlerActionTime <= 0f)
            {
                _nextSceneDiscoveryCrawlerActionTime = now + GetSceneDiscoveryCrawlerStartDelaySeconds();
                return;
            }

            if (now < _nextSceneDiscoveryCrawlerActionTime)
            {
                return;
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                MaybeLogSceneDiscoveryCrawlerStatus("waiting active-scene-not-loaded");
                _nextSceneDiscoveryCrawlerActionTime = now + 2f;
                return;
            }

            var role = GetSceneDiscoveryRole();
            Action<string> sessionWrite = _sessionLog != null ? _sessionLog.Write : (Action<string>)null;
            SceneDiscoveryDump.LogCrawlerIfEnabled(_settings, role, scene, Logger, sessionWrite);
            LogSceneDiscoveryCrawler("dumped scene=" + scene.name + " index=" + _sceneDiscoveryCrawlerIndex);

            var nextIndex = GetNextSceneDiscoveryCrawlerIndex(scene.name);
            if (nextIndex < 0)
            {
                _sceneDiscoveryCrawlerCompleted = true;
                LogSceneDiscoveryCrawler("completed scenes=" + _sceneDiscoveryCrawlerScenes.Length);
                return;
            }

            LoadSceneDiscoveryCrawlerScene(nextIndex);
        }

        private bool IsSceneDiscoveryCrawlerEnabled()
        {
            if (_settings == null ||
                _settings.SceneDiscoveryDump == null ||
                !_settings.SceneDiscoveryDump.Value ||
                _settings.SceneDiscoveryDumpCrawler == null ||
                !_settings.SceneDiscoveryDumpCrawler.Value)
            {
                return false;
            }

            return _settings.ModeSetting.Value == Mode.Host ||
                   _settings.ModeSetting.Value == Mode.CoopHost;
        }

        private void ResetSceneDiscoveryCrawler()
        {
            _sceneDiscoveryCrawlerScenes = new string[0];
            _sceneDiscoveryCrawlerIndex = -1;
            _sceneDiscoveryCrawlerPrepared = false;
            _sceneDiscoveryCrawlerCompleted = false;
            _nextSceneDiscoveryCrawlerActionTime = 0f;
            _nextSceneDiscoveryCrawlerStatusLogTime = 0f;
        }

        private void PrepareSceneDiscoveryCrawler()
        {
            _sceneDiscoveryCrawlerScenes = BuildSceneDiscoveryCrawlerList();
            _sceneDiscoveryCrawlerPrepared = true;
            _sceneDiscoveryCrawlerCompleted = _sceneDiscoveryCrawlerScenes.Length == 0;
            _sceneDiscoveryCrawlerIndex = FindSceneDiscoveryCrawlerSceneIndex(SceneManager.GetActiveScene().name);
            _nextSceneDiscoveryCrawlerActionTime = Time.realtimeSinceStartup + GetSceneDiscoveryCrawlerStartDelaySeconds();
            LogSceneDiscoveryCrawler("enabled scenes=" + string.Join(",", _sceneDiscoveryCrawlerScenes) +
                " startDelay=" + GetSceneDiscoveryCrawlerStartDelaySeconds().ToString("0.###") +
                " sceneSeconds=" + GetSceneDiscoveryCrawlerSceneSeconds().ToString("0.###") +
                " requireLivePeer=" + BoolText(_settings.SceneDiscoveryDumpCrawlerRequireLivePeer != null && _settings.SceneDiscoveryDumpCrawlerRequireLivePeer.Value));
        }

        private string[] BuildSceneDiscoveryCrawlerList()
        {
            var configured = _settings.SceneDiscoveryDumpCrawlerScenes != null
                ? _settings.SceneDiscoveryDumpCrawlerScenes.Value
                : string.Empty;
            var scenes = new List<string>();
            if (!string.IsNullOrWhiteSpace(configured))
            {
                var parts = configured.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < parts.Length; i++)
                {
                    var value = parts[i].Trim();
                    if (value.Length == 0) continue;

                    int buildIndex;
                    if (int.TryParse(value, out buildIndex))
                    {
                        value = GetSceneNameByBuildIndex(buildIndex);
                    }

                    AddUniqueSceneName(scenes, value);
                }
            }
            else
            {
                var count = SceneManager.sceneCountInBuildSettings;
                for (var i = 0; i < count; i++)
                {
                    AddUniqueSceneName(scenes, GetSceneNameByBuildIndex(i));
                }
            }

            return scenes.ToArray();
        }

        private static void AddUniqueSceneName(List<string> scenes, string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) return;
            for (var i = 0; i < scenes.Count; i++)
            {
                if (string.Equals(scenes[i], sceneName, StringComparison.Ordinal))
                {
                    return;
                }
            }

            scenes.Add(sceneName);
        }

        private static string GetSceneNameByBuildIndex(int buildIndex)
        {
            if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
            {
                return string.Empty;
            }

            var path = SceneUtility.GetScenePathByBuildIndex(buildIndex);
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            return Path.GetFileNameWithoutExtension(path);
        }

        private bool CanSceneDiscoveryCrawlerAdvance(out string waitReason)
        {
            waitReason = string.Empty;
            if (_settings.ModeSetting.Value == Mode.CoopHost &&
                _settings.SceneDiscoveryDumpCrawlerRequireLivePeer != null &&
                _settings.SceneDiscoveryDumpCrawlerRequireLivePeer.Value)
            {
                if (_coopServer == null || !_coopServer.IsClientConnected)
                {
                    waitReason = "client-not-connected";
                    return false;
                }

                if (_coopHost == null || !string.Equals(_coopHost.SessionStateLabel, "Live", StringComparison.Ordinal))
                {
                    waitReason = "peer-not-live state=" + (_coopHost != null ? _coopHost.SessionStateLabel : "-");
                    return false;
                }
            }

            return true;
        }

        private int GetNextSceneDiscoveryCrawlerIndex(string currentSceneName)
        {
            var currentIndex = _sceneDiscoveryCrawlerIndex;
            var sceneIndex = FindSceneDiscoveryCrawlerSceneIndex(currentSceneName);
            if (sceneIndex >= 0)
            {
                currentIndex = sceneIndex;
                _sceneDiscoveryCrawlerIndex = sceneIndex;
            }

            var nextIndex = currentIndex + 1;
            if (nextIndex >= _sceneDiscoveryCrawlerScenes.Length)
            {
                if (_settings.SceneDiscoveryDumpCrawlerLoop != null && _settings.SceneDiscoveryDumpCrawlerLoop.Value)
                {
                    nextIndex = 0;
                }
                else
                {
                    return -1;
                }
            }

            return nextIndex;
        }

        private int FindSceneDiscoveryCrawlerSceneIndex(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) return -1;
            for (var i = 0; i < _sceneDiscoveryCrawlerScenes.Length; i++)
            {
                if (string.Equals(_sceneDiscoveryCrawlerScenes[i], sceneName, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private void LoadSceneDiscoveryCrawlerScene(int sceneIndex)
        {
            if (sceneIndex < 0 || sceneIndex >= _sceneDiscoveryCrawlerScenes.Length)
            {
                return;
            }

            var sceneName = _sceneDiscoveryCrawlerScenes[sceneIndex];
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                _sceneDiscoveryCrawlerIndex = sceneIndex;
                _nextSceneDiscoveryCrawlerActionTime = Time.realtimeSinceStartup + 1f;
                return;
            }

            try
            {
                _sceneDiscoveryCrawlerIndex = sceneIndex;
                _nextSceneDiscoveryCrawlerActionTime = Time.realtimeSinceStartup + GetSceneDiscoveryCrawlerSceneSeconds();
                LogSceneDiscoveryCrawler("loading scene=" + sceneName + " index=" + sceneIndex);
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            }
            catch (Exception ex)
            {
                LogSceneDiscoveryCrawler("load-failed scene=" + sceneName +
                    " errorType=" + ex.GetType().Name +
                    " message=" + ex.Message);
                _nextSceneDiscoveryCrawlerActionTime = Time.realtimeSinceStartup + 3f;
            }
        }

        private void HandleSceneDiscoveryCrawlerSceneChanged(Scene newScene)
        {
            if (!IsSceneDiscoveryCrawlerEnabled() || !_sceneDiscoveryCrawlerPrepared)
            {
                return;
            }

            var sceneIndex = FindSceneDiscoveryCrawlerSceneIndex(newScene.name);
            if (sceneIndex >= 0)
            {
                _sceneDiscoveryCrawlerIndex = sceneIndex;
            }

            _nextSceneDiscoveryCrawlerActionTime = Time.realtimeSinceStartup + GetSceneDiscoveryCrawlerSceneSeconds();
            LogSceneDiscoveryCrawler("scene-enter scene=" + newScene.name +
                " index=" + _sceneDiscoveryCrawlerIndex +
                " nextIn=" + GetSceneDiscoveryCrawlerSceneSeconds().ToString("0.###") + "s");
        }

        private float GetSceneDiscoveryCrawlerStartDelaySeconds()
        {
            return Mathf.Clamp(
                _settings.SceneDiscoveryDumpCrawlerStartDelaySeconds != null
                    ? _settings.SceneDiscoveryDumpCrawlerStartDelaySeconds.Value
                    : 20f,
                2f,
                600f);
        }

        private float GetSceneDiscoveryCrawlerSceneSeconds()
        {
            return Mathf.Clamp(
                _settings.SceneDiscoveryDumpCrawlerSceneSeconds != null
                    ? _settings.SceneDiscoveryDumpCrawlerSceneSeconds.Value
                    : 25f,
                5f,
                900f);
        }

        private void MaybeLogSceneDiscoveryCrawlerStatus(string message)
        {
            var now = Time.realtimeSinceStartup;
            if (now < _nextSceneDiscoveryCrawlerStatusLogTime)
            {
                return;
            }

            _nextSceneDiscoveryCrawlerStatusLogTime = now + 10f;
            LogSceneDiscoveryCrawler(message);
        }

        private void LogSceneDiscoveryCrawler(string message)
        {
            var line = "SceneDiscoveryDumpCrawler: " + message;
            Logger.LogInfo(line);
            _sessionLog?.Write(line);
        }

        private string GetSceneDiscoveryRole()
        {
            return _settings.ModeSetting.Value == Mode.CoopHost || _settings.ModeSetting.Value == Mode.Host
                ? "host"
                : _settings.ModeSetting.Value == Mode.CoopClient
                    ? "client"
                    : "spectator";
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
            HandleSceneDiscoveryCrawlerSceneChanged(newScene);

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
                    "Session: " + _coopHost.SessionStateLabel +
                    " sid=" + _coopHost.SessionId +
                    " gen=" + _coopHost.SessionGeneration +
                    " snapAck=" + _coopHost.LastSnapshotAckGeneration +
                    " retry=" + _coopHost.SceneChangeRetryCount,
                    _coopHost.HostWaitSummary,
                    "Peer: scene=" + (string.IsNullOrEmpty(_coopHost.ClientSceneName) ? "-" : _coopHost.ClientSceneName) +
                    " ready=" + (_coopHost.ClientSceneReady ? "yes" : "no") +
                    " awaiting=" + (_coopHost.AwaitingSceneReady ? "yes" : "no"),
                    "SceneSync: req=" + FormatAge(nowMs, _coopHost.LastSceneRequestMs) +
                    " ready=" + FormatAge(nowMs, _coopHost.LastSceneReadyMs),
                    "Net: clientPkt=" + FormatAge(nowMs, _coopHost.LastClientTransformMs) +
                    " hostTx=" + FormatAge(nowMs, _coopHost.LastHostTransformSendMs) +
                    " tcp=" + FormatAge(nowMs, _coopHost.LastHostTransformSendTcpMs) +
                    " udp=" + FormatAge(nowMs, _coopHost.LastHostTransformSendUdpMs),
                    "World: enq=" + _coopServer.HostStateEnqueued +
                    " sent=" + _coopServer.HostStateSent +
                    " type=" + _coopServer.LastHostStateType +
                    " age=" + FormatAge(nowMs, _coopServer.LastHostStateSentMs),
                    "Seq: " + sequenceLabel,
                    "Mike: " + _coopHost.LastCabinMikeSyncDebug,
                    _coopHost.NpcOverlaySummary,
                    _coopHost.BoardGameOverlaySummary,
                    _coopHost.VoiceOverlaySummary,
                    _coopHost.FootstepOverlaySummary,
                    BuildAvatarStatus(),
                    BuildDialogueStatus("Dlg", _coopHost.DialogueConversationId, _coopHost.DialogueEntryId, _coopHost.DialogueChoiceIndex, _coopHost.DialogueLastEventMs, nowMs),
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
                var appliedSeq = _coopClientCoordinator.LastAppliedHostTransformSeq;
                var appliedCount = _coopClientCoordinator.HostTransformAppliedCount;
                var appliedNetMs = _coopClientCoordinator.LastAppliedHostNetMs;
                return new[]
                {
                    "Session: " + _coopClientCoordinator.SessionStateLabel +
                    " sid=" + _coopClientCoordinator.SessionId +
                    " gen=" + _coopClientCoordinator.SessionGeneration +
                    " pending=" + _coopClientCoordinator.SnapshotPendingObjectCount +
                    " missing=" + _coopClientCoordinator.SnapshotMissingObjectCount,
                    "Net: ping=" + FormatPing(_coopClient.LastPingRttMs) +
                    " tcpRx=" + FormatAge(nowMs, _coopClient.LastTcpReceiveMs) +
                    " udpRx=" + FormatAge(nowMs, _coopClient.UdpLastReceiveMs),
                    "SceneSync: " + sceneState + " pending=" + pendingScene,
                    "Queues: doors=" + _coopClientCoordinator.PendingDoorCount +
                    " holdables=" + _coopClientCoordinator.PendingHoldableCount +
                    " ai=" + _coopClientCoordinator.PendingAiCount +
                    " npc=" + _coopClientCoordinator.PendingNpcCount,
                    "HostRx: count=" + _coopClientCoordinator.HostTransformReceiveCount +
                    " id=" + hostId +
                    " age=" + FormatAge(nowMs, _coopClientCoordinator.LastHostTransformReceiveMs),
                    "HostNet: tcp=" + _coopClient.TcpTransformCount +
                    " udp=" + _coopClient.UdpTransformCount +
                    " last=" + _coopClient.LastTransformSource,
                    "Sync: state " + _coopClient.HostStateReadCount +
                    "/" + _coopClient.HostStateEnqueuedCount +
                    "/" + _coopClientCoordinator.HostStateAppliedCount +
                    " latch " + latestSeq + ">" + consumedSeq +
                    " net=" + FormatAge(nowMs, latestNetMs),
                    "Applied: count=" + appliedCount +
                    " seq=" + appliedSeq +
                    " net=" + FormatAge(nowMs, appliedNetMs) +
                    " age=" + _coopClientCoordinator.LastHostUpdateAgeSeconds.ToString("0.0") + "s",
                    "Seq: " + sequenceLabel,
                    "Mike: " + _coopClientCoordinator.LastMikeSyncDebug,
                    _coopClientCoordinator.NpcOverlaySummary,
                    _coopClientCoordinator.BoardGameOverlaySummary,
                    _coopClientCoordinator.VoiceOverlaySummary,
                    _coopClientCoordinator.FootstepOverlaySummary,
                    BuildAvatarStatus(),
                    BuildDialogueStatus("DlgHost", _coopClientCoordinator.HostDialogueConversationId, _coopClientCoordinator.HostDialogueEntryId, _coopClientCoordinator.HostDialogueChoiceIndex, _coopClientCoordinator.HostDialogueEventMs, nowMs),
                    "DlgLocal: " + localDialogue,
                    BuildStoryStatus(_coopClientCoordinator.LastStoryEventKey, _coopClientCoordinator.LastStoryEventValue, _coopClientCoordinator.LastStoryEventMs, nowMs)
                };
            }

            return null;
        }

        private string BuildAvatarStatus()
        {
            var source = _settings.CoopRemotePlayerAvatarSource != null
                ? _settings.CoopRemotePlayerAvatarSource.Value.ToString()
                : "Auto";
            var id = _settings.CoopRemotePlayerAvatarId != null &&
                     !string.IsNullOrWhiteSpace(_settings.CoopRemotePlayerAvatarId.Value)
                ? _settings.CoopRemotePlayerAvatarId.Value.Trim()
                : "-";
            return "Avatar: " + source + " id=" + id;
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

        private static string BoolText(bool value)
        {
            return value ? "1" : "0";
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

            var pizzeria = UnityEngine.Object.FindObjectOfType<PizzeriaGameManager>();
            if (pizzeria != null)
            {
                var mike = pizzeria.mikePizzeria != null
                    ? pizzeria.mikePizzeria
                    : UnityEngine.Object.FindObjectOfType<MikePizzeria>();
                return "Pizzeria:" + pizzeria.currentPlayerState +
                       (mike != null ? " mike=" + mike.state : string.Empty);
            }

            var roadTrip = UnityEngine.Object.FindObjectOfType<RoadTripGameManager>();
            if (roadTrip != null)
            {
                var mike = UnityEngine.Object.FindObjectOfType<MikeInCar>();
                return "RoadTrip:" + (mike != null ? mike.currentConvo.ToString() : "active");
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
            _nextOverlayLogTime = now + SessionSnapshotLogIntervalSeconds;
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
            _nextStatusLogTime = now + SessionSnapshotLogIntervalSeconds;
            _sessionLog.Write("State: mode=" + modeLabel + " status=" + status + " scene=" + sceneName);
        }

        private void MaybeLogHeartbeat()
        {
            if (_sessionLog == null) return;

            var now = Time.realtimeSinceStartup;
            if (now < _nextHeartbeatLogTime) return;
            _nextHeartbeatLogTime = now + SessionSnapshotLogIntervalSeconds;

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
                    " hostRxAge=" + FormatAge(nowMs, _coopClientCoordinator.LastHostTransformReceiveMs) +
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
