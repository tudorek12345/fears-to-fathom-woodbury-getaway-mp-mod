using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using WoodburySpectatorSync.Config;
using WoodburySpectatorSync.Net;

namespace WoodburySpectatorSync.Coop
{
    public sealed class CoopHostCoordinator
    {
        private readonly ManualLogSource _logger;
        private readonly Settings _settings;
        private readonly CoopServer _server;
        private readonly RemoteAvatar _clientAvatar;
        private RemotePlayerProxy _remotePlayer;
        private PlayerInputState _lastInputState;
        private bool _hasInputState;
        private readonly Dictionary<string, DoorState> _doorStates = new Dictionary<string, DoorState>();
        private readonly Dictionary<string, HoldableState> _holdableStates = new Dictionary<string, HoldableState>();
        private readonly Dictionary<string, int> _storyFlags = new Dictionary<string, int>();
        private readonly Dictionary<string, AiTransformState> _aiStates = new Dictionary<string, AiTransformState>();

        private CabinDoor[] _cabinDoors = new CabinDoor[0];
        private NOTLonely_Door.DoorScript[] _doorScripts = new NOTLonely_Door.DoorScript[0];
        private Holdable[] _holdables = new Holdable[0];
        private NavmeshPathAgent[] _aiAgents = new NavmeshPathAgent[0];

        private readonly FieldInfo _doorScriptOpened;
        private readonly MethodInfo _doorScriptOpen;
        private readonly MethodInfo _doorScriptClose;
        private readonly FieldInfo _playerFirstPersonField;

        private long _nextPlayerSendMs;
        private long _nextWorldSendMs;
        private long _nextStorySendMs;
        private long _nextFullSyncMs;
        private bool _lastClientConnected;
        private string _lastSceneName = string.Empty;

        private readonly string[] _storyKeys = new[]
        {
            PlayerPrefKeys.MOE_PIZZA,
            PlayerPrefKeys.WELCOME_TO_WOODBURY,
            PlayerPrefKeys.FISHING,
            PlayerPrefKeys.BOARD_GAME,
            PlayerPrefKeys.HIDE_SEEK,
            PlayerPrefKeys.MIDNIGHT,
            PlayerPrefKeys.SOMEONE_AT_DOOR,
            PlayerPrefKeys.RICK,
            PlayerPrefKeys.BASEMENT,
            PlayerPrefKeys.FROM_MENU,
            PlayerPrefKeys.EATING_SOUNDS,
            PlayerPrefKeys.TOILET_SOUNDS,
            PlayerPrefKeys.START_SEQ
        };

        public CoopHostCoordinator(ManualLogSource logger, Settings settings, CoopServer server)
        {
            _logger = logger;
            _settings = settings;
            _server = server;
            _clientAvatar = new RemoteAvatar("CoopClientAvatar", new Color(0.2f, 0.7f, 1f, 0.8f));

            var doorType = typeof(NOTLonely_Door.DoorScript);
            _doorScriptOpened = doorType.GetField("Opened", BindingFlags.Instance | BindingFlags.NonPublic);
            _doorScriptOpen = doorType.GetMethod("OpenDoor", BindingFlags.Instance | BindingFlags.NonPublic);
            _doorScriptClose = doorType.GetMethod("CloseDoor", BindingFlags.Instance | BindingFlags.NonPublic);
            _playerFirstPersonField = typeof(PlayerController).GetField("firstPersonController", BindingFlags.Instance | BindingFlags.NonPublic);

            SceneManager.activeSceneChanged += OnSceneChanged;
            CacheSceneObjects();
        }

        public void Shutdown()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            _clientAvatar.SetActive(false);
            if (_remotePlayer != null && _remotePlayer.Root != null)
            {
                UnityEngine.Object.Destroy(_remotePlayer.Root.gameObject);
            }
        }

