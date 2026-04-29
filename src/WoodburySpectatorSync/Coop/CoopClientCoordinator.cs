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
        private bool _mikeControllersDisabled;
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
        private readonly Dictionary<string, int> _pendingCabinHouseFlags = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _pendingCabinGameFlags = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _pendingPizzeriaFlags = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _pendingRoadTripFlags = new Dictionary<string, int>();
        private float _loadingStartTime;
        private float _loadingLastProgress;
        private float _loadingLastProgressTime;
        private string _loadingSceneName;
        private CabinHouseManager _cabinHouseManager;
        private CabinGameManager _cabinGameManager;
        private PizzeriaGameManager _pizzeriaGameManager;
        private PizzeriaTruckDoor _pizzeriaTruckDoor;
        private MikePizzeria _pizzeriaMike;
        private RoadTripGameManager _roadTripGameManager;
        private MikeInCar _roadTripMikeInCar;
        private MikeTruckInLoopScene _roadTripTruck;
        private readonly Dictionary<string, FieldInfo> _cabinHouseFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _cabinGameFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _pizzeriaFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _pizzeriaMikeFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _pizzeriaGameObjectFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _pizzeriaMikeGameObjectFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _roadTripFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _roadTripMikeFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _roadTripTruckFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, Transform> _aiFallbackTargets = new Dictionary<string, Transform>(StringComparer.Ordinal);
        private readonly HashSet<string> _aiMissingLogged = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _aiFallbackLogged = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _aiDebugPaths = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _missingSceneFieldLogged = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, Vector3> _aiLastPositions = new Dictionary<string, Vector3>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _aiLastTimes = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, Animator> _aiAnimatorCache = new Dictionary<string, Animator>(StringComparer.Ordinal);
        private readonly Dictionary<string, AnimatorDriveInfo> _aiAnimatorParams = new Dictionary<string, AnimatorDriveInfo>(StringComparer.Ordinal);
        private float _nextAiMissingLogTime;
        private float _nextAiFallbackLogTime;
        private float _nextAiDebugLogTime;
        private int _aiDebugLogCount;
        private readonly List<ICoopClientSceneAdapter> _sceneAdapters = new List<ICoopClientSceneAdapter>();
        private readonly Dictionary<string, float> _pendingDoorFirstSeen = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _pendingHoldableFirstSeen = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _pendingAiFirstSeen = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _pendingCabinHouseFirstSeen = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _pendingCabinGameFirstSeen = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _pendingPizzeriaFirstSeen = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _pendingRoadTripFirstSeen = new Dictionary<string, float>(StringComparer.Ordinal);
        private float _nextPendingRetryLogTime;

        private const string CabinHouseFlagPrefix = "CabinHouse.";
        private const string CabinGameFlagPrefix = "CabinGM.";
        private const string CabinMikeAnimFieldPrefix = "MikeAnim.";
        private const string PizzeriaFlagPrefix = "PizzeriaGM.";
        private const string PizzeriaMikeFlagPrefix = PizzeriaFlagPrefix + "Mike.";
        private const string PizzeriaActiveGamePrefix = PizzeriaFlagPrefix + "Active.Game.";
        private const string PizzeriaActiveMikePrefix = PizzeriaFlagPrefix + "Active.Mike.";
        private const string RoadTripFlagPrefix = "RoadTripGM.";
        private const string RoadTripMikeFlagPrefix = RoadTripFlagPrefix + "Mike.";
        private const string RoadTripTruckFlagPrefix = RoadTripFlagPrefix + "Truck.";
        private int _loadingSceneIndex = -1;
        private const float LoadingTimeoutSeconds = 40f;
        private const float LoadingStallSeconds = 6f;
        private const int MaxMessagesPerFrame = 200;
        private const int MaxTransformsPerFrame = 60;
        private const float PendingRetryLogIntervalSeconds = 5f;
        private const float PendingRetryWarnAgeSeconds = 8f;
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
        private Transform _forcedMikeTarget;
        private string _forcedMikeReason = string.Empty;
        private float _nextMikeSyncLogTime;
        private SequenceType _lastMikeSequence = SequenceType.NotInAnySequence;
        private CabinGameManager.CurrentMike _lastMikeState = CabinGameManager.CurrentMike.Prefishing;
        private bool _hasMikeState;
        private int _remoteMikeAnimStateHash;
        private int _remoteMikeAnimLoop = -1;
        private int _remoteMikeAnimPhase10 = -1;
        private int _remoteMikeAnimTransition;
        private int _remoteMikeAnimNextStateHash;
        private int _appliedMikeAnimStateHash;
        private int _appliedMikeAnimLoop = -1;
        private int _appliedMikeAnimPhase10 = -1;
        private int _appliedMikeAnimTransition;
        private int _appliedMikeAnimNextStateHash;
        private float _nextDialogueUnlockTime;
        private int _sceneReadyGeneration;
        private int _sceneReadySentGeneration = -1;
        private bool _sceneReadyDirty = true;
        private bool _wasConnected;
        private string _lastStartPrefScene = string.Empty;
        private int _lastStartPrefSeq = int.MinValue;
        private int _lastStartPrefFromMenu = int.MinValue;
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

        private readonly string[] _pizzeriaFieldNames = new[]
        {
            "currentPlayerState",
            "phoneUIState",
            "playerTalking",
            "playerGotPizza",
            "canBurp",
            "burp",
            "autoStartFirstConvo",
            "completedFirstConvo",
            "playerCanSendMessage"
        };

        private readonly string[] _pizzeriaMikeFieldNames = new[]
        {
            "currentConvo",
            "onDialogue",
            "firstConvoComplete",
            "completedPizzeria",
            "moving",
            "state",
            "haveCounterConversation",
            "sittingConvo1Done",
            "sittingConvoCanDo",
            "sittingConvo2Done",
            "triggeredTexts",
            "pizzaConversationDone",
            "sittingDownConvoTriggered",
            "goGetPizza",
            "pizzaReadyConvoCalled",
            "mikeGotThePizzaInHand",
            "cashierAllSetConvoStarted",
            "cashierAllSetConvoDone",
            "goTrashCan",
            "pizzaInHand",
            "eatingPizza",
            "mikeThrowPizza"
        };

        private readonly string[] _pizzeriaGameObjectFieldNames = new[]
        {
            "keysUI"
        };

        private readonly string[] _pizzeriaMikeGameObjectFieldNames = new[]
        {
            "doorCollider",
            "phone",
            "pizzaBox",
            "pizzaBoxOnTable",
            "pizzaBoxOnCounter",
            "pizzaSlice",
            "orderConvoTrigger"
        };

        private readonly string[] _roadTripFieldNames = new[]
        {
            "playerTalking",
            "autoStartConversation",
            "convoCompleted",
            "phoneUIState",
            "startBump",
            "playerCanSendMessage"
        };

        private readonly string[] _roadTripMikeFieldNames = new[]
        {
            "currentConvo",
            "mikeinConversation",
            "busConvoCompleted",
            "deerConvoCompleted",
            "finalConvoCompleted",
            "passedBus",
            "passedDeer"
        };

        private readonly string[] _roadTripTruckFieldNames = new[]
        {
            "speed",
            "distanceTravelled",
            "pushBreak",
            "accelerateFromStop",
            "dialogueBreak",
            "run"
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
            CacheSceneAdapterFields();
            InitializeSceneAdapters();
            OnSceneEnterAdapters(SceneManager.GetActiveScene().name);

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
            if (_client.IsConnected && !_wasConnected)
            {
                _wasConnected = true;
                MarkSceneReadyDirty("connect");
                _sceneReadySentGeneration = -1;
            }
            else if (!_client.IsConnected && _wasConnected)
            {
                _wasConnected = false;
                ResetDisconnectedOverlayState();
            }

            if (!_client.IsConnected)
            {
                if (_sceneReadySentGeneration != -1 ||
                    _lastAppliedHostNetMs != 0 ||
                    _lastAppliedHostTransformSeq != 0 ||
                    Interlocked.Read(ref _hostStateAppliedCount) != 0 ||
                    Interlocked.Read(ref _hostTransformAppliedCount) != 0)
                {
                    ResetDisconnectedOverlayState();
                }
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
            EnsureCabinGameManager();
            SyncMikeVariantIfChanged();
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
                    ApplyStoryFlag(flag);
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
                        SendSceneReady(scene.SceneName, force: true);
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
                else if (message is PlayerInputMessage hostInput)
                {
                    _lastHostPlayerId = hostInput.State.PlayerId;
                    _hostProxy?.ApplyInput(hostInput.State);
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
                if (_settings.VerboseLogging.Value && now >= _nextHostTransformForceLogTime)
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
                if (_settings.VerboseLogging.Value && now >= _nextHostTransformForceLogTime)
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
            if (_settings.VerboseLogging.Value && appliedOk && logNow >= _nextHostTransformForceLogTime)
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

            if (sceneName == SceneManager.GetActiveScene().name)
            {
                MarkSceneReadyDirty("host-scene-mismatch-recovery");
                return;
            }

            _pendingScene = sceneName;
            _pendingSceneIndex = buildIndex;
            _lastSceneReadySent = string.Empty;
            MarkSceneReadyDirty("host-scene-change");
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
                        MarkSceneReadyDirty("scene-load-complete");
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
            if (!_sceneReadyDirty) return;
            SendSceneReady(SceneManager.GetActiveScene().name, force: false);
        }

        private void SendSceneReady(string sceneName, bool force)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            if (!force &&
                _sceneReadySentGeneration == _sceneReadyGeneration &&
                string.Equals(sceneName, _lastSceneReadySent, StringComparison.Ordinal))
            {
                return;
            }

            _client.Enqueue(new SceneReadyMessage(sceneName));
            _lastSceneReadySent = sceneName;
            _lastSceneReadySentTime = Time.realtimeSinceStartup;
            _sceneReadySentGeneration = _sceneReadyGeneration;
            _sceneReadyDirty = false;
        }

        private void MarkSceneReadyDirty(string reason)
        {
            unchecked
            {
                _sceneReadyGeneration++;
            }

            _sceneReadyDirty = true;
            if (_settings.VerboseLogging.Value)
            {
                _logger.LogInfo("SceneReady marked dirty: gen=" + _sceneReadyGeneration + " reason=" + reason);
            }
        }

        private void ResetDisconnectedOverlayState()
        {
            Interlocked.Exchange(ref _lastHostAppliedTick, 0);
            _lastHostTransformReceiveMs = 0;
            _hostTransformReceiveCount = 0;
            Interlocked.Exchange(ref _hostStateAppliedCount, 0);
            Interlocked.Exchange(ref _hostTransformAppliedCount, 0);
            _lastAppliedHostTransformSeq = 0;
            _lastAppliedHostNetMs = 0;
            _lastHostPlayerId = 255;
            _hostDialogueConversationId = -1;
            _hostDialogueEntryId = -1;
            _hostDialogueChoiceIndex = -1;
            _hostDialogueEventMs = 0;
            _lastStoryEventKey = string.Empty;
            _lastStoryEventValue = 0;
            _lastStoryEventMs = 0;
            _remoteMikeAnimStateHash = 0;
            _remoteMikeAnimLoop = -1;
            _remoteMikeAnimPhase10 = -1;
            _remoteMikeAnimTransition = 0;
            _remoteMikeAnimNextStateHash = 0;
            _appliedMikeAnimStateHash = 0;
            _appliedMikeAnimLoop = -1;
            _appliedMikeAnimPhase10 = -1;
            _appliedMikeAnimTransition = 0;
            _appliedMikeAnimNextStateHash = 0;
            _lastSceneReadySent = string.Empty;
            _lastSceneReadySentTime = 0f;
            _sceneReadySentGeneration = -1;
            _sceneReadyDirty = true;
            _pendingDoorStates.Clear();
            _pendingHoldableStates.Clear();
            _pendingAiStates.Clear();
            _pendingCabinHouseFlags.Clear();
            _pendingCabinGameFlags.Clear();
            _pendingPizzeriaFlags.Clear();
            _pendingRoadTripFlags.Clear();
            _pendingDoorFirstSeen.Clear();
            _pendingHoldableFirstSeen.Clear();
            _pendingAiFirstSeen.Clear();
            _pendingCabinHouseFirstSeen.Clear();
            _pendingCabinGameFirstSeen.Clear();
            _pendingPizzeriaFirstSeen.Clear();
            _pendingRoadTripFirstSeen.Clear();
            _nextPendingRetryLogTime = 0f;
            ClearRemoteDialogue();
        }

        private void ApplyDoorState(DoorState state)
        {
            if (_isLoading || !TryApplyDoorState(state))
            {
                _pendingDoorStates[state.Path] = state;
                TrackPendingState(_pendingDoorFirstSeen, state.Path);
                return;
            }

            _pendingDoorStates.Remove(state.Path);
            ClearPendingState(_pendingDoorFirstSeen, state.Path);
        }

        private void ApplyStoryFlag(StoryFlagMessage flag)
        {
            if (flag == null) return;
            var key = flag.Key ?? string.Empty;
            if (!TryApplySceneAdapterFlag(key, flag.Value, allowDefer: true))
            {
                PlayerPrefs.SetInt(key, flag.Value);
            }

            _lastStoryEventKey = flag.Key;
            _lastStoryEventValue = flag.Value;
            _lastStoryEventMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private void ApplyHoldableState(HoldableState state)
        {
            if (_isLoading || !TryApplyHoldableState(state))
            {
                _pendingHoldableStates[state.Path] = state;
                TrackPendingState(_pendingHoldableFirstSeen, state.Path);
                return;
            }

            _pendingHoldableStates.Remove(state.Path);
            ClearPendingState(_pendingHoldableFirstSeen, state.Path);
        }

        private void ApplyAiState(AiTransformState state)
        {
            if (_isLoading || !TryApplyAiState(state))
            {
                _pendingAiStates[state.Path] = state;
                TrackPendingState(_pendingAiFirstSeen, state.Path);
                return;
            }

            _pendingAiStates.Remove(state.Path);
            ClearPendingState(_pendingAiFirstSeen, state.Path);
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

        private bool TryApplyCabinHouseFlag(string key, int value, bool allowDefer)
        {
            if (string.IsNullOrEmpty(key) || !key.StartsWith(CabinHouseFlagPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            if (SceneManager.GetActiveScene().name != "CabinScene")
            {
                if (allowDefer)
                {
                    _pendingCabinHouseFlags[key] = value;
                    TrackPendingState(_pendingCabinHouseFirstSeen, key);
                }
                return false;
            }

            if (_cabinHouseManager == null)
            {
                _cabinHouseManager = UnityEngine.Object.FindObjectOfType<CabinHouseManager>();
                if (_cabinHouseManager == null)
                {
                    if (allowDefer)
                    {
                        _pendingCabinHouseFlags[key] = value;
                        TrackPendingState(_pendingCabinHouseFirstSeen, key);
                    }
                    return false;
                }
            }

            var fieldName = key.Substring(CabinHouseFlagPrefix.Length);
            if (!_cabinHouseFieldCache.TryGetValue(fieldName, out var field))
            {
                field = typeof(CabinHouseManager).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                _cabinHouseFieldCache[fieldName] = field;
            }

            if (field == null || field.FieldType != typeof(bool))
            {
                return true;
            }

            try
            {
                field.SetValue(_cabinHouseManager, value != 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ApplyCabinHouseFlag failed: " + ex.Message);
            }

            _pendingCabinHouseFlags.Remove(key);
            ClearPendingState(_pendingCabinHouseFirstSeen, key);

            return true;
        }

        private bool TryApplyCabinGameFlag(string key, int value, bool allowDefer)
        {
            if (string.IsNullOrEmpty(key) || !key.StartsWith(CabinGameFlagPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            if (SceneManager.GetActiveScene().name != "CabinScene")
            {
                if (allowDefer)
                {
                    _pendingCabinGameFlags[key] = value;
                    TrackPendingState(_pendingCabinGameFirstSeen, key);
                }
                return false;
            }

            if (_cabinGameManager == null)
            {
                _cabinGameManager = UnityEngine.Object.FindObjectOfType<CabinGameManager>();
                if (_cabinGameManager == null)
                {
                    if (allowDefer)
                    {
                        _pendingCabinGameFlags[key] = value;
                        TrackPendingState(_pendingCabinGameFirstSeen, key);
                    }
                    return false;
                }
            }

            var fieldName = key.Substring(CabinGameFlagPrefix.Length);
            if (fieldName.StartsWith(CabinMikeAnimFieldPrefix, StringComparison.Ordinal))
            {
                if (!TryApplyCabinMikeAnimationFlag(fieldName, value))
                {
                    if (allowDefer)
                    {
                        _pendingCabinGameFlags[key] = value;
                        TrackPendingState(_pendingCabinGameFirstSeen, key);
                    }
                    return false;
                }

                _pendingCabinGameFlags.Remove(key);
                ClearPendingState(_pendingCabinGameFirstSeen, key);
                return true;
            }

            if (!_cabinGameFieldCache.TryGetValue(fieldName, out var field))
            {
                field = typeof(CabinGameManager).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                _cabinGameFieldCache[fieldName] = field;
            }

            if (field == null)
            {
                LogMissingSceneField(_cabinGameManager.GetType(), fieldName);
                return false;
            }

            try
            {
                if (field.FieldType == typeof(bool))
                {
                    field.SetValue(_cabinGameManager, value != 0);
                }
                else if (field.FieldType.IsEnum)
                {
                    var enumValue = Enum.ToObject(field.FieldType, value);
                    field.SetValue(_cabinGameManager, enumValue);
                }
                else if (field.FieldType == typeof(int))
                {
                    field.SetValue(_cabinGameManager, value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ApplyCabinGameFlag failed: " + ex.Message);
            }

            if (fieldName == "CurrentSequence" || fieldName == "currentMike")
            {
                UpdateMikeVariantFromState();
            }

            _pendingCabinGameFlags.Remove(key);
            ClearPendingState(_pendingCabinGameFirstSeen, key);

            return true;
        }

        private void CacheSceneAdapterFields()
        {
            _pizzeriaFieldCache.Clear();
            for (var i = 0; i < _pizzeriaFieldNames.Length; i++)
            {
                var name = _pizzeriaFieldNames[i];
                _pizzeriaFieldCache[name] = FindInstanceField(typeof(PizzeriaGameManager), name);
            }

            _pizzeriaMikeFieldCache.Clear();
            for (var i = 0; i < _pizzeriaMikeFieldNames.Length; i++)
            {
                var name = _pizzeriaMikeFieldNames[i];
                _pizzeriaMikeFieldCache[name] = FindInstanceField(typeof(MikePizzeria), name);
            }

            _pizzeriaGameObjectFieldCache.Clear();
            for (var i = 0; i < _pizzeriaGameObjectFieldNames.Length; i++)
            {
                var name = _pizzeriaGameObjectFieldNames[i];
                _pizzeriaGameObjectFieldCache[name] = FindInstanceField(typeof(PizzeriaGameManager), name);
            }

            _pizzeriaMikeGameObjectFieldCache.Clear();
            for (var i = 0; i < _pizzeriaMikeGameObjectFieldNames.Length; i++)
            {
                var name = _pizzeriaMikeGameObjectFieldNames[i];
                _pizzeriaMikeGameObjectFieldCache[name] = FindInstanceField(typeof(MikePizzeria), name);
            }

            _roadTripFieldCache.Clear();
            for (var i = 0; i < _roadTripFieldNames.Length; i++)
            {
                var name = _roadTripFieldNames[i];
                _roadTripFieldCache[name] = FindInstanceField(typeof(RoadTripGameManager), name);
            }

            _roadTripMikeFieldCache.Clear();
            for (var i = 0; i < _roadTripMikeFieldNames.Length; i++)
            {
                var name = _roadTripMikeFieldNames[i];
                _roadTripMikeFieldCache[name] = FindInstanceField(typeof(MikeInCar), name);
            }

            _roadTripTruckFieldCache.Clear();
            for (var i = 0; i < _roadTripTruckFieldNames.Length; i++)
            {
                var name = _roadTripTruckFieldNames[i];
                _roadTripTruckFieldCache[name] = FindInstanceField(typeof(MikeTruckInLoopScene), name);
            }
        }

        private void InitializeSceneAdapters()
        {
            _sceneAdapters.Clear();
            _sceneAdapters.Add(new DelegateClientSceneAdapter(
                "Cabin",
                scene => string.Equals(scene, "CabinScene", StringComparison.Ordinal) ||
                         string.Equals(scene, "CabinDarkScene", StringComparison.Ordinal),
                onSceneEnter: () =>
                {
                    _cabinHouseManager = null;
                    _cabinGameManager = null;
                },
                applyStoryFlag: (key, value, allowDefer) =>
                {
                    if (key.StartsWith(CabinHouseFlagPrefix, StringComparison.Ordinal))
                    {
                        return TryApplyCabinHouseFlag(key, value, allowDefer);
                    }

                    if (key.StartsWith(CabinGameFlagPrefix, StringComparison.Ordinal))
                    {
                        return TryApplyCabinGameFlag(key, value, allowDefer);
                    }

                    return false;
                }));

            _sceneAdapters.Add(new DelegateClientSceneAdapter(
                "Pizzeria",
                scene => IsPizzeriaScene(scene),
                onSceneEnter: () =>
                {
                    _pizzeriaGameManager = null;
                    _pizzeriaTruckDoor = null;
                    _pizzeriaMike = null;
                },
                applyStoryFlag: (key, value, allowDefer) => TryApplyPizzeriaFlag(key, value, allowDefer)));

            _sceneAdapters.Add(new DelegateClientSceneAdapter(
                "RoadTrip",
                scene => IsRoadTripScene(scene),
                onSceneEnter: () =>
                {
                    _roadTripGameManager = null;
                    _roadTripMikeInCar = null;
                    _roadTripTruck = null;
                },
                applyStoryFlag: (key, value, allowDefer) => TryApplyRoadTripFlag(key, value, allowDefer)));
        }

        private void OnSceneEnterAdapters(string sceneName)
        {
            for (var i = 0; i < _sceneAdapters.Count; i++)
            {
                var adapter = _sceneAdapters[i];
                if (!adapter.MatchesScene(sceneName))
                {
                    continue;
                }

                adapter.OnSceneEnter();
            }
        }

        private bool TryApplySceneAdapterFlag(string key, int value, bool allowDefer)
        {
            for (var i = 0; i < _sceneAdapters.Count; i++)
            {
                var adapter = _sceneAdapters[i];
                if (adapter.TryApplyStoryFlag(key, value, allowDefer))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryApplyPizzeriaFlag(string key, int value, bool allowDefer)
        {
            if (string.IsNullOrEmpty(key) || !key.StartsWith(PizzeriaFlagPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            if (!IsPizzeriaScene(SceneManager.GetActiveScene().name))
            {
                if (allowDefer)
                {
                    _pendingPizzeriaFlags[key] = value;
                    TrackPendingState(_pendingPizzeriaFirstSeen, key);
                }
                return false;
            }

            if (key.StartsWith(PizzeriaActiveGamePrefix, StringComparison.Ordinal))
            {
                if (_pizzeriaGameManager == null)
                {
                    _pizzeriaGameManager = UnityEngine.Object.FindObjectOfType<PizzeriaGameManager>();
                }

                if (_pizzeriaGameManager == null)
                {
                    if (allowDefer)
                    {
                        _pendingPizzeriaFlags[key] = value;
                        TrackPendingState(_pendingPizzeriaFirstSeen, key);
                    }
                    return false;
                }

                var activeFieldName = key.Substring(PizzeriaActiveGamePrefix.Length);
                if (TryApplyGameObjectActiveFlag(_pizzeriaGameManager, activeFieldName, _pizzeriaGameObjectFieldCache, value))
                {
                    _pendingPizzeriaFlags.Remove(key);
                    ClearPendingState(_pendingPizzeriaFirstSeen, key);
                    return true;
                }

                if (allowDefer)
                {
                    _pendingPizzeriaFlags[key] = value;
                    TrackPendingState(_pendingPizzeriaFirstSeen, key);
                }
                return false;
            }

            if (key.StartsWith(PizzeriaActiveMikePrefix, StringComparison.Ordinal))
            {
                if (!TryEnsurePizzeriaMike(allowDefer, key, value))
                {
                    return false;
                }

                var activeFieldName = key.Substring(PizzeriaActiveMikePrefix.Length);
                if (TryApplyGameObjectActiveFlag(_pizzeriaMike, activeFieldName, _pizzeriaMikeGameObjectFieldCache, value))
                {
                    _pendingPizzeriaFlags.Remove(key);
                    ClearPendingState(_pendingPizzeriaFirstSeen, key);
                    return true;
                }

                if (allowDefer)
                {
                    _pendingPizzeriaFlags[key] = value;
                    TrackPendingState(_pendingPizzeriaFirstSeen, key);
                }
                return false;
            }

            if (key.StartsWith(PizzeriaMikeFlagPrefix, StringComparison.Ordinal))
            {
                if (!TryEnsurePizzeriaMike(allowDefer, key, value))
                {
                    return false;
                }

                var mikeFieldName = key.Substring(PizzeriaMikeFlagPrefix.Length);
                if (TryApplyObjectFieldFlag(_pizzeriaMike, mikeFieldName, _pizzeriaMikeFieldCache, value))
                {
                    _pendingPizzeriaFlags.Remove(key);
                    ClearPendingState(_pendingPizzeriaFirstSeen, key);
                    return true;
                }

                if (allowDefer)
                {
                    _pendingPizzeriaFlags[key] = value;
                    TrackPendingState(_pendingPizzeriaFirstSeen, key);
                }
                return false;
            }

            if (string.Equals(key, PizzeriaFlagPrefix + "TruckDoorInteractable", StringComparison.Ordinal))
            {
                if (_pizzeriaTruckDoor == null)
                {
                    if (_pizzeriaGameManager == null)
                    {
                        _pizzeriaGameManager = UnityEngine.Object.FindObjectOfType<PizzeriaGameManager>();
                    }

                    _pizzeriaTruckDoor = _pizzeriaGameManager != null
                        ? _pizzeriaGameManager.truckDoor
                        : null;
                    if (_pizzeriaTruckDoor == null)
                    {
                        _pizzeriaTruckDoor = UnityEngine.Object.FindObjectOfType<PizzeriaTruckDoor>();
                    }
                }

                if (_pizzeriaTruckDoor == null)
                {
                    if (allowDefer)
                    {
                        _pendingPizzeriaFlags[key] = value;
                        TrackPendingState(_pendingPizzeriaFirstSeen, key);
                    }
                    return false;
                }

                _pizzeriaTruckDoor.isinteractable = value != 0;
                _pendingPizzeriaFlags.Remove(key);
                ClearPendingState(_pendingPizzeriaFirstSeen, key);
                return true;
            }

            if (_pizzeriaGameManager == null)
            {
                _pizzeriaGameManager = UnityEngine.Object.FindObjectOfType<PizzeriaGameManager>();
                if (_pizzeriaGameManager == null)
                {
                    if (allowDefer)
                    {
                        _pendingPizzeriaFlags[key] = value;
                        TrackPendingState(_pendingPizzeriaFirstSeen, key);
                    }
                    return false;
                }
            }

            var fieldName = key.Substring(PizzeriaFlagPrefix.Length);
            if (TryApplyObjectFieldFlag(_pizzeriaGameManager, fieldName, _pizzeriaFieldCache, value))
            {
                _pendingPizzeriaFlags.Remove(key);
                ClearPendingState(_pendingPizzeriaFirstSeen, key);
                return true;
            }

            if (allowDefer)
            {
                _pendingPizzeriaFlags[key] = value;
                TrackPendingState(_pendingPizzeriaFirstSeen, key);
            }
            return false;
        }

        private bool TryApplyRoadTripFlag(string key, int value, bool allowDefer)
        {
            if (string.IsNullOrEmpty(key) || !key.StartsWith(RoadTripFlagPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            if (!IsRoadTripScene(SceneManager.GetActiveScene().name))
            {
                if (allowDefer)
                {
                    _pendingRoadTripFlags[key] = value;
                    TrackPendingState(_pendingRoadTripFirstSeen, key);
                }
                return false;
            }

            if (key.StartsWith(RoadTripMikeFlagPrefix, StringComparison.Ordinal))
            {
                if (_roadTripMikeInCar == null)
                {
                    _roadTripMikeInCar = UnityEngine.Object.FindObjectOfType<MikeInCar>();
                    if (_roadTripMikeInCar == null)
                    {
                        if (allowDefer)
                        {
                            _pendingRoadTripFlags[key] = value;
                            TrackPendingState(_pendingRoadTripFirstSeen, key);
                        }
                        return false;
                    }
                }

                var fieldName = key.Substring(RoadTripMikeFlagPrefix.Length);
                if (TryApplyObjectFieldFlag(_roadTripMikeInCar, fieldName, _roadTripMikeFieldCache, value))
                {
                    _pendingRoadTripFlags.Remove(key);
                    ClearPendingState(_pendingRoadTripFirstSeen, key);
                    return true;
                }

                if (allowDefer)
                {
                    _pendingRoadTripFlags[key] = value;
                    TrackPendingState(_pendingRoadTripFirstSeen, key);
                }
                return false;
            }

            if (key.StartsWith(RoadTripTruckFlagPrefix, StringComparison.Ordinal))
            {
                if (_roadTripTruck == null)
                {
                    _roadTripTruck = UnityEngine.Object.FindObjectOfType<MikeTruckInLoopScene>();
                    if (_roadTripTruck == null)
                    {
                        if (allowDefer)
                        {
                            _pendingRoadTripFlags[key] = value;
                            TrackPendingState(_pendingRoadTripFirstSeen, key);
                        }
                        return false;
                    }
                }

                var fieldName = key.Substring(RoadTripTruckFlagPrefix.Length);
                if (TryApplyObjectFieldFlag(_roadTripTruck, fieldName, _roadTripTruckFieldCache, value))
                {
                    _pendingRoadTripFlags.Remove(key);
                    ClearPendingState(_pendingRoadTripFirstSeen, key);
                    return true;
                }

                if (allowDefer)
                {
                    _pendingRoadTripFlags[key] = value;
                    TrackPendingState(_pendingRoadTripFirstSeen, key);
                }
                return false;
            }

            if (_roadTripGameManager == null)
            {
                _roadTripGameManager = UnityEngine.Object.FindObjectOfType<RoadTripGameManager>();
                if (_roadTripGameManager == null)
                {
                    if (allowDefer)
                    {
                        _pendingRoadTripFlags[key] = value;
                        TrackPendingState(_pendingRoadTripFirstSeen, key);
                    }
                    return false;
                }
            }

            var gameFieldName = key.Substring(RoadTripFlagPrefix.Length);
            if (TryApplyObjectFieldFlag(_roadTripGameManager, gameFieldName, _roadTripFieldCache, value))
            {
                _pendingRoadTripFlags.Remove(key);
                ClearPendingState(_pendingRoadTripFirstSeen, key);
                return true;
            }

            if (allowDefer)
            {
                _pendingRoadTripFlags[key] = value;
                TrackPendingState(_pendingRoadTripFirstSeen, key);
            }
            return false;
        }

        private bool TryEnsurePizzeriaMike(bool allowDefer, string key, int value)
        {
            if (_pizzeriaMike != null)
            {
                return true;
            }

            if (_pizzeriaGameManager == null)
            {
                _pizzeriaGameManager = UnityEngine.Object.FindObjectOfType<PizzeriaGameManager>();
            }

            _pizzeriaMike = _pizzeriaGameManager != null
                ? _pizzeriaGameManager.mikePizzeria
                : null;
            if (_pizzeriaMike == null)
            {
                _pizzeriaMike = UnityEngine.Object.FindObjectOfType<MikePizzeria>();
            }

            if (_pizzeriaMike != null)
            {
                return true;
            }

            if (allowDefer)
            {
                _pendingPizzeriaFlags[key] = value;
                TrackPendingState(_pendingPizzeriaFirstSeen, key);
            }
            return false;
        }

        private bool TryApplyObjectFieldFlag(
            object target,
            string fieldName,
            Dictionary<string, FieldInfo> cache,
            int value)
        {
            if (target == null || string.IsNullOrEmpty(fieldName))
            {
                return false;
            }

            if (!cache.TryGetValue(fieldName, out var field))
            {
                field = FindInstanceField(target.GetType(), fieldName);
                cache[fieldName] = field;
            }

            if (field == null)
            {
                return true;
            }

            try
            {
                if (field.FieldType == typeof(bool))
                {
                    field.SetValue(target, value != 0);
                    return true;
                }

                if (field.FieldType.IsEnum)
                {
                    field.SetValue(target, Enum.ToObject(field.FieldType, value));
                    return true;
                }

                if (field.FieldType == typeof(int))
                {
                    field.SetValue(target, value);
                    return true;
                }

                if (field.FieldType == typeof(byte))
                {
                    field.SetValue(target, (byte)Mathf.Clamp(value, byte.MinValue, byte.MaxValue));
                    return true;
                }

                if (field.FieldType == typeof(float))
                {
                    field.SetValue(target, value / 1000f);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ApplyStoryFlag failed for " + fieldName + ": " + ex.Message);
            }

            LogMissingSceneField(target.GetType(), fieldName + ":" + field.FieldType.Name);
            return false;
        }

        private bool TryApplyGameObjectActiveFlag(
            object target,
            string fieldName,
            Dictionary<string, FieldInfo> cache,
            int value)
        {
            if (target == null || string.IsNullOrEmpty(fieldName))
            {
                return false;
            }

            if (!cache.TryGetValue(fieldName, out var field))
            {
                field = FindInstanceField(target.GetType(), fieldName);
                cache[fieldName] = field;
            }

            if (field == null || field.FieldType != typeof(GameObject))
            {
                LogMissingSceneField(target.GetType(), fieldName + ":GameObject");
                return false;
            }

            try
            {
                var gameObject = field.GetValue(target) as GameObject;
                if (gameObject == null)
                {
                    return false;
                }

                gameObject.SetActive(value != 0);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Apply active scene flag failed for " + fieldName + ": " + ex.Message);
                return false;
            }
        }

        private void LogMissingSceneField(Type type, string fieldName)
        {
            var typeName = type != null ? type.FullName : "?";
            var key = typeName + "." + fieldName;
            if (!_missingSceneFieldLogged.Add(key))
            {
                return;
            }

            _logger.LogWarning("Scene sync field not found or unsupported: " + key);
            _sessionLogWrite?.Invoke("Scene sync field not found or unsupported: " + key);
        }

        private static FieldInfo FindInstanceField(Type type, string name)
        {
            while (type != null)
            {
                var field = type.GetField(
                    name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return field;
                }

                type = type.BaseType;
            }

            return null;
        }

        private static bool IsPizzeriaScene(string sceneName)
        {
            return !string.IsNullOrEmpty(sceneName) &&
                   sceneName.IndexOf("Pizzeria", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsRoadTripScene(string sceneName)
        {
            return !string.IsNullOrEmpty(sceneName) &&
                   sceneName.IndexOf("RoadTrip", StringComparison.OrdinalIgnoreCase) >= 0;
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
            Transform target = null;
            var source = "path";
            var isMike = IsMikePath(state.Path);

            if (!string.IsNullOrEmpty(state.Path))
            {
                target = NetPath.FindByPath(state.Path);
            }

            if (isMike)
            {
                var preferred = GetPreferredMikeTarget();
                if (preferred != null)
                {
                    target = preferred;
                    source = "preferred";
                }
            }

            if (target != null && isMike && !HasRenderable(target))
            {
                var fallback = ResolveAiFallback(state);
                if (fallback != null)
                {
                    target = fallback;
                    source = "fallback";
                }
            }

            if (target == null)
            {
                target = ResolveAiFallback(state);
                if (target != null)
                {
                    source = "fallback";
                }
            }

            if (isMike)
            {
                var needsNearest = target == null || !HasRenderable(target);
                if (!needsNearest)
                {
                    var currentDistance = Vector3.Distance(target.position, state.Position);
                    needsNearest = currentDistance > 6f;
                }

                if (needsNearest && TryResolveNearestMike(state.Position, out var nearest, out var distance, 8f))
                {
                    target = nearest;
                    source = "nearest:" + distance.ToString("0.00");
                }
            }

            if (target == null)
            {
                MaybeLogMissingAi(state.Path);
                return false;
            }

            MaybeLogAiPath(state.Path, target, source);

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
            ApplyAiAnimator(target, state);
            return true;
        }

        private Transform ResolveAiFallback(AiTransformState state)
        {
            if (string.IsNullOrEmpty(state.Path)) return null;

            if (_aiFallbackTargets.TryGetValue(state.Path, out var cached))
            {
                if (cached != null && HasRenderable(cached)) return cached;
                _aiFallbackTargets.Remove(state.Path);
            }

            var leafName = ExtractLeafName(state.Path);
            if (!IsMikePath(state.Path) && leafName.IndexOf("Mike", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return null;
            }

            var candidates = new List<Component>();
            var cabinTarget = GetActiveCabinMikeTarget();
            if (cabinTarget != null)
            {
                candidates.Add(cabinTarget);
            }
            TryAddComponent(candidates, UnityEngine.Object.FindObjectOfType<MikeCabinCookController>());
            TryAddComponent(candidates, UnityEngine.Object.FindObjectOfType<MikeCabin>());
            TryAddComponent(candidates, UnityEngine.Object.FindObjectOfType<MikeFishing>());
            TryAddComponent(candidates, UnityEngine.Object.FindObjectOfType<MikePostEating>());
            TryAddComponent(candidates, UnityEngine.Object.FindObjectOfType<MikeAfterHiding>());
            TryAddComponent(candidates, UnityEngine.Object.FindObjectOfType<MikeRizzlerController>());
            TryAddComponent(candidates, UnityEngine.Object.FindObjectOfType<MikeEndGame>());
            TryAddComponent(candidates, UnityEngine.Object.FindObjectOfType<MikeInCar>());

            Transform best = null;
            Transform bestActive = null;
            var hasLeaf = !string.IsNullOrEmpty(leafName);
            foreach (var candidate in candidates)
            {
                if (candidate == null) continue;
                var transform = candidate.transform;
                if (transform == null) continue;
                if (hasLeaf && transform.name.IndexOf(leafName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (IsActiveRenderable(transform))
                    {
                        bestActive = transform;
                        break;
                    }
                    if (best == null)
                    {
                        best = transform;
                    }
                }
                if (bestActive == null && IsActiveRenderable(transform))
                {
                    bestActive = transform;
                }
                if (best == null)
                {
                    best = transform;
                }
            }

            if (bestActive == null)
            {
                var agents = UnityEngine.Object.FindObjectsOfType<NavMeshAgent>();
                foreach (var agent in agents)
                {
                    if (agent == null) continue;
                    if (agent.gameObject != null &&
                        agent.gameObject.name.IndexOf("Mike", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (IsActiveRenderable(agent.transform))
                        {
                            bestActive = agent.transform;
                            break;
                        }
                        if (best == null)
                        {
                            best = agent.transform;
                        }
                    }
                }
            }

            if (bestActive == null)
            {
                bestActive = best;
            }

            if (bestActive != null)
            {
                if (HasRenderable(bestActive))
                {
                    _aiFallbackTargets[state.Path] = bestActive;
                }
                MaybeLogAiFallback(state.Path, bestActive);
            }

            return bestActive;
        }

        private Transform GetPreferredMikeTarget()
        {
            if (_forcedMikeTarget != null && HasRenderable(_forcedMikeTarget))
            {
                return _forcedMikeTarget;
            }

            var active = GetActiveCabinMikeTarget();
            if (active != null && HasRenderable(active))
            {
                return active;
            }

            return active ?? _forcedMikeTarget;
        }

        private void UpdateMikeVariantFromState()
        {
            if (_cabinGameManager == null)
            {
                _cabinGameManager = UnityEngine.Object.FindObjectOfType<CabinGameManager>();
            }

            if (_cabinGameManager == null) return;

            var desired = ResolveMikeTargetForSequence(out var reason, out var forceActive);
            if (desired == null) return;

            var changed = !ReferenceEquals(_forcedMikeTarget, desired) || _forcedMikeReason != reason;
            _forcedMikeTarget = desired;
            _forcedMikeReason = reason;

            if (forceActive && HasRenderable(desired))
            {
                SetOnlyMikeActive(desired);
                DisableLocalMikeControllers();
            }

            if (changed)
            {
                var now = Time.realtimeSinceStartup;
                if (now >= _nextMikeSyncLogTime)
                {
                    _nextMikeSyncLogTime = now + 5f;
                    _logger.LogInfo("Mike sync: seq=" + _cabinGameManager.CurrentSequence +
                                    " currentMike=" + _cabinGameManager.currentMike +
                                    " target=" + desired.name +
                                    " reason=" + reason +
                                    " active=" + desired.gameObject.activeInHierarchy);
                }
            }

            TryApplyRemoteMikeAnimation();
        }

        private bool TryApplyCabinMikeAnimationFlag(string fieldName, int value)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                return true;
            }

            switch (fieldName)
            {
                case "MikeAnim.StateHash":
                    _remoteMikeAnimStateHash = value;
                    break;
                case "MikeAnim.Loop":
                    _remoteMikeAnimLoop = Mathf.Max(0, value);
                    break;
                case "MikeAnim.Phase10":
                    _remoteMikeAnimPhase10 = Mathf.Clamp(value, 0, 9);
                    break;
                case "MikeAnim.Transition":
                    _remoteMikeAnimTransition = value != 0 ? 1 : 0;
                    break;
                case "MikeAnim.NextStateHash":
                    _remoteMikeAnimNextStateHash = value;
                    break;
                default:
                    return true;
            }

            return TryApplyRemoteMikeAnimation();
        }

        private bool TryApplyRemoteMikeAnimation()
        {
            if (_remoteMikeAnimStateHash == 0)
            {
                return true;
            }

            if (!TryResolveMikeAnimator(out var animator))
            {
                return false;
            }

            var desiredPhase = _remoteMikeAnimPhase10 >= 0
                ? Mathf.Clamp01(_remoteMikeAnimPhase10 / 10f)
                : 0f;

            if (_remoteMikeAnimTransition != 0 &&
                _remoteMikeAnimNextStateHash != 0 &&
                (_appliedMikeAnimTransition == 0 || _appliedMikeAnimNextStateHash != _remoteMikeAnimNextStateHash))
            {
                animator.CrossFade(_remoteMikeAnimNextStateHash, 0.08f, 0, desiredPhase);
            }
            else
            {
                var current = animator.GetCurrentAnimatorStateInfo(0);
                var currentHash = current.fullPathHash != 0 ? current.fullPathHash : current.shortNameHash;
                var currentPhase10 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Repeat(current.normalizedTime, 1f) * 10f), 0, 9);
                var stateChanged = currentHash != _remoteMikeAnimStateHash;
                var phaseDrift = _remoteMikeAnimPhase10 >= 0
                    ? Mathf.Abs(currentPhase10 - _remoteMikeAnimPhase10)
                    : 0;
                if (stateChanged || phaseDrift >= 4)
                {
                    animator.Play(_remoteMikeAnimStateHash, 0, desiredPhase);
                }
            }

            _appliedMikeAnimStateHash = _remoteMikeAnimStateHash;
            _appliedMikeAnimLoop = _remoteMikeAnimLoop;
            _appliedMikeAnimPhase10 = _remoteMikeAnimPhase10;
            _appliedMikeAnimTransition = _remoteMikeAnimTransition;
            _appliedMikeAnimNextStateHash = _remoteMikeAnimNextStateHash;
            return true;
        }

        private bool TryResolveMikeAnimator(out Animator animator)
        {
            animator = null;

            var preferred = GetPreferredMikeTarget();
            if (preferred != null)
            {
                animator = preferred.GetComponentInChildren<Animator>(true);
                if (animator != null)
                {
                    return true;
                }
            }

            var active = GetActiveCabinMikeTarget();
            if (active != null)
            {
                animator = active.GetComponentInChildren<Animator>(true);
                if (animator != null)
                {
                    return true;
                }
            }

            var candidates = new List<Transform>();
            CollectMikeCandidates(candidates);
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate == null) continue;
                animator = candidate.GetComponentInChildren<Animator>(true);
                if (animator != null)
                {
                    return true;
                }
            }

            animator = null;
            return false;
        }

        private Transform ResolveMikeTargetForSequence(out string reason, out bool forceActive)
        {
            reason = string.Empty;
            forceActive = false;

            if (_cabinGameManager == null) return null;

            var seq = _cabinGameManager.CurrentSequence;
            if (seq == SequenceType.PickingBoardGame || seq == SequenceType.PlayingJenga ||
                seq == SequenceType.GoingToPlayOuija || seq == SequenceType.PlayingOuija)
            {
                var controller = GetMikeControllerTransform();
                if (controller != null && HasRenderable(controller))
                {
                    reason = "seq:" + seq;
                    forceActive = true;
                    return controller;
                }

                if (_cabinGameManager.mikeCabin != null)
                {
                    reason = "seq:" + seq + ":cabinfallback";
                    forceActive = true;
                    return _cabinGameManager.mikeCabin.transform;
                }
            }

            if (seq == SequenceType.Fishing)
            {
                var fishing = _cabinGameManager.mikeFishing;
                if (fishing != null)
                {
                    reason = "seq:" + seq;
                    forceActive = true;
                    return fishing.transform;
                }
            }

            var currentMike = _cabinGameManager.currentMike;
            if ((currentMike == CabinGameManager.CurrentMike.Prefishing ||
                 currentMike == CabinGameManager.CurrentMike.PostFishing) &&
                _cabinGameManager.mikeCabin != null)
            {
                reason = "currentMike:" + currentMike;
                forceActive = true;
                return _cabinGameManager.mikeCabin.transform;
            }
            if (currentMike == CabinGameManager.CurrentMike.Fishing && _cabinGameManager.mikeFishing != null)
            {
                reason = "currentMike:" + currentMike;
                forceActive = true;
                return _cabinGameManager.mikeFishing.transform;
            }
            if (currentMike == CabinGameManager.CurrentMike.PostEating && _cabinGameManager.mikePostEating != null)
            {
                reason = "currentMike:" + currentMike;
                forceActive = true;
                return _cabinGameManager.mikePostEating.transform;
            }

            var active = GetActiveCabinMikeTarget();
            if (active != null)
            {
                reason = "active";
                return active;
            }

            return null;
        }

        private void SetOnlyMikeActive(Transform keep)
        {
            if (keep == null) return;

            SetMikeActive(_cabinGameManager != null ? _cabinGameManager.mikeCabin : null, keep);
            SetMikeActive(_cabinGameManager != null ? _cabinGameManager.mikeFishing : null, keep);
            SetMikeActive(_cabinGameManager != null ? _cabinGameManager.mikePostEating : null, keep);
            SetMikeActive(_cabinGameManager != null ? _cabinGameManager.mikeAfterHiding : null, keep);
            SetMikeActive(_cabinGameManager != null ? _cabinGameManager.mikeEnd : null, keep);
            SetMikeActive(GetMikeControllerComponent(), keep);
            SetMikeActive(GetMikeRizzlerComponent(), keep);
            SetMikeActive(UnityEngine.Object.FindObjectOfType<MikeInCar>(), keep);
        }

        private static void SetMikeActive(Component component, Transform keep)
        {
            if (component == null) return;
            SetMikeActive(component.transform, keep);
        }

        private static void SetMikeActive(Transform target, Transform keep)
        {
            if (target == null) return;
            var shouldBeActive = target == keep || target.IsChildOf(keep) || keep.IsChildOf(target);
            if (target.gameObject.activeSelf != shouldBeActive)
            {
                target.gameObject.SetActive(shouldBeActive);
            }
        }

        private Transform GetMikeControllerTransform()
        {
            var controller = GetMikeControllerComponent();
            return controller != null ? controller.transform : null;
        }

        private Component GetMikeControllerComponent()
        {
            if (_cabinGameManager == null) return null;
            var field = typeof(CabinGameManager).GetField("mikeController", BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null ? field.GetValue(_cabinGameManager) as Component : null;
        }

        private Component GetMikeRizzlerComponent()
        {
            if (_cabinGameManager == null) return null;
            var field = typeof(CabinGameManager).GetField("mikeRizzlerController", BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null ? field.GetValue(_cabinGameManager) as Component : null;
        }

        private void ApplyAiAnimator(Transform target, AiTransformState state)
        {
            if (target == null || string.IsNullOrEmpty(state.Path)) return;

            if (!_aiAnimatorCache.TryGetValue(state.Path, out var animator) || animator == null)
            {
                animator = target.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    _aiAnimatorCache[state.Path] = animator;
                }
            }

            if (animator == null) return;

            if (!_aiAnimatorParams.TryGetValue(state.Path, out var driveInfo) || driveInfo == null)
            {
                driveInfo = BuildAnimatorDriveInfo(animator);
                _aiAnimatorParams[state.Path] = driveInfo;
            }

            var now = Time.realtimeSinceStartup;
            var speed = 0f;
            if (_aiLastPositions.TryGetValue(state.Path, out var lastPos) &&
                _aiLastTimes.TryGetValue(state.Path, out var lastTime))
            {
                var deltaTime = Mathf.Max(0.001f, now - lastTime);
                speed = Vector3.Distance(state.Position, lastPos) / deltaTime;
            }

            _aiLastPositions[state.Path] = state.Position;
            _aiLastTimes[state.Path] = now;

            var moving = speed > 0.05f;
            if (!string.IsNullOrEmpty(driveInfo.SpeedFloat))
            {
                animator.SetFloat(driveInfo.SpeedFloat, speed);
            }
            else if (!string.IsNullOrEmpty(driveInfo.MoveFloat))
            {
                animator.SetFloat(driveInfo.MoveFloat, speed);
            }
            else if (!string.IsNullOrEmpty(driveInfo.VelocityFloat))
            {
                animator.SetFloat(driveInfo.VelocityFloat, speed);
            }

            if (!string.IsNullOrEmpty(driveInfo.WalkBool))
            {
                animator.SetBool(driveInfo.WalkBool, moving);
            }
            if (!string.IsNullOrEmpty(driveInfo.MovingBool))
            {
                animator.SetBool(driveInfo.MovingBool, moving);
            }
        }

        private AnimatorDriveInfo BuildAnimatorDriveInfo(Animator animator)
        {
            var info = new AnimatorDriveInfo();
            if (animator == null) return info;

            foreach (var param in animator.parameters)
            {
                var name = param.name;
                var lower = name.ToLowerInvariant();
                if (param.type == AnimatorControllerParameterType.Float)
                {
                    if (info.SpeedFloat == null && lower.Contains("speed"))
                    {
                        info.SpeedFloat = name;
                    }
                    else if (info.MoveFloat == null && lower.Contains("move"))
                    {
                        info.MoveFloat = name;
                    }
                    else if (info.VelocityFloat == null && (lower.Contains("velocity") || lower.Contains("vel")))
                    {
                        info.VelocityFloat = name;
                    }
                }
                else if (param.type == AnimatorControllerParameterType.Bool)
                {
                    if (info.WalkBool == null && lower.Contains("walk"))
                    {
                        info.WalkBool = name;
                    }
                    else if (info.MovingBool == null && (lower.Contains("move") || lower.Contains("moving")))
                    {
                        info.MovingBool = name;
                    }
                }
            }

            return info;
        }

        private static string ExtractLeafName(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            var slash = path.LastIndexOf('/');
            var segment = slash >= 0 ? path.Substring(slash + 1) : path;
            var bracket = segment.LastIndexOf('[');
            return bracket > 0 ? segment.Substring(0, bracket) : segment;
        }

        private void MaybeLogMissingAi(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (_aiMissingLogged.Contains(path)) return;

            var now = Time.realtimeSinceStartup;
            if (now < _nextAiMissingLogTime) return;
            _nextAiMissingLogTime = now + 5f;

            _aiMissingLogged.Add(path);
            _logger.LogWarning("AI target not found for path: " + path);
        }

        private void MaybeLogAiFallback(string path, Transform target)
        {
            if (string.IsNullOrEmpty(path) || target == null) return;
            if (_aiFallbackLogged.Contains(path)) return;

            var now = Time.realtimeSinceStartup;
            if (now < _nextAiFallbackLogTime) return;
            _nextAiFallbackLogTime = now + 5f;

            _aiFallbackLogged.Add(path);
            _logger.LogInfo("AI fallback target: path=" + path + " -> " + target.name +
                            " active=" + target.gameObject.activeInHierarchy);
        }

        private void MaybeLogAiPath(string path, Transform target, string source)
        {
            if (target == null) return;
            if (_aiDebugLogCount >= 8) return;

            var key = string.IsNullOrEmpty(path) ? "(empty)" : path;
            if (_aiDebugPaths.Contains(key)) return;

            var now = Time.realtimeSinceStartup;
            if (now < _nextAiDebugLogTime) return;
            _nextAiDebugLogTime = now + 1f;

            _aiDebugPaths.Add(key);
            _aiDebugLogCount++;
            _logger.LogInfo("AI path: " + key + " -> " + target.name +
                            " src=" + source +
                            " active=" + target.gameObject.activeInHierarchy +
                            " render=" + HasRenderable(target));
        }

        private bool TryResolveNearestMike(Vector3 position, out Transform target, out float distance, float maxDistance)
        {
            target = null;
            distance = float.MaxValue;

            var candidates = new List<Transform>();
            CollectMikeCandidates(candidates);
            foreach (var candidate in candidates)
            {
                if (candidate == null) continue;
                if (!HasRenderable(candidate)) continue;

                var d = Vector3.Distance(position, candidate.position);
                if (d < distance)
                {
                    distance = d;
                    target = candidate;
                }
            }

            return target != null && distance <= maxDistance;
        }

        private void CollectMikeCandidates(List<Transform> results)
        {
            if (results == null) return;

            if (_cabinGameManager == null)
            {
                _cabinGameManager = UnityEngine.Object.FindObjectOfType<CabinGameManager>();
            }

            if (_cabinGameManager != null)
            {
                if (_cabinGameManager.mikeObject != null) results.Add(_cabinGameManager.mikeObject.transform);
                if (_cabinGameManager.mikeCabin != null) results.Add(_cabinGameManager.mikeCabin.transform);
                if (_cabinGameManager.mikeFishing != null) results.Add(_cabinGameManager.mikeFishing.transform);
                if (_cabinGameManager.mikePostEating != null) results.Add(_cabinGameManager.mikePostEating.transform);
                if (_cabinGameManager.mikeAfterHiding != null) results.Add(_cabinGameManager.mikeAfterHiding.transform);
                if (_cabinGameManager.mikeEnd != null) results.Add(_cabinGameManager.mikeEnd.transform);

                var mikeController = GetMikeControllerComponent();
                if (mikeController != null) results.Add(mikeController.transform);
                var mikeRizzler = GetMikeRizzlerComponent();
                if (mikeRizzler != null) results.Add(mikeRizzler.transform);
            }
            else
            {
                TryAddTransform(results, UnityEngine.Object.FindObjectOfType<MikeCabinCookController>());
                TryAddTransform(results, UnityEngine.Object.FindObjectOfType<MikeCabin>());
                TryAddTransform(results, UnityEngine.Object.FindObjectOfType<MikeFishing>());
                TryAddTransform(results, UnityEngine.Object.FindObjectOfType<MikePostEating>());
                TryAddTransform(results, UnityEngine.Object.FindObjectOfType<MikeAfterHiding>());
                TryAddTransform(results, UnityEngine.Object.FindObjectOfType<MikeRizzlerController>());
                TryAddTransform(results, UnityEngine.Object.FindObjectOfType<MikeEndGame>());
                TryAddTransform(results, UnityEngine.Object.FindObjectOfType<MikeInCar>());
            }

            if (results.Count == 0)
            {
                var agents = UnityEngine.Object.FindObjectsOfType<NavMeshAgent>();
                foreach (var agent in agents)
                {
                    if (agent == null || agent.gameObject == null) continue;
                    if (agent.gameObject.name.IndexOf("Mike", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    results.Add(agent.transform);
                }
            }
        }

        private static void TryAddTransform(List<Transform> results, Component component)
        {
            if (component == null || results == null) return;
            results.Add(component.transform);
        }

        private Transform GetActiveCabinMikeTarget()
        {
            if (_cabinGameManager == null)
            {
                _cabinGameManager = UnityEngine.Object.FindObjectOfType<CabinGameManager>();
            }

            if (_cabinGameManager == null) return null;

            var candidates = new List<Transform>();
            if (_cabinGameManager.mikeObject != null) candidates.Add(_cabinGameManager.mikeObject.transform);
            if (_cabinGameManager.mikeCabin != null) candidates.Add(_cabinGameManager.mikeCabin.transform);
            if (_cabinGameManager.mikeFishing != null) candidates.Add(_cabinGameManager.mikeFishing.transform);
            if (_cabinGameManager.mikePostEating != null) candidates.Add(_cabinGameManager.mikePostEating.transform);
            if (_cabinGameManager.mikeAfterHiding != null) candidates.Add(_cabinGameManager.mikeAfterHiding.transform);
            if (_cabinGameManager.mikeEnd != null) candidates.Add(_cabinGameManager.mikeEnd.transform);

            var mikeController = GetMikeControllerComponent();
            if (mikeController != null) candidates.Add(mikeController.transform);

            var mikeRizzler = GetMikeRizzlerComponent();
            if (mikeRizzler != null) candidates.Add(mikeRizzler.transform);

            Transform fallback = null;
            foreach (var candidate in candidates)
            {
                if (candidate == null || !candidate.gameObject.activeInHierarchy) continue;
                if (HasRenderable(candidate))
                {
                    return candidate;
                }
                if (fallback == null)
                {
                    fallback = candidate;
                }
            }

            return fallback;
        }

        private static bool HasRenderable(Transform target)
        {
            if (target == null) return false;
            var renderer = target.GetComponentInChildren<Renderer>(true);
            return renderer != null;
        }

        private static bool IsActiveRenderable(Transform target)
        {
            if (target == null) return false;
            if (!target.gameObject.activeInHierarchy) return false;
            return HasRenderable(target);
        }

        private static bool IsMikePath(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   path.IndexOf("Mike", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void TryAddComponent(List<Component> list, Component component)
        {
            if (component == null) return;
            if (list.Contains(component)) return;
            list.Add(component);
        }

        private sealed class AnimatorDriveInfo
        {
            public string SpeedFloat;
            public string MoveFloat;
            public string VelocityFloat;
            public string WalkBool;
            public string MovingBool;
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
                        ClearPendingState(_pendingDoorFirstSeen, key);
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
                        ClearPendingState(_pendingHoldableFirstSeen, key);
                    }
                }
            }

            if (_pendingCabinHouseFlags.Count > 0)
            {
                var keys = new List<string>(_pendingCabinHouseFlags.Keys);
                foreach (var key in keys)
                {
                    if (TryApplyCabinHouseFlag(key, _pendingCabinHouseFlags[key], allowDefer: false))
                    {
                        _pendingCabinHouseFlags.Remove(key);
                        ClearPendingState(_pendingCabinHouseFirstSeen, key);
                    }
                }
            }

            if (_pendingCabinGameFlags.Count > 0)
            {
                var keys = new List<string>(_pendingCabinGameFlags.Keys);
                foreach (var key in keys)
                {
                    if (TryApplyCabinGameFlag(key, _pendingCabinGameFlags[key], allowDefer: false))
                    {
                        _pendingCabinGameFlags.Remove(key);
                        ClearPendingState(_pendingCabinGameFirstSeen, key);
                    }
                }
            }

            if (_pendingPizzeriaFlags.Count > 0)
            {
                var keys = new List<string>(_pendingPizzeriaFlags.Keys);
                foreach (var key in keys)
                {
                    if (TryApplyPizzeriaFlag(key, _pendingPizzeriaFlags[key], allowDefer: false))
                    {
                        _pendingPizzeriaFlags.Remove(key);
                        ClearPendingState(_pendingPizzeriaFirstSeen, key);
                    }
                }
            }

            if (_pendingRoadTripFlags.Count > 0)
            {
                var keys = new List<string>(_pendingRoadTripFlags.Keys);
                foreach (var key in keys)
                {
                    if (TryApplyRoadTripFlag(key, _pendingRoadTripFlags[key], allowDefer: false))
                    {
                        _pendingRoadTripFlags.Remove(key);
                        ClearPendingState(_pendingRoadTripFirstSeen, key);
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
                        ClearPendingState(_pendingAiFirstSeen, key);
                    }
                }
            }

            MaybeLogPendingRetryAges();
        }

        private static void TrackPendingState(Dictionary<string, float> firstSeen, string key)
        {
            if (firstSeen == null || string.IsNullOrEmpty(key)) return;
            if (firstSeen.ContainsKey(key)) return;
            firstSeen[key] = Time.realtimeSinceStartup;
        }

        private static void ClearPendingState(Dictionary<string, float> firstSeen, string key)
        {
            if (firstSeen == null || string.IsNullOrEmpty(key)) return;
            firstSeen.Remove(key);
        }

        private static float GetMaxPendingAge(Dictionary<string, float> firstSeen, float now)
        {
            if (firstSeen == null || firstSeen.Count == 0) return 0f;

            var maxAge = 0f;
            foreach (var entry in firstSeen)
            {
                var age = now - entry.Value;
                if (age > maxAge)
                {
                    maxAge = age;
                }
            }

            return maxAge;
        }

        private void MaybeLogPendingRetryAges()
        {
            var now = Time.realtimeSinceStartup;
            if (now < _nextPendingRetryLogTime)
            {
                return;
            }

            var maxAge = Mathf.Max(
                GetMaxPendingAge(_pendingDoorFirstSeen, now),
                GetMaxPendingAge(_pendingHoldableFirstSeen, now),
                GetMaxPendingAge(_pendingAiFirstSeen, now),
                GetMaxPendingAge(_pendingCabinHouseFirstSeen, now),
                GetMaxPendingAge(_pendingCabinGameFirstSeen, now),
                GetMaxPendingAge(_pendingPizzeriaFirstSeen, now),
                GetMaxPendingAge(_pendingRoadTripFirstSeen, now));

            if (maxAge < PendingRetryWarnAgeSeconds && !_settings.VerboseLogging.Value)
            {
                return;
            }

            _nextPendingRetryLogTime = now + PendingRetryLogIntervalSeconds;
            var message =
                "Pending retry age: max=" + maxAge.ToString("0.0") + "s" +
                " doors=" + _pendingDoorStates.Count +
                " holdables=" + _pendingHoldableStates.Count +
                " ai=" + _pendingAiStates.Count +
                " cabinHouse=" + _pendingCabinHouseFlags.Count +
                " cabinGame=" + _pendingCabinGameFlags.Count +
                " pizzeria=" + _pendingPizzeriaFlags.Count +
                " roadTrip=" + _pendingRoadTripFlags.Count;
            if (maxAge >= PendingRetryWarnAgeSeconds)
            {
                _logger.LogWarning(message);
            }
            else
            {
                _logger.LogInfo(message);
            }
            _sessionLogWrite?.Invoke(message);
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

        private void EnsureCabinGameManager()
        {
            if (_cabinGameManager != null) return;
            if (SceneManager.GetActiveScene().name != "CabinScene") return;

            _cabinGameManager = UnityEngine.Object.FindObjectOfType<CabinGameManager>();
            if (_cabinGameManager != null)
            {
                UpdateMikeVariantFromState();
            }
        }

        private void SyncMikeVariantIfChanged()
        {
            if (_cabinGameManager == null) return;

            var seq = _cabinGameManager.CurrentSequence;
            var state = _cabinGameManager.currentMike;

            if (!_hasMikeState || seq != _lastMikeSequence || state != _lastMikeState)
            {
                _lastMikeSequence = seq;
                _lastMikeState = state;
                _hasMikeState = true;
                UpdateMikeVariantFromState();
            }
        }

        private void EnsureHostProxy()
        {
            if (_hostProxy != null) return;
            EnsureLocalPlayerRefs();
            if (_localFpc == null && !HasConfiguredRemotePlayerPath()) return;
            _hostProxy = RemotePlayerProxy.Create(
                _settings,
                _localFpc,
                new Color(1f, 0.2f, 0.2f, 0.8f),
                _logger,
                _sessionLogWrite);
        }

        private bool HasConfiguredRemotePlayerPath()
        {
            if (_settings == null)
            {
                return false;
            }

            if (_settings.CoopRemotePlayerPrefabPath != null &&
                !string.IsNullOrWhiteSpace(_settings.CoopRemotePlayerPrefabPath.Value))
            {
                return true;
            }

            return _settings.CoopRemotePlayerAvatarSource != null ||
                   (_settings.CoopRemotePlayerAvatarId != null &&
                    !string.IsNullOrWhiteSpace(_settings.CoopRemotePlayerAvatarId.Value)) ||
                   (_settings.CoopRemotePlayerAvatarBundlePath != null &&
                    !string.IsNullOrWhiteSpace(_settings.CoopRemotePlayerAvatarBundlePath.Value));
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
            _sceneReadySentGeneration = -1;
            _sceneReadyDirty = true;
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
            _forcedMikeTarget = null;
            _forcedMikeReason = string.Empty;
            _mikeControllersDisabled = false;
            _hasMikeState = false;
            _lastMikeSequence = SequenceType.NotInAnySequence;
            _lastMikeState = CabinGameManager.CurrentMike.Prefishing;
            _remoteMikeAnimStateHash = 0;
            _remoteMikeAnimLoop = -1;
            _remoteMikeAnimPhase10 = -1;
            _remoteMikeAnimTransition = 0;
            _remoteMikeAnimNextStateHash = 0;
            _appliedMikeAnimStateHash = 0;
            _appliedMikeAnimLoop = -1;
            _appliedMikeAnimPhase10 = -1;
            _appliedMikeAnimTransition = 0;
            _appliedMikeAnimNextStateHash = 0;
            Interlocked.Exchange(ref _hostStateAppliedCount, 0);
            Interlocked.Exchange(ref _hostTransformAppliedCount, 0);
            _pendingDoorStates.Clear();
            _pendingHoldableStates.Clear();
            _pendingAiStates.Clear();
            _pendingCabinHouseFlags.Clear();
            _pendingCabinGameFlags.Clear();
            _pendingPizzeriaFlags.Clear();
            _pendingRoadTripFlags.Clear();
            _cabinHouseManager = null;
            _cabinGameManager = null;
            _pizzeriaGameManager = null;
            _pizzeriaTruckDoor = null;
            _pizzeriaMike = null;
            _roadTripGameManager = null;
            _roadTripMikeInCar = null;
            _roadTripTruck = null;
            _cabinHouseFieldCache.Clear();
            _cabinGameFieldCache.Clear();
            _pizzeriaFieldCache.Clear();
            _pizzeriaMikeFieldCache.Clear();
            _pizzeriaGameObjectFieldCache.Clear();
            _pizzeriaMikeGameObjectFieldCache.Clear();
            _roadTripFieldCache.Clear();
            _roadTripMikeFieldCache.Clear();
            _roadTripTruckFieldCache.Clear();
            _aiFallbackTargets.Clear();
            _aiMissingLogged.Clear();
            _aiFallbackLogged.Clear();
            _aiDebugPaths.Clear();
            _aiDebugLogCount = 0;
            _nextAiDebugLogTime = 0f;
            _aiLastPositions.Clear();
            _aiLastTimes.Clear();
            _aiAnimatorCache.Clear();
            _aiAnimatorParams.Clear();
            _disabledTriggerBehaviours.Clear();
            _disabledTriggerColliders.Clear();
            _disabledInteractables.Clear();
            _pendingDoorFirstSeen.Clear();
            _pendingHoldableFirstSeen.Clear();
            _pendingAiFirstSeen.Clear();
            _pendingCabinHouseFirstSeen.Clear();
            _pendingCabinGameFirstSeen.Clear();
            _pendingPizzeriaFirstSeen.Clear();
            _pendingRoadTripFirstSeen.Clear();
            _nextPendingRetryLogTime = 0f;
            CacheSceneAdapterFields();
            OnSceneEnterAdapters(newScene.name);
            MarkSceneReadyDirty("active-scene-changed");
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
                if (string.Equals(_lastStartPrefScene, sceneName, StringComparison.Ordinal) &&
                    _lastStartPrefSeq == startSeq &&
                    _lastStartPrefFromMenu == fromMenu)
                {
                    return;
                }

                _lastStartPrefScene = sceneName ?? string.Empty;
                _lastStartPrefSeq = startSeq;
                _lastStartPrefFromMenu = fromMenu;
                var effectiveFromMenu = fromMenu;
                if (startSeq > 0 && effectiveFromMenu <= 0)
                {
                    effectiveFromMenu = 1;
                }
                var changed = false;
                if (effectiveFromMenu >= 0)
                {
                    changed |= SetPlayerPrefIntIfChanged(PlayerPrefKeys.FROM_MENU, effectiveFromMenu);
                }
                if (startSeq >= 0)
                {
                    changed |= SetPlayerPrefIntIfChanged(PlayerPrefKeys.START_SEQ, startSeq);
                }
                if (changed)
                {
                    PlayerPrefs.Save();
                }
                if (changed || _settings.VerboseLogging.Value)
                {
                    _logger.LogInfo("Applied host start prefs (seq=" + startSeq + ", fromMenu=" + fromMenu + " -> " + effectiveFromMenu + ", changed=" + changed + ")");
                }
                _cabinPrefsPrepared = true;
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

        private static bool SetPlayerPrefIntIfChanged(string key, int value)
        {
            if (string.IsNullOrEmpty(key)) return false;
            if (PlayerPrefs.HasKey(key) && PlayerPrefs.GetInt(key) == value)
            {
                return false;
            }

            PlayerPrefs.SetInt(key, value);
            return true;
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
                        cabin.EndConvoWithMike();
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

        private void DisableLocalMikeControllers()
        {
            if (_mikeControllersDisabled) return;

            DisableMikeBehaviour(UnityEngine.Object.FindObjectOfType<MikeCabinCookController>());
            DisableMikeBehaviour(UnityEngine.Object.FindObjectOfType<MikeCabin>());
            DisableMikeBehaviour(UnityEngine.Object.FindObjectOfType<MikeFishing>());
            DisableMikeBehaviour(UnityEngine.Object.FindObjectOfType<MikePostEating>());
            DisableMikeBehaviour(UnityEngine.Object.FindObjectOfType<MikeAfterHiding>());
            DisableMikeBehaviour(UnityEngine.Object.FindObjectOfType<MikeRizzlerController>());
            DisableMikeBehaviour(UnityEngine.Object.FindObjectOfType<MikeEndGame>());
            DisableMikeBehaviour(UnityEngine.Object.FindObjectOfType<MikeInCar>());

            _mikeControllersDisabled = true;
        }

        private static void DisableMikeBehaviour(MonoBehaviour behaviour)
        {
            if (behaviour == null) return;
            if (!behaviour.enabled) return;
            behaviour.enabled = false;
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
            if (_settings.VerboseLogging.Value && now >= _nextHostTransformApplyLogTime)
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
