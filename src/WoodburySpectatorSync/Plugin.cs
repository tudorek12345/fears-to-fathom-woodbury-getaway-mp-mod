using System;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using WoodburySpectatorSync.Coop;
using WoodburySpectatorSync.Config;
using WoodburySpectatorSync.Net;
using WoodburySpectatorSync.Sync;
using WoodburySpectatorSync.UI;

namespace WoodburySpectatorSync
{
    // TODO (IL2CPP): Swap to BepInEx IL2CPP chainloader and update project references.
    [BepInPlugin("com.woodbury.spectatorsync", "Woodbury Spectator Sync", "0.2.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private Settings _settings;
        private HostServer _hostServer;
        private SpectatorClient _spectatorClient;
        private CameraFollower _cameraFollower;
        private SceneSync _sceneSync;
        private Overlay _overlay;
        private CoopServer _coopServer;
        private CoopClient _coopClient;
        private CoopHostCoordinator _coopHost;
        private CoopClientCoordinator _coopClientCoordinator;

        private string _currentProgressMarker = string.Empty;
        private int _progressNoteIndex;
        private readonly string[] _progressNotes = new[] { "checkpoint", "cutscene", "door", "phone", "misc" };

        private void Awake()
        {
            _settings = Settings.Bind(Config);
            _overlay = new Overlay(_settings);
            _cameraFollower = new CameraFollower(_settings);
            _sceneSync = new SceneSync();
            _hostServer = new HostServer(Logger, _settings);
            _spectatorClient = new SpectatorClient(Logger, _settings);
            _coopServer = new CoopServer(Logger, _settings);
            _coopClient = new CoopClient(Logger, _settings);
            _coopHost = new CoopHostCoordinator(Logger, _settings, _coopServer);
            _coopClientCoordinator = new CoopClientCoordinator(Logger, _settings, _coopClient);

            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            Logger.LogInfo("Woodbury Spectator Sync loaded");
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
        }

        private void Update()
        {
            HandleHotkeys();

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
            _overlay.Draw(modeLabel, status, SceneManager.GetActiveScene().name);
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
                }
            }
            else if (_settings.ModeSetting.Value == Mode.CoopHost)
            {
                if (Input.GetKeyDown(KeyCode.F6))
                {
                    if (_coopServer.IsRunning) _coopServer.Stop(); else _coopServer.Start();
                }
            }
            else if (_settings.ModeSetting.Value == Mode.CoopClient)
            {
                if (Input.GetKeyDown(KeyCode.F7))
                {
                    if (_coopClient.IsConnected) _coopClient.Disconnect(); else _coopClient.Connect();
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
                _hostServer.QueueSceneChange(newScene.name);
            }
        }

        private void CycleProgressMarker()
        {
            var note = _progressNotes[_progressNoteIndex % _progressNotes.Length];
            _progressNoteIndex++;
            var marker = DateTime.Now.ToString("HH:mm:ss") + " - " + note;
            _currentProgressMarker = marker;
            _overlay.SetProgressMarker(marker);
            if (_hostServer.IsRunning)
            {
                _hostServer.QueueProgressMarker(marker);
            }
        }
    }
}
