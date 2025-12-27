using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using BepInEx.Logging;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.AI;
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
        private RemoteAvatar _hostAvatar;
        private readonly Action<string> _sessionLogWrite;

        private CoopClientController _controller;
        private Camera _camera;
        private bool _gameplayDisabled;
        private bool _aiDisabled;
        private string _pendingScene;
        private int _pendingSceneIndex = -1;
        private int _pendingStartSeq = -1;
        private AsyncOperation _loading;
        private bool _isLoading;
        private bool _initialCameraSet;
        private Vector3 _pendingHostPlayerPos;
        private Quaternion _pendingHostPlayerRot;
        private Vector3 _pendingHostCamPos;
        private Quaternion _pendingHostCamRot;
        private bool _hasPendingHostCam;
        private string _lastSceneReadySent = string.Empty;
        private float _lastSceneReadySentTime;
        private float _nextPingTime;
        private long _lastHostAppliedTick;
        private long _lastHostTransformReceiveMs;
        private int _hostTransformReceiveCount;
        private float _lastTeleportTime;
        private float _nextInputSendTime;
        private FirstPersonController _localFpc;
        private RemotePlayerProxy _hostProxy;
        private readonly Dictionary<string, DoorState> _pendingDoorStates = new Dictionary<string, DoorState>();
        private readonly Dictionary<string, HoldableState> _pendingHoldableStates = new Dictionary<string, HoldableState>();
        private readonly Dictionary<string, AiTransformState> _pendingAiStates = new Dictionary<string, AiTransformState>();
        private float _loadingStartTime;
        private float _loadingLastProgress;
        private float _loadingLastProgressTime;
        private string _loadingSceneName;
        private int _loadingSceneIndex = -1;
        private const float LoadingTimeoutSeconds = 40f;
        private const float LoadingStallSeconds = 6f;
        private const int MaxMessagesPerFrame = 200;
        private const int MaxTransformsPerFrame = 60;
        private bool _forcedCabinSpawn;
        private bool _cabinPrefsPrepared;
        private bool _dialogueUiDisabled;
        private string _remoteDialogueText = string.Empty;
        private string _remoteDialogueSpeaker = string.Empty;
        private float _remoteDialogueExpiresAt;
        private float _remoteDialogueMinClearAt;
        private byte _remoteDialogueKind;
        private int _hostDialogueConversationId = -1;
        private int _hostDialogueEntryId = -1;
        private int _hostDialogueChoiceIndex = -1;
        private long _hostDialogueEventMs;
        private string _lastStoryEventKey = string.Empty;
        private int _lastStoryEventValue;
        private long _lastStoryEventMs;
        private float _nextDialogueDriftCheckTime;
        private byte _lastHostPlayerId = 255;
        private long _lastAppliedHostNetMs;
        private long _hostStateAppliedCount;
        private int _lastAppliedHostTransformSeq;
        private long _hostTransformAppliedCount;
        private float _nextHostTransformApplyLogTime;
        private float _nextHostTransformForceLogTime;
        private float _nextHostTransformErrorLogTime;
        private float _nextDialogueUnlockTime;
        private readonly HashSet<int> _disabledTriggerBehaviours = new HashSet<int>();
        private readonly HashSet<int> _disabledTriggerColliders = new HashSet<int>();
        private readonly HashSet<int> _disabledInteractables = new HashSet<int>();
        private const float MinDialogueDisplaySeconds = 1.5f;
        private static readonly HashSet<string> TriggerScriptNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ATMTrigger",
            "AtticDoorTrigger",
            "CabinTrafficTrigger",
            "DoubleSidedTriggerDoor",
            "DoorTrigger",
            "FellOffTrigger",
            "OnTrigger",
            "OnTriggerDisplaySub",
            "OnTriggerMouseActions",
            "OnTriggerSub",
            "OnTriggerOfficeBoundsSub",
            "PlayerInsideTrigger",
            "RoadTripTrafficTrigger",
            "SlotTrigger",
            "TriggerDoorAmb",
            "TriggerActionOnInteract",
            "TriggerEventInvoker",
            "TrafficTrigger",
            "TriggerEventOnInteract",
            "ConversationTrigger",
            "BarkTrigger"
        };

        private readonly FieldInfo _doorScriptOpened;
        private readonly MethodInfo _doorScriptOpen;
        private readonly MethodInfo _doorScriptClose;
        private readonly FieldInfo _playerFirstPersonField;

        public CoopClientCoordinator(ManualLogSource logger, Settings settings, CoopClient client, Action<string> sessionLogWrite = null)
        {
            _logger = logger;
            _settings = settings;
            _client = client;
            _sessionLogWrite = sessionLogWrite;
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
            if (_hostProxy != null && _hostProxy.Root != null)
            {
                UnityEngine.Object.Destroy(_hostProxy.Root.gameObject);
            }
            _hostProxy = null;
        }

        public bool IsSceneLoading => _isLoading;
        public string PendingSceneName => _pendingScene;
        public int PendingSceneIndex => _pendingSceneIndex;
        public int PendingDoorCount => _pendingDoorStates.Count;
        public int PendingHoldableCount => _pendingHoldableStates.Count;
        public int PendingAiCount => _pendingAiStates.Count;
        public float LastHostUpdateAgeSeconds => GetHostAppliedAgeSeconds();
        public long LastHostTransformReceiveMs => _lastHostTransformReceiveMs;
        public int HostTransformReceiveCount => _hostTransformReceiveCount;
        public long HostStateAppliedCount => Interlocked.Read(ref _hostStateAppliedCount);
        public long HostTransformAppliedCount => Interlocked.Read(ref _hostTransformAppliedCount);
        public int LastAppliedHostTransformSeq => _lastAppliedHostTransformSeq;
        public long LastAppliedHostNetMs => _lastAppliedHostNetMs;
        public bool HasPendingHostCamera => _hasPendingHostCam;
        public float LoadingProgress => _loading != null ? _loading.progress : (_isLoading ? 0f : 1f);
        public int HostDialogueConversationId => _hostDialogueConversationId;
        public int HostDialogueEntryId => _hostDialogueEntryId;
        public int HostDialogueChoiceIndex => _hostDialogueChoiceIndex;
        public long HostDialogueEventMs => _hostDialogueEventMs;
        public string LastStoryEventKey => _lastStoryEventKey;
        public int LastStoryEventValue => _lastStoryEventValue;
        public long LastStoryEventMs => _lastStoryEventMs;
        public byte LastHostPlayerId => _lastHostPlayerId;

        public bool TryGetRemoteDialogue(out string speaker, out string text, out byte kind)
        {
            if (string.IsNullOrEmpty(_remoteDialogueText))
            {
                speaker = string.Empty;
                text = string.Empty;
                kind = 0;
                return false;
            }

            speaker = _remoteDialogueSpeaker;
            text = _remoteDialogueText;
            kind = _remoteDialogueKind;
            return true;
        }

        public void Update()
        {
            if (!_client.IsConnected)
            {
                Interlocked.Exchange(ref _lastHostAppliedTick, 0);
                _lastHostTransformReceiveMs = 0;
                _hostTransformReceiveCount = 0;
                Interlocked.Exchange(ref _hostTransformAppliedCount, 0);
                _lastAppliedHostTransformSeq = 0;
                _lastAppliedHostNetMs = 0;
                return;
            }

            DrainIncoming();
            ForceApplyLatchedTransform();
            UpdateHostTransformHeartbeat();
            UpdateSceneLoad();
            SendSceneReadyIfNeeded();
            UpdateRemoteDialogue();
            CheckDialogueDrift();
            if (_isLoading)
            {
                SendPingIfDue();
                return;
            }

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
            if (_settings.CoopRouteInteractions.Value || !_settings.CoopUseLocalPlayer.Value)
            {
                _interactor.Update(GetActiveCamera());
            }

            SendPlayerTransform();
            SendInputState();
            TryForceCabinStart();
            ApplyPendingStates();
            MaybeTeleportToHost();
            SendPingIfDue();

            UnlockLocalDialogueCamera();

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

            var allowLocalInput = _settings.CoopUseLocalPlayer.Value;
            var routeInteractions = _settings.CoopRouteInteractions.Value;
            if (!allowLocalInput)
            {
                var inputManagers = UnityEngine.Object.FindObjectsOfType<InputManager>();
                foreach (var input in inputManagers)
                {
                    input.enabled = false;
                }

            }

            if (!allowLocalInput || routeInteractions)
            {
                var interactUis = UnityEngine.Object.FindObjectsOfType<interactableObjectUI>();
                foreach (var ui in interactUis)
                {
                    ui.enabled = false;
                }
            }

            DisableDialogueUIComponents();
            DisableStoryTriggers();
            DisableLocalAi();

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

            var message = new PlayerTransformMessage(state);
            if (_settings.UdpEnabled.Value && _client.HasUdp)
            {
                _client.SendUdp(message);
            }
            else
            {
                _client.Enqueue(message);
            }
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

            var message = new PlayerInputMessage(state);
            if (_settings.UdpEnabled.Value && _client.HasUdp)
            {
                _client.SendUdp(message);
            }
            else
            {
                _client.Enqueue(message);
            }
        }

        private void DrainIncoming()
        {
            var processed = 0;
            var transformCount = 0;
            var hasTransform = false;
            PlayerTransformState latestTransform = default;

            while (_client.TryDequeue(out var message))
            {
                if (message is PlayerTransformMessage player)
                {
                    latestTransform = player.State;
                    hasTransform = true;
                    transformCount++;
                    if (transformCount >= MaxTransformsPerFrame)
                    {
                        break;
                    }
                    continue;
                }

                processed++;
                if (message is DoorStateMessage door)
                {
                    ApplyDoorState(door.State);
                    IncrementHostStateApplied();
                }
                else if (message is HoldableStateMessage holdable)
                {
                    ApplyHoldableState(holdable.State);
                    IncrementHostStateApplied();
                }
                else if (message is StoryFlagMessage flag)
                {
                    PlayerPrefs.SetInt(flag.Key, flag.Value);
                    _lastStoryEventKey = flag.Key;
                    _lastStoryEventValue = flag.Value;
                    _lastStoryEventMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    IncrementHostStateApplied();
                }
                else if (message is AiTransformMessage ai)
                {
                    ApplyAiState(ai.State);
                    IncrementHostStateApplied();
                }
                else if (message is SceneChangeMessage scene)
                {
                    RequestSceneLoad(scene.SceneName, scene.BuildIndex, scene.StartSequence, scene.FromMenu);
                    if (!_isLoading && scene.SceneName == SceneManager.GetActiveScene().name)
                    {
                        SendSceneReady(scene.SceneName);
                    }
                    IncrementHostStateApplied();
                }
                else if (message is DialogueLineMessage dialogue)
                {
                    HandleRemoteDialogue(dialogue);
                    IncrementHostStateApplied();
                }
                else if (message is DialogueStartMessage dialogueStart)
                {
                    UpdateHostDialogueState(dialogueStart.ConversationId, dialogueStart.EntryId, -1);
                    IncrementHostStateApplied();
                }
                else if (message is DialogueAdvanceMessage dialogueAdvance)
                {
                    UpdateHostDialogueState(dialogueAdvance.ConversationId, dialogueAdvance.EntryId, -1);
                    IncrementHostStateApplied();
                }
                else if (message is DialogueChoiceMessage dialogueChoice)
                {
                    UpdateHostDialogueState(dialogueChoice.ConversationId, dialogueChoice.EntryId, dialogueChoice.ChoiceIndex);
                    IncrementHostStateApplied();
                }
                else if (message is DialogueEndMessage dialogueEnd)
                {
                    UpdateHostDialogueState(dialogueEnd.ConversationId, -1, -1, ended: true);
                    IncrementHostStateApplied();
                }

                if (processed >= MaxMessagesPerFrame)
                {
                    break;
                }
            }

            if (hasTransform)
            {
                try
                {
                    _lastHostPlayerId = latestTransform.PlayerId;
                    ApplyHostTransform(latestTransform);
                    _client.MarkLatestHostTransformConsumed();
                }
                catch (Exception ex)
                {
                    var now = Time.realtimeSinceStartup;
                    if (now >= _nextHostTransformErrorLogTime)
                    {
                        _nextHostTransformErrorLogTime = now + 5f;
                        _logger.LogWarning("ApplyHostTransform failed: " + ex.Message);
                    }
                }
            }
            else
            {
                ConsumeLatestHostTransform();
            }
        }

        private void ForceApplyLatchedTransform()
        {
            var latestSeq = _client.LatestHostTransformSeq;
            if (latestSeq <= 0)
            {
                return;
            }
            if (latestSeq <= _lastAppliedHostTransformSeq)
            {
                var now = Time.realtimeSinceStartup;
                if (now >= _nextHostTransformForceLogTime)
                {
                    _nextHostTransformForceLogTime = now + 5f;
                    var message = "ForceApplyLatchedTransform early: latestSeq=" + latestSeq +
                        " appliedSeq=" + _lastAppliedHostTransformSeq +
                        " appliedCount=" + Interlocked.Read(ref _hostTransformAppliedCount) +
                        " reason=seq-not-advanced";
                    _logger.LogInfo(message);
                    _sessionLogWrite?.Invoke(message);
                }
                return;
            }
            if (!_client.TryGetLatestHostTransform(out var state))
            {
                var now = Time.realtimeSinceStartup;
                if (now >= _nextHostTransformForceLogTime)
                {
                    _nextHostTransformForceLogTime = now + 5f;
                    var message = "ForceApplyLatchedTransform skip: latestSeq=" + latestSeq +
                        " appliedSeq=" + _lastAppliedHostTransformSeq +
                        " reason=no-latched-state";
                    _logger.LogInfo(message);
                    _sessionLogWrite?.Invoke(message);
                }
                return;
            }

            var beforeCount = Interlocked.Read(ref _hostTransformAppliedCount);
            var appliedOk = false;
            try
            {
                _lastHostPlayerId = state.PlayerId;
                ApplyHostTransform(state);
                _client.MarkLatestHostTransformConsumed();
                appliedOk = true;
            }
            catch (Exception ex)
            {
                var now = Time.realtimeSinceStartup;
                if (now >= _nextHostTransformErrorLogTime)
                {
                    _nextHostTransformErrorLogTime = now + 5f;
                    _logger.LogWarning("ForceApplyLatchedTransform failed: " + ex.Message);
                    _sessionLogWrite?.Invoke("ForceApplyLatchedTransform failed: " + ex);
                }
            }

            if (appliedOk && Interlocked.Read(ref _hostTransformAppliedCount) == beforeCount)
            {
                // Safety net: if ApplyHostTransform didn't bump the counters, do it here.
                MarkHostTransformAppliedFallback();
            }

            var logNow = Time.realtimeSinceStartup;
            if (appliedOk && logNow >= _nextHostTransformForceLogTime)
            {
                _nextHostTransformForceLogTime = logNow + 5f;
                var message = "ForceApplyLatchedTransform ok: latestSeq=" + latestSeq +
                    " appliedSeq=" + _lastAppliedHostTransformSeq +
                    " appliedCount=" + Interlocked.Read(ref _hostTransformAppliedCount);
                _logger.LogInfo(message);
                _sessionLogWrite?.Invoke(message);
            }
        }

        private void RequestSceneLoad(string sceneName, int buildIndex, int startSeq, int fromMenu)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            if (sceneName == "CabinScene" || sceneName == "CabinDarkScene")
            {
                _pendingStartSeq = sceneName == "CabinScene" ? startSeq : -1;
                PrepareCabinStartPrefs(sceneName, startSeq, fromMenu);
            }
            else
            {
                _pendingStartSeq = -1;
            }

            if (sceneName == SceneManager.GetActiveScene().name) return;
            _pendingScene = sceneName;
            _pendingSceneIndex = buildIndex;
            _lastSceneReadySent = string.Empty;
        }

        private void UpdateSceneLoad()
        {
            if (_isLoading)
            {
                if (_loading == null)
                {
                    ForceSceneLoad(_loadingSceneName, _loadingSceneIndex);
                    return;
                }

                if (_loading != null)
                {
                    var now = Time.realtimeSinceStartup;
                    if (_loading.progress > _loadingLastProgress + 0.001f)
                    {
                        _loadingLastProgress = _loading.progress;
                        _loadingLastProgressTime = now;
                    }

                    if (!_loading.allowSceneActivation && _loading.progress >= 0.9f)
                    {
                        _loading.allowSceneActivation = true;
                    }

                    if (!_loading.isDone && now - _loadingLastProgressTime > LoadingStallSeconds && _loading.progress >= 0.9f)
                    {
                        _loading.allowSceneActivation = true;
                    }

                    if (!_loading.isDone && now - _loadingStartTime > LoadingTimeoutSeconds)
                    {
                        ForceSceneLoad(_loadingSceneName, _loadingSceneIndex);
                        return;
                    }

                    if (_loading.isDone)
                    {
                        _isLoading = false;
                        _loading = null;
                        _loadingSceneName = null;
                        _loadingSceneIndex = -1;
                    }
                }
                return;
            }

            if (!string.IsNullOrEmpty(_pendingScene))
            {
                _isLoading = true;
                _loadingSceneName = _pendingScene;
                _loadingSceneIndex = _pendingSceneIndex;
                _loadingStartTime = Time.realtimeSinceStartup;
                _loadingLastProgress = 0f;
                _loadingLastProgressTime = _loadingStartTime;
                _loading = StartSceneLoad(_pendingScene, _pendingSceneIndex);
                _pendingScene = null;
                _pendingSceneIndex = -1;
            }
        }

        private void SendSceneReadyIfNeeded()
        {
            if (_isLoading || !string.IsNullOrEmpty(_pendingScene)) return;
            SendSceneReady(SceneManager.GetActiveScene().name);
        }

        private void SendSceneReady(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            var now = Time.realtimeSinceStartup;
            if (sceneName == _lastSceneReadySent && now - _lastSceneReadySentTime < 1f) return;

            _client.Enqueue(new SceneReadyMessage(sceneName));
            _lastSceneReadySent = sceneName;
            _lastSceneReadySentTime = now;
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

            var navAgent = target.GetComponent<NavMeshAgent>();
            if (navAgent != null)
            {
                navAgent.enabled = false;
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
            var stale = GetHostAppliedAgeSeconds() > Mathf.Max(1f, _settings.CoopTeleportStaleSeconds.Value);

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

        private void EnsureHostProxy()
        {
            if (_hostProxy != null) return;
            EnsureLocalPlayerRefs();
            if (_localFpc == null) return;
            _hostProxy = new RemotePlayerProxy(_localFpc, new Color(1f, 0.2f, 0.2f, 0.8f));
        }

        private void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            _camera = null;
            _controller = null;
            _gameplayDisabled = false;
            _aiDisabled = false;
            _pendingScene = null;
            _pendingSceneIndex = -1;
            _pendingStartSeq = -1;
            _loading = null;
            _isLoading = false;
            _initialCameraSet = false;
            _hasPendingHostCam = false;
            _pendingHostPlayerPos = Vector3.zero;
            _pendingHostPlayerRot = Quaternion.identity;
            Interlocked.Exchange(ref _lastHostAppliedTick, Stopwatch.GetTimestamp());
            _lastTeleportTime = 0f;
            _nextInputSendTime = 0f;
            _lastSceneReadySent = string.Empty;
            _lastSceneReadySentTime = 0f;
            _nextPingTime = 0f;
            _loadingStartTime = 0f;
            _loadingLastProgress = 0f;
            _loadingLastProgressTime = 0f;
            _loadingSceneName = null;
            _loadingSceneIndex = -1;
            _localFpc = null;
            if (_hostProxy != null && _hostProxy.Root != null)
            {
                UnityEngine.Object.Destroy(_hostProxy.Root.gameObject);
            }
            _hostProxy = null;
            _forcedCabinSpawn = false;
            _cabinPrefsPrepared = false;
            _dialogueUiDisabled = false;
            _remoteDialogueText = string.Empty;
            _remoteDialogueSpeaker = string.Empty;
            _remoteDialogueExpiresAt = 0f;
            _remoteDialogueMinClearAt = 0f;
            _remoteDialogueKind = 0;
            _hostDialogueConversationId = -1;
            _hostDialogueEntryId = -1;
            _hostDialogueChoiceIndex = -1;
            _hostDialogueEventMs = 0;
            _lastStoryEventKey = string.Empty;
            _lastStoryEventValue = 0;
            _lastStoryEventMs = 0;
            _nextDialogueDriftCheckTime = 0f;
            _lastHostPlayerId = 255;
            _lastAppliedHostNetMs = 0;
            _lastAppliedHostTransformSeq = 0;
            Interlocked.Exchange(ref _hostStateAppliedCount, 0);
            Interlocked.Exchange(ref _hostTransformAppliedCount, 0);
            _pendingDoorStates.Clear();
            _pendingHoldableStates.Clear();
            _pendingAiStates.Clear();
            _disabledTriggerBehaviours.Clear();
            _disabledTriggerColliders.Clear();
            _disabledInteractables.Clear();
        }

        private void SendPingIfDue()
        {
            var now = Time.realtimeSinceStartup;
            if (now < _nextPingTime) return;
            _nextPingTime = now + 1.5f;
            _client.SendPing();
        }

        private AsyncOperation StartSceneLoad(string sceneName, int buildIndex)
        {
            var hasIndex = buildIndex >= 0 && !string.IsNullOrEmpty(SceneUtility.GetScenePathByBuildIndex(buildIndex));
            if (hasIndex)
            {
                _logger.LogInfo("Co-op client loading scene index " + buildIndex + " (" + sceneName + ")");
                return SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                _logger.LogWarning("Scene not in build settings: " + sceneName + " (index " + buildIndex + ")");
            }

            _logger.LogInfo("Co-op client loading scene " + sceneName);
            return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }

        private void ForceSceneLoad(string sceneName, int buildIndex)
        {
            _logger.LogWarning("Scene load timeout, forcing sync load");
            _isLoading = false;
            _loading = null;
            _loadingSceneName = null;
            _loadingSceneIndex = -1;
            _pendingScene = null;
            _pendingSceneIndex = -1;

            var hasIndex = buildIndex >= 0 && !string.IsNullOrEmpty(SceneUtility.GetScenePathByBuildIndex(buildIndex));
            if (hasIndex)
            {
                SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
            }
            else if (!string.IsNullOrEmpty(sceneName))
            {
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            }
        }

        private void TryForceCabinStart()
        {
            if (_forcedCabinSpawn) return;
            if (!_settings.CoopForceCabinStart.Value) return;
            if (!_settings.CoopUseLocalPlayer.Value) return;
            if (!_hasPendingHostCam) return;
            if (SceneManager.GetActiveScene().name != "CabinScene") return;

            var gameManager = UnityEngine.Object.FindObjectOfType<CabinGameManager>();
            if (gameManager == null) return;
            if (gameManager.currentCabinSceneType != CabinGameManager.CabinSceneType.CabinScene) return;

            var cabinPlayer = PlayerController.GetInstance() as CabinPlayerController;
            var truckActive = cabinPlayer != null && cabinPlayer.truckPlayer != null && cabinPlayer.truckPlayer.activeInHierarchy;
            if (!truckActive && gameManager.CurrentSequence != SequenceType.DrivingToCabin)
            {
                return;
            }

            var sequenceName = ResolveCabinSequenceName();
            if (string.Equals(sequenceName, "StartAtStart", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            if (!string.IsNullOrEmpty(sequenceName) && TryInvokeCabinSequence(gameManager, sequenceName))
            {
                _forcedCabinSpawn = true;
                TeleportToHost();
                return;
            }

            _logger.LogWarning("Forcing cabin spawn to skip intro (client)");
            gameManager.TestingModeStartInsideBeforeFridgeStocking();
            _forcedCabinSpawn = true;
            TeleportToHost();
        }

        private void PrepareCabinStartPrefs(string sceneName, int startSeq, int fromMenu)
        {
            if (startSeq >= 0 || fromMenu >= 0)
            {
                var effectiveFromMenu = fromMenu;
                if (startSeq > 0 && effectiveFromMenu <= 0)
                {
                    effectiveFromMenu = 1;
                }
                if (effectiveFromMenu >= 0)
                {
                    PlayerPrefs.SetInt(PlayerPrefKeys.FROM_MENU, effectiveFromMenu);
                }
                if (startSeq >= 0)
                {
                    PlayerPrefs.SetInt(PlayerPrefKeys.START_SEQ, startSeq);
                }
                PlayerPrefs.Save();
                _cabinPrefsPrepared = true;
                _logger.LogInfo("Applied host start prefs (seq=" + startSeq + ", fromMenu=" + fromMenu + " -> " + effectiveFromMenu + ")");
                return;
            }

            if (_cabinPrefsPrepared) return;
            if (!_settings.CoopForceCabinStart.Value) return;
            if (sceneName != "CabinScene") return;

            var seqName = _settings.CoopCabinStartSequence.Value;
            var assembly = typeof(CabinGameManager).Assembly;
            var seqType = assembly.GetType("CabinSceneSequences") ?? assembly.GetType("CabinGameManager+CabinSceneSequences");
            if (seqType == null || !seqType.IsEnum)
            {
                _logger.LogWarning("CabinSceneSequences type not found; cannot set start sequence.");
                return;
            }

            try
            {
                var seqValue = Enum.Parse(seqType, seqName, ignoreCase: true);
                PlayerPrefs.SetInt(PlayerPrefKeys.FROM_MENU, 1);
                PlayerPrefs.SetInt(PlayerPrefKeys.START_SEQ, Convert.ToInt32(seqValue));
                PlayerPrefs.Save();
                _cabinPrefsPrepared = true;
                _logger.LogInfo("Set Cabin start sequence to " + seqName + " (client)");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to set Cabin start sequence: " + ex.Message);
            }
        }

        private void DisableDialogueUIComponents()
        {
            if (_dialogueUiDisabled) return;

            var behaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour == null) continue;
                var typeName = behaviour.GetType().FullName;
                if (typeName == "PixelCrushers.DialogueSystem.DialogueSystemController" ||
                    typeName == "PixelCrushers.DialogueSystem.StandardDialogueUI" ||
                    typeName == "PixelCrushers.DialogueSystem.UnityUIDialogueUI" ||
                    typeName == "PixelCrushers.DialogueSystem.SMSDialogueUI" ||
                    typeName == "PixelCrushers.DialogueSystem.StandardUIResponseButton")
                {
                    behaviour.enabled = false;
                }
            }

            _dialogueUiDisabled = true;
        }

        private void UnlockLocalDialogueCamera()
        {
            if (_settings.ModeSetting.Value != Mode.CoopClient) return;

            var now = Time.realtimeSinceStartup;
            if (now < _nextDialogueUnlockTime) return;
            _nextDialogueUnlockTime = now + 0.2f;

            try
            {
                var conversationActive = DialogueManager.isConversationActive;
                if (conversationActive)
                {
                    DialogueManager.StopConversation();
                }

                var cabin = UnityEngine.Object.FindObjectOfType<CabinPlayerController>();
                if (cabin != null)
                {
                    var shouldRelease = conversationActive;
                    if (cabin.dialogueCamera != null && cabin.dialogueCamera.gameObject.activeSelf)
                    {
                        shouldRelease = true;
                    }
                    if (cabin.lockCameraMovement != null && cabin.lockCameraMovement.enabled)
                    {
                        shouldRelease = true;
                    }

                    if (shouldRelease)
                    {
                        cabin.EndConvoWith();
                        cabin.ResumeCameraControl();
                    }

                    if (cabin.dialogueCamera != null && cabin.dialogueCamera.gameObject.activeSelf)
                    {
                        cabin.dialogueCamera.gameObject.SetActive(false);
                    }

                    if (cabin.lockCameraMovement != null && cabin.lockCameraMovement.enabled)
                    {
                        cabin.lockCameraMovement.enabled = false;
                        cabin.lockCameraMovement.disableFov = false;
                    }

                    var cabinFpc = _playerFirstPersonField != null
                        ? _playerFirstPersonField.GetValue(cabin) as FirstPersonController
                        : null;
                    if (cabinFpc != null && !cabinFpc.enabled)
                    {
                        cabinFpc.enabled = true;
                    }

                    if (shouldRelease)
                    {
                        var cabinGameManager = UnityEngine.Object.FindObjectOfType<CabinGameManager>();
                        if (cabinGameManager != null &&
                            cabinGameManager.currentPlayerState != CabinGameManager.PlayerState.Normal)
                        {
                            cabinGameManager.ChangePlayerState(CabinGameManager.PlayerState.Normal);
                        }
                    }
                }

                if (_localFpc != null)
                {
                    if (!_localFpc.enabled) _localFpc.enabled = true;
                    if (_localFpc.characterController != null && !_localFpc.characterController.enabled)
                    {
                        _localFpc.characterController.enabled = true;
                    }
                }

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("UnlockLocalDialogueCamera failed: " + ex.Message);
            }
        }

        private void DisableStoryTriggers()
        {
            var allowLocalInput = _settings.CoopUseLocalPlayer.Value;
            var routeInteractions = _settings.CoopRouteInteractions.Value;
            var disableLocalProgression = true;
            var allowInteractableBehaviours = allowLocalInput && !routeInteractions && !disableLocalProgression;
            var behaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour == null || !behaviour.enabled) continue;
                var instanceId = behaviour.GetInstanceID();
                if (_disabledTriggerBehaviours.Contains(instanceId)) continue;

                if (behaviour is Iinteractable)
                {
                    if (!allowInteractableBehaviours)
                    {
                        behaviour.enabled = false;
                        _disabledInteractables.Add(instanceId);
                        _disabledTriggerBehaviours.Add(instanceId);
                    }
                    continue;
                }

                var typeName = behaviour.GetType().Name;
                if (IsTriggerScriptName(typeName))
                {
                    behaviour.enabled = false;
                    _disabledTriggerBehaviours.Add(instanceId);
                }
            }

            var colliders = UnityEngine.Object.FindObjectsOfType<Collider>();
            foreach (var collider in colliders)
            {
                if (collider == null || !collider.enabled || !collider.isTrigger) continue;
                var id = collider.GetInstanceID();
                if (_disabledTriggerColliders.Contains(id)) continue;

                var components = collider.GetComponents<MonoBehaviour>();
                var hasInteractable = false;
                var hasTriggerScript = false;
                foreach (var component in components)
                {
                    if (component == null) continue;
                    if (component is Iinteractable)
                    {
                        hasInteractable = true;
                        continue;
                    }
                    if (IsTriggerScriptName(component.GetType().Name))
                    {
                        hasTriggerScript = true;
                    }
                }

                var allowInteractableColliders = allowLocalInput || routeInteractions;
                var disable = false;
                if (hasInteractable && !allowInteractableColliders)
                {
                    disable = true;
                }
                if (hasTriggerScript && !(hasInteractable && allowInteractableColliders))
                {
                    disable = true;
                }

                if (disable)
                {
                    collider.enabled = false;
                    _disabledTriggerColliders.Add(id);
                }
            }
        }

        private void DisableLocalAi()
        {
            if (_aiDisabled) return;
            var agents = UnityEngine.Object.FindObjectsOfType<NavMeshAgent>();
            foreach (var agent in agents)
            {
                if (agent == null) continue;
                if (IsLocalPlayerAgent(agent)) continue;
                agent.enabled = false;
            }
            _aiDisabled = true;
        }

        private static bool IsLocalPlayerAgent(NavMeshAgent agent)
        {
            if (agent == null) return false;
            var player = PlayerController.GetInstance();
            if (player == null) return false;
            return agent.transform == player.transform || agent.transform.IsChildOf(player.transform);
        }

        private static bool IsTriggerScriptName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (TriggerScriptNames.Contains(name)) return true;
            return name.IndexOf("Trigger", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void UpdateHostDialogueState(int conversationId, int entryId, int choiceIndex, bool ended = false)
        {
            // TODO: For full dialogue mirroring, start/jump the client conversation here instead of only stopping local drift.
            if (ended)
            {
                _hostDialogueConversationId = -1;
                _hostDialogueEntryId = -1;
                _hostDialogueChoiceIndex = -1;
            }
            else
            {
                _hostDialogueConversationId = conversationId;
                _hostDialogueEntryId = entryId;
                if (choiceIndex >= 0)
                {
                    _hostDialogueChoiceIndex = choiceIndex;
                }
            }

            _hostDialogueEventMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (DialogueManager.isConversationActive && ended)
            {
                DialogueManager.StopConversation();
            }
        }

        public bool TryGetLocalDialogueState(out int conversationId, out int entryId)
        {
            conversationId = -1;
            entryId = -1;
            var state = DialogueManager.currentConversationState;
            if (state == null) return false;
            var entry = state.subtitle != null ? state.subtitle.dialogueEntry : null;
            if (entry == null) return false;
            conversationId = entry.conversationID;
            entryId = entry.id;
            return true;
        }

        private void CheckDialogueDrift()
        {
            var now = Time.realtimeSinceStartup;
            if (now < _nextDialogueDriftCheckTime) return;
            _nextDialogueDriftCheckTime = now + 0.5f;

            var localActive = DialogueManager.isConversationActive;
            if (_hostDialogueConversationId < 0)
            {
                if (localActive)
                {
                    DialogueManager.StopConversation();
                }
                return;
            }

            if (!localActive) return;

            if (!TryGetLocalDialogueState(out var localConvoId, out var localEntryId))
            {
                DialogueManager.StopConversation();
                return;
            }

            if (localConvoId != _hostDialogueConversationId ||
                (_hostDialogueEntryId >= 0 && localEntryId != _hostDialogueEntryId))
            {
                DialogueManager.StopConversation();
            }
        }

        private void HandleRemoteDialogue(DialogueLineMessage dialogue)
        {
            if (dialogue == null) return;
            var now = Time.realtimeSinceStartup;
            if (string.IsNullOrEmpty(dialogue.Text))
            {
                if (!string.IsNullOrEmpty(_remoteDialogueText) && now < _remoteDialogueMinClearAt)
                {
                    return;
                }
                ClearRemoteDialogue();
                return;
            }

            _remoteDialogueSpeaker = dialogue.Speaker ?? string.Empty;
            _remoteDialogueText = dialogue.Text ?? string.Empty;
            _remoteDialogueKind = dialogue.Kind;
            var duration = Mathf.Max(MinDialogueDisplaySeconds, dialogue.Duration);
            _remoteDialogueExpiresAt = now + duration;
            _remoteDialogueMinClearAt = now + MinDialogueDisplaySeconds;
        }

        private void UpdateRemoteDialogue()
        {
            if (string.IsNullOrEmpty(_remoteDialogueText)) return;
            if (Time.realtimeSinceStartup >= _remoteDialogueExpiresAt)
            {
                ClearRemoteDialogue();
            }
        }

        private void ClearRemoteDialogue()
        {
            _remoteDialogueText = string.Empty;
            _remoteDialogueSpeaker = string.Empty;
            _remoteDialogueExpiresAt = 0f;
            _remoteDialogueMinClearAt = 0f;
            _remoteDialogueKind = 0;
        }

        private void ConsumeLatestHostTransform()
        {
            if (!_client.TryConsumeLatestHostTransform(out var state))
            {
                return;
            }

            try
            {
                _lastHostPlayerId = state.PlayerId;
                ApplyHostTransform(state);
            }
            catch (Exception ex)
            {
                var now = Time.realtimeSinceStartup;
                if (now >= _nextHostTransformErrorLogTime)
                {
                    _nextHostTransformErrorLogTime = now + 5f;
                    _logger.LogWarning("ApplyHostTransform failed: " + ex.Message);
                }
            }
        }

        private void IncrementHostStateApplied()
        {
            Interlocked.Increment(ref _hostStateAppliedCount);
        }

        private void UpdateHostTransformHeartbeat()
        {
            var lastMs = _client.LastHostTransformReceivedMs;
            var lastNetMs = Math.Max(_client.LastTcpTransformMs, _client.LastUdpTransformMs);
            if (lastMs <= 0 || lastNetMs > lastMs)
            {
                lastMs = lastNetMs;
            }
            if (lastMs <= 0) return;

            _lastHostTransformReceiveMs = lastMs;
            _hostTransformReceiveCount = _client.TcpTransformCount + _client.UdpTransformCount;
        }

        private void ApplyHostTransform(PlayerTransformState state)
        {
            var latestNetMs = Math.Max(_client.LastTcpTransformMs, _client.LastUdpTransformMs);
            var latestSeq = _client.LatestHostTransformSeq;

            EnsureHostAvatar();
            EnsureHostProxy();
            if (_hostProxy != null && _hostProxy.Root != null)
            {
                _hostProxy.SetActive(true);
                _hostProxy.ApplyTransform(state);
                _hostAvatar?.SetActive(false);
            }
            else
            {
                if (_hostAvatar != null && _hostAvatar.Root != null)
                {
                    _hostAvatar.SetActive(true);
                    _hostAvatar.Root.position = state.Position;
                    _hostAvatar.Root.rotation = state.Rotation;
                }
            }

            _pendingHostPlayerPos = state.Position;
            _pendingHostPlayerRot = state.Rotation;
            _pendingHostCamPos = state.CameraPosition;
            _pendingHostCamRot = state.CameraRotation;
            _hasPendingHostCam = true;
            var appliedTick = MarkHostTransformApplied();
            if (latestNetMs > 0)
            {
                _lastAppliedHostNetMs = latestNetMs;
            }
            if (latestSeq > 0)
            {
                _lastAppliedHostTransformSeq = latestSeq;
            }
            var appliedCount = Interlocked.Read(ref _hostTransformAppliedCount);

            var now = Time.realtimeSinceStartup;
            if (now >= _nextHostTransformApplyLogTime)
            {
                _nextHostTransformApplyLogTime = now + 5f;
                var message = "ApplyHostTransform ok: count=" + appliedCount +
                    " seq=" + _lastAppliedHostTransformSeq +
                    " net=" + _lastAppliedHostNetMs +
                    " tick=" + appliedTick;
                _logger.LogInfo(message);
                _sessionLogWrite?.Invoke(message);
            }
        }

        private long MarkHostTransformApplied()
        {
            var appliedTick = Stopwatch.GetTimestamp();
            Interlocked.Exchange(ref _lastHostAppliedTick, appliedTick);
            Interlocked.Increment(ref _hostTransformAppliedCount);
            return appliedTick;
        }

        private void EnsureHostAvatar()
        {
            if (_hostAvatar == null)
            {
                _hostAvatar = new RemoteAvatar("CoopHostAvatar", new Color(1f, 0.2f, 0.2f, 0.8f));
                return;
            }
            _hostAvatar.EnsureAlive();
        }

        private void MarkHostTransformAppliedFallback()
        {
            var appliedTick = MarkHostTransformApplied();
            var message = "ApplyHostTransform fallback: count=" +
                Interlocked.Read(ref _hostTransformAppliedCount) +
                " seq=" + _lastAppliedHostTransformSeq +
                " net=" + _lastAppliedHostNetMs +
                " tick=" + appliedTick;
            _logger.LogWarning(message);
            _sessionLogWrite?.Invoke(message);
        }

        private float GetHostAppliedAgeSeconds()
        {
            var lastTick = Interlocked.Read(ref _lastHostAppliedTick);
            if (lastTick <= 0) return 0f;

            var deltaTicks = Stopwatch.GetTimestamp() - lastTick;
            if (deltaTicks <= 0) return 0f;

            return (float)(deltaTicks / (double)Stopwatch.Frequency);
        }

        private string ResolveCabinSequenceName()
        {
            if (_pendingStartSeq >= 0 && TryGetCabinSequenceName(_pendingStartSeq, out var hostName))
            {
                return hostName;
            }

            return _settings.CoopCabinStartSequence.Value;
        }

        private bool TryGetCabinSequenceName(int startSeq, out string name)
        {
            name = null;
            var assembly = typeof(CabinGameManager).Assembly;
            var seqType = assembly.GetType("CabinSceneSequences") ?? assembly.GetType("CabinGameManager+CabinSceneSequences");
            if (seqType == null || !seqType.IsEnum) return false;

            name = Enum.GetName(seqType, startSeq);
            return !string.IsNullOrEmpty(name);
        }

        private bool TryInvokeCabinSequence(CabinGameManager gameManager, string sequenceName)
        {
            if (string.IsNullOrEmpty(sequenceName)) return false;
            if (string.Equals(sequenceName, "StartAtStart", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var methodName = "TestingMode" + sequenceName;
            var method = typeof(CabinGameManager).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null) return false;

            try
            {
                _logger.LogWarning("Forcing cabin start sequence: " + sequenceName);
                method.Invoke(gameManager, null);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to force cabin sequence: " + ex.Message);
                return false;
            }
        }
    }
}
