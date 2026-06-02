using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
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
        private readonly Action<string> _sessionLogWrite;
        private readonly VoiceChatSync _voiceChat;
        private RemotePlayerProxy _remotePlayer;
        private string _hostDisplayName = "HOST";
        private string _clientDisplayName = "CLIENT";
        private PlayerInputState _lastInputState;
        private bool _hasInputState;
        private readonly Dictionary<string, DoorState> _doorStates = new Dictionary<string, DoorState>();
        private readonly Dictionary<string, HoldableState> _holdableStates = new Dictionary<string, HoldableState>();
        private readonly Dictionary<string, int> _storyFlags = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _cabinHouseFlags = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _cabinGameFlags = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _pizzeriaFlags = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _roadTripFlags = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _officeFlags = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _parkingLotFlags = new Dictionary<string, int>();
        private readonly Dictionary<string, AiTransformState> _aiStates = new Dictionary<string, AiTransformState>();
        private readonly CabinNpcRegistry _cabinNpcRegistry;
        private readonly GenericNpcRegistry _pizzeriaNpcRegistry;
        private readonly GenericNpcRegistry _roadTripNpcRegistry;
        private readonly GenericNpcRegistry _officeNpcRegistry;
        private readonly GenericNpcRegistry _parkingLotNpcRegistry;
        private readonly Dictionary<string, int> _npcSeqById = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _npcLastStateHashById = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _npcLastSendMsById = new Dictionary<string, long>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _npcLastLogHashById = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _npcLastLogMsById = new Dictionary<string, long>(StringComparer.Ordinal);
        private readonly Dictionary<string, Vector3> _npcLastPositionById = new Dictionary<string, Vector3>(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _npcLastPositionMsById = new Dictionary<string, long>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _vehicleSeqById = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _vehicleLastHashById = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _vehicleLastSendMsById = new Dictionary<string, long>(StringComparer.Ordinal);
        private readonly Dictionary<string, Vector3> _vehicleLastPositionById = new Dictionary<string, Vector3>(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _vehicleLastPositionMsById = new Dictionary<string, long>(StringComparer.Ordinal);
        private readonly List<ICoopHostSceneAdapter> _sceneAdapters = new List<ICoopHostSceneAdapter>();
        private PlayerTransformState _pendingClientTransform;
        private bool _hasPendingClientTransform;

        private CabinDoor[] _cabinDoors = new CabinDoor[0];
        private NOTLonely_Door.DoorScript[] _doorScripts = new NOTLonely_Door.DoorScript[0];
        private Holdable[] _holdables = new Holdable[0];
        private NavmeshPathAgent[] _aiAgents = new NavmeshPathAgent[0];
        private NavMeshAgent[] _navMeshAgents = new NavMeshAgent[0];
        private CabinHouseManager _cabinHouseManager;
        private CabinGameManager _cabinGameManager;
        private PizzeriaGameManager _pizzeriaGameManager;
        private PizzeriaTruckDoor _pizzeriaTruckDoor;
        private MikePizzeria _pizzeriaMike;
        private RoadTripGameManager _roadTripGameManager;
        private MikeInCar _roadTripMikeInCar;
        private MikeTruckInLoopScene _roadTripTruck;
        private OfficeLayoutGameManager _officeLayoutGameManager;
        private ParkingLotGameManager _parkingLotGameManager;

        private readonly FieldInfo _doorScriptOpened;
        private readonly MethodInfo _doorScriptOpen;
        private readonly MethodInfo _doorScriptClose;
        private readonly FieldInfo _playerFirstPersonField;
        private readonly FieldInfo _playerPausedField;
        private readonly FieldInfo _cabinMikeControllerField;
        private readonly FieldInfo _cabinMikeRizzlerControllerField;
        private readonly FieldInfo _cabinHikerControllerField;
        private readonly FieldInfo _cabinHasPlayedEerieHitForHikerField;
        private readonly List<FieldInfo> _cabinHouseBoolFields = new List<FieldInfo>();
        private readonly List<FieldInfo> _cabinGameFields = new List<FieldInfo>();
        private readonly Dictionary<string, FieldInfo> _pizzeriaFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _pizzeriaMikeFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _cabinHouseActiveFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _pizzeriaGameObjectFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _pizzeriaMikeGameObjectFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _roadTripFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _roadTripMikeFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _roadTripTruckFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _officeFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _parkingLotFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _cabinHikerFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _cabinHikerControllerFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _cabinHostFixingSinkFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _cabinMikeAfterHidingFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly SceneHandshakeState _sceneHandshake = new SceneHandshakeState();
        private readonly SessionLifecycle _lifecycle;
        private long _lastDriftRecoveryMs;
        private const long DriftRecoveryClientPktAgeMs = 30000;
        private const long DriftRecoveryCooldownMs = 60000;
        private static readonly int[] SceneChangeRetryBackoffMs = { 1500, 1500, 2000, 2500, 3000, 3500, 4000, 4500 };
        private static readonly int[] CabinSceneChangeRetryBackoffMs = { 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10000 };
        private SnapshotCounts _lastSnapshotCounts = SnapshotCounts.Empty;

        private const string CabinHouseFlagPrefix = "CabinHouse.";
        private const string CabinGameFlagPrefix = "CabinGM.";
        private const string CabinMikeAnimPrefix = CabinGameFlagPrefix + "MikeAnim.";
        private const string CabinMikeAnimStateHashKey = CabinMikeAnimPrefix + "StateHash";
        private const string CabinMikeAnimLoopKey = CabinMikeAnimPrefix + "Loop";
        private const string CabinMikeAnimPhaseKey = CabinMikeAnimPrefix + "Phase10";
        private const string CabinMikeAnimTransitionKey = CabinMikeAnimPrefix + "Transition";
        private const string CabinMikeAnimNextStateKey = CabinMikeAnimPrefix + "NextStateHash";
        private const string CabinHouseActivePrefix = CabinHouseFlagPrefix + "Active.";
        private const string CabinPostEatingFlagPrefix = CabinGameFlagPrefix + "MikePostEating.";
        private const string CabinPostEatingActivePrefix = CabinPostEatingFlagPrefix + "Active.";
        private const string CabinShedFlagPrefix = CabinGameFlagPrefix + "Shed.";
        private const string CabinShedActivePrefix = CabinShedFlagPrefix + "Active.";
        private const string CabinUnderstairsFlagPrefix = CabinGameFlagPrefix + "Understairs.";
        private const string CabinUnderstairsActivePrefix = CabinUnderstairsFlagPrefix + "Active.";
        private const string CabinHostHidingFlagPrefix = CabinGameFlagPrefix + "HostHiding.";
        private const string CabinDeathFlagPrefix = CabinGameFlagPrefix + "Death.";
        private const string CabinDeathActivePrefix = CabinDeathFlagPrefix + "Active.";
        private const string CabinDeathEnabledPrefix = CabinDeathFlagPrefix + "Enabled.";
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
        private const string OfficeLayoutFlagPrefix = "OfficeLayoutGM.";
        private const string ParkingLotFlagPrefix = "ParkingLotGM.";
        private const float MaxInteractDistanceMeters = 3.5f;
        private const float InteractLosPaddingMeters = 0.2f;
        private const long CabinMikeAnimPhaseSendIntervalMs = 250;

        private long _nextPlayerSendMs;
        private long _nextWorldSendMs;
        private long _nextStorySendMs;
        private long _nextInputSendMs;
        private long _nextCabinMikeAnimPhaseSendMs;
        private bool _lastClientConnected;
        private string _lastSceneName = string.Empty;
        private bool _clientSceneReady;
        private bool _awaitingSceneReady;
        private string _clientSceneName = string.Empty;
        private long _lastSceneRequestMs;
        private long _lastSceneReadyMs;
        private long _lastClientTransformMs;
        private long _lastHostTransformSendMs;
        private int _sceneGeneration;
        private int _sceneHandshakeSessionId;

        private DialogueSystemEvents _dialogueEvents;
        private UnityAction<Subtitle> _onConversationLine;
        private UnityAction<Subtitle> _onConversationLineEnd;
        private UnityAction<Subtitle> _onBarkLine;
        private UnityAction<Transform> _onConversationStart;
        private UnityAction<Transform> _onConversationEnd;
        private UnityAction<Response[]> _onConversationResponseMenu;
        private EventHandler<SelectedResponseEventArgs> _onResponseSelected;
        private string _lastDialogueText = string.Empty;
        private string _lastDialogueSpeaker = string.Empty;
        private float _lastDialogueSentTime;
        private int _uiMirrorSeq;
        private float _nextSubTextPollTime;
        private string _lastSubText = string.Empty;
        private int _lastPhoneMirrorHash;
        private long _nextPhoneMirrorSendMs;
        private long _nextPhoneMirrorLogMs;
        private int _dialogueConversationId = -1;
        private int _dialogueEntryId = -1;
        private int _dialogueChoiceIndex = -1;
        private long _lastDialogueEventMs;
        private Response[] _lastDialogueResponses = new Response[0];
        private float _nextDialogueUiScanTime;
        private readonly List<AbstractDialogueUI> _dialogueUis = new List<AbstractDialogueUI>();
        private string _lastStoryEventKey = string.Empty;
        private int _lastStoryEventValue;
        private long _lastStoryEventMs;
        private int _lastCabinMikeSyncTargetId;
        private string _lastCabinMikeSyncReason = string.Empty;
        private float _nextCabinMikeSyncLogTime;
        private string _lastCabinMikeSyncDebug = "-";
        private string _lastCabinHidingDebug = "-";
        private string _npcOverlaySummary = "NPC: -";
        private int _lastCabinCookingPropHash;
        private long _nextCabinCookingPropLogMs;
        private string _boardGameOverlaySummary = "MiniGame: -";
        private int _lastCabinBoardGameHash;
        private long _nextCabinBoardGameLogMs;
        private int _lastCabinAmbientHash;
        private long _nextCabinAmbientLogMs;
        private float _nextCabinHidingLogTime;
        private bool _hostWaitActive;
        private bool _hostWaitPlayerPauseCaptured;
        private bool _hostWaitOriginalPlayerPaused;
        private float _hostWaitPreviousTimeScale = 1f;
        private long _hostWaitEnteredMs;
        private long _hostWaitNextStillLogMs;
        private long _hostWaitNextSnapshotRetryMs;
        private string _hostWaitReason = "-";
        private string _hostWaitScene = "-";
        private string _hostWaitSummary = "HostWait: no";
        private long _nextRemoteVoiceReactionMs;

        private const long HostWaitStillLogIntervalMs = 5000;
        private const long HostWaitSnapshotRetryMs = 15000;

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

        private readonly string[] _cabinHouseFieldNames = new[]
        {
            "fridgeStocked",
            "isMikeTouring",
            "doLivingRoomDialogue",
            "livingRoomDialogueDone",
            "doKitchenDialogue",
            "kitchenDialogueDone",
            "dobackyardDialogue",
            "backyardDialogueDone",
            "doshedDialogue",
            "shedDialogueDone",
            "dobasementDialogue",
            "basementDialogueDone",
            "dolaundryDialogue",
            "laundryDialogueDone",
            "doMikeBedroomDialogue",
            "mikeBedroomDialogueDone",
            "doUpstairdBathroomDialogue",
            "upstairsBathroomDialogueDone",
            "doDownstairsBathroomDialogue",
            "downstairsBathroomDialogueDone",
            "dofishingAreaDialogue",
            "fishingAreaDialogueDone",
            "doBasementDoorDialogue",
            "basementDoorDialogueDone",
            "mikeBedroomJumpscare",
            "insideMikeBedroomTrigger",
            "mikePostJumpscareActive",
            "mikePostJumpscareConvoDone",
            "lookAtStairsRepeat",
            "startedStairsRepeat",
            "washedHands"
        };

        private readonly string[] _cabinHouseActiveFieldNames = new[]
        {
            "ColliderStairsAfterEating",
            "ColliderStairsTexting",
            "triggerMikeTexts2"
        };

        private readonly string[] _cabinHouseActiveArrayFieldNames = new[]
        {
            "hidingSeqTriggers",
            "hidingSeq2Triggers"
        };

        private readonly string[] _cabinGameFieldNames = new[]
        {
            "currentCabinSceneType",
            "CurrentSequence",
            "currentPlayerState",
            "currentMike",
            "playerTalking",
            "phoneUIState",
            "inConversation",
            "hasHadJengaConvo",
            "hasHadTurnOffLightsConvo",
            "mikeHasPlacedBasementTable",
            "isBasementLightTurnedOff",
            "isPlayingOuija",
            "fishingDone",
            "playerHasFish",
            "mikeHasFish",
            "mikeHasSatOnSofaWithFishPlate",
            "playerInAttic",
            "cabinHikerWalkDone",
            "hikerIsVisibleToPlayer",
            "hasShownHikerRealizationSub",
            "hasTalkedWithHiker",
            "stopShowingPostHikerSleepSub",
            "mikeSaidTurnOffLight",
            "playerSittingOnBed",
            "hasPlayedEerieHitForHiker",
            "forcedTurnedOnBasementLights"
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
            "conversationTimer",
            "randomTimerForBump",
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

        private readonly string[] _officeFieldNames = new[]
        {
            "currentState",
            "currentPlayerState"
        };

        private readonly string[] _parkingLotFieldNames = new[]
        {
            "currentState",
            "currentPlayerState",
            "introDone",
            "isPhoneRinging"
        };

        private readonly string[] _cabinHikerFieldNames = new[]
        {
            "state",
            "go",
            "moving",
            "reachedPos"
        };

        private readonly string[] _cabinHikerControllerFieldNames = new[]
        {
            "currentState",
            "nextAnimationState",
            "currentAxiousIdleState",
            "currentAngryIdleState",
            "stopKnocking",
            "canExitTransitionFromAnimation",
            "canCheckIfVisibleToPlayer",
            "isInteractable",
            "playerHasSeenHiker",
            "playerCanSeeHiker",
            "playerIsCrouching",
            "stopKickingDoor",
            "hikerhasDetectedPlayer",
            "canSuddenlyLookAtPlayer",
            "hasPlayedSawYouThereSFX"
        };

        private readonly string[] _cabinHostFixingSinkFieldNames = new[]
        {
            "state",
            "go",
            "moving",
            "isWalkingToRoad"
        };

        private readonly string[] _cabinMikeAfterHidingFieldNames = new[]
        {
            "state",
            "go",
            "moving",
            "reachedPos",
            "followingHost"
        };

        public CoopHostCoordinator(ManualLogSource logger, Settings settings, CoopServer server, Action<string> sessionLogWrite = null)
        {
            _logger = logger;
            _settings = settings;
            _server = server;
            _sessionLogWrite = sessionLogWrite;
            _lifecycle = new SessionLifecycle("host", logger, sessionLogWrite);
            _voiceChat = new VoiceChatSync(logger, settings, "host", sessionLogWrite);
            _cabinNpcRegistry = new CabinNpcRegistry(logger, sessionLogWrite, "host");
            _pizzeriaNpcRegistry = SceneSyncRegistries.CreatePizzeriaNpcRegistry(logger, sessionLogWrite, "host");
            _roadTripNpcRegistry = SceneSyncRegistries.CreateRoadTripNpcRegistry(logger, sessionLogWrite, "host");
            _officeNpcRegistry = SceneSyncRegistries.CreateOfficeNpcRegistry(logger, sessionLogWrite, "host");
            _parkingLotNpcRegistry = SceneSyncRegistries.CreateParkingLotNpcRegistry(logger, sessionLogWrite, "host");
            _clientAvatar = new RemoteAvatar("CoopClientAvatar", new Color(0.2f, 0.7f, 1f, 0.8f));

            var doorType = typeof(NOTLonely_Door.DoorScript);
            _doorScriptOpened = doorType.GetField("Opened", BindingFlags.Instance | BindingFlags.NonPublic);
            _doorScriptOpen = doorType.GetMethod("OpenDoor", BindingFlags.Instance | BindingFlags.NonPublic);
            _doorScriptClose = doorType.GetMethod("CloseDoor", BindingFlags.Instance | BindingFlags.NonPublic);
            _playerFirstPersonField = typeof(PlayerController).GetField("firstPersonController", BindingFlags.Instance | BindingFlags.NonPublic);
            _playerPausedField = typeof(PlayerController).GetField("isPaused", BindingFlags.Instance | BindingFlags.NonPublic);
            _cabinMikeControllerField = typeof(CabinGameManager).GetField("mikeController", BindingFlags.Instance | BindingFlags.NonPublic);
            _cabinMikeRizzlerControllerField = typeof(CabinGameManager).GetField("mikeRizzlerController", BindingFlags.Instance | BindingFlags.NonPublic);
            _cabinHikerControllerField = typeof(CabinGameManager).GetField("hikerCabinController", BindingFlags.Instance | BindingFlags.NonPublic);
            _cabinHasPlayedEerieHitForHikerField = typeof(CabinGameManager).GetField("hasPlayedEerieHitForHiker", BindingFlags.Instance | BindingFlags.NonPublic);

            SceneManager.activeSceneChanged += OnSceneChanged;
            CacheCabinHouseFields();
            CacheCabinGameFields();
            CacheSceneAdapterFields();
            InitializeSceneAdapters();
            CacheSceneObjects();
            OnSceneEnterAdapters(SceneManager.GetActiveScene().name);
            SceneDiscoveryManifest.LogIfDiscoveryOnlyScene("host", SceneManager.GetActiveScene().name, _logger, _sessionLogWrite);
            if (_settings.ModeSetting.Value == Mode.CoopHost)
            {
                SceneDiscoveryDump.LogIfEnabled(_settings, "host", SceneManager.GetActiveScene(), _logger, _sessionLogWrite);
            }
            BeginSceneHandshake();
        }

        public bool ClientSceneReady => _clientSceneReady;
        public string SessionStateLabel => _lifecycle != null ? _lifecycle.State.ToString() : "Disconnected";
        public int SessionGeneration => _sceneGeneration;
        public int SessionId => _lifecycle != null ? _lifecycle.CurrentSessionId : 0;
        public int LastSnapshotAckGeneration => _sceneHandshake.LastAcknowledgedSnapshotGeneration;
        public int SceneChangeRetryCount => _sceneHandshake.LastSentAttemptCount;
        public bool AwaitingSceneReady => _sceneHandshake.AwaitingReady;
        public string ClientSceneName => _clientSceneName;
        public long LastSceneRequestMs => _sceneHandshake.LastSceneRequestMs;
        public long LastSceneReadyMs => _sceneHandshake.LastSceneReadyMs;
        public long LastClientTransformMs => _lastClientTransformMs;
        public long LastHostTransformSendMs => _lastHostTransformSendMs;
        public long LastHostTransformSendUdpMs => _server.LastTransformSentUdpMs;
        public long LastHostTransformSendTcpMs => _server.LastTransformSentTcpMs;
        public int HostTransformSendCount => _server.TransformSendCount;
        public int DialogueConversationId => _dialogueConversationId;
        public int DialogueEntryId => _dialogueEntryId;
        public int DialogueChoiceIndex => _dialogueChoiceIndex;
        public long DialogueLastEventMs => _lastDialogueEventMs;
        public string LastStoryEventKey => _lastStoryEventKey;
        public int LastStoryEventValue => _lastStoryEventValue;
        public long LastStoryEventMs => _lastStoryEventMs;
        public string LastCabinMikeSyncDebug => _lastCabinMikeSyncDebug;
        public string NpcOverlaySummary => _npcOverlaySummary;
        public string BoardGameOverlaySummary => _boardGameOverlaySummary;
        public string VoiceOverlaySummary => _voiceChat != null ? _voiceChat.Summary : "Voice: off";
        public string HostWaitSummary => _hostWaitSummary;
        public bool ShouldShowHostWaitIndicator => ShouldShowHostWaitIndicatorInternal();
        public string HostWaitIndicatorTitle => BuildHostWaitIndicatorTitle();
        public string HostWaitIndicatorDetail => BuildHostWaitIndicatorDetail();

        public void DrawVoiceHud()
        {
            _voiceChat?.DrawHud("Proximity voice");
        }

        public void Shutdown()
        {
            ReleaseHostWait(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), "shutdown");
            SceneManager.activeSceneChanged -= OnSceneChanged;
            UnbindDialogueEvents();
            UnbindDialogueUiSelection();
            _voiceChat?.Shutdown();
            _clientAvatar.SetActive(false);
            if (_remotePlayer != null && _remotePlayer.Root != null)
            {
                UnityEngine.Object.Destroy(_remotePlayer.Root.gameObject);
            }
        }

        public void Update()
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (!_server.IsRunning)
            {
                ReleaseHostWait(nowMs, "server-stopped");
                if (_lifecycle.State != SessionState.Disconnected)
                {
                    _lifecycle.ForceDisconnect("server-stopped", nowMs);
                }
                return;
            }

            if (!_server.IsClientConnected && _lifecycle.State == SessionState.Disconnected)
            {
                _lifecycle.TryTransition(
                    SessionState.Connecting,
                    "server-started",
                    SceneManager.GetActiveScene().name,
                    _sceneGeneration,
                    _server.ActiveSessionId,
                    nowMs);
            }

            if (_server.IsClientConnected && !_lastClientConnected)
            {
                _lastClientConnected = true;
                _sceneHandshakeSessionId = _server.ActiveSessionId;
                _lifecycle.SetSessionId(_server.ActiveSessionId);
                if (_lifecycle.State == SessionState.Disconnected)
                {
                    _lifecycle.TryTransition(
                        SessionState.Connecting,
                        "server-started",
                        SceneManager.GetActiveScene().name,
                        _sceneGeneration,
                        _server.ActiveSessionId,
                        nowMs);
                }
                _lifecycle.TryTransition(
                    SessionState.Hello,
                    "client-tcp-accept",
                    SceneManager.GetActiveScene().name,
                    _sceneGeneration,
                    _server.ActiveSessionId,
                    nowMs);
                _logger.LogInfo("Co-op client connected, waiting for Hello session=" + _server.ActiveSessionId);
            }
            else if (!_server.IsClientConnected && _lastClientConnected)
            {
                _lastClientConnected = false;
                _sceneHandshakeSessionId = 0;
                _clientSceneReady = false;
                _awaitingSceneReady = false;
                _clientSceneName = string.Empty;
                _hasPendingClientTransform = false;
                _lastClientTransformMs = 0;
                _lastHostTransformSendMs = 0;
                _sceneHandshake.ResetReady();
                _lifecycle.ForceDisconnect("client-disconnected", nowMs);
                ReleaseHostWait(nowMs, "client-disconnected");
                if (_remotePlayer != null)
                {
                    _remotePlayer.SetActive(false);
                }
                _clientAvatar.SetActive(false);
            }

            EnsureRemotePlayer();
            DrainIncoming();
            BindDialogueUiSelection();

            // Bounded SceneChange retry; host-wait mode holds the host and continues slow heartbeats.
            if (_server.IsClientConnected && _lifecycle.State == SessionState.SceneSyncing &&
                _sceneHandshake.AwaitingReady &&
                nowMs - _sceneHandshake.LastSceneRequestMs >= GetSceneChangeRetryBackoffMs(_sceneHandshake.LastSentAttemptCount + 1))
            {
                if (_sceneHandshake.LastSentAttemptCount >= SceneHandshakeState.MaxAttempts)
                {
                    if (IsHostWaitEnabled())
                    {
                        LogHostWaitStill(nowMs, "scene-ready");
                        SendSceneChange(countAttempt: false);
                    }
                    else
                    {
                        _logger.LogWarning("Co-op session host: SceneChange exhausted reason=no-scene-ready scene=" +
                            _sceneHandshake.SceneName + " gen=" + _sceneGeneration +
                            " sid=" + _server.ActiveSessionId);
                        _awaitingSceneReady = false;
                        _sceneHandshake.ResetReady();
                        _lifecycle.TryTransition(
                            SessionState.Reconnecting,
                            "no-scene-ready",
                            _sceneHandshake.SceneName,
                            _sceneGeneration,
                            _server.ActiveSessionId,
                            nowMs);
                    }
                }
                else
                {
                    SendSceneChange();
                }
            }

            RetrySnapshotAckWait(nowMs);
            UpdateHostWaitGate(nowMs);

            if (_server.IsClientConnected && nowMs >= _nextPlayerSendMs)
            {
                SendPlayerTransform();
                _nextPlayerSendMs = nowMs + (long)Math.Max(1, 1000.0 / Math.Max(1, _settings.SendHz.Value));
            }

            if (_server.IsClientConnected && nowMs >= _nextInputSendMs)
            {
                SendInputState();
                _nextInputSendMs = nowMs + (long)Math.Max(1, 1000.0 / Math.Max(1, _settings.SendHz.Value));
            }

            // World/story deltas only after the client has applied the snapshot (Live).
            if (!_lifecycle.IsLive)
            {
                return;
            }

            UpdateVoice(nowMs);
            PollSubText();
            PollPhoneMirror(nowMs);

            if (nowMs >= _nextWorldSendMs)
            {
                SendDoorStates();
                SendHoldableStates();
                SendAiStates();
                SendNpcBrainDeltas();
                _nextWorldSendMs = nowMs + 200;
            }

            if (nowMs >= _nextStorySendMs)
            {
                SendStoryFlags();
                _nextStorySendMs = nowMs + 1000;
            }

            // Drift-recovery: only re-broadcast a full snapshot if the client has gone silent for a long time.
            if (_server.IsClientConnected &&
                nowMs - _lastDriftRecoveryMs >= DriftRecoveryCooldownMs &&
                _lastClientTransformMs > 0 &&
                nowMs - _lastClientTransformMs >= DriftRecoveryClientPktAgeMs)
            {
                _logger.LogWarning("Co-op session host: drift recovery, client silent for " +
                    ((nowMs - _lastClientTransformMs) / 1000) + "s; re-broadcasting snapshot");
                EmitSnapshot(force: true);
                _lastDriftRecoveryMs = nowMs;
            }
        }

        private void UpdateHostWaitGate(long nowMs)
        {
            string reason;
            var shouldPause = ShouldPauseHostForClient(out reason);
            if (shouldPause)
            {
                EnterOrMaintainHostWait(nowMs, reason);
            }
            else
            {
                ReleaseHostWait(nowMs, "not-waiting");
            }

            RefreshHostWaitSummary(nowMs);
        }

        private bool IsHostWaitEnabled()
        {
            return _settings != null &&
                   _settings.CoopHostWaitForClient != null &&
                   _settings.CoopHostWaitForClient.Value;
        }

        private bool IsHostWaitIndicatorEnabled()
        {
            return _settings != null &&
                   _settings.CoopHostWaitIndicator != null &&
                   _settings.CoopHostWaitIndicator.Value;
        }

        private bool ShouldPauseHostForClient(out string reason)
        {
            reason = "-";
            if (!IsHostWaitEnabled() ||
                _server == null ||
                !_server.IsRunning ||
                !_server.IsClientConnected ||
                _lifecycle == null ||
                _lifecycle.State == SessionState.Disconnected ||
                _lifecycle.State == SessionState.Connecting ||
                _lifecycle.State == SessionState.Hello ||
                _lifecycle.IsLive)
            {
                return false;
            }

            if (_sceneHandshake.AwaitingReady || _lifecycle.State == SessionState.SceneSyncing)
            {
                reason = "scene-ready";
                return true;
            }

            if (_lifecycle.State == SessionState.SceneReady ||
                _lifecycle.State == SessionState.SnapshotApplying ||
                (_sceneHandshake.LastSnapshotEndGeneration == _sceneHandshake.Generation &&
                 !_sceneHandshake.IsSnapshotAcknowledged()))
            {
                reason = "snapshot-ack";
                return true;
            }

            if (_lifecycle.State == SessionState.Reconnecting)
            {
                reason = "reconnect";
                return true;
            }

            return false;
        }

        private bool ShouldShowHostWaitIndicatorInternal()
        {
            if (!IsHostWaitIndicatorEnabled() || _server == null || !_server.IsRunning) return false;
            if (!_server.IsClientConnected) return true;
            if (_hostWaitActive) return true;
            if (_lifecycle == null) return false;
            return _lifecycle.State == SessionState.Hello ||
                   _lifecycle.State == SessionState.Connecting ||
                   (_lifecycle.IsAtLeast(SessionState.Connected) && !_lifecycle.IsLive);
        }

        private void EnterOrMaintainHostWait(long nowMs, string reason)
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (!_hostWaitActive)
            {
                _hostWaitActive = true;
                _hostWaitReason = string.IsNullOrEmpty(reason) ? "-" : reason;
                _hostWaitScene = string.IsNullOrEmpty(sceneName) ? "-" : sceneName;
                _hostWaitEnteredMs = nowMs;
                _hostWaitNextStillLogMs = nowMs + HostWaitStillLogIntervalMs;
                _hostWaitPreviousTimeScale = Time.timeScale;
                _hostWaitPlayerPauseCaptured = false;
                Time.timeScale = 0f;
                ApplyHostPlayerPause();
                LogHostWait("enter", _hostWaitReason, nowMs, "timeScale=" + _hostWaitPreviousTimeScale.ToString("0.###"));
                return;
            }

            _hostWaitReason = string.IsNullOrEmpty(reason) ? _hostWaitReason : reason;
            _hostWaitScene = string.IsNullOrEmpty(sceneName) ? _hostWaitScene : sceneName;
            if (Mathf.Abs(Time.timeScale) > 0.0001f)
            {
                Time.timeScale = 0f;
            }
            ApplyHostPlayerPause();
            LogHostWaitStill(nowMs, _hostWaitReason);
        }

        private void ReleaseHostWait(long nowMs, string reason)
        {
            if (!_hostWaitActive)
            {
                RefreshHostWaitSummary(nowMs);
                return;
            }

            var heldMs = Math.Max(0, nowMs - _hostWaitEnteredMs);
            Time.timeScale = _hostWaitPreviousTimeScale;
            RestoreHostPlayerPause();
            LogHostWait("leave", string.IsNullOrEmpty(reason) ? "released" : reason, nowMs,
                "heldMs=" + heldMs + " restoredTimeScale=" + _hostWaitPreviousTimeScale.ToString("0.###"));

            _hostWaitActive = false;
            _hostWaitPlayerPauseCaptured = false;
            _hostWaitOriginalPlayerPaused = false;
            _hostWaitReason = "-";
            _hostWaitScene = "-";
            _hostWaitEnteredMs = 0;
            _hostWaitNextStillLogMs = 0;
            _hostWaitNextSnapshotRetryMs = 0;
            RefreshHostWaitSummary(nowMs);
        }

        private void ApplyHostPlayerPause()
        {
            var player = PlayerController.GetInstance();
            if (player == null) return;

            if (!_hostWaitPlayerPauseCaptured)
            {
                _hostWaitOriginalPlayerPaused = player.IsPlayerPaused();
                _hostWaitPlayerPauseCaptured = true;
            }

            SetPlayerPaused(player, true);
        }

        private void RestoreHostPlayerPause()
        {
            if (!_hostWaitPlayerPauseCaptured) return;
            var player = PlayerController.GetInstance();
            if (player == null) return;
            SetPlayerPaused(player, _hostWaitOriginalPlayerPaused);
        }

        private void SetPlayerPaused(PlayerController player, bool paused)
        {
            if (player == null) return;
            try
            {
                if (_playerPausedField != null)
                {
                    _playerPausedField.SetValue(player, paused);
                }
            }
            catch (Exception ex)
            {
                if (_settings.VerboseLogging.Value)
                {
                    _logger.LogWarning("Co-op host wait: failed to set player pause=" + paused + " reason=" + ex.Message);
                }
            }
        }

        private void RetrySnapshotAckWait(long nowMs)
        {
            if (!IsHostWaitEnabled() ||
                !_server.IsClientConnected ||
                _lifecycle.State != SessionState.SnapshotApplying ||
                _sceneHandshake.IsSnapshotAcknowledged() ||
                _sceneHandshake.LastSnapshotEndGeneration != _sceneHandshake.Generation)
            {
                _hostWaitNextSnapshotRetryMs = 0;
                return;
            }

            if (_hostWaitNextSnapshotRetryMs <= 0)
            {
                _hostWaitNextSnapshotRetryMs = nowMs + HostWaitSnapshotRetryMs;
                return;
            }

            if (nowMs < _hostWaitNextSnapshotRetryMs) return;

            _logger.LogWarning("Co-op host wait: snapshot ack timeout; re-emitting snapshot scene=" +
                SceneManager.GetActiveScene().name +
                " gen=" + _sceneHandshake.Generation +
                " sid=" + _server.ActiveSessionId);
            _sessionLogWrite?.Invoke("Co-op host wait: snapshot ack timeout; re-emitting snapshot scene=" +
                SceneManager.GetActiveScene().name +
                " gen=" + _sceneHandshake.Generation +
                " sid=" + _server.ActiveSessionId);
            EmitSnapshot(force: true);
            SendPlayerTransform();
            _hostWaitNextSnapshotRetryMs = nowMs + HostWaitSnapshotRetryMs;
        }

        private void LogHostWaitStill(long nowMs, string reason)
        {
            if (nowMs < _hostWaitNextStillLogMs) return;
            _hostWaitNextStillLogMs = nowMs + HostWaitStillLogIntervalMs;
            LogHostWait("still", string.IsNullOrEmpty(reason) ? _hostWaitReason : reason, nowMs, string.Empty);
        }

        private void LogHostWait(string action, string reason, long nowMs, string extra)
        {
            var heldMs = _hostWaitEnteredMs > 0 ? Math.Max(0, nowMs - _hostWaitEnteredMs) : 0;
            var line = "Co-op host wait: " + action +
                " reason=" + (string.IsNullOrEmpty(reason) ? "-" : reason) +
                " state=" + (_lifecycle != null ? _lifecycle.State.ToString() : "-") +
                " scene=" + (string.IsNullOrEmpty(_hostWaitScene) ? SceneManager.GetActiveScene().name : _hostWaitScene) +
                " gen=" + _sceneGeneration +
                " sid=" + (_server != null ? _server.ActiveSessionId : 0) +
                " retry=" + _sceneHandshake.LastSentAttemptCount +
                " age=" + FormatSeconds(heldMs);
            if (!string.IsNullOrEmpty(extra))
            {
                line += " " + extra;
            }

            _logger.LogInfo(line);
            _sessionLogWrite?.Invoke(line);
        }

        private void RefreshHostWaitSummary(long nowMs)
        {
            if (!IsHostWaitEnabled())
            {
                _hostWaitSummary = "HostWait: off";
                return;
            }

            if (_server == null || !_server.IsRunning)
            {
                _hostWaitSummary = "HostWait: no";
                return;
            }

            if (!_server.IsClientConnected)
            {
                _hostWaitSummary = "HostWait: join reason=client";
                return;
            }

            if (!_hostWaitActive)
            {
                _hostWaitSummary = "HostWait: no";
                return;
            }

            _hostWaitSummary = "HostWait: yes reason=" + _hostWaitReason +
                " scene=" + _hostWaitScene +
                " age=" + FormatSeconds(Math.Max(0, nowMs - _hostWaitEnteredMs));
        }

        private string BuildHostWaitIndicatorTitle()
        {
            if (_server == null || !_server.IsRunning) return string.Empty;
            if (!_server.IsClientConnected) return "Waiting for client to join...";
            if (_lifecycle != null && _lifecycle.State == SessionState.Hello) return "Negotiating co-op session...";
            if (_lifecycle != null && _lifecycle.State == SessionState.Reconnecting) return "Waiting for client to reconnect...";

            var reason = _hostWaitReason;
            if (string.Equals(reason, "scene-ready", StringComparison.Ordinal))
            {
                return "Waiting for client to load " + SceneManager.GetActiveScene().name + "...";
            }

            if (string.Equals(reason, "snapshot-ack", StringComparison.Ordinal))
            {
                return "Syncing world state...";
            }

            return "Waiting for client...";
        }

        private string BuildHostWaitIndicatorDetail()
        {
            if (_server == null || !_server.IsRunning) return string.Empty;
            if (!_server.IsClientConnected)
            {
                return "Start or connect the co-op client. The host is still usable until a client is accepted.";
            }

            var age = _hostWaitActive && _hostWaitEnteredMs > 0
                ? FormatSeconds(Math.Max(0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _hostWaitEnteredMs))
                : "0.0s";
            return "state=" + (_lifecycle != null ? _lifecycle.State.ToString() : "-") +
                   " scene=" + SceneManager.GetActiveScene().name +
                   " gen=" + _sceneGeneration +
                   " sid=" + _server.ActiveSessionId +
                   " retry=" + _sceneHandshake.LastSentAttemptCount +
                   " age=" + age;
        }

        private static string FormatSeconds(long milliseconds)
        {
            return (milliseconds / 1000.0).ToString("0.0") + "s";
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

        private void UpdateVoice(long nowMs)
        {
            if (_voiceChat == null) return;
            _voiceChat.UpdatePlaybackAnchor(ResolveRemoteVoiceAnchor());
            _voiceChat.UpdateLocalCapture(
                _lifecycle.CurrentSessionId,
                _sceneGeneration,
                SceneManager.GetActiveScene().name,
                VoiceChatSync.HostSenderRole,
                state =>
                {
                    var message = new VoiceFrameMessage(state);
                    if (_settings.UdpEnabled.Value && _server.HasUdp)
                    {
                        _server.SendUdp(message);
                    }
                    else
                    {
                        _server.Enqueue(message);
                    }
                    return true;
                });
        }

        private Transform ResolveRemoteVoiceAnchor()
        {
            if (_remotePlayer != null && _remotePlayer.Root != null)
            {
                return _remotePlayer.Root;
            }

            return _clientAvatar != null ? _clientAvatar.Root : null;
        }

        private void MaybeHandleRemoteVoiceReaction(VoiceFrameState state, long nowMs)
        {
            if (_voiceChat == null || !_voiceChat.RemoteIsLoud || nowMs < _nextRemoteVoiceReactionMs)
            {
                return;
            }

            _nextRemoteVoiceReactionMs = nowMs + 1500;
            if (!IsCabinSceneName(SceneManager.GetActiveScene().name))
            {
                return;
            }

            if (TryTriggerRemoteVoiceDeath(nowMs))
            {
                return;
            }

            TryTriggerRemoteVoiceHikerReaction(nowMs);
        }

        private bool TryTriggerRemoteVoiceDeath(long nowMs)
        {
            var death = DeathManager.instance;
            if (death == null)
            {
                death = UnityEngine.Object.FindObjectOfType<DeathManager>();
            }

            if (death == null || death.isDead || (!death.hostIsChasing && !death.hostIsHaunting))
            {
                return false;
            }

            try
            {
                death.playerCaught = true;
                death.hostIsChasing = false;
                death.hostIsHaunting = false;
                death.StartCoroutine(death.PerformJumpscareAtRun(true));
                var line = "Co-op voice host: remote shout triggered shared death scene=" +
                           SceneManager.GetActiveScene().name + " gen=" + _sceneGeneration +
                           " sid=" + _lifecycle.CurrentSessionId;
                _logger.LogWarning(line);
                _sessionLogWrite?.Invoke(line);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Co-op voice host: remote shout death trigger failed reason=" + ex.Message);
                return false;
            }
        }

        private bool TryTriggerRemoteVoiceHikerReaction(long nowMs)
        {
            if (_cabinGameManager == null || _cabinGameManager.CurrentSequence != SequenceType.HikerSequence)
            {
                return false;
            }

            var hiker = _cabinHikerControllerField != null
                ? _cabinHikerControllerField.GetValue(_cabinGameManager) as HikerCabinController
                : UnityEngine.Object.FindObjectOfType<HikerCabinController>();
            if (hiker == null || !hiker.canSuddenlyLookAtPlayer)
            {
                return false;
            }

            var alreadyReacted = false;
            try
            {
                alreadyReacted = _cabinHasPlayedEerieHitForHikerField != null &&
                                 (bool)_cabinHasPlayedEerieHitForHikerField.GetValue(_cabinGameManager);
            }
            catch { }

            if (alreadyReacted)
            {
                return false;
            }

            var clientIsCrouching = _hasInputState && _lastInputState.Crouch;
            var hostIsCrouching = false;
            try
            {
                var player = PlayerController.GetInstance();
                var fpc = player != null && _playerFirstPersonField != null
                    ? _playerFirstPersonField.GetValue(player)
                    : null;
                var crouchField = fpc != null
                    ? fpc.GetType().GetField("isCrouching", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    : null;
                hostIsCrouching = crouchField != null && (bool)crouchField.GetValue(fpc);
            }
            catch { }

            if (!clientIsCrouching && !hostIsCrouching)
            {
                return false;
            }

            try
            {
                hiker.LookAtPlayerSuddenly();
                if (_cabinHasPlayedEerieHitForHikerField != null)
                {
                    _cabinHasPlayedEerieHitForHikerField.SetValue(_cabinGameManager, true);
                }
                if (GenericAudioReferences.instance != null && GenericAudioReferences.instance.eerieHit != null)
                {
                    GenericAudioReferences.instance.eerieHit.Play();
                }
                var line = "Co-op voice host: remote shout triggered hiker reaction crouchClient=" +
                           (clientIsCrouching ? "yes" : "no") +
                           " crouchHost=" + (hostIsCrouching ? "yes" : "no") +
                           " gen=" + _sceneGeneration +
                           " sid=" + _lifecycle.CurrentSessionId;
                _logger.LogWarning(line);
                _sessionLogWrite?.Invoke(line);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Co-op voice host: remote shout hiker trigger failed reason=" + ex.Message);
                return false;
            }
        }

        private void DrainIncoming()
        {
            var nowSeconds = Time.realtimeSinceStartup;
            while (_server.TryDequeueIncoming(out var message))
            {
                if (message is HelloMessage hello)
                {
                    HandleHello(hello);
                }
                else if (message is SnapshotAckMessage ack)
                {
                    HandleSnapshotAck(ack);
                }
                else if (message is SceneReadyMessage ready)
                {
                    HandleSceneReady(ready);
                }
                else if (message is PlayerTransformMessage playerTransform)
                {
                    _lastClientTransformMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    if (!_lifecycle.IsLive)
                    {
                        _lifecycle.NoteDrop("PlayerTransform", "tcp", nowSeconds);
                        _pendingClientTransform = playerTransform.State;
                        _hasPendingClientTransform = true;
                        continue;
                    }

                    SetClientAvatar(playerTransform.State);
                }
                else if (message is InteractRequestMessage interact)
                {
                    if (!_lifecycle.IsLive)
                    {
                        _lifecycle.NoteDrop("InteractRequest", interact.TargetPath, nowSeconds);
                        continue;
                    }
                    HandleInteract(interact);
                }
                else if (message is PlayerInputMessage input)
                {
                    if (!_lifecycle.IsLive)
                    {
                        _lifecycle.NoteDrop("PlayerInput", null, nowSeconds);
                        continue;
                    }
                    _lastInputState = input.State;
                    _hasInputState = true;
                    _remotePlayer?.ApplyInput(input.State);
                }
                else if (message is VoiceFrameMessage voice)
                {
                    if (!_lifecycle.IsLive)
                    {
                        _lifecycle.NoteDrop("VoiceFrame", null, nowSeconds);
                        continue;
                    }

                    _voiceChat?.HandleRemoteFrame(voice.State, ResolveRemoteVoiceAnchor());
                    MaybeHandleRemoteVoiceReaction(voice.State, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                }
                else if (message is SceneActionIntentMessage intent)
                {
                    _logger.LogInfo("SceneActionIntent host received action=" +
                        (string.IsNullOrEmpty(intent.State.ActionId) ? "-" : intent.State.ActionId) +
                        " target=" + (string.IsNullOrEmpty(intent.State.TargetPath) ? "-" : intent.State.TargetPath) +
                        " scene=" + (string.IsNullOrEmpty(intent.State.SceneName) ? "-" : intent.State.SceneName));
                }
                else if (message is PingMessage)
                {
                    if (TryBuildHostTransform(out var state))
                    {
                        _server.UpdateHostTransform(state);
                        _server.Enqueue(new PongMessage(state));
                    }
                    else if (_server.TryGetHostTransform(out var cached))
                    {
                        _server.Enqueue(new PongMessage(cached));
                    }
                    else
                    {
                        _server.Enqueue(new PongMessage());
                    }
                }
            }
        }

        private void HandleHello(HelloMessage hello)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _hostDisplayName = PlayerDisplayName.Resolve(_settings, "HOST");
            if (_lifecycle.State != SessionState.Hello)
            {
                _logger.LogWarning("Co-op session host: ignoring stray Hello in state " + _lifecycle.State);
                return;
            }

            if (hello.ProtocolVersion != Protocol.Version)
            {
                var reason = "version-mismatch host=" + Protocol.Version + " client=" + hello.ProtocolVersion;
                _logger.LogWarning("Co-op session host: rejecting client " + reason);
                _server.Enqueue(new HelloAckMessage(Protocol.Version, Protocol.PluginVersion, _server.ActiveSessionId, false, reason, _hostDisplayName));
                _lifecycle.ForceDisconnect("hello-rejected:" + reason, nowMs);
                _sceneHandshake.ResetReady();
                _awaitingSceneReady = false;
                _clientSceneReady = false;
                return;
            }

            if (!string.Equals(hello.PluginVersion, Protocol.PluginVersion, StringComparison.Ordinal))
            {
                var reason = "plugin-version-mismatch host=" + Protocol.PluginVersion + " client=" + hello.PluginVersion;
                _logger.LogWarning("Co-op session host: rejecting client " + reason);
                _server.Enqueue(new HelloAckMessage(Protocol.Version, Protocol.PluginVersion, _server.ActiveSessionId, false, reason, _hostDisplayName));
                _lifecycle.ForceDisconnect("hello-rejected:" + reason, nowMs);
                _sceneHandshake.ResetReady();
                _awaitingSceneReady = false;
                _clientSceneReady = false;
                return;
            }

            _clientDisplayName = PlayerDisplayName.Normalize(hello.DisplayName, "CLIENT");
            _server.Enqueue(new HelloAckMessage(Protocol.Version, Protocol.PluginVersion, _server.ActiveSessionId, true, "ok", _hostDisplayName));
            UpdateRemotePlayerNameTag();
            _lifecycle.SetSessionId(_server.ActiveSessionId);
            _lifecycle.TryTransition(
                SessionState.Connected,
                "hello-ack-sent",
                SceneManager.GetActiveScene().name,
                _sceneGeneration,
                _server.ActiveSessionId,
                nowMs);
            _logger.LogInfo("Co-op session host: Hello accepted clientDisplayName=" + _clientDisplayName +
                " hostDisplayName=" + _hostDisplayName);
            // Now that the handshake is complete, kick off the scene handshake.
            BeginSceneHandshake();
        }

        private void HandleSnapshotAck(SnapshotAckMessage ack)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (ack.SessionId != _server.ActiveSessionId)
            {
                _logger.LogWarning("Co-op session host: SnapshotAck stale session=" + ack.SessionId +
                    " active=" + _server.ActiveSessionId + " ignoring");
                return;
            }

            if (!_sceneHandshake.AcknowledgeSnapshot(ack.Generation))
            {
                if (_settings.VerboseLogging.Value)
                {
                    _logger.LogInfo("Co-op session host: SnapshotAck duplicate gen=" + ack.Generation +
                        " current=" + _sceneHandshake.Generation + " ignored");
                }
                return;
            }

            if (!ack.Ok)
            {
                _logger.LogWarning("Co-op session host: SnapshotAck reported failure reason=" + ack.Reason +
                    " applied=" + ack.AppliedItemCount +
                    " pending=" + ack.PendingObjectCount +
                    " missing=" + ack.MissingObjectCount +
                    " gen=" + ack.Generation);
            }
            else
            {
                _logger.LogInfo("Co-op session host: SnapshotAck applied story=" + ack.AppliedStoryCount +
                    " doors=" + ack.AppliedDoorCount +
                    " holdables=" + ack.AppliedHoldableCount +
                    " ai=" + ack.AppliedAiCount +
                    " dialogue=" + ack.AppliedDialogueCount +
                    " players=" + ack.AppliedPlayerCount +
                    " custom=" + ack.AppliedCustomCount +
                    " pending=" + ack.PendingObjectCount +
                    " missing=" + ack.MissingObjectCount +
                    " gen=" + ack.Generation + " scene=" + ack.SceneName);
            }

            if (ack.PendingObjectCount > 0 ||
                ack.MissingObjectCount > 0 ||
                ack.AppliedDoorCount < _lastSnapshotCounts.Door ||
                ack.AppliedHoldableCount < _lastSnapshotCounts.Holdable ||
                ack.AppliedCustomCount < _lastSnapshotCounts.Custom)
            {
                _logger.LogWarning("Co-op session host: SnapshotAck incomplete expected story=" +
                    _lastSnapshotCounts.Story +
                    " doors=" + _lastSnapshotCounts.Door +
                    " holdables=" + _lastSnapshotCounts.Holdable +
                    " ai=" + _lastSnapshotCounts.Ai +
                    " dialogue=" + _lastSnapshotCounts.Dialogue +
                    " players=" + _lastSnapshotCounts.Player +
                    " custom=" + _lastSnapshotCounts.Custom +
                    " appliedStory=" + ack.AppliedStoryCount +
                    " appliedDoors=" + ack.AppliedDoorCount +
                    " appliedHoldables=" + ack.AppliedHoldableCount +
                    " appliedAi=" + ack.AppliedAiCount +
                    " appliedDialogue=" + ack.AppliedDialogueCount +
                    " appliedPlayers=" + ack.AppliedPlayerCount +
                    " appliedCustom=" + ack.AppliedCustomCount +
                    " pending=" + ack.PendingObjectCount +
                    " missing=" + ack.MissingObjectCount);
            }

            _lifecycle.TryTransition(
                SessionState.Live,
                "snapshot-ack",
                ack.SceneName,
                ack.Generation,
                ack.SessionId,
                nowMs);
            _hostWaitNextSnapshotRetryMs = 0;
            _nextWorldSendMs = nowMs;
            _nextStorySendMs = nowMs;
            _lastDriftRecoveryMs = nowMs;

            if (_hasPendingClientTransform)
            {
                SetClientAvatar(_pendingClientTransform);
                _hasPendingClientTransform = false;
            }
        }

        private void HandleInteract(InteractRequestMessage interact)
        {
            var target = NetPath.FindByPath(interact.TargetPath);
            if (target == null) return;

            if (!TryGetInteractionSource(out var source))
            {
                return;
            }

            if (!ValidateInteractionDistance(source, target))
            {
                return;
            }

            if (!ValidateInteractionLineOfSight(source, target))
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

        private bool TryGetInteractionSource(out Vector3 source)
        {
            source = Vector3.zero;
            if (_remotePlayer != null)
            {
                if (_remotePlayer.CameraTransform != null)
                {
                    source = _remotePlayer.CameraTransform.position;
                    return true;
                }

                if (_remotePlayer.Root != null)
                {
                    source = _remotePlayer.Root.position;
                    return true;
                }
            }

            if (_clientAvatar != null && _clientAvatar.CameraAnchor != null)
            {
                source = _clientAvatar.CameraAnchor.position;
                return true;
            }

            return false;
        }

        private static Vector3 GetInteractionTargetPoint(Transform target)
        {
            if (target == null) return Vector3.zero;
            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                return collider.bounds.center;
            }

            return target.position;
        }

        private static bool IsSameOrChild(Transform a, Transform b)
        {
            if (a == null || b == null) return false;
            return a == b || a.IsChildOf(b) || b.IsChildOf(a);
        }

        private bool ValidateInteractionDistance(Vector3 source, Transform target)
        {
            var point = GetInteractionTargetPoint(target);
            return Vector3.Distance(source, point) <= MaxInteractDistanceMeters;
        }

        private bool ValidateInteractionLineOfSight(Vector3 source, Transform target)
        {
            var point = GetInteractionTargetPoint(target);
            var direction = point - source;
            var distance = direction.magnitude;
            if (distance <= 0.001f)
            {
                return true;
            }

            direction /= distance;
            var maxDistance = distance + InteractLosPaddingMeters;
            if (!Physics.Raycast(source, direction, out var hit, maxDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            return IsSameOrChild(hit.transform, target);
        }

        private void SendPlayerTransform()
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (!TryBuildHostTransform(out var state)) return;

            _server.PublishHostTransform(state);
            _lastHostTransformSendMs = nowMs;
        }

        private void SendInputState()
        {
            if (!TryBuildHostInput(out var state))
            {
                return;
            }

            _server.Enqueue(new PlayerInputMessage(state));
        }

        private bool TryBuildHostTransform(out PlayerTransformState state)
        {
            state = default;
            var playerController = PlayerController.GetInstance();
            var fps = playerController != null && _playerFirstPersonField != null
                ? _playerFirstPersonField.GetValue(playerController) as FirstPersonController
                : null;

            var camera = CabinPlayerBodyPose.ResolveCamera(fps);
            if (camera == null)
            {
                var cameras = UnityEngine.Object.FindObjectsOfType<Camera>();
                foreach (var candidate in cameras)
                {
                    if (candidate != null && candidate.enabled)
                    {
                        camera = candidate;
                        break;
                    }
                }
                if (camera == null && cameras.Length > 0)
                {
                    camera = cameras[0];
                }
            }

            if (fps == null && playerController == null && camera == null)
            {
                return false;
            }

            if (!CabinPlayerBodyPose.TryResolve(fps, playerController, out var baseTransform, out _))
            {
                baseTransform = camera != null ? camera.transform : null;
            }
            if (baseTransform == null)
            {
                return false;
            }

            var cameraTransform = camera != null ? camera.transform : baseTransform;

            state = new PlayerTransformState
            {
                PlayerId = 0,
                Position = baseTransform.position,
                Rotation = baseTransform.rotation,
                CameraPosition = cameraTransform.position,
                CameraRotation = cameraTransform.rotation
            };
            return true;
        }

        private bool TryBuildHostInput(out PlayerInputState state)
        {
            state = default;

            var playerController = PlayerController.GetInstance();
            var fps = playerController != null && _playerFirstPersonField != null
                ? _playerFirstPersonField.GetValue(playerController) as FirstPersonController
                : null;

            var camera = CabinPlayerBodyPose.ResolveCamera(fps);
            if (camera == null)
            {
                var cameras = UnityEngine.Object.FindObjectsOfType<Camera>();
                foreach (var candidate in cameras)
                {
                    if (candidate != null && candidate.enabled)
                    {
                        camera = candidate;
                        break;
                    }
                }

                if (camera == null && cameras.Length > 0)
                {
                    camera = cameras[0];
                }
            }

            if (fps == null && playerController == null && camera == null)
            {
                return false;
            }

            var look = camera != null
                ? camera.transform.rotation.eulerAngles
                : (fps != null ? fps.transform.rotation.eulerAngles : playerController.transform.rotation.eulerAngles);

            state = new PlayerInputState
            {
                PlayerId = 0,
                MoveX = Input.GetAxisRaw("Horizontal"),
                MoveY = Input.GetAxisRaw("Vertical"),
                LookYaw = look.y,
                LookPitch = look.x,
                Jump = Input.GetKey(KeyCode.Space),
                Crouch = Input.GetKey(KeyCode.LeftControl),
                Sprint = Input.GetKey(KeyCode.LeftShift)
            };

            return true;
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
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var key in _storyKeys)
            {
                var value = PlayerPrefs.GetInt(key, 0);
                if (!_storyFlags.TryGetValue(key, out var last) || last != value)
                {
                    _storyFlags[key] = value;
                    _lastStoryEventKey = key;
                    _lastStoryEventValue = value;
                    _lastStoryEventMs = nowMs;
                    _server.Enqueue(new StoryFlagMessage(key, value));
                }
            }

            EmitSceneAdapterSnapshot(nowMs);
        }

        private void SendCabinHouseFlags()
        {
            if (_cabinHouseBoolFields.Count == 0) return;

            if (_cabinHouseManager == null)
            {
                _cabinHouseManager = UnityEngine.Object.FindObjectOfType<CabinHouseManager>();
                if (_cabinHouseManager == null) return;
            }

            foreach (var field in _cabinHouseBoolFields)
            {
                var value = (bool)field.GetValue(_cabinHouseManager) ? 1 : 0;
                var key = CabinHouseFlagPrefix + field.Name;
                if (!_cabinHouseFlags.TryGetValue(key, out var last) || last != value)
                {
                    _cabinHouseFlags[key] = value;
                    _lastStoryEventKey = key;
                    _lastStoryEventValue = value;
                    _lastStoryEventMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    _server.Enqueue(new StoryFlagMessage(key, value));
                }
            }

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            EmitGameObjectActiveFlags(
                _cabinHouseManager,
                _cabinHouseActiveFieldNames,
                _cabinHouseActiveFieldCache,
                _cabinHouseFlags,
                CabinHouseActivePrefix,
                nowMs);
            EmitGameObjectArrayActiveFlags(
                _cabinHouseManager,
                _cabinHouseActiveArrayFieldNames,
                _cabinHouseActiveFieldCache,
                _cabinHouseFlags,
                CabinHouseActivePrefix,
                nowMs);
        }

        private void SendCabinGameFlags()
        {
            if (_cabinGameFields.Count == 0) return;

            if (_cabinGameManager == null)
            {
                _cabinGameManager = UnityEngine.Object.FindObjectOfType<CabinGameManager>();
                if (_cabinGameManager == null) return;
            }

            foreach (var field in _cabinGameFields)
            {
                var rawValue = field.GetValue(_cabinGameManager);
                var value = 0;
                if (field.FieldType == typeof(bool))
                {
                    value = (bool)rawValue ? 1 : 0;
                }
                else if (field.FieldType.IsEnum)
                {
                    value = Convert.ToInt32(rawValue);
                }
                else if (field.FieldType == typeof(int))
                {
                    value = (int)rawValue;
                }

                var key = CabinGameFlagPrefix + field.Name;
                if (!_cabinGameFlags.TryGetValue(key, out var last) || last != value)
                {
                    _cabinGameFlags[key] = value;
                    _lastStoryEventKey = key;
                    _lastStoryEventValue = value;
                    _lastStoryEventMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    _server.Enqueue(new StoryFlagMessage(key, value));
                }
            }

            var cabinNowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            SendCabinCookingPropFlags(cabinNowMs);
            SendCabinBoardGameFlags(cabinNowMs);
            SendCabinAmbientFlags(cabinNowMs);
            SendCabinTrafficFlags(cabinNowMs);
            SendCabinPostEatingFlags(cabinNowMs);
            SendCabinDeathFlags(cabinNowMs);
            SendCabinHikerFlags(cabinNowMs);
        }

        private void SendCabinTrafficFlags(long nowMs)
        {
            if (!string.Equals(SceneManager.GetActiveScene().name, "CabinScene", StringComparison.Ordinal))
            {
                return;
            }

            CabinTrafficSync.EmitHostFlags(
                CabinGameFlagPrefix,
                (key, value) => EmitStoryFlagIfChanged(_cabinGameFlags, key, value, nowMs));
        }

        private void SendCabinCookingPropFlags(long nowMs)
        {
            if (_cabinGameManager == null) return;

            var hash = CabinCookingPropSync.EmitHostFlags(
                _cabinGameManager,
                CabinGameFlagPrefix,
                (key, value) => EmitStoryFlagIfChanged(_cabinGameFlags, key, value, nowMs));

            if (hash != 0 && (hash != _lastCabinCookingPropHash || nowMs >= _nextCabinCookingPropLogMs))
            {
                _lastCabinCookingPropHash = hash;
                _nextCabinCookingPropLogMs = nowMs + 10000;
                _logger.LogInfo("Cabin cooking prop host send hash=" + hash +
                                " seq=" + _cabinGameManager.CurrentSequence);
            }
        }

        private void SendCabinBoardGameFlags(long nowMs)
        {
            if (_cabinGameManager == null) return;

            var hash = CabinBoardGameSync.EmitHostFlags(
                _cabinGameManager,
                CabinGameFlagPrefix,
                (key, value) => EmitStoryFlagIfChanged(_cabinGameFlags, key, value, nowMs));

            _boardGameOverlaySummary = CabinBoardGameSync.BuildOverlaySummary(_cabinGameManager);

            if (hash != 0 && (hash != _lastCabinBoardGameHash || nowMs >= _nextCabinBoardGameLogMs))
            {
                _lastCabinBoardGameHash = hash;
                _nextCabinBoardGameLogMs = nowMs + 10000;
                _logger.LogInfo("Cabin board game host send hash=" + hash +
                                " seq=" + _cabinGameManager.CurrentSequence);
            }
        }

        private void SendCabinAmbientFlags(long nowMs)
        {
            if (_cabinGameManager == null) return;

            var hash = CabinAmbientSync.EmitHostFlags(
                _cabinGameManager,
                CabinGameFlagPrefix,
                (key, value) => EmitStoryFlagIfChanged(_cabinGameFlags, key, value, nowMs));

            if (hash != 0 && (hash != _lastCabinAmbientHash || nowMs >= _nextCabinAmbientLogMs))
            {
                _lastCabinAmbientHash = hash;
                _nextCabinAmbientLogMs = nowMs + 10000;
                _logger.LogInfo("Cabin ambient host send hash=" + hash +
                                " seq=" + _cabinGameManager.CurrentSequence);
            }
        }

        private void SendCabinPostEatingFlags(long nowMs)
        {
            if (_cabinGameManager == null) return;

            var mike = _cabinGameManager.mikePostEating;
            if (mike == null)
            {
                mike = UnityEngine.Object.FindObjectOfType<MikePostEating>();
            }

            if (mike != null)
            {
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinPostEatingFlagPrefix + "Active", mike.gameObject.activeInHierarchy ? 1 : 0, nowMs);
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinPostEatingFlagPrefix + "state", Convert.ToInt32(mike.state), nowMs);
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinPostEatingFlagPrefix + "startedSeeking", mike.startedSeeking ? 1 : 0, nowMs);
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinPostEatingFlagPrefix + "mikeOnStairs", mike.mikeOnStairs ? 1 : 0, nowMs);
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinPostEatingFlagPrefix + "playerCloseToStairs", mike.playerCloseToStairs ? 1 : 0, nowMs);
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinPostEatingFlagPrefix + "playerFound", mike.playerFound ? 1 : 0, nowMs);
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinPostEatingFlagPrefix + "knockConvoEnded", mike.knockConvoEnded ? 1 : 0, nowMs);
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinPostEatingFlagPrefix + "waitOutsideCloset", mike.waitOutsideCloset ? 1 : 0, nowMs);
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinPostEatingFlagPrefix + "catJumpscareDone", mike.catJumpscareDone ? 1 : 0, nowMs);
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinPostEatingFlagPrefix + "goingToToolShed", mike.goingToToolShed ? 1 : 0, nowMs);
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinPostEatingFlagPrefix + "hidingSeq1", mike.hidingSeq1 ? 1 : 0, nowMs);
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinPostEatingFlagPrefix + "mikeInBackyard", mike.mikeInBackyard ? 1 : 0, nowMs);
                if (mike.bedroomBlockingCollider != null)
                {
                    EmitStoryFlagIfChanged(_cabinGameFlags, CabinPostEatingActivePrefix + "bedroomBlockingCollider", mike.bedroomBlockingCollider.activeSelf ? 1 : 0, nowMs);
                }
            }

            var house = _cabinGameManager.cabinHouseManager;
            if (house == null)
            {
                house = _cabinHouseManager != null ? _cabinHouseManager : UnityEngine.Object.FindObjectOfType<CabinHouseManager>();
            }

            var shed = house != null ? house.shedManager : UnityEngine.Object.FindObjectOfType<ShedManager>();
            if (shed != null)
            {
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinShedFlagPrefix + "playerInsideShed", shed.playerInsideShed ? 1 : 0, nowMs);
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinShedFlagPrefix + "playerHidingInside", shed.playerHidingInside ? 1 : 0, nowMs);
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinShedFlagPrefix + "hidingSeqStarted", shed.hidingSeqStarted ? 1 : 0, nowMs);
                if (shed.mikeSeekingTrigger != null)
                {
                    EmitStoryFlagIfChanged(_cabinGameFlags, CabinShedActivePrefix + "mikeSeekingTrigger", shed.mikeSeekingTrigger.activeSelf ? 1 : 0, nowMs);
                }
            }

            var understairs = house != null ? house.understairsDoor : UnityEngine.Object.FindObjectOfType<UnderStairsDoor>();
            if (understairs != null)
            {
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinUnderstairsFlagPrefix + "playerInside", understairs.playerInside ? 1 : 0, nowMs);
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinUnderstairsFlagPrefix + "hidingSeqStarted", understairs.hidingSeqStarted ? 1 : 0, nowMs);
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinUnderstairsFlagPrefix + "mikeTeleported", understairs.mikeTeleported ? 1 : 0, nowMs);
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinUnderstairsFlagPrefix + "playerHidingInside", understairs.playerHidingInside ? 1 : 0, nowMs);
                if (understairs.hidingLight != null)
                {
                    EmitStoryFlagIfChanged(_cabinGameFlags, CabinUnderstairsActivePrefix + "hidingLight", understairs.hidingLight.activeSelf ? 1 : 0, nowMs);
                }
                if (understairs.blockStairs != null)
                {
                    EmitStoryFlagIfChanged(_cabinGameFlags, CabinUnderstairsActivePrefix + "blockStairs", understairs.blockStairs.activeSelf ? 1 : 0, nowMs);
                }
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinUnderstairsActivePrefix + "triggerHiding", AnyActive(understairs.triggerHiding) ? 1 : 0, nowMs);
            }

            var hostHiding = _cabinGameManager.hostHiding;
            if (hostHiding == null)
            {
                hostHiding = UnityEngine.Object.FindObjectOfType<HostDuringHiding>();
            }

            if (hostHiding != null)
            {
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinHostHidingFlagPrefix + "Active", hostHiding.gameObject.activeSelf ? 1 : 0, nowMs);
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinHostHidingFlagPrefix + "state", Convert.ToInt32(hostHiding.state), nowMs);
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinHostHidingFlagPrefix + "moving", hostHiding.moving ? 1 : 0, nowMs);
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinHostHidingFlagPrefix + "hostSeen", hostHiding.hostSeen ? 1 : 0, nowMs);
            }

            MaybeLogCabinHidingState(mike, shed, understairs, hostHiding);
        }

        private void SendCabinDeathFlags(long nowMs)
        {
            var death = DeathManager.instance;
            if (death == null)
            {
                death = UnityEngine.Object.FindObjectOfType<DeathManager>();
            }

            if (death == null)
            {
                return;
            }

            EmitStoryFlagIfChanged(_cabinGameFlags, CabinDeathFlagPrefix + "Active", death.gameObject.activeSelf ? 1 : 0, nowMs);
            EmitStoryFlagIfChanged(_cabinGameFlags, CabinDeathFlagPrefix + "hostIsChasing", death.hostIsChasing ? 1 : 0, nowMs);
            EmitStoryFlagIfChanged(_cabinGameFlags, CabinDeathFlagPrefix + "hostIsHaunting", death.hostIsHaunting ? 1 : 0, nowMs);
            EmitStoryFlagIfChanged(_cabinGameFlags, CabinDeathFlagPrefix + "waitForFarScare", death.waitForFarScare ? 1 : 0, nowMs);
            EmitStoryFlagIfChanged(_cabinGameFlags, CabinDeathFlagPrefix + "playerCaught", death.playerCaught ? 1 : 0, nowMs);
            EmitStoryFlagIfChanged(_cabinGameFlags, CabinDeathFlagPrefix + "isDead", death.isDead ? 1 : 0, nowMs);
            if (death.jumpscareLight != null)
            {
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinDeathActivePrefix + "jumpscareLight", death.jumpscareLight.activeSelf ? 1 : 0, nowMs);
            }

            EmitDeathBehaviourFlag(death.anomaly, "anomaly", nowMs);
            EmitDeathBehaviourFlag(death.blurFocus, "blurFocus", nowMs);
            EmitDeathBehaviourFlag(death.distortionDream, "distortionDream", nowMs);
            EmitDeathBehaviourFlag(death.realVHS, "realVHS", nowMs);
            EmitDeathBehaviourFlag(death.filmGrain, "filmGrain", nowMs);
            EmitDeathBehaviourFlag(death.bloodPlus, "bloodPlus", nowMs);
            EmitDeathBehaviourFlag(death.cameraShake, "cameraShake", nowMs);
        }

        private void EmitDeathBehaviourFlag(Behaviour behaviour, string key, long nowMs)
        {
            if (behaviour == null || string.IsNullOrEmpty(key))
            {
                return;
            }

            EmitStoryFlagIfChanged(_cabinGameFlags, CabinDeathEnabledPrefix + key, behaviour.enabled ? 1 : 0, nowMs);
        }

        private void SendCabinHikerFlags(long nowMs)
        {
            if (_cabinGameManager == null) return;

            var hiker = _cabinGameManager.cabinHiker;
            if (hiker == null)
            {
                hiker = UnityEngine.Object.FindObjectOfType<CabinHiker>();
            }

            if (hiker != null)
            {
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinHikerFlagPrefix + "Active", hiker.gameObject.activeSelf ? 1 : 0, nowMs);
                EmitFieldFlags(hiker, _cabinHikerFieldNames, _cabinHikerFieldCache, _cabinGameFlags, CabinHikerFlagPrefix, nowMs);
            }

            var hikerCtl = UnityEngine.Object.FindObjectOfType<HikerCabinController>();
            if (hikerCtl != null)
            {
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinHikerControllerFlagPrefix + "Active", hikerCtl.gameObject.activeSelf ? 1 : 0, nowMs);
                EmitFieldFlags(hikerCtl, _cabinHikerControllerFieldNames, _cabinHikerControllerFieldCache, _cabinGameFlags, CabinHikerControllerFlagPrefix, nowMs);
                if (TryGetGameObjectFromComponentField(hikerCtl, "hikerConvoTriggerOnDoor", _cabinHikerControllerFieldCache, out var convoDoor) && convoDoor != null)
                {
                    EmitStoryFlagIfChanged(_cabinGameFlags, CabinHikerActivePrefix + "hikerConvoTriggerOnDoor", convoDoor.activeSelf ? 1 : 0, nowMs);
                }
            }

            var fixing = _cabinGameManager.hostFixingSink;
            if (fixing == null)
            {
                fixing = UnityEngine.Object.FindObjectOfType<HostFixingSink>();
            }

            if (fixing != null)
            {
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinHostFixingSinkFlagPrefix + "Active", fixing.gameObject.activeSelf ? 1 : 0, nowMs);
                EmitFieldFlags(fixing, _cabinHostFixingSinkFieldNames, _cabinHostFixingSinkFieldCache, _cabinGameFlags, CabinHostFixingSinkFlagPrefix, nowMs);
            }

            var afterHiding = _cabinGameManager.mikeAfterHiding;
            if (afterHiding == null)
            {
                afterHiding = UnityEngine.Object.FindObjectOfType<MikeAfterHiding>();
            }

            if (afterHiding != null)
            {
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinMikeAfterHidingFlagPrefix + "Active", afterHiding.gameObject.activeSelf ? 1 : 0, nowMs);
                EmitFieldFlags(afterHiding, _cabinMikeAfterHidingFieldNames, _cabinMikeAfterHidingFieldCache, _cabinGameFlags, CabinMikeAfterHidingFlagPrefix, nowMs);
            }

            if (TryGetGameObjectField(_cabinGameManager, "sinisterAudioTrigger", _cabinHikerFieldCache, out var sinisterTrigger))
            {
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinHikerActivePrefix + "sinisterAudioTrigger", sinisterTrigger.activeSelf ? 1 : 0, nowMs);
            }

            if (TryGetGameObjectField(_cabinGameManager, "closetLight", _cabinHikerFieldCache, out var closetLight))
            {
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinHikerActivePrefix + "closetLight", closetLight.activeSelf ? 1 : 0, nowMs);
            }

            if (TryGetGameObjectFromComponentField(_cabinGameManager, "hikerConvoTrigger", _cabinHikerFieldCache, out var hikerConvo) && hikerConvo != null)
            {
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinHikerActivePrefix + "hikerConvoTrigger", hikerConvo.activeSelf ? 1 : 0, nowMs);
            }
        }

        private bool TryGetGameObjectFromComponentField(
            object target,
            string fieldName,
            Dictionary<string, FieldInfo> cache,
            out GameObject gameObject)
        {
            gameObject = null;
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
                if (raw is GameObject go)
                {
                    gameObject = go;
                    return true;
                }

                if (raw is Component component)
                {
                    gameObject = component.gameObject;
                    return gameObject != null;
                }
            }
            catch
            {
            }

            return false;
        }

        private void SendPizzeriaFlags()
        {
            if (_pizzeriaGameManager == null)
            {
                _pizzeriaGameManager = UnityEngine.Object.FindObjectOfType<PizzeriaGameManager>();
                if (_pizzeriaGameManager == null) return;
            }

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            EmitFieldFlags(
                _pizzeriaGameManager,
                _pizzeriaFieldNames,
                _pizzeriaFieldCache,
                _pizzeriaFlags,
                PizzeriaFlagPrefix,
                nowMs);
            EmitGameObjectActiveFlags(
                _pizzeriaGameManager,
                _pizzeriaGameObjectFieldNames,
                _pizzeriaGameObjectFieldCache,
                _pizzeriaFlags,
                PizzeriaActiveGamePrefix,
                nowMs);

            if (_pizzeriaMike == null)
            {
                _pizzeriaMike = _pizzeriaGameManager.mikePizzeria;
                if (_pizzeriaMike == null)
                {
                    _pizzeriaMike = UnityEngine.Object.FindObjectOfType<MikePizzeria>();
                }
            }

            if (_pizzeriaMike != null)
            {
                EmitFieldFlags(
                    _pizzeriaMike,
                    _pizzeriaMikeFieldNames,
                    _pizzeriaMikeFieldCache,
                    _pizzeriaFlags,
                    PizzeriaMikeFlagPrefix,
                    nowMs);
                EmitGameObjectActiveFlags(
                    _pizzeriaMike,
                    _pizzeriaMikeGameObjectFieldNames,
                    _pizzeriaMikeGameObjectFieldCache,
                    _pizzeriaFlags,
                    PizzeriaActiveMikePrefix,
                    nowMs);
            }

            if (_pizzeriaTruckDoor == null)
            {
                _pizzeriaTruckDoor = _pizzeriaGameManager.truckDoor;
                if (_pizzeriaTruckDoor == null)
                {
                    _pizzeriaTruckDoor = UnityEngine.Object.FindObjectOfType<PizzeriaTruckDoor>();
                }
            }

            if (_pizzeriaTruckDoor != null)
            {
                EmitStoryFlagIfChanged(
                    _pizzeriaFlags,
                    PizzeriaFlagPrefix + "TruckDoorInteractable",
                    _pizzeriaTruckDoor.isinteractable ? 1 : 0,
                    nowMs);
            }

            SceneTrafficSync.EmitPizzeriaHostFlags(
                PizzeriaFlagPrefix,
                (key, value) => EmitStoryFlagIfChanged(_pizzeriaFlags, key, value, nowMs));
            PizzeriaMediaSync.EmitHostFlags(
                PizzeriaFlagPrefix,
                (key, value) => EmitStoryFlagIfChanged(_pizzeriaFlags, key, value, nowMs));
            PizzeriaPropSync.EmitHostFlags(
                PizzeriaFlagPrefix,
                (key, value) => EmitStoryFlagIfChanged(_pizzeriaFlags, key, value, nowMs));
            PizzeriaSceneStateSync.EmitHostFlags(
                PizzeriaFlagPrefix,
                _pizzeriaGameManager,
                (key, value) => EmitStoryFlagIfChanged(_pizzeriaFlags, key, value, nowMs));
        }

        private void SendRoadTripFlags()
        {
            if (_roadTripGameManager == null)
            {
                _roadTripGameManager = UnityEngine.Object.FindObjectOfType<RoadTripGameManager>();
                if (_roadTripGameManager == null) return;
            }

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            EmitFieldFlags(
                _roadTripGameManager,
                _roadTripFieldNames,
                _roadTripFieldCache,
                _roadTripFlags,
                RoadTripFlagPrefix,
                nowMs);

            if (_roadTripMikeInCar == null)
            {
                _roadTripMikeInCar = UnityEngine.Object.FindObjectOfType<MikeInCar>();
            }

            if (_roadTripMikeInCar != null)
            {
                EmitFieldFlags(
                    _roadTripMikeInCar,
                    _roadTripMikeFieldNames,
                    _roadTripMikeFieldCache,
                    _roadTripFlags,
                    RoadTripMikeFlagPrefix,
                    nowMs);
            }

            if (_roadTripTruck == null)
            {
                _roadTripTruck = UnityEngine.Object.FindObjectOfType<MikeTruckInLoopScene>();
            }

            if (_roadTripTruck != null)
            {
                EmitFieldFlags(
                    _roadTripTruck,
                    _roadTripTruckFieldNames,
                    _roadTripTruckFieldCache,
                    _roadTripFlags,
                    RoadTripTruckFlagPrefix,
                    nowMs);
                TryEnqueueAiState(_roadTripTruck.transform, 0.02f, 0.5f);
            }

            SceneTrafficSync.EmitRoadTripHostFlags(
                RoadTripFlagPrefix,
                (key, value) => EmitStoryFlagIfChanged(_roadTripFlags, key, value, nowMs));
            RoadTripSceneStateSync.EmitHostFlags(
                RoadTripFlagPrefix,
                _roadTripGameManager,
                (key, value) => EmitStoryFlagIfChanged(_roadTripFlags, key, value, nowMs));
        }

        private void SendOfficeLayoutFlags()
        {
            if (_officeLayoutGameManager == null)
            {
                _officeLayoutGameManager = UnityEngine.Object.FindObjectOfType<OfficeLayoutGameManager>();
                if (_officeLayoutGameManager == null) return;
            }

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            EmitFieldFlags(
                _officeLayoutGameManager,
                _officeFieldNames,
                _officeFieldCache,
                _officeFlags,
                OfficeLayoutFlagPrefix,
                nowMs);

            OfficeSceneStateSync.EmitHostFlags(
                OfficeLayoutFlagPrefix,
                (key, value) => EmitStoryFlagIfChanged(_officeFlags, key, value, nowMs));
        }

        private void SendParkingLotFlags()
        {
            if (_parkingLotGameManager == null)
            {
                _parkingLotGameManager = UnityEngine.Object.FindObjectOfType<ParkingLotGameManager>();
                if (_parkingLotGameManager == null) return;
            }

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            EmitFieldFlags(
                _parkingLotGameManager,
                _parkingLotFieldNames,
                _parkingLotFieldCache,
                _parkingLotFlags,
                ParkingLotFlagPrefix,
                nowMs);

            ParkingLotSceneStateSync.EmitHostFlags(
                ParkingLotFlagPrefix,
                (key, value) => EmitStoryFlagIfChanged(_parkingLotFlags, key, value, nowMs));
        }

        private void SendRoadTripVehicleState(long nowMs)
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (sceneName.IndexOf("RoadTrip", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            if (_roadTripTruck == null)
            {
                _roadTripTruck = UnityEngine.Object.FindObjectOfType<MikeTruckInLoopScene>();
            }

            if (_roadTripTruck == null || _roadTripTruck.transform == null)
            {
                return;
            }

            var id = "RoadTrip/Vehicle/MikeLoop";
            var transform = _roadTripTruck.transform;
            var path = NetPath.GetPath(transform);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var velocity = CalculateVehicleVelocity(id, transform.position, nowMs);
            var speed = ReadFloatField(_roadTripTruck, "speed", _roadTripTruckFieldCache, 0f);
            var distance = ReadFloatField(_roadTripTruck, "distanceTravelled", _roadTripTruckFieldCache, 0f);
            var flags = 0;
            if (ReadBoolField(_roadTripTruck, "pushBreak", _roadTripTruckFieldCache)) flags |= 1;
            if (ReadBoolField(_roadTripTruck, "accelerateFromStop", _roadTripTruckFieldCache)) flags |= 2;
            if (ReadBoolField(_roadTripTruck, "dialogueBreak", _roadTripTruckFieldCache)) flags |= 4;
            if (ReadBoolField(_roadTripTruck, "run", _roadTripTruckFieldCache)) flags |= 8;

            var signature = path + "|" + transform.position.ToString("F3") + "|" +
                            transform.rotation.eulerAngles.ToString("F2") + "|" +
                            speed.ToString("F2") + "|" + distance.ToString("F2") + "|" +
                            transform.gameObject.activeInHierarchy + "|" + flags;
            var hash = StableHash(signature);
            var lastSend = _vehicleLastSendMsById.TryGetValue(id, out var previousSend) ? previousSend : 0;
            if (_vehicleLastHashById.TryGetValue(id, out var previousHash) &&
                previousHash == hash &&
                nowMs - lastSend < 500)
            {
                return;
            }

            var nextSeq = _vehicleSeqById.TryGetValue(id, out var previousSeq) ? previousSeq + 1 : 1;
            _vehicleSeqById[id] = nextSeq;
            _vehicleLastHashById[id] = hash;
            _vehicleLastSendMsById[id] = nowMs;

            _server.Enqueue(new PathVehicleStateMessage(new PathVehicleState
            {
                SessionId = _server.ActiveSessionId,
                Generation = _sceneHandshake.Generation,
                VehicleSeq = nextSeq,
                UnixTimeMs = nowMs,
                SceneName = sceneName,
                VehicleId = id,
                Path = path,
                Active = transform.gameObject.activeInHierarchy,
                Position = transform.position,
                Rotation = transform.rotation,
                Velocity = velocity,
                PathDistance = distance,
                Speed = speed,
                Flags = flags
            }));
        }

        private void EmitSceneAdapterSnapshot(long nowMs)
        {
            var sceneName = SceneManager.GetActiveScene().name;
            for (var i = 0; i < _sceneAdapters.Count; i++)
            {
                var adapter = _sceneAdapters[i];
                if (!adapter.MatchesScene(sceneName))
                {
                    continue;
                }

                adapter.EmitHostSnapshot(nowMs);
                break;
            }
        }

        private void EmitFieldFlags(
            object target,
            string[] fieldNames,
            Dictionary<string, FieldInfo> fieldCache,
            Dictionary<string, int> valueCache,
            string keyPrefix,
            long nowMs)
        {
            if (target == null || fieldNames == null || fieldNames.Length == 0) return;

            for (var i = 0; i < fieldNames.Length; i++)
            {
                var fieldName = fieldNames[i];
                if (!TryGetFieldAsInt(target, fieldName, fieldCache, out var value))
                {
                    continue;
                }

                EmitStoryFlagIfChanged(valueCache, keyPrefix + fieldName, value, nowMs);
            }
        }

        private void EmitGameObjectActiveFlags(
            object target,
            string[] fieldNames,
            Dictionary<string, FieldInfo> fieldCache,
            Dictionary<string, int> valueCache,
            string keyPrefix,
            long nowMs)
        {
            if (target == null || fieldNames == null || fieldNames.Length == 0) return;

            for (var i = 0; i < fieldNames.Length; i++)
            {
                var fieldName = fieldNames[i];
                if (!TryGetGameObjectField(target, fieldName, fieldCache, out var gameObject))
                {
                    continue;
                }

                EmitStoryFlagIfChanged(valueCache, keyPrefix + fieldName, gameObject.activeSelf ? 1 : 0, nowMs);
            }
        }

        private void EmitGameObjectArrayActiveFlags(
            object target,
            string[] fieldNames,
            Dictionary<string, FieldInfo> fieldCache,
            Dictionary<string, int> valueCache,
            string keyPrefix,
            long nowMs)
        {
            if (target == null || fieldNames == null || fieldNames.Length == 0) return;

            for (var i = 0; i < fieldNames.Length; i++)
            {
                var fieldName = fieldNames[i];
                if (!TryGetGameObjectArrayField(target, fieldName, fieldCache, out var gameObjects))
                {
                    continue;
                }

                EmitStoryFlagIfChanged(valueCache, keyPrefix + fieldName, AnyActive(gameObjects) ? 1 : 0, nowMs);
            }
        }

        private bool TryGetGameObjectField(
            object target,
            string fieldName,
            Dictionary<string, FieldInfo> fieldCache,
            out GameObject gameObject)
        {
            gameObject = null;
            if (target == null || string.IsNullOrEmpty(fieldName)) return false;

            if (!fieldCache.TryGetValue(fieldName, out var field))
            {
                field = FindInstanceField(target.GetType(), fieldName);
                fieldCache[fieldName] = field;
            }

            if (field == null || field.FieldType != typeof(GameObject)) return false;

            try
            {
                gameObject = field.GetValue(target) as GameObject;
                return gameObject != null;
            }
            catch (Exception ex)
            {
                if (_settings.VerboseLogging.Value)
                {
                    _logger.LogWarning("Read scene GameObject field failed: " + fieldName + " (" + ex.Message + ")");
                }
                return false;
            }
        }

        private bool TryGetGameObjectArrayField(
            object target,
            string fieldName,
            Dictionary<string, FieldInfo> fieldCache,
            out GameObject[] gameObjects)
        {
            gameObjects = null;
            if (target == null || string.IsNullOrEmpty(fieldName)) return false;

            if (!fieldCache.TryGetValue(fieldName, out var field))
            {
                field = FindInstanceField(target.GetType(), fieldName);
                fieldCache[fieldName] = field;
            }

            if (field == null || field.FieldType != typeof(GameObject[])) return false;

            try
            {
                gameObjects = field.GetValue(target) as GameObject[];
                return gameObjects != null;
            }
            catch (Exception ex)
            {
                if (_settings.VerboseLogging.Value)
                {
                    _logger.LogWarning("Read scene GameObject[] field failed: " + fieldName + " (" + ex.Message + ")");
                }
                return false;
            }
        }

        private static bool AnyActive(GameObject[] gameObjects)
        {
            if (gameObjects == null) return false;
            for (var i = 0; i < gameObjects.Length; i++)
            {
                var gameObject = gameObjects[i];
                if (gameObject != null && gameObject.activeSelf)
                {
                    return true;
                }
            }

            return false;
        }

        private void EmitStoryFlagIfChanged(
            Dictionary<string, int> cache,
            string key,
            int value,
            long nowMs)
        {
            if (cache.TryGetValue(key, out var last) && last == value)
            {
                return;
            }

            cache[key] = value;
            if (!IsHighFrequencyStoryFlag(key))
            {
                _lastStoryEventKey = key;
                _lastStoryEventValue = value;
                _lastStoryEventMs = nowMs;
            }
            _server.Enqueue(new StoryFlagMessage(key, value));
        }

        private bool TryGetFieldAsInt(
            object target,
            string fieldName,
            Dictionary<string, FieldInfo> fieldCache,
            out int value)
        {
            value = 0;
            if (target == null || string.IsNullOrEmpty(fieldName)) return false;

            if (!fieldCache.TryGetValue(fieldName, out var field))
            {
                field = FindInstanceField(target.GetType(), fieldName);
                fieldCache[fieldName] = field;
            }

            if (field == null) return false;

            try
            {
                var raw = field.GetValue(target);
                if (field.FieldType == typeof(bool))
                {
                    value = raw is bool b && b ? 1 : 0;
                    return true;
                }

                if (field.FieldType.IsEnum)
                {
                    value = Convert.ToInt32(raw);
                    return true;
                }

                if (field.FieldType == typeof(int))
                {
                    value = raw is int i ? i : 0;
                    return true;
                }

                if (field.FieldType == typeof(byte))
                {
                    value = raw is byte bt ? bt : (byte)0;
                    return true;
                }

                if (field.FieldType == typeof(float))
                {
                    value = raw is float f ? Mathf.RoundToInt(f * 1000f) : 0;
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (_settings.VerboseLogging.Value)
                {
                    _logger.LogWarning("Read scene field failed: " + fieldName + " (" + ex.Message + ")");
                }
            }

            return false;
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 23;
                value = value ?? string.Empty;
                for (var i = 0; i < value.Length; i++)
                {
                    hash = (hash * 31) + value[i];
                }
                return hash;
            }
        }

        private Vector3 CalculateVehicleVelocity(string id, Vector3 position, long nowMs)
        {
            if (string.IsNullOrEmpty(id)) return Vector3.zero;
            var velocity = Vector3.zero;
            if (_vehicleLastPositionById.TryGetValue(id, out var previousPosition) &&
                _vehicleLastPositionMsById.TryGetValue(id, out var previousMs))
            {
                var delta = Math.Max(1, nowMs - previousMs) / 1000f;
                velocity = (position - previousPosition) / delta;
            }

            _vehicleLastPositionById[id] = position;
            _vehicleLastPositionMsById[id] = nowMs;
            return velocity;
        }

        private static float ReadFloatField(object target, string fieldName, Dictionary<string, FieldInfo> fieldCache, float fallback)
        {
            if (target == null || string.IsNullOrEmpty(fieldName)) return fallback;
            if (!fieldCache.TryGetValue(fieldName, out var field))
            {
                field = FindInstanceField(target.GetType(), fieldName);
                fieldCache[fieldName] = field;
            }

            if (field == null) return fallback;
            try
            {
                var value = field.GetValue(target);
                if (value is float f) return f;
                if (value is int i) return i;
            }
            catch
            {
            }

            return fallback;
        }

        private static bool ReadBoolField(object target, string fieldName, Dictionary<string, FieldInfo> fieldCache)
        {
            if (target == null || string.IsNullOrEmpty(fieldName)) return false;
            if (!fieldCache.TryGetValue(fieldName, out var field))
            {
                field = FindInstanceField(target.GetType(), fieldName);
                fieldCache[fieldName] = field;
            }

            if (field == null) return false;
            try
            {
                var value = field.GetValue(target);
                return value is bool b && b;
            }
            catch
            {
                return false;
            }
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

        private void SendAiStates()
        {
            foreach (var agent in _aiAgents)
            {
                if (agent == null) continue;
                TryEnqueueAiState(agent.transform);
            }

            foreach (var navAgent in _navMeshAgents)
            {
                if (navAgent == null) continue;
                if (navAgent.GetComponent<NavmeshPathAgent>() != null) continue;
                if (IsLocalPlayerAgent(navAgent)) continue;
                TryEnqueueAiState(navAgent.transform);
            }

            SendCabinMikeAiStates();
            SendCabinCookingPropTransforms();
            SendCabinBoardGameTransforms();
            SendCabinAmbientTransforms();
            SendCabinTrafficTransforms();
            SendPizzeriaTrafficTransforms();
            SendPizzeriaPropTransforms();
            SendPizzeriaSceneTransforms();
            SendRoadTripTrafficTransforms();
            SendRoadTripSceneTransforms();
            SendOfficeLayoutTransforms();
            SendParkingLotTransforms();
            SendRoadTripVehicleState(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            SendCabinMikeAnimationFlags();
        }

        private void SendOfficeLayoutTransforms()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName) ||
                sceneName.IndexOf("Office", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            var syncedTransforms = new List<Transform>();
            OfficeSceneStateSync.CollectSyncedTransforms(syncedTransforms);
            for (var i = 0; i < syncedTransforms.Count; i++)
            {
                var transform = syncedTransforms[i];
                if (transform == null) continue;
                TryEnqueueAiState(
                    transform,
                    movementThreshold: 0.01f,
                    rotationThreshold: 0.25f);
            }
        }

        private void SendParkingLotTransforms()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName) ||
                (sceneName.IndexOf("Parking", StringComparison.OrdinalIgnoreCase) < 0 &&
                 sceneName.IndexOf("Lot", StringComparison.OrdinalIgnoreCase) < 0))
            {
                return;
            }

            var syncedTransforms = new List<Transform>();
            ParkingLotSceneStateSync.CollectSyncedTransforms(syncedTransforms);
            for (var i = 0; i < syncedTransforms.Count; i++)
            {
                var transform = syncedTransforms[i];
                if (transform == null) continue;
                TryEnqueueAiState(
                    transform,
                    movementThreshold: 0.02f,
                    rotationThreshold: 0.5f);
            }
        }

        private void SendCabinTrafficTransforms()
        {
            if (!string.Equals(SceneManager.GetActiveScene().name, "CabinScene", StringComparison.Ordinal))
            {
                return;
            }

            var syncedTransforms = new List<Transform>();
            CabinTrafficSync.CollectSyncedTransforms(syncedTransforms);
            for (var i = 0; i < syncedTransforms.Count; i++)
            {
                var transform = syncedTransforms[i];
                if (transform == null) continue;
                TryEnqueueAiState(
                    transform,
                    movementThreshold: 0.03f,
                    rotationThreshold: 0.75f);
            }
        }

        private void SendPizzeriaTrafficTransforms()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName) ||
                sceneName.IndexOf("Pizzeria", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            var syncedTransforms = new List<Transform>();
            SceneTrafficSync.CollectPizzeriaTransforms(syncedTransforms);
            for (var i = 0; i < syncedTransforms.Count; i++)
            {
                var transform = syncedTransforms[i];
                if (transform == null) continue;
                TryEnqueueAiState(
                    transform,
                    movementThreshold: 0.03f,
                    rotationThreshold: 0.75f);
            }
        }

        private void SendPizzeriaPropTransforms()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName) ||
                sceneName.IndexOf("Pizzeria", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            var syncedTransforms = new List<Transform>();
            PizzeriaPropSync.CollectSyncedTransforms(syncedTransforms);
            for (var i = 0; i < syncedTransforms.Count; i++)
            {
                var transform = syncedTransforms[i];
                if (transform == null) continue;
                TryEnqueueAiState(
                    transform,
                    movementThreshold: 0.015f,
                    rotationThreshold: 0.35f);
            }
        }

        private void SendPizzeriaSceneTransforms()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName) ||
                sceneName.IndexOf("Pizzeria", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            var syncedTransforms = new List<Transform>();
            PizzeriaSceneStateSync.CollectSyncedTransforms(syncedTransforms);
            for (var i = 0; i < syncedTransforms.Count; i++)
            {
                var transform = syncedTransforms[i];
                if (transform == null) continue;
                TryEnqueueAiState(
                    transform,
                    movementThreshold: 0.01f,
                    rotationThreshold: 0.25f);
            }
        }

        private void SendRoadTripTrafficTransforms()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName) ||
                sceneName.IndexOf("RoadTrip", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            var syncedTransforms = new List<Transform>();
            SceneTrafficSync.CollectRoadTripTransforms(syncedTransforms);
            for (var i = 0; i < syncedTransforms.Count; i++)
            {
                var transform = syncedTransforms[i];
                if (transform == null) continue;
                TryEnqueueAiState(
                    transform,
                    movementThreshold: 0.03f,
                    rotationThreshold: 0.75f);
            }
        }

        private void SendRoadTripSceneTransforms()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName) ||
                sceneName.IndexOf("RoadTrip", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            if (_roadTripGameManager == null)
            {
                _roadTripGameManager = UnityEngine.Object.FindObjectOfType<RoadTripGameManager>();
            }

            var syncedTransforms = new List<Transform>();
            RoadTripSceneStateSync.CollectSyncedTransforms(_roadTripGameManager, syncedTransforms);
            for (var i = 0; i < syncedTransforms.Count; i++)
            {
                var transform = syncedTransforms[i];
                if (transform == null) continue;
                TryEnqueueAiState(
                    transform,
                    movementThreshold: 0.02f,
                    rotationThreshold: 0.5f);
            }
        }

        private void SendCabinCookingPropTransforms()
        {
            if (_cabinGameManager == null)
            {
                _cabinGameManager = UnityEngine.Object.FindObjectOfType<CabinGameManager>();
                if (_cabinGameManager == null) return;
            }

            var syncedTransforms = new List<Transform>();
            CabinCookingPropSync.CollectSyncedTransforms(_cabinGameManager, syncedTransforms);
            var hideCookingVisuals = CabinCookingPropSync.ShouldHideCookingVisuals(_cabinGameManager);
            for (var i = 0; i < syncedTransforms.Count; i++)
            {
                var transform = syncedTransforms[i];
                if (transform == null) continue;
                var isCasserole = transform.GetComponent<CasseroleDish>() != null;
                TryEnqueueAiState(
                    transform,
                    movementThreshold: 0.01f,
                    rotationThreshold: 0.25f,
                    activeOverride: isCasserole && hideCookingVisuals ? false : (bool?)null);
            }
        }

        private void SendCabinBoardGameTransforms()
        {
            if (_cabinGameManager == null)
            {
                _cabinGameManager = UnityEngine.Object.FindObjectOfType<CabinGameManager>();
                if (_cabinGameManager == null) return;
            }

            var syncedTransforms = new List<Transform>();
            CabinBoardGameSync.CollectSyncedTransforms(_cabinGameManager, syncedTransforms);
            for (var i = 0; i < syncedTransforms.Count; i++)
            {
                var transform = syncedTransforms[i];
                if (transform == null) continue;
                TryEnqueueAiState(
                    transform,
                    movementThreshold: 0.005f,
                    rotationThreshold: 0.15f);
            }
        }

        private void SendCabinAmbientTransforms()
        {
            if (_cabinGameManager == null)
            {
                _cabinGameManager = UnityEngine.Object.FindObjectOfType<CabinGameManager>();
                if (_cabinGameManager == null) return;
            }

            var syncedTransforms = new List<Transform>();
            CabinAmbientSync.CollectSyncedTransforms(_cabinGameManager, syncedTransforms);
            for (var i = 0; i < syncedTransforms.Count; i++)
            {
                var transform = syncedTransforms[i];
                if (transform == null) continue;
                TryEnqueueAiState(
                    transform,
                    movementThreshold: 0.01f,
                    rotationThreshold: 0.25f);
            }
        }

        private int SendNpcBrainDeltas()
        {
            return SendNpcBrainStates(force: false, snapshot: false);
        }

        private int SendNpcBrainSnapshot()
        {
            return SendNpcBrainStates(force: true, snapshot: true);
        }

        private int SendNpcBrainStates(bool force, bool snapshot)
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName))
            {
                _npcOverlaySummary = "NPC: -";
                return 0;
            }

            if (sceneName.IndexOf("Pizzeria", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return SendGenericNpcBrainStates(_pizzeriaNpcRegistry, sceneName, force, snapshot);
            }

            if (sceneName.IndexOf("RoadTrip", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return SendGenericNpcBrainStates(_roadTripNpcRegistry, sceneName, force, snapshot);
            }

            if (sceneName.IndexOf("Office", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return SendGenericNpcBrainStates(_officeNpcRegistry, sceneName, force, snapshot);
            }

            if (sceneName.IndexOf("Parking", StringComparison.OrdinalIgnoreCase) >= 0 ||
                sceneName.IndexOf("Lot", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return SendGenericNpcBrainStates(_parkingLotNpcRegistry, sceneName, force, snapshot);
            }

            if (sceneName.IndexOf("Cabin", StringComparison.OrdinalIgnoreCase) < 0)
            {
                _npcOverlaySummary = "NPC: -";
                return 0;
            }

            if (_cabinGameManager == null)
            {
                _cabinGameManager = UnityEngine.Object.FindObjectOfType<CabinGameManager>();
            }

            if (_cabinGameManager == null)
            {
                _npcOverlaySummary = "NPC: missing-manager";
                return 0;
            }

            _cabinNpcRegistry.Refresh(_cabinGameManager, sceneName, force);

            var count = 0;
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var record in _cabinNpcRegistry.Records)
            {
                if (record == null || record.Component == null) continue;
                var nextSeq = _npcSeqById.TryGetValue(record.Id, out var previousSeq) ? previousSeq + 1 : 1;
                var velocity = CalculateNpcVelocity(record, nowMs);
                if (!_cabinNpcRegistry.TryBuildState(
                        _cabinGameManager,
                        record.Id,
                        _server.ActiveSessionId,
                        _sceneHandshake.Generation,
                        nextSeq,
                        nowMs,
                        velocity,
                        out var state))
                {
                    continue;
                }

                if (!force && !ShouldSendNpcBrainState(state, nowMs))
                {
                    continue;
                }

                _npcSeqById[record.Id] = nextSeq;
                _npcLastStateHashById[record.Id] = state.StateHash;
                _npcLastSendMsById[record.Id] = nowMs;
                _server.Enqueue(new NpcBrainStateMessage(state));
                count++;

                if (ShouldLogNpcBrainSend(state, snapshot, nowMs))
                {
                    _logger.LogInfo("NPCBrain host send npcId=" + state.NpcId +
                                    " state=" + state.StateName +
                                    " phase=" + state.Phase +
                                    " sequence=" + state.Sequence +
                                    " npcSeq=" + state.NpcSeq);
                }
            }

            _npcOverlaySummary = _cabinNpcRegistry.BuildOverlaySummary(hostSide: true);
            return count;
        }

        private int SendGenericNpcBrainStates(GenericNpcRegistry registry, string sceneName, bool force, bool snapshot)
        {
            if (registry == null)
            {
                _npcOverlaySummary = "NPC: -";
                return 0;
            }

            registry.Refresh(sceneName, force);
            var count = 0;
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var record in registry.Records)
            {
                if (record == null || record.Component == null) continue;
                var nextSeq = _npcSeqById.TryGetValue(record.Id, out var previousSeq) ? previousSeq + 1 : 1;
                var velocity = CalculateNpcVelocity(record, nowMs);
                if (!registry.TryBuildState(
                        record,
                        _server.ActiveSessionId,
                        _sceneHandshake.Generation,
                        nextSeq,
                        nowMs,
                        velocity,
                        out var state))
                {
                    continue;
                }

                if (!force && !ShouldSendNpcBrainState(state, nowMs))
                {
                    continue;
                }

                _npcSeqById[record.Id] = nextSeq;
                _npcLastStateHashById[record.Id] = state.StateHash;
                _npcLastSendMsById[record.Id] = nowMs;
                _server.Enqueue(new NpcBrainStateMessage(state));
                count++;

                if (ShouldLogNpcBrainSend(state, snapshot, nowMs))
                {
                    _logger.LogInfo("NPCBrain host send npcId=" + state.NpcId +
                                    " state=" + state.StateName +
                                    " phase=" + state.Phase +
                                    " sequence=" + state.Sequence +
                                    " npcSeq=" + state.NpcSeq);
                }
            }

            _npcOverlaySummary = registry.BuildOverlaySummary("NPC");
            return count;
        }

        private bool ShouldLogNpcBrainSend(NpcBrainState state, bool snapshot, long nowMs)
        {
            if (snapshot || state.NpcSeq <= 1 || state.NpcSeq % 60 == 0)
            {
                _npcLastLogHashById[state.NpcId] = state.StateHash;
                _npcLastLogMsById[state.NpcId] = nowMs;
                return true;
            }

            if (!_npcLastLogHashById.TryGetValue(state.NpcId, out var lastHash) ||
                lastHash != state.StateHash)
            {
                _npcLastLogHashById[state.NpcId] = state.StateHash;
                _npcLastLogMsById[state.NpcId] = nowMs;
                return true;
            }

            if (!_npcLastLogMsById.TryGetValue(state.NpcId, out var lastMs) ||
                nowMs - lastMs >= 10000)
            {
                _npcLastLogMsById[state.NpcId] = nowMs;
                return true;
            }

            return false;
        }

        private bool ShouldSendNpcBrainState(NpcBrainState state, long nowMs)
        {
            if (!_npcLastSendMsById.TryGetValue(state.NpcId, out var lastSendMs))
            {
                return true;
            }

            if (!_npcLastStateHashById.TryGetValue(state.NpcId, out var lastHash) ||
                lastHash != state.StateHash)
            {
                return true;
            }

            var heartbeatMs = state.Active ? (state.Critical ? 1000 : 2000) : 5000;
            if (nowMs - lastSendMs >= heartbeatMs)
            {
                return true;
            }

            return state.Active && state.IsMoving && nowMs - lastSendMs >= 500;
        }

        private Vector3 CalculateNpcVelocity(CabinNpcRecord record, long nowMs)
        {
            if (record == null || record.Component == null) return Vector3.zero;
            var position = record.Component.transform.position;
            var velocity = Vector3.zero;
            if (_npcLastPositionById.TryGetValue(record.Id, out var previousPosition) &&
                _npcLastPositionMsById.TryGetValue(record.Id, out var previousMs))
            {
                var delta = Math.Max(1, nowMs - previousMs) / 1000f;
                velocity = (position - previousPosition) / delta;
            }

            _npcLastPositionById[record.Id] = position;
            _npcLastPositionMsById[record.Id] = nowMs;
            return velocity;
        }

        private Vector3 CalculateNpcVelocity(GenericNpcRecord record, long nowMs)
        {
            if (record == null || record.Component == null) return Vector3.zero;
            var position = record.Component.transform.position;
            var velocity = Vector3.zero;
            if (_npcLastPositionById.TryGetValue(record.Id, out var previousPosition) &&
                _npcLastPositionMsById.TryGetValue(record.Id, out var previousMs))
            {
                var delta = Math.Max(1, nowMs - previousMs) / 1000f;
                velocity = (position - previousPosition) / delta;
            }

            _npcLastPositionById[record.Id] = position;
            _npcLastPositionMsById[record.Id] = nowMs;
            return velocity;
        }

        private void SendCabinMikeAiStates()
        {
            if (!string.Equals(SceneManager.GetActiveScene().name, "CabinScene", StringComparison.Ordinal))
            {
                return;
            }

            if (_cabinGameManager == null)
            {
                _cabinGameManager = UnityEngine.Object.FindObjectOfType<CabinGameManager>();
            }

            var target = ResolveCabinMikeSyncTarget(out var reason);
            if (target != null)
            {
                MaybeLogCabinMikeSyncTarget(target, reason);
                TryEnqueueAiState(target, movementThreshold: 0.02f, rotationThreshold: 0.5f, allowCabinMike: true);
            }
        }

        private void SendCabinMikeAnimationFlags()
        {
            if (!string.Equals(SceneManager.GetActiveScene().name, "CabinScene", StringComparison.Ordinal))
            {
                return;
            }

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (!TryGetCabinMikeAnimator(out var animator))
            {
                return;
            }

            var current = animator.GetCurrentAnimatorStateInfo(0);
            var stateHash = current.fullPathHash != 0 ? current.fullPathHash : current.shortNameHash;
            var loop = Mathf.Max(0, Mathf.FloorToInt(current.normalizedTime));
            var phase10 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Repeat(current.normalizedTime, 1f) * 10f), 0, 9);
            var transition = animator.IsInTransition(0) ? 1 : 0;
            var nextStateHash = 0;
            if (transition != 0)
            {
                var next = animator.GetNextAnimatorStateInfo(0);
                nextStateHash = next.fullPathHash != 0 ? next.fullPathHash : next.shortNameHash;
            }

            EmitStoryFlagIfChanged(_cabinGameFlags, CabinMikeAnimTransitionKey, transition, nowMs);
            EmitStoryFlagIfChanged(_cabinGameFlags, CabinMikeAnimNextStateKey, nextStateHash, nowMs);
            EmitStoryFlagIfChanged(_cabinGameFlags, CabinMikeAnimStateHashKey, stateHash, nowMs);
            if (nowMs >= _nextCabinMikeAnimPhaseSendMs)
            {
                _nextCabinMikeAnimPhaseSendMs = nowMs + CabinMikeAnimPhaseSendIntervalMs;
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinMikeAnimLoopKey, loop, nowMs);
                EmitStoryFlagIfChanged(_cabinGameFlags, CabinMikeAnimPhaseKey, phase10, nowMs);
            }
        }

        private bool TryGetCabinMikeAnimator(out Animator animator)
        {
            animator = null;

            var target = ResolveCabinMikeSyncTarget();
            if (target != null)
            {
                animator = target.GetComponentInChildren<Animator>(true);
                if (animator != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void CollectCabinMikeTransforms(List<Transform> candidates)
        {
            if (candidates == null) return;

            if (_cabinGameManager != null)
            {
                if (_cabinGameManager.mikeObject != null) candidates.Add(_cabinGameManager.mikeObject.transform);
                if (_cabinGameManager.mikeCabin != null) candidates.Add(_cabinGameManager.mikeCabin.transform);
                if (_cabinGameManager.mikeFishing != null) candidates.Add(_cabinGameManager.mikeFishing.transform);
                if (_cabinGameManager.mikePostEating != null) candidates.Add(_cabinGameManager.mikePostEating.transform);
                if (_cabinGameManager.mikeAfterHiding != null) candidates.Add(_cabinGameManager.mikeAfterHiding.transform);
                if (_cabinGameManager.mikeEnd != null) candidates.Add(_cabinGameManager.mikeEnd.transform);
                TryAddMikeTransform(candidates, _cabinMikeControllerField != null ? _cabinMikeControllerField.GetValue(_cabinGameManager) as Component : null);
                TryAddMikeTransform(candidates, _cabinMikeRizzlerControllerField != null ? _cabinMikeRizzlerControllerField.GetValue(_cabinGameManager) as Component : null);
            }

            TryAddMikeTransform(candidates, UnityEngine.Object.FindObjectOfType<MikeCabinCookController>());
            TryAddMikeTransform(candidates, UnityEngine.Object.FindObjectOfType<MikeCabin>());
            TryAddMikeTransform(candidates, UnityEngine.Object.FindObjectOfType<MikeFishing>());
            TryAddMikeTransform(candidates, UnityEngine.Object.FindObjectOfType<MikePostEating>());
            TryAddMikeTransform(candidates, UnityEngine.Object.FindObjectOfType<MikeAfterHiding>());
            TryAddMikeTransform(candidates, UnityEngine.Object.FindObjectOfType<MikeRizzlerController>());
            TryAddMikeTransform(candidates, UnityEngine.Object.FindObjectOfType<MikeEndGame>());
        }

        private Transform ResolveCabinMikeSyncTarget()
        {
            return ResolveCabinMikeSyncTarget(out _);
        }

        private Transform ResolveCabinMikeSyncTarget(out string reason)
        {
            reason = string.Empty;
            if (_cabinGameManager == null)
            {
                reason = "active:no-manager";
                return GetActiveCabinMikeTarget();
            }

            var seq = _cabinGameManager.CurrentSequence;
            var postEatingTarget = ResolveCabinPostEatingTarget(out var postEatingReason);
            if (postEatingTarget != null)
            {
                reason = postEatingReason;
                return postEatingTarget;
            }

            if (IsCabinCookMikeSequence(seq))
            {
                var controller = _cabinMikeControllerField != null
                    ? _cabinMikeControllerField.GetValue(_cabinGameManager) as Component
                    : null;
                if (controller != null && IsActiveRenderable(controller.transform))
                {
                    reason = "seq:" + seq;
                    return controller.transform;
                }

                var activeCookTarget = GetActiveCabinCookMikeTarget();
                if (activeCookTarget != null)
                {
                    reason = "seq:" + seq + ":activefallback";
                    return activeCookTarget;
                }

                if (controller != null && HasRenderable(controller.transform))
                {
                    reason = "seq:" + seq + ":inactive-controller";
                    return controller.transform;
                }

                if (_cabinGameManager.mikeCabin != null && IsActiveRenderable(_cabinGameManager.mikeCabin.transform))
                {
                    reason = "seq:" + seq + ":cabinfallback";
                    return _cabinGameManager.mikeCabin.transform;
                }
            }

            if (seq == SequenceType.Fishing && _cabinGameManager.mikeFishing != null)
            {
                reason = "seq:" + seq;
                return _cabinGameManager.mikeFishing.transform;
            }

            switch (_cabinGameManager.currentMike)
            {
                case CabinGameManager.CurrentMike.Prefishing:
                case CabinGameManager.CurrentMike.PostFishing:
                    reason = "currentMike:" + _cabinGameManager.currentMike;
                    if (_cabinGameManager.mikeCabin != null) return _cabinGameManager.mikeCabin.transform;
                    break;
                case CabinGameManager.CurrentMike.Fishing:
                    reason = "currentMike:" + _cabinGameManager.currentMike;
                    if (_cabinGameManager.mikeFishing != null) return _cabinGameManager.mikeFishing.transform;
                    break;
                case CabinGameManager.CurrentMike.PostEating:
                    reason = "currentMike:" + _cabinGameManager.currentMike;
                    if (_cabinGameManager.mikePostEating != null) return _cabinGameManager.mikePostEating.transform;
                    break;
            }

            reason = "active";
            return GetActiveCabinMikeTarget();
        }

        private Transform ResolveCabinPostEatingTarget(out string reason)
        {
            reason = string.Empty;
            if (_cabinGameManager == null) return null;

            var mike = _cabinGameManager.mikePostEating;
            if (mike == null) return null;
            if (!ShouldPreferPostEatingMike(mike)) return null;

            reason = "postEating:" + mike.state;
            return mike.transform;
        }

        private bool ShouldPreferPostEatingMike(MikePostEating mike)
        {
            if (mike == null) return false;

            if (_cabinGameManager != null)
            {
                var seq = _cabinGameManager.CurrentSequence;
                if (seq == SequenceType.Eating &&
                    (_cabinGameManager.currentMike == CabinGameManager.CurrentMike.PostEating ||
                     IsActivePostEatingState(mike.state)))
                {
                    return true;
                }

                if (IsEarlyCabinMikeSequence(seq))
                {
                    return false;
                }

                if (_cabinGameManager.currentMike == CabinGameManager.CurrentMike.PostEating)
                {
                    return true;
                }

                if (seq == SequenceType.RizzSequence ||
                    seq == SequenceType.HikerSequence ||
                    seq == SequenceType.HostAtDoor ||
                    seq == SequenceType.HostHittingDoor)
                {
                    return true;
                }
            }

            if (mike.gameObject.activeInHierarchy &&
                IsActivePostEatingState(mike.state))
            {
                return true;
            }

            var house = _cabinGameManager != null ? _cabinGameManager.cabinHouseManager : _cabinHouseManager;
            return house != null &&
                   (house.mikeBedroomJumpscare ||
                    house.mikePostJumpscareActive ||
                    house.mikePostJumpscareConvoDone);
        }

        private static bool IsActivePostEatingState(MikePostEating.State state)
        {
            return state != MikePostEating.State.None &&
                   state != MikePostEating.State.IdleStanding;
        }

        private static bool IsEarlyCabinMikeSequence(SequenceType seq)
        {
            return seq == SequenceType.DrivingToCabin ||
                   seq == SequenceType.Fishing ||
                   IsCabinCookMikeSequence(seq);
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

        private void MaybeLogCabinMikeSyncTarget(Transform target, string reason)
        {
            if (target == null || _cabinGameManager == null) return;

            var path = NetPath.GetPath(target);
            _lastCabinMikeSyncDebug =
                _cabinGameManager.CurrentSequence + "/" +
                _cabinGameManager.currentMike + " -> " +
                target.name + " (" + reason + ")";

            var targetId = target.GetInstanceID();
            var changed = targetId != _lastCabinMikeSyncTargetId ||
                          !string.Equals(_lastCabinMikeSyncReason, reason, StringComparison.Ordinal);
            var now = Time.realtimeSinceStartup;
            if (!changed && now < _nextCabinMikeSyncLogTime)
            {
                return;
            }

            _lastCabinMikeSyncTargetId = targetId;
            _lastCabinMikeSyncReason = reason ?? string.Empty;
            _nextCabinMikeSyncLogTime = now + 5f;
            _logger.LogInfo("Mike sync target: seq=" + _cabinGameManager.CurrentSequence +
                            " currentMike=" + _cabinGameManager.currentMike +
                            " target=" + target.name +
                            " path=" + (string.IsNullOrEmpty(path) ? "-" : path) +
                            " reason=" + reason +
                            " active=" + target.gameObject.activeInHierarchy +
                            " renderable=" + HasRenderable(target));
        }

        private void MaybeLogCabinHidingState(
            MikePostEating mike,
            ShedManager shed,
            UnderStairsDoor understairs,
            HostDuringHiding hostHiding)
        {
            var debug =
                "MPE=" + (mike != null ? mike.state.ToString() : "-") +
                " seek=" + BoolText(mike != null && mike.startedSeeking) +
                " found=" + BoolText(mike != null && mike.playerFound) +
                " shedGo=" + BoolText(mike != null && mike.goingToToolShed) +
                " Shed hide=" + BoolText(shed != null && shed.playerHidingInside) +
                " started=" + BoolText(shed != null && shed.hidingSeqStarted) +
                " Under hide=" + BoolText(understairs != null && understairs.playerHidingInside) +
                " started=" + BoolText(understairs != null && understairs.hidingSeqStarted) +
                " HostHiding=" + (hostHiding != null ? hostHiding.state.ToString() : "-");

            var now = Time.realtimeSinceStartup;
            if (string.Equals(debug, _lastCabinHidingDebug, StringComparison.Ordinal) &&
                now < _nextCabinHidingLogTime)
            {
                return;
            }

            _lastCabinHidingDebug = debug;
            _nextCabinHidingLogTime = now + 10f;
            var message = "Cabin hiding state: " + debug;
            _logger.LogInfo(message);
            _sessionLogWrite?.Invoke(message);
        }

        private static string BoolText(bool value)
        {
            return value ? "1" : "0";
        }

        private Transform GetActiveCabinMikeTarget()
        {
            var candidates = new List<Transform>();
            CollectCabinMikeTransforms(candidates);
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate == null || !candidate.gameObject.activeInHierarchy) continue;
                if (HasRenderable(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private Transform GetActiveCabinCookMikeTarget()
        {
            if (_cabinGameManager == null) return null;

            var candidates = new List<Transform>();
            TryAddMikeTransform(candidates, _cabinMikeControllerField != null ? _cabinMikeControllerField.GetValue(_cabinGameManager) as Component : null);
            if (_cabinGameManager.mikeCabin != null) candidates.Add(_cabinGameManager.mikeCabin.transform);
            if (_cabinGameManager.mikeObject != null) candidates.Add(_cabinGameManager.mikeObject.transform);

            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (IsActiveRenderable(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void TryAddMikeTransform(List<Transform> list, Component component)
        {
            if (list == null || component == null || component.transform == null) return;
            if (list.Contains(component.transform)) return;
            list.Add(component.transform);
        }

        private static bool HasRenderable(Transform target)
        {
            if (target == null) return false;
            return target.GetComponentInChildren<Renderer>(true) != null;
        }

        private static bool IsActiveRenderable(Transform target)
        {
            if (target == null) return false;
            if (!target.gameObject.activeInHierarchy) return false;
            return HasRenderable(target);
        }

        private void TryEnqueueAiState(
            Transform transform,
            float movementThreshold = 0.05f,
            float rotationThreshold = 1f,
            bool allowCabinMike = false,
            bool? activeOverride = null)
        {
            if (transform == null) return;

            var state = new AiTransformState
            {
                Path = NetPath.GetPath(transform),
                Position = transform.position,
                Rotation = transform.rotation,
                Active = activeOverride ?? transform.gameObject.activeInHierarchy
            };
            if (string.IsNullOrEmpty(state.Path) || IsCoopProxyPath(state.Path)) return;
            if (!allowCabinMike && IsCabinMikePath(state.Path)) return;
            if (allowCabinMike && !state.Active) return;

            if (_aiStates.TryGetValue(state.Path, out var last) &&
                last.Active == state.Active &&
                Vector3.Distance(last.Position, state.Position) <= movementThreshold &&
                Quaternion.Angle(last.Rotation, state.Rotation) <= rotationThreshold)
            {
                return;
            }

            _aiStates[state.Path] = state;
            _server.Enqueue(new AiTransformMessage(state));
        }

        private static bool IsHighFrequencyStoryFlag(string key)
        {
            return !string.IsNullOrEmpty(key) &&
                   (key.StartsWith(CabinMikeAnimPrefix, StringComparison.Ordinal) ||
                    IsCabinCookingHighFrequencyStoryFlag(key) ||
                    key.StartsWith(CabinGameFlagPrefix + CabinAmbientSync.KeyPrefix, StringComparison.Ordinal) &&
                    CabinAmbientSync.IsHighFrequencyStoryFlag(key.Substring(CabinGameFlagPrefix.Length)) ||
                    key.StartsWith(CabinGameFlagPrefix + CabinBoardGameSync.KeyPrefix, StringComparison.Ordinal) &&
                    CabinBoardGameSync.IsHighFrequencyStoryFlag(key.Substring(CabinGameFlagPrefix.Length)));
        }

        private static bool IsCabinCookingHighFrequencyStoryFlag(string key)
        {
            return key.IndexOf("Cooking.LivingRoomTV.VideoTimeMs", StringComparison.Ordinal) >= 0 ||
                   key.IndexOf("Cooking.Ouija.randomPositionMoveTimer", StringComparison.Ordinal) >= 0 ||
                   key.IndexOf("Cooking.Ouija.moveToYesPointTimer", StringComparison.Ordinal) >= 0 ||
                   key.IndexOf("Cooking.Ouija.roundTimer", StringComparison.Ordinal) >= 0 ||
                   key.IndexOf("Cooking.Ouija.mouseX", StringComparison.Ordinal) >= 0 ||
                   key.IndexOf("Cooking.Ouija.mouseY", StringComparison.Ordinal) >= 0;
        }

        private static bool IsCoopProxyPath(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   (path.IndexOf("CoopRemotePlayer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("CoopHostAvatar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("CoopClientAvatar", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool IsCabinMikePath(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   path.StartsWith("CabinScene/", StringComparison.OrdinalIgnoreCase) &&
                   path.IndexOf("Mike", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private sealed class SnapshotCounts
        {
            public static readonly SnapshotCounts Empty = new SnapshotCounts();

            public int Story;
            public int Door;
            public int Holdable;
            public int Ai;
            public int Dialogue;
            public int Player;
            public int Custom;

            public int Total
            {
                get { return Story + Door + Holdable + Ai + Dialogue + Player + Custom; }
            }
        }

        private SnapshotCounts SendFullState(bool force)
        {
            if (force)
            {
                _doorStates.Clear();
                _holdableStates.Clear();
                _aiStates.Clear();
                _storyFlags.Clear();
                _cabinHouseFlags.Clear();
                _cabinGameFlags.Clear();
                _pizzeriaFlags.Clear();
                _roadTripFlags.Clear();
            }

            var counts = new SnapshotCounts();
            counts.Dialogue = 0;
            counts.Player = 0;
            counts.Custom = 0;

            var before = _server.HostStateEnqueued;
            SendDoorStates();
            counts.Door = (int)Math.Max(0, _server.HostStateEnqueued - before);

            before = _server.HostStateEnqueued;
            SendHoldableStates();
            counts.Holdable = (int)Math.Max(0, _server.HostStateEnqueued - before);

            before = _server.HostStateEnqueued;
            SendStoryFlags();
            counts.Story = (int)Math.Max(0, _server.HostStateEnqueued - before);

            before = _server.HostStateEnqueued;
            SendAiStates();
            counts.Ai = (int)Math.Max(0, _server.HostStateEnqueued - before);

            before = _server.HostStateEnqueued;
            SendPhoneMirror(force: true);
            counts.Dialogue = (int)Math.Max(0, _server.HostStateEnqueued - before);

            before = _server.HostStateEnqueued;
            SendNpcBrainSnapshot();
            counts.Custom = (int)Math.Max(0, _server.HostStateEnqueued - before);

            return counts;
        }

        private void EmitSnapshot(bool force)
        {
            var sceneName = SceneManager.GetActiveScene().name;
            var sessionId = _server.ActiveSessionId;
            var generation = _sceneHandshake.Generation;

            _sceneHandshake.MarkSnapshotBegin(generation);
            // SnapshotBegin first so the client can flip state before deltas arrive.
            _server.Enqueue(new SnapshotBeginMessage(sessionId, generation, sceneName, 0, 0, 0, 0, 0, 0, 0));
            var counts = SendFullState(force);
            _lastSnapshotCounts = counts;
            _sceneHandshake.MarkSnapshotEnd(generation);
            _server.Enqueue(new SnapshotEndMessage(
                sessionId,
                generation,
                sceneName,
                counts.Story,
                counts.Door,
                counts.Holdable,
                counts.Ai,
                counts.Dialogue,
                counts.Player,
                counts.Custom));
            _logger.LogInfo("Co-op session host: snapshot emitted gen=" + generation +
                " story=" + counts.Story +
                " doors=" + counts.Door +
                " holdables=" + counts.Holdable +
                " ai=" + counts.Ai +
                " dialogue=" + counts.Dialogue +
                " players=" + counts.Player +
                " custom=" + counts.Custom +
                " total=" + counts.Total +
                " scene=" + sceneName);
        }

        private void BeginSceneHandshake()
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var scene = SceneManager.GetActiveScene();
            _sceneGeneration++;
            _lastSceneName = scene.name;
            _clientSceneReady = false;
            _awaitingSceneReady = _server.IsClientConnected;
            _clientSceneName = string.Empty;
            _lastSceneRequestMs = 0;
            _lastSceneReadyMs = 0;
            _hasPendingClientTransform = false;
            _sceneHandshake.Begin(_sceneGeneration, _lastSceneName, _server.IsClientConnected, nowMs);
            _hostWaitNextSnapshotRetryMs = 0;

            // Only transition into SceneSyncing once we have a connected client past Hello/HelloAck.
            if (_server.IsClientConnected && _lifecycle.IsAtLeast(SessionState.Connected) &&
                _lifecycle.State != SessionState.Disconnected)
            {
                _lifecycle.TryTransition(
                    SessionState.SceneSyncing,
                    "scene-change",
                    _lastSceneName,
                    _sceneGeneration,
                    _server.ActiveSessionId,
                    nowMs);
                SendSceneChange();
            }
        }

        private int GetSceneChangeRetryBackoffMs(int attempt)
        {
            var schedule = IsCabinSceneName(_sceneHandshake.SceneName) || IsCabinSceneName(_lastSceneName)
                ? CabinSceneChangeRetryBackoffMs
                : SceneChangeRetryBackoffMs;
            var index = Math.Max(0, Math.Min(schedule.Length - 1, attempt - 1));
            return schedule[index];
        }

        private static bool IsCabinSceneName(string sceneName)
        {
            return string.Equals(sceneName, "CabinScene", StringComparison.Ordinal) ||
                   string.Equals(sceneName, "CabinSceneDark", StringComparison.Ordinal) ||
                   string.Equals(sceneName, "CabinDarkScene", StringComparison.Ordinal);
        }

        private void SendSceneChange(bool countAttempt = true)
        {
            var scene = SceneManager.GetActiveScene();
            _lastSceneName = scene.name;
            if (scene.buildIndex < 0)
            {
                _logger.LogWarning("Scene has invalid build index: " + scene.name);
            }
            var startSeq = PlayerPrefs.HasKey(PlayerPrefKeys.START_SEQ)
                ? PlayerPrefs.GetInt(PlayerPrefKeys.START_SEQ)
                : -1;
            var fromMenu = PlayerPrefs.HasKey(PlayerPrefKeys.FROM_MENU)
                ? PlayerPrefs.GetInt(PlayerPrefKeys.FROM_MENU)
                : -1;
            var attempt = countAttempt
                ? _sceneHandshake.IncrementAttempt()
                : Math.Max(1, _sceneHandshake.LastSentAttemptCount);
            var sessionId = _server.ActiveSessionId;
            _server.Enqueue(new SceneChangeMessage(_lastSceneName, scene.buildIndex, startSeq, fromMenu, sessionId, _sceneGeneration));
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _lastSceneRequestMs = nowMs;
            _sceneHandshake.MarkSceneRequest(nowMs);
            _awaitingSceneReady = _sceneHandshake.AwaitingReady;
            if (countAttempt && attempt > 1)
            {
                var backoffMs = GetSceneChangeRetryBackoffMs(attempt);
                _logger.LogInfo("Co-op session host: SceneChange retry attempt=" + attempt +
                    "/" + SceneHandshakeState.MaxAttempts +
                    " backoff=" + backoffMs + "ms scene=" + _lastSceneName +
                    " gen=" + _sceneGeneration +
                    " sid=" + sessionId);
            }
        }

        private void HandleSceneReady(SceneReadyMessage ready)
        {
            if (!_server.IsClientConnected) return;
            if (_sceneHandshakeSessionId != 0 &&
                _server.ActiveSessionId != 0 &&
                _sceneHandshakeSessionId != _server.ActiveSessionId)
            {
                _sceneHandshakeSessionId = _server.ActiveSessionId;
                BeginSceneHandshake();
                return;
            }

            if (ready.SessionId != 0 && ready.SessionId != _server.ActiveSessionId)
            {
                _logger.LogWarning("Co-op session host: SceneReady stale session=" + ready.SessionId +
                    " active=" + _server.ActiveSessionId + " ignoring");
                return;
            }

            if (ready.Generation != 0 && ready.Generation != _sceneHandshake.Generation)
            {
                _logger.LogWarning("Co-op session host: SceneReady stale generation=" + ready.Generation +
                    " active=" + _sceneHandshake.Generation +
                    " scene=" + ready.SceneName +
                    " reason=" + ready.Reason);
                SendSceneChange();
                return;
            }

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var activeScene = SceneManager.GetActiveScene().name;
            if (!string.Equals(ready.SceneName, activeScene, StringComparison.Ordinal))
            {
                _clientSceneReady = false;
                _awaitingSceneReady = true;
                SendSceneChange();
                return;
            }

            var accepted = _sceneHandshake.AcceptReady(ready.SceneName, nowMs);
            _clientSceneReady = true;
            _awaitingSceneReady = _sceneHandshake.AwaitingReady;
            _clientSceneName = ready.SceneName;
            _lastSceneReadyMs = _sceneHandshake.LastSceneReadyMs;

            if (accepted)
            {
                _lifecycle.TryTransition(
                    SessionState.SceneReady,
                    "client-ready status=" + ready.ReadyStatus,
                    ready.SceneName,
                    _sceneGeneration,
                    _server.ActiveSessionId,
                    nowMs);
                if (_sceneHandshake.TryMarkSnapshotSentForReadyGeneration())
                {
                    _lifecycle.TryTransition(
                        SessionState.SnapshotApplying,
                        "snapshot-begin",
                        ready.SceneName,
                        _sceneGeneration,
                        _server.ActiveSessionId,
                        nowMs);
                    EmitSnapshot(force: true);
                    SendPlayerTransform();
                }
                SnapRemotePlayerToHost();
                _logger.LogInfo("Co-op scene ready: " + ready.SceneName + " (gen " + _sceneGeneration + ")");
            }
            else
            {
                if (_settings.VerboseLogging.Value)
                {
                    _logger.LogInfo("Co-op duplicate SceneReady ignored: " + ready.SceneName + " (gen " + _sceneGeneration + ")");
                }
                SnapRemotePlayerToHost();
            }
        }

        private void CacheSceneObjects()
        {
            _cabinDoors = UnityEngine.Object.FindObjectsOfType<CabinDoor>();
            _doorScripts = UnityEngine.Object.FindObjectsOfType<NOTLonely_Door.DoorScript>();
            _holdables = UnityEngine.Object.FindObjectsOfType<Holdable>();
            _aiAgents = UnityEngine.Object.FindObjectsOfType<NavmeshPathAgent>();
            _navMeshAgents = UnityEngine.Object.FindObjectsOfType<NavMeshAgent>();
            _cabinHouseManager = UnityEngine.Object.FindObjectOfType<CabinHouseManager>();
            _cabinGameManager = UnityEngine.Object.FindObjectOfType<CabinGameManager>();
            _pizzeriaGameManager = UnityEngine.Object.FindObjectOfType<PizzeriaGameManager>();
            _pizzeriaTruckDoor = UnityEngine.Object.FindObjectOfType<PizzeriaTruckDoor>();
            _pizzeriaMike = UnityEngine.Object.FindObjectOfType<MikePizzeria>();
            _roadTripGameManager = UnityEngine.Object.FindObjectOfType<RoadTripGameManager>();
            _roadTripMikeInCar = UnityEngine.Object.FindObjectOfType<MikeInCar>();
            _roadTripTruck = UnityEngine.Object.FindObjectOfType<MikeTruckInLoopScene>();
            _cabinNpcRegistry.Refresh(_cabinGameManager, SceneManager.GetActiveScene().name, force: true);
            BindDialogueEvents();
            BindDialogueUiSelection();
        }

        private static bool IsLocalPlayerAgent(NavMeshAgent agent)
        {
            if (agent == null) return false;
            var player = PlayerController.GetInstance();
            if (player == null) return false;
            return agent.transform == player.transform || agent.transform.IsChildOf(player.transform);
        }

        private void EnsureRemotePlayer()
        {
            if (_remotePlayer != null && _remotePlayer.Root != null) return;

            var playerController = PlayerController.GetInstance();
            if (playerController == null && !HasConfiguredRemotePlayerPath()) return;

            var fps = playerController != null && _playerFirstPersonField != null
                ? _playerFirstPersonField.GetValue(playerController) as FirstPersonController
                : null;
            if (fps == null && !HasConfiguredRemotePlayerPath()) return;

            _remotePlayer = RemotePlayerProxy.Create(
                _settings,
                fps,
                new Color(0.2f, 0.7f, 1f, 0.8f),
                _logger,
                _sessionLogWrite);
            if (_remotePlayer == null)
            {
                return;
            }
            UpdateRemotePlayerNameTag();
            _remotePlayer.SetActive(_server.IsClientConnected);

            if (_hasInputState)
            {
                _remotePlayer.ApplyInput(_lastInputState);
            }
        }

        private void UpdateRemotePlayerNameTag()
        {
            if (_remotePlayer == null)
            {
                return;
            }

            var displayName = PlayerDisplayName.Normalize(_clientDisplayName, "CLIENT");
            _remotePlayer.SetNameTag(displayName, "CLIENT", new Color(0.45f, 0.78f, 1f, 1f));
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
            _npcSeqById.Clear();
            _npcLastStateHashById.Clear();
            _npcLastSendMsById.Clear();
            _npcLastPositionById.Clear();
            _npcLastPositionMsById.Clear();
            _vehicleSeqById.Clear();
            _vehicleLastHashById.Clear();
            _vehicleLastSendMsById.Clear();
            _vehicleLastPositionById.Clear();
            _vehicleLastPositionMsById.Clear();
            _npcOverlaySummary = "NPC: -";
            _boardGameOverlaySummary = "MiniGame: -";
            _lastCabinBoardGameHash = 0;
            _nextCabinBoardGameLogMs = 0;
            _lastCabinAmbientHash = 0;
            _nextCabinAmbientLogMs = 0;
            _storyFlags.Clear();
            _cabinHouseFlags.Clear();
            _cabinGameFlags.Clear();
            _pizzeriaFlags.Clear();
            _roadTripFlags.Clear();
            _officeFlags.Clear();
            _parkingLotFlags.Clear();
            _cabinHouseManager = null;
            _cabinGameManager = null;
            _pizzeriaGameManager = null;
            _pizzeriaTruckDoor = null;
            _pizzeriaMike = null;
            _roadTripGameManager = null;
            _roadTripMikeInCar = null;
            _roadTripTruck = null;
            _officeLayoutGameManager = null;
            _parkingLotGameManager = null;
            _cabinHouseActiveFieldCache.Clear();
            _officeFieldCache.Clear();
            _parkingLotFieldCache.Clear();
            _lastDialogueText = string.Empty;
            _lastDialogueSpeaker = string.Empty;
            _lastDialogueSentTime = 0f;
            _uiMirrorSeq = 0;
            _lastSubText = string.Empty;
            _dialogueConversationId = -1;
            _dialogueEntryId = -1;
            _dialogueChoiceIndex = -1;
            _lastDialogueEventMs = 0;
            _lastDialogueResponses = new Response[0];
            _nextDialogueUiScanTime = 0f;
            _lastStoryEventKey = string.Empty;
            _lastStoryEventValue = 0;
            _lastStoryEventMs = 0;
            _lastCabinHidingDebug = "-";
            _nextCabinHidingLogTime = 0f;
            UnbindDialogueUiSelection();

            if (_remotePlayer != null && _remotePlayer.Root != null)
            {
                UnityEngine.Object.Destroy(_remotePlayer.Root.gameObject);
            }
            _remotePlayer = null;
            _lastSceneName = newScene.name;
            OnSceneEnterAdapters(newScene.name);
            SceneDiscoveryManifest.LogIfDiscoveryOnlyScene("host", newScene.name, _logger, _sessionLogWrite);
            if (_settings.ModeSetting.Value == Mode.CoopHost)
            {
                SceneDiscoveryDump.LogIfEnabled(_settings, "host", newScene, _logger, _sessionLogWrite);
            }
            BeginSceneHandshake();
        }

        private void CacheCabinHouseFields()
        {
            _cabinHouseBoolFields.Clear();
            var type = typeof(CabinHouseManager);
            foreach (var name in _cabinHouseFieldNames)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null || field.FieldType != typeof(bool)) continue;
                _cabinHouseBoolFields.Add(field);
            }
        }

        private void CacheCabinGameFields()
        {
            _cabinGameFields.Clear();
            var type = typeof(CabinGameManager);
            foreach (var name in _cabinGameFieldNames)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null) continue;
                if (field.FieldType != typeof(bool) && !field.FieldType.IsEnum && field.FieldType != typeof(int)) continue;
                _cabinGameFields.Add(field);
            }
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

            _officeFieldCache.Clear();
            for (var i = 0; i < _officeFieldNames.Length; i++)
            {
                var name = _officeFieldNames[i];
                _officeFieldCache[name] = FindInstanceField(typeof(OfficeLayoutGameManager), name);
            }

            _parkingLotFieldCache.Clear();
            for (var i = 0; i < _parkingLotFieldNames.Length; i++)
            {
                var name = _parkingLotFieldNames[i];
                _parkingLotFieldCache[name] = FindInstanceField(typeof(ParkingLotGameManager), name);
            }
        }

        private void InitializeSceneAdapters()
        {
            _sceneAdapters.Clear();
            _sceneAdapters.Add(new DelegateHostSceneAdapter(
                "Cabin",
                IsCabinSceneName,
                onSceneEnter: () =>
                {
                    _cabinHouseManager = null;
                    _cabinGameManager = null;
                },
                emitHostSnapshot: _ =>
                {
                    if (string.Equals(SceneManager.GetActiveScene().name, "CabinScene", StringComparison.Ordinal))
                    {
                        SendCabinHouseFlags();
                    }

                    if (IsCabinSceneName(SceneManager.GetActiveScene().name))
                    {
                        SendCabinGameFlags();
                    }
                }));

            _sceneAdapters.Add(new DelegateHostSceneAdapter(
                "Pizzeria",
                scene => scene.IndexOf("Pizzeria", StringComparison.OrdinalIgnoreCase) >= 0,
                onSceneEnter: () =>
                {
                    _pizzeriaGameManager = null;
                    _pizzeriaTruckDoor = null;
                    _pizzeriaMike = null;
                    _pizzeriaFlags.Clear();
                },
                emitHostSnapshot: _ => SendPizzeriaFlags()));

            _sceneAdapters.Add(new DelegateHostSceneAdapter(
                "RoadTrip",
                scene => scene.IndexOf("RoadTrip", StringComparison.OrdinalIgnoreCase) >= 0,
                onSceneEnter: () =>
                {
                    _roadTripGameManager = null;
                    _roadTripMikeInCar = null;
                    _roadTripTruck = null;
                    _roadTripFlags.Clear();
                },
                emitHostSnapshot: _ => SendRoadTripFlags()));

            _sceneAdapters.Add(new DelegateHostSceneAdapter(
                "OfficeLayout",
                scene => scene.IndexOf("Office", StringComparison.OrdinalIgnoreCase) >= 0,
                onSceneEnter: () =>
                {
                    _officeLayoutGameManager = null;
                    _officeFlags.Clear();
                },
                emitHostSnapshot: _ => SendOfficeLayoutFlags()));

            _sceneAdapters.Add(new DelegateHostSceneAdapter(
                "ParkingLot",
                scene => scene.IndexOf("Parking", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         scene.IndexOf("Lot", StringComparison.OrdinalIgnoreCase) >= 0,
                onSceneEnter: () =>
                {
                    _parkingLotGameManager = null;
                    _parkingLotFlags.Clear();
                },
                emitHostSnapshot: _ => SendParkingLotFlags()));
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

        private void BindDialogueEvents()
        {
            var events = UnityEngine.Object.FindObjectOfType<DialogueSystemEvents>();
            if (events == _dialogueEvents) return;

            UnbindDialogueEvents();
            _dialogueEvents = events;
            if (_dialogueEvents == null) return;

            _onConversationLine = HandleConversationLine;
            _onConversationLineEnd = HandleConversationLineEnd;
            _onBarkLine = HandleBarkLine;
            _onConversationStart = HandleConversationStart;
            _onConversationEnd = HandleConversationEnd;
            _onConversationResponseMenu = HandleConversationResponseMenu;
            _dialogueEvents.conversationEvents.onConversationLine.AddListener(_onConversationLine);
            _dialogueEvents.conversationEvents.onConversationLineEnd.AddListener(_onConversationLineEnd);
            _dialogueEvents.conversationEvents.onConversationStart.AddListener(_onConversationStart);
            _dialogueEvents.conversationEvents.onConversationEnd.AddListener(_onConversationEnd);
            _dialogueEvents.conversationEvents.onConversationResponseMenu.AddListener(_onConversationResponseMenu);
            _dialogueEvents.barkEvents.onBarkLine.AddListener(_onBarkLine);
        }

        private void UnbindDialogueEvents()
        {
            if (_dialogueEvents == null) return;
            if (_onConversationLine != null)
            {
                _dialogueEvents.conversationEvents.onConversationLine.RemoveListener(_onConversationLine);
            }
            if (_onConversationLineEnd != null)
            {
                _dialogueEvents.conversationEvents.onConversationLineEnd.RemoveListener(_onConversationLineEnd);
            }
            if (_onConversationStart != null)
            {
                _dialogueEvents.conversationEvents.onConversationStart.RemoveListener(_onConversationStart);
            }
            if (_onConversationEnd != null)
            {
                _dialogueEvents.conversationEvents.onConversationEnd.RemoveListener(_onConversationEnd);
            }
            if (_onConversationResponseMenu != null)
            {
                _dialogueEvents.conversationEvents.onConversationResponseMenu.RemoveListener(_onConversationResponseMenu);
            }
            if (_onBarkLine != null)
            {
                _dialogueEvents.barkEvents.onBarkLine.RemoveListener(_onBarkLine);
            }
            _dialogueEvents = null;
            _onConversationLine = null;
            _onConversationLineEnd = null;
            _onConversationStart = null;
            _onConversationEnd = null;
            _onConversationResponseMenu = null;
            _onBarkLine = null;
        }

        private void BindDialogueUiSelection()
        {
            var now = Time.realtimeSinceStartup;
            if (now < _nextDialogueUiScanTime) return;
            _nextDialogueUiScanTime = now + 2f;

            var uis = Resources.FindObjectsOfTypeAll<AbstractDialogueUI>();
            foreach (var ui in uis)
            {
                if (ui == null) continue;
                if (_dialogueUis.Contains(ui)) continue;
                if (_onResponseSelected == null)
                {
                    _onResponseSelected = OnDialogueResponseSelected;
                }
                ui.SelectedResponseHandler += _onResponseSelected;
                _dialogueUis.Add(ui);
            }
        }

        private void UnbindDialogueUiSelection()
        {
            if (_dialogueUis.Count == 0) return;
            foreach (var ui in _dialogueUis)
            {
                if (ui == null) continue;
                if (_onResponseSelected != null)
                {
                    ui.SelectedResponseHandler -= _onResponseSelected;
                }
            }
            _dialogueUis.Clear();
            _onResponseSelected = null;
        }

        private void HandleConversationStart(Transform actor)
        {
            if (!_clientSceneReady) return;
            var entry = DialogueManager.currentConversationState != null
                ? DialogueManager.currentConversationState.subtitle?.dialogueEntry
                : null;
            var convoId = entry != null ? entry.conversationID : -1;
            var entryId = entry != null ? entry.id : -1;
            _dialogueConversationId = convoId;
            _dialogueEntryId = entryId;
            _dialogueChoiceIndex = -1;
            _lastDialogueEventMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _server.Enqueue(new DialogueStartMessage(convoId, entryId));
        }

        private void HandleConversationEnd(Transform actor)
        {
            if (!_clientSceneReady) return;
            var convoId = _dialogueConversationId;
            _dialogueConversationId = -1;
            _dialogueEntryId = -1;
            _dialogueChoiceIndex = -1;
            _lastDialogueEventMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _server.Enqueue(new DialogueEndMessage(convoId));
        }

        private void HandleConversationResponseMenu(Response[] responses)
        {
            _lastDialogueResponses = responses ?? new Response[0];
        }

        private void OnDialogueResponseSelected(object sender, SelectedResponseEventArgs e)
        {
            if (!_clientSceneReady) return;
            var entry = e != null ? e.DestinationEntry : null;
            var convoId = entry != null ? entry.conversationID : _dialogueConversationId;
            var entryId = entry != null ? entry.id : -1;
            var choiceIndex = -1;

            if (_lastDialogueResponses != null && e != null && e.response != null)
            {
                for (var i = 0; i < _lastDialogueResponses.Length; i++)
                {
                    var response = _lastDialogueResponses[i];
                    if (response == null) continue;
                    if (ReferenceEquals(response, e.response) ||
                        (response.destinationEntry != null && entry != null && response.destinationEntry.id == entry.id))
                    {
                        choiceIndex = i;
                        break;
                    }
                }
            }

            _dialogueConversationId = convoId;
            _dialogueEntryId = entryId;
            _dialogueChoiceIndex = choiceIndex;
            _lastDialogueEventMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _server.Enqueue(new DialogueChoiceMessage(convoId, entryId, choiceIndex));
        }

        private void HandleConversationLine(Subtitle subtitle)
        {
            SendDialogueLine(subtitle, kind: 0);
            SendDialogueAdvance(subtitle);
        }

        private void HandleConversationLineEnd(Subtitle subtitle)
        {
            if (!_clientSceneReady) return;
            _server.Enqueue(new DialogueLineMessage(string.Empty, string.Empty, 0f, 0));
            SendUiMirrorDialogue(string.Empty, string.Empty, 0f, 0, visible: false);
        }

        private void HandleBarkLine(Subtitle subtitle)
        {
            SendDialogueLine(subtitle, kind: 1);
        }

        private void SendDialogueLine(Subtitle subtitle, byte kind)
        {
            if (!_clientSceneReady || subtitle == null) return;
            var text = subtitle.formattedText != null ? subtitle.formattedText.text : string.Empty;
            if (string.IsNullOrEmpty(text)) return;

            var speaker = subtitle.speakerInfo != null ? subtitle.speakerInfo.Name : string.Empty;
            var now = Time.realtimeSinceStartup;
            if (text == _lastDialogueText && speaker == _lastDialogueSpeaker && now - _lastDialogueSentTime < 0.25f)
            {
                return;
            }

            _lastDialogueText = text;
            _lastDialogueSpeaker = speaker;
            _lastDialogueSentTime = now;
            var duration = Mathf.Clamp(1.5f + text.Length * 0.05f, 2f, 8f);
            _server.Enqueue(new DialogueLineMessage(speaker, text, duration, kind));
            SendUiMirrorDialogue(speaker, text, duration, kind, visible: true);
        }

        private void SendUiMirrorDialogue(string speaker, string text, float duration, byte kind, bool visible)
        {
            if (!_clientSceneReady) return;
            _uiMirrorSeq++;
            _server.Enqueue(new UiMirrorStateMessage(new UiMirrorState
            {
                SessionId = _server.ActiveSessionId,
                Generation = _sceneHandshake.Generation,
                UiSeq = _uiMirrorSeq,
                UnixTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                SceneName = SceneManager.GetActiveScene().name,
                UiKind = "Dialogue",
                Speaker = speaker ?? string.Empty,
                Text = text ?? string.Empty,
                ChoiceIndex = -1,
                Visible = visible,
                Duration = duration,
                Flags = kind
            }));
        }

        private void SendDialogueAdvance(Subtitle subtitle)
        {
            if (!_clientSceneReady || subtitle == null) return;
            if (subtitle.dialogueEntry == null) return;
            var convoId = subtitle.dialogueEntry.conversationID;
            var entryId = subtitle.dialogueEntry.id;
            _dialogueConversationId = convoId;
            _dialogueEntryId = entryId;
            _dialogueChoiceIndex = -1;
            _lastDialogueEventMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _server.Enqueue(new DialogueAdvanceMessage(convoId, entryId));
        }

        private void PollSubText()
        {
            var now = Time.realtimeSinceStartup;
            if (now < _nextSubTextPollTime) return;
            _nextSubTextPollTime = now + 0.2f;

            var manager = SubTextManager.GetInstance();
            if (manager == null) return;

            var component = manager.GetComponent("TMPro.TMP_Text");
            if (component == null) return;

            var textProp = component.GetType().GetProperty("text");
            var text = textProp != null ? textProp.GetValue(component, null) as string : string.Empty;
            if (text == null) text = string.Empty;
            if (text == _lastSubText) return;

            _lastSubText = text;
            if (string.IsNullOrEmpty(text))
            {
                _server.Enqueue(new DialogueLineMessage(string.Empty, string.Empty, 0f, 2));
                SendUiMirrorDialogue(string.Empty, string.Empty, 0f, 2, visible: false);
                return;
            }

            var duration = Mathf.Clamp(1.5f + text.Length * 0.05f, 2f, 8f);
            _server.Enqueue(new DialogueLineMessage(string.Empty, text, duration, 2));
            SendUiMirrorDialogue(string.Empty, text, duration, 2, visible: true);
        }

        private void PollPhoneMirror(long nowMs)
        {
            if (nowMs < _nextPhoneMirrorSendMs)
            {
                return;
            }

            _nextPhoneMirrorSendMs = nowMs + 500;
            SendPhoneMirror(force: false);
        }

        private bool SendPhoneMirror(bool force)
        {
            if (!_clientSceneReady)
            {
                return false;
            }

            string payload;
            int hash;
            PhoneMirrorSync.Summary summary;
            if (!PhoneMirrorSync.TryBuildPayload(SceneManager.GetActiveScene().name, out payload, out hash, out summary))
            {
                return false;
            }

            if (!force && hash == _lastPhoneMirrorHash)
            {
                return false;
            }

            _lastPhoneMirrorHash = hash;
            _uiMirrorSeq++;
            _server.Enqueue(new UiMirrorStateMessage(new UiMirrorState
            {
                SessionId = _server.ActiveSessionId,
                Generation = _sceneHandshake.Generation,
                UiSeq = _uiMirrorSeq,
                UnixTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                SceneName = SceneManager.GetActiveScene().name,
                UiKind = PhoneMirrorSync.UiKind,
                Speaker = summary.ToString(),
                Text = payload,
                ChoiceIndex = -1,
                Visible = true,
                Duration = 1.5f,
                Flags = 0
            }));

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (force || nowMs >= _nextPhoneMirrorLogMs)
            {
                var line = "PhoneMirror host send " + summary + " hash=" + hash + " force=" + (force ? "yes" : "no");
                _logger.LogInfo(line);
                _sessionLogWrite?.Invoke(line);
                _nextPhoneMirrorLogMs = nowMs + 5000;
            }

            return true;
        }
    }
}