        public void Update()
        {
            if (!_server.IsRunning) return;

            if (_server.IsClientConnected && !_lastClientConnected)
            {
                _lastClientConnected = true;
                var sceneName = SceneManager.GetActiveScene().name;
                _lastSceneName = sceneName;
                _server.Enqueue(new SceneChangeMessage(sceneName));
                SendFullState(force: true);
                _nextFullSyncMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 5000;
                SnapRemotePlayerToHost();
                _logger.LogInfo("Co-op client connected, sent initial state");
            }
            else if (!_server.IsClientConnected && _lastClientConnected)
            {
                _lastClientConnected = false;
                if (_remotePlayer != null)
                {
                    _remotePlayer.SetActive(false);
                }
                _clientAvatar.SetActive(false);
            }

            EnsureRemotePlayer();
            DrainIncoming();

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (nowMs >= _nextPlayerSendMs)
            {
                SendPlayerTransform();
                _nextPlayerSendMs = nowMs + (long)Math.Max(1, 1000.0 / Math.Max(1, _settings.SendHz.Value));
            }

            if (nowMs >= _nextWorldSendMs)
            {
                SendDoorStates();
                SendHoldableStates();
                SendAiStates();
                _nextWorldSendMs = nowMs + 200;
            }

            if (nowMs >= _nextStorySendMs)
            {
                SendStoryFlags();
                _nextStorySendMs = nowMs + 1000;
            }

            if (_server.IsClientConnected && nowMs >= _nextFullSyncMs)
            {
                SendFullState(force: true);
                _nextFullSyncMs = nowMs + 5000;
            }
        }

        public void SetClientAvatar(PlayerTransformState state)
        {
            if (_remotePlayer != null && _remotePlayer.Root != null)
            {
                _remotePlayer.SetActive(true);
                _remotePlayer.ApplyTransform(state);
                _clientAvatar.SetActive(false);
            }
            else
            {
                _clientAvatar.SetActive(true);
                _clientAvatar.Root.position = state.Position;
                _clientAvatar.Root.rotation = state.Rotation;
            }
        }

        private void DrainIncoming()
        {
            while (_server.TryDequeueIncoming(out var message))
            {
                if (message is PlayerTransformMessage playerTransform)
                {
                    SetClientAvatar(playerTransform.State);
                }
                else if (message is InteractRequestMessage interact)
                {
                    HandleInteract(interact);
                }
                else if (message is PlayerInputMessage input)
                {
                    _lastInputState = input.State;
                    _hasInputState = true;
                    _remotePlayer?.ApplyInput(input.State);
                }
            }
        }

        private void HandleInteract(InteractRequestMessage interact)
        {
            var target = NetPath.FindByPath(interact.TargetPath);
            if (target == null) return;

            var source = _remotePlayer != null && _remotePlayer.Root != null
                ? _remotePlayer.Root.position
                : _clientAvatar.Root.position;
            var distance = Vector3.Distance(source, target.position);
            if (distance > 3.5f)
            {
                return;
            }

            var interactable = target.GetComponent<Iinteractable>();
            if (interactable != null)
            {
                try
                {
                    interactable.Clicked(null);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Co-op interact failed: " + ex.Message);
                }
            }
        }

        private void SendPlayerTransform()
        {
            var playerController = PlayerController.GetInstance();
            if (playerController == null) return;

            var fps = _playerFirstPersonField != null
                ? _playerFirstPersonField.GetValue(playerController) as FirstPersonController
                : null;
            if (fps == null) return;

            var camera = fps.playerCamera != null ? fps.playerCamera : Camera.main;
            if (camera == null) return;

            var state = new PlayerTransformState
            {
                PlayerId = 0,
                Position = fps.transform.position,
                Rotation = fps.transform.rotation,
                CameraPosition = camera.transform.position,
                CameraRotation = camera.transform.rotation
            };

            _server.Enqueue(new PlayerTransformMessage(state));
        }

        private void SendDoorStates()
        {
            foreach (var door in _cabinDoors)
            {
                if (door == null) continue;
                var state = new DoorState
                {
                    Path = NetPath.GetPath(door.transform),
                    DoorType = 0,
                    IsOpen = door.IsOpen,
                    IsLocked = door.isLocked
                };
                SendDoorIfChanged(state);
            }

            foreach (var door in _doorScripts)
            {
                if (door == null) continue;
                var opened = _doorScriptOpened != null && (bool)_doorScriptOpened.GetValue(door);
                var state = new DoorState
                {
                    Path = NetPath.GetPath(door.transform),
                    DoorType = 1,
                    IsOpen = opened,
                    IsLocked = door.keySystem != null && door.keySystem.enabled && !door.keySystem.isUnlock
                };
                SendDoorIfChanged(state);
            }
        }

        private void SendDoorIfChanged(DoorState state)
        {
            if (!_doorStates.TryGetValue(state.Path, out var last) ||
                last.IsOpen != state.IsOpen || last.IsLocked != state.IsLocked)
            {
                _doorStates[state.Path] = state;
                _server.Enqueue(new DoorStateMessage(state));
            }
        }

