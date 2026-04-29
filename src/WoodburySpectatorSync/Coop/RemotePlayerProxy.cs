using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using WoodburySpectatorSync.Config;
using WoodburySpectatorSync.Net;

namespace WoodburySpectatorSync.Coop
{
    public sealed class RemotePlayerProxy
    {
        private sealed class AnimatorRigMap
        {
            public readonly string RigName;
            public readonly string[] StrafeFloatNames;
            public readonly string[] ForwardFloatNames;
            public readonly string[] SpeedFloatNames;
            public readonly string[] MovingBoolNames;
            public readonly string[] SprintBoolNames;
            public readonly string[] CrouchBoolNames;
            public readonly string[] JumpBoolNames;

            public AnimatorRigMap(
                string rigName,
                string[] strafeFloatNames,
                string[] forwardFloatNames,
                string[] speedFloatNames,
                string[] movingBoolNames,
                string[] sprintBoolNames,
                string[] crouchBoolNames,
                string[] jumpBoolNames)
            {
                RigName = rigName ?? string.Empty;
                StrafeFloatNames = strafeFloatNames ?? Array.Empty<string>();
                ForwardFloatNames = forwardFloatNames ?? Array.Empty<string>();
                SpeedFloatNames = speedFloatNames ?? Array.Empty<string>();
                MovingBoolNames = movingBoolNames ?? Array.Empty<string>();
                SprintBoolNames = sprintBoolNames ?? Array.Empty<string>();
                CrouchBoolNames = crouchBoolNames ?? Array.Empty<string>();
                JumpBoolNames = jumpBoolNames ?? Array.Empty<string>();
            }
        }

        private sealed class SourceDescriptor
        {
            public GameObject SourceObject;
            public bool UseSyntheticCapsule;
            public Vector3 InitialPosition;
            public Quaternion InitialRotation = Quaternion.identity;
            public string RigProfile;
            public bool UseAvatarWrapper;
            public float AvatarScale = 1f;
            public float AvatarYOffset;
            public string SourceKind = string.Empty;
            public string SourceName = string.Empty;
            public string FallbackReason = string.Empty;
        }

        private static readonly AnimatorRigMap WoodburyFpcRig = new AnimatorRigMap(
            "WoodburyFpc",
            new[] { "MoveX", "Horizontal", "Strafe", "InputX" },
            new[] { "MoveY", "Vertical", "Forward", "InputY" },
            new[] { "Speed", "Velocity", "Move", "MoveSpeed", "GroundSpeed" },
            new[] { "IsMoving", "Moving", "Walk", "IsWalking" },
            new[] { "IsSprinting", "Sprint", "Run", "IsRunning" },
            new[] { "IsCrouching", "Crouch" },
            new[] { "IsJumping", "Jump" });

        private static readonly AnimatorRigMap ThirdPersonBasicRig = new AnimatorRigMap(
            "ThirdPersonBasic",
            new[] { "Strafe", "MoveX", "Horizontal", "InputX", "X" },
            new[] { "Forward", "MoveY", "Vertical", "InputY", "Y" },
            new[] { "GroundSpeed", "Speed", "MoveSpeed", "Velocity" },
            new[] { "IsMoving", "Moving", "Walk", "IsWalking" },
            new[] { "IsRunning", "Sprint", "Run", "IsSprinting" },
            new[] { "IsCrouching", "Crouch" },
            new[] { "IsJumping", "Jump" });

        private static readonly AnimatorRigMap LegacyHumanoidRig = new AnimatorRigMap(
            "LegacyHumanoid",
            new[] { "X", "InputX", "MoveX", "Horizontal" },
            new[] { "Y", "InputY", "MoveY", "Vertical" },
            new[] { "Speed", "Velocity", "Move", "MoveSpeed" },
            new[] { "IsMoving", "Moving", "Walk", "IsWalking" },
            new[] { "Sprint", "Run", "IsRunning", "IsSprinting" },
            new[] { "Crouch", "IsCrouching" },
            new[] { "Jump", "IsJumping" });

