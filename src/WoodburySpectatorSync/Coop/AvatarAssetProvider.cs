using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using WoodburySpectatorSync.Config;

namespace WoodburySpectatorSync.Coop
{
    internal static class AvatarAssetProvider
    {
        public const string DefaultBundlePath = "BepInEx/plugins/WoodburySpectatorSync/avatars/woodbury_avatars.bundle";
        public const string DefaultAvatarId = "quaternius_regular_male";

        private static AssetBundle _bundle;
        private static string _bundlePath;
        private static AvatarManifest _manifest;
        private static string _lastMissingPath;
        private static string _lastLoadFailurePath;
        private static string _lastManifestFailurePath;

        public static bool TryResolve(Settings settings, ManualLogSource logger, Action<string> diagnosticsSink, out AvatarAssetSelection selection)
        {
            selection = null;

            if (settings == null)
            {
                return false;
            }

            var explicitPrefabPath = settings.CoopRemotePlayerPrefabPath != null
                ? settings.CoopRemotePlayerPrefabPath.Value
                : string.Empty;
            if (!string.IsNullOrWhiteSpace(explicitPrefabPath))
            {
                return false;
            }

            var configuredBundlePath = settings.CoopRemotePlayerAvatarBundlePath != null
                ? settings.CoopRemotePlayerAvatarBundlePath.Value
                : DefaultBundlePath;
            var resolvedBundlePath = ResolveBundlePath(configuredBundlePath);
            if (!File.Exists(resolvedBundlePath))
            {
                LogPathWarningOnce(ref _lastMissingPath, resolvedBundlePath, logger,
                    diagnosticsSink,
                    "Remote player avatar fallback: bundle not found (" + resolvedBundlePath + ").");
                return false;
            }

            if (!EnsureBundleLoaded(resolvedBundlePath, logger, diagnosticsSink))
            {
                return false;
            }

            if (_manifest == null)
            {
                _manifest = LoadManifest(_bundle, logger);
                if (_manifest == null)
                {
                    LogPathWarningOnce(ref _lastManifestFailurePath, resolvedBundlePath, logger,
                        diagnosticsSink,
                        "Remote player avatar fallback: avatar_manifest could not be loaded from bundle (" + resolvedBundlePath + ").");
                    return false;
                }
            }

            var avatarId = settings.CoopRemotePlayerAvatarId != null
                ? settings.CoopRemotePlayerAvatarId.Value
                : DefaultAvatarId;
            avatarId = string.IsNullOrWhiteSpace(avatarId) ? DefaultAvatarId : avatarId.Trim();

            var entry = FindEntry(_manifest, avatarId);
            if (entry == null)
            {
                LogWarning(logger, diagnosticsSink, "Remote player avatar fallback: avatar id not found (avatarId=" + avatarId +
                                             ", bundle=" + resolvedBundlePath + ").");
                return false;
            }

            var prefabName = string.IsNullOrWhiteSpace(entry.prefabName) ? entry.id : entry.prefabName.Trim();
            var prefab = LoadPrefab(_bundle, prefabName);
            if (prefab == null)
            {
                LogWarning(logger, diagnosticsSink, "Remote player avatar fallback: prefab not found (avatarId=" + avatarId +
                                             ", prefab=" + prefabName + ", bundle=" + resolvedBundlePath + ").");
                return false;
            }

            var configScale = settings.CoopRemotePlayerAvatarScale != null
                ? settings.CoopRemotePlayerAvatarScale.Value
                : 1f;
            var configYOffset = settings.CoopRemotePlayerAvatarYOffset != null
                ? settings.CoopRemotePlayerAvatarYOffset.Value
                : 0f;

            selection = new AvatarAssetSelection
            {
                AvatarId = avatarId,
                BundlePath = resolvedBundlePath,
                PrefabName = prefabName,
                DisplayName = string.IsNullOrWhiteSpace(entry.displayName) ? avatarId : entry.displayName.Trim(),
                RigProfile = string.IsNullOrWhiteSpace(entry.rigProfile) ? "ThirdPersonBasic" : entry.rigProfile.Trim(),
                Scale = SanitizeScale(entry.scale) * SanitizeScale(configScale),
                YOffset = SanitizeOffset(entry.yOffset) + SanitizeOffset(configYOffset),
                Prefab = prefab
            };

            LogInfo(logger, diagnosticsSink, "Remote player avatar source: AssetBundle avatarId=" + selection.AvatarId +
                            " bundle=" + selection.BundlePath +
                            " prefab=" + selection.PrefabName +
                            " display=\"" + selection.DisplayName + "\"" +
                            " rig=" + selection.RigProfile +
                            " scale=" + selection.Scale.ToString("0.###") +
                            " yOffset=" + selection.YOffset.ToString("0.###"));
            return true;
        }

