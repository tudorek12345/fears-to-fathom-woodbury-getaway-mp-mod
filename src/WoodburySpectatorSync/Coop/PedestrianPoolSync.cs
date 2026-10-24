using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace WoodburySpectatorSync.Coop
{
    // Native spawners roll different prefab/path choices on each PC. Reconcile their
    // child inventory before snapshot AI transforms, which address these exact indices.
    internal static class PedestrianPoolSync
    {
        private const string Prefix = "CoopPedPool:";
        private const string EmptySlot = "CoopPedestrianSlot";
        private static readonly FieldInfo Prefabs = typeof(NavmeshPedsSpawner).GetField("pedsPrefab", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void Emit(Action<string, int> emit)
        {
            foreach (var spawner in Resources.FindObjectsOfTypeAll<NavmeshPedsSpawner>())
            {
                if (spawner == null || !InWorld(spawner.gameObject.scene)) continue;
                var prefabs = Prefabs?.GetValue(spawner) as GameObject[];
                if (prefabs == null) continue;
                var parent = spawner.transform;
                if (parent.childCount > 128) throw new InvalidOperationException("Pedestrian pool exceeds 128 slots");
                string path = NetPath.GetPath(parent);
                emit(Prefix + "-1:" + path, parent.childCount);
                for (int i = 0; i < parent.childCount; i++)
                {
                    string name = parent.GetChild(i).name;
                    for (int p = 0; p < prefabs.Length; p++)
                        if (prefabs[p] != null && name == prefabs[p].name + "(Clone)") { emit(Prefix + i + ":" + path, p); break; }
                }
            }
        }

        public static bool Apply(string key, int value)
        {
            if (!key.StartsWith(Prefix, StringComparison.Ordinal)) return false;
            int separator = key.IndexOf(':', Prefix.Length);
            if (separator < 0 || !int.TryParse(key.Substring(Prefix.Length, separator - Prefix.Length), out int slot) || slot < -1 || slot >= 128)
                throw new InvalidOperationException("Invalid pedestrian slot");
            string path = key.Substring(separator + 1);
            var parent = NetPath.FindByPath(path);
            var spawner = parent != null ? parent.GetComponent<NavmeshPedsSpawner>() : null;
            if (spawner == null || !InWorld(spawner.gameObject.scene)) throw new InvalidOperationException("Pedestrian pool unavailable: " + path);
            SuppressSpawner(spawner);
            if (slot == -1)
            {
                if (value < 0 || value > 128) throw new InvalidOperationException("Invalid pedestrian pool count");
                while (parent.childCount > value)
                {
                    var child = parent.GetChild(parent.childCount - 1);
                    if (!IsReplica(child)) throw new InvalidOperationException("Pedestrian pool contains an unexpected static child");
                    Retire(child);
                }
                Fill(parent, value);
                return true;
            }
            var prefabs = Prefabs?.GetValue(spawner) as GameObject[];
            if (prefabs == null || value < 0 || value >= prefabs.Length || prefabs[value] == null)
                throw new InvalidOperationException("Invalid pedestrian prefab");
            Fill(parent, slot + 1);
            var existing = parent.GetChild(slot);
            string expected = prefabs[value].name + "(Clone)";
            if (existing.name == expected) { SuppressAgent(existing.gameObject); return true; }
            if (!IsReplica(existing)) throw new InvalidOperationException("Pedestrian prefab conflicts with a static child");
            Retire(existing);
            var clone = UnityEngine.Object.Instantiate(prefabs[value], parent);
            clone.name = expected; clone.transform.SetSiblingIndex(slot);
            SuppressAgent(clone);
            clone.SetActive(false); // The following authoritative AI state supplies its pose and activation.
            NetPath.InvalidateCache();
            return true;
        }

        public static void Suppress(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var spawner in root.GetComponentsInChildren<NavmeshPedsSpawner>(true)) SuppressSpawner(spawner);
        }

        private static bool InWorld(Scene scene) => scene.IsValid() && scene.isLoaded &&
            (scene == SceneManager.GetActiveScene() || (SceneCatalog.HasOfficeWorld(SceneManager.GetActiveScene().name) && scene.name == "OfficeLayout"));
        private static bool IsReplica(Transform child) => child.name == EmptySlot || child.GetComponent<NavmeshPathAgent>() != null;
        private static void Fill(Transform parent, int count)
        {
            while (parent.childCount < count)
            {
                var placeholder = new GameObject(EmptySlot); placeholder.SetActive(false); placeholder.transform.SetParent(parent, false);
                NetPath.InvalidateCache();
            }
        }
        private static void Retire(Transform child)
        {
            child.gameObject.SetActive(false); child.SetParent(null); UnityEngine.Object.Destroy(child.gameObject); NetPath.InvalidateCache();
        }
        private static void SuppressSpawner(NavmeshPedsSpawner spawner)
        {
            spawner.CancelInvoke(); spawner.StopAllCoroutines(); spawner.enabled = false;
            foreach (var agent in spawner.GetComponentsInChildren<NavmeshPathAgent>(true)) SuppressAgent(agent.gameObject);
        }
        private static void SuppressAgent(GameObject obj)
        {
            foreach (var brain in obj.GetComponentsInChildren<NavmeshPathAgent>(true)) { brain.StopAllCoroutines(); brain.CancelInvoke(); brain.enabled = false; }
            foreach (var agent in obj.GetComponentsInChildren<NavMeshAgent>(true)) agent.enabled = false;
        }
    }
}
