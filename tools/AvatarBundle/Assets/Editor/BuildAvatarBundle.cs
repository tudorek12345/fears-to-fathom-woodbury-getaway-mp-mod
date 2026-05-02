using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace WoodburyAvatarBundle
{
    public static class BuildAvatarBundle
    {
        private const string DefaultBundleName = "woodbury_avatars.bundle";
        private const string ManifestAssetPath = "Assets/AvatarBundle/avatar_manifest.json";
        private const string PrefabRoot = "Assets/AvatarBundle/Prefabs";

        private static readonly AvatarEntry[] Entries =
        {
            new AvatarEntry("quaternius_regular_male", "quaternius_regular_male", "Quaternius Regular Male"),
            new AvatarEntry("quaternius_regular_female", "quaternius_regular_female", "Quaternius Regular Female"),
            new AvatarEntry("quaternius_teen_male", "quaternius_teen_male", "Quaternius Teen Male"),
            new AvatarEntry("quaternius_teen_female", "quaternius_teen_female", "Quaternius Teen Female")
        };

        public static void Build()
        {
            var outputDir = GetCommandLineArg("-outputDir", Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "output", "avatars")));
            var bundleName = GetCommandLineArg("-bundleName", DefaultBundleName);

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            PlayerSettings.stripEngineCode = false;
            Directory.CreateDirectory(outputDir);
            QuaterniusAvatarPrefabBuilder.EnsurePrefabs();
            WriteManifest();
            AssignBundleNames(bundleName);

            var manifest = BuildPipeline.BuildAssetBundles(
                outputDir,
                BuildAssetBundleOptions.UncompressedAssetBundle,
                BuildTarget.StandaloneWindows64);

            if (manifest == null)
            {
                throw new InvalidOperationException("BuildPipeline.BuildAssetBundles returned null.");
            }

            var bundlePath = Path.Combine(outputDir, bundleName);
            if (!File.Exists(bundlePath))
            {
                throw new FileNotFoundException("Expected avatar bundle was not written.", bundlePath);
            }

            ValidateBuiltBundle(bundlePath);
            Debug.Log("Woodbury avatar bundle written: " + bundlePath);
        }

        private static void ValidateBuiltBundle(string bundlePath)
        {
            var bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
            {
                throw new InvalidOperationException("Built avatar bundle could not be loaded for validation: " + bundlePath);
            }

            try
            {
                var manifest = LoadAssetByNameOrPath<TextAsset>(bundle, "avatar_manifest.json");
                if (manifest == null)
                {
                    manifest = LoadAssetByNameOrPath<TextAsset>(bundle, "avatar_manifest");
                }

                if (manifest == null || string.IsNullOrWhiteSpace(manifest.text))
                {
                    throw new InvalidOperationException("Avatar bundle validation failed: avatar_manifest.json is missing or empty.");
                }

                foreach (var entry in Entries)
                {
                    if (manifest.text.IndexOf("\"id\": \"" + entry.Id + "\"", StringComparison.Ordinal) < 0)
                    {
                        throw new InvalidOperationException("Avatar bundle validation failed: manifest missing id " + entry.Id);
                    }

                    var prefab = LoadAssetByNameOrPath<GameObject>(bundle, entry.PrefabName + ".prefab");
                    if (prefab == null)
                    {
                        prefab = LoadAssetByNameOrPath<GameObject>(bundle, entry.PrefabName);
                    }

                    if (prefab == null)
                    {
                        throw new InvalidOperationException("Avatar bundle validation failed: missing prefab " + entry.PrefabName);
                    }

                    var rendererCount = prefab.GetComponentsInChildren<Renderer>(true).Length;
                    if (rendererCount <= 0)
                    {
                        throw new InvalidOperationException("Avatar bundle validation failed: prefab has no renderers " + entry.PrefabName);
                    }

                    var animators = prefab.GetComponentsInChildren<Animator>(true);
                    var hasController = false;
                    for (var i = 0; i < animators.Length; i++)
                    {
                        if (animators[i] != null && animators[i].runtimeAnimatorController != null)
                        {
                            hasController = true;
                            break;
                        }
                    }

                    if (!hasController)
                    {
                        throw new InvalidOperationException("Avatar bundle validation failed: prefab has no AnimatorController " + entry.PrefabName);
                    }
                }

                Debug.Log("Woodbury avatar bundle validated: " + bundlePath);
            }
            finally
            {
                bundle.Unload(false);
            }
        }

        private static T LoadAssetByNameOrPath<T>(AssetBundle bundle, string assetName)
            where T : UnityEngine.Object
        {
            if (bundle == null || string.IsNullOrWhiteSpace(assetName))
            {
                return null;
            }

            var direct = bundle.LoadAsset<T>(assetName);
            if (direct != null)
            {
                return direct;
            }

            var names = bundle.GetAllAssetNames();
            for (var i = 0; i < names.Length; i++)
            {
                var name = names[i];
                if (name.EndsWith("/" + assetName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(Path.GetFileName(name), assetName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(Path.GetFileNameWithoutExtension(name), assetName, StringComparison.OrdinalIgnoreCase))
                {
                    return bundle.LoadAsset<T>(name);
                }
            }

            return null;
        }

        private static void AssignBundleNames(string bundleName)
        {
            var missing = new List<string>();

            AssignBundleName(ManifestAssetPath, bundleName, missing);
            AssignBundleName("Assets/AvatarBundle/Controllers/QuaterniusLocomotion.controller", bundleName, missing);
            foreach (var entry in Entries)
            {
                AssignBundleName(PrefabRoot + "/" + entry.PrefabName + ".prefab", bundleName, missing);
            }

            if (missing.Count > 0)
            {
                throw new FileNotFoundException(
                    "Missing avatar bundle source assets. Import/create prefabs at these exact Unity paths:\n" +
                    string.Join("\n", missing.ToArray()));
            }

            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.SaveAssets();
        }

        private static void AssignBundleName(string assetPath, string bundleName, List<string> missing)
        {
            var importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null)
            {
                missing.Add(assetPath);
                return;
            }

            importer.assetBundleName = bundleName;
            importer.assetBundleVariant = string.Empty;
        }

        private static void WriteManifest()
        {
            var fullPath = ToFullPath(ManifestAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, CreateManifestJson(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(ManifestAssetPath, ImportAssetOptions.ForceUpdate);
        }

        private static string CreateManifestJson()
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"avatars\": [");

            for (var i = 0; i < Entries.Length; i++)
            {
                var entry = Entries[i];
                builder.AppendLine("    {");
                builder.AppendLine("      \"id\": \"" + entry.Id + "\",");
                builder.AppendLine("      \"prefabName\": \"" + entry.PrefabName + "\",");
                builder.AppendLine("      \"displayName\": \"" + entry.DisplayName + "\",");
                builder.AppendLine("      \"rigProfile\": \"ThirdPersonBasic\",");
                builder.AppendLine("      \"scale\": 1.0,");
                builder.AppendLine("      \"yOffset\": 0.0");
                builder.Append("    }");
                builder.AppendLine(i == Entries.Length - 1 ? string.Empty : ",");
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string ToFullPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string GetCommandLineArg(string name, string fallback)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return fallback;
        }

        private readonly struct AvatarEntry
        {
            public readonly string Id;
            public readonly string PrefabName;
            public readonly string DisplayName;

            public AvatarEntry(string id, string prefabName, string displayName)
            {
                Id = id;
                PrefabName = prefabName;
                DisplayName = displayName;
            }
        }
    }
}