        private readonly GameObject _root;
        private readonly CharacterController _characterController;
        private readonly bool _allowCharacterController;
        private readonly Transform _cameraTransform;
        private readonly Animator _animator;
        private readonly HashSet<int> _animFloatParams = new HashSet<int>();
        private readonly HashSet<int> _animBoolParams = new HashSet<int>();
        private readonly AnimatorRigMap _animRig;
        private Vector3 _lastRootPosition;
        private float _lastSampleTime;
        private bool _hasMotionSample;

        public Transform Root => _root != null ? _root.transform : null;
        public Transform CameraTransform => _cameraTransform;

        public static RemotePlayerProxy Create(
            Settings settings,
            FirstPersonController fallbackSource,
            Color tint,
            ManualLogSource logger,
            Action<string> diagnosticsSink = null,
            bool allowCharacterController = false)
        {
            var source = ResolveSourceObject(settings, fallbackSource, logger, diagnosticsSink);
            if (source == null || (source.SourceObject == null && !source.UseSyntheticCapsule))
            {
                return null;
            }

            return new RemotePlayerProxy(
                source,
                tint,
                allowCharacterController,
                logger,
                diagnosticsSink);
        }

        public RemotePlayerProxy(FirstPersonController source, Color tint, bool allowCharacterController = false)
            : this(new SourceDescriptor
            {
                SourceObject = source != null ? source.gameObject : null,
                InitialPosition = source != null ? source.transform.position : Vector3.zero,
                InitialRotation = source != null ? source.transform.rotation : Quaternion.identity,
                RigProfile = "Auto",
                SourceKind = "FpcClone",
                SourceName = source != null ? source.name : string.Empty
            }, tint, allowCharacterController, null, null)
        {
        }

        private RemotePlayerProxy(
            SourceDescriptor source,
            Color tint,
            bool allowCharacterController,
            ManualLogSource logger,
            Action<string> diagnosticsSink)
        {
            if (source == null || (source.SourceObject == null && !source.UseSyntheticCapsule))
            {
                throw new ArgumentNullException(nameof(source));
            }

            _allowCharacterController = allowCharacterController;
            if (source.UseSyntheticCapsule)
            {
                _root = new GameObject("CoopRemotePlayer");
                _root.transform.SetPositionAndRotation(source.InitialPosition, source.InitialRotation);
            }
            else if (source.UseAvatarWrapper)
            {
                _root = new GameObject("CoopRemotePlayer");
                _root.transform.SetPositionAndRotation(source.InitialPosition, source.InitialRotation);

                var model = UnityEngine.Object.Instantiate(source.SourceObject);
                model.name = "CoopRemotePlayerAvatar";
                model.transform.SetParent(_root.transform, false);
                model.transform.localPosition = new Vector3(0f, source.AvatarYOffset, 0f);
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one * Mathf.Max(0.01f, source.AvatarScale);
            }
            else
            {
                _root = UnityEngine.Object.Instantiate(source.SourceObject, source.InitialPosition, source.InitialRotation);
                _root.name = "CoopRemotePlayer";
            }

            HardenCloneForRemoteAvatar();

            var fpc = _root.GetComponent<FirstPersonController>();
            if (fpc != null)
            {
                fpc.enabled = false;
            }

            var cameras = _root.GetComponentsInChildren<Camera>(true);
            foreach (var cam in cameras)
            {
                cam.enabled = false;
            }

            var listeners = _root.GetComponentsInChildren<AudioListener>(true);
            foreach (var listener in listeners)
            {
                listener.enabled = false;
            }

            var audioSources = _root.GetComponentsInChildren<AudioSource>(true);
            foreach (var audio in audioSources)
            {
                audio.enabled = false;
            }

            _characterController = _root.GetComponent<CharacterController>();
            _cameraTransform = cameras.Length > 0 ? cameras[0].transform : null;

            if (!_allowCharacterController)
            {
                DisableColliders();
            }

            _animator = _root.GetComponentInChildren<Animator>(true);
            if (_animator != null)
            {
                foreach (var param in _animator.parameters)
                {
                    if (param.type == AnimatorControllerParameterType.Float)
                    {
                        _animFloatParams.Add(param.nameHash);
                    }
                    else if (param.type == AnimatorControllerParameterType.Bool)
                    {
                        _animBoolParams.Add(param.nameHash);
                    }
                }
            }

            _animRig = ResolveRigMap(source.RigProfile);

            var usedFallbackBody = EnsureVisibleBody(tint, logger, diagnosticsSink);
            LogAvatarDiagnostics(source, usedFallbackBody, logger, diagnosticsSink);
            if (_root != null)
            {
                _lastRootPosition = _root.transform.position;
                _lastSampleTime = Time.realtimeSinceStartup;
                _hasMotionSample = true;
            }
        }

