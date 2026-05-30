using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace WoodburySpectatorSync.Coop
{
    internal static class SceneTrafficSync
    {
        public const string KeyPrefix = "Traffic.";

        private const string ControllerPrefix = KeyPrefix + "C";
        private const string TriggerPrefix = KeyPrefix + "T";
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Dictionary<string, FieldInfo> FieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private static long _nextPizzeriaSuppressLogMs;
        private static long _nextRoadTripSuppressLogMs;

        public static int EmitPizzeriaHostFlags(string fullPrefix, Action<string, int> emit)
        {
            var hash = EmitHostFlags(fullPrefix, FindPizzeriaControllers(), emit);
            EmitPizzeriaTriggers(fullPrefix, emit, ref hash);
            return hash;
        }

        public static int EmitRoadTripHostFlags(string fullPrefix, Action<string, int> emit)
        {
            var hash = EmitHostFlags(fullPrefix, FindRoadTripControllers(), emit);
            EmitRoadTripTriggers(fullPrefix, emit, ref hash);
            return hash;
        }

        public static void CollectPizzeriaTransforms(List<Transform> transforms)
        {
            CollectTransforms(FindPizzeriaControllers(), transforms);
        }

        public static void CollectRoadTripTransforms(List<Transform> transforms)
        {
            CollectTransforms(FindRoadTripControllers(), transforms);
        }

        public static bool TryApplyPizzeriaFlag(string fieldName, int value, ManualLogSource logger)
        {
            if (!IsTrafficKey(fieldName)) return false;
            SuppressPizzeriaTrafficBrain(logger);
            if (IsTriggerKey(fieldName))
            {
                return TryApplyPizzeriaTriggerFlag(fieldName, value);
            }

            return TryApplyFlag(fieldName, value, FindPizzeriaControllers(), logger);
        }

        public static bool TryApplyRoadTripFlag(string fieldName, int value, ManualLogSource logger)
        {
            if (!IsTrafficKey(fieldName)) return false;
            SuppressRoadTripTrafficBrain(logger);
            if (IsTriggerKey(fieldName))
            {
                return TryApplyRoadTripTriggerFlag(fieldName, value);
            }

            return TryApplyFlag(fieldName, value, FindRoadTripControllers(), logger);
        }

        public static void SuppressPizzeriaTrafficBrain(ManualLogSource logger)
        {
            var triggers = UnityEngine.Object.FindObjectsOfType<TrafficTrigger>();
            for (var i = 0; i < triggers.Length; i++)
            {
                if (triggers[i] != null) triggers[i].enabled = false;
            }

            var controllers = FindPizzeriaControllers();
            for (var i = 0; i < controllers.Count; i++)
            {
                SetCanSpawn(controllers[i], false);
            }

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (logger != null && nowMs >= _nextPizzeriaSuppressLogMs)
            {
                _nextPizzeriaSuppressLogMs = nowMs + 10000;
                logger.LogInfo("Pizzeria traffic client brain suppressed triggers=" + triggers.Length +
                               " controllers=" + controllers.Count);
            }
        }

        public static void SuppressRoadTripTrafficBrain(ManualLogSource logger)
        {
            var triggers = UnityEngine.Object.FindObjectsOfType<RoadTripTrafficTrigger>();
            for (var i = 0; i < triggers.Length; i++)
            {
                if (triggers[i] != null) triggers[i].enabled = false;
            }

            var controllers = FindRoadTripControllers();
            for (var i = 0; i < controllers.Count; i++)
            {
                SetCanSpawn(controllers[i], false);
            }

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (logger != null && nowMs >= _nextRoadTripSuppressLogMs)
            {
                _nextRoadTripSuppressLogMs = nowMs + 10000;
                logger.LogInfo("RoadTrip traffic client brain suppressed triggers=" + triggers.Length +
                               " controllers=" + controllers.Count);
            }
        }

        private static int EmitHostFlags(string fullPrefix, List<Component> controllers, Action<string, int> emit)
        {
            if (emit == null) return 0;

            var hash = 17;
            for (var controllerIndex = 0; controllerIndex < controllers.Count; controllerIndex++)
            {
                var controller = controllers[controllerIndex];
                if (controller == null) continue;

                var prefix = fullPrefix + ControllerPrefix + controllerIndex + ".";
                var instances = GetFieldValue<CarInALoop[]>(controller, "carInstances");
                var activeMask = 0;
                if (instances != null)
                {
                    for (var i = 0; i < instances.Length && i < 30; i++)
                    {
                        var car = instances[i];
                        if (car == null) continue;
                        if (car.gameObject.activeSelf) activeMask |= 1 << i;
                        Emit(prefix + "I" + i + ".PrefabHash", StableNameHash(GetBaseCarName(car.name)), emit, ref hash);
                    }
                }

                Emit(prefix + "RootActive", controller.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(prefix + "CanSpawn", GetCanSpawn(controller) ? 1 : 0, emit, ref hash);
                Emit(prefix + "ActiveMask", activeMask, emit, ref hash);
            }

            return hash;
        }

        private static void EmitPizzeriaTriggers(string fullPrefix, Action<string, int> emit, ref int hash)
        {
            if (emit == null) return;
            var triggers = UnityEngine.Object.FindObjectsOfType<TrafficTrigger>();
            SortByPath(triggers);
            Emit(fullPrefix + TriggerPrefix + "Count", triggers != null ? triggers.Length : 0, emit, ref hash);
            if (triggers == null) return;

            for (var i = 0; i < triggers.Length; i++)
            {
                var trigger = triggers[i];
                if (trigger == null) continue;
                var prefix = fullPrefix + TriggerPrefix + i + ".";
                Emit(prefix + "RootActive", trigger.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(prefix + "Enabled", trigger.enabled ? 1 : 0, emit, ref hash);
            }
        }

        private static void EmitRoadTripTriggers(string fullPrefix, Action<string, int> emit, ref int hash)
        {
            if (emit == null) return;
            var triggers = UnityEngine.Object.FindObjectsOfType<RoadTripTrafficTrigger>();
            SortByPath(triggers);
            Emit(fullPrefix + TriggerPrefix + "Count", triggers != null ? triggers.Length : 0, emit, ref hash);
            if (triggers == null) return;

            for (var i = 0; i < triggers.Length; i++)
            {
                var trigger = triggers[i];
                if (trigger == null) continue;
                var prefix = fullPrefix + TriggerPrefix + i + ".";
                Emit(prefix + "RootActive", trigger.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(prefix + "Enabled", trigger.enabled ? 1 : 0, emit, ref hash);
                Emit(prefix + "OnlyOnce", trigger.onlyOnce ? 1 : 0, emit, ref hash);
                Emit(prefix + "OverrideSpeed", trigger.overrideSpeed ? 1 : 0, emit, ref hash);
                Emit(prefix + "CarSpeed100", Mathf.RoundToInt(trigger.carSpeed * 100f), emit, ref hash);
                Emit(prefix + "Spline100", Mathf.RoundToInt(trigger.splineDisplacement * 100f), emit, ref hash);
            }
        }

        private static void CollectTransforms(List<Component> controllers, List<Transform> transforms)
        {
            if (transforms == null) return;

            for (var controllerIndex = 0; controllerIndex < controllers.Count; controllerIndex++)
            {
                var instances = GetFieldValue<CarInALoop[]>(controllers[controllerIndex], "carInstances");
                if (instances == null) continue;

                for (var i = 0; i < instances.Length; i++)
                {
                    var car = instances[i];
                    if (car == null || car.transform == null) continue;
                    transforms.Add(car.transform);
                }
            }
        }

        private static bool TryApplyFlag(string fieldName, int value, List<Component> controllers, ManualLogSource logger)
        {
            var localKey = fieldName.Substring(KeyPrefix.Length);
            if (!localKey.StartsWith("C", StringComparison.Ordinal)) return true;
            var dot = localKey.IndexOf('.');
            if (dot <= 1) return true;

            if (!int.TryParse(localKey.Substring(1, dot - 1), out var controllerIndex))
            {
                return true;
            }

            if (controllerIndex < 0 || controllerIndex >= controllers.Count)
            {
                return false;
            }

            var controller = controllers[controllerIndex];
            if (controller == null) return false;

            var name = localKey.Substring(dot + 1);
            if (string.Equals(name, "RootActive", StringComparison.Ordinal))
            {
                controller.gameObject.SetActive(value != 0);
                return true;
            }

            if (string.Equals(name, "CanSpawn", StringComparison.Ordinal))
            {
                SetCanSpawn(controller, false);
                return true;
            }

            if (string.Equals(name, "ActiveMask", StringComparison.Ordinal))
            {
                ApplyActiveMask(controller, value);
                return true;
            }

            if (name.StartsWith("I", StringComparison.Ordinal))
            {
                var nextDot = name.IndexOf('.');
                if (nextDot <= 1) return true;
                if (!int.TryParse(name.Substring(1, nextDot - 1), out var slot)) return true;
                var slotField = name.Substring(nextDot + 1);
                if (string.Equals(slotField, "PrefabHash", StringComparison.Ordinal))
                {
                    return EnsurePoolSlotPrefab(controller, slot, value, logger);
                }
            }

            return true;
        }

        private static bool TryApplyPizzeriaTriggerFlag(string fieldName, int value)
        {
            var localKey = fieldName.Substring(KeyPrefix.Length);
            if (string.Equals(localKey, "TCount", StringComparison.Ordinal)) return true;
            if (!TryParseTriggerKey(localKey, out var index, out var name)) return true;

            var triggers = UnityEngine.Object.FindObjectsOfType<TrafficTrigger>();
            SortByPath(triggers);
            if (index < 0 || index >= triggers.Length) return false;
            var trigger = triggers[index];
            if (trigger == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { trigger.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { trigger.enabled = false; return true; }
            return true;
        }

        private static bool TryApplyRoadTripTriggerFlag(string fieldName, int value)
        {
            var localKey = fieldName.Substring(KeyPrefix.Length);
            if (string.Equals(localKey, "TCount", StringComparison.Ordinal)) return true;
            if (!TryParseTriggerKey(localKey, out var index, out var name)) return true;

            var triggers = UnityEngine.Object.FindObjectsOfType<RoadTripTrafficTrigger>();
            SortByPath(triggers);
            if (index < 0 || index >= triggers.Length) return false;
            var trigger = triggers[index];
            if (trigger == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { trigger.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { trigger.enabled = false; return true; }
            if (string.Equals(name, "OnlyOnce", StringComparison.Ordinal)) { trigger.onlyOnce = value != 0; return true; }
            if (string.Equals(name, "OverrideSpeed", StringComparison.Ordinal)) { trigger.overrideSpeed = value != 0; return true; }
            if (string.Equals(name, "CarSpeed100", StringComparison.Ordinal)) { trigger.carSpeed = value / 100f; return true; }
            if (string.Equals(name, "Spline100", StringComparison.Ordinal)) { trigger.splineDisplacement = value / 100f; return true; }
            return true;
        }

        private static void ApplyActiveMask(Component controller, int mask)
        {
            var instances = GetFieldValue<CarInALoop[]>(controller, "carInstances");
            if (instances == null) return;

            for (var i = 0; i < instances.Length && i < 30; i++)
            {
                var car = instances[i];
                if (car == null || car.gameObject == null) continue;
                var active = (mask & (1 << i)) != 0;
                car.gameObject.SetActive(active);
                car.carIsActive = false;
            }
        }

        private static bool EnsurePoolSlotPrefab(Component controller, int slot, int expectedHash, ManualLogSource logger)
        {
            var instances = GetFieldValue<CarInALoop[]>(controller, "carInstances");
            if (instances == null || slot < 0 || slot >= instances.Length) return false;

            var current = instances[slot];
            if (current != null && StableNameHash(GetBaseCarName(current.name)) == expectedHash)
            {
                current.carIsActive = false;
                return true;
            }

            var prefab = FindPrefabByHash(controller, expectedHash);
            if (prefab == null)
            {
                logger?.LogWarning("Scene traffic prefab hash missing controller=" +
                                   NetPath.GetPath(controller.transform) +
                                   " slot=" + slot +
                                   " hash=" + expectedHash);
                return true;
            }

            var siblingIndex = current != null && current.transform != null
                ? current.transform.GetSiblingIndex()
                : slot;
            var replacement = UnityEngine.Object.Instantiate(prefab);
            if (replacement == null) return false;

            if (replacement.transform != null)
            {
                replacement.transform.SetSiblingIndex(Mathf.Max(0, siblingIndex));
            }

            replacement.gameObject.SetActive(false);
            replacement.carIsActive = false;
            ConfigureReplacement(controller, replacement);

            if (current != null)
            {
                UnityEngine.Object.Destroy(current.gameObject);
            }

            instances[slot] = replacement;
            SetFieldValue(controller, "carInstances", instances);

            logger?.LogInfo("Scene traffic client pool slot replaced slot=" + slot +
                            " prefab=" + GetBaseCarName(replacement.name) +
                            " controller=" + NetPath.GetPath(controller.transform));
            return true;
        }

        private static void ConfigureReplacement(Component controller, CarInALoop car)
        {
            if (controller == null || car == null) return;

            if (controller is RoadTripTrafficController)
            {
                car.disableTimer = GetFieldValue<float>(controller, "disableVehicleAfterSeconds");
                car.disableZRotation = GetFieldValue<bool>(controller, "disableZRotation");
                return;
            }

            var sensor = car.GetComponentInChildren<PlayerInFrontOfVehicle>(true);
            if (sensor != null)
            {
                sensor.trafficController = controller as TrafficController;
            }
        }

        private static CarInALoop FindPrefabByHash(Component controller, int hash)
        {
            var prefabs = GetFieldValue<CarInALoop[]>(controller, "cars");
            if (prefabs == null) return null;

            for (var i = 0; i < prefabs.Length; i++)
            {
                var prefab = prefabs[i];
                if (prefab == null) continue;
                if (StableNameHash(GetBaseCarName(prefab.name)) == hash)
                {
                    return prefab;
                }
            }

            return null;
        }

        private static bool IsTrafficKey(string fieldName)
        {
            return !string.IsNullOrEmpty(fieldName) &&
                   fieldName.StartsWith(KeyPrefix, StringComparison.Ordinal);
        }

        private static bool IsTriggerKey(string fieldName)
        {
            return !string.IsNullOrEmpty(fieldName) &&
                   fieldName.StartsWith(TriggerPrefix, StringComparison.Ordinal);
        }

        private static List<Component> FindPizzeriaControllers()
        {
            var list = new List<Component>();
            var controllers = UnityEngine.Object.FindObjectsOfType<TrafficController>();
            for (var i = 0; i < controllers.Length; i++)
            {
                if (controllers[i] != null) list.Add(controllers[i]);
            }

            SortByPath(list);
            return list;
        }

        private static List<Component> FindRoadTripControllers()
        {
            var list = new List<Component>();
            var controllers = UnityEngine.Object.FindObjectsOfType<RoadTripTrafficController>();
            for (var i = 0; i < controllers.Length; i++)
            {
                if (controllers[i] != null) list.Add(controllers[i]);
            }

            SortByPath(list);
            return list;
        }

        private static void SortByPath(List<Component> list)
        {
            list.Sort((left, right) => string.CompareOrdinal(
                left != null ? NetPath.GetPath(left.transform) : string.Empty,
                right != null ? NetPath.GetPath(right.transform) : string.Empty));
        }

        private static void SortByPath<T>(T[] items) where T : Component
        {
            if (items == null || items.Length <= 1) return;
            Array.Sort(items, (left, right) => string.CompareOrdinal(
                left != null ? NetPath.GetPath(left.transform) : string.Empty,
                right != null ? NetPath.GetPath(right.transform) : string.Empty));
        }

        private static bool TryParseTriggerKey(string localKey, out int index, out string name)
        {
            index = -1;
            name = string.Empty;
            if (string.IsNullOrEmpty(localKey) || localKey.Length < 3 || localKey[0] != 'T')
            {
                return false;
            }

            var dot = localKey.IndexOf('.');
            if (dot <= 1 || dot >= localKey.Length - 1)
            {
                return false;
            }

            if (!int.TryParse(localKey.Substring(1, dot - 1), out index))
            {
                return false;
            }

            name = localKey.Substring(dot + 1);
            return true;
        }

        private static bool GetCanSpawn(Component controller)
        {
            if (controller is TrafficController pizzeria) return pizzeria.canSpawnVehicle;
            if (controller is RoadTripTrafficController roadTrip) return roadTrip.canSpawnVehicle;
            return false;
        }

        private static void SetCanSpawn(Component controller, bool value)
        {
            if (controller is TrafficController pizzeria) pizzeria.canSpawnVehicle = value;
            if (controller is RoadTripTrafficController roadTrip) roadTrip.canSpawnVehicle = value;
        }

        private static string GetBaseCarName(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            return name.Replace("(Clone)", string.Empty).Trim();
        }

        private static int StableNameHash(string value)
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

        private static T GetFieldValue<T>(object target, string fieldName)
        {
            if (target == null || string.IsNullOrEmpty(fieldName)) return default(T);
            var field = GetField(target.GetType(), fieldName);
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
            if (target == null || string.IsNullOrEmpty(fieldName)) return;
            var field = GetField(target.GetType(), fieldName);
            if (field == null) return;

            try
            {
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
