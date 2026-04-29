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
        private RemotePlayerProxy _remotePlayer;
        private PlayerInputState _lastInputState;
        private bool _hasInputState;
        private readonly Dictionary<string, DoorState> _doorStates = new Dictionary<string, DoorState>();
        private readonly Dictionary<string, HoldableState> _holdableStates = new Dictionary<string, HoldableState>();
        private readonly Dictionary<string, int> _storyFlags = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _cabinHouseFlags = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _cabinGameFlags = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _pizzeriaFlags = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _roadTripFlags = new Dictionary<string, int>();
        private readonly Dictionary<string, AiTransformState> _aiStates = new Dictionary<string, AiTransformState>();
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

        private readonly FieldInfo _doorScriptOpened;
        private readonly MethodInfo _doorScriptOpen;
        private readonly MethodInfo _doorScriptClose;
        private readonly FieldInfo _playerFirstPersonField;
        private readonly FieldInfo _cabinMikeControllerField;
        private readonly FieldInfo _cabinMikeRizzlerControllerField;
        private readonly List<FieldInfo> _cabinHouseBoolFields = new List<FieldInfo>();
        private readonly List<FieldInfo> _cabinGameFields = new List<FieldInfo>();
        private readonly Dictionary<string, FieldInfo> _pizzeriaFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _pizzeriaMikeFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _pizzeriaGameObjectFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _pizzeriaMikeGameObjectFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _roadTripFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _roadTripMikeFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, FieldInfo> _roadTripTruckFieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private readonly SceneHandshakeState _sceneHandshake = new SceneHandshakeState();

        private const string CabinHouseFlagPrefix = "CabinHouse.";
        private const string CabinGameFlagPrefix = "CabinGM.";
        private const string CabinMikeAnimPrefix = CabinGameFlagPrefix + "MikeAnim.";
        private const string CabinMikeAnimStateHashKey = CabinMikeAnimPrefix + "StateHash";
        private const string CabinMikeAnimLoopKey = CabinMikeAnimPrefix + "Loop";
        private const string CabinMikeAnimPhaseKey = CabinMikeAnimPrefix + "Phase10";
        private const string CabinMikeAnimTransitionKey = CabinMikeAnimPrefix + "Transition";
        private const string CabinMikeAnimNextStateKey = CabinMikeAnimPrefix + "NextStateHash";
        private const string PizzeriaFlagPrefix = "PizzeriaGM.";
        private const string PizzeriaMikeFlagPrefix = PizzeriaFlagPrefix + "Mike.";
        private const string PizzeriaActiveGamePrefix = PizzeriaFlagPrefix + "Active.Game.";
        private const string PizzeriaActiveMikePrefix = PizzeriaFlagPrefix + "Active.Mike.";
        private const string RoadTripFlagPrefix = "RoadTripGM.";
        private const string RoadTripMikeFlagPrefix = RoadTripFlagPrefix + "Mike.";
        private const string RoadTripTruckFlagPrefix = RoadTripFlagPrefix + "Truck.";
        private const float MaxInteractDistanceMeters = 3.5f;
        private const float InteractLosPaddingMeters = 0.2f;

        private long _nextPlayerSendMs;
        private long _nextWorldSendMs;
        private long _nextStorySendMs;
        private long _nextFullSyncMs;
        private long _nextInputSendMs;
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
        private float _nextSubTextPollTime;
        private string _lastSubText = string.Empty;
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

        private readonly string[] _cabinGameFieldNames = new[]
        {
            "CurrentSequence",
            "currentPlayerState",
            "inConversation",
            "currentMike"
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

        public CoopHostCoordinator(ManualLogSource logger, Settings settings, CoopServer server, Action<string> sessionLogWrite = null)
        {
            _logger = logger;
            _settings = settings;
            _server = server;
            _sessionLogWrite = sessionLogWrite;
            _clientAvatar = new RemoteAvatar("CoopClientAvatar", new Color(0.2f, 0.7f, 1f, 0.8f));

            var doorType = typeof(NOTLonely_Door.DoorScript);
            _doorScriptOpened = doorType.GetField("Opened", BindingFlags.Instance | BindingFlags.NonPublic);
            _doorScriptOpen = doorType.GetMethod("OpenDoor", BindingFlags.Instance | BindingFlags.NonPublic);
            _doorScriptClose = doorType.GetMethod("CloseDoor", BindingFlags.Instance | BindingFlags.NonPublic);
            _playerFirstPersonField = typeof(PlayerController).GetField("firstPersonController", BindingFlags.Instance | BindingFlags.NonPublic);
            _cabinMikeControllerField = typeof(CabinGameManager).GetField("mikeController", BindingFlags.Instance | BindingFlags.NonPublic);
            _cabinMikeRizzlerControllerField = typeof(CabinGameManager).GetField("mikeRizzlerController", BindingFlags.Instance | BindingFlags.NonPublic);

            SceneManager.activeSceneChanged += OnSceneChanged;
            CacheCabinHouseFields();
            CacheCabinGameFields();
            CacheSceneAdapterFields();
            InitializeSceneAdapters();
            CacheSceneObjects();
            OnSceneEnterAdapters(SceneManager.GetActiveScene().name);
            BeginSceneHandshake();
        }

        public bool ClientSceneReady => _clientSceneReady;
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

        public void Shutdown()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            UnbindDialogueEvents();
            UnbindDialogueUiSelection();
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
                _sceneHandshakeSessionId = _server.ActiveSessionId;
                BeginSceneHandshake();
                _logger.LogInfo("Co-op client connected, waiting for SceneReady");
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
                if (_remotePlayer != null)
                {
                    _remotePlayer.SetActive(false);
                }
                _clientAvatar.SetActive(false);
            }

            EnsureRemotePlayer();
            DrainIncoming();
            BindDialogueUiSelection();

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (_sceneHandshake.AwaitingReady && _server.IsClientConnected)
            {
                if (nowMs - _sceneHandshake.LastSceneRequestMs >= 1500)
                {
                    SendSceneChange();
                }
            }

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

            if (!_clientSceneReady)
            {
                return;
            }

            PollSubText();

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
                if (message is SceneReadyMessage ready)
                {
                    HandleSceneReady(ready);
                }
                else if (message is PlayerTransformMessage playerTransform)
                {
                    _lastClientTransformMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    if (!_clientSceneReady)
                    {
                        _pendingClientTransform = playerTransform.State;
                        _hasPendingClientTransform = true;
                        continue;
                    }

                    SetClientAvatar(playerTransform.State);
                }
                else if (message is InteractRequestMessage interact)
                {
                    if (!_clientSceneReady) continue;
                    HandleInteract(interact);
                }
                else if (message is PlayerInputMessage input)
                {
                    if (!_clientSceneReady) continue;
                    _lastInputState = input.State;
                    _hasInputState = true;
                    _remotePlayer?.ApplyInput(input.State);
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

            var camera = fps != null && fps.playerCamera != null ? fps.playerCamera : Camera.main;
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

            var baseTransform = fps != null
                ? fps.transform
                : (playerController != null ? playerController.transform : camera.transform);
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

            var camera = fps != null && fps.playerCamera != null ? fps.playerCamera : Camera.main;
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
            _lastStoryEventKey = key;
            _lastStoryEventValue = value;
            _lastStoryEventMs = nowMs;
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
            SendCabinMikeAnimationFlags();
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

            var candidates = new List<Transform>();
            CollectCabinMikeTransforms(candidates);

            for (var i = 0; i < candidates.Count; i++)
            {
                TryEnqueueAiState(candidates[i], movementThreshold: 0.02f, rotationThreshold: 0.5f);
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

            EmitStoryFlagIfChanged(_cabinGameFlags, CabinMikeAnimStateHashKey, stateHash, nowMs);
            EmitStoryFlagIfChanged(_cabinGameFlags, CabinMikeAnimLoopKey, loop, nowMs);
            EmitStoryFlagIfChanged(_cabinGameFlags, CabinMikeAnimPhaseKey, phase10, nowMs);
            EmitStoryFlagIfChanged(_cabinGameFlags, CabinMikeAnimTransitionKey, transition, nowMs);
            EmitStoryFlagIfChanged(_cabinGameFlags, CabinMikeAnimNextStateKey, nextStateHash, nowMs);
        }

        private bool TryGetCabinMikeAnimator(out Animator animator)
        {
            animator = null;

            var candidates = new List<Transform>();
            CollectCabinMikeTransforms(candidates);

            Animator fallback = null;
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate == null) continue;

                var candidateAnimator = candidate.GetComponentInChildren<Animator>(true);
                if (candidateAnimator == null) continue;

                if (candidate.gameObject.activeInHierarchy && HasRenderable(candidate))
                {
                    animator = candidateAnimator;
                    return true;
                }

                if (fallback == null)
                {
                    fallback = candidateAnimator;
                }
            }

            animator = fallback;
            return animator != null;
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

        private void TryEnqueueAiState(Transform transform, float movementThreshold = 0.05f, float rotationThreshold = 1f)
        {
            if (transform == null) return;

            var state = new AiTransformState
            {
                Path = NetPath.GetPath(transform),
                Position = transform.position,
                Rotation = transform.rotation,
                Active = transform.gameObject.activeInHierarchy
            };
            if (string.IsNullOrEmpty(state.Path)) return;

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

        private void SendFullState(bool force)
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

            SendDoorStates();
            SendHoldableStates();
            SendStoryFlags();
            SendAiStates();
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
            if (_server.IsClientConnected)
            {
                SendSceneChange();
            }
        }

        private void SendSceneChange()
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
            _server.Enqueue(new SceneChangeMessage(_lastSceneName, scene.buildIndex, startSeq, fromMenu));
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _lastSceneRequestMs = nowMs;
            _sceneHandshake.MarkSceneRequest(nowMs);
            _awaitingSceneReady = _sceneHandshake.AwaitingReady;
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

            if (_hasPendingClientTransform)
            {
                SetClientAvatar(_pendingClientTransform);
                _hasPendingClientTransform = false;
            }

            if (_sceneHandshake.TryMarkSnapshotSentForReadyGeneration())
            {
                SendFullState(force: true);
                SendPlayerTransform();
                _nextFullSyncMs = nowMs + 5000;
            }

            SnapRemotePlayerToHost();
            if (accepted)
            {
                _logger.LogInfo("Co-op scene ready: " + ready.SceneName + " (gen " + _sceneGeneration + ")");
            }
            else if (_settings.VerboseLogging.Value)
            {
                _logger.LogInfo("Co-op duplicate SceneReady ignored: " + ready.SceneName + " (gen " + _sceneGeneration + ")");
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

            var fps = _playerFirstPersonField != null
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
            _remotePlayer.SetActive(_server.IsClientConnected);

            if (_hasInputState)
            {
                _remotePlayer.ApplyInput(_lastInputState);
            }
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
            _storyFlags.Clear();
            _cabinHouseFlags.Clear();
            _cabinGameFlags.Clear();
            _pizzeriaFlags.Clear();
            _roadTripFlags.Clear();
            _cabinHouseManager = null;
            _cabinGameManager = null;
            _pizzeriaGameManager = null;
            _pizzeriaTruckDoor = null;
            _pizzeriaMike = null;
            _roadTripGameManager = null;
            _roadTripMikeInCar = null;
            _roadTripTruck = null;
            _lastDialogueText = string.Empty;
            _lastDialogueSpeaker = string.Empty;
            _lastDialogueSentTime = 0f;
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
            UnbindDialogueUiSelection();

            if (_remotePlayer != null && _remotePlayer.Root != null)
            {
                UnityEngine.Object.Destroy(_remotePlayer.Root.gameObject);
            }
            _remotePlayer = null;
            _lastSceneName = newScene.name;
            OnSceneEnterAdapters(newScene.name);
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
        }

        private void InitializeSceneAdapters()
        {
            _sceneAdapters.Clear();
            _sceneAdapters.Add(new DelegateHostSceneAdapter(
                "Cabin",
                scene => string.Equals(scene, "CabinScene", StringComparison.Ordinal) ||
                         string.Equals(scene, "CabinDarkScene", StringComparison.Ordinal),
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
                return;
            }

            var duration = Mathf.Clamp(1.5f + text.Length * 0.05f, 2f, 8f);
            _server.Enqueue(new DialogueLineMessage(string.Empty, text, duration, 2));
        }
    }
}
