using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace WoodburyAvatarBundle
{
    public static class QuaterniusAvatarPrefabBuilder
    {
        private const string MaleModelPath = "Assets/AvatarBundle/Source/QuaterniusBaseCharacters/Superhero_Male_FullBody.fbx";
        private const string FemaleModelPath = "Assets/AvatarBundle/Source/QuaterniusBaseCharacters/Superhero_Female_FullBody.fbx";
        private const string AnimationLibraryPath = "Assets/AvatarBundle/Source/QuaterniusAnimations/UAL1_Standard.fbx";
        private const string ControllerPath = "Assets/AvatarBundle/Controllers/QuaterniusLocomotion.controller";
        private const string PrefabRoot = "Assets/AvatarBundle/Prefabs";

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
            EnsureAnimationImportSettings(AnimationLibraryPath);

            Directory.CreateDirectory(ToFullPath(PrefabRoot));
            var controller = EnsureAnimatorController();

            foreach (var spec in Prefabs)
            {
                CreateOrReplacePrefab(spec, controller);
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
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                changed = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
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

        private static void EnsureAnimationImportSettings(string assetPath)
        {
            if (!File.Exists(ToFullPath(assetPath)))
            {
                throw new FileNotFoundException("Missing Quaternius animation library.", assetPath);
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                return;
            }

            var changed = false;
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                changed = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }

            if (!importer.importAnimation)
            {
                importer.importAnimation = true;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static AnimatorController EnsureAnimatorController()
        {
            Directory.CreateDirectory(ToFullPath(Path.GetDirectoryName(ControllerPath).Replace('\\', '/')));
            AssetDatabase.DeleteAsset(ControllerPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Strafe", AnimatorControllerParameterType.Float);
            controller.AddParameter("Forward", AnimatorControllerParameterType.Float);
            controller.AddParameter("GroundSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsCrouching", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsJumping", AnimatorControllerParameterType.Bool);

            var clips = LoadAnimationClips(AnimationLibraryPath);
            if (clips.Length == 0)
            {
                throw new InvalidOperationException("Animation library has no usable AnimationClip assets: " + AnimationLibraryPath);
            }

            var idle = FindClip(clips, "idle") ?? (clips.Length > 0 ? clips[0] : null);
            var walk = FindClip(clips, "walk") ?? idle;
            var run = FindClip(clips, "run") ?? walk;

            var tree = new BlendTree
            {
                name = "LocomotionBlend",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "GroundSpeed",
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(tree, ControllerPath);

            if (idle != null) tree.AddChild(idle, 0f);
            if (walk != null && walk != idle) tree.AddChild(walk, 1.5f);
            if (run != null && run != walk) tree.AddChild(run, 4.0f);

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.AddState("Locomotion");
            state.motion = tree;
            stateMachine.defaultState = state;

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(ControllerPath, ImportAssetOptions.ForceUpdate);
            return controller;
        }

        private static void CreateOrReplacePrefab(AvatarPrefabSpec spec, RuntimeAnimatorController controller)
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
                EnsureAnimator(model, controller);

                var prefabPath = PrefabRoot + "/" + spec.PrefabName + ".prefab";
                Directory.CreateDirectory(ToFullPath(PrefabRoot));
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void EnsureAnimator(GameObject root, RuntimeAnimatorController controller)
        {
            if (root == null) return;

            Avatar avatar = null;
            var animators = root.GetComponentsInChildren<Animator>(true);
            for (var i = 0; i < animators.Length; i++)
            {
                var existing = animators[i];
                if (existing == null) continue;
                if (avatar == null && existing.avatar != null)
                {
                    avatar = existing.avatar;
                }

                if (existing.gameObject != root)
                {
                    UnityEngine.Object.DestroyImmediate(existing, true);
                }
            }

            var animator = root.GetComponent<Animator>();
            if (animator == null)
            {
                animator = root.AddComponent<Animator>();
            }

            if (avatar != null)
            {
                animator.avatar = avatar;
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        private static AnimationClip[] LoadAnimationClips(string assetPath)
        {
            var objects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            var clips = new System.Collections.Generic.List<AnimationClip>();
            for (var i = 0; i < objects.Length; i++)
            {
                var clip = objects[i] as AnimationClip;
                if (clip == null) continue;
                if (clip.name.StartsWith("__", StringComparison.Ordinal)) continue;
                clips.Add(clip);
            }

            return clips.ToArray();
        }

        private static AnimationClip FindClip(AnimationClip[] clips, string contains)
        {
            if (clips == null || string.IsNullOrEmpty(contains)) return null;
            for (var i = 0; i < clips.Length; i++)
            {
                var clip = clips[i];
                if (clip == null) continue;
                if (clip.name.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return clip;
                }
            }

            return null;
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
