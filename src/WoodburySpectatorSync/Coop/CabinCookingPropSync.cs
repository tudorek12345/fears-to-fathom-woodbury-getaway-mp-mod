using System;
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

        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

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

        private static readonly string[] MikeCookRigFields =
        {
            "singleArmPickupRig",
            "standingPlateHoldRig"
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

        private static readonly Dictionary<string, FieldInfo> FieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);

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
