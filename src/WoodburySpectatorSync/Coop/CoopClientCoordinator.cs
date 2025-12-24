using System;
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
        private Vector3 _pendingHostCamPos;
        private Quaternion _pendingHostCamRot;
        private bool _hasPendingHostCam;

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

            EnsureCamera();
            DisableLocalGameplay();
            _controller?.Update();
            _interactor.Update(_camera);

            SendPlayerTransform();
            DrainIncoming();
            UpdateSceneLoad();

            if (!_initialCameraSet && _camera != null && _hasPendingHostCam)
            {
                _camera.transform.SetPositionAndRotation(_pendingHostCamPos, _pendingHostCamRot);
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

            _gameplayDisabled = true;
        }

        private void SendPlayerTransform()
        {
            var state = new PlayerTransformState
            {
                PlayerId = 1,
                Position = _camera.transform.position,
                Rotation = _camera.transform.rotation,
                CameraPosition = _camera.transform.position,
                CameraRotation = _camera.transform.rotation
            };

            _client.Enqueue(new PlayerTransformMessage(state));
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

                        _pendingHostCamPos = player.State.CameraPosition;
                        _pendingHostCamRot = player.State.CameraRotation;
                        _hasPendingHostCam = true;
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
            var target = NetPath.FindByPath(state.Path);
            if (target == null) return;

            if (state.DoorType == 0)
            {
                var door = target.GetComponent<CabinDoor>();
                if (door == null) return;
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
                if (door == null) return;

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
        }

        private void ApplyHoldableState(HoldableState state)
        {
            var target = NetPath.FindByPath(state.Path);
            if (target == null) return;

            target.gameObject.SetActive(state.Active);
            target.position = state.Position;
            target.rotation = state.Rotation;
        }

        private void ApplyAiState(AiTransformState state)
        {
            var target = NetPath.FindByPath(state.Path);
            if (target == null) return;

            var agent = target.GetComponent<NavmeshPathAgent>();
            if (agent != null)
            {
                agent.enabled = false;
            }

            target.gameObject.SetActive(state.Active);
            target.position = state.Position;
            target.rotation = state.Rotation;
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
        }
    }
}
