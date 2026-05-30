using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace WoodburySpectatorSync.Coop
{
    internal static class RoadTripSceneStateSync
    {
        public const string KeyPrefix = "SceneState.";

        private const string RadioPrefix = KeyPrefix + "Radio.";
        private const string MikePrefix = KeyPrefix + "Mike.";
        private const string TruckPrefix = KeyPrefix + "Truck.";
        private const string UiPrefix = KeyPrefix + "UI.";
        private const string PlayerPrefix = KeyPrefix + "Player.";
        private const string ManagerPrefix = KeyPrefix + "Manager.";
        private const string BobblePrefix = KeyPrefix + "Bobble.";
        private const string ObjectPrefix = KeyPrefix + "Object.";
        private const string TriggerSubPrefix = KeyPrefix + "TriggerSub.";
        private const string DisplaySubPrefix = KeyPrefix + "DisplaySub.";
        private const string GenericTriggerPrefix = KeyPrefix + "GenericTrigger.";
        private const string EventTriggerPrefix = KeyPrefix + "EventTrigger.";
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Dictionary<string, FieldInfo> FieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private static long _nextSuppressLogMs;

        public static int EmitHostFlags(string fullPrefix, RoadTripGameManager manager, Action<string, int> emit)
        {
            if (manager == null || emit == null) return 0;

            var hash = 29;
            EmitRadio(fullPrefix + RadioPrefix, GetFieldValue<RadioMikeCar>(manager, "radioMikeCar"), emit, ref hash);
            EmitMike(fullPrefix + MikePrefix, GetFieldValue<MikeInCar>(manager, "mikeInCar"), emit, ref hash);
            EmitTruck(fullPrefix + TruckPrefix, GetFieldValue<MikeTruckInLoopScene>(manager, "mikeTruckInLoopScene"), emit, ref hash);
            EmitUi(fullPrefix + UiPrefix, UnityEngine.Object.FindObjectOfType<RoadTripUIManager>(), emit, ref hash);
            EmitPlayer(fullPrefix + PlayerPrefix, GetFieldValue<RoadTripPlayerController>(manager, "roadTripPlayerController"), emit, ref hash);
            EmitManager(fullPrefix + ManagerPrefix, manager, emit, ref hash);
            EmitBobble(fullPrefix + BobblePrefix, GetFieldValue<BobbleHeadMikeCar>(manager, "bobbleHeadMikeCar"), emit, ref hash);
            EmitObject(fullPrefix + ObjectPrefix + "schoolBus.", GetFieldObject(manager, "schoolBus"), emit, ref hash);
            EmitAudio(fullPrefix + ObjectPrefix + "horrorSound.", GetFieldValue<AudioSource>(manager, "horrorSound"), emit, ref hash);
            EmitTriggerSubs(fullPrefix + TriggerSubPrefix, UnityEngine.Object.FindObjectsOfType<OnTriggerSub>(), emit, ref hash);
            EmitDisplaySubs(fullPrefix + DisplaySubPrefix, UnityEngine.Object.FindObjectsOfType<OnTriggerDisplaySub>(), emit, ref hash);
            EmitGenericTriggers(fullPrefix + GenericTriggerPrefix, UnityEngine.Object.FindObjectsOfType<OnTrigger>(), emit, ref hash);
            EmitEventTriggers(fullPrefix + EventTriggerPrefix, UnityEngine.Object.FindObjectsOfType<TriggerEventInvoker>(), emit, ref hash);
            return hash;
        }

        public static void CollectSyncedTransforms(RoadTripGameManager manager, List<Transform> transforms)
        {
            if (manager == null || transforms == null) return;
            AddTransform(GetFieldObject(manager, "schoolBus"), transforms);
            var truck = GetFieldValue<MikeTruckInLoopScene>(manager, "mikeTruckInLoopScene");
            AddTransform(GetFieldObject(truck, "deer"), transforms);
            AddTransform(GetFieldObject(manager, "bobbleHeadMikeCar"), transforms);
        }

        public static bool TryApplyFlag(string fieldName, int value, ManualLogSource logger)
        {
            if (string.IsNullOrEmpty(fieldName) ||
                !fieldName.StartsWith(KeyPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            var manager = UnityEngine.Object.FindObjectOfType<RoadTripGameManager>();
            if (manager == null) return false;

            SuppressLocalRoadTripSceneBrains(manager, logger);

            if (fieldName.StartsWith(RadioPrefix, StringComparison.Ordinal))
            {
                return TryApplyRadioFlag(
                    GetFieldValue<RadioMikeCar>(manager, "radioMikeCar"),
                    fieldName.Substring(RadioPrefix.Length),
                    value);
            }

            if (fieldName.StartsWith(MikePrefix, StringComparison.Ordinal))
            {
                return TryApplyMikeFlag(
                    GetFieldValue<MikeInCar>(manager, "mikeInCar"),
                    fieldName.Substring(MikePrefix.Length),
                    value);
            }

            if (fieldName.StartsWith(TruckPrefix, StringComparison.Ordinal))
            {
                return TryApplyTruckFlag(
                    GetFieldValue<MikeTruckInLoopScene>(manager, "mikeTruckInLoopScene"),
                    fieldName.Substring(TruckPrefix.Length),
                    value);
            }

            if (fieldName.StartsWith(UiPrefix, StringComparison.Ordinal))
            {
                return TryApplyUiFlag(
                    UnityEngine.Object.FindObjectOfType<RoadTripUIManager>(),
                    fieldName.Substring(UiPrefix.Length),
                    value);
            }

            if (fieldName.StartsWith(PlayerPrefix, StringComparison.Ordinal))
            {
                return TryApplyPlayerFlag(
                    GetFieldValue<RoadTripPlayerController>(manager, "roadTripPlayerController"),
                    fieldName.Substring(PlayerPrefix.Length),
                    value);
            }

            if (fieldName.StartsWith(ManagerPrefix, StringComparison.Ordinal))
            {
                return TryApplyManagerFlag(
                    manager,
                    fieldName.Substring(ManagerPrefix.Length),
                    value);
            }

            if (fieldName.StartsWith(BobblePrefix, StringComparison.Ordinal))
            {
                return TryApplyBobbleFlag(
                    GetFieldValue<BobbleHeadMikeCar>(manager, "bobbleHeadMikeCar"),
                    fieldName.Substring(BobblePrefix.Length),
                    value);
            }

            if (fieldName.StartsWith(ObjectPrefix + "schoolBus.", StringComparison.Ordinal))
            {
                return TryApplyObjectFlag(
                    GetFieldObject(manager, "schoolBus"),
                    fieldName.Substring((ObjectPrefix + "schoolBus.").Length),
                    value);
            }

            if (fieldName.StartsWith(ObjectPrefix + "horrorSound.", StringComparison.Ordinal))
            {
                return TryApplyAudioFlag(
                    GetFieldValue<AudioSource>(manager, "horrorSound"),
                    fieldName.Substring((ObjectPrefix + "horrorSound.").Length),
                    value);
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

        private static void EmitRadio(string prefix, RadioMikeCar radio, Action<string, int> emit, ref int hash)
        {
            if (radio == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            var audio = GetFieldValue<AudioSource>(radio, "audioSource");
            var click = GetFieldValue<AudioSource>(radio, "radioClickSound");
            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", radio.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Enabled", radio.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "Playing", audio != null && audio.isPlaying ? 1 : 0, emit, ref hash);
            Emit(prefix + "TimeMs", audio != null ? Mathf.Max(0, Mathf.RoundToInt(audio.time * 1000f)) : 0, emit, ref hash);
            Emit(prefix + "ClipHash", audio != null ? StableObjectNameHash(audio.clip) : 0, emit, ref hash);
            Emit(prefix + "ClickPlaying", click != null && click.isPlaying ? 1 : 0, emit, ref hash);
        }

        private static void EmitMike(string prefix, MikeInCar mike, Action<string, int> emit, ref int hash)
        {
            if (mike == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", mike.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Enabled", mike.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "CurrentConvo", (int)mike.currentConvo, emit, ref hash);
            Emit(prefix + "InConversation", mike.mikeinConversation ? 1 : 0, emit, ref hash);
            Emit(prefix + "RadioIsOn", GetFieldValue<bool>(mike, "radioIsOn") ? 1 : 0, emit, ref hash);
            Emit(prefix + "FirstConvoComplete", GetFieldValue<bool>(mike, "firstConvoComplete") ? 1 : 0, emit, ref hash);
            Emit(prefix + "SnowConvoComplete", GetFieldValue<bool>(mike, "snowConvoComplete") ? 1 : 0, emit, ref hash);
            Emit(prefix + "BobbleConvoComplete", GetFieldValue<bool>(mike, "bobbleConvoComplete") ? 1 : 0, emit, ref hash);
            Emit(prefix + "RadioConvoComplete", GetFieldValue<bool>(mike, "radioConvoComplete") ? 1 : 0, emit, ref hash);
            Emit(prefix + "BusConvoComplete", mike.busConvoCompleted ? 1 : 0, emit, ref hash);
            Emit(prefix + "DeerConvoComplete", mike.deerConvoCompleted ? 1 : 0, emit, ref hash);
            Emit(prefix + "FinalConvoComplete", mike.finalConvoCompleted ? 1 : 0, emit, ref hash);
            Emit(prefix + "PassedBus", mike.passedBus ? 1 : 0, emit, ref hash);
            Emit(prefix + "PassedDeer", mike.passedDeer ? 1 : 0, emit, ref hash);
            Emit(prefix + "LookAtRoadMs", Mathf.RoundToInt(GetFieldValue<float>(mike, "lookAtRoadTimer") * 1000f), emit, ref hash);
            Emit(prefix + "LookAtPlayerMs", Mathf.RoundToInt(GetFieldValue<float>(mike, "lookAtPlayerTimer") * 1000f), emit, ref hash);
            Emit(prefix + "HeadRig100", Mathf.RoundToInt(GetRigWeight(GetFieldValue<Component>(mike, "headRig")) * 100f), emit, ref hash);
            Emit(prefix + "BusTrigger", IsObjectActive(GetFieldObject(mike, "busTriggerGO")) ? 1 : 0, emit, ref hash);
            Emit(prefix + "DeerTrigger", IsObjectActive(GetFieldObject(mike, "deerTriggerGO")) ? 1 : 0, emit, ref hash);
        }

        private static void EmitTruck(string prefix, MikeTruckInLoopScene truck, Action<string, int> emit, ref int hash)
        {
            if (truck == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", truck.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Enabled", truck.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "PushBreak", GetFieldValue<bool>(truck, "pushBreak") ? 1 : 0, emit, ref hash);
            Emit(prefix + "Accelerate", GetFieldValue<bool>(truck, "accelerateFromStop") ? 1 : 0, emit, ref hash);
            Emit(prefix + "DialogueBreak", GetFieldValue<bool>(truck, "dialogueBreak") ? 1 : 0, emit, ref hash);
            Emit(prefix + "DeerRun", GetFieldValue<bool>(truck, "run") ? 1 : 0, emit, ref hash);
            Emit(prefix + "SlowTimerMs", Mathf.RoundToInt(GetFieldValue<float>(truck, "slowTimer") * 1000f), emit, ref hash);
            Emit(prefix + "DeerSoundTimerMs", Mathf.RoundToInt(GetFieldValue<float>(truck, "deerSoundTimer") * 1000f), emit, ref hash);
            Emit(prefix + "DeerRunInMs", Mathf.RoundToInt(GetFieldValue<float>(truck, "deerRunIn") * 1000f), emit, ref hash);
            Emit(prefix + "DeerActive", IsObjectActive(GetFieldObject(truck, "deer")) ? 1 : 0, emit, ref hash);
            Emit(prefix + "DeerAnimState", GetAnimatorStateHash(GetFieldValue<Animator>(truck, "deerAnimator")), emit, ref hash);
            EmitAudio(prefix + "Horn.", GetFieldValue<AudioSource>(truck, "horn"), emit, ref hash);
            EmitAudio(prefix + "Rev.", GetFieldValue<AudioSource>(truck, "rev"), emit, ref hash);
            EmitAudio(prefix + "Engine.", GetFieldValue<AudioSource>(truck, "engineSound"), emit, ref hash);
            EmitAudio(prefix + "DeerSound.", GetFieldValue<AudioSource>(truck, "deerSound"), emit, ref hash);
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

        private static void EmitUi(string prefix, RoadTripUIManager ui, Action<string, int> emit, ref int hash)
        {
            if (ui == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            var phone = GetFieldValue<Phone>(ui, "phoneUI");
            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", ui.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "InConversation", ui.inCoversation ? 1 : 0, emit, ref hash);
            Emit(prefix + "FadeCanvas", IsObjectActive(GetFieldObject(ui, "fadeCanvas")) ? 1 : 0, emit, ref hash);
            Emit(prefix + "WhiteIntroCanvas", IsObjectActive(GetFieldObject(ui, "whiteIntroCanvas")) ? 1 : 0, emit, ref hash);
            Emit(prefix + "PhoneAllowed", phone != null && phone.allowPhone ? 1 : 0, emit, ref hash);
            Emit(prefix + "PhonePaused", phone != null && phone.isPaused ? 1 : 0, emit, ref hash);
            Emit(prefix + "PhoneCanvas", phone != null && IsObjectActive(phone.phoneCanvas) ? 1 : 0, emit, ref hash);
            Emit(prefix + "TransitionalMusic", ui.transitionalMusic != null && ui.transitionalMusic.isPlaying ? 1 : 0, emit, ref hash);
            Emit(prefix + "TransitionalMusicTimeMs", ui.transitionalMusic != null ? Mathf.RoundToInt(ui.transitionalMusic.time * 1000f) : 0, emit, ref hash);
        }

        private static void EmitPlayer(string prefix, RoadTripPlayerController player, Action<string, int> emit, ref int hash)
        {
            if (player == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            var drivingCam = GetFieldValue<DrivingCam>(player, "drivingCam");
            var fovZoom = GetFieldValue<FovZoom>(player, "fovZoom");
            var dialogueCamera = GetFieldObject(player, "dialogueCamera");
            var camera = GetFieldValue<Camera>(player, "camera");
            var lookAtObject = GetFieldValue<Transform>(player, "lookAtObject");
            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", player.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "DialogueCamera", IsObjectActive(dialogueCamera) ? 1 : 0, emit, ref hash);
            Emit(prefix + "ZoomIntoTransform", GetFieldValue<bool>(player, "zoomIntoTransform") ? 1 : 0, emit, ref hash);
            Emit(prefix + "ReturnToDefaultZoom", GetFieldValue<bool>(player, "returnToDefaultZoom") ? 1 : 0, emit, ref hash);
            Emit(prefix + "LookTargetKind", GetRoadTripLookTargetKind(player, lookAtObject), emit, ref hash);
            Emit(prefix + "CameraFov10", camera != null ? Mathf.RoundToInt(camera.fieldOfView * 10f) : 0, emit, ref hash);
            Emit(prefix + "DrivingFreeze", drivingCam != null && drivingCam.FreezeCam ? 1 : 0, emit, ref hash);
            Emit(prefix + "DrivingX10", drivingCam != null ? Mathf.RoundToInt(drivingCam.xRotation * 10f) : 0, emit, ref hash);
            Emit(prefix + "DrivingY10", drivingCam != null ? Mathf.RoundToInt(drivingCam.yRotation * 10f) : 0, emit, ref hash);
            Emit(prefix + "FovDisable", fovZoom != null && fovZoom.disableFov ? 1 : 0, emit, ref hash);
            Emit(prefix + "FovDontZoom", fovZoom != null && fovZoom.dontZoom ? 1 : 0, emit, ref hash);
        }

        private static void EmitManager(string prefix, RoadTripGameManager manager, Action<string, int> emit, ref int hash)
        {
            if (manager == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "PlayerTalking", manager.playerTalking ? 1 : 0, emit, ref hash);
            Emit(prefix + "AutoStartConversation", GetFieldValue<bool>(manager, "autoStartConversation") ? 1 : 0, emit, ref hash);
            Emit(prefix + "ConversationTimerMs", Mathf.RoundToInt(GetFieldValue<float>(manager, "conversationTimer") * 1000f), emit, ref hash);
            Emit(prefix + "ConvoCompleted", GetFieldValue<bool>(manager, "convoCompleted") ? 1 : 0, emit, ref hash);
            Emit(prefix + "PhoneUiState", GetFieldValue<bool>(manager, "phoneUIState") ? 1 : 0, emit, ref hash);
            Emit(prefix + "StartBump", GetFieldValue<bool>(manager, "startBump") ? 1 : 0, emit, ref hash);
            Emit(prefix + "RandomBumpTimerMs", Mathf.RoundToInt(GetFieldValue<float>(manager, "randomTimerForBump") * 1000f), emit, ref hash);
            Emit(prefix + "PlayerCanSendMessage", GetFieldValue<bool>(manager, "playerCanSendMessage") ? 1 : 0, emit, ref hash);
            Emit(prefix + "CarStartPoint10", Mathf.RoundToInt(manager.carStartPoint * 10f), emit, ref hash);
        }

        private static void EmitBobble(string prefix, BobbleHeadMikeCar bobble, Action<string, int> emit, ref int hash)
        {
            if (bobble == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            var layerSwitch = bobble.GetComponent<SwitchObjectLayer>();
            var animator = GetFieldValue<Animator>(bobble, "bobbleHeadAnimator");
            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", bobble.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Layer", bobble.gameObject.layer, emit, ref hash);
            Emit(prefix + "LayerSwitched", layerSwitch != null && layerSwitch.switched ? 1 : 0, emit, ref hash);
            Emit(prefix + "AnimatorEnabled", animator != null && animator.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "AnimatorState", GetAnimatorStateHash(animator), emit, ref hash);
            Emit(prefix + "AnimatorSpeed100", animator != null ? Mathf.RoundToInt(animator.speed * 100f) : 0, emit, ref hash);
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
            Emit(prefix + "ClipHash", StableObjectNameHash(audio.clip), emit, ref hash);
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
                Emit(itemPrefix + "SubKeyHash", StableObjectNameHash(trigger.subKey), emit, ref hash);
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
                Emit(itemPrefix + "SubKeyHash", StableObjectNameHash(GetFieldValue<string>(trigger, "subKey")), emit, ref hash);
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

        private static bool TryApplyRadioFlag(RadioMikeCar radio, string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (radio == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal))
            {
                radio.gameObject.SetActive(value != 0);
                return true;
            }

            radio.enabled = false;
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) return true;
            var audio = GetFieldValue<AudioSource>(radio, "audioSource");
            if (string.Equals(name, "Playing", StringComparison.Ordinal))
            {
                ApplyAudioPlayback(audio, value != 0);
                return true;
            }

            if (string.Equals(name, "TimeMs", StringComparison.Ordinal))
            {
                if (audio != null) audio.time = Mathf.Max(0f, value / 1000f);
                return true;
            }

            if (string.Equals(name, "ClipHash", StringComparison.Ordinal)) return true;

            var click = GetFieldValue<AudioSource>(radio, "radioClickSound");
            if (string.Equals(name, "ClickPlaying", StringComparison.Ordinal))
            {
                ApplyAudioPlayback(click, value != 0);
                return true;
            }

            return true;
        }

        private static bool TryApplyMikeFlag(MikeInCar mike, string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (mike == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal))
            {
                mike.gameObject.SetActive(value != 0);
                return true;
            }

            mike.enabled = false;
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) return true;
            if (string.Equals(name, "CurrentConvo", StringComparison.Ordinal)) { mike.currentConvo = (MikeInCar.MikeRoadTripConvoEnum)value; return true; }
            if (string.Equals(name, "InConversation", StringComparison.Ordinal)) { mike.mikeinConversation = value != 0; return true; }
            if (string.Equals(name, "RadioIsOn", StringComparison.Ordinal)) { SetFieldValue(mike, "radioIsOn", value != 0); return true; }
            if (string.Equals(name, "FirstConvoComplete", StringComparison.Ordinal)) { SetFieldValue(mike, "firstConvoComplete", value != 0); return true; }
            if (string.Equals(name, "SnowConvoComplete", StringComparison.Ordinal)) { SetFieldValue(mike, "snowConvoComplete", value != 0); return true; }
            if (string.Equals(name, "BobbleConvoComplete", StringComparison.Ordinal)) { SetFieldValue(mike, "bobbleConvoComplete", value != 0); return true; }
            if (string.Equals(name, "RadioConvoComplete", StringComparison.Ordinal)) { SetFieldValue(mike, "radioConvoComplete", value != 0); return true; }
            if (string.Equals(name, "BusConvoComplete", StringComparison.Ordinal)) { mike.busConvoCompleted = value != 0; return true; }
            if (string.Equals(name, "DeerConvoComplete", StringComparison.Ordinal)) { mike.deerConvoCompleted = value != 0; return true; }
            if (string.Equals(name, "FinalConvoComplete", StringComparison.Ordinal)) { mike.finalConvoCompleted = value != 0; return true; }
            if (string.Equals(name, "PassedBus", StringComparison.Ordinal)) { mike.passedBus = value != 0; return true; }
            if (string.Equals(name, "PassedDeer", StringComparison.Ordinal)) { mike.passedDeer = value != 0; return true; }
            if (string.Equals(name, "LookAtRoadMs", StringComparison.Ordinal)) { SetFieldValue(mike, "lookAtRoadTimer", value / 1000f); return true; }
            if (string.Equals(name, "LookAtPlayerMs", StringComparison.Ordinal)) { SetFieldValue(mike, "lookAtPlayerTimer", value / 1000f); return true; }
            if (string.Equals(name, "HeadRig100", StringComparison.Ordinal)) { SetRigWeight(GetFieldValue<Component>(mike, "headRig"), value / 100f); return true; }
            if (string.Equals(name, "BusTrigger", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(mike, "busTriggerGO"), value != 0); return true; }
            if (string.Equals(name, "DeerTrigger", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(mike, "deerTriggerGO"), value != 0); return true; }
            return true;
        }

        private static bool TryApplyTruckFlag(MikeTruckInLoopScene truck, string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (truck == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal))
            {
                truck.gameObject.SetActive(value != 0);
                return true;
            }

            truck.enabled = false;
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) return true;
            if (string.Equals(name, "PushBreak", StringComparison.Ordinal)) { SetFieldValue(truck, "pushBreak", value != 0); return true; }
            if (string.Equals(name, "Accelerate", StringComparison.Ordinal)) { SetFieldValue(truck, "accelerateFromStop", value != 0); return true; }
            if (string.Equals(name, "DialogueBreak", StringComparison.Ordinal)) { SetFieldValue(truck, "dialogueBreak", value != 0); return true; }
            if (string.Equals(name, "DeerRun", StringComparison.Ordinal)) { SetFieldValue(truck, "run", value != 0); return true; }
            if (string.Equals(name, "SlowTimerMs", StringComparison.Ordinal)) { SetFieldValue(truck, "slowTimer", value / 1000f); return true; }
            if (string.Equals(name, "DeerSoundTimerMs", StringComparison.Ordinal)) { SetFieldValue(truck, "deerSoundTimer", value / 1000f); return true; }
            if (string.Equals(name, "DeerRunInMs", StringComparison.Ordinal)) { SetFieldValue(truck, "deerRunIn", value / 1000f); return true; }
            if (string.Equals(name, "DeerActive", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(truck, "deer"), value != 0); return true; }
            if (string.Equals(name, "DeerAnimState", StringComparison.Ordinal)) { CrossFadeAnimator(GetFieldValue<Animator>(truck, "deerAnimator"), value); return true; }
            if (name.StartsWith("Horn.", StringComparison.Ordinal)) return TryApplyAudioFlag(GetFieldValue<AudioSource>(truck, "horn"), name.Substring(5), value);
            if (name.StartsWith("Rev.", StringComparison.Ordinal)) return TryApplyAudioFlag(GetFieldValue<AudioSource>(truck, "rev"), name.Substring(4), value);
            if (name.StartsWith("Engine.", StringComparison.Ordinal)) return TryApplyAudioFlag(GetFieldValue<AudioSource>(truck, "engineSound"), name.Substring(7), value);
            if (name.StartsWith("DeerSound.", StringComparison.Ordinal)) return TryApplyAudioFlag(GetFieldValue<AudioSource>(truck, "deerSound"), name.Substring(10), value);
            return true;
        }

        private static bool TryApplyUiFlag(RoadTripUIManager ui, string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (ui == null) return false;

            var phone = GetFieldValue<Phone>(ui, "phoneUI");
            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { ui.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "InConversation", StringComparison.Ordinal)) { ui.inCoversation = value != 0; return true; }
            if (string.Equals(name, "FadeCanvas", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(ui, "fadeCanvas"), value != 0); return true; }
            if (string.Equals(name, "WhiteIntroCanvas", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(ui, "whiteIntroCanvas"), value != 0); return true; }
            if (string.Equals(name, "PhoneAllowed", StringComparison.Ordinal)) { if (phone != null) phone.allowPhone = value != 0; return true; }
            if (string.Equals(name, "PhonePaused", StringComparison.Ordinal)) { if (phone != null) phone.isPaused = value != 0; return true; }
            if (string.Equals(name, "PhoneCanvas", StringComparison.Ordinal)) { if (phone != null) SetObjectActive(phone.phoneCanvas, value != 0); return true; }
            if (string.Equals(name, "TransitionalMusic", StringComparison.Ordinal)) { ApplyAudioPlayback(ui.transitionalMusic, value != 0); return true; }
            if (string.Equals(name, "TransitionalMusicTimeMs", StringComparison.Ordinal)) { if (ui.transitionalMusic != null) ui.transitionalMusic.time = Mathf.Max(0f, value / 1000f); return true; }
            return true;
        }

        private static bool TryApplyPlayerFlag(RoadTripPlayerController player, string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (player == null) return false;

            var drivingCam = GetFieldValue<DrivingCam>(player, "drivingCam");
            var fovZoom = GetFieldValue<FovZoom>(player, "fovZoom");
            var camera = GetFieldValue<Camera>(player, "camera");
            if (drivingCam != null) drivingCam.FreezeCam = true;
            if (fovZoom != null) fovZoom.disableFov = true;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { player.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "DialogueCamera", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(player, "dialogueCamera"), value != 0); return true; }
            if (string.Equals(name, "ZoomIntoTransform", StringComparison.Ordinal)) { SetFieldValue(player, "zoomIntoTransform", value != 0); return true; }
            if (string.Equals(name, "ReturnToDefaultZoom", StringComparison.Ordinal)) { SetFieldValue(player, "returnToDefaultZoom", value != 0); return true; }
            if (string.Equals(name, "LookTargetKind", StringComparison.Ordinal)) { SetRoadTripLookTargetKind(player, value); return true; }
            if (string.Equals(name, "CameraFov10", StringComparison.Ordinal)) { if (camera != null) camera.fieldOfView = Mathf.Max(1f, value / 10f); return true; }
            if (string.Equals(name, "DrivingFreeze", StringComparison.Ordinal)) { if (drivingCam != null) drivingCam.FreezeCam = true; return true; }
            if (string.Equals(name, "DrivingX10", StringComparison.Ordinal)) { if (drivingCam != null) drivingCam.xRotation = value / 10f; return true; }
            if (string.Equals(name, "DrivingY10", StringComparison.Ordinal)) { if (drivingCam != null) drivingCam.yRotation = value / 10f; return true; }
            if (string.Equals(name, "FovDisable", StringComparison.Ordinal)) { if (fovZoom != null) fovZoom.disableFov = true; return true; }
            if (string.Equals(name, "FovDontZoom", StringComparison.Ordinal)) { if (fovZoom != null) fovZoom.dontZoom = value != 0; return true; }
            return true;
        }

        private static int GetRoadTripLookTargetKind(RoadTripPlayerController player, Transform lookAtObject)
        {
            if (player == null || lookAtObject == null) return 0;
            if (ReferenceEquals(lookAtObject, GetFieldValue<Transform>(player, "mikeTransform"))) return 1;
            if (ReferenceEquals(lookAtObject, GetFieldValue<Transform>(player, "busTransform"))) return 2;
            if (ReferenceEquals(lookAtObject, GetFieldValue<Transform>(player, "deerTransform"))) return 3;
            return StableObjectNameHash(lookAtObject);
        }

        private static void SetRoadTripLookTargetKind(RoadTripPlayerController player, int value)
        {
            if (player == null) return;

            Transform target = null;
            if (value == 1) target = GetFieldValue<Transform>(player, "mikeTransform");
            else if (value == 2) target = GetFieldValue<Transform>(player, "busTransform");
            else if (value == 3) target = GetFieldValue<Transform>(player, "deerTransform");

            if (target != null || value == 0)
            {
                SetFieldValue(player, "lookAtObject", target);
            }
        }

        private static bool TryApplyManagerFlag(RoadTripGameManager manager, string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (manager == null) return false;

            if (string.Equals(name, "PlayerTalking", StringComparison.Ordinal)) { manager.playerTalking = value != 0; return true; }
            if (string.Equals(name, "AutoStartConversation", StringComparison.Ordinal)) { SetFieldValue(manager, "autoStartConversation", value != 0); return true; }
            if (string.Equals(name, "ConversationTimerMs", StringComparison.Ordinal)) { SetFieldValue(manager, "conversationTimer", value / 1000f); return true; }
            if (string.Equals(name, "ConvoCompleted", StringComparison.Ordinal)) { SetFieldValue(manager, "convoCompleted", value != 0); return true; }
            if (string.Equals(name, "PhoneUiState", StringComparison.Ordinal)) { SetFieldValue(manager, "phoneUIState", value != 0); return true; }
            if (string.Equals(name, "StartBump", StringComparison.Ordinal)) { SetFieldValue(manager, "startBump", value != 0); return true; }
            if (string.Equals(name, "RandomBumpTimerMs", StringComparison.Ordinal)) { SetFieldValue(manager, "randomTimerForBump", value / 1000f); return true; }
            if (string.Equals(name, "PlayerCanSendMessage", StringComparison.Ordinal)) { SetFieldValue(manager, "playerCanSendMessage", value != 0); return true; }
            if (string.Equals(name, "CarStartPoint10", StringComparison.Ordinal)) { manager.carStartPoint = value / 10f; return true; }
            return true;
        }

        private static bool TryApplyBobbleFlag(BobbleHeadMikeCar bobble, string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (bobble == null) return false;

            var layerSwitch = bobble.GetComponent<SwitchObjectLayer>();
            var animator = GetFieldValue<Animator>(bobble, "bobbleHeadAnimator");
            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { bobble.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Layer", StringComparison.Ordinal)) { bobble.gameObject.layer = value; return true; }
            if (string.Equals(name, "LayerSwitched", StringComparison.Ordinal))
            {
                if (layerSwitch != null)
                {
                    if (value != 0) layerSwitch.SwitchToCustomLayer();
                    else layerSwitch.SwitchToOriginalLayer();
                }
                return true;
            }
            if (string.Equals(name, "AnimatorEnabled", StringComparison.Ordinal)) { if (animator != null) animator.enabled = value != 0; return true; }
            if (string.Equals(name, "AnimatorState", StringComparison.Ordinal)) { CrossFadeAnimator(animator, value); return true; }
            if (string.Equals(name, "AnimatorSpeed100", StringComparison.Ordinal)) { if (animator != null) animator.speed = value / 100f; return true; }
            return true;
        }

        private static bool TryApplyObjectFlag(object target, string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var go = GetGameObject(target);
            if (go == null) return false;
            if (string.Equals(name, "Active", StringComparison.Ordinal))
            {
                go.SetActive(value != 0);
                return true;
            }

            return true;
        }

        private static bool TryApplyAudioFlag(AudioSource audio, string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (audio == null) return false;
            if (string.Equals(name, "Playing", StringComparison.Ordinal))
            {
                ApplyAudioPlayback(audio, value != 0);
                return true;
            }

            if (string.Equals(name, "TimeMs", StringComparison.Ordinal))
            {
                audio.time = Mathf.Max(0f, value / 1000f);
                return true;
            }

            if (string.Equals(name, "ClipHash", StringComparison.Ordinal)) return true;
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

        private static void SuppressLocalRoadTripSceneBrains(RoadTripGameManager manager, ManualLogSource logger)
        {
            var radio = GetFieldValue<RadioMikeCar>(manager, "radioMikeCar");
            if (radio != null) radio.enabled = false;
            var mike = GetFieldValue<MikeInCar>(manager, "mikeInCar");
            if (mike != null) mike.enabled = false;
            var truck = GetFieldValue<MikeTruckInLoopScene>(manager, "mikeTruckInLoopScene");
            if (truck != null) truck.enabled = false;
            var player = GetFieldValue<RoadTripPlayerController>(manager, "roadTripPlayerController");
            var drivingCam = GetFieldValue<DrivingCam>(player, "drivingCam");
            if (drivingCam != null) drivingCam.FreezeCam = true;
            var fovZoom = GetFieldValue<FovZoom>(player, "fovZoom");
            if (fovZoom != null) fovZoom.disableFov = true;
            var triggerSubCount = DisableBehaviours(UnityEngine.Object.FindObjectsOfType<OnTriggerSub>());
            var displaySubCount = DisableBehaviours(UnityEngine.Object.FindObjectsOfType<OnTriggerDisplaySub>());
            var triggerCount = DisableBehaviours(UnityEngine.Object.FindObjectsOfType<OnTrigger>());
            var eventTriggerCount = DisableBehaviours(UnityEngine.Object.FindObjectsOfType<TriggerEventInvoker>());

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (logger != null && nowMs >= _nextSuppressLogMs)
            {
                _nextSuppressLogMs = nowMs + 10000;
                logger.LogInfo("RoadTrip scene-state client brain suppressed radio=" + (radio != null) +
                               " mike=" + (mike != null) +
                               " truck=" + (truck != null) +
                               " playerCamera=" + (drivingCam != null || fovZoom != null) +
                               " triggers=" + (triggerSubCount + displaySubCount + triggerCount + eventTriggerCount));
            }
        }

        private static int DisableBehaviours<T>(T[] behaviours) where T : Behaviour
        {
            if (behaviours == null) return 0;
            var count = 0;
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null) continue;
                behaviour.enabled = false;
                count++;
            }

            return count;
        }

        private static void ApplyAudioPlayback(AudioSource audio, bool playing)
        {
            if (audio == null) return;
            if (playing)
            {
                if (!audio.isPlaying) audio.Play();
            }
            else
            {
                audio.Stop();
            }
        }

        private static GameObject GetGameObject(object target)
        {
            if (target is GameObject go) return go;
            if (target is Component component) return component.gameObject;
            return null;
        }

        private static void AddTransform(object target, List<Transform> transforms)
        {
            var go = GetGameObject(target);
            if (go != null && go.activeInHierarchy) transforms.Add(go.transform);
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
            Array.Sort(items, (left, right) => string.CompareOrdinal(
                left != null ? NetPath.GetPath(left.transform) : string.Empty,
                right != null ? NetPath.GetPath(right.transform) : string.Empty));
        }

        private static void SuppressTriggerLog(ManualLogSource logger, string typeName, int count)
        {
            if (logger == null) return;
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (nowMs < _nextSuppressLogMs) return;
            _nextSuppressLogMs = nowMs + 10000;
            logger.LogInfo("RoadTrip scene-state client trigger suppressed type=" + typeName + " count=" + count);
        }

        private static int GetAnimatorStateHash(Animator animator)
        {
            if (animator == null || !animator.isActiveAndEnabled) return 0;
            try
            {
                return animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            }
            catch
            {
                return 0;
            }
        }

        private static void CrossFadeAnimator(Animator animator, int stateHash)
        {
            if (animator == null || stateHash == 0) return;
            try
            {
                if (animator.GetCurrentAnimatorStateInfo(0).shortNameHash != stateHash)
                {
                    animator.CrossFade(stateHash, 0.05f, 0, 0f);
                }
            }
            catch
            {
            }
        }

        private static float GetRigWeight(Component rig)
        {
            if (rig == null) return 0f;
            var field = GetField(rig.GetType(), "weight");
            if (field != null && field.FieldType == typeof(float))
            {
                try { return (float)field.GetValue(rig); } catch { return 0f; }
            }

            var property = rig.GetType().GetProperty("weight", FieldFlags);
            if (property != null && property.PropertyType == typeof(float) && property.CanRead)
            {
                try { return (float)property.GetValue(rig, null); } catch { return 0f; }
            }

            return 0f;
        }

        private static void SetRigWeight(Component rig, float weight)
        {
            if (rig == null) return;
            var field = GetField(rig.GetType(), "weight");
            if (field != null && field.FieldType == typeof(float))
            {
                try { field.SetValue(rig, weight); return; } catch { }
            }

            var property = rig.GetType().GetProperty("weight", FieldFlags);
            if (property != null && property.PropertyType == typeof(float) && property.CanWrite)
            {
                try { property.SetValue(rig, weight, null); } catch { }
            }
        }

        private static int StableObjectNameHash(object target)
        {
            if (target == null) return 0;
            string name;
            if (target is UnityEngine.Object unityObject)
            {
                name = unityObject.name;
            }
            else
            {
                name = target.ToString();
            }

            unchecked
            {
                var hash = 2166136261u;
                if (!string.IsNullOrEmpty(name))
                {
                    for (var i = 0; i < name.Length; i++)
                    {
                        hash ^= char.ToUpperInvariant(name[i]);
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

        private static object GetFieldObject(object target, string fieldName)
        {
            if (target == null || string.IsNullOrEmpty(fieldName)) return null;
            var field = GetField(target.GetType(), fieldName);
            if (field == null) return null;

            try
            {
                return field.GetValue(target);
            }
            catch
            {
                return null;
            }
        }

        private static T GetFieldValue<T>(object target, string fieldName)
        {
            var value = GetFieldObject(target, fieldName);
            if (value is T typed) return typed;
            return default(T);
        }

        private static void SetFieldValue(object target, string fieldName, object value)
        {
            if (target == null || string.IsNullOrEmpty(fieldName)) return;
            var field = GetField(target.GetType(), fieldName);
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

        private static FieldInfo GetField(Type type, string name)
        {
            var key = (type != null ? type.FullName : string.Empty) + "." + name;
            if (FieldCache.TryGetValue(key, out var field))
            {
                return field;
            }

            var current = type;
            while (current != null)
            {
                field = current.GetField(name, FieldFlags | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    FieldCache[key] = field;
                    return field;
                }

                current = current.BaseType;
            }

            FieldCache[key] = null;
            return null;
        }
    }
}
