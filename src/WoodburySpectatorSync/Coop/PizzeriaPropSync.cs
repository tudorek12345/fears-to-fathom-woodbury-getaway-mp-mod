using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WoodburySpectatorSync.Coop
{
    internal static class PizzeriaPropSync
    {
        public const string KeyPrefix = "Props.";

        private const string BoxPrefix = KeyPrefix + "PizzaBox";
        private const string LidPrefix = KeyPrefix + "PizzaLid";
        private const string SlicePrefix = KeyPrefix + "PizzaSlice";
        private const string BoxManagerPrefix = KeyPrefix + "BoxManager";
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Dictionary<string, FieldInfo> FieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private static long _nextSuppressLogMs;

        public static int EmitHostFlags(string fullPrefix, Action<string, int> emit)
        {
            if (emit == null) return 0;

            var hash = 53;
            var boxes = FindPizzaBoxes();
            for (var i = 0; i < boxes.Count; i++)
            {
                EmitBox(fullPrefix + BoxPrefix + i + ".", boxes[i], emit, ref hash);
            }

            var lids = FindPizzaLids();
            for (var i = 0; i < lids.Count; i++)
            {
                EmitLid(fullPrefix + LidPrefix + i + ".", lids[i], emit, ref hash);
            }

            var slices = FindPizzaSlices();
            for (var i = 0; i < slices.Count; i++)
            {
                EmitSlice(fullPrefix + SlicePrefix + i + ".", slices[i], emit, ref hash);
            }

            var managers = FindBoxManagers();
            for (var i = 0; i < managers.Count; i++)
            {
                EmitBoxManager(fullPrefix + BoxManagerPrefix + i + ".", managers[i], emit, ref hash);
            }

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

            if (fieldName.StartsWith(BoxPrefix, StringComparison.Ordinal))
            {
                return TryApplyIndexedBox(fieldName.Substring(BoxPrefix.Length), value);
            }

            if (fieldName.StartsWith(LidPrefix, StringComparison.Ordinal))
            {
                return TryApplyIndexedLid(fieldName.Substring(LidPrefix.Length), value);
            }

            if (fieldName.StartsWith(SlicePrefix, StringComparison.Ordinal))
            {
                return TryApplyIndexedSlice(fieldName.Substring(SlicePrefix.Length), value);
            }

            if (fieldName.StartsWith(BoxManagerPrefix, StringComparison.Ordinal))
            {
                return TryApplyIndexedBoxManager(fieldName.Substring(BoxManagerPrefix.Length), value);
            }

            return true;
        }

        public static void CollectSyncedTransforms(List<Transform> transforms)
        {
            if (transforms == null) return;

            var boxes = FindPizzaBoxes();
            for (var i = 0; i < boxes.Count; i++)
            {
                if (boxes[i] != null && boxes[i].gameObject.activeInHierarchy) transforms.Add(boxes[i].transform);
            }

            var slices = FindPizzaSlices();
            for (var i = 0; i < slices.Count; i++)
            {
                if (slices[i] != null && slices[i].gameObject.activeInHierarchy) transforms.Add(slices[i].transform);
            }

            var lids = FindPizzaLids();
            for (var i = 0; i < lids.Count; i++)
            {
                if (lids[i] != null && lids[i].gameObject.activeInHierarchy) transforms.Add(lids[i].transform);
            }

            var managers = FindBoxManagers();
            for (var i = 0; i < managers.Count; i++)
            {
                var animatedBox = GetFieldValue<Transform>(managers[i], "animatedPizzaBox");
                if (animatedBox != null && animatedBox.gameObject.activeInHierarchy) transforms.Add(animatedBox);
                var latest = GetFieldValue<GameObject>(managers[i], "latestFoldedBox");
                if (latest != null && latest.activeInHierarchy) transforms.Add(latest.transform);
            }
        }

        private static void EmitBox(string prefix, PizzaBox box, Action<string, int> emit, ref int hash)
        {
            if (box == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", box.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "IsChewing", box.isChewing ? 1 : 0, emit, ref hash);
            Emit(prefix + "OnTable", box.onTable ? 1 : 0, emit, ref hash);
            Emit(prefix + "SlicesEaten", box.slicesEaten, emit, ref hash);
            Emit(prefix + "CrustEaten", PizzaBox.crustEaten ? 1 : 0, emit, ref hash);
            Emit(prefix + "CrustConversationDone", box.crustConversationDone ? 1 : 0, emit, ref hash);
            Emit(prefix + "DisableChairOnClick", box.disablePlayerChairOnClick ? 1 : 0, emit, ref hash);
            Emit(prefix + "Slice1Mask", BuildActiveMask(box.slice1), emit, ref hash);
            Emit(prefix + "Slice2Mask", BuildActiveMask(box.slice2), emit, ref hash);
            Emit(prefix + "Slice3Mask", BuildActiveMask(box.slice3), emit, ref hash);
            Emit(prefix + "AudioEating", box.eatingPizzaAS != null && box.eatingPizzaAS.isPlaying ? 1 : 0, emit, ref hash);
        }

        private static void EmitLid(string prefix, PizzaBoxLid lid, Action<string, int> emit, ref int hash)
        {
            if (lid == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", lid.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Open", GetFieldValue<bool>(lid, "isOpen") ? 1 : 0, emit, ref hash);
            Emit(prefix + "Moving", GetFieldValue<bool>(lid, "isMoving") ? 1 : 0, emit, ref hash);
        }

        private static void EmitSlice(string prefix, PizzaSlice slice, Action<string, int> emit, ref int hash)
        {
            if (slice == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", slice.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "GlobalChewing", PizzaSlice.isChewing ? 1 : 0, emit, ref hash);
            Emit(prefix + "IsCrust", slice.isCrust ? 1 : 0, emit, ref hash);
            Emit(prefix + "HasNext", slice.next != null ? 1 : 0, emit, ref hash);
        }

        private static void EmitBoxManager(string prefix, PizzaBoxesManager manager, Action<string, int> emit, ref int hash)
        {
            if (manager == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            var workerAnimator = GetFieldValue<Animator>(manager, "workerAnimator");
            var boxAnimator = GetFieldValue<Animator>(manager, "animatedPizzaBoxAnimator");
            var foldingAudio = GetFieldValue<AudioSource>(manager, "foldingAS");
            var rightHandRig = GetFieldValue<Component>(manager, "rightHandRig");
            var foldedBoxes = GetFieldValue<List<GameObject>>(manager, "foldedPizzaBoxes");

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", manager.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "CanInteract", manager.CanInteract ? 1 : 0, emit, ref hash);
            Emit(prefix + "CurrentState", (int)manager.CurrentAnimationState, emit, ref hash);
            Emit(prefix + "FoldingBoxes", GetFieldValue<bool>(manager, "foldingBoxes") ? 1 : 0, emit, ref hash);
            Emit(prefix + "PlayerInteracting", GetFieldValue<bool>(manager, "playerIsInteracting") ? 1 : 0, emit, ref hash);
            Emit(prefix + "AnimationPaused", GetFieldValue<bool>(manager, "animationPaused") ? 1 : 0, emit, ref hash);
            Emit(prefix + "FoldedCount", foldedBoxes != null ? foldedBoxes.Count : 0, emit, ref hash);
            Emit(prefix + "FoldedActiveMask", BuildActiveMask(foldedBoxes), emit, ref hash);
            Emit(prefix + "AnimatedBox", IsObjectActive(GetFieldValue<Transform>(manager, "animatedPizzaBox")) ? 1 : 0, emit, ref hash);
            Emit(prefix + "LatestFoldedBox", IsObjectActive(GetFieldValue<GameObject>(manager, "latestFoldedBox")) ? 1 : 0, emit, ref hash);
            Emit(prefix + "WorkerAnimState", workerAnimator != null ? workerAnimator.GetInteger(Animator.StringToHash("state")) : 0, emit, ref hash);
            Emit(prefix + "WorkerIdleIndex", workerAnimator != null ? workerAnimator.GetInteger(Animator.StringToHash("idleIndex")) : 0, emit, ref hash);
            Emit(prefix + "WorkerSpeed100", workerAnimator != null ? Mathf.RoundToInt(workerAnimator.speed * 100f) : 0, emit, ref hash);
            Emit(prefix + "BoxAnimatorEnabled", boxAnimator != null && boxAnimator.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "BoxAnimatorSpeed100", boxAnimator != null ? Mathf.RoundToInt(boxAnimator.speed * 100f) : 0, emit, ref hash);
            Emit(prefix + "RightHandRig100", Mathf.RoundToInt(GetRigWeight(rightHandRig) * 100f), emit, ref hash);
            Emit(prefix + "FoldingAudio", foldingAudio != null && foldingAudio.isPlaying ? 1 : 0, emit, ref hash);
        }

        private static bool TryApplyIndexedBox(string localKey, int value)
        {
            if (!TryParseIndexedKey(localKey, out var index, out var name)) return true;
            var boxes = FindPizzaBoxes();
            if (index < 0 || index >= boxes.Count) return IsOptionalMissing(name, value);
            var box = boxes[index];
            if (box == null) return IsOptionalMissing(name, value);

            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { box.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "IsChewing", StringComparison.Ordinal)) { box.isChewing = value != 0; return true; }
            if (string.Equals(name, "OnTable", StringComparison.Ordinal)) { box.onTable = value != 0; return true; }
            if (string.Equals(name, "SlicesEaten", StringComparison.Ordinal)) { box.slicesEaten = value; return true; }
            if (string.Equals(name, "CrustEaten", StringComparison.Ordinal)) { PizzaBox.crustEaten = value != 0; return true; }
            if (string.Equals(name, "CrustConversationDone", StringComparison.Ordinal)) { box.crustConversationDone = value != 0; return true; }
            if (string.Equals(name, "DisableChairOnClick", StringComparison.Ordinal)) { box.disablePlayerChairOnClick = value != 0; return true; }
            if (string.Equals(name, "Slice1Mask", StringComparison.Ordinal)) { ApplyActiveMask(box.slice1, value); return true; }
            if (string.Equals(name, "Slice2Mask", StringComparison.Ordinal)) { ApplyActiveMask(box.slice2, value); return true; }
            if (string.Equals(name, "Slice3Mask", StringComparison.Ordinal)) { ApplyActiveMask(box.slice3, value); return true; }
            if (string.Equals(name, "AudioEating", StringComparison.Ordinal)) { ApplyAudioPlayback(box.eatingPizzaAS, value != 0); return true; }
            return true;
        }

        private static bool TryApplyIndexedLid(string localKey, int value)
        {
            if (!TryParseIndexedKey(localKey, out var index, out var name)) return true;
            var lids = FindPizzaLids();
            if (index < 0 || index >= lids.Count) return IsOptionalMissing(name, value);
            var lid = lids[index];
            if (lid == null) return IsOptionalMissing(name, value);

            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { lid.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Open", StringComparison.Ordinal)) { SetFieldValue(lid, "isOpen", value != 0); ApplyLidRotation(lid, value != 0); return true; }
            if (string.Equals(name, "Moving", StringComparison.Ordinal)) { SetFieldValue(lid, "isMoving", false); return true; }
            return true;
        }

        private static bool TryApplyIndexedSlice(string localKey, int value)
        {
            if (!TryParseIndexedKey(localKey, out var index, out var name)) return true;
            var slices = FindPizzaSlices();
            if (index < 0 || index >= slices.Count) return true;
            var slice = slices[index];
            if (slice == null) return true;

            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { slice.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "GlobalChewing", StringComparison.Ordinal)) { PizzaSlice.isChewing = value != 0; return true; }
            if (string.Equals(name, "IsCrust", StringComparison.Ordinal)) { slice.isCrust = value != 0; return true; }
            if (string.Equals(name, "HasNext", StringComparison.Ordinal)) return true;
            return true;
        }

        private static bool TryApplyIndexedBoxManager(string localKey, int value)
        {
            if (!TryParseIndexedKey(localKey, out var index, out var name)) return true;
            var managers = FindBoxManagers();
            if (index < 0 || index >= managers.Count) return IsOptionalMissing(name, value);
            var manager = managers[index];
            if (manager == null) return IsOptionalMissing(name, value);

            var workerAnimator = GetFieldValue<Animator>(manager, "workerAnimator");
            var boxAnimator = GetFieldValue<Animator>(manager, "animatedPizzaBoxAnimator");
            var rightHandRig = GetFieldValue<Component>(manager, "rightHandRig");
            var foldingAudio = GetFieldValue<AudioSource>(manager, "foldingAS");

            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { manager.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "CanInteract", StringComparison.Ordinal)) { SetFieldValue(manager, "canInteract", value != 0); return true; }
            if (string.Equals(name, "CurrentState", StringComparison.Ordinal)) { SetFieldValue(manager, "currentAnimationState", value); return true; }
            if (string.Equals(name, "FoldingBoxes", StringComparison.Ordinal)) { SetFieldValue(manager, "foldingBoxes", value != 0); return true; }
            if (string.Equals(name, "PlayerInteracting", StringComparison.Ordinal)) { SetFieldValue(manager, "playerIsInteracting", value != 0); return true; }
            if (string.Equals(name, "AnimationPaused", StringComparison.Ordinal)) { SetFieldValue(manager, "animationPaused", value != 0); return true; }
            if (string.Equals(name, "FoldedCount", StringComparison.Ordinal)) { CoerceFoldedBoxCount(manager, value); return true; }
            if (string.Equals(name, "FoldedActiveMask", StringComparison.Ordinal)) { ApplyFoldedBoxActiveMask(manager, value); return true; }
            if (string.Equals(name, "AnimatedBox", StringComparison.Ordinal)) { ApplyAnimatedBoxState(manager, value != 0); return true; }
            if (string.Equals(name, "LatestFoldedBox", StringComparison.Ordinal)) { ApplyLatestFoldedBoxState(manager, value != 0); return true; }
            if (string.Equals(name, "WorkerAnimState", StringComparison.Ordinal)) { if (workerAnimator != null) workerAnimator.SetInteger(Animator.StringToHash("state"), value); return true; }
            if (string.Equals(name, "WorkerIdleIndex", StringComparison.Ordinal)) { if (workerAnimator != null) workerAnimator.SetInteger(Animator.StringToHash("idleIndex"), value); return true; }
            if (string.Equals(name, "WorkerSpeed100", StringComparison.Ordinal)) { if (workerAnimator != null) workerAnimator.speed = value / 100f; return true; }
            if (string.Equals(name, "BoxAnimatorEnabled", StringComparison.Ordinal)) { if (boxAnimator != null) boxAnimator.enabled = value != 0; return true; }
            if (string.Equals(name, "BoxAnimatorSpeed100", StringComparison.Ordinal)) { if (boxAnimator != null) boxAnimator.speed = value / 100f; return true; }
            if (string.Equals(name, "RightHandRig100", StringComparison.Ordinal)) { SetRigWeight(rightHandRig, value / 100f); return true; }
            if (string.Equals(name, "FoldingAudio", StringComparison.Ordinal)) { ApplyAudioPlayback(foldingAudio, value != 0); return true; }
            return true;
        }

        private static void SuppressLocalBrains(ManualLogSource logger)
        {
            var managers = FindBoxManagers();
            for (var i = 0; i < managers.Count; i++)
            {
                var manager = managers[i];
                if (manager == null) continue;

                manager.StopAllCoroutines();
                manager.enabled = false;
                SetFieldValue(manager, "foldingBoxes", false);
                SetFieldValue(manager, "canInteract", false);
                SetFieldValue(manager, "playerIsInteracting", false);
                SetFieldValue(manager, "animationPaused", false);

                var foldingAudio = GetFieldValue<AudioSource>(manager, "foldingAS");
                if (foldingAudio != null) foldingAudio.Stop();
            }

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (logger != null && nowMs >= _nextSuppressLogMs)
            {
                _nextSuppressLogMs = nowMs + 10000;
                logger.LogInfo("Pizzeria prop client brain suppressed boxManagers=" + managers.Count);
            }
        }

        private static List<PizzaBox> FindPizzaBoxes()
        {
            var list = FindSceneComponents<PizzaBox>();
            SortByPath(list);
            return list;
        }

        private static List<PizzaBoxLid> FindPizzaLids()
        {
            var list = FindSceneComponents<PizzaBoxLid>();
            SortByPath(list);
            return list;
        }

        private static List<PizzaSlice> FindPizzaSlices()
        {
            var list = FindSceneComponents<PizzaSlice>();
            SortByPath(list);
            return list;
        }

        private static List<PizzaBoxesManager> FindBoxManagers()
        {
            var list = FindSceneComponents<PizzaBoxesManager>();
            SortByPath(list);
            return list;
        }

        private static List<T> FindSceneComponents<T>() where T : Component
        {
            var results = new List<T>();
            var all = Resources.FindObjectsOfTypeAll<T>();
            for (var i = 0; i < all.Length; i++)
            {
                var component = all[i];
                if (component == null || component.gameObject == null) continue;
                var scene = component.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded) continue;
                if (scene.name != SceneManager.GetActiveScene().name) continue;
                results.Add(component);
            }

            return results;
        }

        private static bool IsOptionalMissing(string name, int value)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal) && value == 0) return true;
            return false;
        }

        private static void SortByPath<T>(List<T> list) where T : Component
        {
            list.Sort((left, right) => string.CompareOrdinal(
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

        private static void ApplyActiveMask(GameObject[] objects, int mask)
        {
            if (objects == null) return;
            for (var i = 0; i < objects.Length && i < 30; i++)
            {
                if (objects[i] == null) continue;
                objects[i].SetActive((mask & (1 << i)) != 0);
            }
        }

        private static void ApplyActiveMask(List<GameObject> objects, int mask)
        {
            if (objects == null) return;
            for (var i = 0; i < objects.Count && i < 30; i++)
            {
                if (objects[i] == null) continue;
                objects[i].SetActive((mask & (1 << i)) != 0);
            }
        }

        private static void ApplyFoldedBoxActiveMask(PizzaBoxesManager manager, int mask)
        {
            var foldedBoxes = GetFieldValue<List<GameObject>>(manager, "foldedPizzaBoxes");
            if (foldedBoxes != null)
            {
                var needed = HighestActiveIndex(mask) + 1;
                if (needed > foldedBoxes.Count)
                {
                    CoerceFoldedBoxCount(manager, needed);
                    foldedBoxes = GetFieldValue<List<GameObject>>(manager, "foldedPizzaBoxes");
                }
            }

            ApplyActiveMask(foldedBoxes, mask);
            if (mask == 0)
            {
                ApplyLatestFoldedBoxState(manager, false);
            }
        }

        private static void CoerceFoldedBoxCount(PizzaBoxesManager manager, int hostCount)
        {
            var foldedBoxes = GetFieldValue<List<GameObject>>(manager, "foldedPizzaBoxes");
            if (foldedBoxes == null) return;

            var safeCount = Mathf.Clamp(hostCount, 0, 30);
            while (foldedBoxes.Count > safeCount)
            {
                var last = foldedBoxes.Count - 1;
                var extra = foldedBoxes[last];
                foldedBoxes.RemoveAt(last);
                if (extra != null)
                {
                    extra.SetActive(false);
                    UnityEngine.Object.Destroy(extra);
                }
            }

            while (foldedBoxes.Count < safeCount)
            {
                var created = CreateFoldedBox(manager, foldedBoxes.Count);
                if (created == null) break;
                foldedBoxes.Add(created);
            }

            LayoutFoldedBoxes(manager, foldedBoxes);

            if (safeCount == 0)
            {
                SetFieldValue(manager, "latestFoldedBox", null);
                ApplyLatestFoldedBoxState(manager, false);
                return;
            }

            for (var i = safeCount; i < foldedBoxes.Count; i++)
            {
                if (foldedBoxes[i] != null)
                {
                    foldedBoxes[i].SetActive(false);
                }
            }

            if (foldedBoxes.Count > 0)
            {
                SetFieldValue(manager, "latestFoldedBox", foldedBoxes[Mathf.Min(safeCount, foldedBoxes.Count) - 1]);
            }
        }

        private static void ApplyAnimatedBoxState(PizzaBoxesManager manager, bool active)
        {
            SetObjectActive(GetFieldValue<Transform>(manager, "animatedPizzaBox"), active);
            var animator = GetFieldValue<Animator>(manager, "animatedPizzaBoxAnimator");
            if (animator != null)
            {
                animator.enabled = active;
                SetObjectActive(animator, active);
            }
        }

        private static void ApplyLatestFoldedBoxState(PizzaBoxesManager manager, bool active)
        {
            var latest = GetFieldValue<GameObject>(manager, "latestFoldedBox");
            if (active && latest == null)
            {
                var foldedBoxes = GetFieldValue<List<GameObject>>(manager, "foldedPizzaBoxes");
                if (foldedBoxes != null && foldedBoxes.Count > 0)
                {
                    latest = foldedBoxes[foldedBoxes.Count - 1];
                    SetFieldValue(manager, "latestFoldedBox", latest);
                }
            }

            SetObjectActive(latest, active);
        }

        private static int HighestActiveIndex(int mask)
        {
            for (var i = 29; i >= 0; i--)
            {
                if ((mask & (1 << i)) != 0) return i;
            }

            return -1;
        }

        private static GameObject CreateFoldedBox(PizzaBoxesManager manager, int index)
        {
            var prefab = GetFieldValue<GameObject>(manager, "foldedPizzaBoxPrefab");
            var parent = GetFieldValue<Transform>(manager, "foldedStacksPosition");
            if (prefab == null || parent == null) return null;

            var heightOffset = GetFieldValue<float>(manager, "heightOffset");
            if (heightOffset <= 0.001f) heightOffset = 0.066f;

            var instance = UnityEngine.Object.Instantiate(
                prefab,
                parent.position + Vector3.up * (heightOffset * (index + 1)),
                parent.rotation,
                parent);
            instance.name = prefab.name + "_CoopFolded_" + index;
            instance.SetActive(false);
            return instance;
        }

        private static void LayoutFoldedBoxes(PizzaBoxesManager manager, List<GameObject> foldedBoxes)
        {
            if (foldedBoxes == null || foldedBoxes.Count == 0) return;
            var parent = GetFieldValue<Transform>(manager, "foldedStacksPosition");
            if (parent == null) return;

            var heightOffset = GetFieldValue<float>(manager, "heightOffset");
            if (heightOffset <= 0.001f) heightOffset = 0.066f;

            for (var i = 0; i < foldedBoxes.Count; i++)
            {
                var box = foldedBoxes[i];
                if (box == null) continue;
                box.transform.SetParent(parent, true);
                box.transform.position = parent.position + Vector3.up * (heightOffset * (i + 1));
                box.transform.rotation = parent.rotation;
            }
        }

        private static void ApplyLidRotation(PizzaBoxLid lid, bool open)
        {
            if (lid == null) return;
            lid.transform.localRotation = Quaternion.Euler(open ? new Vector3(0f, 0f, -151.63f) : Vector3.zero);
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
            else if (audio.isPlaying)
            {
                audio.Stop();
            }
        }

        private static float GetRigWeight(Component rig)
        {
            if (rig == null) return 0f;
            var field = GetField(rig, "weight");
            if (field != null && field.FieldType == typeof(float))
            {
                try
                {
                    return (float)field.GetValue(rig);
                }
                catch
                {
                    return 0f;
                }
            }

            var property = rig.GetType().GetProperty(
                "weight",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.PropertyType == typeof(float) && property.CanRead)
            {
                try
                {
                    return (float)property.GetValue(rig, null);
                }
                catch
                {
                    return 0f;
                }
            }

            return 0f;
        }

        private static void SetRigWeight(Component rig, float weight)
        {
            if (rig == null) return;
            var field = GetField(rig, "weight");
            if (field != null && field.FieldType == typeof(float))
            {
                try
                {
                    field.SetValue(rig, weight);
                    return;
                }
                catch
                {
                }
            }

            var property = rig.GetType().GetProperty(
                "weight",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.PropertyType == typeof(float) && property.CanWrite)
            {
                try
                {
                    property.SetValue(rig, weight, null);
                }
                catch
                {
                }
            }
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
