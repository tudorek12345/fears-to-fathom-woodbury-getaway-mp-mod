using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace WoodburySpectatorSync.Coop
{
    internal static class CabinTrafficSync
    {
        public const string KeyPrefix = "Traffic.";

        private const string ControllerPrefix = KeyPrefix + "C";
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Dictionary<string, FieldInfo> FieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private static long _nextSuppressLogMs;

        public static int EmitHostFlags(string fullPrefix, Action<string, int> emit)
        {
            if (emit == null) return 0;

            var hash = 17;
            var controllers = FindControllers();
            for (var controllerIndex = 0; controllerIndex < controllers.Count; controllerIndex++)
            {
                var controller = controllers[controllerIndex];
                if (controller == null) continue;

                var prefix = fullPrefix + ControllerPrefix + controllerIndex + ".";
                var instances = GetFieldValue<CarInALoop[]>(controller, "carInstances");
                var activeMask = 0;
                var puppetMask = 0;
                if (instances != null)
                {
                    for (var i = 0; i < instances.Length && i < 30; i++)
                    {
                        var car = instances[i];
                        if (car == null) continue;

                        var isActive = car.gameObject.activeSelf;
                        var isPuppetMoving = car.carIsActive;
                        if (isActive) activeMask |= 1 << i;
                        if (isPuppetMoving) puppetMask |= 1 << i;
                        Emit(prefix + "I" + i + ".PrefabHash", StableNameHash(GetBaseCarName(car.name)), emit, ref hash);
                    }
                }

                Emit(prefix + "RootActive", controller.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(prefix + "CanSpawn", controller.canSpawnVehicle ? 1 : 0, emit, ref hash);
                Emit(prefix + "ActiveMask", activeMask, emit, ref hash);
                Emit(prefix + "CarIsActiveMask", puppetMask, emit, ref hash);
            }

            return hash;
        }

        public static void CollectSyncedTransforms(List<Transform> transforms)
        {
            if (transforms == null) return;

            var controllers = FindControllers();
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

        public static bool TryApplyFlag(string fieldName, int value, ManualLogSource logger)
        {
            if (string.IsNullOrEmpty(fieldName) ||
                !fieldName.StartsWith(KeyPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            SuppressClientTrafficBrain(logger);

            var localKey = fieldName.Substring(KeyPrefix.Length);
            if (!localKey.StartsWith("C", StringComparison.Ordinal)) return true;
            var dot = localKey.IndexOf('.');
            if (dot <= 1) return true;

            if (!int.TryParse(localKey.Substring(1, dot - 1), out var controllerIndex))
            {
                return true;
            }

            var controllers = FindControllers();
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
                // The host owns traffic spawning. Keep client controllers from making local random choices.
                controller.canSpawnVehicle = false;
                return true;
            }

            if (string.Equals(name, "ActiveMask", StringComparison.Ordinal))
            {
                ApplyActiveMask(controller, value);
                return true;
            }

            if (string.Equals(name, "CarIsActiveMask", StringComparison.Ordinal))
            {
                ApplyCarIsActiveMask(controller, value);
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

        public static void SuppressClientTrafficBrain(ManualLogSource logger)
        {
            var triggers = UnityEngine.Object.FindObjectsOfType<CabinTrafficTrigger>();
            for (var i = 0; i < triggers.Length; i++)
            {
                if (triggers[i] != null) triggers[i].enabled = false;
            }

            var timed = UnityEngine.Object.FindObjectsOfType<CabinTimedTrafficSpawner>();
            for (var i = 0; i < timed.Length; i++)
            {
                if (timed[i] != null) timed[i].enabled = false;
            }

            var controllers = FindControllers();
            for (var i = 0; i < controllers.Count; i++)
            {
                if (controllers[i] != null) controllers[i].canSpawnVehicle = false;
            }

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (logger != null && nowMs >= _nextSuppressLogMs)
            {
                _nextSuppressLogMs = nowMs + 10000;
                logger.LogInfo("Cabin traffic client brain suppressed triggers=" + triggers.Length +
                               " timed=" + timed.Length +
                               " controllers=" + controllers.Count);
            }
        }

        private static void ApplyActiveMask(CabinTrafficController controller, int mask)
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

        private static void ApplyCarIsActiveMask(CabinTrafficController controller, int mask)
        {
            var instances = GetFieldValue<CarInALoop[]>(controller, "carInstances");
            if (instances == null) return;

            for (var i = 0; i < instances.Length && i < 30; i++)
            {
                var car = instances[i];
                if (car == null) continue;
                // Client traffic is transform-driven by host AiTransform packets.
                car.carIsActive = false;
            }
        }

        private static bool EnsurePoolSlotPrefab(CabinTrafficController controller, int slot, int expectedHash, ManualLogSource logger)
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
                logger?.LogWarning("Cabin traffic prefab hash missing controller=" +
                                   NetPath.GetPath(controller.transform) +
                                   " slot=" + slot +
                                   " hash=" + expectedHash);
                return true;
            }

            var parent = GetFieldValue<Transform>(controller, "poolParent");
            var siblingIndex = current != null && current.transform != null
                ? current.transform.GetSiblingIndex()
                : slot;
            var replacement = parent != null
                ? UnityEngine.Object.Instantiate(prefab, parent)
                : UnityEngine.Object.Instantiate(prefab);
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

            logger?.LogInfo("Cabin traffic client pool slot replaced slot=" + slot +
                            " prefab=" + GetBaseCarName(replacement.name) +
                            " controller=" + NetPath.GetPath(controller.transform));
            return true;
        }

        private static void ConfigureReplacement(CabinTrafficController controller, CarInALoop car)
        {
            if (controller == null || car == null) return;

            if (GetFieldValue<bool>(controller, "disableVehicleOnReachingCertainDistance"))
            {
                car.endOfSplineDistance = GetFieldValue<float>(controller, "endOfSplineDistance");
                car.disableOnReachingThisSplineDistance = true;
            }

            if (GetFieldValue<bool>(controller, "overrideVehicleVolume"))
            {
                var audio = car.GetComponent<AudioSource>();
                if (audio != null)
                {
                    audio.volume = GetFieldValue<float>(controller, "overriddenVolume");
                }
            }

            var sensor = car.GetComponentInChildren<MultipleObjectsInFrontOfVehicle>(true);
            if (sensor != null)
            {
                sensor.cabinTrafficController = controller;
                var objectsToStopAt = GetFieldValue<List<GameObject>>(controller, "objectsToStopAt");
                sensor.gameObjectsThatStop = objectsToStopAt;
                if (objectsToStopAt != null && !objectsToStopAt.Contains(car.gameObject))
                {
                    objectsToStopAt.Add(car.gameObject);
                }

                if (GetFieldValue<bool>(controller, "overrideVehicleStartTime"))
                {
                    sensor.timeToStart = GetFieldValue<float>(controller, "timeToStart");
                }

                if (GetFieldValue<bool>(controller, "overrideVehicleStopTime"))
                {
                    sensor.timeToStop = GetFieldValue<float>(controller, "timeToStop");
                }
            }
        }

        private static CarInALoop FindPrefabByHash(CabinTrafficController controller, int hash)
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

        private static List<CabinTrafficController> FindControllers()
        {
            var list = new List<CabinTrafficController>();
            var controllers = UnityEngine.Object.FindObjectsOfType<CabinTrafficController>();
            for (var i = 0; i < controllers.Length; i++)
            {
                if (controllers[i] != null) list.Add(controllers[i]);
            }

            list.Sort((left, right) => string.CompareOrdinal(
                left != null ? NetPath.GetPath(left.transform) : string.Empty,
                right != null ? NetPath.GetPath(right.transform) : string.Empty));
            return list;
        }

        private static string GetBaseCarName(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            var result = name.Replace("(Clone)", string.Empty).Trim();
            return result;
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
