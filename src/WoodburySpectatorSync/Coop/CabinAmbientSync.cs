using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace WoodburySpectatorSync.Coop
{
    internal static class CabinAmbientSync
    {
        public const string KeyPrefix = "Ambient.";

        private const string PlayerPrefix = KeyPrefix + "Player.";
        private const string PlayerActivePrefix = PlayerPrefix + "Active.";
        private const string LightPrefix = KeyPrefix + "Light.";
        private const string ActivePrefix = KeyPrefix + "Active.";
        private const string OutsidePrefix = KeyPrefix + "OutsideLights.";
        private const string BedroomTvPrefix = KeyPrefix + "BedroomTV.";
        private const string BedroomTvActivePrefix = BedroomTvPrefix + "Active.";
        private const string TruckPrefix = KeyPrefix + "Truck.";
        private const string TruckActivePrefix = TruckPrefix + "Active.";
        private const string TruckRadioPrefix = TruckPrefix + "Radio.";
        private const string SinkPrefix = KeyPrefix + "Sink.";
        private const string SinkActivePrefix = SinkPrefix + "Active.";
        private const string FlashlightPrefix = KeyPrefix + "Flashlight.";
        private const string FlashlightActivePrefix = FlashlightPrefix + "Active.";
        private const int TvTimeQuantizeMs = 250;
        private const int RadioTimeQuantizeMs = 250;
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly string[] PlayerActiveFields =
        {
            "sittingPlayerEating",
            "foodParentInSittingPlayer"
        };

        private static readonly string[] PlayerBoolFields =
        {
            "playerHasFullFoodPlate",
            "playerHasEmptyFoodPlate",
            "holdingStackedPlates"
        };

        private static readonly string[] ManagerActiveFields =
        {
            "basementTablePointLight",
            "closetLight"
        };

        private static readonly string[] TruckBoolFields =
        {
            "justPressedAcc",
            "isCloseToDestination",
            "rvSlowDown",
            "bagOutsideRV",
            "carStopped"
        };

        private static readonly string[] TruckActiveFields =
        {
            "headLight1",
            "headLight2",
            "breakLight1",
            "breakLight2",
            "bag",
            "hornCollider",
            "mikeLight",
            "doorCollider",
            "insidePointLight",
            "snowCover"
        };

        private static readonly string[] SinkActiveFields =
        {
            "washingPlates",
            "washedPlates",
            "sinkClickable"
        };

        private static readonly string[] FlashlightBoolFields =
        {
            "isOn",
            "inHand"
        };

        private static readonly Dictionary<string, FieldInfo> FieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private static readonly Dictionary<int, int> BedroomTvHostTimeMsByInstance = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> RadioHostTimeMsByInstance = new Dictionary<int, int>();

        public static int EmitHostFlags(CabinGameManager manager, string fullPrefix, Action<string, int> emit)
        {
            if (manager == null || emit == null) return 0;

            var hash = 17;
            var player = ResolveCabinPlayer(manager);
            var house = ResolveCabinHouse(manager);

            if (player != null)
            {
                for (var i = 0; i < PlayerActiveFields.Length; i++)
                {
                    EmitObjectActive(fullPrefix, PlayerActivePrefix + PlayerActiveFields[i], player, PlayerActiveFields[i], emit, ref hash);
                }

                for (var i = 0; i < PlayerBoolFields.Length; i++)
                {
                    var fieldName = PlayerBoolFields[i];
                    Emit(fullPrefix + PlayerPrefix + fieldName, GetBool(player, fieldName) ? 1 : 0, emit, ref hash);
                }

                Emit(fullPrefix + PlayerPrefix + "platePresetMask", BuildGameObjectArrayMask(player, "platePresets"), emit, ref hash);
                EmitColliderEnabled(fullPrefix, PlayerPrefix + "couchColliderEnabled", player, "couchCollider", emit, ref hash);
            }

            for (var i = 0; i < ManagerActiveFields.Length; i++)
            {
                EmitObjectActive(fullPrefix, ActivePrefix + ManagerActiveFields[i], manager, ManagerActiveFields[i], emit, ref hash);
            }

            EmitLightSwitch(fullPrefix, "Basement", ResolveLightSwitch(manager, "basementLightSwitch"), emit, ref hash);
            EmitLightSwitch(fullPrefix, "Closet", house != null ? GetFieldValue<LightSwitch>(house, "closetLightSwitch") : null, emit, ref hash);
            EmitOutsideLights(fullPrefix, house != null ? GetFieldValue<OutsideLights>(house, "outsideLights") : null, emit, ref hash);
            EmitBedroomTv(fullPrefix, ResolveBedroomTv(manager), emit, ref hash);
            EmitTruck(fullPrefix, ResolveTruck(manager), emit, ref hash);
            EmitSink(fullPrefix, ResolveSink(manager), emit, ref hash);
            EmitFlashlight(fullPrefix, house != null ? GetFieldValue<FlashLight>(house, "flashLight") : null, emit, ref hash);

            return hash;
        }

        public static void CollectSyncedTransforms(CabinGameManager manager, List<Transform> transforms)
        {
            if (manager == null || transforms == null) return;

            var player = ResolveCabinPlayer(manager);
            AddTransform(GetFieldObject(player, "sittingPlayerEating"), transforms);
            AddTransform(GetFieldObject(player, "foodParentInSittingPlayer"), transforms);

            var sink = ResolveSink(manager);
            AddTransform(GetFieldObject(sink, "washingPlates"), transforms);
            AddTransform(GetFieldObject(sink, "washedPlates"), transforms);

            var truck = ResolveTruck(manager);
            AddTransform(truck, transforms);
            AddTransform(GetFieldObject(truck, "steeringHandle"), transforms);
            AddTransform(GetFieldObject(truck, "frontRightTransform"), transforms);
            AddTransform(GetFieldObject(truck, "frontLeftTransform"), transforms);
            AddTransform(GetFieldObject(truck, "backRightTransform"), transforms);
            AddTransform(GetFieldObject(truck, "backLeftTransform"), transforms);
            AddTransform(GetFieldObject(truck, "bag"), transforms);
            AddTransform(GetFieldObject(truck, "doorCollider"), transforms);
        }

        public static bool TryApplyFlag(CabinGameManager manager, string fieldName, int value, ManualLogSource logger)
        {
            if (manager == null ||
                string.IsNullOrEmpty(fieldName) ||
                !fieldName.StartsWith(KeyPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            var player = ResolveCabinPlayer(manager);
            var house = ResolveCabinHouse(manager);
            var applied = false;

            if (fieldName.StartsWith(PlayerActivePrefix, StringComparison.Ordinal))
            {
                applied = TrySetObjectActive(player, fieldName.Substring(PlayerActivePrefix.Length), value != 0);
            }
            else if (fieldName.StartsWith(PlayerPrefix, StringComparison.Ordinal))
            {
                var name = fieldName.Substring(PlayerPrefix.Length);
                if (string.Equals(name, "platePresetMask", StringComparison.Ordinal))
                {
                    applied = TryApplyGameObjectArrayMask(player, "platePresets", value);
                }
                else if (string.Equals(name, "couchColliderEnabled", StringComparison.Ordinal))
                {
                    applied = TrySetColliderEnabled(player, "couchCollider", value != 0);
                }
                else
                {
                    applied = TrySetBool(player, name, value != 0);
                }
            }
            else if (fieldName.StartsWith(ActivePrefix, StringComparison.Ordinal))
            {
                applied = TrySetObjectActive(manager, fieldName.Substring(ActivePrefix.Length), value != 0);
            }
            else if (fieldName.StartsWith(LightPrefix, StringComparison.Ordinal))
            {
                applied = ApplyLightSwitchFlag(manager, house, fieldName.Substring(LightPrefix.Length), value);
            }
            else if (fieldName.StartsWith(OutsidePrefix, StringComparison.Ordinal))
            {
                applied = ApplyOutsideLightsFlag(house != null ? GetFieldValue<OutsideLights>(house, "outsideLights") : null, fieldName.Substring(OutsidePrefix.Length), value);
            }
            else if (fieldName.StartsWith(BedroomTvActivePrefix, StringComparison.Ordinal))
            {
                var tv = ResolveBedroomTv(manager);
                applied = TrySetObjectActive(tv, fieldName.Substring(BedroomTvActivePrefix.Length), value != 0);
                ApplyBedroomTvVisualState(tv);
            }
            else if (fieldName.StartsWith(BedroomTvPrefix, StringComparison.Ordinal))
            {
                applied = ApplyBedroomTvFlag(ResolveBedroomTv(manager), fieldName.Substring(BedroomTvPrefix.Length), value);
            }
            else if (fieldName.StartsWith(TruckActivePrefix, StringComparison.Ordinal))
            {
                applied = ApplyTruckActiveFlag(ResolveTruck(manager), fieldName.Substring(TruckActivePrefix.Length), value);
            }
            else if (fieldName.StartsWith(TruckRadioPrefix, StringComparison.Ordinal))
            {
                applied = ApplyTruckRadioFlag(ResolveTruck(manager), fieldName.Substring(TruckRadioPrefix.Length), value);
            }
            else if (fieldName.StartsWith(TruckPrefix, StringComparison.Ordinal))
            {
                applied = ApplyTruckFlag(ResolveTruck(manager), fieldName.Substring(TruckPrefix.Length), value);
            }
            else if (fieldName.StartsWith(SinkActivePrefix, StringComparison.Ordinal))
            {
                applied = ApplySinkActiveFlag(ResolveSink(manager), fieldName.Substring(SinkActivePrefix.Length), value);
            }
            else if (fieldName.StartsWith(SinkPrefix, StringComparison.Ordinal))
            {
                applied = ApplySinkFlag(ResolveSink(manager), fieldName.Substring(SinkPrefix.Length), value);
            }
            else if (fieldName.StartsWith(FlashlightActivePrefix, StringComparison.Ordinal))
            {
                var flashlight = house != null ? GetFieldValue<FlashLight>(house, "flashLight") : null;
                applied = TrySetObjectActive(flashlight, fieldName.Substring(FlashlightActivePrefix.Length), value != 0);
                ApplyFlashlightVisualState(flashlight);
            }
            else if (fieldName.StartsWith(FlashlightPrefix, StringComparison.Ordinal))
            {
                var flashlight = house != null ? GetFieldValue<FlashLight>(house, "flashLight") : null;
                applied = ApplyFlashlightFlag(flashlight, fieldName.Substring(FlashlightPrefix.Length), value);
            }

            if (!applied && logger != null)
            {
                logger.LogWarning("Cabin ambient apply skipped key=" + fieldName);
            }

            return applied;
        }

        public static void ApplyClientPhaseVisuals(CabinGameManager manager, ManualLogSource logger)
        {
            if (manager == null) return;

            ApplyBedroomTvVisualState(ResolveBedroomTv(manager));
            ApplyTruckVisualState(ResolveTruck(manager));

            var house = ResolveCabinHouse(manager);
            ApplyLightSwitchVisualState(ResolveLightSwitch(manager, "basementLightSwitch"));
            ApplyLightSwitchVisualState(house != null ? GetFieldValue<LightSwitch>(house, "closetLightSwitch") : null);
            ApplyFlashlightVisualState(house != null ? GetFieldValue<FlashLight>(house, "flashLight") : null);
        }

        public static bool IsHighFrequencyStoryFlag(string fieldName)
        {
            return !string.IsNullOrEmpty(fieldName) &&
                   (fieldName.StartsWith(BedroomTvPrefix + "VideoTimeMs", StringComparison.Ordinal) ||
                    fieldName.StartsWith(TruckRadioPrefix + "AudioTimeMs", StringComparison.Ordinal));
        }

        private static void EmitTruck(string fullPrefix, TruckController truck, Action<string, int> emit, ref int hash)
        {
            if (truck == null)
            {
                Emit(fullPrefix + TruckPrefix + "RootActive", 0, emit, ref hash);
                return;
            }

            Emit(fullPrefix + TruckPrefix + "RootActive", truck.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(fullPrefix + TruckPrefix + "Enabled", truck.enabled ? 1 : 0, emit, ref hash);
            Emit(fullPrefix + TruckPrefix + "rvState", Convert.ToInt32(truck.rvState), emit, ref hash);
            Emit(fullPrefix + TruckPrefix + "avgRPM100", Mathf.RoundToInt(truck.avgRPM * 100f), emit, ref hash);
            Emit(fullPrefix + TruckPrefix + "currentAcceleration", Mathf.RoundToInt(GetFloat(truck, "currentAcceleration")), emit, ref hash);
            Emit(fullPrefix + TruckPrefix + "currentBreakForce", Mathf.RoundToInt(GetFloat(truck, "currentBreakForce")), emit, ref hash);
            Emit(fullPrefix + TruckPrefix + "currentTurnAngle100", Mathf.RoundToInt(GetFloat(truck, "currentTurnAngle") * 100f), emit, ref hash);

            for (var i = 0; i < TruckBoolFields.Length; i++)
            {
                var fieldName = TruckBoolFields[i];
                Emit(fullPrefix + TruckPrefix + fieldName, GetBool(truck, fieldName) ? 1 : 0, emit, ref hash);
            }

            for (var i = 0; i < TruckActiveFields.Length; i++)
            {
                EmitObjectActive(fullPrefix, TruckActivePrefix + TruckActiveFields[i], truck, TruckActiveFields[i], emit, ref hash);
            }

            EmitLightState(fullPrefix + TruckPrefix + "headLight1.", GetLightFromObject(GetFieldObject(truck, "headLight1")), emit, ref hash);
            EmitLightState(fullPrefix + TruckPrefix + "headLight2.", GetLightFromObject(GetFieldObject(truck, "headLight2")), emit, ref hash);
            EmitAudioState(fullPrefix + TruckPrefix + "idleAS.", GetFieldObject(truck, "idleAS") as AudioSource, emit, ref hash);
            EmitAudioState(fullPrefix + TruckPrefix + "accelerateAS.", truck.accelerateAS, emit, ref hash);
            EmitAudioState(fullPrefix + TruckPrefix + "reverseAS.", GetFieldObject(truck, "reverseAS") as AudioSource, emit, ref hash);
            EmitAnimatorState(fullPrefix + TruckPrefix + "bobbleHead.", truck.bobbleHeadAnimator, emit, ref hash);
            EmitRadio(fullPrefix, GetFieldValue<Radio>(truck, "radio"), emit, ref hash);
        }

        private static bool ApplyTruckActiveFlag(TruckController truck, string name, int value)
        {
            if (truck == null) return value == 0;
            return TrySetObjectActive(truck, name, value != 0);
        }

        private static bool ApplyTruckFlag(TruckController truck, string name, int value)
        {
            if (truck == null) return value == 0;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal))
            {
                truck.gameObject.SetActive(value != 0);
                return true;
            }

            if (string.Equals(name, "Enabled", StringComparison.Ordinal))
            {
                // Keep the local controller from making visual state decisions while still allowing the object to render.
                truck.enabled = false;
                return true;
            }

            if (string.Equals(name, "rvState", StringComparison.Ordinal))
            {
                truck.rvState = (TruckController.RVState)Mathf.Clamp(value, 0, 3);
                ApplyTruckVisualState(truck);
                return true;
            }

            if (string.Equals(name, "avgRPM100", StringComparison.Ordinal))
            {
                truck.avgRPM = value / 100f;
                ApplyTruckVisualState(truck);
                return true;
            }

            if (string.Equals(name, "currentAcceleration", StringComparison.Ordinal) ||
                string.Equals(name, "currentBreakForce", StringComparison.Ordinal))
            {
                return TrySetFloat(truck, name, value);
            }

            if (string.Equals(name, "currentTurnAngle100", StringComparison.Ordinal))
            {
                return TrySetFloat(truck, "currentTurnAngle", value / 100f);
            }

            if (name.StartsWith("headLight1.", StringComparison.Ordinal))
            {
                return ApplyLightState(GetLightFromObject(GetFieldObject(truck, "headLight1")), name.Substring("headLight1.".Length), value);
            }

            if (name.StartsWith("headLight2.", StringComparison.Ordinal))
            {
                return ApplyLightState(GetLightFromObject(GetFieldObject(truck, "headLight2")), name.Substring("headLight2.".Length), value);
            }

            if (name.StartsWith("idleAS.", StringComparison.Ordinal))
            {
                return ApplyAudioState(GetFieldObject(truck, "idleAS") as AudioSource, name.Substring("idleAS.".Length), value);
            }

            if (name.StartsWith("accelerateAS.", StringComparison.Ordinal))
            {
                return ApplyAudioState(truck.accelerateAS, name.Substring("accelerateAS.".Length), value);
            }

            if (name.StartsWith("reverseAS.", StringComparison.Ordinal))
            {
                return ApplyAudioState(GetFieldObject(truck, "reverseAS") as AudioSource, name.Substring("reverseAS.".Length), value);
            }

            if (name.StartsWith("bobbleHead.", StringComparison.Ordinal))
            {
                return ApplyAnimatorState(truck.bobbleHeadAnimator, name.Substring("bobbleHead.".Length), value);
            }

            for (var i = 0; i < TruckBoolFields.Length; i++)
            {
                if (string.Equals(name, TruckBoolFields[i], StringComparison.Ordinal))
                {
                    return TrySetBool(truck, name, value != 0);
                }
            }

            return false;
        }

        private static void EmitRadio(string fullPrefix, Radio radio, Action<string, int> emit, ref int hash)
        {
            if (radio == null)
            {
                Emit(fullPrefix + TruckRadioPrefix + "RootActive", 0, emit, ref hash);
                return;
            }

            Emit(fullPrefix + TruckRadioPrefix + "RootActive", radio.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(fullPrefix + TruckRadioPrefix + "Enabled", radio.enabled ? 1 : 0, emit, ref hash);
            Emit(fullPrefix + TruckRadioPrefix + "isStopped", radio.isStopped ? 1 : 0, emit, ref hash);
            Emit(fullPrefix + TruckRadioPrefix + "currentClip", radio.currentClip, emit, ref hash);
            Emit(fullPrefix + TruckRadioPrefix + "cliptimerMs", QuantizeMs(Mathf.RoundToInt(GetFloat(radio, "cliptimer") * 1000f), RadioTimeQuantizeMs), emit, ref hash);
            EmitTransient(fullPrefix + TruckRadioPrefix + "AudioTimeMs", QuantizeMs(GetAudioTimeMs(radio.audioSource), RadioTimeQuantizeMs), emit);
            EmitAudioState(fullPrefix + TruckRadioPrefix + "audio.", radio.audioSource, emit, ref hash);
        }

        private static bool ApplyTruckRadioFlag(TruckController truck, string name, int value)
        {
            var radio = GetFieldValue<Radio>(truck, "radio") ?? UnityEngine.Object.FindObjectOfType<Radio>();
            if (radio == null) return value == 0;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal))
            {
                radio.gameObject.SetActive(value != 0);
                return true;
            }

            if (string.Equals(name, "Enabled", StringComparison.Ordinal))
            {
                radio.enabled = false;
                return true;
            }

            if (string.Equals(name, "isStopped", StringComparison.Ordinal))
            {
                radio.isStopped = value != 0;
                ApplyRadioVisualState(radio);
                return true;
            }

            if (string.Equals(name, "currentClip", StringComparison.Ordinal))
            {
                radio.currentClip = value;
                ApplyRadioVisualState(radio);
                return true;
            }

            if (string.Equals(name, "cliptimerMs", StringComparison.Ordinal))
            {
                return TrySetFloat(radio, "cliptimer", value / 1000f);
            }

            if (string.Equals(name, "AudioTimeMs", StringComparison.Ordinal))
            {
                RadioHostTimeMsByInstance[radio.GetInstanceID()] = value;
                ApplyRadioVisualState(radio);
                return true;
            }

            if (name.StartsWith("audio.", StringComparison.Ordinal))
            {
                var applied = ApplyAudioState(radio.audioSource, name.Substring("audio.".Length), value);
                ApplyRadioVisualState(radio);
                return applied;
            }

            return false;
        }

        private static void ApplyTruckVisualState(TruckController truck)
        {
            if (truck == null) return;

            var driving = truck.rvState == TruckController.RVState.DrivingRV;
            TrySetObjectActive(truck, "headLight1", driving || truck.rvState == TruckController.RVState.StoppedRV);
            TrySetObjectActive(truck, "headLight2", driving || truck.rvState == TruckController.RVState.StoppedRV);
            TrySetObjectActive(truck, "breakLight1", truck.rvState == TruckController.RVState.StoppedRV || GetFloat(truck, "currentBreakForce") > 1f);
            TrySetObjectActive(truck, "breakLight2", truck.rvState == TruckController.RVState.StoppedRV || GetFloat(truck, "currentBreakForce") > 1f);

            if (truck.bobbleHeadAnimator != null)
            {
                truck.bobbleHeadAnimator.speed = Mathf.Abs(truck.avgRPM) > 10f ? 1f : 0f;
            }
        }

        private static void ApplyRadioVisualState(Radio radio)
        {
            if (radio == null || radio.audioSource == null) return;

            var clips = radio.radioClips;
            if (clips != null && clips.Length > 0 && radio.currentClip >= 0 && radio.currentClip < clips.Length)
            {
                var clip = clips[radio.currentClip];
                if (clip != null && radio.audioSource.clip != clip)
                {
                    radio.audioSource.clip = clip;
                }
            }
            else if (radio.beSincereRadioClip != null && radio.audioSource.clip == null)
            {
                radio.audioSource.clip = radio.beSincereRadioClip;
            }

            if (RadioHostTimeMsByInstance.TryGetValue(radio.GetInstanceID(), out var hostTimeMs))
            {
                ApplyAudioTime(radio.audioSource, hostTimeMs);
            }

            if (radio.isStopped)
            {
                radio.audioSource.volume = 0f;
                if (radio.audioSource.isPlaying)
                {
                    radio.audioSource.Stop();
                }
            }
            else
            {
                if (!radio.audioSource.isPlaying && radio.audioSource.clip != null)
                {
                    radio.audioSource.Play();
                }
            }
        }

        private static void EmitLightSwitch(string fullPrefix, string id, LightSwitch lightSwitch, Action<string, int> emit, ref int hash)
        {
            if (lightSwitch == null)
            {
                Emit(fullPrefix + LightPrefix + id + ".RootActive", 0, emit, ref hash);
                return;
            }

            var prefix = fullPrefix + LightPrefix + id + ".";
            Emit(prefix + "RootActive", lightSwitch.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "isOn", GetBool(lightSwitch, "isOn") ? 1 : 0, emit, ref hash);
            Emit(prefix + "lightMask", BuildGameObjectArrayMask(lightSwitch, "lightGameobjects"), emit, ref hash);
            var switchTransform = GetFieldValue<Transform>(lightSwitch, "switchTransform");
            Emit(prefix + "switchY100", switchTransform != null ? Mathf.RoundToInt(NormalizeAngle(switchTransform.localEulerAngles.y) * 100f) : 0, emit, ref hash);
        }

        private static bool ApplyLightSwitchFlag(CabinGameManager manager, CabinHouseManager house, string localKey, int value)
        {
            if (string.IsNullOrEmpty(localKey)) return false;
            var dot = localKey.IndexOf('.');
            if (dot <= 0 || dot + 1 >= localKey.Length) return false;

            var id = localKey.Substring(0, dot);
            var name = localKey.Substring(dot + 1);
            var lightSwitch = string.Equals(id, "Basement", StringComparison.Ordinal)
                ? ResolveLightSwitch(manager, "basementLightSwitch")
                : (house != null ? GetFieldValue<LightSwitch>(house, "closetLightSwitch") : null);

            if (lightSwitch == null) return value == 0;

            var applied = false;
            if (string.Equals(name, "RootActive", StringComparison.Ordinal))
            {
                lightSwitch.gameObject.SetActive(value != 0);
                applied = true;
            }
            else if (string.Equals(name, "isOn", StringComparison.Ordinal))
            {
                applied = TrySetBool(lightSwitch, "isOn", value != 0);
            }
            else if (string.Equals(name, "lightMask", StringComparison.Ordinal))
            {
                applied = TryApplyGameObjectArrayMask(lightSwitch, "lightGameobjects", value);
            }
            else if (string.Equals(name, "switchY100", StringComparison.Ordinal))
            {
                var switchTransform = GetFieldValue<Transform>(lightSwitch, "switchTransform");
                if (switchTransform != null)
                {
                    var euler = switchTransform.localEulerAngles;
                    euler.y = value / 100f;
                    switchTransform.localEulerAngles = euler;
                    applied = true;
                }
                else
                {
                    applied = value == 0;
                }
            }

            ApplyLightSwitchVisualState(lightSwitch);
            return applied;
        }

        private static void EmitOutsideLights(string fullPrefix, OutsideLights outside, Action<string, int> emit, ref int hash)
        {
            if (outside == null)
            {
                Emit(fullPrefix + OutsidePrefix + "RootActive", 0, emit, ref hash);
                return;
            }

            Emit(fullPrefix + OutsidePrefix + "RootActive", outside.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(fullPrefix + OutsidePrefix + "lightsOn", GetBool(outside, "lightsOn") ? 1 : 0, emit, ref hash);
            EmitLightState(fullPrefix + OutsidePrefix + "light1", outside.outsideLight1, emit, ref hash);
            EmitLightState(fullPrefix + OutsidePrefix + "light2", outside.outsideLight2, emit, ref hash);
        }

        private static bool ApplyOutsideLightsFlag(OutsideLights outside, string name, int value)
        {
            if (outside == null) return value == 0;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal))
            {
                outside.gameObject.SetActive(value != 0);
                return true;
            }

            if (string.Equals(name, "lightsOn", StringComparison.Ordinal))
            {
                return TrySetBool(outside, "lightsOn", value != 0);
            }

            if (name.StartsWith("light1", StringComparison.Ordinal))
            {
                return ApplyLightState(outside.outsideLight1, name.Substring("light1".Length), value);
            }

            if (name.StartsWith("light2", StringComparison.Ordinal))
            {
                return ApplyLightState(outside.outsideLight2, name.Substring("light2".Length), value);
            }

            return false;
        }

        private static void EmitBedroomTv(string fullPrefix, Component tv, Action<string, int> emit, ref int hash)
        {
            if (tv == null)
            {
                Emit(fullPrefix + BedroomTvPrefix + "RootActive", 0, emit, ref hash);
                return;
            }

            Emit(fullPrefix + BedroomTvPrefix + "RootActive", tv.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(fullPrefix + BedroomTvPrefix + "isTurnedOn", GetBool(tv, "isTurnedOn") ? 1 : 0, emit, ref hash);
            Emit(fullPrefix + BedroomTvPrefix + "canBeTurnedOff", GetBool(tv, "canBeTurnedOff") ? 1 : 0, emit, ref hash);
            Emit(fullPrefix + BedroomTvPrefix + "MeshRendererEnabled", GetRendererEnabled(tv, "meshRenderer") ? 1 : 0, emit, ref hash);
            EmitTransient(fullPrefix + BedroomTvPrefix + "VideoTimeMs", QuantizeMs(GetVideoPlayerTimeMs(GetFieldObject(tv, "videoPlayer")), TvTimeQuantizeMs), emit);
            EmitObjectActive(fullPrefix, BedroomTvActivePrefix + "tvLight", tv, "tvLight", emit, ref hash);
            EmitObjectActive(fullPrefix, BedroomTvActivePrefix + "videoMesh", tv, "videoMesh", emit, ref hash);
        }

        private static bool ApplyBedroomTvFlag(Component tv, string name, int value)
        {
            if (tv == null) return value == 0;

            var applied = false;
            if (string.Equals(name, "RootActive", StringComparison.Ordinal))
            {
                tv.gameObject.SetActive(value != 0);
                applied = true;
            }
            else if (string.Equals(name, "isTurnedOn", StringComparison.Ordinal) ||
                     string.Equals(name, "canBeTurnedOff", StringComparison.Ordinal))
            {
                applied = TrySetBool(tv, name, value != 0);
            }
            else if (string.Equals(name, "MeshRendererEnabled", StringComparison.Ordinal))
            {
                applied = TrySetRendererEnabled(tv, "meshRenderer", value != 0);
            }
            else if (string.Equals(name, "VideoTimeMs", StringComparison.Ordinal))
            {
                BedroomTvHostTimeMsByInstance[tv.GetInstanceID()] = value;
                applied = true;
            }

            ApplyBedroomTvVisualState(tv);
            return applied;
        }

        private static void EmitSink(string fullPrefix, Sink sink, Action<string, int> emit, ref int hash)
        {
            if (sink == null)
            {
                Emit(fullPrefix + SinkPrefix + "RootActive", 0, emit, ref hash);
                return;
            }

            Emit(fullPrefix + SinkPrefix + "RootActive", sink.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            for (var i = 0; i < SinkActiveFields.Length; i++)
            {
                EmitObjectActive(fullPrefix, SinkActivePrefix + SinkActiveFields[i], sink, SinkActiveFields[i], emit, ref hash);
            }

            Emit(fullPrefix + SinkPrefix + "washingPS1Playing", IsParticlePlaying(GetFieldObject(sink, "washingPS1")) ? 1 : 0, emit, ref hash);
            Emit(fullPrefix + SinkPrefix + "washingPS2Playing", IsParticlePlaying(GetFieldObject(sink, "washingPS2")) ? 1 : 0, emit, ref hash);
        }

        private static bool ApplySinkActiveFlag(Sink sink, string name, int value)
        {
            if (sink == null) return value == 0;
            return TrySetObjectActive(sink, name, value != 0);
        }

        private static bool ApplySinkFlag(Sink sink, string name, int value)
        {
            if (sink == null) return value == 0;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal))
            {
                sink.gameObject.SetActive(value != 0);
                return true;
            }

            if (string.Equals(name, "washingPS1Playing", StringComparison.Ordinal))
            {
                return TrySetParticlePlaying(GetFieldObject(sink, "washingPS1"), value != 0);
            }

            if (string.Equals(name, "washingPS2Playing", StringComparison.Ordinal))
            {
                return TrySetParticlePlaying(GetFieldObject(sink, "washingPS2"), value != 0);
            }

            return false;
        }

        private static void EmitFlashlight(string fullPrefix, FlashLight flashlight, Action<string, int> emit, ref int hash)
        {
            if (flashlight == null)
            {
                Emit(fullPrefix + FlashlightPrefix + "RootActive", 0, emit, ref hash);
                return;
            }

            Emit(fullPrefix + FlashlightPrefix + "RootActive", flashlight.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            for (var i = 0; i < FlashlightBoolFields.Length; i++)
            {
                var fieldName = FlashlightBoolFields[i];
                Emit(fullPrefix + FlashlightPrefix + fieldName, GetBool(flashlight, fieldName) ? 1 : 0, emit, ref hash);
            }

            EmitObjectActive(fullPrefix, FlashlightActivePrefix + "lightComp", flashlight, "lightComp", emit, ref hash);
        }

        private static bool ApplyFlashlightFlag(FlashLight flashlight, string name, int value)
        {
            if (flashlight == null) return value == 0;

            var applied = false;
            if (string.Equals(name, "RootActive", StringComparison.Ordinal))
            {
                flashlight.gameObject.SetActive(value != 0);
                applied = true;
            }
            else
            {
                applied = TrySetBool(flashlight, name, value != 0);
            }

            ApplyFlashlightVisualState(flashlight);
            return applied;
        }

        private static CabinHouseManager ResolveCabinHouse(CabinGameManager manager)
        {
            return manager != null && manager.cabinHouseManager != null
                ? manager.cabinHouseManager
                : UnityEngine.Object.FindObjectOfType<CabinHouseManager>();
        }

        private static CabinPlayerController ResolveCabinPlayer(CabinGameManager manager)
        {
            return manager != null && manager.cabinPlayerController != null
                ? manager.cabinPlayerController
                : UnityEngine.Object.FindObjectOfType<CabinPlayerController>();
        }

        private static LightSwitch ResolveLightSwitch(CabinGameManager manager, string fieldName)
        {
            return GetFieldValue<LightSwitch>(manager, fieldName);
        }

        private static Sink ResolveSink(CabinGameManager manager)
        {
            var player = ResolveCabinPlayer(manager);
            return player != null && player.sink != null ? player.sink : UnityEngine.Object.FindObjectOfType<Sink>();
        }

        private static Component ResolveBedroomTv(CabinGameManager manager)
        {
            var direct = GetFieldValue<Component>(manager, "bedroomTV");
            if (direct != null) return direct;

            var tvs = UnityEngine.Object.FindObjectsOfType<InteractableTV>();
            for (var i = 0; i < tvs.Length; i++)
            {
                var tv = tvs[i];
                if (tv == null) continue;
                var type = GetFieldObject(tv, "type");
                if (type != null && string.Equals(type.ToString(), "Bedroom", StringComparison.OrdinalIgnoreCase))
                {
                    return tv;
                }
            }

            var legacy = UnityEngine.Object.FindObjectOfType<InteractableTVBedroom>();
            return legacy;
        }

        private static TruckController ResolveTruck(CabinGameManager manager)
        {
            if (manager != null && manager.truckController != null)
            {
                return manager.truckController;
            }

            return UnityEngine.Object.FindObjectOfType<TruckController>();
        }

        private static void ApplyLightSwitchVisualState(LightSwitch lightSwitch)
        {
            if (lightSwitch == null) return;

            var isOn = GetBool(lightSwitch, "isOn");
            var lightMask = isOn ? BuildGameObjectArrayMask(lightSwitch, "lightGameobjects") : 0;
            if (isOn && lightMask == 0)
            {
                var lights = GetFieldValue<Array>(lightSwitch, "lightGameobjects");
                if (lights != null)
                {
                    for (var i = 0; i < lights.Length; i++)
                    {
                        var go = ExtractGameObject(lights.GetValue(i));
                        if (go != null) go.SetActive(true);
                    }
                }
            }

            SetMaterialEmission(GetFieldValue<Material>(lightSwitch, "bulbMaterial"), isOn);
            SetMaterialEmission(GetFieldValue<Material>(lightSwitch, "lightBoxMaterial"), !isOn);
        }

        private static void ApplyBedroomTvVisualState(Component tv)
        {
            if (tv == null) return;

            var isOn = GetBool(tv, "isTurnedOn");
            TrySetObjectActive(tv, "tvLight", isOn);
            TrySetObjectActive(tv, "videoMesh", isOn);
            TrySetRendererEnabled(tv, "meshRenderer", isOn);

            var renderer = GetFieldValue<MeshRenderer>(tv, "meshRenderer");
            var material = GetFieldValue<Material>(tv, isOn ? "tvOnMaterial" : "tvOffMaterial");
            if (renderer != null && material != null)
            {
                renderer.material = material;
            }

            var videoPlayer = GetFieldObject(tv, "videoPlayer");
            if (BedroomTvHostTimeMsByInstance.TryGetValue(tv.GetInstanceID(), out var hostTimeMs))
            {
                ApplyVideoPlayerTime(videoPlayer, hostTimeMs);
            }

            InvokeNoArg(videoPlayer, isOn ? "Play" : "Stop");
        }

        private static void ApplyFlashlightVisualState(FlashLight flashlight)
        {
            if (flashlight == null) return;
            TrySetObjectActive(flashlight, "lightComp", GetBool(flashlight, "isOn"));
        }

        private static void EmitLightState(string prefix, Light light, Action<string, int> emit, ref int hash)
        {
            Emit(prefix + "Active", light != null && light.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Enabled", light != null && light.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "Intensity1000", light != null ? Mathf.RoundToInt(light.intensity * 1000f) : 0, emit, ref hash);
        }

        private static bool ApplyLightState(Light light, string suffix, int value)
        {
            if (light == null) return value == 0;
            if (string.Equals(suffix, "Active", StringComparison.Ordinal))
            {
                light.gameObject.SetActive(value != 0);
                return true;
            }

            if (string.Equals(suffix, "Enabled", StringComparison.Ordinal))
            {
                light.enabled = value != 0;
                return true;
            }

            if (string.Equals(suffix, "Intensity1000", StringComparison.Ordinal))
            {
                light.intensity = Mathf.Max(0f, value / 1000f);
                return true;
            }

            return false;
        }

        private static void EmitAudioState(string prefix, AudioSource audioSource, Action<string, int> emit, ref int hash)
        {
            Emit(prefix + "Exists", audioSource != null ? 1 : 0, emit, ref hash);
            if (audioSource == null) return;

            Emit(prefix + "Enabled", audioSource.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "Playing", audioSource.isPlaying ? 1 : 0, emit, ref hash);
            Emit(prefix + "Volume1000", Mathf.RoundToInt(audioSource.volume * 1000f), emit, ref hash);
            Emit(prefix + "Loop", audioSource.loop ? 1 : 0, emit, ref hash);
        }

        private static bool ApplyAudioState(AudioSource audioSource, string suffix, int value)
        {
            if (audioSource == null) return value == 0;

            if (string.Equals(suffix, "Exists", StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(suffix, "Enabled", StringComparison.Ordinal))
            {
                audioSource.enabled = value != 0;
                return true;
            }

            if (string.Equals(suffix, "Playing", StringComparison.Ordinal))
            {
                if (value != 0)
                {
                    if (!audioSource.isPlaying && audioSource.clip != null)
                    {
                        audioSource.Play();
                    }
                }
                else if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                }

                return true;
            }

            if (string.Equals(suffix, "Volume1000", StringComparison.Ordinal))
            {
                audioSource.volume = Mathf.Clamp01(value / 1000f);
                return true;
            }

            if (string.Equals(suffix, "Loop", StringComparison.Ordinal))
            {
                audioSource.loop = value != 0;
                return true;
            }

            return false;
        }

        private static void EmitAnimatorState(string prefix, Animator animator, Action<string, int> emit, ref int hash)
        {
            Emit(prefix + "Exists", animator != null ? 1 : 0, emit, ref hash);
            if (animator == null) return;

            Emit(prefix + "Enabled", animator.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "Speed1000", Mathf.RoundToInt(animator.speed * 1000f), emit, ref hash);
        }

        private static bool ApplyAnimatorState(Animator animator, string suffix, int value)
        {
            if (animator == null) return value == 0;

            if (string.Equals(suffix, "Exists", StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(suffix, "Enabled", StringComparison.Ordinal))
            {
                animator.enabled = value != 0;
                return true;
            }

            if (string.Equals(suffix, "Speed1000", StringComparison.Ordinal))
            {
                animator.speed = value / 1000f;
                return true;
            }

            return false;
        }

        private static void EmitObjectActive(string fullPrefix, string key, object target, string fieldName, Action<string, int> emit, ref int hash)
        {
            Emit(fullPrefix + key, GetObjectActive(target, fieldName) ? 1 : 0, emit, ref hash);
        }

        private static void EmitColliderEnabled(string fullPrefix, string key, object target, string fieldName, Action<string, int> emit, ref int hash)
        {
            Emit(fullPrefix + key, GetColliderEnabled(target, fieldName) ? 1 : 0, emit, ref hash);
        }

        private static void Emit(string key, int value, Action<string, int> emit, ref int hash)
        {
            hash = unchecked(hash * 31 + value);
            emit(key, value);
        }

        private static void EmitTransient(string key, int value, Action<string, int> emit)
        {
            emit(key, value);
        }

        private static void AddTransform(object value, List<Transform> transforms)
        {
            if (transforms == null) return;
            var go = ExtractGameObject(value);
            if (go == null) return;
            for (var i = 0; i < transforms.Count; i++)
            {
                if (transforms[i] == go.transform) return;
            }

            transforms.Add(go.transform);
        }

        private static int BuildGameObjectArrayMask(object target, string fieldName)
        {
            var array = GetFieldValue<Array>(target, fieldName);
            if (array == null) return 0;

            var mask = 0;
            var count = Math.Min(array.Length, 30);
            for (var i = 0; i < count; i++)
            {
                var go = ExtractGameObject(array.GetValue(i));
                if (go != null && go.activeSelf)
                {
                    mask |= 1 << i;
                }
            }

            return mask;
        }

        private static bool TryApplyGameObjectArrayMask(object target, string fieldName, int mask)
        {
            var array = GetFieldValue<Array>(target, fieldName);
            if (array == null) return mask == 0;

            var count = Math.Min(array.Length, 30);
            for (var i = 0; i < count; i++)
            {
                var go = ExtractGameObject(array.GetValue(i));
                if (go != null)
                {
                    go.SetActive((mask & (1 << i)) != 0);
                }
            }

            return true;
        }

        private static bool GetObjectActive(object target, string fieldName)
        {
            var go = ExtractGameObject(GetFieldObject(target, fieldName));
            return go != null && go.activeSelf;
        }

        private static bool TrySetObjectActive(object target, string fieldName, bool active)
        {
            var go = ExtractGameObject(GetFieldObject(target, fieldName));
            if (go == null) return !active;
            go.SetActive(active);
            return true;
        }

        private static bool GetColliderEnabled(object target, string fieldName)
        {
            var collider = GetFieldObject(target, fieldName) as Collider;
            return collider != null && collider.enabled;
        }

        private static bool TrySetColliderEnabled(object target, string fieldName, bool enabled)
        {
            var collider = GetFieldObject(target, fieldName) as Collider;
            if (collider == null) return !enabled;
            collider.enabled = enabled;
            return true;
        }

        private static bool GetRendererEnabled(object target, string fieldName)
        {
            var renderer = GetFieldObject(target, fieldName) as Renderer;
            return renderer != null && renderer.enabled;
        }

        private static bool TrySetRendererEnabled(object target, string fieldName, bool enabled)
        {
            var renderer = GetFieldObject(target, fieldName) as Renderer;
            if (renderer == null) return !enabled;
            renderer.enabled = enabled;
            return true;
        }

        private static bool IsParticlePlaying(object particleSystem)
        {
            return TryGetBoolProperty(particleSystem, "isPlaying", out var isPlaying) && isPlaying;
        }

        private static bool TrySetParticlePlaying(object particleSystem, bool playing)
        {
            if (particleSystem == null) return !playing;
            InvokeNoArg(particleSystem, playing ? "Play" : "Stop");
            return true;
        }

        private static int GetVideoPlayerTimeMs(object videoPlayer)
        {
            if (TryGetDoubleProperty(videoPlayer, "time", out var time) && time >= 0d)
            {
                return Mathf.Clamp(Mathf.RoundToInt((float)(time * 1000d)), 0, int.MaxValue);
            }

            return 0;
        }

        private static int GetAudioTimeMs(AudioSource audioSource)
        {
            if (audioSource == null) return 0;
            return Mathf.Clamp(Mathf.RoundToInt(audioSource.time * 1000f), 0, int.MaxValue);
        }

        private static void ApplyVideoPlayerTime(object videoPlayer, int hostTimeMs)
        {
            if (videoPlayer == null || hostTimeMs <= 0) return;

            var targetSeconds = hostTimeMs / 1000d;
            if (TryGetDoubleProperty(videoPlayer, "time", out var currentSeconds) &&
                Math.Abs(currentSeconds - targetSeconds) < 0.35d)
            {
                return;
            }

            TrySetDoubleProperty(videoPlayer, "time", targetSeconds);
        }

        private static void ApplyAudioTime(AudioSource audioSource, int hostTimeMs)
        {
            if (audioSource == null || audioSource.clip == null || hostTimeMs <= 0) return;

            var targetSeconds = Mathf.Clamp(hostTimeMs / 1000f, 0f, Mathf.Max(0f, audioSource.clip.length - 0.01f));
            if (Mathf.Abs(audioSource.time - targetSeconds) < 0.35f)
            {
                return;
            }

            audioSource.time = targetSeconds;
        }

        private static int QuantizeMs(int value, int quantum)
        {
            if (value <= 0 || quantum <= 1) return Mathf.Max(0, value);
            return (value / quantum) * quantum;
        }

        private static bool GetBool(object target, string fieldName)
        {
            var field = FindField(target, fieldName);
            if (field == null || field.FieldType != typeof(bool)) return false;
            try
            {
                return (bool)field.GetValue(target);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool TrySetBool(object target, string fieldName, bool value)
        {
            var field = FindField(target, fieldName);
            if (field == null || field.FieldType != typeof(bool)) return false;
            try
            {
                field.SetValue(target, value);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static float GetFloat(object target, string fieldName)
        {
            var field = FindField(target, fieldName);
            if (field == null) return 0f;
            try
            {
                var raw = field.GetValue(target);
                if (raw == null) return 0f;
                return Convert.ToSingle(raw);
            }
            catch (Exception)
            {
                return 0f;
            }
        }

        private static bool TrySetFloat(object target, string fieldName, float value)
        {
            var field = FindField(target, fieldName);
            if (field == null || field.FieldType != typeof(float)) return false;
            try
            {
                field.SetValue(target, value);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static T GetFieldValue<T>(object target, string fieldName) where T : class
        {
            return GetFieldObject(target, fieldName) as T;
        }

        private static object GetFieldObject(object target, string fieldName)
        {
            var field = FindField(target, fieldName);
            if (field == null) return null;
            try
            {
                return field.GetValue(target);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static FieldInfo FindField(object target, string fieldName)
        {
            if (target == null || string.IsNullOrEmpty(fieldName)) return null;
            var type = target.GetType();
            var key = type.FullName + "." + fieldName;
            if (!FieldCache.TryGetValue(key, out var field))
            {
                field = FindInstanceField(type, fieldName);
                FieldCache[key] = field;
            }

            return field;
        }

        private static FieldInfo FindInstanceField(Type type, string name)
        {
            while (type != null)
            {
                var field = type.GetField(name, FieldFlags | BindingFlags.DeclaredOnly);
                if (field != null) return field;
                type = type.BaseType;
            }

            return null;
        }

        private static GameObject ExtractGameObject(object value)
        {
            if (value is GameObject go) return go;
            if (value is Component component) return component.gameObject;
            return null;
        }

        private static Light GetLightFromObject(object value)
        {
            var go = ExtractGameObject(value);
            return go != null ? go.GetComponent<Light>() : null;
        }

        private static void SetMaterialEmission(Material material, bool enabled)
        {
            if (material == null) return;
            if (enabled)
            {
                material.EnableKeyword("_EMISSION");
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }
        }

        private static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;
            return angle;
        }

        private static bool TryGetDoubleProperty(object target, string propertyName, out double value)
        {
            value = 0d;
            if (target == null || string.IsNullOrEmpty(propertyName)) return false;
            try
            {
                var property = target.GetType().GetProperty(propertyName, FieldFlags);
                if (property == null || !property.CanRead) return false;
                var raw = property.GetValue(target, null);
                if (raw == null) return false;
                value = Convert.ToDouble(raw);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool TrySetDoubleProperty(object target, string propertyName, double value)
        {
            if (target == null || string.IsNullOrEmpty(propertyName)) return false;
            try
            {
                var property = target.GetType().GetProperty(propertyName, FieldFlags);
                if (property == null || !property.CanWrite) return false;
                property.SetValue(target, value, null);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool TryGetBoolProperty(object target, string propertyName, out bool value)
        {
            value = false;
            if (target == null || string.IsNullOrEmpty(propertyName)) return false;
            try
            {
                var property = target.GetType().GetProperty(propertyName, FieldFlags);
                if (property == null || !property.CanRead) return false;
                var raw = property.GetValue(target, null);
                if (raw == null) return false;
                value = Convert.ToBoolean(raw);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void InvokeNoArg(object target, string methodName)
        {
            if (target == null || string.IsNullOrEmpty(methodName)) return;
            try
            {
                var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                method?.Invoke(target, null);
            }
            catch (Exception)
            {
                // Visual/audio best-effort only.
            }
        }
    }
}
