using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace WoodburySpectatorSync.Coop
{
    internal static class VendingMachineSceneSync
    {
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Dictionary<string, FieldInfo> FieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private static long _nextSuppressLogMs;

        public static int EmitHostFlags(string prefix, Action<string, int> emit)
        {
            if (emit == null) return 0;

            var hash = 97;
            var machines = UnityEngine.Object.FindObjectsOfType<VendingMachineManager>();
            SortByPath(machines);
            Emit(prefix + "Count", machines.Length, emit, ref hash);
            for (var i = 0; i < machines.Length; i++)
            {
                EmitMachine(prefix + i + ".", machines[i], emit, ref hash);
            }

            return hash;
        }

        public static bool TryApplyFlag(string localKey, int value, ManualLogSource logger)
        {
            if (string.Equals(localKey, "Count", StringComparison.Ordinal)) return true;
            if (!TryParseIndexedKey(localKey, out var index, out var name)) return true;

            var machines = UnityEngine.Object.FindObjectsOfType<VendingMachineManager>();
            SortByPath(machines);
            if (index < 0 || index >= machines.Length) return false;

            var machine = machines[index];
            if (machine == null) return false;
            SuppressLocalBrains(machines, logger);

            return TryApplyMachine(machine, name, value);
        }

        public static void CollectSyncedTransforms(List<Transform> transforms)
        {
            if (transforms == null) return;
            var machines = UnityEngine.Object.FindObjectsOfType<VendingMachineManager>();
            SortByPath(machines);
            for (var i = 0; i < machines.Length; i++)
            {
                var can = GetFieldObject(machines[i], "currentSodaCan") as GameObject;
                if (can != null && can.activeInHierarchy) transforms.Add(can.transform);
            }
        }

        private static void EmitMachine(string prefix, VendingMachineManager machine, Action<string, int> emit, ref int hash)
        {
            if (machine == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            var currentCan = GetFieldObject(machine, "currentSodaCan") as GameObject;
            var currentCanBody = currentCan != null ? currentCan.GetComponent<Rigidbody>() : null;
            var audio = GetFieldValue<AudioSource>(machine, "machineAudioSource");
            var playerCamera = GetFieldValue<Camera>(machine, "playerCamera");
            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", machine.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Enabled", machine.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "InUse", machine.inUse ? 1 : 0, emit, ref hash);
            Emit(prefix + "SelectingDrink", machine.selectingDrink ? 1 : 0, emit, ref hash);
            Emit(prefix + "DialogueCamera", IsObjectActive(GetFieldObject(machine, "dialogueCamera")) ? 1 : 0, emit, ref hash);
            Emit(prefix + "MachineClickTrigger", IsObjectActive(GetFieldObject(machine, "machineClickTrigger")) ? 1 : 0, emit, ref hash);
            Emit(prefix + "Slot", IsObjectActive(GetFieldObject(machine, "slot")) ? 1 : 0, emit, ref hash);
            Emit(prefix + "SlidingCollider", IsObjectActive(GetFieldObject(machine, "slidingCollider")) ? 1 : 0, emit, ref hash);
            Emit(prefix + "CurrentCan", currentCan != null && currentCan.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "CurrentCanKinematic", currentCanBody != null && currentCanBody.isKinematic ? 1 : 0, emit, ref hash);
            Emit(prefix + "AudioPlaying", audio != null && audio.isPlaying ? 1 : 0, emit, ref hash);
            Emit(prefix + "AudioTimeMs", audio != null ? Mathf.Max(0, Mathf.RoundToInt(audio.time * 1000f)) : 0, emit, ref hash);
            Emit(prefix + "AudioClipHash", audio != null ? StableObjectNameHash(audio.clip) : 0, emit, ref hash);
            Emit(prefix + "PlayerCameraFov10", playerCamera != null ? Mathf.RoundToInt(playerCamera.fieldOfView * 10f) : 0, emit, ref hash);
        }

        private static bool TryApplyMachine(VendingMachineManager machine, string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { machine.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { machine.enabled = false; return true; }
            if (string.Equals(name, "InUse", StringComparison.Ordinal)) { machine.inUse = value != 0; return true; }
            if (string.Equals(name, "SelectingDrink", StringComparison.Ordinal)) { machine.selectingDrink = value != 0; return true; }
            if (string.Equals(name, "DialogueCamera", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(machine, "dialogueCamera"), value != 0); return true; }
            if (string.Equals(name, "MachineClickTrigger", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(machine, "machineClickTrigger"), value != 0); return true; }
            if (string.Equals(name, "Slot", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(machine, "slot"), value != 0); return true; }
            if (string.Equals(name, "SlidingCollider", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(machine, "slidingCollider"), value != 0); return true; }
            if (string.Equals(name, "CurrentCan", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(machine, "currentSodaCan"), value != 0); return true; }
            if (string.Equals(name, "CurrentCanKinematic", StringComparison.Ordinal))
            {
                var can = GetFieldObject(machine, "currentSodaCan") as GameObject;
                var body = can != null ? can.GetComponent<Rigidbody>() : null;
                if (body != null) body.isKinematic = value != 0;
                return true;
            }

            var audio = GetFieldValue<AudioSource>(machine, "machineAudioSource");
            if (string.Equals(name, "AudioPlaying", StringComparison.Ordinal)) { ApplyAudioPlayback(audio, value != 0); return true; }
            if (string.Equals(name, "AudioTimeMs", StringComparison.Ordinal)) { if (audio != null) audio.time = Mathf.Max(0f, value / 1000f); return true; }
            if (string.Equals(name, "AudioClipHash", StringComparison.Ordinal)) return true;
            if (string.Equals(name, "PlayerCameraFov10", StringComparison.Ordinal))
            {
                var playerCamera = GetFieldValue<Camera>(machine, "playerCamera");
                if (playerCamera != null) playerCamera.fieldOfView = Mathf.Max(1f, value / 10f);
                return true;
            }

            return true;
        }

        private static void SuppressLocalBrains(VendingMachineManager[] machines, ManualLogSource logger)
        {
            if (machines == null) return;
            for (var i = 0; i < machines.Length; i++)
            {
                if (machines[i] != null) machines[i].enabled = false;
            }

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (logger != null && nowMs >= _nextSuppressLogMs)
            {
                _nextSuppressLogMs = nowMs + 10000;
                logger.LogInfo("Vending scene-state client brain suppressed machines=" + machines.Length);
            }
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

        private static int StableObjectNameHash(object target)
        {
            if (target == null) return 0;
            var unityObject = target as UnityEngine.Object;
            var name = unityObject != null ? unityObject.name : target.ToString();
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

        private static FieldInfo GetField(Type type, string fieldName)
        {
            if (type == null || string.IsNullOrEmpty(fieldName)) return null;
            var key = type.FullName + "." + fieldName;
            if (FieldCache.TryGetValue(key, out var cached)) return cached;

            var current = type;
            while (current != null)
            {
                var field = current.GetField(fieldName, FieldFlags);
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
