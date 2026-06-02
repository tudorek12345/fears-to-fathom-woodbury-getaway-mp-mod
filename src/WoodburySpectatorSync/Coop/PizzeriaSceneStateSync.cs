using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.AI;

namespace WoodburySpectatorSync.Coop
{
    internal static class PizzeriaSceneStateSync
    {
        public const string KeyPrefix = "SceneState.";

        private const string DoorPrefix = KeyPrefix + "Door";
        private const string LightPrefix = KeyPrefix + "Light";
        private const string GamePrefix = KeyPrefix + "Game.";
        private const string PhonePrefix = KeyPrefix + "Phone.";
        private const string UiPrefix = KeyPrefix + "UI.";
        private const string PlayerPrefix = KeyPrefix + "Player.";
        private const string DrivingPrefix = KeyPrefix + "Driving.";
        private const string MikeDetailPrefix = KeyPrefix + "MikeDetail.";
        private const string ChairPrefix = KeyPrefix + "Chair.";
        private const string BoundaryPrefix = KeyPrefix + "Boundary.";
        private const string OutOfZonePrefix = KeyPrefix + "OutOfZone";
        private const string VendingPrefix = KeyPrefix + "Vending.";
        private const string ObjectPrefix = KeyPrefix + "Object.";
        private const string AudioPrefix = KeyPrefix + "Audio.";
        private const string RoadTripMusicPrefix = KeyPrefix + "RoadTripMusic.";
        private const string TvAudioProximityPrefix = KeyPrefix + "TVAudioProximity.";
        private const string TriggerSubPrefix = KeyPrefix + "TriggerSub.";
        private const string DisplaySubPrefix = KeyPrefix + "DisplaySub.";
        private const string GenericTriggerPrefix = KeyPrefix + "GenericTrigger.";
        private const string EventTriggerPrefix = KeyPrefix + "EventTrigger.";
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Dictionary<string, FieldInfo> FieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private static long _nextSuppressLogMs;
        private static long _nextHostMikeLogMs;
        private static long _nextClientMikeLogMs;
        private static string _lastHostMikeLogSignature = string.Empty;
        private static string _lastClientMikeLogSignature = string.Empty;

        private enum PizzeriaMikePhase
        {
            Unknown = 0,
            DrivingIntro = 1,
            ParkedOutside = 2,
            TableSitting = 3,
            Eating = 4,
            GetPizza = 5,
            TrashCan = 6,
            ReturningToCar = 7,
            WaitingInCar = 8
        }

        public static int EmitHostFlags(
            string fullPrefix,
            PizzeriaGameManager manager,
            Action<string, int> emit,
            ManualLogSource logger = null,
            Action<string> sessionLogWrite = null)
        {
            if (emit == null) return 0;

            var hash = 71;
            var driving = GetFieldValue<MikeDrivingInPizzeriaScene>(manager, "mikeDriving") ?? UnityEngine.Object.FindObjectOfType<MikeDrivingInPizzeriaScene>();
            var mike = ResolvePizzeriaMike(manager);
            EmitGame(fullPrefix + GamePrefix, manager, emit, ref hash);
            EmitPhone(fullPrefix + PhonePrefix, GetFieldValue<Phone>(manager, "phoneUI"), emit, ref hash);
            EmitUi(fullPrefix + UiPrefix, UnityEngine.Object.FindObjectOfType<PizzerriaUIManager>(), emit, ref hash);
            EmitPlayer(fullPrefix + PlayerPrefix, UnityEngine.Object.FindObjectOfType<PizzeriaPlayerController>(), emit, ref hash);
            EmitDriving(fullPrefix + DrivingPrefix, driving, emit, ref hash);
            EmitPizzeriaMike(fullPrefix + MikeDetailPrefix, manager, mike, driving, emit, ref hash, logger, sessionLogWrite);
            EmitChair(fullPrefix + ChairPrefix, manager != null ? manager.pizzeriaChair : UnityEngine.Object.FindObjectOfType<PizzeriaChair>(), emit, ref hash);
            EmitBoundaries(fullPrefix + BoundaryPrefix, UnityEngine.Object.FindObjectsOfType<BoundaryCollidersTogglePizzeria>(), emit, ref hash);
            EmitOutOfZones(fullPrefix + OutOfZonePrefix, UnityEngine.Object.FindObjectsOfType<OutOfPlayZonePizzeria>(), emit, ref hash);
            hash = hash * 31 + VendingMachineSceneSync.EmitHostFlags(fullPrefix + VendingPrefix, emit);
            EmitObjectArray(fullPrefix + ObjectPrefix + "TruckTriggerMask", manager != null ? manager.truckTriggers : null, emit, ref hash);
            EmitObject(fullPrefix + ObjectPrefix + "KeysUI.", manager != null ? manager.keysUI : null, emit, ref hash);
            EmitAudio(fullPrefix + AudioPrefix + "CashRegister.", manager != null ? manager.cashRegisterAS : null, emit, ref hash);
            EmitAudio(fullPrefix + AudioPrefix + "Burp1.", manager != null ? manager.burp1 : null, emit, ref hash);
            EmitAudio(fullPrefix + AudioPrefix + "Burp2.", manager != null ? manager.burp2 : null, emit, ref hash);
            EmitAudio(fullPrefix + AudioPrefix + "Keys.", manager != null ? manager.keysAS : null, emit, ref hash);
            EmitAudio(fullPrefix + AudioPrefix + "TruckStart.", manager != null ? manager.truckStart : null, emit, ref hash);
            EmitRoadTripMusic(fullPrefix + RoadTripMusicPrefix, UnityEngine.Object.FindObjectOfType<DontDestroyRoadTripMusic>(), emit, ref hash);
            EmitTvAudioProximity(fullPrefix + TvAudioProximityPrefix, UnityEngine.Object.FindObjectsOfType<PizzeriaTVAudioProximity>(), emit, ref hash);
            EmitTriggerSubs(fullPrefix + TriggerSubPrefix, UnityEngine.Object.FindObjectsOfType<OnTriggerSub>(), emit, ref hash);
            EmitDisplaySubs(fullPrefix + DisplaySubPrefix, UnityEngine.Object.FindObjectsOfType<OnTriggerDisplaySub>(), emit, ref hash);
            EmitGenericTriggers(fullPrefix + GenericTriggerPrefix, UnityEngine.Object.FindObjectsOfType<OnTrigger>(), emit, ref hash);
            EmitEventTriggers(fullPrefix + EventTriggerPrefix, UnityEngine.Object.FindObjectsOfType<TriggerEventInvoker>(), emit, ref hash);

            var doors = FindDoors();
            for (var i = 0; i < doors.Count; i++)
            {
                EmitDoor(fullPrefix + DoorPrefix + i + ".", doors[i], emit, ref hash);
            }

            var lights = FindLightToggles();
            for (var i = 0; i < lights.Count; i++)
            {
                EmitLightToggle(fullPrefix + LightPrefix + i + ".", lights[i], emit, ref hash);
            }

            return hash;
        }

        public static void CollectSyncedTransforms(List<Transform> transforms)
        {
            if (transforms == null) return;

            var doors = FindDoors();
            for (var i = 0; i < doors.Count; i++)
            {
                if (doors[i] != null && doors[i].gameObject.activeInHierarchy) transforms.Add(doors[i].transform);
            }

            VendingMachineSceneSync.CollectSyncedTransforms(transforms);

            var manager = UnityEngine.Object.FindObjectOfType<PizzeriaGameManager>();
            var driving = GetFieldValue<MikeDrivingInPizzeriaScene>(manager, "mikeDriving") ?? UnityEngine.Object.FindObjectOfType<MikeDrivingInPizzeriaScene>();
            AddTransform(driving, transforms);
            AddTransform(ResolvePizzeriaMike(manager), transforms);
        }