        private void SendHoldableStates()
        {
            foreach (var holdable in _holdables)
            {
                if (holdable == null) continue;
                var state = new HoldableState
                {
                    Path = NetPath.GetPath(holdable.transform),
                    Holder = 0,
                    Position = holdable.transform.position,
                    Rotation = holdable.transform.rotation,
                    Active = holdable.gameObject.activeInHierarchy
                };

                if (!_holdableStates.TryGetValue(state.Path, out var last) ||
                    last.Active != state.Active ||
                    Vector3.Distance(last.Position, state.Position) > 0.01f ||
                    Quaternion.Angle(last.Rotation, state.Rotation) > 0.5f)
                {
                    _holdableStates[state.Path] = state;
                    _server.Enqueue(new HoldableStateMessage(state));
                }
            }
        }

        private void SendStoryFlags()
        {
            foreach (var key in _storyKeys)
            {
                var value = PlayerPrefs.GetInt(key, 0);
                if (!_storyFlags.TryGetValue(key, out var last) || last != value)
                {
                    _storyFlags[key] = value;
                    _server.Enqueue(new StoryFlagMessage(key, value));
                }
            }
        }

        private void SendAiStates()
        {
            foreach (var agent in _aiAgents)
            {
                if (agent == null) continue;
                var state = new AiTransformState
                {
                    Path = NetPath.GetPath(agent.transform),
                    Position = agent.transform.position,
                    Rotation = agent.transform.rotation,
                    Active = agent.gameObject.activeInHierarchy
                };
                if (!_aiStates.TryGetValue(state.Path, out var last) ||
                    last.Active != state.Active ||
                    Vector3.Distance(last.Position, state.Position) > 0.05f ||
                    Quaternion.Angle(last.Rotation, state.Rotation) > 1f)
                {
                    _aiStates[state.Path] = state;
                    _server.Enqueue(new AiTransformMessage(state));
                }
            }
        }

        private void SendFullState(bool force)
        {
            if (force)
            {
                _doorStates.Clear();
                _holdableStates.Clear();
                _aiStates.Clear();
                _storyFlags.Clear();
            }

            SendDoorStates();
            SendHoldableStates();
            SendStoryFlags();
            SendAiStates();
        }

        private void CacheSceneObjects()
        {
            _cabinDoors = UnityEngine.Object.FindObjectsOfType<CabinDoor>();
            _doorScripts = UnityEngine.Object.FindObjectsOfType<NOTLonely_Door.DoorScript>();
            _holdables = UnityEngine.Object.FindObjectsOfType<Holdable>();
            _aiAgents = UnityEngine.Object.FindObjectsOfType<NavmeshPathAgent>();
        }

        private void EnsureRemotePlayer()
        {
            if (_remotePlayer != null && _remotePlayer.Root != null) return;

            var playerController = PlayerController.GetInstance();
            if (playerController == null) return;

            var fps = _playerFirstPersonField != null
                ? _playerFirstPersonField.GetValue(playerController) as FirstPersonController
                : null;
            if (fps == null) return;

            _remotePlayer = new RemotePlayerProxy(fps, new Color(0.2f, 0.7f, 1f, 0.8f));
            _remotePlayer.SetActive(_server.IsClientConnected);

            if (_hasInputState)
            {
                _remotePlayer.ApplyInput(_lastInputState);
            }
        }

        private void SnapRemotePlayerToHost()
        {
            EnsureRemotePlayer();
            if (_remotePlayer == null || _remotePlayer.Root == null) return;

            var playerController = PlayerController.GetInstance();
            if (playerController == null) return;

            var fps = _playerFirstPersonField != null
                ? _playerFirstPersonField.GetValue(playerController) as FirstPersonController
                : null;
            if (fps == null) return;

            var camera = fps.playerCamera != null ? fps.playerCamera : Camera.main;
            if (camera == null) return;

            var state = new PlayerTransformState
            {
                PlayerId = 1,
                Position = fps.transform.position,
                Rotation = fps.transform.rotation,
                CameraPosition = camera.transform.position,
                CameraRotation = camera.transform.rotation
            };
            _remotePlayer.ApplyTransform(state);
        }

        private void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            CacheSceneObjects();
            _doorStates.Clear();
            _holdableStates.Clear();
            _aiStates.Clear();
            _storyFlags.Clear();

            if (_remotePlayer != null && _remotePlayer.Root != null)
            {
                UnityEngine.Object.Destroy(_remotePlayer.Root.gameObject);
            }
            _remotePlayer = null;
            _lastSceneName = newScene.name;
            if (_server.IsRunning && _server.IsClientConnected)
            {
                _server.Enqueue(new SceneChangeMessage(_lastSceneName));
                SendFullState(force: true);
                _nextFullSyncMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 5000;
            }
        }
    }
}