        public void SetActive(bool value)
        {
            if (_root != null)
            {
                _root.SetActive(value);
            }
        }

        public void ApplyTransform(PlayerTransformState state)
        {
            if (_root == null) return;

            if (_characterController != null && _allowCharacterController)
            {
                _characterController.enabled = false;
            }

            _root.transform.SetPositionAndRotation(state.Position, state.Rotation);
            DriveAnimatorFromTransform(state.Position, state.Rotation);

            if (_cameraTransform != null)
            {
                _cameraTransform.rotation = state.CameraRotation;
                _cameraTransform.position = state.CameraPosition;
            }

            if (_characterController != null && _allowCharacterController)
            {
                _characterController.enabled = true;
            }
        }

        private void DriveAnimatorFromTransform(Vector3 newPosition, Quaternion newRotation)
        {
            if (_animator == null) return;

            var now = Time.realtimeSinceStartup;
            if (!_hasMotionSample)
            {
                _lastRootPosition = newPosition;
                _lastSampleTime = now;
                _hasMotionSample = true;
                return;
            }

            var deltaTime = Mathf.Max(0.001f, now - _lastSampleTime);
            var worldVelocity = (newPosition - _lastRootPosition) / deltaTime;
            var localVelocity = Quaternion.Inverse(newRotation) * worldVelocity;
            var planarSpeed = new Vector2(worldVelocity.x, worldVelocity.z).magnitude;
            var moving = planarSpeed > 0.05f;
            var sprinting = planarSpeed > 3f;

            SetFloatFromRig(_animRig.StrafeFloatNames, localVelocity.x);
            SetFloatFromRig(_animRig.ForwardFloatNames, localVelocity.z);
            SetFloatFromRig(_animRig.SpeedFloatNames, planarSpeed);

            SetBoolFromRig(_animRig.MovingBoolNames, moving);
            SetBoolFromRig(_animRig.SprintBoolNames, sprinting);
            SetBoolFromRig(_animRig.CrouchBoolNames, false);
            SetBoolFromRig(_animRig.JumpBoolNames, false);

            _lastRootPosition = newPosition;
            _lastSampleTime = now;
        }

        public void ApplyInput(PlayerInputState input)
        {
            if (_animator == null) return;

            SetFloatFromRig(_animRig.StrafeFloatNames, input.MoveX);
            SetFloatFromRig(_animRig.ForwardFloatNames, input.MoveY);
            SetFloatFromRig(_animRig.SpeedFloatNames, new Vector2(input.MoveX, input.MoveY).magnitude);

            SetBoolFromRig(_animRig.CrouchBoolNames, input.Crouch);
            SetBoolFromRig(_animRig.SprintBoolNames, input.Sprint);
            SetBoolFromRig(_animRig.JumpBoolNames, input.Jump);
            SetBoolFromRig(_animRig.MovingBoolNames, Mathf.Abs(input.MoveX) > 0.01f || Mathf.Abs(input.MoveY) > 0.01f);
        }

