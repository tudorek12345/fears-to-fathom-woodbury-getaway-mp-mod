using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace WoodburySpectatorSync.Coop
{
    internal static class CabinCookingPropSync
    {
        public const string KeyPrefix = "Cooking.";

        private const string CasserolePrefix = KeyPrefix + "Casserole.";
        private const string CasseroleActivePrefix = CasserolePrefix + "Active.";
        private const string OvenPrefix = KeyPrefix + "Oven.";
        private const string OvenActivePrefix = OvenPrefix + "Active.";
        private const string MikeCookPrefix = KeyPrefix + "MikeCook.";
        private const string MikeCookActivePrefix = MikeCookPrefix + "Active.";
        private const string MikeCookRigPrefix = MikeCookPrefix + "Rig.";
        private const string LivingRoomTvPrefix = KeyPrefix + "LivingRoomTV.";
        private const string LivingRoomTvActivePrefix = LivingRoomTvPrefix + "Active.";
        private const string OuijaPrefix = KeyPrefix + "Ouija.";
        private const string OuijaActivePrefix = OuijaPrefix + "Active.";

        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const int TvTimeQuantizeMs = 250;

        private static readonly string[] CasseroleBoolFields =
        {
            "canHold",
            "isPlacedInOven",
            "cookingStarted",
            "isMarinated",
            "isCooked",
            "areFishesAdded",
            "areVeggiesAdded",
            "hasPlayerPickedUpFish"
        };

        private static readonly string[] CasseroleActiveFields =
        {
            "veggiePlate",
            "veggiesInContainer",
            "veggiesInPlate",
            "todoCanvas",
            "fish1",
            "fish2"
        };

        private static readonly string[] MikeCookPropFields =
        {
            "fishInHand",
            "knife",
            "casseroleInHand",
            "bagsOnCounter",
            "tomatoBagInHand",
            "lemonBagInHand",
            "choppingBoard",
            "foodLight",
            "mikePlateOnTable"
        };

        private static readonly string[] MikeCookBoolFields =
        {
            "isTVOn",
            "isPlayerSeated",
            "playerHasFish",
            "isSitting",
            "hasPlayerFinishedEating"
        };

        private static readonly string[] MikeCookEnumFields =
        {
            "currentState",
            "currentSittingState"
        };

        private static readonly string[] MikeCookRigFields =
        {
            "singleArmPickupRig",
            "standingPlateHoldRig"
        };

        private static readonly string[] MikeCookOuijaRigFields =
        {
            "tableHoldRig",
            "ouijaRig",
            "sittingHeadRigBasementTable",
            "basementSoundRig"
        };

        private static readonly string[] OvenBoolFields =
        {
            "isCasseroleCooked",
            "isCasserolePlaced",
            "ovenStartSoundPlayed",
            "hasOvenOpenCompleted",
            "ovenCanTurnOn",
            "ovenCookingCompleted"
        };

        private static readonly string[] LivingRoomTvBoolFields =
        {
            "isTurnedOn",
            "canBeTurnedOff",
            "isPlayingNonEatingSequenceVideo",
            "isInEatingSequence"
        };

        private static readonly string[] LivingRoomTvActiveFields =
        {
            "tvLight",
            "videoMesh"
        };

        private static readonly string[] OuijaBoolFields =
        {
            "canTakeMouseInput",
            "startRound",
            "movingToYesPoint",
            "hasStartedMovingToYes",
            "hasReachedYesPoint",
            "startMoveYesTimer",
            "canMoveToNextTarget",
            "canRandomlyMove",
            "hasReachedRandomPoint",
            "parentIsMoving"
        };

        private static readonly string[] OuijaIntFields =
        {
            "roundIndex"
        };

        private static readonly string[] OuijaScaledFloatFields =
        {
            "randomPositionMoveTimer",
            "moveToYesPointTimer",
            "roundTimer",
            "timeBetweenRounds",
            "timeAfterWhichMoveToYesPoint",
            "mouseX",
            "mouseY"
        };

        private static readonly Dictionary<string, FieldInfo> FieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private static readonly Dictionary<int, string> LivingRoomTvClipSignatureByInstance = new Dictionary<int, string>();
        private static readonly Dictionary<int, int> LivingRoomTvHostClipHashByInstance = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> LivingRoomTvHostTimeMsByInstance = new Dictionary<int, int>();

        public static int EmitHostFlags(CabinGameManager manager, string fullPrefix, Action<string, int> emit)
        {
            if (manager == null || emit == null) return 0;

            var hash = 17;
            var hideCookingVisuals = ShouldHideCookingVisuals(manager);
            var casserole = GetCasserole(manager);
            if (casserole != null)
            {
                Emit(fullPrefix + CasserolePrefix + "RootActive", casserole.gameObject.activeSelf && !hideCookingVisuals ? 1 : 0, emit, ref hash);
                Emit(fullPrefix + CasserolePrefix + "PickableEnabled", !hideCookingVisuals && IsBehaviourEnabled(casserole, "pickableComponent") ? 1 : 0, emit, ref hash);
                Emit(fullPrefix + CasserolePrefix + "PointLight", !hideCookingVisuals && IsLightEnabled(casserole, "pointLight") ? 1 : 0, emit, ref hash);
                Emit(fullPrefix + CasserolePrefix + "IngredientMask", hideCookingVisuals ? 0 : BuildIngredientMask(casserole), emit, ref hash);

                for (var i = 0; i < CasseroleBoolFields.Length; i++)
                {
                    var name = CasseroleBoolFields[i];
                    Emit(fullPrefix + CasserolePrefix + name, GetBool(casserole, name) ? 1 : 0, emit, ref hash);
                }

                for (var i = 0; i < CasseroleActiveFields.Length; i++)
                {
                    var name = CasseroleActiveFields[i];
                    Emit(fullPrefix + CasseroleActivePrefix + name, !hideCookingVisuals && GetObjectActive(casserole, name) ? 1 : 0, emit, ref hash);
                }
            }

            var oven = GetOven(manager);
            if (oven != null)
            {
                Emit(fullPrefix + OvenPrefix + "RootActive", oven.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                for (var i = 0; i < OvenBoolFields.Length; i++)
                {
                    var name = OvenBoolFields[i];
                    Emit(fullPrefix + OvenPrefix + name, GetBool(oven, name) ? 1 : 0, emit, ref hash);
                }

                Emit(fullPrefix + OvenActivePrefix + "doorNotInteractibleTrigger", GetObjectActive(oven, "doorNotInteractibleTrigger") ? 1 : 0, emit, ref hash);
            }

            var cook = GetMikeCookController(manager);
            if (cook != null)
            {
                for (var i = 0; i < MikeCookBoolFields.Length; i++)
                {
                    var name = MikeCookBoolFields[i];
                    Emit(fullPrefix + MikeCookPrefix + name, GetBool(cook, name) ? 1 : 0, emit, ref hash);
                }

                for (var i = 0; i < MikeCookEnumFields.Length; i++)
                {
                    var name = MikeCookEnumFields[i];
                    Emit(fullPrefix + MikeCookPrefix + name, GetInt(cook, name), emit, ref hash);
                }

                for (var i = 0; i < MikeCookPropFields.Length; i++)
                {
                    var name = MikeCookPropFields[i];
                    Emit(fullPrefix + MikeCookActivePrefix + name, !hideCookingVisuals && GetObjectActive(cook, name) ? 1 : 0, emit, ref hash);
                }

                Emit(fullPrefix + MikeCookPrefix + "PlateMask", hideCookingVisuals ? 0 : BuildGameObjectArrayMask(cook, "plates"), emit, ref hash);

                for (var i = 0; i < MikeCookRigFields.Length; i++)
                {
                    var name = MikeCookRigFields[i];
                    Emit(fullPrefix + MikeCookRigPrefix + name, Mathf.RoundToInt(GetRigWeight(cook, name) * 1000f), emit, ref hash);
                }

                for (var i = 0; i < MikeCookOuijaRigFields.Length; i++)
                {
                    var name = MikeCookOuijaRigFields[i];
                    Emit(fullPrefix + MikeCookRigPrefix + name, Mathf.RoundToInt(GetRigWeight(cook, name) * 1000f), emit, ref hash);
                }

                Emit(fullPrefix + MikeCookPrefix + "OuijaTableActive", GetObjectActive(cook, "ouijaTable") ? 1 : 0, emit, ref hash);
            }

            var tv = GetLivingRoomTv(manager);
            if (tv != null)
            {
                Emit(fullPrefix + LivingRoomTvPrefix + "RootActive", tv.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(fullPrefix + LivingRoomTvPrefix + "currentVideoIndex", GetInt(tv, "currentVideoIndex"), emit, ref hash);
                Emit(fullPrefix + LivingRoomTvPrefix + "CurrentClipHash", GetLivingRoomTvClipHash(tv), emit, ref hash);
                EmitTransient(fullPrefix + LivingRoomTvPrefix + "VideoTimeMs", QuantizeMs(GetLivingRoomTvTimeMs(tv), TvTimeQuantizeMs), emit);
                Emit(fullPrefix + LivingRoomTvPrefix + "MeshRendererEnabled", GetRendererEnabled(tv, "meshRenderer") ? 1 : 0, emit, ref hash);
                Emit(fullPrefix + LivingRoomTvPrefix + "AudioMuted", GetAudioMuted(tv, "tvAudioSource") ? 1 : 0, emit, ref hash);

                for (var i = 0; i < LivingRoomTvBoolFields.Length; i++)
                {
                    var name = LivingRoomTvBoolFields[i];
                    Emit(fullPrefix + LivingRoomTvPrefix + name, GetBool(tv, name) ? 1 : 0, emit, ref hash);
                }

                for (var i = 0; i < LivingRoomTvActiveFields.Length; i++)
                {
                    var name = LivingRoomTvActiveFields[i];
                    Emit(fullPrefix + LivingRoomTvActivePrefix + name, GetObjectActive(tv, name) ? 1 : 0, emit, ref hash);
                }
            }

            var ouija = GetOuijaController(manager);
            if (ouija != null)
            {
                Emit(fullPrefix + OuijaPrefix + "RootActive", ouija.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(fullPrefix + OuijaPrefix + "Enabled", ouija.enabled ? 1 : 0, emit, ref hash);
                Emit(fullPrefix + OuijaPrefix + "TargetCode", BuildOuijaTargetCode(ouija), emit, ref hash);

                for (var i = 0; i < OuijaBoolFields.Length; i++)
                {
                    var name = OuijaBoolFields[i];
                    Emit(fullPrefix + OuijaPrefix + name, GetBool(ouija, name) ? 1 : 0, emit, ref hash);
                }

                for (var i = 0; i < OuijaIntFields.Length; i++)
                {
                    var name = OuijaIntFields[i];
                    Emit(fullPrefix + OuijaPrefix + name, GetInt(ouija, name), emit, ref hash);
                }

                for (var i = 0; i < OuijaScaledFloatFields.Length; i++)
                {
                    var name = OuijaScaledFloatFields[i];
                    EmitTransient(fullPrefix + OuijaPrefix + name, QuantizeOuijaScaledFloat(name, Mathf.RoundToInt(GetFloat(ouija, name) * 1000f)), emit);
                }
            }

            return hash;
        }

        public static Transform GetCasseroleTransform(CabinGameManager manager)
        {
            var casserole = GetCasserole(manager);
            return casserole != null ? casserole.transform : null;
        }

        public static bool ShouldHideCookingVisuals(CabinGameManager manager)
        {
            if (manager == null) return false;

            switch (manager.CurrentSequence)
            {
                case SequenceType.PickingBoardGame:
                case SequenceType.PlayingJenga:
                case SequenceType.GoingToPlayOuija:
                case SequenceType.PlayingOuija:
                    return true;
                default:
                    return false;
            }
        }

        public static void ApplyClientPhaseVisuals(CabinGameManager manager, ManualLogSource logger)
        {
            if (!ShouldHideCookingVisuals(manager)) return;

            var casserole = GetCasserole(manager);
            if (casserole != null)
            {
                casserole.gameObject.SetActive(false);
                TrySetBehaviourEnabled(casserole, "pickableComponent", false);
                TrySetLightEnabled(casserole, "pointLight", false);
                TryApplyIngredientMask(casserole, 0);

                for (var i = 0; i < CasseroleActiveFields.Length; i++)
                {
                    TrySetObjectActive(casserole, CasseroleActiveFields[i], false);
                }
            }

            var cook = GetMikeCookController(manager);
            if (cook != null)
            {
                HideMikeCookProps(cook, manager.CurrentSequence);
            }
        }

        public static void CollectSyncedTransforms(CabinGameManager manager, List<Transform> transforms)
        {
            if (manager == null || transforms == null) return;

            if (!ShouldHideCookingVisuals(manager))
            {
                var casserole = GetCasseroleTransform(manager);
                if (casserole != null)
                {
                    AddUniqueTransform(transforms, casserole);
                }
            }

            var sequence = manager.CurrentSequence;
            if (sequence == SequenceType.GoingToPlayOuija || sequence == SequenceType.PlayingOuija)
            {
                var cook = GetMikeCookController(manager);
                var ouijaTable = GetFieldValue<Transform>(cook, "ouijaTable");
                if (ouijaTable != null)
                {
                    AddUniqueTransform(transforms, ouijaTable);
                }

                var ouija = GetOuijaController(manager);
                AddUniqueTransform(transforms, GetFieldValue<Transform>(ouija, "parentTransform"));
                AddUniqueTransform(transforms, GetFieldValue<Transform>(ouija, "planchetteTransform"));
            }
        }

        public static bool TryApplyFlag(CabinGameManager manager, string fieldName, int value, ManualLogSource logger)
        {
            if (manager == null || string.IsNullOrEmpty(fieldName) ||
                !fieldName.StartsWith(KeyPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            var casserole = GetCasserole(manager);
            var oven = GetOven(manager);
            var applied = false;

            if (fieldName.StartsWith(CasseroleActivePrefix, StringComparison.Ordinal))
            {
                if (casserole == null) return value == 0;
                applied = TrySetObjectActive(casserole, fieldName.Substring(CasseroleActivePrefix.Length), value != 0);
                ApplyCasseroleMaterials(casserole);
            }
            else if (fieldName.StartsWith(CasserolePrefix, StringComparison.Ordinal))
            {
                if (casserole == null) return value == 0;
                var name = fieldName.Substring(CasserolePrefix.Length);
                if (string.Equals(name, "RootActive", StringComparison.Ordinal))
                {
                    casserole.gameObject.SetActive(value != 0);
                    applied = true;
                }
                else if (string.Equals(name, "PickableEnabled", StringComparison.Ordinal))
                {
                    applied = TrySetBehaviourEnabled(casserole, "pickableComponent", value != 0);
                }
                else if (string.Equals(name, "PointLight", StringComparison.Ordinal))
                {
                    applied = TrySetLightEnabled(casserole, "pointLight", value != 0);
                }
                else if (string.Equals(name, "IngredientMask", StringComparison.Ordinal))
                {
                    applied = TryApplyIngredientMask(casserole, value);
                }
                else
                {
                    applied = TrySetBool(casserole, name, value != 0);
                }

                ApplyCasseroleMaterials(casserole);
                ApplyOvenPlacement(casserole, oven);
            }
            else if (fieldName.StartsWith(OvenActivePrefix, StringComparison.Ordinal))
            {
                if (oven == null) return value == 0;
                applied = TrySetObjectActive(oven, fieldName.Substring(OvenActivePrefix.Length), value != 0);
            }
            else if (fieldName.StartsWith(OvenPrefix, StringComparison.Ordinal))
            {
                if (oven == null) return value == 0;
                var name = fieldName.Substring(OvenPrefix.Length);
                if (string.Equals(name, "RootActive", StringComparison.Ordinal))
                {
                    oven.gameObject.SetActive(value != 0);
                    applied = true;
                }
                else
                {
                    applied = TrySetBool(oven, name, value != 0);
                }

                ApplyOvenPlacement(casserole, oven);
            }
            else if (fieldName.StartsWith(MikeCookActivePrefix, StringComparison.Ordinal))
            {
                var cook = GetMikeCookController(manager);
                if (cook == null) return value == 0;
                applied = TrySetObjectActive(cook, fieldName.Substring(MikeCookActivePrefix.Length), value != 0);
                ApplyMikeCookVisualState(cook);
            }
            else if (fieldName.StartsWith(MikeCookRigPrefix, StringComparison.Ordinal))
            {
                var cook = GetMikeCookController(manager);
                if (cook == null) return value == 0;
                TrySetRigWeight(cook, fieldName.Substring(MikeCookRigPrefix.Length), Mathf.Clamp01(value / 1000f));
                applied = true;
                ApplyMikeCookVisualState(cook);
            }
            else if (fieldName.StartsWith(MikeCookPrefix, StringComparison.Ordinal))
            {
                var cook = GetMikeCookController(manager);
                if (cook == null) return value == 0;
                var name = fieldName.Substring(MikeCookPrefix.Length);
                if (string.Equals(name, "PlateMask", StringComparison.Ordinal))
                {
                    applied = TryApplyGameObjectArrayMask(cook, "plates", value);
                }
                else if (string.Equals(name, "OuijaTableActive", StringComparison.Ordinal))
                {
                    applied = TrySetObjectActive(cook, "ouijaTable", value != 0);
                }
                else
                {
                    applied = TrySetBool(cook, name, value != 0) || TrySetInt(cook, name, value);
                }

                ApplyMikeCookVisualState(cook);
            }
            else if (fieldName.StartsWith(LivingRoomTvActivePrefix, StringComparison.Ordinal))
            {
                var tv = GetLivingRoomTv(manager);
                if (tv == null) return value == 0;
                applied = TrySetObjectActive(tv, fieldName.Substring(LivingRoomTvActivePrefix.Length), value != 0);
                ApplyLivingRoomTvVisualState(tv);
            }
            else if (fieldName.StartsWith(LivingRoomTvPrefix, StringComparison.Ordinal))
            {
                var tv = GetLivingRoomTv(manager);
                if (tv == null) return value == 0;
                var name = fieldName.Substring(LivingRoomTvPrefix.Length);
                if (string.Equals(name, "RootActive", StringComparison.Ordinal))
                {
                    tv.gameObject.SetActive(value != 0);
                    applied = true;
                }
                else if (string.Equals(name, "MeshRendererEnabled", StringComparison.Ordinal))
                {
                    applied = TrySetRendererEnabled(tv, "meshRenderer", value != 0);
                }
                else if (string.Equals(name, "AudioMuted", StringComparison.Ordinal))
                {
                    applied = TrySetAudioMuted(tv, "tvAudioSource", value != 0);
                }
                else if (string.Equals(name, "CurrentClipHash", StringComparison.Ordinal))
                {
                    LivingRoomTvHostClipHashByInstance[tv.GetInstanceID()] = value;
                    applied = true;
                }
                else if (string.Equals(name, "VideoTimeMs", StringComparison.Ordinal))
                {
                    LivingRoomTvHostTimeMsByInstance[tv.GetInstanceID()] = value;
                    applied = true;
                }
                else
                {
                    applied = TrySetBool(tv, name, value != 0) || TrySetInt(tv, name, value);
                }

                ApplyLivingRoomTvVisualState(tv);
            }
            else if (fieldName.StartsWith(OuijaActivePrefix, StringComparison.Ordinal))
            {
                var ouija = GetOuijaController(manager);
                if (ouija == null) return value == 0;
                applied = TrySetObjectActive(ouija, fieldName.Substring(OuijaActivePrefix.Length), value != 0);
                SuppressLocalOuijaBrain(ouija);
            }
            else if (fieldName.StartsWith(OuijaPrefix, StringComparison.Ordinal))
            {
                var ouija = GetOuijaController(manager);
                if (ouija == null) return value == 0;
                var name = fieldName.Substring(OuijaPrefix.Length);
                if (string.Equals(name, "RootActive", StringComparison.Ordinal))
                {
                    ouija.gameObject.SetActive(value != 0);
                    applied = true;
                }
                else if (string.Equals(name, "Enabled", StringComparison.Ordinal))
                {
                    SuppressLocalOuijaBrain(ouija);
                    applied = true;
                }
                else if (string.Equals(name, "TargetCode", StringComparison.Ordinal))
                {
                    applied = ApplyOuijaTargetCode(ouija, value);
                }
                else
                {
                    applied = TrySetBool(ouija, name, value != 0) ||
                              TrySetInt(ouija, name, value) ||
                              TrySetFloat(ouija, name, value / 1000f);
                }

                SuppressLocalOuijaBrain(ouija);
            }

            if (!applied && logger != null)
            {
                logger.LogWarning("Cabin cooking prop apply skipped key=" + fieldName);
            }

            ApplyClientPhaseVisuals(manager, logger);
            return applied;
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

        private static CasseroleDish GetCasserole(CabinGameManager manager)
        {
            return GetFieldValue<CasseroleDish>(manager, "casserole") ?? UnityEngine.Object.FindObjectOfType<CasseroleDish>();
        }

        private static Oven GetOven(CabinGameManager manager)
        {
            return GetFieldValue<Oven>(manager, "oven") ?? UnityEngine.Object.FindObjectOfType<Oven>();
        }

        private static MikeCabinCookController GetMikeCookController(CabinGameManager manager)
        {
            return GetFieldValue<MikeCabinCookController>(manager, "mikeController") ??
                   UnityEngine.Object.FindObjectOfType<MikeCabinCookController>();
        }

        private static InteractableTV GetLivingRoomTv(CabinGameManager manager)
        {
            var tv = GetFieldValue<InteractableTV>(manager, "livingRoomTV");
            if (tv != null) return tv;

            var all = UnityEngine.Object.FindObjectsOfType<InteractableTV>();
            for (var i = 0; i < all.Length; i++)
            {
                if (IsLivingRoomTv(all[i]))
                {
                    return all[i];
                }
            }

            return null;
        }

        private static bool IsLivingRoomTv(InteractableTV tv)
        {
            if (tv == null) return false;

            var typeField = FindField(tv, "type");
            if (typeField != null)
            {
                try
                {
                    var raw = typeField.GetValue(tv);
                    if (raw != null && string.Equals(raw.ToString(), "LivingRoom", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                }
            }

            var path = NetPath.GetPath(tv.transform);
            return !string.IsNullOrEmpty(path) &&
                   path.IndexOf("Living", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static CabinPlayerController GetCabinPlayerController(CabinGameManager manager)
        {
            return GetFieldValue<CabinPlayerController>(manager, "cabinPlayerController") ??
                   UnityEngine.Object.FindObjectOfType<CabinPlayerController>();
        }

        private static OuijaController GetOuijaController(CabinGameManager manager)
        {
            var ouija = GetFieldValue<OuijaController>(manager, "ouijaController");
            if (ouija != null) return ouija;

            var player = GetCabinPlayerController(manager);
            ouija = GetFieldValue<OuijaController>(player, "ouijaBoardGame");
            return ouija ?? UnityEngine.Object.FindObjectOfType<OuijaController>();
        }

        private static void AddUniqueTransform(List<Transform> transforms, Transform transform)
        {
            if (transforms == null || transform == null) return;
            for (var i = 0; i < transforms.Count; i++)
            {
                if (transforms[i] == transform) return;
            }

            transforms.Add(transform);
        }

        private static int QuantizeMs(int value, int quantum)
        {
            if (value <= 0 || quantum <= 1) return Mathf.Max(0, value);
            return (value / quantum) * quantum;
        }

        private static int QuantizeOuijaScaledFloat(string name, int value)
        {
            var quantum = string.Equals(name, "mouseX", StringComparison.Ordinal) ||
                          string.Equals(name, "mouseY", StringComparison.Ordinal)
                ? 100
                : 250;
            if (value >= 0) return QuantizeMs(value, quantum);
            return -QuantizeMs(-value, quantum);
        }

        private static int GetLivingRoomTvClipHash(InteractableTV tv)
        {
            var videoPlayer = GetFieldObject(tv, "videoPlayer");
            var clip = GetPropertyObject(videoPlayer, "clip");
            var name = GetObjectName(clip);
            if (!string.IsNullOrEmpty(name))
            {
                return StableHash(name);
            }

            var index = GetInt(tv, "currentVideoIndex");
            var nonEating = GetBool(tv, "isPlayingNonEatingSequenceVideo");
            var list = GetLivingRoomTvClipList(tv, nonEating);
            var indexed = GetListItem(list, index);
            return StableHash(GetObjectName(indexed));
        }

        private static int GetLivingRoomTvTimeMs(InteractableTV tv)
        {
            var videoPlayer = GetFieldObject(tv, "videoPlayer");
            if (TryGetDoubleProperty(videoPlayer, "time", out var time) && time >= 0d)
            {
                return Mathf.Clamp(Mathf.RoundToInt((float)(time * 1000d)), 0, int.MaxValue);
            }

            return Mathf.Max(0, Mathf.RoundToInt(GetFloat(tv, "videoTimer") * 1000f));
        }

        private static void ApplyLivingRoomTvTime(InteractableTV tv, object videoPlayer, int hostTimeMs)
        {
            if (tv == null || videoPlayer == null || hostTimeMs <= 0) return;

            var targetSeconds = hostTimeMs / 1000d;
            if (TryGetDoubleProperty(videoPlayer, "time", out var currentSeconds) &&
                Math.Abs(currentSeconds - targetSeconds) < 0.35d)
            {
                SetFieldValue(tv, "videoTimer", hostTimeMs / 1000f);
                return;
            }

            TrySetDoubleProperty(videoPlayer, "time", targetSeconds);
            SetFieldValue(tv, "videoTimer", hostTimeMs / 1000f);
        }

        private static bool TryFindLivingRoomTvClip(InteractableTV tv, bool nonEating, int clipHash, out object clip)
        {
            clip = null;
            if (tv == null || clipHash == 0) return false;

            var list = GetLivingRoomTvClipList(tv, nonEating);
            var enumerable = list as IEnumerable;
            if (enumerable == null) return false;

            foreach (var candidate in enumerable)
            {
                if (StableHash(GetObjectName(candidate)) == clipHash)
                {
                    clip = candidate;
                    return true;
                }
            }

            return false;
        }

        private static int ResolveLivingRoomTvClipIndex(InteractableTV tv, bool nonEating, int clipHash, int fallbackIndex)
        {
            var list = GetLivingRoomTvClipList(tv, nonEating) as IList;
            if (list == null) return fallbackIndex;

            for (var i = 0; i < list.Count; i++)
            {
                if (StableHash(GetObjectName(list[i])) == clipHash)
                {
                    return i;
                }
            }

            return fallbackIndex;
        }

        private static object GetLivingRoomTvClipList(InteractableTV tv, bool nonEating)
        {
            return GetFieldObject(tv, nonEating ? "nonEatingSequenceVideos" : "eatingSequenceVideos");
        }

        private static object GetListItem(object listObject, int index)
        {
            var list = listObject as IList;
            if (list == null || index < 0 || index >= list.Count) return null;
            try
            {
                return list[index];
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void SetVideoPlayerClip(object videoPlayer, object clip)
        {
            if (videoPlayer == null || clip == null) return;

            try
            {
                var property = videoPlayer.GetType().GetProperty("clip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(videoPlayer, clip, null);
                }
            }
            catch (Exception)
            {
                // Best-effort media correction only.
            }
        }

        private static bool TryGetDoubleProperty(object target, string propertyName, out double value)
        {
            value = 0d;
            if (target == null || string.IsNullOrEmpty(propertyName)) return false;

            try
            {
                var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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
                var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property == null || !property.CanWrite) return false;
                property.SetValue(target, value, null);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static object GetPropertyObject(object target, string propertyName)
        {
            if (target == null || string.IsNullOrEmpty(propertyName)) return null;

            try
            {
                var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return property != null && property.CanRead ? property.GetValue(target, null) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GetObjectName(object obj)
        {
            if (obj is UnityEngine.Object unityObject)
            {
                return unityObject.name ?? string.Empty;
            }

            return string.Empty;
        }

        private static int StableHash(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;

            unchecked
            {
                var hash = (int)2166136261;
                for (var i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= 16777619;
                }

                return hash == 0 ? 1 : hash;
            }
        }

        private static int BuildOuijaTargetCode(OuijaController ouija)
        {
            if (ouija == null) return -1;

            var current = GetFieldValue<Transform>(ouija, "currentTarget");
            if (current == null) return -1;
            if (current == GetFieldValue<Transform>(ouija, "yesTransformPoint")) return -2;
            if (current == GetFieldValue<Transform>(ouija, "noTransformPoint")) return -3;

            var randomPoints = GetFieldValue<Array>(ouija, "randomPointsOnBoardToMoveTowards");
            if (randomPoints == null) return -1;
            for (var i = 0; i < randomPoints.Length; i++)
            {
                var point = randomPoints.GetValue(i) as Transform;
                if (current == point)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool ApplyOuijaTargetCode(OuijaController ouija, int code)
        {
            if (ouija == null) return false;

            Transform target = null;
            if (code == -2)
            {
                target = GetFieldValue<Transform>(ouija, "yesTransformPoint");
            }
            else if (code == -3)
            {
                target = GetFieldValue<Transform>(ouija, "noTransformPoint");
            }
            else if (code >= 0)
            {
                var randomPoints = GetFieldValue<Array>(ouija, "randomPointsOnBoardToMoveTowards");
                if (randomPoints != null && code < randomPoints.Length)
                {
                    target = randomPoints.GetValue(code) as Transform;
                }
            }

            return SetFieldValue(ouija, "currentTarget", target);
        }

        private static void SuppressLocalOuijaBrain(OuijaController ouija)
        {
            if (ouija == null) return;

            ouija.enabled = false;
            var audio = GetFieldValue<AudioSource>(ouija, "dragSFXAudioSource");
            if (audio != null)
            {
                audio.volume = 0f;
                if (audio.isPlaying) audio.Stop();
            }
        }

        private static void HideMikeCookProps(MikeCabinCookController cook, SequenceType sequence)
        {
            for (var i = 0; i < MikeCookPropFields.Length; i++)
            {
                TrySetObjectActive(cook, MikeCookPropFields[i], false);
            }

            var plates = GetFieldValue<Array>(cook, "plates");
            if (plates != null)
            {
                for (var i = 0; i < plates.Length; i++)
                {
                    var gameObject = ExtractGameObject(plates.GetValue(i));
                    if (gameObject != null)
                    {
                        gameObject.SetActive(false);
                    }
                }
            }

            if (sequence != SequenceType.PlayingJenga)
            {
                TrySetObjectActive(cook, "jengaPieceInHand", false);
            }

            for (var i = 0; i < MikeCookRigFields.Length; i++)
            {
                TrySetRigWeight(cook, MikeCookRigFields[i], 0f);
            }

            TrySetAnimatorLayerWeight(cook, "veggiesUpperBodyMaskLayerInt", 0f);
            TrySetAnimatorLayerWeight(cook, "casseroleUpperBodyMaskLayerInt", 0f);
        }

        private static void ApplyMikeCookVisualState(MikeCabinCookController cook)
        {
            if (cook == null) return;

            if (GetBool(cook, "isSitting"))
            {
                var holdPoint = GetFieldValue<Transform>(cook, "doubleHandHoldPoint");
                var sitPoint = GetFieldValue<Transform>(cook, "doubleHandHoldSitPosition");
                if (holdPoint != null && sitPoint != null)
                {
                    holdPoint.SetPositionAndRotation(sitPoint.position, sitPoint.rotation);
                }
            }
        }

        private static void ApplyLivingRoomTvVisualState(InteractableTV tv)
        {
            if (tv == null) return;

            var isOn = GetBool(tv, "isTurnedOn");
            tv.enabled = false;

            var renderer = GetFieldValue<MeshRenderer>(tv, "meshRenderer");
            if (renderer != null)
            {
                renderer.enabled = isOn || GetRendererEnabled(tv, "meshRenderer");
                var material = GetFieldValue<Material>(tv, isOn ? "tvOnMaterial" : "tvOffMaterial");
                if (material != null)
                {
                    renderer.material = material;
                }
            }

            var videoMesh = GetFieldValue<GameObject>(tv, "videoMesh");
            if (videoMesh != null)
            {
                videoMesh.SetActive(isOn);
            }

            var tvLight = GetFieldValue<GameObject>(tv, "tvLight");
            if (tvLight != null && !isOn)
            {
                tvLight.SetActive(false);
            }

            var audioSource = GetFieldValue<AudioSource>(tv, "tvAudioSource");
            if (audioSource != null)
            {
                audioSource.mute = !isOn;
            }

            var tvPlaying = GetFieldValue<AudioSource>(tv, "tvPlaying");
            if (tvPlaying != null)
            {
                if (isOn && !tvPlaying.isPlaying) tvPlaying.Play();
                if (!isOn && tvPlaying.isPlaying) tvPlaying.Stop();
            }

            ApplyLivingRoomTvClip(tv, isOn);
        }

        private static void ApplyLivingRoomTvClip(InteractableTV tv, bool isOn)
        {
            var videoPlayer = GetFieldObject(tv, "videoPlayer");
            if (!isOn)
            {
                InvokeNoArg(videoPlayer, "Stop");
                return;
            }

            var index = GetInt(tv, "currentVideoIndex");
            var nonEating = GetBool(tv, "isPlayingNonEatingSequenceVideo");
            var id = tv.GetInstanceID();
            LivingRoomTvHostClipHashByInstance.TryGetValue(id, out var clipHash);
            LivingRoomTvHostTimeMsByInstance.TryGetValue(id, out var hostTimeMs);

            var signature = (nonEating ? "nonEating:" : "eating:") + index + ":hash:" + clipHash;
            if (!LivingRoomTvClipSignatureByInstance.TryGetValue(id, out var last) ||
                !string.Equals(last, signature, StringComparison.Ordinal))
            {
                if (clipHash != 0 && TryFindLivingRoomTvClip(tv, nonEating, clipHash, out var clip))
                {
                    SetVideoPlayerClip(videoPlayer, clip);
                    TrySetInt(tv, "currentVideoIndex", ResolveLivingRoomTvClipIndex(tv, nonEating, clipHash, index));
                    SetFieldValue(tv, "videoTimer", hostTimeMs / 1000f);
                    InvokeNoArg(videoPlayer, "Play");
                }
                else
                {
                    InvokeInt(tv, nonEating ? "PlayVideo" : "PlayEatingVideo", index);
                }

                LivingRoomTvClipSignatureByInstance[id] = signature;
            }

            InvokeNoArg(videoPlayer, "Play");
            ApplyLivingRoomTvTime(tv, videoPlayer, hostTimeMs);
        }

        private static int BuildIngredientMask(CasseroleDish casserole)
        {
            var ingredients = GetFieldValue<Array>(casserole, "ingredients");
            if (ingredients == null) return 0;

            var mask = 0;
            for (var i = 0; i < ingredients.Length && i < 30; i++)
            {
                var component = ingredients.GetValue(i) as Component;
                if (component != null && component.gameObject.activeSelf)
                {
                    mask |= 1 << i;
                }
            }

            return mask;
        }

        private static bool TryApplyIngredientMask(CasseroleDish casserole, int mask)
        {
            var ingredients = GetFieldValue<Array>(casserole, "ingredients");
            if (ingredients == null) return false;

            for (var i = 0; i < ingredients.Length && i < 30; i++)
            {
                var component = ingredients.GetValue(i) as Component;
                if (component != null)
                {
                    component.gameObject.SetActive((mask & (1 << i)) != 0);
                }
            }

            return true;
        }

        private static int BuildGameObjectArrayMask(object target, string fieldName)
        {
            var objects = GetFieldValue<Array>(target, fieldName);
            if (objects == null) return 0;

            var mask = 0;
            for (var i = 0; i < objects.Length && i < 30; i++)
            {
                var gameObject = ExtractGameObject(objects.GetValue(i));
                if (gameObject != null && gameObject.activeSelf)
                {
                    mask |= 1 << i;
                }
            }

            return mask;
        }

        private static bool TryApplyGameObjectArrayMask(object target, string fieldName, int mask)
        {
            var objects = GetFieldValue<Array>(target, fieldName);
            if (objects == null) return false;

            for (var i = 0; i < objects.Length && i < 30; i++)
            {
                var gameObject = ExtractGameObject(objects.GetValue(i));
                if (gameObject != null)
                {
                    gameObject.SetActive((mask & (1 << i)) != 0);
                }
            }

            return true;
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

        private static int GetInt(object target, string fieldName)
        {
            var field = FindField(target, fieldName);
            if (field == null) return 0;
            try
            {
                var value = field.GetValue(target);
                if (value == null) return 0;
                if (field.FieldType == typeof(int)) return (int)value;
                if (field.FieldType.IsEnum) return Convert.ToInt32(value);
            }
            catch (Exception)
            {
                return 0;
            }

            return 0;
        }

        private static bool TrySetInt(object target, string fieldName, int value)
        {
            var field = FindField(target, fieldName);
            if (field == null) return false;
            try
            {
                if (field.FieldType == typeof(int))
                {
                    field.SetValue(target, value);
                    return true;
                }

                if (field.FieldType.IsEnum)
                {
                    field.SetValue(target, Enum.ToObject(field.FieldType, value));
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        private static float GetFloat(object target, string fieldName)
        {
            var field = FindField(target, fieldName);
            if (field == null) return 0f;
            try
            {
                var value = field.GetValue(target);
                if (value == null) return 0f;
                if (field.FieldType == typeof(float)) return (float)value;
                if (field.FieldType == typeof(int)) return (int)value;
            }
            catch (Exception)
            {
                return 0f;
            }

            return 0f;
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

        private static bool GetObjectActive(object target, string fieldName)
        {
            var obj = GetFieldObject(target, fieldName);
            var gameObject = ExtractGameObject(obj);
            return gameObject != null && gameObject.activeSelf;
        }

        private static bool TrySetObjectActive(object target, string fieldName, bool active)
        {
            var obj = GetFieldObject(target, fieldName);
            var gameObject = ExtractGameObject(obj);
            if (gameObject == null) return !active;
            gameObject.SetActive(active);
            return true;
        }

        private static float GetRigWeight(object target, string fieldName)
        {
            var rig = GetFieldObject(target, fieldName);
            if (rig == null) return 0f;

            var property = rig.GetType().GetProperty("weight", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanRead && property.PropertyType == typeof(float))
            {
                try
                {
                    return (float)property.GetValue(rig, null);
                }
                catch (Exception)
                {
                    return 0f;
                }
            }

            var field = rig.GetType().GetField("weight", FieldFlags);
            if (field == null || field.FieldType != typeof(float)) return 0f;
            try
            {
                return (float)field.GetValue(rig);
            }
            catch (Exception)
            {
                return 0f;
            }
        }

        private static void TrySetRigWeight(object target, string fieldName, float weight)
        {
            var rig = GetFieldObject(target, fieldName);
            if (rig == null) return;

            var property = rig.GetType().GetProperty("weight", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanWrite)
            {
                try
                {
                    property.SetValue(rig, weight, null);
                    return;
                }
                catch (Exception)
                {
                    return;
                }
            }

            var field = rig.GetType().GetField("weight", FieldFlags);
            if (field == null || field.FieldType != typeof(float)) return;
            try
            {
                field.SetValue(rig, weight);
            }
            catch (Exception)
            {
                // Best-effort visual cleanup only.
            }
        }

        private static bool GetRendererEnabled(object target, string fieldName)
        {
            var renderer = GetFieldObject(target, fieldName) as Renderer;
            return renderer != null && renderer.enabled;
        }

        private static bool TrySetRendererEnabled(object target, string fieldName, bool enabled)
        {
            var renderer = GetFieldObject(target, fieldName) as Renderer;
            if (renderer == null) return false;
            renderer.enabled = enabled;
            return true;
        }

        private static bool GetAudioMuted(object target, string fieldName)
        {
            var audioSource = GetFieldObject(target, fieldName) as AudioSource;
            return audioSource != null && audioSource.mute;
        }

        private static bool TrySetAudioMuted(object target, string fieldName, bool muted)
        {
            var audioSource = GetFieldObject(target, fieldName) as AudioSource;
            if (audioSource == null) return false;
            audioSource.mute = muted;
            return true;
        }

        private static void InvokeNoArg(object target, string methodName)
        {
            if (target == null || string.IsNullOrEmpty(methodName)) return;

            try
            {
                var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                method?.Invoke(target, null);
            }
            catch (Exception)
            {
                // Reflective media state is best-effort only.
            }
        }

        private static void InvokeInt(object target, string methodName, int value)
        {
            if (target == null || string.IsNullOrEmpty(methodName)) return;

            try
            {
                var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(int) }, null);
                method?.Invoke(target, new object[] { value });
            }
            catch (Exception)
            {
                // Reflective media state is best-effort only.
            }
        }

        private static void TrySetAnimatorLayerWeight(object target, string layerFieldName, float weight)
        {
            var animator = GetFieldValue<Animator>(target, "animator");
            var field = FindField(target, layerFieldName);
            if (animator == null || field == null || field.FieldType != typeof(int)) return;

            try
            {
                var layer = (int)field.GetValue(target);
                if (layer >= 0 && layer < animator.layerCount)
                {
                    animator.SetLayerWeight(layer, weight);
                }
            }
            catch (Exception)
            {
                // Best-effort visual cleanup only.
            }
        }

        private static bool IsBehaviourEnabled(object target, string fieldName)
        {
            var behaviour = GetFieldObject(target, fieldName) as Behaviour;
            if (behaviour == null && string.Equals(fieldName, "pickableComponent", StringComparison.Ordinal) && target is Component component)
            {
                behaviour = component.GetComponent<StandardPickable>();
            }
            return behaviour != null && behaviour.enabled;
        }

        private static bool TrySetBehaviourEnabled(object target, string fieldName, bool enabled)
        {
            var behaviour = GetFieldObject(target, fieldName) as Behaviour;
            if (behaviour == null && string.Equals(fieldName, "pickableComponent", StringComparison.Ordinal) && target is Component component)
            {
                behaviour = component.GetComponent<StandardPickable>();
            }
            if (behaviour == null) return false;
            behaviour.enabled = enabled;
            return true;
        }

        private static bool IsLightEnabled(object target, string fieldName)
        {
            var light = GetFieldObject(target, fieldName) as Light;
            return light != null && light.enabled;
        }

        private static bool TrySetLightEnabled(object target, string fieldName, bool enabled)
        {
            var light = GetFieldObject(target, fieldName) as Light;
            if (light == null) return false;
            light.enabled = enabled;
            return true;
        }

        private static void ApplyOvenPlacement(CasseroleDish casserole, Oven oven)
        {
            if (casserole == null || oven == null || !GetBool(casserole, "isPlacedInOven")) return;

            SetFieldValue(oven, "casserole", casserole);
            var placePoint = GetFieldValue<Transform>(oven, "placePoint");
            if (placePoint == null) return;

            casserole.transform.SetParent(placePoint, false);
            casserole.transform.localPosition = Vector3.zero;
            casserole.transform.localRotation = Quaternion.identity;
        }

        private static void ApplyCasseroleMaterials(CasseroleDish casserole)
        {
            if (casserole == null) return;

            var cooked = GetBool(casserole, "isCooked");
            var marinated = GetBool(casserole, "isMarinated");
            if (!cooked && !marinated) return;

            var fish1 = GetFieldValue<Renderer>(casserole, "fish1");
            var fish2 = GetFieldValue<Renderer>(casserole, "fish2");
            var container = GetFieldValue<Renderer>(casserole, "container");
            var veggies = GetFieldValue<Renderer>(casserole, "veggies");
            if (cooked)
            {
                SetRendererMaterial(fish1, GetFieldValue<Material>(casserole, "cookedFishMaterial"));
                SetRendererMaterial(fish2, GetFieldValue<Material>(casserole, "cookedFishMaterial"));
                SetRendererMaterial(veggies, GetFieldValue<Material>(casserole, "cookedVeggiesMaterial"));
                return;
            }

            SetRendererMaterial(fish1, GetFieldValue<Material>(casserole, "marinatedFishMaterial"));
            SetRendererMaterial(fish2, GetFieldValue<Material>(casserole, "marinatedFishMaterial"));
            SetRendererMaterial(container, GetFieldValue<Material>(casserole, "marinatedContainerMaterial"));
        }

        private static void SetRendererMaterial(Renderer renderer, Material material)
        {
            if (renderer == null || material == null) return;
            renderer.material = material;
        }

        private static GameObject ExtractGameObject(object obj)
        {
            if (obj == null) return null;
            if (obj is GameObject gameObject) return gameObject;
            if (obj is Component component) return component.gameObject;
            return null;
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

        private static T GetFieldValue<T>(object target, string fieldName) where T : class
        {
            return GetFieldObject(target, fieldName) as T;
        }

        private static bool SetFieldValue(object target, string fieldName, object value)
        {
            var field = FindField(target, fieldName);
            if (field == null) return false;
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

        private static FieldInfo FindField(object target, string fieldName)
        {
            if (target == null || string.IsNullOrEmpty(fieldName)) return null;
            var type = target.GetType();
            var key = type.FullName + "." + fieldName;
            if (FieldCache.TryGetValue(key, out var cached)) return cached;

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
    }
}