        public static bool TryApplyFlag(string fieldName, int value, ManualLogSource logger)
        {
            if (string.IsNullOrEmpty(fieldName) ||
                !fieldName.StartsWith(KeyPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            SuppressLocalBrains(logger);

            if (fieldName.StartsWith(GamePrefix, StringComparison.Ordinal))
            {
                return TryApplyGameFlag(
                    UnityEngine.Object.FindObjectOfType<PizzeriaGameManager>(),
                    fieldName.Substring(GamePrefix.Length),
                    value);
            }

            if (fieldName.StartsWith(PhonePrefix, StringComparison.Ordinal))
            {
                var manager = UnityEngine.Object.FindObjectOfType<PizzeriaGameManager>();
                return TryApplyPhoneFlag(GetFieldValue<Phone>(manager, "phoneUI"), fieldName.Substring(PhonePrefix.Length), value);
            }

            if (fieldName.StartsWith(UiPrefix, StringComparison.Ordinal))
            {
                return TryApplyUiFlag(
                    UnityEngine.Object.FindObjectOfType<PizzerriaUIManager>(),
                    fieldName.Substring(UiPrefix.Length),
                    value);
            }

            if (fieldName.StartsWith(PlayerPrefix, StringComparison.Ordinal))
            {
                return TryApplyPlayerFlag(
                    UnityEngine.Object.FindObjectOfType<PizzeriaPlayerController>(),
                    fieldName.Substring(PlayerPrefix.Length),
                    value);
            }

            if (fieldName.StartsWith(DrivingPrefix, StringComparison.Ordinal))
            {
                var manager = UnityEngine.Object.FindObjectOfType<PizzeriaGameManager>();
                return TryApplyDrivingFlag(
                    GetFieldValue<MikeDrivingInPizzeriaScene>(manager, "mikeDriving") ?? UnityEngine.Object.FindObjectOfType<MikeDrivingInPizzeriaScene>(),
                    fieldName.Substring(DrivingPrefix.Length),
                    value);
            }

            if (fieldName.StartsWith(MikeDetailPrefix, StringComparison.Ordinal))
            {
                var manager = UnityEngine.Object.FindObjectOfType<PizzeriaGameManager>();
                var driving = GetFieldValue<MikeDrivingInPizzeriaScene>(manager, "mikeDriving") ?? UnityEngine.Object.FindObjectOfType<MikeDrivingInPizzeriaScene>();
                return TryApplyPizzeriaMikeFlag(
                    manager,
                    ResolvePizzeriaMike(manager),
                    driving,
                    fieldName.Substring(MikeDetailPrefix.Length),
                    value,
                    logger);
            }

            if (fieldName.StartsWith(ChairPrefix, StringComparison.Ordinal))
            {
                var manager = UnityEngine.Object.FindObjectOfType<PizzeriaGameManager>();
                var chair = manager != null ? manager.pizzeriaChair : UnityEngine.Object.FindObjectOfType<PizzeriaChair>();
                return TryApplyChairFlag(chair, fieldName.Substring(ChairPrefix.Length), value);
            }

            if (fieldName.StartsWith(BoundaryPrefix, StringComparison.Ordinal))
            {
                return TryApplyBoundaryFlag(fieldName.Substring(BoundaryPrefix.Length), value);
            }

            if (fieldName.StartsWith(OutOfZonePrefix, StringComparison.Ordinal))
            {
                return TryApplyOutOfZoneFlag(fieldName.Substring(OutOfZonePrefix.Length), value);
            }

            if (fieldName.StartsWith(VendingPrefix, StringComparison.Ordinal))
            {
                return VendingMachineSceneSync.TryApplyFlag(fieldName.Substring(VendingPrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(ObjectPrefix + "TruckTriggerMask", StringComparison.Ordinal))
            {
                var manager = UnityEngine.Object.FindObjectOfType<PizzeriaGameManager>();
                if (manager == null) return false;
                ApplyObjectArrayMask(manager.truckTriggers, value);
                return true;
            }

            if (fieldName.StartsWith(ObjectPrefix + "KeysUI.", StringComparison.Ordinal))
            {
                var manager = UnityEngine.Object.FindObjectOfType<PizzeriaGameManager>();
                if (manager == null) return false;
                return TryApplyObjectFlag(manager.keysUI, fieldName.Substring((ObjectPrefix + "KeysUI.").Length), value);
            }

            if (fieldName.StartsWith(AudioPrefix, StringComparison.Ordinal))
            {
                return TryApplyAudioByKey(fieldName.Substring(AudioPrefix.Length), value);
            }

            if (fieldName.StartsWith(RoadTripMusicPrefix, StringComparison.Ordinal))
            {
                return TryApplyRoadTripMusicFlag(fieldName.Substring(RoadTripMusicPrefix.Length), value);
            }

            if (fieldName.StartsWith(TvAudioProximityPrefix, StringComparison.Ordinal))
            {
                return TryApplyTvAudioProximity(fieldName.Substring(TvAudioProximityPrefix.Length), value, logger);
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

            if (fieldName.StartsWith(DoorPrefix, StringComparison.Ordinal))
            {
                return TryApplyIndexedDoor(fieldName.Substring(DoorPrefix.Length), value);
            }

            if (fieldName.StartsWith(LightPrefix, StringComparison.Ordinal))
            {
                return TryApplyIndexedLight(fieldName.Substring(LightPrefix.Length), value);
            }

            return true;
        }

        private static void EmitGame(string prefix, PizzeriaGameManager manager, Action<string, int> emit, ref int hash)
        {
            if (manager == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", manager.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "CurrentPlayerState", (int)manager.currentPlayerState, emit, ref hash);
            Emit(prefix + "PlayerTalking", manager.playerTalking ? 1 : 0, emit, ref hash);
            Emit(prefix + "PhoneUIState", manager.phoneUIState ? 1 : 0, emit, ref hash);
            Emit(prefix + "AutoStartFirstConvo", GetFieldValue<bool>(manager, "autoStartFirstConvo") ? 1 : 0, emit, ref hash);
            Emit(prefix + "FirstConvoTimerMs", Mathf.RoundToInt(GetFieldValue<float>(manager, "firstConvoTimer") * 1000f), emit, ref hash);
            Emit(prefix + "CompletedFirstConvo", GetFieldValue<bool>(manager, "completedFirstConvo") ? 1 : 0, emit, ref hash);
            Emit(prefix + "PlayerGotPizza", manager.playerGotPizza ? 1 : 0, emit, ref hash);
            Emit(prefix + "CanBurp", manager.canBurp ? 1 : 0, emit, ref hash);
            Emit(prefix + "Burp", manager.burp, emit, ref hash);
            Emit(prefix + "PlayerCanSendMessage", GetFieldValue<bool>(manager, "playerCanSendMessage") ? 1 : 0, emit, ref hash);
            Emit(prefix + "WasHoldingSodaCan", GetFieldValue<bool>(manager, "wasHoldingSodaCan") ? 1 : 0, emit, ref hash);
            Emit(prefix + "FirstPersonControllerGO", IsObjectActive(GetFieldObject(manager, "firstPersonControllerGO")) ? 1 : 0, emit, ref hash);
            Emit(prefix + "TrashCanLayerSwitched", GetFieldValue<SwitchObjectLayer>(manager, "trashCanLayer") != null && GetFieldValue<SwitchObjectLayer>(manager, "trashCanLayer").switched ? 1 : 0, emit, ref hash);
            Emit(prefix + "TruckDoorLayerSwitched", GetFieldValue<SwitchObjectLayer>(manager, "truckDoorLayer") != null && GetFieldValue<SwitchObjectLayer>(manager, "truckDoorLayer").switched ? 1 : 0, emit, ref hash);
            Emit(prefix + "EditorStartMusic", GetFieldValue<AudioSource>(manager, "editorTestStartMusic") != null && GetFieldValue<AudioSource>(manager, "editorTestStartMusic").isPlaying ? 1 : 0, emit, ref hash);
        }

        private static void EmitChair(string prefix, PizzeriaChair chair, Action<string, int> emit, ref int hash)
        {
            if (chair == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", chair.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "LayerSwitched", chair.pizzeriaChairLayer != null && chair.pizzeriaChairLayer.switched ? 1 : 0, emit, ref hash);
            Emit(prefix + "Layer", chair.gameObject.layer, emit, ref hash);
        }

        private static void EmitPhone(string prefix, Phone phone, Action<string, int> emit, ref int hash)
        {
            if (phone == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", phone.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "CanvasActive", IsObjectActive(phone.phoneCanvas) ? 1 : 0, emit, ref hash);
            Emit(prefix + "IsPaused", phone.isPaused ? 1 : 0, emit, ref hash);
            Emit(prefix + "AllowPhone", phone.allowPhone ? 1 : 0, emit, ref hash);
            Emit(prefix + "NetworkStatus", (int)phone.networkStatus, emit, ref hash);
            Emit(prefix + "BarsActive", IsObjectActive(phone.networkBarsObj) ? 1 : 0, emit, ref hash);
            Emit(prefix + "NoServiceActive", IsObjectActive(phone.noServiceObj) ? 1 : 0, emit, ref hash);
        }

        private static void EmitUi(string prefix, PizzerriaUIManager ui, Action<string, int> emit, ref int hash)
        {
            if (ui == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", ui.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "InConversation", ui.inCoversation ? 1 : 0, emit, ref hash);
            Emit(prefix + "FadeCanvas", IsObjectActive(GetFieldObject(ui, "fadeCanvas")) ? 1 : 0, emit, ref hash);
            Emit(prefix + "WhiteIntroCanvas", IsObjectActive(GetFieldObject(ui, "whiteIntroCanvas")) ? 1 : 0, emit, ref hash);
            Emit(prefix + "DialogueCamera", IsObjectActive(GetFieldObject(ui, "dialogueCamera")) ? 1 : 0, emit, ref hash);
            Emit(prefix + "PhoneAllowed", ui.phoneUI != null && ui.phoneUI.allowPhone ? 1 : 0, emit, ref hash);
            Emit(prefix + "PhonePaused", ui.phoneUI != null && ui.phoneUI.isPaused ? 1 : 0, emit, ref hash);
            Emit(prefix + "PhoneCanvas", ui.phoneUI != null && IsObjectActive(ui.phoneUI.phoneCanvas) ? 1 : 0, emit, ref hash);
        }

        private static void EmitPlayer(string prefix, PizzeriaPlayerController player, Action<string, int> emit, ref int hash)
        {
            if (player == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", player.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "FirstPerson", IsObjectActive(GetFieldObject(player, "firstPersonController")) ? 1 : 0, emit, ref hash);
            Emit(prefix + "DrivingParent", IsObjectActive(player.playerDrivingParent) ? 1 : 0, emit, ref hash);
            Emit(prefix + "DrivingCam", IsObjectActive(player.playerDrivingCam) ? 1 : 0, emit, ref hash);
            Emit(prefix + "Sitdown", IsObjectActive(player.playerSitdownPizzeria) ? 1 : 0, emit, ref hash);
            Emit(prefix + "SittingCamera", player.sittingDownCamera != null && player.sittingDownCamera.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "PlayerSitting", player.playerSitting ? 1 : 0, emit, ref hash);
            Emit(prefix + "CanThrowItem", player.canThrowItem ? 1 : 0, emit, ref hash);
            Emit(prefix + "PizzaOnTable", IsObjectActive(player.pizzaOnTable) ? 1 : 0, emit, ref hash);
            Emit(prefix + "GuyLookingAtPlayer", player.guyLookingAtPlayer ? 1 : 0, emit, ref hash);
            Emit(prefix + "LookingAtGuy", player.lookingAtGuy ? 1 : 0, emit, ref hash);
            Emit(prefix + "FovZoomEnabled", player.fovZoomPizzeria != null && player.fovZoomPizzeria.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "FovZoomDisable", player.fovZoomPizzeria != null && player.fovZoomPizzeria.disableFov ? 1 : 0, emit, ref hash);
            Emit(prefix + "FovZoomDontZoom", player.fovZoomPizzeria != null && player.fovZoomPizzeria.dontZoom ? 1 : 0, emit, ref hash);
            Emit(prefix + "HandBrakeAudio", player.handBreakAS != null && player.handBreakAS.isPlaying ? 1 : 0, emit, ref hash);
            Emit(prefix + "TrashAudio", player.trashCan != null && player.trashCan.isPlaying ? 1 : 0, emit, ref hash);
            var drivingCam = GetFieldValue<DrivingCam>(player, "drivingCam");
            var fovZoom = GetFieldValue<FovZoom>(player, "fovZoom");
            var sittingCam = GetFieldValue<SittingCam>(player, "sittingCamPizzeria");
            var dialogueCamera = GetFieldValue<Camera>(player, "dialogueCamera");
            var mainCamera = GetFieldValue<Camera>(player, "mainCamera");
            var lookAtObject = GetFieldValue<Transform>(player, "lookAtObject");
            var lookHere = GetFieldValue<Transform>(player, "lookHere");
            Emit(prefix + "DrivingFreeze", drivingCam != null && drivingCam.FreezeCam ? 1 : 0, emit, ref hash);
            Emit(prefix + "GlobalFovEnabled", fovZoom != null && fovZoom.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "GlobalFovDisable", fovZoom != null && fovZoom.disableFov ? 1 : 0, emit, ref hash);
            Emit(prefix + "GlobalFovDontZoom", fovZoom != null && fovZoom.dontZoom ? 1 : 0, emit, ref hash);
            Emit(prefix + "SittingCamEnabled", sittingCam != null && sittingCam.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "DialogueCamera", dialogueCamera != null && dialogueCamera.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "ZoomIntoTransform", GetFieldValue<bool>(player, "zoomIntoTransform") ? 1 : 0, emit, ref hash);
            Emit(prefix + "ReturnToDefaultZoom", GetFieldValue<bool>(player, "returnToDefaultZoom") ? 1 : 0, emit, ref hash);
            Emit(prefix + "LookTargetHash", lookAtObject != null ? StableStringHash(NetPath.GetPath(lookAtObject)) : 0, emit, ref hash);
            Emit(prefix + "LookTargetKind", GetPizzeriaLookTargetKind(player, lookAtObject), emit, ref hash);
            Emit(prefix + "LookHereKind", GetPizzeriaLookHereKind(player, lookHere), emit, ref hash);
            Emit(prefix + "MainCameraFov10", mainCamera != null ? Mathf.RoundToInt(mainCamera.fieldOfView * 10f) : 0, emit, ref hash);
        }

        private static void EmitBoundaries(string prefix, BoundaryCollidersTogglePizzeria[] toggles, Action<string, int> emit, ref int hash)
        {
            SortByPath(toggles);
            Emit(prefix + "Count", toggles != null ? toggles.Length : 0, emit, ref hash);
            if (toggles == null) return;

            for (var i = 0; i < toggles.Length; i++)
            {
                var toggle = toggles[i];
                if (toggle == null) continue;
                var itemPrefix = prefix + i + ".";
                var colliders = GetFieldValue<List<GameObject>>(toggle, "boundaryColliders");
                Emit(itemPrefix + "RootActive", toggle.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Enabled", toggle.enabled ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "ColliderMask", BuildActiveMask(colliders), emit, ref hash);
            }
        }

        private static void EmitOutOfZones(string prefix, OutOfPlayZonePizzeria[] zones, Action<string, int> emit, ref int hash)
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
                Emit(itemPrefix + "OnlyOnce", zone.onlyOnce ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Entered", GetFieldValue<bool>(zone, "entered") ? 1 : 0, emit, ref hash);
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

        private static void EmitDoor(string prefix, PizzeriaDoor door, Action<string, int> emit, ref int hash)
        {
            if (door == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", door.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Open", door.isOpen ? 1 : 0, emit, ref hash);
            Emit(prefix + "Interactible", door.isInteractible ? 1 : 0, emit, ref hash);
            Emit(prefix + "PlayerIn", door.playerIn ? 1 : 0, emit, ref hash);
            Emit(prefix + "Yaw10", Mathf.RoundToInt(door.transform.localEulerAngles.y * 10f), emit, ref hash);
            Emit(prefix + "AudioPlaying", door.doorAS != null && door.doorAS.isPlaying ? 1 : 0, emit, ref hash);
        }

        private static void EmitLightToggle(string prefix, ToggleLightsPizzeria toggle, Action<string, int> emit, ref int hash)
        {
            if (toggle == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            var lights = GetFieldValue<Light[]>(toggle, "lightsToToggle");
            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", toggle.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Enabled", toggle.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "Entered", GetFieldValue<bool>(toggle, "entered") ? 1 : 0, emit, ref hash);
            Emit(prefix + "EnabledMask", BuildLightEnabledMask(lights), emit, ref hash);
            Emit(prefix + "IntensitySum10", BuildIntensitySum10(lights), emit, ref hash);
        }

        private static void EmitObject(string prefix, object target, Action<string, int> emit, ref int hash)
        {
            var go = GetGameObject(target);
            if (go == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "Active", go.activeSelf ? 1 : 0, emit, ref hash);
        }

        private static void EmitDriving(string prefix, MikeDrivingInPizzeriaScene driving, Action<string, int> emit, ref int hash)
        {
            if (driving == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", driving.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Enabled", driving.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "Speed100", Mathf.RoundToInt(driving.speed * 100f), emit, ref hash);
            Emit(prefix + "InitialDistance100", Mathf.RoundToInt(GetFieldValue<float>(driving, "initialDistance") * 100f), emit, ref hash);
            Emit(prefix + "DistanceTravelled100", Mathf.RoundToInt(GetFieldValue<float>(driving, "distanceTravelled") * 100f), emit, ref hash);
            Emit(prefix + "PushBreak", GetFieldValue<bool>(driving, "pushBreak") ? 1 : 0, emit, ref hash);
            Emit(prefix + "AccelerateFromStop", GetFieldValue<bool>(driving, "accelerateFromStop") ? 1 : 0, emit, ref hash);
            Emit(prefix + "DialogueBreak", GetFieldValue<bool>(driving, "dialogueBreak") ? 1 : 0, emit, ref hash);
            Emit(prefix + "StartSlowDown", GetFieldValue<bool>(driving, "startSlowDown") ? 1 : 0, emit, ref hash);
            Emit(prefix + "SlowTimerMs", Mathf.RoundToInt(GetFieldValue<float>(driving, "slowTimer") * 1000f), emit, ref hash);
            Emit(prefix + "HeadLightsMask", BuildActiveMask(driving.headLights), emit, ref hash);
            Emit(prefix + "PanelActive", IsObjectActive(driving.panel) ? 1 : 0, emit, ref hash);
            Emit(prefix + "BobbleSpeed1000", driving.bobbleHead != null ? Mathf.RoundToInt(driving.bobbleHead.speed * 1000f) : 0, emit, ref hash);
            EmitAudio(prefix + "Engine.", GetFieldValue<AudioSource>(driving, "engineSound"), emit, ref hash);
            EmitAudio(prefix + "Key.", driving.keyAS, emit, ref hash);
            EmitAudio(prefix + "HandBreak.", driving.handBreakAS, emit, ref hash);
        }

        private static void EmitPizzeriaMike(
            string prefix,
            PizzeriaGameManager manager,
            MikePizzeria mike,
            MikeDrivingInPizzeriaScene driving,
            Action<string, int> emit,
            ref int hash,
            ManualLogSource logger,
            Action<string> sessionLogWrite)
        {
            if (mike == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            var phase = ResolvePizzeriaMikePhase(manager, mike, driving);
            var animator = mike.animator ?? mike.GetComponentInChildren<Animator>(true);
            var nav = mike.navMeshAgent ?? mike.GetComponentInChildren<NavMeshAgent>(true);

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "Phase", (int)phase, emit, ref hash);
            Emit(prefix + "RootActive", mike.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "ActiveInHierarchy", mike.gameObject.activeInHierarchy ? 1 : 0, emit, ref hash);
            Emit(prefix + "Layer", mike.gameObject.layer, emit, ref hash);
            Emit(prefix + "Visible", CountVisibleRenderers(mike.gameObject) > 0 ? 1 : 0, emit, ref hash);
            Emit(prefix + "RendererCount", CountRenderers(mike.gameObject), emit, ref hash);
            Emit(prefix + "VisibleRendererCount", CountVisibleRenderers(mike.gameObject), emit, ref hash);
            Emit(prefix + "ParentMode", ResolvePizzeriaMikeParentMode(mike), emit, ref hash);
            Emit(prefix + "State", (int)mike.state, emit, ref hash);
            Emit(prefix + "Moving", mike.moving ? 1 : 0, emit, ref hash);
            Emit(prefix + "GoGetPizza", GetFieldValue<bool>(mike, "goGetPizza") ? 1 : 0, emit, ref hash);
            Emit(prefix + "GoTrashCan", GetFieldValue<bool>(mike, "goTrashCan") ? 1 : 0, emit, ref hash);
            Emit(prefix + "PizzaInHand", GetFieldValue<bool>(mike, "pizzaInHand") ? 1 : 0, emit, ref hash);
            Emit(prefix + "EatingPizza", mike.eatingPizza ? 1 : 0, emit, ref hash);
            Emit(prefix + "MikeGotPizzaInHand", mike.mikeGotThePizzaInHand ? 1 : 0, emit, ref hash);
            Emit(prefix + "CapsuleEnabled", mike.capsuleCollider != null && mike.capsuleCollider.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "NavEnabled", nav != null && nav.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "NavStopped", nav != null && nav.enabled && nav.isStopped ? 1 : 0, emit, ref hash);
            Emit(prefix + "AnimatorEnabled", animator != null && animator.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "AnimatorSpeed1000", animator != null ? Mathf.RoundToInt(animator.speed * 1000f) : 0, emit, ref hash);
            Emit(prefix + "AnimatorParamState", animator != null ? GetAnimatorInt(animator, "State") : 0, emit, ref hash);
            Emit(prefix + "ArmRigPizzaWeight1000", Mathf.RoundToInt(GetRigWeight(GetFieldObject(mike, "armRigPizza")) * 1000f), emit, ref hash);
            Emit(prefix + "WorldX", Mathf.RoundToInt(mike.transform.position.x * 1000f), emit, ref hash);
            Emit(prefix + "WorldY", Mathf.RoundToInt(mike.transform.position.y * 1000f), emit, ref hash);
            Emit(prefix + "WorldZ", Mathf.RoundToInt(mike.transform.position.z * 1000f), emit, ref hash);
            Emit(prefix + "Yaw10", Mathf.RoundToInt(mike.transform.eulerAngles.y * 10f), emit, ref hash);
            Emit(prefix + "PizzaBoxActive", IsObjectActive(mike.pizzaBox) ? 1 : 0, emit, ref hash);
            Emit(prefix + "PizzaBoxOnTableActive", IsObjectActive(mike.pizzaBoxOnTable) ? 1 : 0, emit, ref hash);
            Emit(prefix + "PizzaBoxOnCounterActive", IsObjectActive(mike.pizzaBoxOnCounter) ? 1 : 0, emit, ref hash);
            Emit(prefix + "PizzaSliceActive", IsObjectActive(mike.pizzaSlice) ? 1 : 0, emit, ref hash);
            Emit(prefix + "PhoneActive", IsObjectActive(mike.phone) ? 1 : 0, emit, ref hash);
            Emit(prefix + "DoorColliderActive", IsObjectActive(mike.doorCollider) ? 1 : 0, emit, ref hash);
            Emit(prefix + "OrderConvoTriggerActive", IsObjectActive(mike.orderConvoTrigger) ? 1 : 0, emit, ref hash);

            MaybeLogPizzeriaMikeDiagnostics(
                "host",
                phase,
                manager,
                mike,
                driving,
                logger,
                sessionLogWrite);
        }

        private static void EmitObjectArray(string key, GameObject[] objects, Action<string, int> emit, ref int hash)
        {
            Emit(key, BuildActiveMask(objects), emit, ref hash);
        }

        private static void EmitAudio(string prefix, AudioSource audio, Action<string, int> emit, ref int hash)
        {
            if (audio == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "Playing", audio.isPlaying ? 1 : 0, emit, ref hash);
            Emit(prefix + "TimeMs", Mathf.Max(0, Mathf.RoundToInt(audio.time * 1000f)), emit, ref hash);
            Emit(prefix + "Volume1000", Mathf.RoundToInt(audio.volume * 1000f), emit, ref hash);
            Emit(prefix + "Enabled", audio.enabled ? 1 : 0, emit, ref hash);
        }

        private static void EmitRoadTripMusic(string prefix, DontDestroyRoadTripMusic music, Action<string, int> emit, ref int hash)
        {
            if (music == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", music.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Enabled", music.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "FadeStarted", GetFieldValue<bool>(music, "fadeStarted") ? 1 : 0, emit, ref hash);
            Emit(prefix + "PizzeriaBuildIndex", GetFieldValue<int>(music, "pizzeriaSceneBuildIndex"), emit, ref hash);
            EmitAudio(prefix + "Audio.", GetFieldValue<AudioSource>(music, "audioSource"), emit, ref hash);
        }

        private static void EmitTvAudioProximity(string prefix, PizzeriaTVAudioProximity[] proximities, Action<string, int> emit, ref int hash)
        {
            SortByPath(proximities);
            Emit(prefix + "Count", proximities != null ? proximities.Length : 0, emit, ref hash);
            if (proximities == null) return;

            for (var i = 0; i < proximities.Length; i++)
            {
                var proximity = proximities[i];
                if (proximity == null) continue;

                var audio = GetFieldValue<AudioSource>(proximity, "audioSource") ?? proximity.GetComponent<AudioSource>();
                var itemPrefix = prefix + i + ".";
                Emit(itemPrefix + "RootActive", proximity.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Enabled", proximity.enabled ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "PlayerInsideRange", proximity.playerInsideRange ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "InsidePizzeria", GetFieldValue<bool>(proximity, "insidePizziera") ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "AudioVolume1000", audio != null ? Mathf.RoundToInt(audio.volume * 1000f) : 0, emit, ref hash);
                Emit(itemPrefix + "AudioPlaying", audio != null && audio.isPlaying ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "MaxDistance100", Mathf.RoundToInt(proximity.maxDistance * 100f), emit, ref hash);
                Emit(itemPrefix + "MinDistance100", Mathf.RoundToInt(proximity.minDistance * 100f), emit, ref hash);
            }
        }

        private static bool TryApplyPhoneFlag(Phone phone, string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (phone == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { phone.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "CanvasActive", StringComparison.Ordinal)) { SetObjectActive(phone.phoneCanvas, value != 0); return true; }
            if (string.Equals(name, "IsPaused", StringComparison.Ordinal)) { phone.isPaused = value != 0; return true; }
            if (string.Equals(name, "AllowPhone", StringComparison.Ordinal)) { phone.allowPhone = value != 0; return true; }
            if (string.Equals(name, "NetworkStatus", StringComparison.Ordinal)) { phone.networkStatus = (Phone.NetworkStatus)value; return true; }
            if (string.Equals(name, "BarsActive", StringComparison.Ordinal)) { SetObjectActive(phone.networkBarsObj, value != 0); return true; }
            if (string.Equals(name, "NoServiceActive", StringComparison.Ordinal)) { SetObjectActive(phone.noServiceObj, value != 0); return true; }
            return true;
        }

        private static bool TryApplyGameFlag(PizzeriaGameManager manager, string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (manager == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { manager.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "CurrentPlayerState", StringComparison.Ordinal)) { manager.currentPlayerState = (PizzeriaGameManager.PlayerState)value; return true; }
            if (string.Equals(name, "PlayerTalking", StringComparison.Ordinal)) { manager.playerTalking = value != 0; return true; }
            if (string.Equals(name, "PhoneUIState", StringComparison.Ordinal)) { manager.phoneUIState = value != 0; return true; }
            if (string.Equals(name, "AutoStartFirstConvo", StringComparison.Ordinal)) { SetFieldValue(manager, "autoStartFirstConvo", value != 0); return true; }
            if (string.Equals(name, "FirstConvoTimerMs", StringComparison.Ordinal)) { SetFieldValue(manager, "firstConvoTimer", value / 1000f); return true; }
            if (string.Equals(name, "CompletedFirstConvo", StringComparison.Ordinal)) { SetFieldValue(manager, "completedFirstConvo", value != 0); return true; }
            if (string.Equals(name, "PlayerGotPizza", StringComparison.Ordinal)) { manager.playerGotPizza = value != 0; return true; }
            if (string.Equals(name, "CanBurp", StringComparison.Ordinal)) { manager.canBurp = value != 0; return true; }
            if (string.Equals(name, "Burp", StringComparison.Ordinal)) { manager.burp = value; return true; }
            if (string.Equals(name, "PlayerCanSendMessage", StringComparison.Ordinal)) { SetFieldValue(manager, "playerCanSendMessage", value != 0); return true; }
            if (string.Equals(name, "WasHoldingSodaCan", StringComparison.Ordinal)) { SetFieldValue(manager, "wasHoldingSodaCan", value != 0); return true; }
            if (string.Equals(name, "FirstPersonControllerGO", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(manager, "firstPersonControllerGO"), value != 0); return true; }
            if (string.Equals(name, "TrashCanLayerSwitched", StringComparison.Ordinal)) { ApplySwitchLayer(GetFieldValue<SwitchObjectLayer>(manager, "trashCanLayer"), value != 0); return true; }
            if (string.Equals(name, "TruckDoorLayerSwitched", StringComparison.Ordinal)) { ApplySwitchLayer(GetFieldValue<SwitchObjectLayer>(manager, "truckDoorLayer"), value != 0); return true; }
            if (string.Equals(name, "EditorStartMusic", StringComparison.Ordinal)) { ApplyAudioPlayback(GetFieldValue<AudioSource>(manager, "editorTestStartMusic"), value != 0); return true; }
            return true;
        }

        private static bool TryApplyUiFlag(PizzerriaUIManager ui, string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (ui == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { ui.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "InConversation", StringComparison.Ordinal)) { ui.inCoversation = value != 0; return true; }
            if (string.Equals(name, "FadeCanvas", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(ui, "fadeCanvas"), value != 0); return true; }
            if (string.Equals(name, "WhiteIntroCanvas", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(ui, "whiteIntroCanvas"), value != 0); return true; }
            if (string.Equals(name, "DialogueCamera", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(ui, "dialogueCamera"), value != 0); return true; }
            if (string.Equals(name, "PhoneAllowed", StringComparison.Ordinal)) { if (ui.phoneUI != null) ui.phoneUI.allowPhone = value != 0; return true; }
            if (string.Equals(name, "PhonePaused", StringComparison.Ordinal)) { if (ui.phoneUI != null) ui.phoneUI.isPaused = value != 0; return true; }
            if (string.Equals(name, "PhoneCanvas", StringComparison.Ordinal)) { if (ui.phoneUI != null) SetObjectActive(ui.phoneUI.phoneCanvas, value != 0); return true; }
            return true;
        }

        private static bool TryApplyPlayerFlag(PizzeriaPlayerController player, string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (player == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { player.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "FirstPerson", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(player, "firstPersonController"), value != 0); return true; }
            if (string.Equals(name, "DrivingParent", StringComparison.Ordinal)) { SetObjectActive(player.playerDrivingParent, value != 0); return true; }
            if (string.Equals(name, "DrivingCam", StringComparison.Ordinal)) { SetObjectActive(player.playerDrivingCam, value != 0); return true; }
            if (string.Equals(name, "Sitdown", StringComparison.Ordinal)) { SetObjectActive(player.playerSitdownPizzeria, value != 0); return true; }
            if (string.Equals(name, "SittingCamera", StringComparison.Ordinal)) { if (player.sittingDownCamera != null) player.sittingDownCamera.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "PlayerSitting", StringComparison.Ordinal)) { player.playerSitting = value != 0; return true; }
            if (string.Equals(name, "CanThrowItem", StringComparison.Ordinal)) { player.canThrowItem = value != 0; return true; }
            if (string.Equals(name, "PizzaOnTable", StringComparison.Ordinal)) { SetObjectActive(player.pizzaOnTable, value != 0); return true; }
            if (string.Equals(name, "GuyLookingAtPlayer", StringComparison.Ordinal)) { player.guyLookingAtPlayer = value != 0; return true; }
            if (string.Equals(name, "LookingAtGuy", StringComparison.Ordinal)) { player.lookingAtGuy = value != 0; return true; }
            if (string.Equals(name, "FovZoomEnabled", StringComparison.Ordinal)) { if (player.fovZoomPizzeria != null) player.fovZoomPizzeria.enabled = value != 0; return true; }
            if (string.Equals(name, "FovZoomDisable", StringComparison.Ordinal)) { if (player.fovZoomPizzeria != null) player.fovZoomPizzeria.disableFov = value != 0; return true; }
            if (string.Equals(name, "FovZoomDontZoom", StringComparison.Ordinal)) { if (player.fovZoomPizzeria != null) player.fovZoomPizzeria.dontZoom = value != 0; return true; }
            if (string.Equals(name, "HandBrakeAudio", StringComparison.Ordinal)) { ApplyAudioPlayback(player.handBreakAS, value != 0); return true; }
            if (string.Equals(name, "TrashAudio", StringComparison.Ordinal)) { ApplyAudioPlayback(player.trashCan, value != 0); return true; }
            var drivingCam = GetFieldValue<DrivingCam>(player, "drivingCam");
            var fovZoom = GetFieldValue<FovZoom>(player, "fovZoom");
            var sittingCam = GetFieldValue<SittingCam>(player, "sittingCamPizzeria");
            var dialogueCamera = GetFieldValue<Camera>(player, "dialogueCamera");
            var mainCamera = GetFieldValue<Camera>(player, "mainCamera");
            if (string.Equals(name, "DrivingFreeze", StringComparison.Ordinal)) { if (drivingCam != null) drivingCam.FreezeCam = value != 0; return true; }
            if (string.Equals(name, "GlobalFovEnabled", StringComparison.Ordinal)) { if (fovZoom != null) fovZoom.enabled = value != 0; return true; }
            if (string.Equals(name, "GlobalFovDisable", StringComparison.Ordinal)) { if (fovZoom != null) fovZoom.disableFov = value != 0; return true; }
            if (string.Equals(name, "GlobalFovDontZoom", StringComparison.Ordinal)) { if (fovZoom != null) fovZoom.dontZoom = value != 0; return true; }
            if (string.Equals(name, "SittingCamEnabled", StringComparison.Ordinal)) { if (sittingCam != null) sittingCam.enabled = value != 0; return true; }
            if (string.Equals(name, "DialogueCamera", StringComparison.Ordinal)) { if (dialogueCamera != null) dialogueCamera.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "ZoomIntoTransform", StringComparison.Ordinal)) { SetFieldValue(player, "zoomIntoTransform", value != 0); return true; }
            if (string.Equals(name, "ReturnToDefaultZoom", StringComparison.Ordinal)) { SetFieldValue(player, "returnToDefaultZoom", value != 0); return true; }
            if (string.Equals(name, "LookTargetHash", StringComparison.Ordinal)) return true;
            if (string.Equals(name, "LookTargetKind", StringComparison.Ordinal)) { SetPizzeriaLookTargetKind(player, value); return true; }
            if (string.Equals(name, "LookHereKind", StringComparison.Ordinal)) { SetPizzeriaLookHereKind(player, value); return true; }
            if (string.Equals(name, "MainCameraFov10", StringComparison.Ordinal)) { if (mainCamera != null) mainCamera.fieldOfView = value / 10f; return true; }
            return true;
        }

        private static int GetPizzeriaLookTargetKind(PizzeriaPlayerController player, Transform target)
        {
            if (player == null || target == null) return 0;
            if (ReferenceEquals(target, GetFieldValue<Transform>(player, "mikeTransform"))) return 1;
            if (ReferenceEquals(target, GetFieldValue<Transform>(player, "pizzeriaTransform"))) return 2;
            return StableStringHash(NetPath.GetPath(target));
        }

        private static void SetPizzeriaLookTargetKind(PizzeriaPlayerController player, int value)
        {
            if (player == null) return;

            Transform target = null;
            if (value == 1) target = GetFieldValue<Transform>(player, "mikeTransform");
            else if (value == 2) target = GetFieldValue<Transform>(player, "pizzeriaTransform");

            if (target != null || value == 0)
            {
                SetFieldValue(player, "lookAtObject", target);
            }
        }

        private static int GetPizzeriaLookHereKind(PizzeriaPlayerController player, Transform target)
        {
            if (player == null || target == null) return 0;
            if (ReferenceEquals(target, player.lookHereCashier)) return 1;
            if (ReferenceEquals(target, player.lookHereMike)) return 2;
            if (ReferenceEquals(target, player.lookHereHatGuy)) return 3;
            if (ReferenceEquals(target, player.lookHereGreyShirt)) return 4;
            if (ReferenceEquals(target, player.lookHereYoungMan)) return 5;
            if (ReferenceEquals(target, player.lookHereBlackWoman)) return 6;
            if (ReferenceEquals(target, player.lookHereHobo)) return 7;
            if (ReferenceEquals(target, player.lookHereHiker)) return 8;
            return StableStringHash(NetPath.GetPath(target));
        }

        private static void SetPizzeriaLookHereKind(PizzeriaPlayerController player, int value)
        {
            if (player == null) return;

            Transform target = null;
            if (value == 1) target = player.lookHereCashier;
            else if (value == 2) target = player.lookHereMike;
            else if (value == 3) target = player.lookHereHatGuy;
            else if (value == 4) target = player.lookHereGreyShirt;
            else if (value == 5) target = player.lookHereYoungMan;
            else if (value == 6) target = player.lookHereBlackWoman;
            else if (value == 7) target = player.lookHereHobo;
            else if (value == 8) target = player.lookHereHiker;

            if (target != null || value == 0)
            {
                player.lookHere = target;
            }
        }

        private static bool TryApplyDrivingFlag(MikeDrivingInPizzeriaScene driving, string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (driving == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { driving.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { driving.enabled = false; return true; }
            if (string.Equals(name, "Speed100", StringComparison.Ordinal)) { driving.speed = value / 100f; return true; }
            if (string.Equals(name, "InitialDistance100", StringComparison.Ordinal)) { SetFieldValue(driving, "initialDistance", value / 100f); return true; }
            if (string.Equals(name, "DistanceTravelled100", StringComparison.Ordinal)) { SetFieldValue(driving, "distanceTravelled", value / 100f); return true; }
            if (string.Equals(name, "PushBreak", StringComparison.Ordinal)) { SetFieldValue(driving, "pushBreak", value != 0); return true; }
            if (string.Equals(name, "AccelerateFromStop", StringComparison.Ordinal)) { SetFieldValue(driving, "accelerateFromStop", value != 0); return true; }
            if (string.Equals(name, "DialogueBreak", StringComparison.Ordinal)) { SetFieldValue(driving, "dialogueBreak", value != 0); return true; }
            if (string.Equals(name, "StartSlowDown", StringComparison.Ordinal)) { SetFieldValue(driving, "startSlowDown", value != 0); return true; }
            if (string.Equals(name, "SlowTimerMs", StringComparison.Ordinal)) { SetFieldValue(driving, "slowTimer", value / 1000f); return true; }
            if (string.Equals(name, "HeadLightsMask", StringComparison.Ordinal)) { ApplyObjectArrayMask(driving.headLights, value); return true; }
            if (string.Equals(name, "PanelActive", StringComparison.Ordinal)) { SetObjectActive(driving.panel, value != 0); return true; }
            if (string.Equals(name, "BobbleSpeed1000", StringComparison.Ordinal)) { if (driving.bobbleHead != null) driving.bobbleHead.speed = value / 1000f; return true; }
            if (name.StartsWith("Engine.", StringComparison.Ordinal)) return TryApplyAudioFlag(GetFieldValue<AudioSource>(driving, "engineSound"), name.Substring("Engine.".Length), value);
            if (name.StartsWith("Key.", StringComparison.Ordinal)) return TryApplyAudioFlag(driving.keyAS, name.Substring("Key.".Length), value);
            if (name.StartsWith("HandBreak.", StringComparison.Ordinal)) return TryApplyAudioFlag(driving.handBreakAS, name.Substring("HandBreak.".Length), value);
            return true;
        }

        private static bool TryApplyPizzeriaMikeFlag(
            PizzeriaGameManager manager,
            MikePizzeria mike,
            MikeDrivingInPizzeriaScene driving,
            string name,
            int value,
            ManualLogSource logger)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (mike == null) return false;

            if (string.Equals(name, "Phase", StringComparison.Ordinal))
            {
                ApplyPizzeriaMikePhase(manager, mike, driving, (PizzeriaMikePhase)value, logger);
                return true;
            }

            if (string.Equals(name, "RootActive", StringComparison.Ordinal))
            {
                if (value != 0 || !ShouldForcePizzeriaMikeVisible(manager, mike))
                {
                    mike.gameObject.SetActive(value != 0);
                }
                return true;
            }

            if (string.Equals(name, "ActiveInHierarchy", StringComparison.Ordinal) ||
                string.Equals(name, "RendererCount", StringComparison.Ordinal) ||
                string.Equals(name, "VisibleRendererCount", StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(name, "Layer", StringComparison.Ordinal)) { mike.gameObject.layer = value; return true; }
            if (string.Equals(name, "Visible", StringComparison.Ordinal)) { ApplyPizzeriaMikeVisibility(manager, mike, value != 0); return true; }
            if (string.Equals(name, "ParentMode", StringComparison.Ordinal)) { ApplyPizzeriaMikeParentMode(mike, value); return true; }
            if (string.Equals(name, "State", StringComparison.Ordinal)) { mike.state = (MikePizzeria.State)value; return true; }
            if (string.Equals(name, "Moving", StringComparison.Ordinal)) { mike.moving = value != 0; return true; }
            if (string.Equals(name, "GoGetPizza", StringComparison.Ordinal)) { SetFieldValue(mike, "goGetPizza", value != 0); return true; }
            if (string.Equals(name, "GoTrashCan", StringComparison.Ordinal)) { SetFieldValue(mike, "goTrashCan", value != 0); return true; }
            if (string.Equals(name, "PizzaInHand", StringComparison.Ordinal)) { SetFieldValue(mike, "pizzaInHand", value != 0); return true; }
            if (string.Equals(name, "EatingPizza", StringComparison.Ordinal)) { mike.eatingPizza = value != 0; return true; }
            if (string.Equals(name, "MikeGotPizzaInHand", StringComparison.Ordinal)) { mike.mikeGotThePizzaInHand = value != 0; return true; }
            if (string.Equals(name, "CapsuleEnabled", StringComparison.Ordinal)) { if (mike.capsuleCollider != null) mike.capsuleCollider.enabled = value != 0; return true; }
            if (string.Equals(name, "NavEnabled", StringComparison.Ordinal)) { if (mike.navMeshAgent != null) mike.navMeshAgent.enabled = value != 0; return true; }
            if (string.Equals(name, "NavStopped", StringComparison.Ordinal)) { if (mike.navMeshAgent != null && mike.navMeshAgent.enabled) mike.navMeshAgent.isStopped = value != 0; return true; }
            if (string.Equals(name, "AnimatorEnabled", StringComparison.Ordinal)) { if (mike.animator != null) mike.animator.enabled = true; return true; }
            if (string.Equals(name, "AnimatorSpeed1000", StringComparison.Ordinal)) { if (mike.animator != null) mike.animator.speed = Mathf.Max(0f, value / 1000f); return true; }
            if (string.Equals(name, "AnimatorParamState", StringComparison.Ordinal)) { SetAnimatorInt(mike.animator, "State", value); return true; }
            if (string.Equals(name, "ArmRigPizzaWeight1000", StringComparison.Ordinal)) { SetRigWeight(GetFieldObject(mike, "armRigPizza"), value / 1000f); return true; }
            if (string.Equals(name, "WorldX", StringComparison.Ordinal)) { SetAxisPosition(mike.transform, 0, value / 1000f); return true; }
            if (string.Equals(name, "WorldY", StringComparison.Ordinal)) { SetAxisPosition(mike.transform, 1, value / 1000f); return true; }
            if (string.Equals(name, "WorldZ", StringComparison.Ordinal)) { SetAxisPosition(mike.transform, 2, value / 1000f); return true; }
            if (string.Equals(name, "Yaw10", StringComparison.Ordinal)) { SetYaw(mike.transform, value / 10f); return true; }
            if (string.Equals(name, "PizzaBoxActive", StringComparison.Ordinal)) { SetObjectActive(mike.pizzaBox, value != 0); return true; }
            if (string.Equals(name, "PizzaBoxOnTableActive", StringComparison.Ordinal)) { SetObjectActive(mike.pizzaBoxOnTable, value != 0); return true; }
            if (string.Equals(name, "PizzaBoxOnCounterActive", StringComparison.Ordinal)) { SetObjectActive(mike.pizzaBoxOnCounter, value != 0); return true; }
            if (string.Equals(name, "PizzaSliceActive", StringComparison.Ordinal)) { SetObjectActive(mike.pizzaSlice, value != 0); return true; }
            if (string.Equals(name, "PhoneActive", StringComparison.Ordinal)) { SetObjectActive(mike.phone, value != 0); return true; }
            if (string.Equals(name, "DoorColliderActive", StringComparison.Ordinal)) { SetObjectActive(mike.doorCollider, value != 0); return true; }
            if (string.Equals(name, "OrderConvoTriggerActive", StringComparison.Ordinal)) { SetObjectActive(mike.orderConvoTrigger, value != 0); return true; }

            return true;
        }

        private static bool TryApplyChairFlag(PizzeriaChair chair, string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (chair == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { chair.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "LayerSwitched", StringComparison.Ordinal)) { ApplySwitchLayer(chair.pizzeriaChairLayer, value != 0); return true; }
            if (string.Equals(name, "Layer", StringComparison.Ordinal)) { return true; }
            return true;
        }

        private static bool TryApplyBoundaryFlag(string localKey, int value)
        {
            if (string.Equals(localKey, "Count", StringComparison.Ordinal)) return true;
            if (!TryParseIndexedKey(localKey, out var index, out var name)) return true;
            var toggles = UnityEngine.Object.FindObjectsOfType<BoundaryCollidersTogglePizzeria>();
            SortByPath(toggles);
            if (index < 0 || index >= toggles.Length) return false;
            var toggle = toggles[index];
            if (toggle == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { toggle.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { toggle.enabled = false; return true; }
            if (string.Equals(name, "ColliderMask", StringComparison.Ordinal)) { ApplyObjectListMask(GetFieldValue<List<GameObject>>(toggle, "boundaryColliders"), value); return true; }
            return true;
        }

        private static bool TryApplyOutOfZoneFlag(string localKey, int value)
        {
            if (string.Equals(localKey, "Count", StringComparison.Ordinal)) return true;
            if (!TryParseIndexedKey(localKey, out var index, out var name)) return true;
            var zones = UnityEngine.Object.FindObjectsOfType<OutOfPlayZonePizzeria>();
            SortByPath(zones);
            if (index < 0 || index >= zones.Length) return false;
            var zone = zones[index];
            if (zone == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { zone.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { zone.enabled = false; return true; }
            if (string.Equals(name, "OnlyOnce", StringComparison.Ordinal)) { zone.onlyOnce = value != 0; return true; }
            if (string.Equals(name, "Entered", StringComparison.Ordinal)) { SetFieldValue(zone, "entered", value != 0); return true; }
            return true;
        }

        private static bool TryApplyTriggerSub(string localKey, int value, ManualLogSource logger)
        {
            if (string.Equals(localKey, "Count", StringComparison.Ordinal)) return true;
            if (!TryParseIndexedKey(localKey, out var index, out var name)) return true;
            var triggers = UnityEngine.Object.FindObjectsOfType<OnTriggerSub>();
            SortByPath(triggers);
            if (index < 0 || index >= triggers.Length) return false;
            var trigger = triggers[index];
            if (trigger == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { trigger.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { trigger.enabled = false; SuppressTriggerLog(logger, "OnTriggerSub", triggers.Length); return true; }
            if (string.Equals(name, "OnlyOnce", StringComparison.Ordinal)) { trigger.onlyOnce = value != 0; return true; }
            if (string.Equals(name, "Entered", StringComparison.Ordinal)) { SetFieldValue(trigger, "entered", value != 0); return true; }
            if (string.Equals(name, "EnterObject", StringComparison.Ordinal)) { SetObjectActive(trigger.enterGameObject, value != 0); return true; }
            if (string.Equals(name, "SubKeyHash", StringComparison.Ordinal)) return true;
            if (string.Equals(name, "SubTimeMs", StringComparison.Ordinal)) { trigger.subTime = value / 1000f; return true; }
            return true;
        }

        private static bool TryApplyDisplaySub(string localKey, int value, ManualLogSource logger)
        {
            if (string.Equals(localKey, "Count", StringComparison.Ordinal)) return true;
            if (!TryParseIndexedKey(localKey, out var index, out var name)) return true;
            var triggers = UnityEngine.Object.FindObjectsOfType<OnTriggerDisplaySub>();
            SortByPath(triggers);
            if (index < 0 || index >= triggers.Length) return false;
            var trigger = triggers[index];
            if (trigger == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { trigger.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { trigger.enabled = false; SuppressTriggerLog(logger, "OnTriggerDisplaySub", triggers.Length); return true; }
            if (string.Equals(name, "OnlyOnce", StringComparison.Ordinal)) { trigger.onlyOnce = value != 0; return true; }
            if (string.Equals(name, "Entered", StringComparison.Ordinal)) { SetFieldValue(trigger, "entered", value != 0); return true; }
            if (string.Equals(name, "EnterObject", StringComparison.Ordinal)) { SetObjectActive(trigger.enterGameObject, value != 0); return true; }
            if (string.Equals(name, "SubKeyHash", StringComparison.Ordinal)) return true;
            return true;
        }

        private static bool TryApplyGenericTrigger(string localKey, int value, ManualLogSource logger)
        {
            if (string.Equals(localKey, "Count", StringComparison.Ordinal)) return true;
            if (!TryParseIndexedKey(localKey, out var index, out var name)) return true;
            var triggers = UnityEngine.Object.FindObjectsOfType<OnTrigger>();
            SortByPath(triggers);
            if (index < 0 || index >= triggers.Length) return false;
            var trigger = triggers[index];
            if (trigger == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { trigger.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { trigger.enabled = false; SuppressTriggerLog(logger, "OnTrigger", triggers.Length); return true; }
            if (string.Equals(name, "OnlyOnce", StringComparison.Ordinal)) { trigger.onlyOnce = value != 0; return true; }
            if (string.Equals(name, "Entered", StringComparison.Ordinal)) { SetFieldValue(trigger, "entered", value != 0); return true; }
            if (string.Equals(name, "EnterObject", StringComparison.Ordinal)) { SetObjectActive(trigger.enterGameObject, value != 0); return true; }
            return true;
        }

        private static bool TryApplyEventTrigger(string localKey, int value, ManualLogSource logger)
        {
            if (string.Equals(localKey, "Count", StringComparison.Ordinal)) return true;
            if (!TryParseIndexedKey(localKey, out var index, out var name)) return true;
            var triggers = UnityEngine.Object.FindObjectsOfType<TriggerEventInvoker>();
            SortByPath(triggers);
            if (index < 0 || index >= triggers.Length) return false;
            var trigger = triggers[index];
            if (trigger == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { trigger.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { trigger.enabled = false; SuppressTriggerLog(logger, "TriggerEventInvoker", triggers.Length); return true; }
            if (string.Equals(name, "OnlyOnce", StringComparison.Ordinal)) { SetFieldValue(trigger, "onlyOnce", value != 0); return true; }
            if (string.Equals(name, "Triggered", StringComparison.Ordinal)) { trigger.isTriggered = value != 0; return true; }
            if (string.Equals(name, "EnterObject", StringComparison.Ordinal)) { SetObjectActive(trigger.enterGameObject, value != 0); return true; }
            return true;
        }

        private static bool TryApplyIndexedDoor(string localKey, int value)
        {
            if (!TryParseIndexedKey(localKey, out var index, out var name)) return true;
            var doors = FindDoors();
            if (index < 0 || index >= doors.Count) return false;
            var door = doors[index];
            if (door == null) return false;

            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { door.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Open", StringComparison.Ordinal)) { door.isOpen = value != 0; ApplyDoorRotation(door); return true; }
            if (string.Equals(name, "Interactible", StringComparison.Ordinal)) { door.isInteractible = value != 0; return true; }
            if (string.Equals(name, "PlayerIn", StringComparison.Ordinal)) { door.playerIn = value != 0; return true; }
            if (string.Equals(name, "Yaw10", StringComparison.Ordinal)) { ApplyDoorYaw(door, value / 10f); return true; }
            if (string.Equals(name, "AudioPlaying", StringComparison.Ordinal)) { ApplyAudioPlayback(door.doorAS, value != 0); return true; }
            return true;
        }

        private static bool TryApplyIndexedLight(string localKey, int value)
        {
            if (!TryParseIndexedKey(localKey, out var index, out var name)) return true;
            var toggles = FindLightToggles();
            if (index < 0 || index >= toggles.Count) return false;
            var toggle = toggles[index];
            if (toggle == null) return false;

            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { toggle.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { toggle.enabled = false; return true; }
            if (string.Equals(name, "Entered", StringComparison.Ordinal)) { SetFieldValue(toggle, "entered", value != 0); return true; }
            if (string.Equals(name, "EnabledMask", StringComparison.Ordinal)) { ApplyLightEnabledMask(GetFieldValue<Light[]>(toggle, "lightsToToggle"), value); return true; }
            if (string.Equals(name, "IntensitySum10", StringComparison.Ordinal)) return true;
            return true;
        }

        private static bool TryApplyAudioByKey(string localKey, int value)
        {
            var manager = UnityEngine.Object.FindObjectOfType<PizzeriaGameManager>();
            if (manager == null) return false;

            if (localKey.StartsWith("CashRegister.", StringComparison.Ordinal)) return TryApplyAudioFlag(manager.cashRegisterAS, localKey.Substring(13), value);
            if (localKey.StartsWith("Burp1.", StringComparison.Ordinal)) return TryApplyAudioFlag(manager.burp1, localKey.Substring(6), value);
            if (localKey.StartsWith("Burp2.", StringComparison.Ordinal)) return TryApplyAudioFlag(manager.burp2, localKey.Substring(6), value);
            if (localKey.StartsWith("Keys.", StringComparison.Ordinal)) return TryApplyAudioFlag(manager.keysAS, localKey.Substring(5), value);
            if (localKey.StartsWith("TruckStart.", StringComparison.Ordinal)) return TryApplyAudioFlag(manager.truckStart, localKey.Substring(11), value);
            return true;
        }

        private static bool TryApplyObjectFlag(object target, string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var go = GetGameObject(target);
            if (go == null) return false;
            if (string.Equals(name, "Active", StringComparison.Ordinal)) { go.SetActive(value != 0); return true; }
            return true;
        }

        private static bool TryApplyAudioFlag(AudioSource audio, string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (audio == null) return false;
            if (string.Equals(name, "Playing", StringComparison.Ordinal)) { ApplyAudioPlayback(audio, value != 0); return true; }
            if (string.Equals(name, "TimeMs", StringComparison.Ordinal)) { audio.time = Mathf.Max(0f, value / 1000f); return true; }
            if (string.Equals(name, "Volume1000", StringComparison.Ordinal)) { audio.volume = Mathf.Clamp01(value / 1000f); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { audio.enabled = value != 0; return true; }
            return true;
        }

        private static bool TryApplyRoadTripMusicFlag(string name, int value)
        {
            var music = UnityEngine.Object.FindObjectOfType<DontDestroyRoadTripMusic>();
            if (string.Equals(name, "Exists", StringComparison.Ordinal))
            {
                if (value == 0 && music != null)
                {
                    music.gameObject.SetActive(false);
                    music.enabled = false;
                }

                return true;
            }

            if (music == null) return value == 0;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { music.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { music.enabled = false; return true; }
            if (string.Equals(name, "FadeStarted", StringComparison.Ordinal)) { SetFieldValue(music, "fadeStarted", value != 0); return true; }
            if (string.Equals(name, "PizzeriaBuildIndex", StringComparison.Ordinal)) { SetFieldValue(music, "pizzeriaSceneBuildIndex", value); return true; }
            if (name.StartsWith("Audio.", StringComparison.Ordinal)) return TryApplyAudioFlag(GetFieldValue<AudioSource>(music, "audioSource"), name.Substring("Audio.".Length), value);
            return true;
        }

        private static bool TryApplyTvAudioProximity(string localKey, int value, ManualLogSource logger)
        {
            if (string.Equals(localKey, "Count", StringComparison.Ordinal)) return true;
            if (!TryParseIndexedKey(localKey, out var index, out var name)) return true;

            var proximities = UnityEngine.Object.FindObjectsOfType<PizzeriaTVAudioProximity>();
            SortByPath(proximities);
            if (index < 0 || index >= proximities.Length) return false;

            var proximity = proximities[index];
            if (proximity == null) return false;

            var audio = GetFieldValue<AudioSource>(proximity, "audioSource") ?? proximity.GetComponent<AudioSource>();
            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { proximity.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { proximity.enabled = false; SuppressTriggerLog(logger, "PizzeriaTVAudioProximity", proximities.Length); return true; }
            if (string.Equals(name, "PlayerInsideRange", StringComparison.Ordinal)) { proximity.playerInsideRange = value != 0; return true; }
            if (string.Equals(name, "InsidePizzeria", StringComparison.Ordinal)) { SetFieldValue(proximity, "insidePizziera", value != 0); return true; }
            if (string.Equals(name, "AudioVolume1000", StringComparison.Ordinal)) { if (audio != null) audio.volume = Mathf.Clamp01(value / 1000f); return true; }
            if (string.Equals(name, "AudioPlaying", StringComparison.Ordinal)) { ApplyAudioPlayback(audio, value != 0); return true; }
            if (string.Equals(name, "MaxDistance100", StringComparison.Ordinal)) { proximity.maxDistance = value / 100f; return true; }
            if (string.Equals(name, "MinDistance100", StringComparison.Ordinal)) { proximity.minDistance = value / 100f; return true; }
            return true;
        }

        private static void SuppressLocalBrains(ManualLogSource logger)
        {
            var toggles = FindLightToggles();
            for (var i = 0; i < toggles.Count; i++)
            {
                if (toggles[i] != null) toggles[i].enabled = false;
            }

            var boundaries = UnityEngine.Object.FindObjectsOfType<BoundaryCollidersTogglePizzeria>();
            for (var i = 0; i < boundaries.Length; i++)
            {
                if (boundaries[i] != null) boundaries[i].enabled = false;
            }

            var outOfZones = UnityEngine.Object.FindObjectsOfType<OutOfPlayZonePizzeria>();
            for (var i = 0; i < outOfZones.Length; i++)
            {
                if (outOfZones[i] != null) outOfZones[i].enabled = false;
            }

            var tvAudioProximities = UnityEngine.Object.FindObjectsOfType<PizzeriaTVAudioProximity>();
            for (var i = 0; i < tvAudioProximities.Length; i++)
            {
                if (tvAudioProximities[i] != null) tvAudioProximities[i].enabled = false;
            }

            var driving = UnityEngine.Object.FindObjectOfType<MikeDrivingInPizzeriaScene>();
            if (driving != null)
            {
                driving.enabled = false;
            }

            var roadTripMusic = UnityEngine.Object.FindObjectOfType<DontDestroyRoadTripMusic>();
            if (roadTripMusic != null)
            {
                roadTripMusic.enabled = false;
            }

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (logger != null && nowMs >= _nextSuppressLogMs)
            {
                _nextSuppressLogMs = nowMs + 10000;
                logger.LogInfo("Pizzeria scene-state client brain suppressed lightToggles=" + toggles.Count +
                               " boundaries=" + boundaries.Length +
                               " outOfZones=" + outOfZones.Length +
                               " tvAudioProximity=" + tvAudioProximities.Length +
                               " driving=" + (driving != null ? 1 : 0) +
                               " roadTripMusic=" + (roadTripMusic != null ? 1 : 0));
            }
        }

        private static void SuppressTriggerLog(ManualLogSource logger, string typeName, int count)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (logger != null && nowMs >= _nextSuppressLogMs)
            {
                _nextSuppressLogMs = nowMs + 10000;
                logger.LogInfo("Pizzeria scene-state client brain suppressed " + typeName + " triggers=" + count);
            }
        }

        private static List<PizzeriaDoor> FindDoors()
        {
            var list = new List<PizzeriaDoor>(UnityEngine.Object.FindObjectsOfType<PizzeriaDoor>());
            SortByPath(list);
            return list;
        }

        private static List<ToggleLightsPizzeria> FindLightToggles()
        {
            var list = new List<ToggleLightsPizzeria>(UnityEngine.Object.FindObjectsOfType<ToggleLightsPizzeria>());
            SortByPath(list);
            return list;
        }

        private static void SortByPath<T>(List<T> list) where T : Component
        {
            list.Sort((left, right) => string.CompareOrdinal(
                left != null ? NetPath.GetPath(left.transform) : string.Empty,
                right != null ? NetPath.GetPath(right.transform) : string.Empty));
        }

        private static void SortByPath<T>(T[] array) where T : Component
        {
            if (array == null || array.Length <= 1) return;
            Array.Sort(array, (left, right) => string.CompareOrdinal(
                left != null ? NetPath.GetPath(left.transform) : string.Empty,
                right != null ? NetPath.GetPath(right.transform) : string.Empty));
        }

        private static bool TryParseIndexedKey(string value, out int index, out string name)
        {
            index = -1;
            name = string.Empty;
            if (string.IsNullOrEmpty(value) || !char.IsDigit(value[0])) return false;
            var dot = value.IndexOf('.');
            if (dot <= 0) return false;
            if (!int.TryParse(value.Substring(0, dot), out index)) return false;
            name = value.Substring(dot + 1);
            return true;
        }

        private static int BuildActiveMask(GameObject[] objects)
        {
            var mask = 0;
            if (objects == null) return mask;
            for (var i = 0; i < objects.Length && i < 30; i++)
            {
                if (objects[i] != null && objects[i].activeSelf) mask |= 1 << i;
            }

            return mask;
        }

        private static int BuildActiveMask(List<GameObject> objects)
        {
            var mask = 0;
            if (objects == null) return mask;
            for (var i = 0; i < objects.Count && i < 30; i++)
            {
                if (objects[i] != null && objects[i].activeSelf) mask |= 1 << i;
            }

            return mask;
        }

        private static void ApplyObjectArrayMask(GameObject[] objects, int mask)
        {
            if (objects == null) return;
            for (var i = 0; i < objects.Length && i < 30; i++)
            {
                if (objects[i] != null) objects[i].SetActive((mask & (1 << i)) != 0);
            }
        }

        private static void ApplyObjectListMask(List<GameObject> objects, int mask)
        {
            if (objects == null) return;
            for (var i = 0; i < objects.Count && i < 30; i++)
            {
                if (objects[i] != null) objects[i].SetActive((mask & (1 << i)) != 0);
            }
        }

        private static int BuildLightEnabledMask(Light[] lights)
        {
            var mask = 0;
            if (lights == null) return mask;
            for (var i = 0; i < lights.Length && i < 30; i++)
            {
                if (lights[i] != null && lights[i].enabled) mask |= 1 << i;
            }

            return mask;
        }

        private static int BuildIntensitySum10(Light[] lights)
        {
            var sum = 0f;
            if (lights != null)
            {
                for (var i = 0; i < lights.Length; i++)
                {
                    if (lights[i] != null) sum += lights[i].intensity;
                }
            }

            return Mathf.RoundToInt(sum * 10f);
        }

        private static void ApplyLightEnabledMask(Light[] lights, int mask)
        {
            if (lights == null) return;
            for (var i = 0; i < lights.Length && i < 30; i++)
            {
                if (lights[i] == null) continue;
                lights[i].enabled = (mask & (1 << i)) != 0;
            }
        }

        private static void ApplyDoorRotation(PizzeriaDoor door)
        {
            if (door == null) return;
            ApplyDoorYaw(door, door.isOpen ? 270f : 0f);
        }

        private static void ApplyDoorYaw(PizzeriaDoor door, float yaw)
        {
            if (door == null) return;
            door.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
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

        private static void ApplySwitchLayer(SwitchObjectLayer layer, bool switched)
        {
            if (layer == null) return;
            if (switched)
            {
                layer.SwitchToCustomLayer();
            }
            else
            {
                layer.SwitchToOriginalLayer();
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

        private static void AddTransform(object target, List<Transform> transforms)
        {
            if (transforms == null) return;
            var go = GetGameObject(target);
            if (go == null) return;
            var transform = go.transform;
            for (var i = 0; i < transforms.Count; i++)
            {
                if (transforms[i] == transform) return;
            }

            transforms.Add(transform);
        }

        private static object GetFieldObject(object target, string fieldName)
        {
            return GetFieldValue<object>(target, fieldName);
        }

        private static T GetFieldValue<T>(object target, string fieldName)
        {
            var field = GetField(target, fieldName);
            if (field == null) return default(T);
            try
            {
                var value = field.GetValue(target);
                if (value is T typed) return typed;
            }
            catch
            {
            }

            return default(T);
        }

        private static void SetFieldValue(object target, string fieldName, object value)
        {
            var field = GetField(target, fieldName);
            if (field == null) return;
            try
            {
                if (field.FieldType.IsEnum && value is int intValue)
                {
                    field.SetValue(target, Enum.ToObject(field.FieldType, intValue));
                    return;
                }

                field.SetValue(target, value);
            }
            catch
            {
            }
        }

        private static FieldInfo GetField(object target, string fieldName)
        {
            if (target == null || string.IsNullOrEmpty(fieldName)) return null;
            var key = target.GetType().FullName + "." + fieldName;
            if (FieldCache.TryGetValue(key, out var cached)) return cached;

            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName, FieldFlags | BindingFlags.DeclaredOnly);
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

        private static int StableStringHash(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                if (!string.IsNullOrEmpty(value))
                {
                    for (var i = 0; i < value.Length; i++)
                    {
                        hash ^= char.ToUpperInvariant(value[i]);
                        hash *= 16777619u;
                    }
                }

                return (int)hash;
            }
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