        private void SetFloatFromRig(string[] names, float value)
        {
            if (names == null || names.Length == 0) return;

            for (var i = 0; i < names.Length; i++)
            {
                var name = names[i];
                if (string.IsNullOrEmpty(name)) continue;

                var hash = Animator.StringToHash(name);
                if (_animFloatParams.Contains(hash))
                {
                    _animator.SetFloat(hash, value);
                }
            }
        }

        private void SetBoolFromRig(string[] names, bool value)
        {
            if (names == null || names.Length == 0) return;

            for (var i = 0; i < names.Length; i++)
            {
                var name = names[i];
                if (string.IsNullOrEmpty(name)) continue;

                var hash = Animator.StringToHash(name);
                if (_animBoolParams.Contains(hash))
                {
                    _animator.SetBool(hash, value);
                }
            }
        }

        private AnimatorRigMap ResolveRigMap(string rigProfile)
        {
            if (TryGetRigByName(rigProfile, out var explicitRig))
            {
                return explicitRig;
            }

            if (_animator == null)
            {
                return WoodburyFpcRig;
            }

            if (HasFloatParam("MoveX") && HasFloatParam("MoveY"))
            {
                return WoodburyFpcRig;
            }

            if (HasFloatParam("Strafe") || HasFloatParam("Forward"))
            {
                return ThirdPersonBasicRig;
            }

            if (HasFloatParam("X") || HasFloatParam("Y"))
            {
                return LegacyHumanoidRig;
            }

            return WoodburyFpcRig;
        }

        private bool HasFloatParam(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return _animFloatParams.Contains(Animator.StringToHash(name));
        }

        private static bool TryGetRigByName(string rigProfile, out AnimatorRigMap map)
        {
            map = null;
            if (string.IsNullOrWhiteSpace(rigProfile)) return false;
            if (string.Equals(rigProfile, "Auto", StringComparison.OrdinalIgnoreCase)) return false;

            if (string.Equals(rigProfile, WoodburyFpcRig.RigName, StringComparison.OrdinalIgnoreCase))
            {
                map = WoodburyFpcRig;
                return true;
            }

            if (string.Equals(rigProfile, ThirdPersonBasicRig.RigName, StringComparison.OrdinalIgnoreCase))
            {
                map = ThirdPersonBasicRig;
                return true;
            }

            if (string.Equals(rigProfile, LegacyHumanoidRig.RigName, StringComparison.OrdinalIgnoreCase))
            {
                map = LegacyHumanoidRig;
                return true;
            }

            return false;
        }