        private static bool EnsureBundleLoaded(string resolvedBundlePath, ManualLogSource logger, Action<string> diagnosticsSink = null)
        {
            if (_bundle != null && string.Equals(_bundlePath, resolvedBundlePath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (_bundle != null)
            {
                _bundle.Unload(false);
                _bundle = null;
                _manifest = null;
                _bundlePath = null;
            }

            try
            {
                _bundle = AssetBundle.LoadFromFile(resolvedBundlePath);
                if (_bundle == null)
                {
                    LogPathWarningOnce(ref _lastLoadFailurePath, resolvedBundlePath, logger,
                        diagnosticsSink,
                        "Remote player avatar fallback: AssetBundle.LoadFromFile returned null (" + resolvedBundlePath + ").");
                    return false;
                }

                _bundlePath = resolvedBundlePath;
                _manifest = null;
                return true;
            }
            catch (Exception ex)
            {
                LogPathWarningOnce(ref _lastLoadFailurePath, resolvedBundlePath, logger,
                    diagnosticsSink,
                    "Remote player avatar fallback: AssetBundle.LoadFromFile failed (" + resolvedBundlePath + "): " + ex.Message);
                return false;
            }
        }

        private static AvatarManifest LoadManifest(AssetBundle bundle, ManualLogSource logger)
        {
            if (bundle == null) return null;

            var manifestAsset = bundle.LoadAsset<TextAsset>("avatar_manifest")
                                ?? bundle.LoadAsset<TextAsset>("avatar_manifest.json")
                                ?? bundle.LoadAsset<TextAsset>("AvatarManifest");

            if (manifestAsset == null)
            {
                var textAssets = bundle.LoadAllAssets<TextAsset>();
                for (var i = 0; i < textAssets.Length; i++)
                {
                    var textAsset = textAssets[i];
                    if (textAsset == null) continue;
                    if (string.Equals(textAsset.name, "avatar_manifest", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(textAsset.name, "avatar_manifest.json", StringComparison.OrdinalIgnoreCase) ||
                        textAsset.name.IndexOf("manifest", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        manifestAsset = textAsset;
                        break;
                    }
                }
            }

            if (manifestAsset == null || string.IsNullOrWhiteSpace(manifestAsset.text))
            {
                return null;
            }

            try
            {
                var manifest = JsonUtility.FromJson<AvatarManifest>(manifestAsset.text);
                if (manifest == null || manifest.avatars == null || manifest.avatars.Length == 0)
                {
                    return null;
                }

                return manifest;
            }
            catch (Exception ex)
            {
                LogWarning(logger, null, "Remote player avatar fallback: avatar_manifest JSON parse failed: " + ex.Message);
                return null;
            }
        }

        private static AvatarManifestEntry FindEntry(AvatarManifest manifest, string avatarId)
        {
            if (manifest == null || manifest.avatars == null || string.IsNullOrWhiteSpace(avatarId))
            {
                return null;
            }

            for (var i = 0; i < manifest.avatars.Length; i++)
            {
                var entry = manifest.avatars[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.id)) continue;
                if (string.Equals(entry.id.Trim(), avatarId, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }

        private static GameObject LoadPrefab(AssetBundle bundle, string prefabName)
        {
            if (bundle == null || string.IsNullOrWhiteSpace(prefabName))
            {
                return null;
            }

            var prefab = bundle.LoadAsset<GameObject>(prefabName);
            if (prefab != null)
            {
                return prefab;
            }

            var gameObjects = bundle.LoadAllAssets<GameObject>();
            for (var i = 0; i < gameObjects.Length; i++)
            {
                var candidate = gameObjects[i];
                if (candidate == null) continue;
                if (string.Equals(candidate.name, prefabName, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string ResolveBundlePath(string configuredPath)
        {
            configuredPath = string.IsNullOrWhiteSpace(configuredPath)
                ? DefaultBundlePath
                : configuredPath.Trim();

            if (Path.IsPathRooted(configuredPath))
            {
                return Path.GetFullPath(configuredPath);
            }

            var bepInExRoot = string.IsNullOrWhiteSpace(Paths.BepInExRootPath)
                ? Directory.GetCurrentDirectory()
                : Paths.BepInExRootPath;
            var normalized = configuredPath.Replace('/', Path.DirectorySeparatorChar);

            if (normalized.StartsWith("BepInEx" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                var gameRoot = Directory.GetParent(bepInExRoot);
                return Path.GetFullPath(Path.Combine(gameRoot != null ? gameRoot.FullName : bepInExRoot, normalized));
            }

            return Path.GetFullPath(Path.Combine(bepInExRoot, normalized));
        }

        private static float SanitizeScale(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                return 1f;
            }

            return Mathf.Max(0.01f, value);
        }

        private static float SanitizeOffset(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            return value;
        }

        private static void LogPathWarningOnce(ref string lastPath, string path, ManualLogSource logger, Action<string> diagnosticsSink, string message)
        {
            if (!string.Equals(lastPath, path, StringComparison.OrdinalIgnoreCase))
            {
                LogWarning(logger, diagnosticsSink, message);
                lastPath = path;
            }
        }

        private static void LogInfo(ManualLogSource logger, Action<string> diagnosticsSink, string message)
        {
            logger?.LogInfo(message);
            diagnosticsSink?.Invoke(message);
        }

        private static void LogWarning(ManualLogSource logger, Action<string> diagnosticsSink, string message)
        {
            logger?.LogWarning(message);
            diagnosticsSink?.Invoke(message);
        }

#pragma warning disable 0649
        [Serializable]
        private sealed class AvatarManifest
        {
            public AvatarManifestEntry[] avatars;
        }

        [Serializable]
        private sealed class AvatarManifestEntry
        {
            public string id;
            public string prefabName;
            public string displayName;
            public string rigProfile;
            public float scale = 1f;
            public float yOffset;
        }
#pragma warning restore 0649
    }

    internal sealed class AvatarAssetSelection
    {
        public string AvatarId;
        public string BundlePath;
        public string PrefabName;
        public string DisplayName;
        public string RigProfile;
        public float Scale = 1f;
        public float YOffset;
        public GameObject Prefab;
    }
}
