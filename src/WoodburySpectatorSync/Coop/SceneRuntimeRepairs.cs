using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WoodburySpectatorSync.Coop
{
    internal static class SceneRuntimeRepairs
    {
        private static readonly FieldInfo CrouchPlayer = typeof(BlindCrouchHeight).GetField("firstPersonController", BindingFlags.NonPublic | BindingFlags.Instance);
        private static Harmony _patches;

        public static void Install()
        {
            if (_patches != null) return;
            _patches = new Harmony("com.woodbury.spectatorsync.lifecycle");
            _patches.Patch(AccessTools.Method(typeof(BlindCrouchHeight), "OnTriggerEnter"),
                prefix: new HarmonyMethod(typeof(SceneRuntimeRepairs), nameof(CanEnterCrouchTrigger)));
            foreach (string name in new[] { "StartFade", "FadeToZeroAndReset" })
                _patches.Patch(AccessTools.Method(typeof(FadeAudioSource), name),
                    postfix: new HarmonyMethod(typeof(SceneRuntimeRepairs), nameof(GuardAudioFade)));
        }

        public static void Uninstall()
        {
            if (_patches == null) return;
            _patches.UnpatchSelf(); _patches = null;
        }

        private static bool CanEnterCrouchTrigger(BlindCrouchHeight __instance)
            => CrouchPlayer == null || CrouchPlayer.GetValue(__instance) as FirstPersonController != null;

        private static void GuardAudioFade(AudioSource audioSource, ref IEnumerator __result)
            => __result = FadeWhileAlive(audioSource, __result);

        private static IEnumerator FadeWhileAlive(AudioSource source, IEnumerator fade)
        {
            // Native fades can outlive scene-owned audio, including their initial delay.
            // Preserve all native timing/callbacks while the target exists.
            while (source != null && fade != null && fade.MoveNext()) yield return fade.Current;
        }

        public static void OnLoaded(Scene scene, Action<string> log)
        {
            if (!scene.IsValid() || !scene.isLoaded || CrouchPlayer == null) return;
            FirstPersonController player = null;
            foreach (var root in scene.GetRootGameObjects())
                if (player == null) player = root.GetComponentInChildren<FirstPersonController>(true);
            int repaired = 0, removed = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var trigger in root.GetComponentsInChildren<BlindCrouchHeight>(true))
                {
                    if (CrouchPlayer.GetValue(trigger) as FirstPersonController != null) continue;
                    if (player != null) { CrouchPlayer.SetValue(trigger, player); repaired++; }
                    // Unity delivers trigger callbacks even to disabled MonoBehaviours.
                    // A decorative set with no player cannot use this orphan component.
                    else { UnityEngine.Object.Destroy(trigger); removed++; }
                }
            if (repaired + removed > 0) log?.Invoke("SceneRepair scene=" + scene.name + " crouchReferences=" + repaired + " orphanCrouchTriggers=" + removed);
        }
    }
}