        private static SourceDescriptor ResolveSourceObject(
            Settings settings,
            FirstPersonController fallbackSource,
            ManualLogSource logger,
            Action<string> diagnosticsSink)
        {
            var rigProfile = ResolveConfiguredRig(settings, "Auto");
            var path = settings != null && settings.CoopRemotePlayerPrefabPath != null
                ? settings.CoopRemotePlayerPrefabPath.Value
                : string.Empty;
            path = string.IsNullOrWhiteSpace(path) ? string.Empty : path.Trim();

            if (!string.IsNullOrEmpty(path))
            {
                var sourceByPath = NetPath.FindByPath(path);
                if (sourceByPath != null)
                {
                    LogInfo(logger, diagnosticsSink, "Remote player avatar source: NetPath path=" + path + " rig=" + rigProfile);
                    return new SourceDescriptor
                    {
                        SourceObject = sourceByPath.gameObject,
                        InitialPosition = sourceByPath.position,
                        InitialRotation = sourceByPath.rotation,
                        RigProfile = rigProfile,
                        SourceKind = "NetPath",
                        SourceName = path
                    };
                }

                var sourceByResources = Resources.Load<GameObject>(path);
                if (sourceByResources != null)
                {
                    LogInfo(logger, diagnosticsSink, "Remote player avatar source: Resources path=" + path + " rig=" + rigProfile);
                    return new SourceDescriptor
                    {
                        SourceObject = sourceByResources,
                        InitialPosition = fallbackSource != null ? fallbackSource.transform.position : Vector3.zero,
                        InitialRotation = fallbackSource != null ? fallbackSource.transform.rotation : Quaternion.identity,
                        RigProfile = rigProfile,
                        SourceKind = "Resources",
                        SourceName = path
                    };
                }

                LogWarning(logger, diagnosticsSink, "RemotePlayerPrefabPath not found (" + path + "), falling back to configured avatar source.");
            }

            var avatarSource = ResolveAvatarSource(settings);
            if (avatarSource == RemotePlayerAvatarSource.Capsule)
            {
                return CreateCapsuleSource(settings, fallbackSource, "configured source=Capsule");
            }

            if (avatarSource == RemotePlayerAvatarSource.Auto || avatarSource == RemotePlayerAvatarSource.GameModel)
            {
                if (TryResolveGameModelAvatar(settings, fallbackSource, logger, diagnosticsSink, out var gameModel))
                {
                    return gameModel;
                }

                if (avatarSource == RemotePlayerAvatarSource.GameModel)
                {
                    return CreateCapsuleSource(settings, fallbackSource, "no safe in-scene game model found");
                }
            }

            var avatarId = GetConfiguredAvatarId(settings);
            var shouldTryBundle = avatarSource == RemotePlayerAvatarSource.AssetBundle ||
                                  (avatarSource == RemotePlayerAvatarSource.Auto && !IsSceneAutoAvatarId(avatarId));
            if (shouldTryBundle)
            {
                if (AvatarAssetProvider.TryResolve(settings, logger, diagnosticsSink, out var avatarSelection))
                {
                    return new SourceDescriptor
                    {
                        SourceObject = avatarSelection.Prefab,
                        InitialPosition = fallbackSource != null ? fallbackSource.transform.position : Vector3.zero,
                        InitialRotation = fallbackSource != null ? fallbackSource.transform.rotation : Quaternion.identity,
                        RigProfile = ResolveConfiguredRig(settings, avatarSelection.RigProfile),
                        UseAvatarWrapper = true,
                        AvatarScale = avatarSelection.Scale,
                        AvatarYOffset = avatarSelection.YOffset,
                        SourceKind = "AssetBundle",
                        SourceName = avatarSelection.AvatarId
                    };
                }
            }

            return CreateCapsuleSource(settings, fallbackSource, avatarSource == RemotePlayerAvatarSource.AssetBundle
                ? "AssetBundle avatar unavailable"
                : "no game model or AssetBundle avatar available");
        }

        private static RemotePlayerAvatarSource ResolveAvatarSource(Settings settings)
        {
            return settings != null && settings.CoopRemotePlayerAvatarSource != null
                ? settings.CoopRemotePlayerAvatarSource.Value
                : RemotePlayerAvatarSource.Auto;
        }

        private static string GetConfiguredAvatarId(Settings settings)
        {
            var avatarId = settings != null && settings.CoopRemotePlayerAvatarId != null
                ? settings.CoopRemotePlayerAvatarId.Value
                : string.Empty;
            return string.IsNullOrWhiteSpace(avatarId) ? "woodbury_scene_auto" : avatarId.Trim();
        }

