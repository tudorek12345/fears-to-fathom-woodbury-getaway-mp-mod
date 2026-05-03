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
        private string _deferredScene;
        private int _deferredSceneIndex = -1;
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
        private float _nextSceneLoadProgressLogTime;
        private bool _sceneLoadTimeoutLogged;
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
        private readonly Dictionary<string, FieldInfo> _cabinPostEatingFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _cabinShedFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _cabinUnderstairsFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _cabinHostHidingFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _cabinHikerFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _cabinHikerControllerFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _cabinHostFixingSinkFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _cabinMikeAfterHidingFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
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
        private readonly Dictionary<string, SmoothedAiState> _smoothedAiStates = new Dictionary<string, SmoothedAiState>(StringComparer.Ordinal);
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
        private const string CabinHouseActivePrefix = CabinHouseFlagPrefix + "Active.";
        private const string CabinPostEatingFlagPrefix = CabinGameFlagPrefix + "MikePostEating.";
        private const string CabinPostEatingActivePrefix = CabinPostEatingFlagPrefix + "Active.";
        private const string CabinShedFlagPrefix = CabinGameFlagPrefix + "Shed.";
        private const string CabinShedActivePrefix = CabinShedFlagPrefix + "Active.";
        private const string CabinUnderstairsFlagPrefix = CabinGameFlagPrefix + "Understairs.";
        private const string CabinUnderstairsActivePrefix = CabinUnderstairsFlagPrefix + "Active.";
        private const string CabinHostHidingFlagPrefix = CabinGameFlagPrefix + "HostHiding.";
        private const string CabinHikerFlagPrefix = CabinGameFlagPrefix + "CabinHiker.";
        private const string CabinHikerControllerFlagPrefix = CabinGameFlagPrefix + "HikerController.";
        private const string CabinHostFixingSinkFlagPrefix = CabinGameFlagPrefix + "HostFixingSink.";
        private const string CabinMikeAfterHidingFlagPrefix = CabinGameFlagPrefix + "MikeAfterHiding.";
        private const string CabinHikerActivePrefix = CabinGameFlagPrefix + "HikerActive.";
        private const string PizzeriaFlagPrefix = "PizzeriaGM.";
        private const string PizzeriaMikeFlagPrefix = PizzeriaFlagPrefix + "Mike.";
        private const string PizzeriaActiveGamePrefix = PizzeriaFlagPrefix + "Active.Game.";
        private const string PizzeriaActiveMikePrefix = PizzeriaFlagPrefix + "Active.Mike.";
        private const string RoadTripFlagPrefix = "RoadTripGM.";
        private const string RoadTripMikeFlagPrefix = RoadTripFlagPrefix + "Mike.";
        private const string RoadTripTruckFlagPrefix = RoadTripFlagPrefix + "Truck.";
        private int _loadingSceneIndex = -1;
        private const float LoadingTimeoutWarnSeconds = 45f;
        private const float LoadingStallSeconds = 6f;
        private const float LoadingProgressLogIntervalSeconds = 5f;
        private const int MaxMessagesPerFrame = 200;
        private const int MaxTransformsPerFrame = 60;
        private const float PendingRetryLogIntervalSeconds = 5f;
        private const float PendingRetryWarnAgeSeconds = 8f;
        private const float AiSnapDistanceMeters = 4.0f;
        private const float AiStaleSnapSeconds = 1.0f;
        private const float AiGeneralSmoothing = 13f;
        private const float AiMikeSmoothing = 18f;
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
        private string _lastMikeSyncDebug = "-";
        private string _lastCabinHidingDebug = "-";
        private float _nextCabinHidingLogTime;
        private float _lastScriptedHostSnapTime;
        private bool _hostSceneScriptedLock;
        private int _hostCabinPlayerState = -1;
        private bool _hostCabinPlayerTalking;
        private bool _hostCabinInConversation;
        private bool _hostCabinPhoneUi;
        private int _hostPizzeriaPlayerState = -1;
        private bool _hostPizzeriaPlayerTalking;
        private bool _hostPizzeriaPhoneUi;
        private bool _hostPizzeriaMikeDialogue;
        private bool _hostRoadTripPlayerTalking;
        private bool _hostRoadTripPhoneUi;
        private bool _hostRoadTripMikeDialogue;
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
        private readonly Dictionary<string, float> _cabinRuntimeSyncLogTimes = new Dictionary<string, float>(StringComparer.Ordinal);
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
        public string PendingSceneName
        {
            get
            {
                if (!string.IsNullOrEmpty(_pendingScene)) return _pendingScene;
                if (!string.IsNullOrEmpty(_deferredScene)) return _deferredScene + " (deferred)";
                return string.Empty;
            }
        }
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
        public string LastMikeSyncDebug => _lastMikeSyncDebug;

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
            StabilizeLocalCabinPlayerState();
            StabilizeLocalCabinHouseState();
            SyncMikeVariantIfChanged();
            ApplyCabinSequenceVisuals();
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
            UpdateSmoothedAiStates();
            UnlockLocalDialogueCamera();
            MaybeTeleportToHost();
            SendPingIfDue();

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
            var activeSceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "CabinScene" || sceneName == "CabinDarkScene")
            {
                _pendingStartSeq = sceneName == "CabinScene" ? startSeq : -1;
                PrepareCabinStartPrefs(sceneName, startSeq, fromMenu);
            }
            else
            {
                _pendingStartSeq = -1;
            }

            if (_isLoading && string.Equals(sceneName, _loadingSceneName, StringComparison.Ordinal))
            {
                _pendingScene = null;
                _pendingSceneIndex = -1;
                if (_settings.VerboseLogging.Value)
                {
                    _logger.LogInfo("Scene load request ignored; already loading " + sceneName +
                        " progress=" + LoadingProgress.ToString("0.00"));
                }
                return;
            }

            if (ShouldDeferBootstrapScene(activeSceneName, sceneName))
            {
                var alreadyDeferred = string.Equals(_deferredScene, sceneName, StringComparison.Ordinal);
                _deferredScene = sceneName;
                _deferredSceneIndex = buildIndex;
                _pendingScene = null;
                _pendingSceneIndex = -1;
                _lastSceneReadySent = string.Empty;
                if (!alreadyDeferred)
                {
                    _logger.LogInfo("Co-op client deferring " + sceneName +
                        " load while game bootstrap is already leaving " + activeSceneName);
                }
                MarkSceneReadyDirty("host-scene-bootstrap-deferred");
                return;
            }

            _deferredScene = null;
            _deferredSceneIndex = -1;

            if (sceneName == activeSceneName)
            {
                MarkSceneReadyDirty("host-scene-mismatch-recovery");
                return;
            }

            if (string.Equals(sceneName, _pendingScene, StringComparison.Ordinal))
            {
                _pendingSceneIndex = buildIndex;
                return;
            }

            _pendingScene = sceneName;
            _pendingSceneIndex = buildIndex;
            _lastSceneReadySent = string.Empty;
            MarkSceneReadyDirty("host-scene-change");
        }

        private static bool ShouldDeferBootstrapScene(string activeSceneName, string requestedSceneName)
        {
            return string.Equals(activeSceneName, "Disclaimer", StringComparison.Ordinal) &&
                   string.Equals(requestedSceneName, "MainMenu", StringComparison.Ordinal);
        }

        private void UpdateSceneLoad()
        {
            if (_isLoading)
            {
                if (_loading == null)
                {
                    _logger.LogWarning("Scene load operation missing for " + _loadingSceneName +
                        "; clearing load state without sync fallback");
                    _isLoading = false;
                    _loadingSceneName = null;
                    _loadingSceneIndex = -1;
                    return;
                }

                if (_loading != null)
                {
                    var now = Time.realtimeSinceStartup;
                    var progress = _loading.progress;
                    if (_loading.progress > _loadingLastProgress + 0.001f)
                    {
                        _loadingLastProgress = progress;
                        _loadingLastProgressTime = now;
                    }

                    if (now >= _nextSceneLoadProgressLogTime)
                    {
                        _nextSceneLoadProgressLogTime = now + LoadingProgressLogIntervalSeconds;
                        _logger.LogInfo("Co-op scene load progress scene=" + _loadingSceneName +
                            " index=" + _loadingSceneIndex +
                            " progress=" + (progress * 100f).ToString("0") + "%" +
                            " elapsed=" + (now - _loadingStartTime).ToString("0.0") + "s" +
                            " stalledFor=" + (now - _loadingLastProgressTime).ToString("0.0") + "s" +
                            " allowActivation=" + _loading.allowSceneActivation);
                    }

                    if (!_loading.allowSceneActivation && progress >= 0.9f)
                    {
                        _logger.LogInfo("Co-op scene load reached activation threshold scene=" + _loadingSceneName);
                        _loading.allowSceneActivation = true;
                    }

                    if (!_loading.isDone && now - _loadingLastProgressTime > LoadingStallSeconds && progress >= 0.9f)
                    {
                        _loading.allowSceneActivation = true;
                    }

                    if (!_loading.isDone &&
                        !_sceneLoadTimeoutLogged &&
                        now - _loadingStartTime > LoadingTimeoutWarnSeconds)
                    {
                        _sceneLoadTimeoutLogged = true;
                        _logger.LogWarning("Co-op scene load still pending scene=" + _loadingSceneName +
                            " index=" + _loadingSceneIndex +
                            " progress=" + (progress * 100f).ToString("0") + "%" +
                            " elapsed=" + (now - _loadingStartTime).ToString("0.0") + "s" +
                            "; leaving async load active");
                    }

                    if (_loading.isDone)
                    {
                        _logger.LogInfo("Co-op scene load completed scene=" + _loadingSceneName);
                        _isLoading = false;
                        _loading = null;
                        _loadingSceneName = null;
                        _loadingSceneIndex = -1;
                        _nextSceneLoadProgressLogTime = 0f;
                        _sceneLoadTimeoutLogged = false;
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
                _nextSceneLoadProgressLogTime = _loadingStartTime;
                _sceneLoadTimeoutLogged = false;
                _loading = StartSceneLoad(_pendingScene, _pendingSceneIndex);
                _pendingScene = null;
                _pendingSceneIndex = -1;
            }
        }

        private void SendSceneReadyIfNeeded()
        {
            if (_isLoading || !string.IsNullOrEmpty(_pendingScene)) return;
            if (!string.IsNullOrEmpty(_deferredScene) &&
                !string.Equals(SceneManager.GetActiveScene().name, _deferredScene, StringComparison.Ordinal))
            {
                return;
            }
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
            _pendingScene = null;
            _pendingSceneIndex = -1;
            _deferredScene = null;
            _deferredSceneIndex = -1;
            if (_isLoading && _loading != null)
            {
                _loadingStartTime = Time.realtimeSinceStartup;
                _loadingLastProgress = _loading.progress;
                _loadingLastProgressTime = _loadingStartTime;
                _nextSceneLoadProgressLogTime = _loadingStartTime;
                _sceneLoadTimeoutLogged = false;
            }
            else
            {
                _loading = null;
                _isLoading = false;
                _loadingStartTime = 0f;
                _loadingLastProgress = 0f;
                _loadingLastProgressTime = 0f;
                _nextSceneLoadProgressLogTime = 0f;
                _sceneLoadTimeoutLogged = false;
                _loadingSceneName = null;
                _loadingSceneIndex = -1;
            }
            _pendingDoorStates.Clear();
            _pendingHoldableStates.Clear();
            _pendingAiStates.Clear();
            _smoothedAiStates.Clear();
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

            if (!IsHighFrequencyStoryFlag(key))
            {
                _lastStoryEventKey = key;
                _lastStoryEventValue = flag.Value;
                _lastStoryEventMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
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
            if (string.IsNullOrEmpty(state.Path))
            {
                return;
            }

            if (IsCoopProxyPath(state.Path))
            {
                _pendingAiStates.Remove(state.Path);
                ClearPendingState(_pendingAiFirstSeen, state.Path);
                return;
            }

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

            var fieldName = key.Substring(CabinHouseFlagPrefix.Length);
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

            if (key.StartsWith(CabinHouseActivePrefix, StringComparison.Ordinal))
            {
                var activeFieldName = key.Substring(CabinHouseActivePrefix.Length);
                if (TryApplyCabinHouseActiveFlag(activeFieldName, value))
                {
                    _pendingCabinHouseFlags.Remove(key);
                    ClearPendingState(_pendingCabinHouseFirstSeen, key);
                    return true;
                }

                if (allowDefer)
                {
                    _pendingCabinHouseFlags[key] = value;
                    TrackPendingState(_pendingCabinHouseFirstSeen, key);
                }
                return false;
            }

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

            var fieldName = key.Substring(CabinGameFlagPrefix.Length);
            if (IsClientLocalCabinRuntimeField(fieldName))
            {
                StoreHostCabinRuntimeField(fieldName, value);
                LogSuppressedCabinRuntimeSync("host-only:" + fieldName + "=" + value);
                StabilizeLocalCabinPlayerState();
                _pendingCabinGameFlags.Remove(key);
                ClearPendingState(_pendingCabinGameFirstSeen, key);
                return true;
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

            if (key.StartsWith(CabinPostEatingFlagPrefix, StringComparison.Ordinal))
            {
                if (TryApplyCabinPostEatingFlag(key.Substring(CabinPostEatingFlagPrefix.Length), value))
                {
                    _pendingCabinGameFlags.Remove(key);
                    ClearPendingState(_pendingCabinGameFirstSeen, key);
                    return true;
                }

                if (allowDefer)
                {
                    _pendingCabinGameFlags[key] = value;
                    TrackPendingState(_pendingCabinGameFirstSeen, key);
                }
                return false;
            }

            if (key.StartsWith(CabinShedFlagPrefix, StringComparison.Ordinal))
            {
                if (TryApplyCabinShedFlag(key.Substring(CabinShedFlagPrefix.Length), value))
                {
                    _pendingCabinGameFlags.Remove(key);
                    ClearPendingState(_pendingCabinGameFirstSeen, key);
                    return true;
                }

                if (allowDefer)
                {
                    _pendingCabinGameFlags[key] = value;
                    TrackPendingState(_pendingCabinGameFirstSeen, key);
                }
                return false;
            }

            if (key.StartsWith(CabinUnderstairsFlagPrefix, StringComparison.Ordinal))
            {
                if (TryApplyCabinUnderstairsFlag(key.Substring(CabinUnderstairsFlagPrefix.Length), value))
                {
                    _pendingCabinGameFlags.Remove(key);
                    ClearPendingState(_pendingCabinGameFirstSeen, key);
                    return true;
                }

                if (allowDefer)
                {
                    _pendingCabinGameFlags[key] = value;
                    TrackPendingState(_pendingCabinGameFirstSeen, key);
                }
                return false;
            }

            if (key.StartsWith(CabinHostHidingFlagPrefix, StringComparison.Ordinal))
            {
                if (TryApplyCabinHostHidingFlag(key.Substring(CabinHostHidingFlagPrefix.Length), value))
                {
                    _pendingCabinGameFlags.Remove(key);
                    ClearPendingState(_pendingCabinGameFirstSeen, key);
                    return true;
                }

                if (allowDefer)
                {
                    _pendingCabinGameFlags[key] = value;
                    TrackPendingState(_pendingCabinGameFirstSeen, key);
                }
                return false;
            }

            if (key.StartsWith(CabinHikerControllerFlagPrefix, StringComparison.Ordinal))
            {
                if (TryApplyCabinHikerControllerFlag(key.Substring(CabinHikerControllerFlagPrefix.Length), value))
                {
                    _pendingCabinGameFlags.Remove(key);
                    ClearPendingState(_pendingCabinGameFirstSeen, key);
                    return true;
                }

                if (allowDefer)
                {
                    _pendingCabinGameFlags[key] = value;
                    TrackPendingState(_pendingCabinGameFirstSeen, key);
                }
                return false;
            }

            if (key.StartsWith(CabinHikerFlagPrefix, StringComparison.Ordinal))
            {
                if (TryApplyCabinHikerFlag(key.Substring(CabinHikerFlagPrefix.Length), value))
                {
                    _pendingCabinGameFlags.Remove(key);
                    ClearPendingState(_pendingCabinGameFirstSeen, key);
                    return true;
                }

                if (allowDefer)
                {
                    _pendingCabinGameFlags[key] = value;
                    TrackPendingState(_pendingCabinGameFirstSeen, key);
                }
                return false;
            }

            if (key.StartsWith(CabinHostFixingSinkFlagPrefix, StringComparison.Ordinal))
            {
                if (TryApplyCabinHostFixingSinkFlag(key.Substring(CabinHostFixingSinkFlagPrefix.Length), value))
                {
                    _pendingCabinGameFlags.Remove(key);
                    ClearPendingState(_pendingCabinGameFirstSeen, key);
                    return true;
                }

                if (allowDefer)
                {
                    _pendingCabinGameFlags[key] = value;
                    TrackPendingState(_pendingCabinGameFirstSeen, key);
                }
                return false;
            }

            if (key.StartsWith(CabinMikeAfterHidingFlagPrefix, StringComparison.Ordinal))
            {
                if (TryApplyCabinMikeAfterHidingFlag(key.Substring(CabinMikeAfterHidingFlagPrefix.Length), value))
                {
                    _pendingCabinGameFlags.Remove(key);
                    ClearPendingState(_pendingCabinGameFirstSeen, key);
                    return true;
                }

                if (allowDefer)
                {
                    _pendingCabinGameFlags[key] = value;
                    TrackPendingState(_pendingCabinGameFirstSeen, key);
                }
                return false;
            }

            if (key.StartsWith(CabinHikerActivePrefix, StringComparison.Ordinal))
            {
                if (TryApplyCabinHikerActiveFlag(key.Substring(CabinHikerActivePrefix.Length), value))
                {
                    _pendingCabinGameFlags.Remove(key);
                    ClearPendingState(_pendingCabinGameFirstSeen, key);
                    return true;
                }

                if (allowDefer)
                {
                    _pendingCabinGameFlags[key] = value;
                    TrackPendingState(_pendingCabinGameFirstSeen, key);
                }
                return false;
            }

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

            ApplyCabinSequenceVisuals();

            _pendingCabinGameFlags.Remove(key);
            ClearPendingState(_pendingCabinGameFirstSeen, key);

            return true;
        }

        private bool TryApplyCabinHouseActiveFlag(string fieldName, int value)
        {
            if (_cabinHouseManager == null)
            {
                _cabinHouseManager = UnityEngine.Object.FindObjectOfType<CabinHouseManager>();
            }

            if (_cabinHouseManager == null) return false;
            return TryApplyGameObjectOrArrayActiveFlag(_cabinHouseManager, fieldName, _cabinHouseFieldCache, value);
        }

        private bool TryApplyCabinPostEatingFlag(string fieldName, int value)
        {
            if (!TryEnsureCabinGameManager()) return false;

            var mike = _cabinGameManager.mikePostEating;
            if (mike == null)
            {
                mike = UnityEngine.Object.FindObjectOfType<MikePostEating>();
            }

            if (mike == null) return false;

            if (string.Equals(fieldName, "Active", StringComparison.Ordinal))
            {
                mike.gameObject.SetActive(value != 0);
                if (value != 0)
                {
                    ForceMikePostEatingTarget(mike, "postEating:active");
                }
                MaybeLogCabinHidingMirror(mike);
                return true;
            }

            if (fieldName.StartsWith("Active.", StringComparison.Ordinal))
            {
                var activeFieldName = fieldName.Substring("Active.".Length);
                var applied = TryApplyGameObjectOrArrayActiveFlag(mike, activeFieldName, _cabinPostEatingFieldCache, value);
                MaybeLogCabinHidingMirror(mike);
                return applied;
            }

            if (!TryApplyObjectFieldFlag(mike, fieldName, _cabinPostEatingFieldCache, value))
            {
                return false;
            }

            if (ShouldPreferPostEatingMike(mike))
            {
                ForceMikePostEatingTarget(mike, "postEating:" + mike.state);
            }

            MaybeLogCabinHidingMirror(mike);
            return true;
        }

        private bool TryApplyCabinShedFlag(string fieldName, int value)
        {
            if (!TryEnsureCabinHouseManager()) return false;

            var shed = _cabinHouseManager.shedManager;
            if (shed == null)
            {
                shed = UnityEngine.Object.FindObjectOfType<ShedManager>();
            }

            if (shed == null) return false;

            if (fieldName.StartsWith("Active.", StringComparison.Ordinal))
            {
                return TryApplyGameObjectOrArrayActiveFlag(shed, fieldName.Substring("Active.".Length), _cabinShedFieldCache, value);
            }

            var applied = TryApplyObjectFieldFlag(shed, fieldName, _cabinShedFieldCache, value);
            MaybeLogCabinHidingMirror(_cabinGameManager != null ? _cabinGameManager.mikePostEating : null);
            return applied;
        }

        private bool TryApplyCabinUnderstairsFlag(string fieldName, int value)
        {
            if (!TryEnsureCabinHouseManager()) return false;

            var understairs = _cabinHouseManager.understairsDoor;
            if (understairs == null)
            {
                understairs = UnityEngine.Object.FindObjectOfType<UnderStairsDoor>();
            }

            if (understairs == null) return false;

            if (fieldName.StartsWith("Active.", StringComparison.Ordinal))
            {
                return TryApplyGameObjectOrArrayActiveFlag(understairs, fieldName.Substring("Active.".Length), _cabinUnderstairsFieldCache, value);
            }

            var applied = TryApplyObjectFieldFlag(understairs, fieldName, _cabinUnderstairsFieldCache, value);
            MaybeLogCabinHidingMirror(_cabinGameManager != null ? _cabinGameManager.mikePostEating : null);
            return applied;
        }

        private bool TryApplyCabinHostHidingFlag(string fieldName, int value)
        {
            if (!TryEnsureCabinGameManager()) return false;

            var host = _cabinGameManager.hostHiding;
            if (host == null)
            {
                host = UnityEngine.Object.FindObjectOfType<HostDuringHiding>();
            }

            if (host == null) return false;

            if (string.Equals(fieldName, "Active", StringComparison.Ordinal))
            {
                host.gameObject.SetActive(value != 0);
                MaybeLogCabinHidingMirror(_cabinGameManager.mikePostEating);
                return true;
            }

            var applied = TryApplyObjectFieldFlag(host, fieldName, _cabinHostHidingFieldCache, value);
            MaybeLogCabinHidingMirror(_cabinGameManager.mikePostEating);
            return applied;
        }

        private bool TryApplyCabinHikerFlag(string fieldName, int value)
        {
            if (!TryEnsureCabinGameManager()) return false;

            var hiker = _cabinGameManager.cabinHiker;
            if (hiker == null)
            {
                hiker = UnityEngine.Object.FindObjectOfType<CabinHiker>();
            }

            if (hiker == null) return false;

            if (string.Equals(fieldName, "Active", StringComparison.Ordinal))
            {
                hiker.gameObject.SetActive(value != 0);
                return true;
            }

            return TryApplyObjectFieldFlag(hiker, fieldName, _cabinHikerFieldCache, value);
        }

        private bool TryApplyCabinHikerControllerFlag(string fieldName, int value)
        {
            if (!TryEnsureCabinGameManager()) return false;

            var hikerCtl = UnityEngine.Object.FindObjectOfType<HikerCabinController>();
            if (hikerCtl == null) return false;

            if (string.Equals(fieldName, "Active", StringComparison.Ordinal))
            {
                hikerCtl.gameObject.SetActive(value != 0);
                return true;
            }

            return TryApplyObjectFieldFlag(hikerCtl, fieldName, _cabinHikerControllerFieldCache, value);
        }

        private bool TryApplyCabinHostFixingSinkFlag(string fieldName, int value)
        {
            if (!TryEnsureCabinGameManager()) return false;

            var fixing = _cabinGameManager.hostFixingSink;
            if (fixing == null)
            {
                fixing = UnityEngine.Object.FindObjectOfType<HostFixingSink>();
            }

            if (fixing == null) return false;

            if (string.Equals(fieldName, "Active", StringComparison.Ordinal))
            {
                fixing.gameObject.SetActive(value != 0);
                return true;
            }

            return TryApplyObjectFieldFlag(fixing, fieldName, _cabinHostFixingSinkFieldCache, value);
        }

        private bool TryApplyCabinMikeAfterHidingFlag(string fieldName, int value)
        {
            if (!TryEnsureCabinGameManager()) return false;

            var afterHiding = _cabinGameManager.mikeAfterHiding;
            if (afterHiding == null)
            {
                afterHiding = UnityEngine.Object.FindObjectOfType<MikeAfterHiding>();
            }

            if (afterHiding == null) return false;

            if (string.Equals(fieldName, "Active", StringComparison.Ordinal))
            {
                afterHiding.gameObject.SetActive(value != 0);
                return true;
            }

            return TryApplyObjectFieldFlag(afterHiding, fieldName, _cabinMikeAfterHidingFieldCache, value);
        }

        private bool TryApplyCabinHikerActiveFlag(string fieldName, int value)
        {
            if (!TryEnsureCabinGameManager()) return false;
            if (string.IsNullOrEmpty(fieldName)) return false;

            if (string.Equals(fieldName, "hikerConvoTriggerOnDoor", StringComparison.Ordinal))
            {
                var hikerCtl = UnityEngine.Object.FindObjectOfType<HikerCabinController>();
                if (hikerCtl == null) return false;
                return TryApplyComponentGameObjectActive(hikerCtl, "hikerConvoTriggerOnDoor", _cabinHikerControllerFieldCache, value);
            }

            if (string.Equals(fieldName, "hikerConvoTrigger", StringComparison.Ordinal))
            {
                return TryApplyComponentGameObjectActive(_cabinGameManager, "hikerConvoTrigger", _cabinHikerFieldCache, value);
            }

            // sinisterAudioTrigger and closetLight are GameObject fields directly on CabinGameManager
            return TryApplyGameObjectActiveOnTarget(_cabinGameManager, fieldName, _cabinHikerFieldCache, value);
        }

        private bool TryApplyGameObjectActiveOnTarget(
            object target,
            string fieldName,
            Dictionary<string, FieldInfo> cache,
            int value)
        {
            if (target == null || string.IsNullOrEmpty(fieldName)) return false;

            if (!cache.TryGetValue(fieldName, out var field))
            {
                field = FindInstanceField(target.GetType(), fieldName);
                cache[fieldName] = field;
            }

            if (field == null || field.FieldType != typeof(GameObject))
            {
                return false;
            }

            try
            {
                var go = field.GetValue(target) as GameObject;
                if (go == null) return false;
                go.SetActive(value != 0);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ApplyHikerActiveFlag failed: " + fieldName + " (" + ex.Message + ")");
                return false;
            }
        }

        private bool TryApplyComponentGameObjectActive(
            object target,
            string fieldName,
            Dictionary<string, FieldInfo> cache,
            int value)
        {
            if (target == null || string.IsNullOrEmpty(fieldName)) return false;

            if (!cache.TryGetValue(fieldName, out var field))
            {
                field = FindInstanceField(target.GetType(), fieldName);
                cache[fieldName] = field;
            }

            if (field == null) return false;

            try
            {
                var raw = field.GetValue(target);
                GameObject go = null;
                if (raw is GameObject direct)
                {
                    go = direct;
                }
                else if (raw is Component component)
                {
                    go = component.gameObject;
                }

                if (go == null) return false;
                go.SetActive(value != 0);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ApplyHikerActiveFlag (component) failed: " + fieldName + " (" + ex.Message + ")");
                return false;
            }
        }

        private bool TryEnsureCabinGameManager()
        {
            if (SceneManager.GetActiveScene().name != "CabinScene") return false;
            if (_cabinGameManager == null)
            {
                _cabinGameManager = UnityEngine.Object.FindObjectOfType<CabinGameManager>();
            }

            return _cabinGameManager != null;
        }

        private bool TryEnsureCabinHouseManager()
        {
            if (SceneManager.GetActiveScene().name != "CabinScene") return false;
            if (_cabinHouseManager == null)
            {
                _cabinHouseManager = UnityEngine.Object.FindObjectOfType<CabinHouseManager>();
            }

            if (_cabinHouseManager == null && _cabinGameManager != null)
            {
                _cabinHouseManager = _cabinGameManager.cabinHouseManager;
            }

            return _cabinHouseManager != null;
        }

        private bool TryApplyGameObjectOrArrayActiveFlag(
            object target,
            string fieldName,
            Dictionary<string, FieldInfo> cache,
            int value)
        {
            if (target == null || string.IsNullOrEmpty(fieldName)) return false;

            if (!cache.TryGetValue(fieldName, out var field))
            {
                field = FindInstanceField(target.GetType(), fieldName);
                cache[fieldName] = field;
            }

            if (field == null)
            {
                LogMissingSceneField(target.GetType(), fieldName + ":GameObject");
                return false;
            }

            try
            {
                if (field.FieldType == typeof(GameObject))
                {
                    var gameObject = field.GetValue(target) as GameObject;
                    if (gameObject == null) return false;
                    gameObject.SetActive(value != 0);
                    return true;
                }

                if (field.FieldType == typeof(GameObject[]))
                {
                    var gameObjects = field.GetValue(target) as GameObject[];
                    if (gameObjects == null) return false;
                    for (var i = 0; i < gameObjects.Length; i++)
                    {
                        if (gameObjects[i] != null)
                        {
                            gameObjects[i].SetActive(value != 0);
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Apply active Cabin flag failed for " + fieldName + ": " + ex.Message);
                return false;
            }

            LogMissingSceneField(target.GetType(), fieldName + ":" + field.FieldType.Name);
            return false;
        }

        private void StoreHostCabinRuntimeField(string fieldName, int value)
        {
            if (string.Equals(fieldName, "currentPlayerState", StringComparison.Ordinal))
            {
                _hostCabinPlayerState = value;
            }
            else if (string.Equals(fieldName, "playerTalking", StringComparison.Ordinal))
            {
                _hostCabinPlayerTalking = value != 0;
            }
            else if (string.Equals(fieldName, "inConversation", StringComparison.Ordinal))
            {
                _hostCabinInConversation = value != 0;
            }
            else if (string.Equals(fieldName, "phoneUIState", StringComparison.Ordinal))
            {
                _hostCabinPhoneUi = value != 0;
            }
        }

        private void StoreHostPizzeriaRuntimeField(string fieldName, int value)
        {
            if (string.Equals(fieldName, "currentPlayerState", StringComparison.Ordinal))
            {
                _hostPizzeriaPlayerState = value;
            }
            else if (string.Equals(fieldName, "playerTalking", StringComparison.Ordinal))
            {
                _hostPizzeriaPlayerTalking = value != 0;
            }
            else if (string.Equals(fieldName, "phoneUIState", StringComparison.Ordinal))
            {
                _hostPizzeriaPhoneUi = value != 0;
            }
        }

        private void StoreHostPizzeriaMikeField(string fieldName, int value)
        {
            if (string.Equals(fieldName, "onDialogue", StringComparison.Ordinal))
            {
                _hostPizzeriaMikeDialogue = value != 0;
            }
        }

        private void StoreHostRoadTripRuntimeField(string fieldName, int value)
        {
            if (string.Equals(fieldName, "playerTalking", StringComparison.Ordinal))
            {
                _hostRoadTripPlayerTalking = value != 0;
            }
            else if (string.Equals(fieldName, "phoneUIState", StringComparison.Ordinal))
            {
                _hostRoadTripPhoneUi = value != 0;
            }
        }

        private void StoreHostRoadTripMikeField(string fieldName, int value)
        {
            if (string.Equals(fieldName, "mikeinConversation", StringComparison.Ordinal))
            {
                _hostRoadTripMikeDialogue = value != 0;
            }
        }

        private void ForceMikePostEatingTarget(MikePostEating mike, string reason)
        {
            if (mike == null) return;

            _forcedMikeTarget = mike.transform;
            _forcedMikeReason = reason ?? "postEating";
            SetOnlyMikeActive(mike.transform);
            DisableLocalMikeControllers();
            TryApplyRemoteMikeAnimation();
        }

        private bool ShouldPreferPostEatingMike(MikePostEating mike)
        {
            if (mike == null) return false;
            if (_cabinGameManager != null &&
                _cabinGameManager.currentMike == CabinGameManager.CurrentMike.PostEating)
            {
                return true;
            }

            if (mike.startedSeeking ||
                mike.playerFound ||
                mike.goingToToolShed ||
                mike.catJumpscareDone ||
                mike.mikeInBackyard ||
                !mike.waitOutsideCloset ||
                !mike.hidingSeq1)
            {
                return true;
            }

            if (mike.gameObject.activeInHierarchy &&
                mike.state != MikePostEating.State.None &&
                mike.state != MikePostEating.State.IdleStanding)
            {
                return true;
            }

            var house = _cabinHouseManager;
            if (house == null && _cabinGameManager != null)
            {
                house = _cabinGameManager.cabinHouseManager;
            }

            return house != null &&
                   (house.mikeBedroomJumpscare ||
                    house.mikePostJumpscareActive ||
                    house.mikePostJumpscareConvoDone);
        }

        private void MaybeLogCabinHidingMirror(MikePostEating mike)
        {
            ShedManager shed = null;
            UnderStairsDoor understairs = null;
            HostDuringHiding host = null;
            if (_cabinHouseManager == null && _cabinGameManager != null)
            {
                _cabinHouseManager = _cabinGameManager.cabinHouseManager;
            }

            if (_cabinHouseManager != null)
            {
                shed = _cabinHouseManager.shedManager;
                understairs = _cabinHouseManager.understairsDoor;
            }
            if (_cabinGameManager != null)
            {
                host = _cabinGameManager.hostHiding;
            }

            var debug =
                "MPE=" + (mike != null ? mike.state.ToString() : "-") +
                " seek=" + BoolText(mike != null && mike.startedSeeking) +
                " found=" + BoolText(mike != null && mike.playerFound) +
                " shedGo=" + BoolText(mike != null && mike.goingToToolShed) +
                " Shed hide=" + BoolText(shed != null && shed.playerHidingInside) +
                " started=" + BoolText(shed != null && shed.hidingSeqStarted) +
                " Under hide=" + BoolText(understairs != null && understairs.playerHidingInside) +
                " started=" + BoolText(understairs != null && understairs.hidingSeqStarted) +
                " HostHiding=" + (host != null ? host.state.ToString() : "-");

            var now = Time.realtimeSinceStartup;
            if (string.Equals(debug, _lastCabinHidingDebug, StringComparison.Ordinal) &&
                now < _nextCabinHidingLogTime)
            {
                return;
            }

            _lastCabinHidingDebug = debug;
            _nextCabinHidingLogTime = now + 10f;
            var message = "Cabin hiding mirror: " + debug;
            _logger.LogInfo(message);
            _sessionLogWrite?.Invoke(message);
        }

        private static string BoolText(bool value)
        {
            return value ? "1" : "0";
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
                StoreHostPizzeriaMikeField(mikeFieldName, value);
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
            StoreHostPizzeriaRuntimeField(fieldName, value);
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
                StoreHostRoadTripMikeField(fieldName, value);
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
            StoreHostRoadTripRuntimeField(gameFieldName, value);
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
            if (string.IsNullOrEmpty(state.Path) || IsCoopProxyPath(state.Path))
            {
                return true;
            }
            Transform target = null;
            Transform pathTarget = null;
            var source = "path";
            var isMike = IsMikePath(state.Path);

            if (!string.IsNullOrEmpty(state.Path))
            {
                pathTarget = NetPath.FindByPath(state.Path);
                target = pathTarget;
            }

            if (!state.Active)
            {
                if (isMike)
                {
                    RemoveSmoothedAiState(state, pathTarget, true);
                    if (pathTarget == null)
                    {
                        return true;
                    }

                    if (_forcedMikeTarget != null && IsSameTransformFamily(pathTarget, _forcedMikeTarget))
                    {
                        return true;
                    }
                }
                else if (pathTarget == null)
                {
                    _smoothedAiStates.Remove(state.Path ?? string.Empty);
                    return true;
                }

                target = pathTarget;
                DisableRemoteAiLocomotion(target);
                target.position = state.Position;
                target.rotation = state.Rotation;
                target.gameObject.SetActive(false);
                RemoveSmoothedAiState(state, target, isMike);
                ApplyAiAnimator(target, state, 0f);
                return true;
            }

            if (isMike && state.Active)
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

            if (isMike && IsCabinMikePath(state.Path) && state.Active)
            {
                SetOnlyMikeActive(target);
                DisableLocalMikeControllers();
            }

            DisableRemoteAiLocomotion(target);

            target.gameObject.SetActive(true);
            QueueSmoothedAiState(state, target, isMike);
            return true;
        }

        private void DisableRemoteAiLocomotion(Transform target)
        {
            if (target == null) return;

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
        }

        private void QueueSmoothedAiState(AiTransformState state, Transform target, bool isMike)
        {
            if (target == null || string.IsNullOrEmpty(state.Path)) return;

            var now = Time.realtimeSinceStartup;
            var smoothKey = GetAiSmoothKey(state, target, isMike);
            if (string.IsNullOrEmpty(smoothKey)) return;

            if (isMike)
            {
                RemoveOtherMikeSmoothStates(target);
            }

            if (!_smoothedAiStates.TryGetValue(smoothKey, out var smooth) || smooth == null)
            {
                smooth = new SmoothedAiState
                {
                    Path = smoothKey
                };
                _smoothedAiStates[smoothKey] = smooth;
            }

            var targetChanged = smooth.Target != null && !ReferenceEquals(smooth.Target, target);
            var activeChanged = smooth.Initialized && smooth.Active != state.Active;
            var stale = smooth.LastPacketTime > 0f && now - smooth.LastPacketTime > AiStaleSnapSeconds;
            var distance = Vector3.Distance(target.position, state.Position);
            var shouldSnap = !smooth.Initialized || targetChanged || activeChanged || stale || distance > AiSnapDistanceMeters;

            smooth.Target = target;
            var animatorState = state;
            if (isMike)
            {
                animatorState.Path = smoothKey;
            }

            smooth.Latest = animatorState;
            smooth.TargetPosition = state.Position;
            smooth.TargetRotation = state.Rotation;
            smooth.Active = state.Active;
            smooth.IsMike = isMike;
            smooth.LastPacketTime = now;

            if (shouldSnap)
            {
                target.position = state.Position;
                target.rotation = state.Rotation;
                smooth.Initialized = true;
                smooth.LastRenderTime = now;
                ApplyAiAnimator(target, state, 0f);
            }
        }

        private string GetAiSmoothKey(AiTransformState state, Transform target, bool isMike)
        {
            if (isMike && target != null)
            {
                var targetPath = NetPath.GetPath(target);
                if (!string.IsNullOrEmpty(targetPath))
                {
                    return "Mike:" + targetPath;
                }
            }

            return state.Path ?? string.Empty;
        }

        private void RemoveSmoothedAiState(AiTransformState state, Transform target, bool isMike)
        {
            _smoothedAiStates.Remove(state.Path ?? string.Empty);
            if (!isMike || target == null) return;

            var smoothKey = GetAiSmoothKey(state, target, true);
            if (!string.IsNullOrEmpty(smoothKey))
            {
                _smoothedAiStates.Remove(smoothKey);
            }
        }

        private void RemoveOtherMikeSmoothStates(Transform keep)
        {
            if (keep == null || _smoothedAiStates.Count == 0) return;

            var keys = new List<string>(_smoothedAiStates.Keys);
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                if (!_smoothedAiStates.TryGetValue(key, out var smooth) || smooth == null) continue;
                if (!smooth.IsMike) continue;
                if (smooth.Target != null && IsSameTransformFamily(smooth.Target, keep)) continue;
                _smoothedAiStates.Remove(key);
            }
        }

        private void UpdateSmoothedAiStates()
        {
            if (_smoothedAiStates.Count == 0) return;

            var now = Time.realtimeSinceStartup;
            var keys = new List<string>(_smoothedAiStates.Keys);
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                if (!_smoothedAiStates.TryGetValue(key, out var smooth) || smooth == null)
                {
                    continue;
                }

                var target = smooth.Target;
                if (target == null || target.gameObject == null)
                {
                    _smoothedAiStates.Remove(key);
                    continue;
                }

                if (!smooth.Active)
                {
                    _smoothedAiStates.Remove(key);
                    continue;
                }

                if (!target.gameObject.activeSelf)
                {
                    target.gameObject.SetActive(true);
                }

                DisableRemoteAiLocomotion(target);

                var dt = smooth.LastRenderTime > 0f
                    ? Mathf.Clamp(now - smooth.LastRenderTime, 0.001f, 0.1f)
                    : Mathf.Clamp(Time.deltaTime, 0.001f, 0.1f);
                smooth.LastRenderTime = now;

                var before = target.position;
                var distance = Vector3.Distance(before, smooth.TargetPosition);
                if (!smooth.Initialized || distance > AiSnapDistanceMeters)
                {
                    target.position = smooth.TargetPosition;
                    target.rotation = smooth.TargetRotation;
                    smooth.Initialized = true;
                    ApplyAiAnimator(target, smooth.Latest, 0f);
                    continue;
                }

                var response = smooth.IsMike ? AiMikeSmoothing : AiGeneralSmoothing;
                var alpha = 1f - Mathf.Exp(-response * dt);
                target.position = Vector3.Lerp(before, smooth.TargetPosition, alpha);
                target.rotation = Quaternion.Slerp(target.rotation, smooth.TargetRotation, alpha);

                var speed = Vector3.Distance(target.position, before) / dt;
                ApplyAiAnimator(target, smooth.Latest, speed);
            }
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
                    var path = NetPath.GetPath(desired);
                    _logger.LogInfo("Mike sync: seq=" + _cabinGameManager.CurrentSequence +
                                    " currentMike=" + _cabinGameManager.currentMike +
                                    " target=" + desired.name +
                                    " path=" + (string.IsNullOrEmpty(path) ? "-" : path) +
                                    " reason=" + reason +
                                    " active=" + desired.gameObject.activeInHierarchy +
                                    " renderable=" + HasRenderable(desired));
                }
            }

            _lastMikeSyncDebug =
                _cabinGameManager.CurrentSequence + "/" +
                _cabinGameManager.currentMike + " -> " +
                desired.name + " (" + reason + ")";

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
            var postEating = ResolvePostEatingMikeTarget(out var postEatingReason);
            if (postEating != null)
            {
                reason = postEatingReason;
                forceActive = true;
                return postEating;
            }

            if (IsCabinCookMikeSequence(seq))
            {
                var controller = GetMikeControllerTransform();
                if (controller != null && IsActiveRenderable(controller))
                {
                    reason = "seq:" + seq;
                    forceActive = true;
                    return controller;
                }

                var activeCookTarget = GetActiveCabinCookMikeTarget();
                if (activeCookTarget != null)
                {
                    reason = "seq:" + seq + ":activefallback";
                    forceActive = true;
                    return activeCookTarget;
                }

                if (controller != null && HasRenderable(controller))
                {
                    reason = "seq:" + seq + ":inactive-controller";
                    forceActive = true;
                    return controller;
                }

                if (_cabinGameManager.mikeCabin != null && IsActiveRenderable(_cabinGameManager.mikeCabin.transform))
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

        private Transform ResolvePostEatingMikeTarget(out string reason)
        {
            reason = string.Empty;
            if (_cabinGameManager == null) return null;

            var mike = _cabinGameManager.mikePostEating;
            if (mike == null) return null;
            if (!ShouldPreferPostEatingMike(mike)) return null;

            reason = "postEating:" + mike.state;
            return mike.transform;
        }

        private static bool IsCabinCookMikeSequence(SequenceType seq)
        {
            return seq == SequenceType.Cooking ||
                   seq == SequenceType.PickingBoardGame ||
                   seq == SequenceType.PlayingJenga ||
                   seq == SequenceType.GoingToPlayOuija ||
                   seq == SequenceType.PlayingOuija ||
                   seq == SequenceType.Eating;
        }

        private void SetOnlyMikeActive(Transform keep)
        {
            if (keep == null) return;

            RemoveOtherMikeSmoothStates(keep);
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
            var shouldBeActive = IsSameTransformFamily(target, keep);
            if (target.gameObject.activeSelf != shouldBeActive)
            {
                target.gameObject.SetActive(shouldBeActive);
            }
        }

        private static bool IsSameTransformFamily(Transform first, Transform second)
        {
            if (first == null || second == null) return false;
            return ReferenceEquals(first, second) || first.IsChildOf(second) || second.IsChildOf(first);
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

        private void ApplyAiAnimator(Transform target, AiTransformState state, float speedOverride = -1f)
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
            var speed = Mathf.Max(0f, speedOverride);
            if (speedOverride < 0f &&
                _aiLastPositions.TryGetValue(state.Path, out var lastPos) &&
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

        private Transform GetActiveCabinCookMikeTarget()
        {
            if (_cabinGameManager == null)
            {
                _cabinGameManager = UnityEngine.Object.FindObjectOfType<CabinGameManager>();
            }

            if (_cabinGameManager == null) return null;

            var candidates = new List<Transform>();
            var mikeController = GetMikeControllerComponent();
            if (mikeController != null) candidates.Add(mikeController.transform);
            if (_cabinGameManager.mikePostEating != null) candidates.Add(_cabinGameManager.mikePostEating.transform);
            if (_cabinGameManager.mikeCabin != null) candidates.Add(_cabinGameManager.mikeCabin.transform);
            if (_cabinGameManager.mikeObject != null) candidates.Add(_cabinGameManager.mikeObject.transform);

            foreach (var candidate in candidates)
            {
                if (IsActiveRenderable(candidate))
                {
                    return candidate;
                }
            }

            return null;
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

        private static bool IsCabinMikePath(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   path.StartsWith("Cabin", StringComparison.OrdinalIgnoreCase) &&
                   path.IndexOf("Mike", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsCoopProxyPath(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   (path.IndexOf("CoopRemotePlayer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("CoopHostAvatar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("CoopClientAvatar", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool IsHighFrequencyStoryFlag(string key)
        {
            return !string.IsNullOrEmpty(key) &&
                   key.StartsWith(CabinGameFlagPrefix + CabinMikeAnimFieldPrefix, StringComparison.Ordinal);
        }

        private static bool IsClientLocalCabinRuntimeField(string fieldName)
        {
            return string.Equals(fieldName, "currentPlayerState", StringComparison.Ordinal) ||
                   string.Equals(fieldName, "playerTalking", StringComparison.Ordinal) ||
                   string.Equals(fieldName, "phoneUIState", StringComparison.Ordinal) ||
                   string.Equals(fieldName, "inConversation", StringComparison.Ordinal);
        }

        private static bool IsClientLocalCabinHouseRuntimeField(string fieldName)
        {
            return false;
        }

        private void LogSuppressedCabinRuntimeSync(string reason)
        {
            if (string.IsNullOrEmpty(reason)) return;

            var now = Time.realtimeSinceStartup;
            if (_cabinRuntimeSyncLogTimes.TryGetValue(reason, out var nextLogTime) && now < nextLogTime)
            {
                return;
            }

            _cabinRuntimeSyncLogTimes[reason] = now + 5f;
            var message = "Cabin client runtime state held local: " + reason;
            _logger.LogInfo(message);
            _sessionLogWrite?.Invoke(message);
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

        private sealed class SmoothedAiState
        {
            public string Path;
            public Transform Target;
            public AiTransformState Latest;
            public Vector3 TargetPosition;
            public Quaternion TargetRotation;
            public bool Active;
            public bool IsMike;
            public bool Initialized;
            public float LastPacketTime;
            public float LastRenderTime;
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
            if (ShouldUseHostScriptedLock())
            {
                if (now - _lastScriptedHostSnapTime >= 0.1f)
                {
                    TeleportToHost();
                    _lastScriptedHostSnapTime = now;
                    _lastTeleportTime = now;
                }
                return;
            }

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

        private bool ShouldUseHostScriptedLock()
        {
            if (!_hasPendingHostCam) return false;
            if (_hostDialogueConversationId >= 0) return true;
            if (!string.IsNullOrEmpty(_remoteDialogueText)) return true;
            if (_hostSceneScriptedLock) return true;
            if (_hostCabinPlayerTalking || _hostCabinInConversation || _hostCabinPhoneUi) return true;
            if (_hostPizzeriaPlayerTalking || _hostPizzeriaPhoneUi || _hostPizzeriaMikeDialogue) return true;
            if (_hostRoadTripPlayerTalking || _hostRoadTripPhoneUi || _hostRoadTripMikeDialogue) return true;

            if (_hostCabinPlayerState >= 0 &&
                _hostCabinPlayerState != Convert.ToInt32(CabinGameManager.PlayerState.Normal) &&
                _hostCabinPlayerState != Convert.ToInt32(CabinGameManager.PlayerState.Driving))
            {
                return true;
            }

            if (_hostPizzeriaPlayerState > Convert.ToInt32(PizzeriaGameManager.PlayerState.Normal))
            {
                return true;
            }

            if (_cabinGameManager != null)
            {
                var mike = _cabinGameManager.mikePostEating;
                if (mike != null && ShouldPreferPostEatingMike(mike))
                {
                    return true;
                }

                if (IsCabinHikerWindowActive())
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsCabinHikerWindowActive()
        {
            if (_cabinGameManager == null) return false;

            // Gate on CurrentSequence — the relevant sub-component GameObjects (cabinHiker,
            // hostFixingSink, mikeAfterHiding) can be active in the scene hierarchy during
            // normal play because their parents are scene-resident; relying on activeSelf
            // would assert the scripted lock from scene load and clump the client onto the host.
            var seq = _cabinGameManager.CurrentSequence;
            return seq == SequenceType.HikerSequence ||
                   seq == SequenceType.HostAtDoor ||
                   seq == SequenceType.HostHittingDoor;
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

        private void StabilizeLocalCabinPlayerState()
        {
            if (SceneManager.GetActiveScene().name != "CabinScene") return;

            if (_cabinGameManager == null)
            {
                _cabinGameManager = UnityEngine.Object.FindObjectOfType<CabinGameManager>();
            }

            if (_cabinGameManager == null) return;

            var stabilized = false;
            var stabilizedReason = string.Empty;

            if (!_cabinGameFieldCache.TryGetValue("inConversation", out var inConversationField))
            {
                inConversationField = FindInstanceField(typeof(CabinGameManager), "inConversation");
                _cabinGameFieldCache["inConversation"] = inConversationField;
            }

            if (inConversationField != null && inConversationField.FieldType == typeof(bool))
            {
                try
                {
                    if ((bool)inConversationField.GetValue(_cabinGameManager))
                    {
                        inConversationField.SetValue(_cabinGameManager, false);
                        stabilized = true;
                        stabilizedReason = "inConversation";
                    }
                }
                catch (Exception ex)
                {
                    if (_settings.VerboseLogging.Value)
                    {
                        _logger.LogWarning("Cabin local conversation stabilizer failed: " + ex.Message);
                    }
                }
            }

            if (_cabinGameManager.playerTalking)
            {
                _cabinGameManager.playerTalking = false;
                stabilized = true;
                stabilizedReason = "playerTalking";
            }

            if (IsUnsafeClientCabinPlayerState(_cabinGameManager.currentPlayerState))
            {
                stabilized = true;
                stabilizedReason = "playerState:" + _cabinGameManager.currentPlayerState;
                _cabinGameManager.ChangePlayerState(CabinGameManager.PlayerState.Normal);
            }

            if (stabilized)
            {
                LogSuppressedCabinRuntimeSync(stabilizedReason);
            }
        }

        private void StabilizeLocalCabinHouseState()
        {
            // These fields are now host-authored so the bedroom jumpscare and hiding
            // sequences can progress consistently on the client.
        }

        private bool SetCabinHouseBoolIfTrue(string fieldName)
        {
            if (_cabinHouseManager == null || string.IsNullOrEmpty(fieldName)) return false;

            if (!_cabinHouseFieldCache.TryGetValue(fieldName, out var field))
            {
                field = FindInstanceField(typeof(CabinHouseManager), fieldName);
                _cabinHouseFieldCache[fieldName] = field;
            }

            if (field == null || field.FieldType != typeof(bool)) return false;

            try
            {
                if (!(bool)field.GetValue(_cabinHouseManager)) return false;
                field.SetValue(_cabinHouseManager, false);
                return true;
            }
            catch (Exception ex)
            {
                if (_settings.VerboseLogging.Value)
                {
                    _logger.LogWarning("Cabin house stabilizer failed field=" + fieldName + " error=" + ex.Message);
                }
                return false;
            }
        }

        private void ApplyCabinSequenceVisuals()
        {
            if (SceneManager.GetActiveScene().name != "CabinScene") return;

            if (_cabinGameManager == null)
            {
                _cabinGameManager = UnityEngine.Object.FindObjectOfType<CabinGameManager>();
            }

            if (_cabinGameManager == null) return;

            var seq = _cabinGameManager.CurrentSequence;
            if (seq != SequenceType.PlayingOuija && seq != SequenceType.GoingToPlayOuija)
            {
                SetPrivateBool(_cabinGameManager, "isPlayingOuija", false);
                return;
            }

            SetPrivateBool(_cabinGameManager, "mikeHasPlacedBasementTable", true);
            SetPrivateBool(_cabinGameManager, "isPlayingOuija", seq == SequenceType.PlayingOuija);

            var cabinPlayer = PlayerController.GetInstance() as CabinPlayerController;
            if (cabinPlayer == null)
            {
                cabinPlayer = UnityEngine.Object.FindObjectOfType<CabinPlayerController>();
            }

            if (cabinPlayer == null) return;

            if (seq == SequenceType.PlayingOuija)
            {
                SetPrivateComponentGameObjectActive(cabinPlayer, "ouijaBoardGame", true);
                SetPrivateGameObjectActive(cabinPlayer, "basementTableLight", true);
                SetPrivateColliderEnabled(cabinPlayer, "basementTableCollider", false);
                SetPrivateComponentGameObjectActive(cabinPlayer, "triggerSubGetOuija", false);
                SetPrivateEnum(cabinPlayer, "currentHoldingBoardGameType", "Ouija");
            }
        }

        private static void SetPrivateBool(object target, string fieldName, bool value)
        {
            var field = target != null ? FindInstanceField(target.GetType(), fieldName) : null;
            if (field == null || field.FieldType != typeof(bool)) return;
            try
            {
                field.SetValue(target, value);
            }
            catch
            {
                // Best-effort scene visual sync; unsupported fields should not break co-op.
            }
        }

        private static void SetPrivateEnum(object target, string fieldName, string valueName)
        {
            var field = target != null ? FindInstanceField(target.GetType(), fieldName) : null;
            if (field == null || !field.FieldType.IsEnum) return;
            try
            {
                field.SetValue(target, Enum.Parse(field.FieldType, valueName, ignoreCase: true));
            }
            catch
            {
                // Best-effort scene visual sync; unsupported fields should not break co-op.
            }
        }

        private static void SetPrivateComponentGameObjectActive(object target, string fieldName, bool active)
        {
            var field = target != null ? FindInstanceField(target.GetType(), fieldName) : null;
            if (field == null) return;
            try
            {
                var component = field.GetValue(target) as Component;
                if (component != null && component.gameObject != null)
                {
                    component.gameObject.SetActive(active);
                }
            }
            catch
            {
                // Best-effort scene visual sync; unsupported fields should not break co-op.
            }
        }

        private static void SetPrivateGameObjectActive(object target, string fieldName, bool active)
        {
            var field = target != null ? FindInstanceField(target.GetType(), fieldName) : null;
            if (field == null || field.FieldType != typeof(GameObject)) return;
            try
            {
                var gameObject = field.GetValue(target) as GameObject;
                if (gameObject != null)
                {
                    gameObject.SetActive(active);
                }
            }
            catch
            {
                // Best-effort scene visual sync; unsupported fields should not break co-op.
            }
        }

        private static void SetPrivateColliderEnabled(object target, string fieldName, bool enabled)
        {
            var field = target != null ? FindInstanceField(target.GetType(), fieldName) : null;
            if (field == null) return;
            try
            {
                var collider = field.GetValue(target) as Collider;
                if (collider != null)
                {
                    collider.enabled = enabled;
                }
            }
            catch
            {
                // Best-effort scene visual sync; unsupported fields should not break co-op.
            }
        }

        private static bool IsUnsafeClientCabinPlayerState(CabinGameManager.PlayerState state)
        {
            return state != CabinGameManager.PlayerState.Normal &&
                   state != CabinGameManager.PlayerState.Driving;
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
            _deferredScene = null;
            _deferredSceneIndex = -1;
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
            _nextSceneLoadProgressLogTime = 0f;
            _sceneLoadTimeoutLogged = false;
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
            _smoothedAiStates.Clear();
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
            _cabinPostEatingFieldCache.Clear();
            _cabinShedFieldCache.Clear();
            _cabinUnderstairsFieldCache.Clear();
            _cabinHostHidingFieldCache.Clear();
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
            _smoothedAiStates.Clear();
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
            _lastCabinHidingDebug = "-";
            _nextCabinHidingLogTime = 0f;
            _lastScriptedHostSnapTime = 0f;
            _hostSceneScriptedLock = false;
            _hostCabinPlayerState = -1;
            _hostCabinPlayerTalking = false;
            _hostCabinInConversation = false;
            _hostCabinPhoneUi = false;
            _hostPizzeriaPlayerState = -1;
            _hostPizzeriaPlayerTalking = false;
            _hostPizzeriaPhoneUi = false;
            _hostPizzeriaMikeDialogue = false;
            _hostRoadTripPlayerTalking = false;
            _hostRoadTripPhoneUi = false;
            _hostRoadTripMikeDialogue = false;
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
            DisableMikeBehaviour(UnityEngine.Object.FindObjectOfType<MikeCabinCookController>());
            DisableMikeBehaviour(UnityEngine.Object.FindObjectOfType<MikeCabin>());
            DisableMikeBehaviour(UnityEngine.Object.FindObjectOfType<MikeFishing>());
            DisableMikeBehaviour(UnityEngine.Object.FindObjectOfType<MikePostEating>());
            DisableMikeBehaviour(UnityEngine.Object.FindObjectOfType<MikeAfterHiding>());
            DisableMikeBehaviour(UnityEngine.Object.FindObjectOfType<MikeRizzlerController>());
            DisableMikeBehaviour(UnityEngine.Object.FindObjectOfType<MikeEndGame>());
            DisableMikeBehaviour(UnityEngine.Object.FindObjectOfType<MikeInCar>());

        }

        private static void DisableMikeBehaviour(MonoBehaviour behaviour)
        {
            if (behaviour == null) return;
            if (behaviour.enabled)
            {
                behaviour.enabled = false;
            }

            var pathAgent = behaviour.GetComponent<NavmeshPathAgent>();
            if (pathAgent != null && pathAgent.enabled)
            {
                pathAgent.enabled = false;
            }

            var navAgent = behaviour.GetComponent<NavMeshAgent>();
            if (navAgent != null && navAgent.enabled)
            {
                navAgent.enabled = false;
            }
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
