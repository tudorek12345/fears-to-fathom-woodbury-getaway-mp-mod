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
        private RemotePlayerProxy _remotePlayer;
        private PlayerInputState _lastInputState;
        private bool _hasInputState;
        private readonly Dictionary<string, DoorState> _doorStates = new Dictionary<string, DoorState>();
        private readonly Dictionary<string, HoldableState> _holdableStates = new Dictionary<string, HoldableState>();
        private readonly Dictionary<string, int> _storyFlags = new Dictionary<string, int>();
        private readonly Dictionary<string, AiTransformState> _aiStates = new Dictionary<string, AiTransformState>();
        private PlayerTransformState _pendingClientTransform;
        private bool _hasPendingClientTransform;

        private CabinDoor[] _cabinDoors = new CabinDoor[0];
        private NOTLonely_Door.DoorScript[] _doorScripts = new NOTLonely_Door.DoorScript[0];
        private Holdable[] _holdables = new Holdable[0];
        private NavmeshPathAgent[] _aiAgents = new NavmeshPathAgent[0];
        private NavMeshAgent[] _navMeshAgents = new NavMeshAgent[0];

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
        private bool _clientSceneReady;
        private bool _awaitingSceneReady;
        private string _clientSceneName = string.Empty;
        private long _lastSceneRequestMs;
        private long _lastSceneReadyMs;
        private long _lastClientTransformMs;
        private long _lastHostTransformSendMs;

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

        public bool ClientSceneReady => _clientSceneReady;
        public bool AwaitingSceneReady => _awaitingSceneReady;
        public string ClientSceneName => _clientSceneName;
        public long LastSceneRequestMs => _lastSceneRequestMs;
        public long LastSceneReadyMs => _lastSceneReadyMs;
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
                BeginSceneHandshake();
                _logger.LogInfo("Co-op client connected, waiting for SceneReady");
            }
            else if (!_server.IsClientConnected && _lastClientConnected)
            {
                _lastClientConnected = false;
                _clientSceneReady = false;
                _awaitingSceneReady = false;
                _clientSceneName = string.Empty;
                _hasPendingClientTransform = false;
                _lastClientTransformMs = 0;
                _lastHostTransformSendMs = 0;
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
            if (_awaitingSceneReady && _server.IsClientConnected)
            {
                if (nowMs - _lastSceneRequestMs >= 1500)
                {
                    SendSceneChange();
                }
            }

            if (_server.IsClientConnected && nowMs >= _nextPlayerSendMs)
            {
                SendPlayerTransform();
                _nextPlayerSendMs = nowMs + (long)Math.Max(1, 1000.0 / Math.Max(1, _settings.SendHz.Value));
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
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (!TryBuildHostTransform(out var state)) return;

            _server.PublishHostTransform(state);
            _lastHostTransformSendMs = nowMs;
        }

        private bool TryBuildHostTransform(out PlayerTransformState state)
        {
            state = default;
            var playerController = PlayerController.GetInstance();
            var fps = _playerFirstPersonField != null
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
                    _lastStoryEventKey = key;
                    _lastStoryEventValue = value;
                    _lastStoryEventMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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

            foreach (var navAgent in _navMeshAgents)
            {
                if (navAgent == null) continue;
                if (navAgent.GetComponent<NavmeshPathAgent>() != null) continue;
                if (IsLocalPlayerAgent(navAgent)) continue;

                var state = new AiTransformState
                {
                    Path = NetPath.GetPath(navAgent.transform),
                    Position = navAgent.transform.position,
                    Rotation = navAgent.transform.rotation,
                    Active = navAgent.gameObject.activeInHierarchy
                };
                if (string.IsNullOrEmpty(state.Path)) continue;

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

        private void BeginSceneHandshake()
        {
            _clientSceneReady = false;
            _awaitingSceneReady = _server.IsClientConnected;
            _clientSceneName = string.Empty;
            _lastSceneRequestMs = 0;
            _lastSceneReadyMs = 0;
            _hasPendingClientTransform = false;
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
            _lastSceneRequestMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private void HandleSceneReady(SceneReadyMessage ready)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var activeScene = SceneManager.GetActiveScene().name;
            if (!string.Equals(ready.SceneName, activeScene, StringComparison.Ordinal))
            {
                _clientSceneReady = false;
                _awaitingSceneReady = true;
                SendSceneChange();
                return;
            }

            _clientSceneReady = true;
            _awaitingSceneReady = false;
            _clientSceneName = ready.SceneName;
            _lastSceneReadyMs = nowMs;

            if (_hasPendingClientTransform)
            {
                SetClientAvatar(_pendingClientTransform);
                _hasPendingClientTransform = false;
            }

            SendFullState(force: true);
            SendPlayerTransform();
            _nextFullSyncMs = nowMs + 5000;
            SnapRemotePlayerToHost();
            _logger.LogInfo("Co-op scene ready: " + ready.SceneName);
        }

        private void CacheSceneObjects()
        {
            _cabinDoors = UnityEngine.Object.FindObjectsOfType<CabinDoor>();
            _doorScripts = UnityEngine.Object.FindObjectsOfType<NOTLonely_Door.DoorScript>();
            _holdables = UnityEngine.Object.FindObjectsOfType<Holdable>();
            _aiAgents = UnityEngine.Object.FindObjectsOfType<NavmeshPathAgent>();
            _navMeshAgents = UnityEngine.Object.FindObjectsOfType<NavMeshAgent>();
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
            if (_server.IsRunning && _server.IsClientConnected)
            {
                _clientSceneReady = false;
                _awaitingSceneReady = true;
                _clientSceneName = string.Empty;
                _hasPendingClientTransform = false;
                SendSceneChange();
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