        private static bool IsSceneAutoAvatarId(string avatarId)
        {
            return string.IsNullOrWhiteSpace(avatarId) ||
                   string.Equals(avatarId, "woodbury_scene_auto", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryResolveGameModelAvatar(
            Settings settings,
            FirstPersonController fallbackSource,
            ManualLogSource logger,
            Action<string> diagnosticsSink,
            out SourceDescriptor source)
        {
            source = null;
            var avatarId = GetConfiguredAvatarId(settings);
            if (!TryFindGameModelAvatar(avatarId, out var candidate, out var sourceName))
            {
                LogWarning(logger, diagnosticsSink, "Remote player avatar fallback: no safe in-scene game model found id=" +
                    avatarId + " scene=" + SceneManager.GetActiveScene().name);
                return false;
            }

            source = new SourceDescriptor
            {
                SourceObject = candidate,
                InitialPosition = fallbackSource != null ? fallbackSource.transform.position : candidate.transform.position,
                InitialRotation = fallbackSource != null ? fallbackSource.transform.rotation : candidate.transform.rotation,
                RigProfile = ResolveConfiguredRig(settings, "Auto"),
                SourceKind = "GameModel",
                SourceName = sourceName
            };

            LogInfo(logger, diagnosticsSink, "Remote player avatar source: GameModel id=" + avatarId +
                " object=" + GetTransformPath(candidate.transform) +
                " renderers=" + CountRenderers(candidate) +
                " bounds=" + FormatBounds(candidate));
            return true;
        }

        private static bool TryFindGameModelAvatar(string avatarId, out GameObject candidate, out string sourceName)
        {
            candidate = null;
            sourceName = string.Empty;

            var normalizedId = string.IsNullOrWhiteSpace(avatarId) ? "woodbury_scene_auto" : avatarId.Trim().ToLowerInvariant();
            switch (normalizedId)
            {
                case "woodbury_pizzeria_backpacker":
                    return TryFindNamedAvatar(new[] { "BackpackerV2", "Backpacker" }, out candidate, out sourceName) ||
                           TryFindComponentAvatar<PizzeriaHiker>(out candidate, out sourceName);
                case "woodbury_pizzeria_hobo":
                    return TryFindComponentAvatar<Hobo>(out candidate, out sourceName) ||
                           TryFindNamedAvatar(new[] { "hobo", "Hobo" }, out candidate, out sourceName);
                case "woodbury_pizzeria_mike":
                    return TryFindComponentAvatar<MikePizzeria>(out candidate, out sourceName) ||
                           TryFindNamedAvatar(new[] { "Mike New", "MikePizzeria" }, out candidate, out sourceName);
                case "woodbury_cabin_mike":
                    return TryFindCabinMikeAvatar(out candidate, out sourceName);
                case "woodbury_roadtrip_mike":
                    return TryFindComponentAvatar<MikeInCar>(out candidate, out sourceName) ||
                           TryFindNamedAvatar(new[] { "MikeInCar", "Mike In Car" }, out candidate, out sourceName);
            }

            var sceneName = SceneManager.GetActiveScene().name ?? string.Empty;
            if (sceneName.IndexOf("Pizzeria", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return TryFindComponentAvatar<MikePizzeria>(out candidate, out sourceName) ||
                       TryFindNamedAvatar(new[] { "BackpackerV2", "Backpacker", "Mike New" }, out candidate, out sourceName) ||
                       TryFindComponentAvatar<PizzeriaHiker>(out candidate, out sourceName) ||
                       TryFindComponentAvatar<Hobo>(out candidate, out sourceName);
            }

            if (sceneName.IndexOf("RoadTrip", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return TryFindComponentAvatar<MikeInCar>(out candidate, out sourceName) ||
                       TryFindNamedAvatar(new[] { "MikeInCar", "Mike In Car" }, out candidate, out sourceName);
            }

            if (sceneName.IndexOf("Cabin", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return TryFindCabinMikeAvatar(out candidate, out sourceName);
            }

            return TryFindCabinMikeAvatar(out candidate, out sourceName) ||
                   TryFindComponentAvatar<MikePizzeria>(out candidate, out sourceName) ||
                   TryFindComponentAvatar<MikeInCar>(out candidate, out sourceName);
        }

        private static bool TryFindCabinMikeAvatar(out GameObject candidate, out string sourceName)
        {
            return TryFindComponentAvatar<MikeCabinCookController>(out candidate, out sourceName) ||
                   TryFindComponentAvatar<MikeFishing>(out candidate, out sourceName) ||
                   TryFindComponentAvatar<MikePostEating>(out candidate, out sourceName) ||
                   TryFindComponentAvatar<MikeAfterHiding>(out candidate, out sourceName) ||
                   TryFindComponentAvatar<MikeRizzlerController>(out candidate, out sourceName) ||
                   TryFindComponentAvatar<MikeEndGame>(out candidate, out sourceName) ||
                   TryFindComponentAvatar<MikeCabin>(out candidate, out sourceName);
        }

        private static bool TryFindComponentAvatar<T>(out GameObject candidate, out string sourceName)
            where T : Component
        {
            candidate = null;
            sourceName = string.Empty;

            var components = UnityEngine.Object.FindObjectsOfType<T>();
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component == null) continue;
                if (!IsSafeAvatarCandidate(component.gameObject)) continue;

                candidate = component.gameObject;
                sourceName = typeof(T).Name + ":" + GetTransformPath(component.transform);
                return true;
            }

            return false;
        }

        private static bool TryFindNamedAvatar(string[] names, out GameObject candidate, out string sourceName)
        {
            candidate = null;
            sourceName = string.Empty;
            if (names == null || names.Length == 0) return false;

            var transforms = UnityEngine.Object.FindObjectsOfType<Transform>();
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || transform.gameObject == null) continue;

                for (var nameIndex = 0; nameIndex < names.Length; nameIndex++)
                {
                    var name = names[nameIndex];
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    if (!string.Equals(transform.name, name, StringComparison.OrdinalIgnoreCase) &&
                        transform.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    if (!IsSafeAvatarCandidate(transform.gameObject)) continue;

                    candidate = transform.gameObject;
                    sourceName = "Name:" + GetTransformPath(transform);
                    return true;
                }
            }

            return false;
        }

        private static bool IsSafeAvatarCandidate(GameObject candidate)
        {
            if (candidate == null) return false;
            if (!candidate.activeInHierarchy) return false;

            var scene = candidate.scene;
            var activeScene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene != activeScene) return false;

            var path = GetTransformPath(candidate.transform);
            if (path.IndexOf("CoopRemotePlayer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("CoopHostAvatar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("CoopClientAvatar", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            if (candidate.GetComponentInParent<FirstPersonController>() != null ||
                candidate.GetComponentInParent<PlayerController>() != null)
            {
                return false;
            }

            return CountVisibleRenderers(candidate) > 0;
        }

        private static SourceDescriptor CreateCapsuleSource(Settings settings, FirstPersonController fallbackSource, string reason)
        {
            return new SourceDescriptor
            {
                UseSyntheticCapsule = true,
                InitialPosition = fallbackSource != null ? fallbackSource.transform.position : Vector3.zero,
                InitialRotation = fallbackSource != null ? fallbackSource.transform.rotation : Quaternion.identity,
                RigProfile = ResolveConfiguredRig(settings, "Auto"),
                SourceKind = "Capsule",
                SourceName = "compact_fallback",
                FallbackReason = reason ?? string.Empty
            };
        }

        private static string ResolveConfiguredRig(Settings settings, string fallbackRig)
        {
            var configuredRig = settings != null && settings.CoopRemotePlayerRig != null
                ? settings.CoopRemotePlayerRig.Value
                : "Auto";
            if (!string.IsNullOrWhiteSpace(configuredRig) &&
                !string.Equals(configuredRig, "Auto", StringComparison.OrdinalIgnoreCase))
            {
                return configuredRig.Trim();
            }

            return string.IsNullOrWhiteSpace(fallbackRig) ? "Auto" : fallbackRig.Trim();
        }

        private bool EnsureVisibleBody(Color tint, ManualLogSource logger, Action<string> diagnosticsSink)
        {
            if (CountVisibleRenderers(_root) > 0) return false;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(_root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.35f, 0.9f, 0.35f);

            var collider = body.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                UnityEngine.Object.Destroy(collider);
            }

            var renderer = body.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = tint;
            }

            LogWarning(logger, diagnosticsSink, "Remote player avatar fallback body created: compact capsule height=1.8 colliders=disabled");
            return true;
        }

        private void HardenCloneForRemoteAvatar()
        {
            if (_root == null) return;

            var behaviours = _root.GetComponentsInChildren<MonoBehaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null) continue;
                behaviour.enabled = false;
            }

            var navAgents = _root.GetComponentsInChildren<NavMeshAgent>(true);
            for (var i = 0; i < navAgents.Length; i++)
            {
                var agent = navAgents[i];
                if (agent == null) continue;
                agent.enabled = false;
            }

            var rigidbodies = _root.GetComponentsInChildren<Rigidbody>(true);
            for (var i = 0; i < rigidbodies.Length; i++)
            {
                var rb = rigidbodies[i];
                if (rb == null) continue;
                rb.detectCollisions = false;
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            var cameras = _root.GetComponentsInChildren<Camera>(true);
            for (var i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null)
                {
                    cameras[i].enabled = false;
                }
            }

            var listeners = _root.GetComponentsInChildren<AudioListener>(true);
            for (var i = 0; i < listeners.Length; i++)
            {
                if (listeners[i] != null)
                {
                    listeners[i].enabled = false;
                }
            }

            var audioSources = _root.GetComponentsInChildren<AudioSource>(true);
            for (var i = 0; i < audioSources.Length; i++)
            {
                if (audioSources[i] != null)
                {
                    audioSources[i].enabled = false;
                }
            }

            DisableColliders();
        }

