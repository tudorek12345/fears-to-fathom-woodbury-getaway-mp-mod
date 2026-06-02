using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.AI;

namespace WoodburySpectatorSync.Coop
{
    internal static class ParkingLotSceneStateSync
    {
        public const string KeyPrefix = "SceneState.";

        private const string ManagerPrefix = KeyPrefix + "Manager.";
        private const string GamePrefix = KeyPrefix + "Game.";
        private const string PlayerPrefix = KeyPrefix + "Player.";
        private const string StrangerPrefix = KeyPrefix + "Stranger.";
        private const string MikePrefix = KeyPrefix + "Mike.";
        private const string WalkingCopPrefix = KeyPrefix + "WalkingCop.";
        private const string MikeTruckPrefix = KeyPrefix + "MikeTruck.";
        private const string StrangerCarPrefix = KeyPrefix + "StrangerCar.";
        private const string TruckDoorPrefix = KeyPrefix + "TruckDoor.";
        private const string ParkingTruckDoorPrefix = KeyPrefix + "ParkingTruckDoor.";
        private const string SuitcasePrefix = KeyPrefix + "Suitcase.";
        private const string UiPrefix = KeyPrefix + "UI.";
        private const string ElevatorPrefix = KeyPrefix + "Elevator.";
        private const string AntiThrowPrefix = KeyPrefix + "AntiThrow.";
        private const string TriggerSubPrefix = KeyPrefix + "TriggerSub.";
        private const string DisplaySubPrefix = KeyPrefix + "DisplaySub.";
        private const string GenericTriggerPrefix = KeyPrefix + "GenericTrigger.";
        private const string EventTriggerPrefix = KeyPrefix + "EventTrigger.";
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Dictionary<string, FieldInfo> FieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private static long _nextSuppressLogMs;

        public static int EmitHostFlags(string fullPrefix, Action<string, int> emit)
        {
            if (emit == null) return 0;

            var hash = 47;
            EmitManager(fullPrefix + ManagerPrefix, UnityEngine.Object.FindObjectOfType<ParkingLotManager>(), emit, ref hash);
            EmitGame(fullPrefix + GamePrefix, UnityEngine.Object.FindObjectOfType<ParkingLotGameManager>(), emit, ref hash);
            EmitPlayer(fullPrefix + PlayerPrefix, UnityEngine.Object.FindObjectOfType<ParkingLotPlayerController>(), emit, ref hash);
            EmitStranger(fullPrefix + StrangerPrefix, UnityEngine.Object.FindObjectOfType<ElevatorStranger>(), emit, ref hash);
            EmitMike(fullPrefix + MikePrefix, UnityEngine.Object.FindObjectOfType<MikeParkingLot>(), emit, ref hash);
            EmitWalkingCop(fullPrefix + WalkingCopPrefix, UnityEngine.Object.FindObjectOfType<WalkingCopController>(), emit, ref hash);
            EmitMikeTruck(fullPrefix + MikeTruckPrefix, UnityEngine.Object.FindObjectOfType<MikeTruckInParking>(), emit, ref hash);
            EmitStrangerCar(fullPrefix + StrangerCarPrefix, UnityEngine.Object.FindObjectOfType<StrangerCar>(), emit, ref hash);
            EmitTruckDoor(fullPrefix + TruckDoorPrefix, UnityEngine.Object.FindObjectOfType<TruckBackDoor>(), emit, ref hash);
            EmitParkingTruckDoor(fullPrefix + ParkingTruckDoorPrefix, UnityEngine.Object.FindObjectOfType<ParkingLotTruckDoor>(), emit, ref hash);
            EmitSuitcase(fullPrefix + SuitcasePrefix, UnityEngine.Object.FindObjectOfType<Suitcase>(), emit, ref hash);
            EmitUi(fullPrefix + UiPrefix, UnityEngine.Object.FindObjectOfType<ParkingLotUIManager>(), emit, ref hash);
            EmitElevator(fullPrefix + ElevatorPrefix, UnityEngine.Object.FindObjectOfType<ParkingLotElevatorManager>(), emit, ref hash);
            EmitAntiThrow(fullPrefix + AntiThrowPrefix, UnityEngine.Object.FindObjectsOfType<ParkingLotAntiThrowZone>(), emit, ref hash);
            EmitTriggerSubs(fullPrefix + TriggerSubPrefix, UnityEngine.Object.FindObjectsOfType<OnTriggerSub>(), emit, ref hash);
            EmitDisplaySubs(fullPrefix + DisplaySubPrefix, UnityEngine.Object.FindObjectsOfType<OnTriggerDisplaySub>(), emit, ref hash);
            EmitGenericTriggers(fullPrefix + GenericTriggerPrefix, UnityEngine.Object.FindObjectsOfType<OnTrigger>(), emit, ref hash);
            EmitEventTriggers(fullPrefix + EventTriggerPrefix, UnityEngine.Object.FindObjectsOfType<TriggerEventInvoker>(), emit, ref hash);
            return hash;
        }

