using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace WoodburyAvatarBundle
{
    public static class QuaterniusAvatarPrefabBuilder
    {
        private const string MaleModelPath = "Assets/AvatarBundle/Source/QuaterniusBaseCharacters/Superhero_Male_FullBody.fbx";
        private const string FemaleModelPath = "Assets/AvatarBundle/Source/QuaterniusBaseCharacters/Superhero_Female_FullBody.fbx";
        private const string ControllerPath = "Assets/AvatarBundle/Controllers/QuaterniusLocomotion.controller";
        private const string PrefabRoot = "Assets/AvatarBundle/Prefabs";
        private const string ModuleReferenceScenePath = "Assets/AvatarBundle/Scenes/AvatarBundleModuleRefs.unity";

        private static readonly AvatarPrefabSpec[] Prefabs =
        {
            new AvatarPrefabSpec("quaternius_regular_male", MaleModelPath, 1.0f),
            new AvatarPrefabSpec("quaternius_regular_female", FemaleModelPath, 1.0f),
            new AvatarPrefabSpec("quaternius_teen_male", MaleModelPath, 0.88f),
            new AvatarPrefabSpec("quaternius_teen_female", FemaleModelPath, 0.88f)
        };

        public static void EnsurePrefabs()
        {
            EnsureModelImportSettings(MaleModelPath);
            EnsureModelImportSettings(FemaleModelPath);

            Directory.CreateDirectory(ToFullPath(PrefabRoot));
            RemoveAnimationBuildReferences();

            foreach (var spec in Prefabs)
            {
                CreateOrReplacePrefab(spec);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureModelImportSettings(string assetPath)
        {
            if (!File.Exists(ToFullPath(assetPath)))
            {
                Debug.LogWarning("Avatar source asset is missing: " + assetPath);
                return;
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                return;
            }

            var changed = false;
            if (importer.animationType != ModelImporterAnimationType.None)
            {
                importer.animationType = ModelImporterAnimationType.None;
                changed = true;
            }

            if (importer.importAnimation)
            {
                importer.importAnimation = false;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static void RemoveAnimationBuildReferences()
        {
            EditorBuildSettings.scenes = Array.Empty<EditorBuildSettingsScene>();
            AssetDatabase.DeleteAsset(ModuleReferenceScenePath);
            AssetDatabase.DeleteAsset(ControllerPath);
        }

        private static void CreateOrReplacePrefab(AvatarPrefabSpec spec)
        {
            if (!File.Exists(ToFullPath(spec.SourceModelPath)))
            {
                throw new FileNotFoundException("Missing Quaternius source model.", spec.SourceModelPath);
            }

            var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.SourceModelPath);
            if (modelPrefab == null)
            {
                throw new InvalidOperationException("Could not load source model prefab: " + spec.SourceModelPath);
            }

            var root = new GameObject(spec.PrefabName);
            try
            {
                var model = PrefabUtility.InstantiatePrefab(modelPrefab) as GameObject;
                if (model == null)
                {
                    throw new InvalidOperationException("Could not instantiate source model prefab: " + spec.SourceModelPath);
                }

                model.name = spec.PrefabName + "_model";
                model.transform.SetParent(root.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one * spec.ModelScale;

                PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                RemoveAnimators(model);

                var prefabPath = PrefabRoot + "/" + spec.PrefabName + ".prefab";
                Directory.CreateDirectory(ToFullPath(PrefabRoot));
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void RemoveAnimators(GameObject root)
        {
            if (root == null) return;

            var animators = root.GetComponentsInChildren<Animator>(true);
            for (var i = 0; i < animators.Length; i++)
            {
                if (animators[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(animators[i], true);
                }
            }
        }

        private static string ToFullPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private readonly struct AvatarPrefabSpec
        {
            public readonly string PrefabName;
            public readonly string SourceModelPath;
            public readonly float ModelScale;

            public AvatarPrefabSpec(string prefabName, string sourceModelPath, float modelScale)
            {
                PrefabName = prefabName;
                SourceModelPath = sourceModelPath;
                ModelScale = modelScale;
            }
        }
    }
}