        private void LogAvatarDiagnostics(SourceDescriptor source, bool usedFallbackBody, ManualLogSource logger, Action<string> diagnosticsSink)
        {
            if (_root == null) return;

            var renderers = _root.GetComponentsInChildren<Renderer>(true);
            var animators = _root.GetComponentsInChildren<Animator>(true);
            var colliders = _root.GetComponentsInChildren<Collider>(true);
            var enabledColliders = 0;
            for (var i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null && colliders[i].enabled)
                {
                    enabledColliders++;
                }
            }

            var message = "Remote player avatar diagnostics: source=" + (source != null ? source.SourceKind : "-") +
                " name=" + (source != null ? source.SourceName : "-") +
                " fallbackBody=" + usedFallbackBody +
                " fallbackReason=" + (source != null ? source.FallbackReason : string.Empty) +
                " renderers=" + renderers.Length +
                " animators=" + animators.Length +
                " enabledColliders=" + enabledColliders +
                " bounds=" + FormatBounds(_root);
            LogInfo(logger, diagnosticsSink, message);
        }

        private static int CountRenderers(GameObject target)
        {
            return target != null ? target.GetComponentsInChildren<Renderer>(true).Length : 0;
        }

        private static int CountVisibleRenderers(GameObject target)
        {
            if (target == null) return 0;
            var renderers = target.GetComponentsInChildren<Renderer>(true);
            var count = 0;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }

