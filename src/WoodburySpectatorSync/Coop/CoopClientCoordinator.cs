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
    public sealed class CoopClientCoordinator
    {
        private readonly ManualLogSource _logger;
        private readonly Settings _settings;
        private readonly CoopClient _client;
        private readonly CoopClientInteractor _interactor;
        private readonly RemoteAvatar _hostAvatar;

        private CoopClientController _controller;
        private Camera _camera;
        private bool _gameplayDisabled;
        private string _pendingScene;
        private AsyncOperation _loading;
        private bool _isLoading;
        private bool _initialCameraSet;
        private Vector3 _pendingHostPlayerPos;
        private Quaternion _pendingHostPlayerRot;
        private Vector3 _pendingHostCamPos;
        private Quaternion _pendingHostCamRot;
        private bool _hasPendingHostCam;
        private float _lastHostUpdateTime;
        private float _lastTeleportTime;
        private float _nextInputSendTime;
        private FirstPersonController _localFpc;
        private readonly Dictionary<string, DoorState> _pendingDoorStates = new Dictionary<string, DoorState>();
        private readonly Dictionary<string, HoldableState> _pendingHoldableStates = new Dictionary<string, HoldableState>();
        private readonly Dictionary<string, AiTransformState> _pendingAiStates = new Dictionary<string, AiTransformState>();

        private readonly FieldInfo _doorScriptOpened;
        private readonly MethodInfo _doorScriptOpen;
        private readonly MethodInfo _doorScriptClose;
        private readonly FieldInfo _playerFirstPersonField;

        public CoopClientCoordinator(ManualLogSource logger, Settings settings, CoopClient client)
        {
            _logger = logger;
            _settings = settings;
            _client = client;
            _interactor = new CoopClientInteractor(client);
            _hostAvatar = new RemoteAvatar("CoopHostAvatar", new Color(1f, 0.2f, 0.2f, 0.8f));

            var doorType = typeof(NOTLonely_Door.DoorScript);
            _doorScriptOpened = doorType.GetField("Opened", BindingFlags.Instance | BindingFlags.NonPublic);
            _doorScriptOpen = doorType.GetMethod("OpenDoor", BindingFlags.Instance | BindingFlags.NonPublic);
            _doorScriptClose = doorType.GetMethod("CloseDoor", BindingFlags.Instance | BindingFlags.NonPublic);
            _playerFirstPersonField = typeof(PlayerController).GetField("firstPersonController", BindingFlags.Instance | BindingFlags.NonPublic);

            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        public void Shutdown()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            _hostAvatar.SetActive(false);
        }

        public void Update()
        {
            if (!_client.IsConnected) return;

            if (!_settings.CoopUseLocalPlayer.Value)
            {
                EnsureCamera();
            }
            else
            {
                EnsureLocalPlayerRefs();
            }
            DisableLocalGameplay();
            _controller?.Update();
            _interactor.Update(GetActiveCamera());

            SendPlayerTransform();
            SendInputState();
            DrainIncoming();
            UpdateSceneLoad();
            ApplyPendingStates();
            MaybeTeleportToHost();

            if (_settings.CoopSnapToHostOnSceneLoad.Value && !_isLoading && !_initialCameraSet && GetActiveCamera() != null && _hasPendingHostCam)
            {
                TeleportToHost();
                _initialCameraSet = true;
                _hasPendingHostCam = false;
            }
        }

        private void EnsureCamera()
        {
            if (_camera != null) return;

            var existingCameras = UnityEngine.Object.FindObjectsOfType<Camera>();
            foreach (var cam in existingCameras)
            {
                cam.enabled = false;
            }

            var go = new GameObject("CoopClientCamera");
            _camera = go.AddComponent<Camera>();
            go.tag = "MainCamera";
            _controller = new CoopClientController(_camera);

            var audio = _camera.GetComponent<AudioListener>();
            if (audio != null)
            {
                audio.enabled = false;
            }
        }

        private void DisableLocalGameplay()
        {
            if (_gameplayDisabled) return;

            var inputManagers = UnityEngine.Object.FindObjectsOfType<InputManager>();
            foreach (var input in inputManagers)
            {
                input.enabled = false;
            }

            var interactUis = UnityEngine.Object.FindObjectsOfType<interactableObjectUI>();
            foreach (var ui in interactUis)
            {
                ui.enabled = false;
            }

            if (!_settings.CoopUseLocalPlayer.Value)
            {
                var players = UnityEngine.Object.FindObjectsOfType<PlayerController>();
                foreach (var player in players)
                {
                    player.enabled = false;
                    var fpc = _playerFirstPersonField != null
                        ? _playerFirstPersonField.GetValue(player) as FirstPersonController
                        : null;
                    if (fpc != null)
                    {
                        fpc.enabled = false;
                        if (fpc.characterController != null)
                        {
                            fpc.characterController.enabled = false;
                        }
                    }
                }
            }

            _gameplayDisabled = true;
        }

        private void SendPlayerTransform()
        {
            var camera = GetActiveCamera();
            if (camera == null) return;

            Vector3 position;
            Quaternion rotation;
            if (_settings.CoopUseLocalPlayer.Value && _localFpc != null)
            {
                position = _localFpc.transform.position;
                rotation = _localFpc.transform.rotation;
            }
            else
            {
                position = camera.transform.position;
                rotation = camera.transform.rotation;
            }

            var state = new PlayerTransformState
            {
                PlayerId = 1,
                Position = position,
                Rotation = rotation,
                CameraPosition = camera.transform.position,
                CameraRotation = camera.transform.rotation
            };

            _client.Enqueue(new PlayerTransformMessage(state));
        }

        private void SendInputState()
        {
            var now = Time.realtimeSinceStartup;
            var interval = 1f / Mathf.Max(1f, _settings.SendHz.Value);
            if (now < _nextInputSendTime) return;
            _nextInputSendTime = now + interval;

            var cam = GetActiveCamera();
            if (cam == null) return;

            var euler = cam.transform.rotation.eulerAngles;
            var state = new PlayerInputState
            {
                PlayerId = 1,
                MoveX = Input.GetAxisRaw("Horizontal"),
                MoveY = Input.GetAxisRaw("Vertical"),
                LookYaw = euler.y,
                LookPitch = euler.x,
                Jump = Input.GetKey(KeyCode.Space),
                Crouch = Input.GetKey(KeyCode.LeftControl),
                Sprint = Input.GetKey(KeyCode.LeftShift)
            };

            _client.Enqueue(new PlayerInputMessage(state));
        }

        private void DrainIncoming()
        {
            while (_client.TryDequeue(out var message))
            {
                if (message is PlayerTransformMessage player)
                {
                    if (player.State.PlayerId == 0)
                    {
                        _hostAvatar.SetActive(true);
                        _hostAvatar.Root.position = player.State.Position;
                        _hostAvatar.Root.rotation = player.State.Rotation;

                        _pendingHostPlayerPos = player.State.Position;
                        _pendingHostPlayerRot = player.State.Rotation;
                        _pendingHostCamPos = player.State.CameraPosition;
                        _pendingHostCamRot = player.State.CameraRotation;
                        _hasPendingHostCam = true;
                        _lastHostUpdateTime = Time.realtimeSinceStartup;
                    }
                }
                else if (message is DoorStateMessage door)
                {
                    ApplyDoorState(door.State);
                }
                else if (message is HoldableStateMessage holdable)
                {
                    ApplyHoldableState(holdable.State);
                }
                else if (message is StoryFlagMessage flag)
                {
                    PlayerPrefs.SetInt(flag.Key, flag.Value);
                }
                else if (message is AiTransformMessage ai)
                {
                    ApplyAiState(ai.State);
                }
                else if (message is SceneChangeMessage scene)
                {
                    RequestSceneLoad(scene.SceneName);
                }
            }
        }

        private void RequestSceneLoad(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            if (sceneName == SceneManager.GetActiveScene().name) return;
            _pendingScene = sceneName;
        }

        private void UpdateSceneLoad()
        {
            if (_isLoading)
            {
                if (_loading != null && _loading.isDone)
                {
                    _isLoading = false;
                    _loading = null;
                }
                return;
            }

            if (!string.IsNullOrEmpty(_pendingScene))
            {
                _isLoading = true;
                _loading = SceneManager.LoadSceneAsync(_pendingScene);
                _pendingScene = null;
            }
        }

        private void ApplyDoorState(DoorState state)
        {
            if (_isLoading || !TryApplyDoorState(state))
            {
                _pendingDoorStates[state.Path] = state;
                return;
            }

            _pendingDoorStates.Remove(state.Path);
        }

        private void ApplyHoldableState(HoldableState state)
        {
            if (_isLoading || !TryApplyHoldableState(state))
            {
                _pendingHoldableStates[state.Path] = state;
                return;
            }

            _pendingHoldableStates.Remove(state.Path);
        }

        private void ApplyAiState(AiTransformState state)
        {
            if (_isLoading || !TryApplyAiState(state))
            {
                _pendingAiStates[state.Path] = state;
                return;
            }

            _pendingAiStates.Remove(state.Path);
        }

        private bool TryApplyDoorState(DoorState state)
        {
            var target = NetPath.FindByPath(state.Path);
            if (target == null) return false;

            if (state.DoorType == 0)
            {
                var door = target.GetComponent<CabinDoor>();
                if (door == null) return false;
                door.isLocked = state.IsLocked;
                if (state.IsOpen && !door.IsOpen)
                {
                    door.OpenDoor();
                }
                else if (!state.IsOpen && door.IsOpen)
                {
                    door.CloseDoor();
                }
            }
            else if (state.DoorType == 1)
            {
                var door = target.GetComponent<NOTLonely_Door.DoorScript>();
                if (door == null) return false;

                if (_doorScriptOpened != null)
                {
                    var opened = (bool)_doorScriptOpened.GetValue(door);
                    if (state.IsOpen && !opened)
                    {
                        _doorScriptOpen?.Invoke(door, null);
                    }
                    else if (!state.IsOpen && opened)
                    {
                        _doorScriptClose?.Invoke(door, null);
                    }
                }
            }

            return true;
        }

        private bool TryApplyHoldableState(HoldableState state)
        {
            var target = NetPath.FindByPath(state.Path);
            if (target == null) return false;

            target.gameObject.SetActive(state.Active);
            target.position = state.Position;
            target.rotation = state.Rotation;
            return true;
        }

        private bool TryApplyAiState(AiTransformState state)
        {
            var target = NetPath.FindByPath(state.Path);
            if (target == null) return false;

            var agent = target.GetComponent<NavmeshPathAgent>();
            if (agent != null)
            {
                agent.enabled = false;
            }

            target.gameObject.SetActive(state.Active);
            target.position = state.Position;
            target.rotation = state.Rotation;
            return true;
        }

        private void ApplyPendingStates()
        {
            if (_isLoading) return;

            if (_pendingDoorStates.Count > 0)
            {
                var keys = new List<string>(_pendingDoorStates.Keys);
                foreach (var key in keys)
                {
                    if (TryApplyDoorState(_pendingDoorStates[key]))
                    {
                        _pendingDoorStates.Remove(key);
                    }
                }
            }

            if (_pendingHoldableStates.Count > 0)
            {
                var keys = new List<string>(_pendingHoldableStates.Keys);
                foreach (var key in keys)
                {
                    if (TryApplyHoldableState(_pendingHoldableStates[key]))
                    {
                        _pendingHoldableStates.Remove(key);
                    }
                }
            }

            if (_pendingAiStates.Count > 0)
            {
                var keys = new List<string>(_pendingAiStates.Keys);
                foreach (var key in keys)
                {
                    if (TryApplyAiState(_pendingAiStates[key]))
                    {
                        _pendingAiStates.Remove(key);
                    }
                }
            }
        }

        private void MaybeTeleportToHost()
        {
            if (!_hasPendingHostCam) return;
            if (_settings.CoopTeleportDistance.Value <= 0f) return;
            if (_isLoading) return;

            var now = Time.realtimeSinceStartup;
            if (now - _lastTeleportTime < Mathf.Max(0.1f, _settings.CoopTeleportCooldownSeconds.Value)) return;

            var distance = 0f;
            if (_settings.CoopUseLocalPlayer.Value && _localFpc != null)
            {
                distance = Vector3.Distance(_localFpc.transform.position, _pendingHostPlayerPos);
            }
            else
            {
                var activeCamera = GetActiveCamera();
                if (activeCamera == null) return;
                distance = Vector3.Distance(activeCamera.transform.position, _pendingHostCamPos);
            }
            var stale = now - _lastHostUpdateTime > Mathf.Max(1f, _settings.CoopTeleportStaleSeconds.Value);

            if (distance >= _settings.CoopTeleportDistance.Value || stale)
            {
                TeleportToHost();
                _lastTeleportTime = now;
            }
        }

        private void TeleportToHost()
        {
            if (_settings.CoopUseLocalPlayer.Value && _localFpc != null)
            {
                var controller = _localFpc.characterController;
                if (controller != null) controller.enabled = false;

                _localFpc.transform.SetPositionAndRotation(_pendingHostPlayerPos, _pendingHostPlayerRot);
                if (_localFpc.playerCamera != null)
                {
                    _localFpc.playerCamera.transform.rotation = _pendingHostCamRot;
                }

                if (controller != null) controller.enabled = true;
            }
            else if (_camera != null)
            {
                _camera.transform.SetPositionAndRotation(_pendingHostCamPos, _pendingHostCamRot);
            }
        }

        private Camera GetActiveCamera()
        {
            if (_settings.CoopUseLocalPlayer.Value)
            {
                if (_localFpc != null && _localFpc.playerCamera != null) return _localFpc.playerCamera;
                return Camera.main;
            }

            return _camera;
        }

        private void EnsureLocalPlayerRefs()
        {
            if (_localFpc != null) return;

            var player = PlayerController.GetInstance();
            if (player == null) return;

            _localFpc = _playerFirstPersonField != null
                ? _playerFirstPersonField.GetValue(player) as FirstPersonController
                : null;
        }

        private void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            _camera = null;
            _controller = null;
            _gameplayDisabled = false;
            _pendingScene = null;
            _loading = null;
            _isLoading = false;
            _initialCameraSet = false;
            _hasPendingHostCam = false;
            _pendingHostPlayerPos = Vector3.zero;
            _pendingHostPlayerRot = Quaternion.identity;
            _lastHostUpdateTime = Time.realtimeSinceStartup;
            _lastTeleportTime = 0f;
            _nextInputSendTime = 0f;
            _localFpc = null;
            _pendingDoorStates.Clear();
            _pendingHoldableStates.Clear();
            _pendingAiStates.Clear();
        }
    }
}