        public static bool TryApplyFlag(string fieldName, int value, ManualLogSource logger)
        {
            if (string.IsNullOrEmpty(fieldName) ||
                !fieldName.StartsWith(KeyPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            SuppressLocalBrains(logger);

            if (fieldName.StartsWith(ManagerPrefix, StringComparison.Ordinal))
            {
                return TryApplyManager(fieldName.Substring(ManagerPrefix.Length), value);
            }

            if (fieldName.StartsWith(GamePrefix, StringComparison.Ordinal))
            {
                return TryApplyGame(fieldName.Substring(GamePrefix.Length), value);
            }

            if (fieldName.StartsWith(PlayerPrefix, StringComparison.Ordinal))
            {
                return TryApplyPlayer(fieldName.Substring(PlayerPrefix.Length), value);
            }

            if (fieldName.StartsWith(StrangerPrefix, StringComparison.Ordinal))
            {
                return TryApplyStranger(fieldName.Substring(StrangerPrefix.Length), value);
            }

            if (fieldName.StartsWith(MikePrefix, StringComparison.Ordinal))
            {
                return TryApplyMike(fieldName.Substring(MikePrefix.Length), value);
            }

            if (fieldName.StartsWith(WalkingCopPrefix, StringComparison.Ordinal))
            {
                return TryApplyWalkingCop(fieldName.Substring(WalkingCopPrefix.Length), value);
            }

            if (fieldName.StartsWith(MikeTruckPrefix, StringComparison.Ordinal))
            {
                return TryApplyMikeTruck(fieldName.Substring(MikeTruckPrefix.Length), value);
            }

            if (fieldName.StartsWith(StrangerCarPrefix, StringComparison.Ordinal))
            {
                return TryApplyStrangerCar(fieldName.Substring(StrangerCarPrefix.Length), value);
            }

            if (fieldName.StartsWith(TruckDoorPrefix, StringComparison.Ordinal))
            {
                return TryApplyTruckDoor(fieldName.Substring(TruckDoorPrefix.Length), value);
            }

            if (fieldName.StartsWith(ParkingTruckDoorPrefix, StringComparison.Ordinal))
            {
                return TryApplyParkingTruckDoor(fieldName.Substring(ParkingTruckDoorPrefix.Length), value);
            }

            if (fieldName.StartsWith(SuitcasePrefix, StringComparison.Ordinal))
            {
                return TryApplySuitcase(fieldName.Substring(SuitcasePrefix.Length), value);
            }

            if (fieldName.StartsWith(UiPrefix, StringComparison.Ordinal))
            {
                return TryApplyUi(fieldName.Substring(UiPrefix.Length), value);
            }

            if (fieldName.StartsWith(ElevatorPrefix, StringComparison.Ordinal))
            {
                return TryApplyElevator(fieldName.Substring(ElevatorPrefix.Length), value);
            }

            if (fieldName.StartsWith(AntiThrowPrefix, StringComparison.Ordinal))
            {
                return TryApplyAntiThrow(fieldName.Substring(AntiThrowPrefix.Length), value);
            }

            if (fieldName.StartsWith(TriggerSubPrefix, StringComparison.Ordinal))
            {
                return TryApplyTriggerSub(fieldName.Substring(TriggerSubPrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(DisplaySubPrefix, StringComparison.Ordinal))
            {
                return TryApplyDisplaySub(fieldName.Substring(DisplaySubPrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(GenericTriggerPrefix, StringComparison.Ordinal))
            {
                return TryApplyGenericTrigger(fieldName.Substring(GenericTriggerPrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(EventTriggerPrefix, StringComparison.Ordinal))
            {
                return TryApplyEventTrigger(fieldName.Substring(EventTriggerPrefix.Length), value, logger);
            }

            return true;
        }

        public static void CollectSyncedTransforms(List<Transform> transforms)
        {
            if (transforms == null) return;

            var stranger = UnityEngine.Object.FindObjectOfType<ElevatorStranger>();
            if (stranger != null && stranger.gameObject.activeInHierarchy)
            {
                transforms.Add(stranger.transform);
            }

            var suitcase = UnityEngine.Object.FindObjectOfType<Suitcase>();
            if (suitcase != null && suitcase.gameObject.activeInHierarchy)
            {
                transforms.Add(suitcase.transform);
            }

            var mike = UnityEngine.Object.FindObjectOfType<MikeParkingLot>();
            if (mike != null && mike.gameObject.activeInHierarchy)
            {
                transforms.Add(mike.transform);
            }

            var walkingCop = UnityEngine.Object.FindObjectOfType<WalkingCopController>();
            if (walkingCop != null && walkingCop.gameObject.activeInHierarchy)
            {
                transforms.Add(walkingCop.transform);
            }

            var mikeTruck = UnityEngine.Object.FindObjectOfType<MikeTruckInParking>();
            if (mikeTruck != null && mikeTruck.gameObject.activeInHierarchy)
            {
                transforms.Add(mikeTruck.transform);
            }

            var strangerCar = UnityEngine.Object.FindObjectOfType<StrangerCar>();
            if (strangerCar != null && strangerCar.gameObject.activeInHierarchy)
            {
                transforms.Add(strangerCar.transform);
            }

            var elevator = UnityEngine.Object.FindObjectOfType<ElevatorDoorManager>();
            if (elevator != null && elevator.gameObject.activeInHierarchy)
            {
                AddTransform(transforms, GetFieldValue<Transform>(elevator, "DoorLeft"));
                AddTransform(transforms, GetFieldValue<Transform>(elevator, "DoorRight"));
            }

            var truckBackDoor = UnityEngine.Object.FindObjectOfType<TruckBackDoor>();
            if (truckBackDoor != null && truckBackDoor.gameObject.activeInHierarchy)
            {
                transforms.Add(truckBackDoor.transform);
            }
        }

        private static void EmitManager(string prefix, ParkingLotManager manager, Action<string, int> emit, ref int hash)
        {
            if (manager == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", manager.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "NotificationOnScreen", manager.notificationOnScreen ? 1 : 0, emit, ref hash);
            Emit(prefix + "TalkingToStranger", manager.talkingToStranger ? 1 : 0, emit, ref hash);
            Emit(prefix + "Called", GetFieldValue<bool>(manager, "called") ? 1 : 0, emit, ref hash);
            Emit(prefix + "MikeTruck", IsObjectActive(manager.mikeTruck) ? 1 : 0, emit, ref hash);
        }

        private static void EmitGame(string prefix, ParkingLotGameManager game, Action<string, int> emit, ref int hash)
        {
            if (game == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", game.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "CurrentState", (int)game.currentState, emit, ref hash);
            Emit(prefix + "CurrentPlayerState", (int)game.currentPlayerState, emit, ref hash);
            Emit(prefix + "IntroDone", game.introDone ? 1 : 0, emit, ref hash);
            Emit(prefix + "PhoneRinging", game.isPhoneRinging ? 1 : 0, emit, ref hash);
            Emit(prefix + "WasInConvoDuringCallCheck", GetFieldValue<bool>(game, "wasInConvoDuringCallCheck") ? 1 : 0, emit, ref hash);
            Emit(prefix + "HangUpAudio", game.hangUpAS != null && game.hangUpAS.isPlaying ? 1 : 0, emit, ref hash);
        }

        private static void EmitPlayer(string prefix, ParkingLotPlayerController player, Action<string, int> emit, ref int hash)
        {
            if (player == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            var firstPerson = GetFieldValue<Behaviour>(player, "firstPersonController");
            var mainCamera = GetFieldValue<Camera>(player, "mainCamera");
            var currentHoldingObject = GetFieldObject(player, "currentHoldingObject");
            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", player.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Enabled", player.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "CanThrowItem", player.canThrowItem ? 1 : 0, emit, ref hash);
            Emit(prefix + "FirstPersonEnabled", firstPerson != null && firstPerson.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "HoldingObject", currentHoldingObject != null ? 1 : 0, emit, ref hash);
            Emit(prefix + "CameraFov10", mainCamera != null ? Mathf.RoundToInt(mainCamera.fieldOfView * 10f) : 0, emit, ref hash);
            Emit(prefix + "LookAt", player.lookAt != null && player.lookAt.gameObject.activeInHierarchy ? 1 : 0, emit, ref hash);
        }

        private static void EmitStranger(string prefix, ElevatorStranger stranger, Action<string, int> emit, ref int hash)
        {
            if (stranger == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            var agent = stranger.GetComponent<NavMeshAgent>();
            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", stranger.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Enabled", stranger.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "State", (int)stranger.state, emit, ref hash);
            Emit(prefix + "Moving", stranger.moving ? 1 : 0, emit, ref hash);
            Emit(prefix + "GoingToCar", stranger.goingToCar ? 1 : 0, emit, ref hash);
            Emit(prefix + "CallHangUp", stranger.callHangUpBecauseOfStranger ? 1 : 0, emit, ref hash);
            Emit(prefix + "Convo1Done", GetFieldValue<bool>(stranger, "convo1Done") ? 1 : 0, emit, ref hash);
            Emit(prefix + "Convo2Done", GetFieldValue<bool>(stranger, "convo2Done") ? 1 : 0, emit, ref hash);
            Emit(prefix + "StartedFirstConvo", GetFieldValue<bool>(stranger, "startedFirstConvo") ? 1 : 0, emit, ref hash);
            Emit(prefix + "Go", GetFieldValue<bool>(stranger, "go") ? 1 : 0, emit, ref hash);
            Emit(prefix + "Handheld", IsObjectActive(GetFieldObject(stranger, "handheldConsole")) ? 1 : 0, emit, ref hash);
            Emit(prefix + "Footsteps", IsObjectActive(stranger.footStepsSystem) ? 1 : 0, emit, ref hash);
            Emit(prefix + "Collider", stranger.capsuleCollider != null && stranger.capsuleCollider.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "AgentEnabled", agent != null && agent.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "AgentStopped", agent != null && agent.isStopped ? 1 : 0, emit, ref hash);
            Emit(prefix + "AnimatorState", stranger.animator != null ? stranger.animator.GetInteger(Animator.StringToHash("State")) : 0, emit, ref hash);
        }

        private static void EmitMike(string prefix, MikeParkingLot mike, Action<string, int> emit, ref int hash)
        {
            if (mike == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            var agent = mike.GetComponent<NavMeshAgent>();
            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", mike.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Enabled", mike.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "State", (int)mike.state, emit, ref hash);
            Emit(prefix + "Moving", mike.moving ? 1 : 0, emit, ref hash);
            Emit(prefix + "FirstConvoDone", mike.firstConvoDone ? 1 : 0, emit, ref hash);
            Emit(prefix + "LookAtArrival", mike.lookAtPlayerDuringArrivalInCar ? 1 : 0, emit, ref hash);
            Emit(prefix + "Go", GetFieldValue<bool>(mike, "go") ? 1 : 0, emit, ref hash);
            Emit(prefix + "LookAtAfterSittingBack", GetFieldValue<bool>(mike, "lookAtPlayerAfterSittingBack") ? 1 : 0, emit, ref hash);
            Emit(prefix + "HugConversationDone", GetFieldValue<bool>(mike, "hugConversationDone") ? 1 : 0, emit, ref hash);
            Emit(prefix + "Collider", mike.capsuleCollider != null && mike.capsuleCollider.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "AgentEnabled", agent != null && agent.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "AgentStopped", agent != null && agent.isStopped ? 1 : 0, emit, ref hash);
            Emit(prefix + "AnimatorState", mike.animator != null ? mike.animator.GetInteger(Animator.StringToHash("State")) : 0, emit, ref hash);
            Emit(prefix + "BagInsideTruckTrigger", IsObjectActive(GetFieldObject(mike, "bagInsideTruckTrigger")) ? 1 : 0, emit, ref hash);
            Emit(prefix + "AntiPlayerPushCapsule", IsObjectActive(GetFieldObject(mike, "antiPlayerPushCapsule")) ? 1 : 0, emit, ref hash);
        }

        private static void EmitWalkingCop(string prefix, WalkingCopController cop, Action<string, int> emit, ref int hash)
        {
            if (cop == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            var agent = GetFieldValue<NavMeshAgent>(cop, "navmeshAgent") ?? cop.GetComponent<NavMeshAgent>();
            var animator = GetFieldValue<Animator>(cop, "animator") ?? cop.GetComponent<Animator>();
            var targetPoint = GetFieldValue<Transform>(cop, "targetPoint");
            var walkToPoint = GetFieldValue<Transform>(cop, "walkToPoint");
            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", cop.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Enabled", cop.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "State", GetEnumFieldInt(cop, "currentState"), emit, ref hash);
            Emit(prefix + "AgentEnabled", agent != null && agent.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "AgentStopped", agent != null && agent.enabled && agent.isStopped ? 1 : 0, emit, ref hash);
            Emit(prefix + "AnimatorEnabled", animator != null && animator.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "AnimatorStateHash", animator != null ? animator.GetCurrentAnimatorStateInfo(0).shortNameHash : 0, emit, ref hash);
            Emit(prefix + "AnimatorSpeed100", animator != null ? Mathf.RoundToInt(animator.speed * 100f) : 0, emit, ref hash);
            Emit(prefix + "TargetIsWalkPoint", targetPoint != null && walkToPoint != null && targetPoint == walkToPoint ? 1 : 0, emit, ref hash);
        }

        private static void EmitMikeTruck(string prefix, MikeTruckInParking truck, Action<string, int> emit, ref int hash)
        {
            if (truck == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", truck.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Enabled", truck.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "Speed100", Mathf.RoundToInt(truck.speed * 100f), emit, ref hash);
            Emit(prefix + "InitialDistance100", Mathf.RoundToInt(truck.initialDistance * 100f), emit, ref hash);
            Emit(prefix + "Distance100", Mathf.RoundToInt(GetFieldValue<float>(truck, "distanceTravelled") * 100f), emit, ref hash);
            Emit(prefix + "HasMikeExited", GetFieldValue<bool>(truck, "hasMikeExited") ? 1 : 0, emit, ref hash);
            Emit(prefix + "Exited", GetFieldValue<bool>(truck, "exited") ? 1 : 0, emit, ref hash);
            Emit(prefix + "LookAtCalled", GetFieldValue<bool>(truck, "lookAtCalled") ? 1 : 0, emit, ref hash);
            Emit(prefix + "AnimatorEnabled", truck.animator != null && truck.animator.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "AnimatorSpeed100", truck.animator != null ? Mathf.RoundToInt(truck.animator.speed * 100f) : 0, emit, ref hash);
            Emit(prefix + "AntiAccident", IsObjectActive(GetFieldObject(truck, "antiTruckAccidentCollider")) ? 1 : 0, emit, ref hash);
            Emit(prefix + "TruckAudio", GetFieldValue<AudioSource>(truck, "truckAS") != null && GetFieldValue<AudioSource>(truck, "truckAS").isPlaying ? 1 : 0, emit, ref hash);
            Emit(prefix + "HonkAudio", GetFieldValue<AudioSource>(truck, "honkAS") != null && GetFieldValue<AudioSource>(truck, "honkAS").isPlaying ? 1 : 0, emit, ref hash);
        }

        private static void EmitStrangerCar(string prefix, StrangerCar car, Action<string, int> emit, ref int hash)
        {
            if (car == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            var animator = GetFieldValue<Animator>(car, "animator");
            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", car.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Enabled", car.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "AnimatorState", animator != null ? animator.GetInteger(Animator.StringToHash("state")) : 0, emit, ref hash);
            Emit(prefix + "AnimatorSpeed100", animator != null ? Mathf.RoundToInt(animator.speed * 100f) : 0, emit, ref hash);
            Emit(prefix + "HornPlayed", car.hornPlayed ? 1 : 0, emit, ref hash);
            Emit(prefix + "Reversing", car.isReversing ? 1 : 0, emit, ref hash);
            Emit(prefix + "MovingForward", car.isMovingForwad ? 1 : 0, emit, ref hash);
            Emit(prefix + "PlayerBlocking", car.playerBlockingPath ? 1 : 0, emit, ref hash);
            Emit(prefix + "HeadLights", IsObjectActive(car.headLights) ? 1 : 0, emit, ref hash);
            Emit(prefix + "AntiPlayerTrigger", IsObjectActive(car.antiPlayerTrigger) ? 1 : 0, emit, ref hash);
            Emit(prefix + "EngineAudio", car.engineIdleAS != null && car.engineIdleAS.isPlaying ? 1 : 0, emit, ref hash);
            Emit(prefix + "HornAudio", car.hornAS != null && car.hornAS.isPlaying ? 1 : 0, emit, ref hash);
        }

        private static void EmitTruckDoor(string prefix, TruckBackDoor door, Action<string, int> emit, ref int hash)
        {
            if (door == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", door.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Open", door.doorOpen ? 1 : 0, emit, ref hash);
            Emit(prefix + "LocalRotX10", Mathf.RoundToInt(door.transform.localEulerAngles.x * 10f), emit, ref hash);
            Emit(prefix + "LocalRotY10", Mathf.RoundToInt(door.transform.localEulerAngles.y * 10f), emit, ref hash);
            Emit(prefix + "LocalRotZ10", Mathf.RoundToInt(door.transform.localEulerAngles.z * 10f), emit, ref hash);
        }

        private static void EmitParkingTruckDoor(string prefix, ParkingLotTruckDoor door, Action<string, int> emit, ref int hash)
        {
            if (door == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", door.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "BagPlaced", door.bagPlaced ? 1 : 0, emit, ref hash);
        }

        private static void EmitSuitcase(string prefix, Suitcase suitcase, Action<string, int> emit, ref int hash)
        {
            if (suitcase == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            var body = suitcase.GetComponent<Rigidbody>();
            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", suitcase.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "IsInside", suitcase.isInside ? 1 : 0, emit, ref hash);
            Emit(prefix + "InTruck", suitcase.inTruck ? 1 : 0, emit, ref hash);
            Emit(prefix + "ConversationTriggered", suitcase.conversationTriggered ? 1 : 0, emit, ref hash);
            Emit(prefix + "TeleportingBagNextToMike", suitcase.teleportingBagNextToMike ? 1 : 0, emit, ref hash);
            Emit(prefix + "TeleportedBag", suitcase.teleportedBag ? 1 : 0, emit, ref hash);
            Emit(prefix + "Layer", suitcase.gameObject.layer, emit, ref hash);
            Emit(prefix + "Kinematic", body != null && body.isKinematic ? 1 : 0, emit, ref hash);
        }

        private static void EmitUi(string prefix, ParkingLotUIManager ui, Action<string, int> emit, ref int hash)
        {
            if (ui == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            var introCanvas = GetFieldObject(ui, "introCanvas");
            var whiteIntro = GetFieldObject(ui, "whiteIntroManager");
            var dialogueCamera = GetFieldObject(ui, "dialogueCamera");
            var phoneUi = ui.phoneUI;
            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", ui.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "InConversation", ui.inCoversation ? 1 : 0, emit, ref hash);
            Emit(prefix + "IntroCanvas", IsObjectActive(introCanvas) ? 1 : 0, emit, ref hash);
            Emit(prefix + "WhiteIntroActive", IsObjectActive(whiteIntro) ? 1 : 0, emit, ref hash);
            Emit(prefix + "WhiteIntroTextCount", GetTextBehaviourCount(whiteIntro, "textsToShow"), emit, ref hash);
            Emit(prefix + "WhiteIntroTextMask", BuildTextBehaviourEnabledMask(whiteIntro, "textsToShow"), emit, ref hash);
            Emit(prefix + "DialogueCamera", IsObjectActive(dialogueCamera) ? 1 : 0, emit, ref hash);
            Emit(prefix + "NotifWasOnscreen", GetFieldValue<bool>(ui, "notifWasOnscreen") ? 1 : 0, emit, ref hash);
            Emit(prefix + "PhoneAllowed", phoneUi != null && phoneUi.allowPhone ? 1 : 0, emit, ref hash);
            Emit(prefix + "PhonePaused", phoneUi != null && phoneUi.isPaused ? 1 : 0, emit, ref hash);
            Emit(prefix + "FadeCanvas", IsObjectActive(GetFieldObject(ui, "fadeCanvas")) ? 1 : 0, emit, ref hash);
        }

        private static void EmitElevator(string prefix, ParkingLotElevatorManager manager, Action<string, int> emit, ref int hash)
        {
            if (manager == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            var door = GetFieldValue<ElevatorDoorManager>(manager, "elevatorDoorManager");
            var antiThrow = GetFieldValue<BoxCollider>(manager, "doorAntiThrowTrigger");
            var elevatorAudio = GetFieldValue<AudioSource>(manager, "elevatorAudioSource");
            var effectsAudio = GetFieldValue<AudioSource>(manager, "effectsAudioSource");
            var emission = GetFieldValue<Transform>(manager, "emissionPlanesTransform");
            var floorText = GetFieldObject(manager, "floorTMPText");
            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", manager.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Testing", manager.IsTesting ? 1 : 0, emit, ref hash);
            Emit(prefix + "Travelling", GetFieldValue<bool>(manager, "travelling") ? 1 : 0, emit, ref hash);
            Emit(prefix + "TripTimeMs", Mathf.RoundToInt(GetFieldValue<float>(manager, "tripTime") * 1000f), emit, ref hash);
            Emit(prefix + "DoorObject", IsObjectActive(door) ? 1 : 0, emit, ref hash);
            Emit(prefix + "DoorOpen", door != null && door.IsOpen ? 1 : 0, emit, ref hash);
            Emit(prefix + "DoorTransitioning", door != null && door.IsTransitioning ? 1 : 0, emit, ref hash);
            Emit(prefix + "AntiThrowEnabled", antiThrow != null && antiThrow.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "ElevatorAudio", elevatorAudio != null && elevatorAudio.isPlaying ? 1 : 0, emit, ref hash);
            Emit(prefix + "EffectsAudio", effectsAudio != null && effectsAudio.isPlaying ? 1 : 0, emit, ref hash);
            Emit(prefix + "EmissionLocalX", emission != null ? Mathf.RoundToInt(emission.localPosition.x * 1000f) : 0, emit, ref hash);
            Emit(prefix + "EmissionLocalY", emission != null ? Mathf.RoundToInt(emission.localPosition.y * 1000f) : 0, emit, ref hash);
            Emit(prefix + "EmissionLocalZ", emission != null ? Mathf.RoundToInt(emission.localPosition.z * 1000f) : 0, emit, ref hash);
            Emit(prefix + "FloorTextActive", IsObjectActive(floorText) ? 1 : 0, emit, ref hash);
        }

        private static void EmitAntiThrow(string prefix, ParkingLotAntiThrowZone[] zones, Action<string, int> emit, ref int hash)
        {
            SortByPath(zones);
            Emit(prefix + "Count", zones != null ? zones.Length : 0, emit, ref hash);
            if (zones == null) return;

            for (var i = 0; i < zones.Length; i++)
            {
                var zone = zones[i];
                if (zone == null) continue;
                var itemPrefix = prefix + i + ".";
                Emit(itemPrefix + "RootActive", zone.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Enabled", zone.enabled ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Triggered", zone.triggerd ? 1 : 0, emit, ref hash);
                var collider = zone.GetComponent<Collider>();
                Emit(itemPrefix + "Collider", collider != null && collider.enabled ? 1 : 0, emit, ref hash);
            }
        }

        private static void EmitTriggerSubs(string prefix, OnTriggerSub[] triggers, Action<string, int> emit, ref int hash)
        {
            SortByPath(triggers);
            Emit(prefix + "Count", triggers != null ? triggers.Length : 0, emit, ref hash);
            if (triggers == null) return;

            for (var i = 0; i < triggers.Length; i++)
            {
                var trigger = triggers[i];
                if (trigger == null) continue;
                var itemPrefix = prefix + i + ".";
                Emit(itemPrefix + "RootActive", trigger.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Enabled", trigger.enabled ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "OnlyOnce", trigger.onlyOnce ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Entered", GetFieldValue<bool>(trigger, "entered") ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "EnterObject", IsObjectActive(trigger.enterGameObject) ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "SubKeyHash", StableStringHash(trigger.subKey), emit, ref hash);
                Emit(itemPrefix + "SubTimeMs", Mathf.RoundToInt(trigger.subTime * 1000f), emit, ref hash);
            }
        }

        private static void EmitDisplaySubs(string prefix, OnTriggerDisplaySub[] triggers, Action<string, int> emit, ref int hash)
        {
            SortByPath(triggers);
            Emit(prefix + "Count", triggers != null ? triggers.Length : 0, emit, ref hash);
            if (triggers == null) return;

            for (var i = 0; i < triggers.Length; i++)
            {
                var trigger = triggers[i];
                if (trigger == null) continue;
                var itemPrefix = prefix + i + ".";
                Emit(itemPrefix + "RootActive", trigger.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Enabled", trigger.enabled ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "OnlyOnce", trigger.onlyOnce ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Entered", GetFieldValue<bool>(trigger, "entered") ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "EnterObject", IsObjectActive(trigger.enterGameObject) ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "SubKeyHash", StableStringHash(GetFieldValue<string>(trigger, "subKey")), emit, ref hash);
            }
        }

        private static void EmitGenericTriggers(string prefix, OnTrigger[] triggers, Action<string, int> emit, ref int hash)
        {
            SortByPath(triggers);
            Emit(prefix + "Count", triggers != null ? triggers.Length : 0, emit, ref hash);
            if (triggers == null) return;

            for (var i = 0; i < triggers.Length; i++)
            {
                var trigger = triggers[i];
                if (trigger == null) continue;
                var itemPrefix = prefix + i + ".";
                Emit(itemPrefix + "RootActive", trigger.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Enabled", trigger.enabled ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "OnlyOnce", trigger.onlyOnce ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Entered", GetFieldValue<bool>(trigger, "entered") ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "EnterObject", IsObjectActive(trigger.enterGameObject) ? 1 : 0, emit, ref hash);
            }
        }

        private static void EmitEventTriggers(string prefix, TriggerEventInvoker[] triggers, Action<string, int> emit, ref int hash)
        {
            SortByPath(triggers);
            Emit(prefix + "Count", triggers != null ? triggers.Length : 0, emit, ref hash);
            if (triggers == null) return;

            for (var i = 0; i < triggers.Length; i++)
            {
                var trigger = triggers[i];
                if (trigger == null) continue;
                var itemPrefix = prefix + i + ".";
                Emit(itemPrefix + "RootActive", trigger.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Enabled", trigger.enabled ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "OnlyOnce", GetFieldValue<bool>(trigger, "onlyOnce") ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Triggered", trigger.isTriggered ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "EnterObject", IsObjectActive(trigger.enterGameObject) ? 1 : 0, emit, ref hash);
            }
        }

        private static bool TryApplyManager(string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var manager = UnityEngine.Object.FindObjectOfType<ParkingLotManager>();
            if (manager == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { manager.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "NotificationOnScreen", StringComparison.Ordinal)) { manager.notificationOnScreen = value != 0; return true; }
            if (string.Equals(name, "TalkingToStranger", StringComparison.Ordinal)) { manager.talkingToStranger = value != 0; return true; }
            if (string.Equals(name, "Called", StringComparison.Ordinal)) { SetFieldValue(manager, "called", value != 0); return true; }
            if (string.Equals(name, "MikeTruck", StringComparison.Ordinal)) { SetObjectActive(manager.mikeTruck, value != 0); return true; }
            return true;
        }

        private static bool TryApplyGame(string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var game = UnityEngine.Object.FindObjectOfType<ParkingLotGameManager>();
            if (game == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { game.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "CurrentState", StringComparison.Ordinal)) { game.currentState = (ParkingLotGameManager.GameState)value; return true; }
            if (string.Equals(name, "CurrentPlayerState", StringComparison.Ordinal)) { game.currentPlayerState = (ParkingLotGameManager.PlayerState)value; return true; }
            if (string.Equals(name, "IntroDone", StringComparison.Ordinal)) { game.introDone = value != 0; return true; }
            if (string.Equals(name, "PhoneRinging", StringComparison.Ordinal)) { game.isPhoneRinging = value != 0; return true; }
            if (string.Equals(name, "WasInConvoDuringCallCheck", StringComparison.Ordinal)) { SetFieldValue(game, "wasInConvoDuringCallCheck", value != 0); return true; }
            if (string.Equals(name, "HangUpAudio", StringComparison.Ordinal)) { ApplyAudioPlayback(game.hangUpAS, value != 0); return true; }
            return true;
        }

        private static bool TryApplyPlayer(string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var player = UnityEngine.Object.FindObjectOfType<ParkingLotPlayerController>();
            if (player == null) return false;

            var firstPerson = GetFieldValue<Behaviour>(player, "firstPersonController");
            var mainCamera = GetFieldValue<Camera>(player, "mainCamera");
            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { player.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { return true; }
            if (string.Equals(name, "CanThrowItem", StringComparison.Ordinal)) { player.canThrowItem = value != 0; return true; }
            if (string.Equals(name, "FirstPersonEnabled", StringComparison.Ordinal)) { if (firstPerson != null) firstPerson.enabled = value != 0; return true; }
            if (string.Equals(name, "HoldingObject", StringComparison.Ordinal)) { return true; }
            if (string.Equals(name, "CameraFov10", StringComparison.Ordinal)) { if (mainCamera != null) mainCamera.fieldOfView = value / 10f; return true; }
            if (string.Equals(name, "LookAt", StringComparison.Ordinal)) { return true; }
            return true;
        }

        private static bool TryApplyStranger(string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var stranger = UnityEngine.Object.FindObjectOfType<ElevatorStranger>();
            if (stranger == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { stranger.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { stranger.enabled = false; return true; }
            if (string.Equals(name, "State", StringComparison.Ordinal)) { stranger.state = (ElevatorStranger.State)value; return true; }
            if (string.Equals(name, "Moving", StringComparison.Ordinal)) { stranger.moving = value != 0; return true; }
            if (string.Equals(name, "GoingToCar", StringComparison.Ordinal)) { stranger.goingToCar = value != 0; return true; }
            if (string.Equals(name, "CallHangUp", StringComparison.Ordinal)) { stranger.callHangUpBecauseOfStranger = value != 0; return true; }
            if (string.Equals(name, "Convo1Done", StringComparison.Ordinal)) { SetFieldValue(stranger, "convo1Done", value != 0); return true; }
            if (string.Equals(name, "Convo2Done", StringComparison.Ordinal)) { SetFieldValue(stranger, "convo2Done", value != 0); return true; }
            if (string.Equals(name, "StartedFirstConvo", StringComparison.Ordinal)) { SetFieldValue(stranger, "startedFirstConvo", value != 0); return true; }
            if (string.Equals(name, "Go", StringComparison.Ordinal)) { SetFieldValue(stranger, "go", value != 0); return true; }
            if (string.Equals(name, "Handheld", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(stranger, "handheldConsole"), value != 0); return true; }
            if (string.Equals(name, "Footsteps", StringComparison.Ordinal)) { SetObjectActive(stranger.footStepsSystem, value != 0); return true; }
            if (string.Equals(name, "Collider", StringComparison.Ordinal)) { if (stranger.capsuleCollider != null) stranger.capsuleCollider.enabled = value != 0; return true; }
            if (string.Equals(name, "AgentEnabled", StringComparison.Ordinal))
            {
                var agent = stranger.GetComponent<NavMeshAgent>();
                if (agent != null) agent.enabled = false;
                return true;
            }
            if (string.Equals(name, "AgentStopped", StringComparison.Ordinal))
            {
                var agent = stranger.GetComponent<NavMeshAgent>();
                if (agent != null && agent.enabled) agent.isStopped = value != 0;
                return true;
            }
            if (string.Equals(name, "AnimatorState", StringComparison.Ordinal))
            {
                if (stranger.animator != null) stranger.animator.SetInteger(Animator.StringToHash("State"), value);
                return true;
            }

            return true;
        }

        private static bool TryApplyMike(string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var mike = UnityEngine.Object.FindObjectOfType<MikeParkingLot>();
            if (mike == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { mike.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { mike.enabled = false; return true; }
            if (string.Equals(name, "State", StringComparison.Ordinal)) { mike.state = (MikeParkingLot.State)value; return true; }
            if (string.Equals(name, "Moving", StringComparison.Ordinal)) { mike.moving = value != 0; return true; }
            if (string.Equals(name, "FirstConvoDone", StringComparison.Ordinal)) { mike.firstConvoDone = value != 0; return true; }
            if (string.Equals(name, "LookAtArrival", StringComparison.Ordinal)) { mike.lookAtPlayerDuringArrivalInCar = value != 0; return true; }
            if (string.Equals(name, "Go", StringComparison.Ordinal)) { SetFieldValue(mike, "go", value != 0); return true; }
            if (string.Equals(name, "LookAtAfterSittingBack", StringComparison.Ordinal)) { SetFieldValue(mike, "lookAtPlayerAfterSittingBack", value != 0); return true; }
            if (string.Equals(name, "HugConversationDone", StringComparison.Ordinal)) { SetFieldValue(mike, "hugConversationDone", value != 0); return true; }
            if (string.Equals(name, "Collider", StringComparison.Ordinal)) { if (mike.capsuleCollider != null) mike.capsuleCollider.enabled = value != 0; return true; }
            if (string.Equals(name, "AgentEnabled", StringComparison.Ordinal)) { var agent = mike.GetComponent<NavMeshAgent>(); if (agent != null) agent.enabled = false; return true; }
            if (string.Equals(name, "AgentStopped", StringComparison.Ordinal)) { var agent = mike.GetComponent<NavMeshAgent>(); if (agent != null && agent.enabled) agent.isStopped = value != 0; return true; }
            if (string.Equals(name, "AnimatorState", StringComparison.Ordinal)) { if (mike.animator != null) mike.animator.SetInteger(Animator.StringToHash("State"), value); return true; }
            if (string.Equals(name, "BagInsideTruckTrigger", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(mike, "bagInsideTruckTrigger"), value != 0); return true; }
            if (string.Equals(name, "AntiPlayerPushCapsule", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(mike, "antiPlayerPushCapsule"), value != 0); return true; }
            return true;
        }

        private static bool TryApplyWalkingCop(string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var cop = UnityEngine.Object.FindObjectOfType<WalkingCopController>();
            if (cop == null) return false;

            var agent = GetFieldValue<NavMeshAgent>(cop, "navmeshAgent") ?? cop.GetComponent<NavMeshAgent>();
            var animator = GetFieldValue<Animator>(cop, "animator") ?? cop.GetComponent<Animator>();
            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { cop.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { cop.enabled = false; return true; }
            if (string.Equals(name, "State", StringComparison.Ordinal)) { SetEnumFieldInt(cop, "currentState", value); return true; }
            if (string.Equals(name, "AgentEnabled", StringComparison.Ordinal)) { if (agent != null) agent.enabled = false; return true; }
            if (string.Equals(name, "AgentStopped", StringComparison.Ordinal)) { if (agent != null && agent.enabled) agent.isStopped = value != 0; return true; }
            if (string.Equals(name, "AnimatorEnabled", StringComparison.Ordinal)) { if (animator != null) animator.enabled = true; return true; }
            if (string.Equals(name, "AnimatorStateHash", StringComparison.Ordinal)) { return true; }
            if (string.Equals(name, "AnimatorSpeed100", StringComparison.Ordinal)) { if (animator != null) animator.speed = Mathf.Max(0f, value / 100f); return true; }
            if (string.Equals(name, "TargetIsWalkPoint", StringComparison.Ordinal))
            {
                if (value != 0)
                {
                    SetFieldValue(cop, "targetPoint", GetFieldValue<Transform>(cop, "walkToPoint"));
                }

                return true;
            }

            return true;
        }

        private static bool TryApplyMikeTruck(string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var truck = UnityEngine.Object.FindObjectOfType<MikeTruckInParking>();
            if (truck == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { truck.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { truck.enabled = false; return true; }
            if (string.Equals(name, "Speed100", StringComparison.Ordinal)) { truck.speed = value / 100f; return true; }
            if (string.Equals(name, "InitialDistance100", StringComparison.Ordinal)) { truck.initialDistance = value / 100f; return true; }
            if (string.Equals(name, "Distance100", StringComparison.Ordinal)) { SetFieldValue(truck, "distanceTravelled", value / 100f); return true; }
            if (string.Equals(name, "HasMikeExited", StringComparison.Ordinal)) { SetFieldValue(truck, "hasMikeExited", value != 0); return true; }
            if (string.Equals(name, "Exited", StringComparison.Ordinal)) { SetFieldValue(truck, "exited", value != 0); return true; }
            if (string.Equals(name, "LookAtCalled", StringComparison.Ordinal)) { SetFieldValue(truck, "lookAtCalled", value != 0); return true; }
            if (string.Equals(name, "AnimatorEnabled", StringComparison.Ordinal)) { if (truck.animator != null) truck.animator.enabled = value != 0; return true; }
            if (string.Equals(name, "AnimatorSpeed100", StringComparison.Ordinal)) { if (truck.animator != null) truck.animator.speed = value / 100f; return true; }
            if (string.Equals(name, "AntiAccident", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(truck, "antiTruckAccidentCollider"), value != 0); return true; }
            if (string.Equals(name, "TruckAudio", StringComparison.Ordinal)) { ApplyAudioPlayback(GetFieldValue<AudioSource>(truck, "truckAS"), value != 0); return true; }
            if (string.Equals(name, "HonkAudio", StringComparison.Ordinal)) { ApplyAudioPlayback(GetFieldValue<AudioSource>(truck, "honkAS"), value != 0); return true; }
            return true;
        }

        private static bool TryApplyStrangerCar(string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var car = UnityEngine.Object.FindObjectOfType<StrangerCar>();
            if (car == null) return false;

            var animator = GetFieldValue<Animator>(car, "animator");
            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { car.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { car.enabled = false; return true; }
            if (string.Equals(name, "AnimatorState", StringComparison.Ordinal)) { if (animator != null) animator.SetInteger(Animator.StringToHash("state"), value); return true; }
            if (string.Equals(name, "AnimatorSpeed100", StringComparison.Ordinal)) { if (animator != null) animator.speed = value / 100f; return true; }
            if (string.Equals(name, "HornPlayed", StringComparison.Ordinal)) { car.hornPlayed = value != 0; return true; }
            if (string.Equals(name, "Reversing", StringComparison.Ordinal)) { car.isReversing = value != 0; return true; }
            if (string.Equals(name, "MovingForward", StringComparison.Ordinal)) { car.isMovingForwad = value != 0; return true; }
            if (string.Equals(name, "PlayerBlocking", StringComparison.Ordinal)) { car.playerBlockingPath = value != 0; return true; }
            if (string.Equals(name, "HeadLights", StringComparison.Ordinal)) { SetObjectActive(car.headLights, value != 0); return true; }
            if (string.Equals(name, "AntiPlayerTrigger", StringComparison.Ordinal)) { SetObjectActive(car.antiPlayerTrigger, value != 0); return true; }
            if (string.Equals(name, "EngineAudio", StringComparison.Ordinal)) { ApplyAudioPlayback(car.engineIdleAS, value != 0); return true; }
            if (string.Equals(name, "HornAudio", StringComparison.Ordinal)) { ApplyAudioPlayback(car.hornAS, value != 0); return true; }
            return true;
        }

        private static bool TryApplyTruckDoor(string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var door = UnityEngine.Object.FindObjectOfType<TruckBackDoor>();
            if (door == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { door.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Open", StringComparison.Ordinal)) { door.doorOpen = value != 0; ApplyTruckDoorRotation(door); return true; }
            if (string.Equals(name, "LocalRotX10", StringComparison.Ordinal) ||
                string.Equals(name, "LocalRotY10", StringComparison.Ordinal) ||
                string.Equals(name, "LocalRotZ10", StringComparison.Ordinal))
            {
                return true;
            }

            return true;
        }

        private static bool TryApplyParkingTruckDoor(string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var door = UnityEngine.Object.FindObjectOfType<ParkingLotTruckDoor>();
            if (door == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { door.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "BagPlaced", StringComparison.Ordinal)) { door.bagPlaced = value != 0; return true; }
            return true;
        }

        private static bool TryApplySuitcase(string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var suitcase = UnityEngine.Object.FindObjectOfType<Suitcase>();
            if (suitcase == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { suitcase.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "IsInside", StringComparison.Ordinal)) { suitcase.isInside = value != 0; return true; }
            if (string.Equals(name, "InTruck", StringComparison.Ordinal)) { suitcase.inTruck = value != 0; return true; }
            if (string.Equals(name, "ConversationTriggered", StringComparison.Ordinal)) { suitcase.conversationTriggered = value != 0; return true; }
            if (string.Equals(name, "TeleportingBagNextToMike", StringComparison.Ordinal)) { suitcase.teleportingBagNextToMike = value != 0; return true; }
            if (string.Equals(name, "TeleportedBag", StringComparison.Ordinal)) { suitcase.teleportedBag = value != 0; return true; }
            if (string.Equals(name, "Layer", StringComparison.Ordinal)) { suitcase.gameObject.layer = value; return true; }
            if (string.Equals(name, "Kinematic", StringComparison.Ordinal))
            {
                var body = suitcase.GetComponent<Rigidbody>();
                if (body != null) body.isKinematic = value != 0;
                return true;
            }

            return true;
        }

        private static bool TryApplyUi(string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var ui = UnityEngine.Object.FindObjectOfType<ParkingLotUIManager>();
            if (ui == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { ui.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "InConversation", StringComparison.Ordinal)) { ui.inCoversation = value != 0; return true; }
            if (string.Equals(name, "IntroCanvas", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(ui, "introCanvas"), value != 0); return true; }
            if (string.Equals(name, "WhiteIntroActive", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(ui, "whiteIntroManager"), value != 0); return true; }
            if (string.Equals(name, "WhiteIntroTextCount", StringComparison.Ordinal)) { return true; }
            if (string.Equals(name, "WhiteIntroTextMask", StringComparison.Ordinal)) { ApplyTextBehaviourEnabledMask(GetFieldObject(ui, "whiteIntroManager"), "textsToShow", value); return true; }
            if (string.Equals(name, "DialogueCamera", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(ui, "dialogueCamera"), value != 0); return true; }
            if (string.Equals(name, "NotifWasOnscreen", StringComparison.Ordinal)) { SetFieldValue(ui, "notifWasOnscreen", value != 0); return true; }
            if (string.Equals(name, "PhoneAllowed", StringComparison.Ordinal)) { if (ui.phoneUI != null) ui.phoneUI.allowPhone = value != 0; return true; }
            if (string.Equals(name, "PhonePaused", StringComparison.Ordinal)) { return true; }
            if (string.Equals(name, "FadeCanvas", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(ui, "fadeCanvas"), value != 0); return true; }
            return true;
        }

        private static bool TryApplyElevator(string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var manager = UnityEngine.Object.FindObjectOfType<ParkingLotElevatorManager>();
            if (manager == null) return false;

            var door = GetFieldValue<ElevatorDoorManager>(manager, "elevatorDoorManager");
            var antiThrow = GetFieldValue<BoxCollider>(manager, "doorAntiThrowTrigger");
            var elevatorAudio = GetFieldValue<AudioSource>(manager, "elevatorAudioSource");
            var effectsAudio = GetFieldValue<AudioSource>(manager, "effectsAudioSource");
            var emission = GetFieldValue<Transform>(manager, "emissionPlanesTransform");
            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { manager.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Testing", StringComparison.Ordinal)) { SetFieldValue(manager, "isTesting", value != 0); return true; }
            if (string.Equals(name, "Travelling", StringComparison.Ordinal)) { SetFieldValue(manager, "travelling", value != 0); return true; }
            if (string.Equals(name, "TripTimeMs", StringComparison.Ordinal)) { SetFieldValue(manager, "tripTime", value / 1000f); return true; }
            if (string.Equals(name, "DoorObject", StringComparison.Ordinal)) { SetObjectActive(door, value != 0); return true; }
            if (string.Equals(name, "DoorOpen", StringComparison.Ordinal)) { if (door != null) SetFieldValue(door, "isOpen", value != 0); return true; }
            if (string.Equals(name, "DoorTransitioning", StringComparison.Ordinal)) { if (door != null) SetFieldValue(door, "isTransitioning", false); return true; }
            if (string.Equals(name, "AntiThrowEnabled", StringComparison.Ordinal)) { if (antiThrow != null) antiThrow.enabled = value != 0; return true; }
            if (string.Equals(name, "ElevatorAudio", StringComparison.Ordinal)) { ApplyAudioPlayback(elevatorAudio, value != 0); return true; }
            if (string.Equals(name, "EffectsAudio", StringComparison.Ordinal)) { ApplyAudioPlayback(effectsAudio, value != 0); return true; }
            if (emission != null && string.Equals(name, "EmissionLocalX", StringComparison.Ordinal)) { var p = emission.localPosition; p.x = value / 1000f; emission.localPosition = p; return true; }
            if (emission != null && string.Equals(name, "EmissionLocalY", StringComparison.Ordinal)) { var p = emission.localPosition; p.y = value / 1000f; emission.localPosition = p; return true; }
            if (emission != null && string.Equals(name, "EmissionLocalZ", StringComparison.Ordinal)) { var p = emission.localPosition; p.z = value / 1000f; emission.localPosition = p; return true; }
            if (string.Equals(name, "FloorTextActive", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(manager, "floorTMPText"), value != 0); return true; }
            return true;
        }

        private static bool TryApplyAntiThrow(string name, int value)
        {
            if (string.Equals(name, "Count", StringComparison.Ordinal)) return true;
            var index = ParseIndexedName(name, out var fieldName);
            if (index < 0) return true;
            var zones = UnityEngine.Object.FindObjectsOfType<ParkingLotAntiThrowZone>();
            SortByPath(zones);
            if (index >= zones.Length) return false;
            var zone = zones[index];
            if (zone == null) return false;
            var collider = zone.GetComponent<Collider>();
            if (string.Equals(fieldName, "RootActive", StringComparison.Ordinal)) { zone.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(fieldName, "Enabled", StringComparison.Ordinal)) { zone.enabled = value != 0; return true; }
            if (string.Equals(fieldName, "Triggered", StringComparison.Ordinal)) { zone.triggerd = value != 0; return true; }
            if (string.Equals(fieldName, "Collider", StringComparison.Ordinal)) { if (collider != null) collider.enabled = value != 0; return true; }
            return true;
        }

        private static bool TryApplyTriggerSub(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Count", StringComparison.Ordinal)) return true;
            var index = ParseIndexedName(name, out var fieldName);
            if (index < 0) return true;
            var triggers = UnityEngine.Object.FindObjectsOfType<OnTriggerSub>();
            SortByPath(triggers);
            if (index >= triggers.Length) return false;
            var trigger = triggers[index];
            if (trigger == null) return false;

            if (string.Equals(fieldName, "RootActive", StringComparison.Ordinal)) { trigger.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(fieldName, "Enabled", StringComparison.Ordinal)) { trigger.enabled = false; SuppressTriggerLog(logger, "OnTriggerSub", triggers.Length); return true; }
            if (string.Equals(fieldName, "OnlyOnce", StringComparison.Ordinal)) { trigger.onlyOnce = value != 0; return true; }
            if (string.Equals(fieldName, "Entered", StringComparison.Ordinal)) { SetFieldValue(trigger, "entered", value != 0); return true; }
            if (string.Equals(fieldName, "EnterObject", StringComparison.Ordinal)) { SetObjectActive(trigger.enterGameObject, value != 0); return true; }
            if (string.Equals(fieldName, "SubKeyHash", StringComparison.Ordinal)) return true;
            if (string.Equals(fieldName, "SubTimeMs", StringComparison.Ordinal)) { trigger.subTime = value / 1000f; return true; }
            return true;
        }

        private static bool TryApplyDisplaySub(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Count", StringComparison.Ordinal)) return true;
            var index = ParseIndexedName(name, out var fieldName);
            if (index < 0) return true;
            var triggers = UnityEngine.Object.FindObjectsOfType<OnTriggerDisplaySub>();
            SortByPath(triggers);
            if (index >= triggers.Length) return false;
            var trigger = triggers[index];
            if (trigger == null) return false;

            if (string.Equals(fieldName, "RootActive", StringComparison.Ordinal)) { trigger.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(fieldName, "Enabled", StringComparison.Ordinal)) { trigger.enabled = false; SuppressTriggerLog(logger, "OnTriggerDisplaySub", triggers.Length); return true; }
            if (string.Equals(fieldName, "OnlyOnce", StringComparison.Ordinal)) { trigger.onlyOnce = value != 0; return true; }
            if (string.Equals(fieldName, "Entered", StringComparison.Ordinal)) { SetFieldValue(trigger, "entered", value != 0); return true; }
            if (string.Equals(fieldName, "EnterObject", StringComparison.Ordinal)) { SetObjectActive(trigger.enterGameObject, value != 0); return true; }
            if (string.Equals(fieldName, "SubKeyHash", StringComparison.Ordinal)) return true;
            return true;
        }

        private static bool TryApplyGenericTrigger(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Count", StringComparison.Ordinal)) return true;
            var index = ParseIndexedName(name, out var fieldName);
            if (index < 0) return true;
            var triggers = UnityEngine.Object.FindObjectsOfType<OnTrigger>();
            SortByPath(triggers);
            if (index >= triggers.Length) return false;
            var trigger = triggers[index];
            if (trigger == null) return false;

            if (string.Equals(fieldName, "RootActive", StringComparison.Ordinal)) { trigger.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(fieldName, "Enabled", StringComparison.Ordinal)) { trigger.enabled = false; SuppressTriggerLog(logger, "OnTrigger", triggers.Length); return true; }
            if (string.Equals(fieldName, "OnlyOnce", StringComparison.Ordinal)) { trigger.onlyOnce = value != 0; return true; }
            if (string.Equals(fieldName, "Entered", StringComparison.Ordinal)) { SetFieldValue(trigger, "entered", value != 0); return true; }
            if (string.Equals(fieldName, "EnterObject", StringComparison.Ordinal)) { SetObjectActive(trigger.enterGameObject, value != 0); return true; }
            return true;
        }

        private static bool TryApplyEventTrigger(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Count", StringComparison.Ordinal)) return true;
            var index = ParseIndexedName(name, out var fieldName);
            if (index < 0) return true;
            var triggers = UnityEngine.Object.FindObjectsOfType<TriggerEventInvoker>();
            SortByPath(triggers);
            if (index >= triggers.Length) return false;
            var trigger = triggers[index];
            if (trigger == null) return false;

            if (string.Equals(fieldName, "RootActive", StringComparison.Ordinal)) { trigger.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(fieldName, "Enabled", StringComparison.Ordinal)) { trigger.enabled = false; SuppressTriggerLog(logger, "TriggerEventInvoker", triggers.Length); return true; }
            if (string.Equals(fieldName, "OnlyOnce", StringComparison.Ordinal)) { SetFieldValue(trigger, "onlyOnce", value != 0); return true; }
            if (string.Equals(fieldName, "Triggered", StringComparison.Ordinal)) { trigger.isTriggered = value != 0; return true; }
            if (string.Equals(fieldName, "EnterObject", StringComparison.Ordinal)) { SetObjectActive(trigger.enterGameObject, value != 0); return true; }
            return true;
        }

        private static void SuppressLocalBrains(ManualLogSource logger)
        {
            var stranger = UnityEngine.Object.FindObjectOfType<ElevatorStranger>();
            if (stranger != null)
            {
                stranger.enabled = false;
                var agent = stranger.GetComponent<NavMeshAgent>();
                if (agent != null && agent.enabled)
                {
                    agent.isStopped = true;
                    agent.enabled = false;
                }
            }

            var mike = UnityEngine.Object.FindObjectOfType<MikeParkingLot>();
            if (mike != null)
            {
                mike.enabled = false;
                var mikeAgent = mike.GetComponent<NavMeshAgent>();
                if (mikeAgent != null && mikeAgent.enabled)
                {
                    mikeAgent.isStopped = true;
                    mikeAgent.enabled = false;
                }
            }

            var truck = UnityEngine.Object.FindObjectOfType<MikeTruckInParking>();
            if (truck != null) truck.enabled = false;

            var car = UnityEngine.Object.FindObjectOfType<StrangerCar>();
            if (car != null) car.enabled = false;

            var walkingCop = UnityEngine.Object.FindObjectOfType<WalkingCopController>();
            if (walkingCop != null)
            {
                walkingCop.enabled = false;
                var copAgent = GetFieldValue<NavMeshAgent>(walkingCop, "navmeshAgent") ?? walkingCop.GetComponent<NavMeshAgent>();
                if (copAgent != null && copAgent.enabled)
                {
                    copAgent.isStopped = true;
                    copAgent.enabled = false;
                }
            }

            var triggerSubCount = DisableTriggers(UnityEngine.Object.FindObjectsOfType<OnTriggerSub>());
            var displaySubCount = DisableTriggers(UnityEngine.Object.FindObjectsOfType<OnTriggerDisplaySub>());
            var triggerCount = DisableTriggers(UnityEngine.Object.FindObjectsOfType<OnTrigger>());
            var eventTriggerCount = DisableTriggers(UnityEngine.Object.FindObjectsOfType<TriggerEventInvoker>());

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (logger != null && nowMs >= _nextSuppressLogMs)
            {
                _nextSuppressLogMs = nowMs + 10000;
                logger.LogInfo("ParkingLot scene-state client brain suppressed stranger=" + (stranger != null) +
                               " mike=" + (mike != null) +
                               " walkingCop=" + (walkingCop != null) +
                               " truck=" + (truck != null) +
                               " car=" + (car != null) +
                               " triggers=" + (triggerSubCount + displaySubCount + triggerCount + eventTriggerCount));
            }
        }

        private static int DisableTriggers<T>(T[] triggers) where T : Behaviour
        {
            if (triggers == null) return 0;
            var count = 0;
            for (var i = 0; i < triggers.Length; i++)
            {
                var trigger = triggers[i];
                if (trigger == null) continue;
                trigger.enabled = false;
                count++;
            }

            return count;
        }

        private static void ApplyTruckDoorRotation(TruckBackDoor door)
        {
            if (door == null) return;
            door.transform.localRotation = door.doorOpen
                ? Quaternion.Euler(-202.096f, 0f, -90f)
                : Quaternion.Euler(-90f, 0f, -90f);
        }

        private static void AddTransform(List<Transform> transforms, Transform transform)
        {
            if (transforms == null || transform == null || !transform.gameObject.activeInHierarchy) return;
            transforms.Add(transform);
        }

        private static int ParseIndexedName(string name, out string fieldName)
        {
            fieldName = string.Empty;
            if (string.IsNullOrEmpty(name)) return -1;

            var dot = name.IndexOf('.');
            if (dot <= 0 || dot >= name.Length - 1) return -1;

            if (!int.TryParse(name.Substring(0, dot), out var index)) return -1;
            fieldName = name.Substring(dot + 1);
            return index;
        }

        private static void SortByPath<T>(T[] items) where T : Component
        {
            if (items == null || items.Length <= 1) return;
            Array.Sort(items, (a, b) =>
            {
                var left = a != null ? NetPath.GetPath(a.transform) : string.Empty;
                var right = b != null ? NetPath.GetPath(b.transform) : string.Empty;
                return string.CompareOrdinal(left, right);
            });
        }

        private static int StableStringHash(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            unchecked
            {
                var hash = 23;
                for (var i = 0; i < value.Length; i++)
                {
                    hash = (hash * 31) + value[i];
                }

                return hash;
            }
        }

        private static void SuppressTriggerLog(ManualLogSource logger, string typeName, int count)
        {
            if (logger == null) return;
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (nowMs < _nextSuppressLogMs) return;
            _nextSuppressLogMs = nowMs + 10000;
            logger.LogInfo("ParkingLot scene-state client trigger suppressed type=" + typeName + " count=" + count);
        }

        private static void ApplyAudioPlayback(AudioSource audio, bool playing)
        {
            if (audio == null) return;
            if (playing)
            {
                if (!audio.isPlaying) audio.Play();
            }
            else if (audio.isPlaying)
            {
                audio.Stop();
            }
        }

        private static bool IsObjectActive(object target)
        {
            var go = GetGameObject(target);
            return go != null && go.activeSelf;
        }

        private static void SetObjectActive(object target, bool active)
        {
            var go = GetGameObject(target);
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }

        private static GameObject GetGameObject(object target)
        {
            if (target is GameObject go) return go;
            if (target is Component component) return component.gameObject;
            return null;
        }

        private static object GetFieldObject(object target, string name)
        {
            return GetFieldValue<object>(target, name);
        }

        private static int GetEnumFieldInt(object target, string name)
        {
            var field = GetField(target, name);
            if (field == null) return 0;

            try
            {
                var value = field.GetValue(target);
                return value != null ? Convert.ToInt32(value) : 0;
            }
            catch
            {
                return 0;
            }
        }

        private static void SetEnumFieldInt(object target, string name, int value)
        {
            var field = GetField(target, name);
            if (field == null) return;

            try
            {
                if (field.FieldType.IsEnum)
                {
                    field.SetValue(target, Enum.ToObject(field.FieldType, value));
                }
                else
                {
                    field.SetValue(target, value);
                }
            }
            catch
            {
            }
        }

        private static T GetFieldValue<T>(object target, string name)
        {
            var field = GetField(target, name);
            if (field == null) return default(T);
            var value = field.GetValue(target);
            if (value is T typed) return typed;
            return default(T);
        }

        private static void SetFieldValue(object target, string name, object value)
        {
            var field = GetField(target, name);
            if (field == null) return;

            try
            {
                field.SetValue(target, value);
            }
            catch
            {
            }
        }

        private static int GetTextBehaviourCount(object target, string fieldName)
        {
            var items = GetFieldValue<Array>(target, fieldName);
            return items != null ? items.Length : 0;
        }

        private static int BuildTextBehaviourEnabledMask(object target, string fieldName)
        {
            var items = GetFieldValue<Array>(target, fieldName);
            if (items == null) return 0;

            var mask = 0;
            for (var i = 0; i < items.Length && i < 30; i++)
            {
                if (items.GetValue(i) is Behaviour behaviour && behaviour.enabled)
                {
                    mask |= 1 << i;
                }
            }

            return mask;
        }

        private static void ApplyTextBehaviourEnabledMask(object target, string fieldName, int mask)
        {
            var items = GetFieldValue<Array>(target, fieldName);
            if (items == null) return;

            for (var i = 0; i < items.Length && i < 30; i++)
            {
                if (items.GetValue(i) is Behaviour behaviour)
                {
                    behaviour.enabled = (mask & (1 << i)) != 0;
                }
            }
        }

        private static FieldInfo GetField(object target, string name)
        {
            if (target == null || string.IsNullOrEmpty(name)) return null;
            var key = target.GetType().FullName + "." + name;
            if (FieldCache.TryGetValue(key, out var cached)) return cached;
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(name, FieldFlags | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    FieldCache[key] = field;
                    return field;
                }

                type = type.BaseType;
            }

            FieldCache[key] = null;
            return null;
        }

        private static void Emit(string key, int value, Action<string, int> emit, ref int hash)
        {
            emit(key, value);
            unchecked
            {
                hash = hash * 31 + (key != null ? key.GetHashCode() : 0);
                hash = hash * 31 + value;
            }
        }
    }
}