        private static string FormatBounds(GameObject target)
        {
            if (target == null) return "none";
            var renderers = target.GetComponentsInChildren<Renderer>(true);
            var hasBounds = false;
            var bounds = new Bounds(target.transform.position, Vector3.zero);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null) continue;
                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds
                ? "center=" + FormatVector(bounds.center) + " size=" + FormatVector(bounds.size)
                : "none";
        }

        private static string FormatVector(Vector3 value)
        {
            return value.x.ToString("0.00") + "," +
                   value.y.ToString("0.00") + "," +
                   value.z.ToString("0.00");
        }

        private static string GetTransformPath(Transform transform)
        {
            if (transform == null) return string.Empty;
            var parts = new List<string>();
            var current = transform;
            while (current != null)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }

        private static void LogInfo(ManualLogSource logger, Action<string> diagnosticsSink, string message)
        {
            if (logger != null)
            {
                logger.LogInfo(message);
            }

            diagnosticsSink?.Invoke(message);
        }

        private static void LogWarning(ManualLogSource logger, Action<string> diagnosticsSink, string message)
        {
            if (logger != null)
            {
                logger.LogWarning(message);
            }

            diagnosticsSink?.Invoke(message);
        }

        private void DisableColliders()
        {
            var colliders = _root.GetComponentsInChildren<Collider>(true);
            foreach (var collider in colliders)
            {
                if (collider == null) continue;
                collider.enabled = false;
            }
        }
    }
}
