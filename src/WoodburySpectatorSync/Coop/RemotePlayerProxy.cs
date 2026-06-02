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
            public bool UseInvisiblePlaceholder;
            public bool AllowSyntheticFallbackBody;
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

        private sealed class RemotePlayerNameTag : MonoBehaviour
        {
            private const float MaxVisibleDistance = 34f;
            private const float VerticalPadding = 0.24f;
            private static GUIStyle _nameStyle;
            private static GUIStyle _roleStyle;

            private string _displayName = string.Empty;
            private string _role = string.Empty;
            private Color _accentColor = Color.white;

            public void Configure(string displayName, string role, Color accentColor)
            {
                _displayName = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim();
                _role = string.IsNullOrWhiteSpace(role) ? string.Empty : role.Trim().ToUpperInvariant();
                if (!string.IsNullOrEmpty(_displayName) &&
                    string.Equals(_displayName, _role, StringComparison.OrdinalIgnoreCase))
                {
                    _role = string.Empty;
                }
                _accentColor = accentColor;
                enabled = !string.IsNullOrEmpty(_displayName) || !string.IsNullOrEmpty(_role);
            }

            private void OnGUI()
            {
                if (string.IsNullOrEmpty(_displayName) && string.IsNullOrEmpty(_role)) return;
                if (!ShouldDrawInScene()) return;
                if (CountVisibleRenderers(gameObject) <= 0) return;

                var cam = Camera.main;
                if (cam == null) return;

                var anchor = transform.position + Vector3.up * 1.9f;
                if (TryGetRendererBounds(gameObject, out var bounds))
                {
                    anchor = new Vector3(bounds.center.x, bounds.max.y + VerticalPadding, bounds.center.z);
                }

                var viewport = cam.WorldToViewportPoint(anchor);
                if (viewport.z <= 0f || viewport.x < -0.05f || viewport.x > 1.05f || viewport.y < -0.05f || viewport.y > 1.05f)
                {
                    return;
                }

                var distance = Vector3.Distance(cam.transform.position, anchor);
                if (distance > MaxVisibleDistance) return;

                EnsureStyles();

                var displayName = string.IsNullOrEmpty(_displayName) ? _role : _displayName;
                var hasRole = !string.IsNullOrEmpty(_role);
                var role = hasRole ? _role : string.Empty;
                var nameWidth = _nameStyle.CalcSize(new GUIContent(displayName)).x;
                var roleWidth = hasRole ? _roleStyle.CalcSize(new GUIContent(role)).x : 0f;
                var width = Mathf.Clamp(Mathf.Max(nameWidth, roleWidth) + 34f, 92f, 190f);
                var height = hasRole ? 34f : 24f;
                var rect = new Rect(
                    (viewport.x * Screen.width) - (width * 0.5f),
                    ((1f - viewport.y) * Screen.height) - height - 4f,
                    width,
                    height);

                var oldColor = GUI.color;
                GUI.color = new Color(0.02f, 0.018f, 0.015f, 0.74f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = _accentColor;
                GUI.DrawTexture(new Rect(rect.x, rect.y, 3f, rect.height), Texture2D.whiteTexture);

                _nameStyle.normal.textColor = new Color(0.95f, 0.94f, 0.9f, 1f);
                _roleStyle.normal.textColor = _accentColor;
                GUI.Label(new Rect(rect.x + 10f, rect.y + (hasRole ? 3f : 4f), rect.width - 16f, 17f), displayName, _nameStyle);
                if (hasRole)
                {
                    GUI.Label(new Rect(rect.x + 10f, rect.y + 17f, rect.width - 16f, 13f), role, _roleStyle);
                }
                GUI.color = oldColor;
            }

            private static void EnsureStyles()
            {
                if (_nameStyle == null)
                {
                    _nameStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 12,
                        fontStyle = FontStyle.Bold,
                        clipping = TextClipping.Clip
                    };
                }

                if (_roleStyle == null)
                {
                    _roleStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 9,
                        fontStyle = FontStyle.Bold,
                        clipping = TextClipping.Clip
                    };
                }
            }

            private static bool ShouldDrawInScene()
            {
                var sceneName = SceneManager.GetActiveScene().name;
                if (string.IsNullOrEmpty(sceneName)) return false;
                if (sceneName.Equals("MainMenu", StringComparison.OrdinalIgnoreCase)) return false;
                if (sceneName.Equals("Disclaimer", StringComparison.OrdinalIgnoreCase)) return false;
                return sceneName.IndexOf("Menu", StringComparison.OrdinalIgnoreCase) < 0;
            }
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

        private const float CameraFallbackEyeHeight = 1.65f;
        private const float SceneModelFallbackGroundYOffset = -0.06f;
        private const float BodyGroundProjectionMaxOffset = 0.35f;

        private readonly GameObject _root;
        private readonly CharacterController _characterController;
        private readonly bool _allowCharacterController;
        private readonly Transform _cameraTransform;
        private readonly Animator _animator;
        private readonly HashSet<int> _animFloatParams = new HashSet<int>();
        private readonly HashSet<int> _animBoolParams = new HashSet<int>();
        private readonly AnimatorRigMap _animRig;
        private readonly bool _animatorHasMotionControlParams;
        private Vector3 _lastRootPosition;
        private float _lastSampleTime;
        private bool _hasMotionSample;
        private Vector3 _smoothedRootPosition;
        private Quaternion _smoothedRootRotation = Quaternion.identity;
        private float _lastSmoothSampleTime;
        private bool _hasSmoothedRoot;
        private RemotePlayerNameTag _nameTag;
        private string _idleAnimationStateName;
        private bool _lastAnimatorMoving;
        private float _lastIdlePoseTime;
        private int _hiddenStoryCarryPropCount;
        private int _hiddenUtilityVisualCount;

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
            if (source == null || (source.SourceObject == null && !source.UseSyntheticCapsule && !source.UseInvisiblePlaceholder))
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
            if (source == null || (source.SourceObject == null && !source.UseSyntheticCapsule && !source.UseInvisiblePlaceholder))
            {
                throw new ArgumentNullException(nameof(source));
            }

            _allowCharacterController = allowCharacterController;
            if (source.UseSyntheticCapsule || source.UseInvisiblePlaceholder)
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
                model.SetActive(true);
                model.transform.SetParent(_root.transform, false);
                model.transform.localPosition = new Vector3(0f, source.AvatarYOffset, 0f);
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one * Mathf.Max(0.01f, source.AvatarScale);
                AlignWrappedAvatarToGround(model, source.AvatarYOffset, logger, diagnosticsSink);
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

            var usedFallbackBody = source.AllowSyntheticFallbackBody && EnsureVisibleBody(tint, logger, diagnosticsSink);
            EnsureAnimatorComponent(logger, diagnosticsSink, source.AllowSyntheticFallbackBody || CountRenderers(_root) > 0);

            _animator = _root.GetComponentInChildren<Animator>(true);
            if (_animator != null && _animator.runtimeAnimatorController != null)
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
            _animatorHasMotionControlParams = HasMotionControlParams();
            _idleAnimationStateName = ResolveIdleAnimationStateName(_animator);
            InitializeAnimatorPlayback();

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

        public void SetNameTag(string displayName, string role, Color color)
        {
            if (_root == null) return;

            if (_nameTag == null)
            {
                _nameTag = _root.GetComponent<RemotePlayerNameTag>();
                if (_nameTag == null)
                {
                    _nameTag = _root.AddComponent<RemotePlayerNameTag>();
                }
            }

            _nameTag.Configure(displayName, role, color);
        }

        public void ApplyTransform(PlayerTransformState state)
        {
            if (_root == null) return;

            if (_characterController != null && _allowCharacterController)
            {
                _characterController.enabled = false;
            }

            var bodyPosition = ResolveBodyRootPosition(state);
            var bodyRotation = ResolveUprightBodyRotation(state);
            SmoothBodyTransform(ref bodyPosition, ref bodyRotation);
            _root.transform.SetPositionAndRotation(bodyPosition, bodyRotation);
            DriveAnimatorFromTransform(bodyPosition, bodyRotation);

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

        private void SmoothBodyTransform(ref Vector3 bodyPosition, ref Quaternion bodyRotation)
        {
            var now = Time.realtimeSinceStartup;
            if (!_hasSmoothedRoot || Vector3.Distance(_smoothedRootPosition, bodyPosition) > 4f)
            {
                _smoothedRootPosition = bodyPosition;
                _smoothedRootRotation = bodyRotation;
                _lastSmoothSampleTime = now;
                _hasSmoothedRoot = true;
                return;
            }

            var deltaTime = Mathf.Max(0.001f, now - _lastSmoothSampleTime);
            var alpha = 1f - Mathf.Exp(-20f * deltaTime);
            _smoothedRootPosition = Vector3.Lerp(_smoothedRootPosition, bodyPosition, alpha);
            _smoothedRootRotation = Quaternion.Slerp(_smoothedRootRotation, bodyRotation, alpha);
            _lastSmoothSampleTime = now;
            bodyPosition = _smoothedRootPosition;
            bodyRotation = _smoothedRootRotation;
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
            DriveAnimatorPlaybackSpeed(planarSpeed, moving);

            _lastRootPosition = newPosition;
            _lastSampleTime = now;
        }

        public void ApplyInput(PlayerInputState input)
        {
            if (_animator == null) return;

            var planarSpeed = new Vector2(input.MoveX, input.MoveY).magnitude;
            var moving = Mathf.Abs(input.MoveX) > 0.01f || Mathf.Abs(input.MoveY) > 0.01f;
            SetFloatFromRig(_animRig.StrafeFloatNames, input.MoveX);
            SetFloatFromRig(_animRig.ForwardFloatNames, input.MoveY);
            SetFloatFromRig(_animRig.SpeedFloatNames, planarSpeed);

            SetBoolFromRig(_animRig.CrouchBoolNames, input.Crouch);
            SetBoolFromRig(_animRig.SprintBoolNames, input.Sprint);
            SetBoolFromRig(_animRig.JumpBoolNames, input.Jump);
            SetBoolFromRig(_animRig.MovingBoolNames, moving);
            DriveAnimatorPlaybackSpeed(planarSpeed, moving);
        }

        private void InitializeAnimatorPlayback()
        {
            if (_animator == null) return;

            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (_animatorHasMotionControlParams)
            {
                _animator.speed = 1f;
                return;
            }

            _animator.Rebind();
            _animator.Update(0f);
            ForceIdlePoseIfAvailable();
            _animator.speed = 0f;
        }

        private void DriveAnimatorPlaybackSpeed(float planarSpeed, bool moving)
        {
            if (_animator == null || _animatorHasMotionControlParams) return;

            if (!moving || planarSpeed < 0.05f)
            {
                ForceIdlePoseIfAvailable();
                _animator.speed = 0f;
                _lastAnimatorMoving = false;
                return;
            }

            _lastAnimatorMoving = true;
            _animator.speed = Mathf.Clamp(planarSpeed / 1.25f, 0.35f, 1.35f);
        }

        private void ForceIdlePoseIfAvailable()
        {
            if (_animator == null || string.IsNullOrEmpty(_idleAnimationStateName)) return;

            var now = Time.realtimeSinceStartup;
            if (!_lastAnimatorMoving && now - _lastIdlePoseTime < 0.75f)
            {
                return;
            }

            _animator.Play(_idleAnimationStateName, 0, 0f);
            _animator.Update(0f);
            _lastIdlePoseTime = now;
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

        private bool HasBoolParam(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return _animBoolParams.Contains(Animator.StringToHash(name));
        }

        private bool HasMotionControlParams()
        {
            if (_animator == null || _animRig == null)
            {
                return false;
            }

            return HasAnyFloatParam(_animRig.StrafeFloatNames) ||
                   HasAnyFloatParam(_animRig.ForwardFloatNames) ||
                   HasAnyFloatParam(_animRig.SpeedFloatNames) ||
                   HasAnyBoolParam(_animRig.MovingBoolNames) ||
                   HasAnyBoolParam(_animRig.SprintBoolNames) ||
                   HasAnyBoolParam(_animRig.CrouchBoolNames) ||
                   HasAnyBoolParam(_animRig.JumpBoolNames);
        }

        private bool HasAnyFloatParam(string[] names)
        {
            if (names == null) return false;
            for (var i = 0; i < names.Length; i++)
            {
                if (!string.IsNullOrEmpty(names[i]) && HasFloatParam(names[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasAnyBoolParam(string[] names)
        {
            if (names == null) return false;
            for (var i = 0; i < names.Length; i++)
            {
                if (!string.IsNullOrEmpty(names[i]) && HasBoolParam(names[i]))
                {
                    return true;
                }
            }

            return false;
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

            var configuredAvatarId = GetConfiguredAvatarId(settings);
            var prefersSceneModel = avatarSource == RemotePlayerAvatarSource.Auto &&
                                    IsSceneModelAvatarId(configuredAvatarId);
            if (prefersSceneModel)
            {
                if (TryResolveGameModelAvatar(settings, fallbackSource, logger, diagnosticsSink, out var preferredSceneModel))
                {
                    preferredSceneModel.FallbackReason = "configured scene avatar";
                    return preferredSceneModel;
                }

                LogWarning(logger, diagnosticsSink, "Remote player avatar fallback: configured scene avatar not found id=" +
                    configuredAvatarId + " scene=" + SceneManager.GetActiveScene().name);
                return CreateHiddenSource(settings, fallbackSource, "configured scene avatar unavailable");
            }

            if (avatarSource == RemotePlayerAvatarSource.Auto ||
                avatarSource == RemotePlayerAvatarSource.AssetBundle)
            {
                if (AvatarAssetProvider.TryResolve(settings, logger, diagnosticsSink, out var avatarSelection))
                {
                    if (!IsUsableBundleAvatar(avatarSelection.Prefab, out var rejectReason))
                    {
                        LogWarning(logger, diagnosticsSink,
                            "Remote player avatar fallback: AssetBundle avatar rejected id=" +
                            avatarSelection.AvatarId + " reason=" + rejectReason);
                    }
                    else
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

                if (TryResolveSceneGameModelFallback(settings, fallbackSource, logger, diagnosticsSink, out var sceneHuman))
                {
                    if (avatarSource == RemotePlayerAvatarSource.AssetBundle)
                    {
                        sceneHuman.FallbackReason = "AssetBundle avatar unavailable";
                    }
                    return sceneHuman;
                }

                if (avatarSource == RemotePlayerAvatarSource.AssetBundle)
                {
                    return CreateHiddenSource(settings, fallbackSource, "AssetBundle avatar unavailable");
                }
            }

            if (avatarSource == RemotePlayerAvatarSource.GameModel)
            {
                if (TryResolveGameModelAvatar(settings, fallbackSource, logger, diagnosticsSink, out var gameModel))
                {
                    return gameModel;
                }

                if (avatarSource == RemotePlayerAvatarSource.GameModel)
                {
                    return CreateHiddenSource(settings, fallbackSource, "no safe in-scene game model found");
                }
            }

            return CreateHiddenSource(settings, fallbackSource, "AssetBundle avatar unavailable");
        }

        private static bool IsSceneModelAvatarId(string avatarId)
        {
            if (string.IsNullOrWhiteSpace(avatarId))
            {
                return true;
            }

            var normalized = avatarId.Trim();
            return string.Equals(normalized, "woodbury_scene_auto", StringComparison.OrdinalIgnoreCase) ||
                   normalized.StartsWith("woodbury_", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsUsableBundleAvatar(GameObject prefab, out string reason)
        {
            reason = string.Empty;
            if (prefab == null)
            {
                reason = "prefab is null";
                return false;
            }

            if (CountRenderers(prefab) <= 0)
            {
                reason = "no renderers";
                return false;
            }

            if (!HasUsableAnimator(prefab, out var animatorReason))
            {
                reason = animatorReason;
                return false;
            }

            return true;
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
            return string.IsNullOrWhiteSpace(avatarId) ? AvatarAssetProvider.DefaultAvatarId : avatarId.Trim();
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
                UseAvatarWrapper = true,
                AvatarYOffset = ResolveSceneModelFallbackYOffset(settings),
                SourceKind = "GameModel",
                SourceName = sourceName
            };

            LogInfo(logger, diagnosticsSink, "Remote player avatar source: GameModel id=" + avatarId +
                " object=" + GetTransformPath(candidate.transform) +
                " renderers=" + CountRenderers(candidate) +
                " bounds=" + FormatBounds(candidate));
            return true;
        }

        private static bool TryResolveSceneGameModelFallback(
            Settings settings,
            FirstPersonController fallbackSource,
            ManualLogSource logger,
            Action<string> diagnosticsSink,
            out SourceDescriptor source)
        {
            source = null;
            GameObject candidate;
            string sourceName;

            if (!TryFindNonMikeSceneHuman(out candidate, out sourceName) &&
                !TryFindGameModelAvatar("woodbury_scene_auto", out candidate, out sourceName))
            {
                LogWarning(logger, diagnosticsSink, "Remote player avatar fallback: no safe in-scene human or AI model found scene=" +
                    SceneManager.GetActiveScene().name);
                return false;
            }

            source = new SourceDescriptor
            {
                SourceObject = candidate,
                InitialPosition = fallbackSource != null ? fallbackSource.transform.position : candidate.transform.position,
                InitialRotation = fallbackSource != null ? fallbackSource.transform.rotation : candidate.transform.rotation,
                RigProfile = ResolveConfiguredRig(settings, "Auto"),
                UseAvatarWrapper = true,
                AvatarYOffset = ResolveSceneModelFallbackYOffset(settings),
                SourceKind = "GameModelFallback",
                SourceName = sourceName
            };

            LogInfo(logger, diagnosticsSink, "Remote player avatar source: GameModelFallback object=" +
                GetTransformPath(candidate.transform) +
                " renderers=" + CountRenderers(candidate) +
                " bounds=" + FormatBounds(candidate) +
                " cloned=scripts-disabled colliders-disabled");
            return true;
        }

        private static float ResolveSceneModelFallbackYOffset(Settings settings)
        {
            var configuredOffset = settings != null && settings.CoopRemotePlayerAvatarYOffset != null
                ? settings.CoopRemotePlayerAvatarYOffset.Value
                : 0f;
            return SceneModelFallbackGroundYOffset + configuredOffset;
        }

        private static bool TryFindGameModelAvatar(string avatarId, out GameObject candidate, out string sourceName)
        {
            candidate = null;
            sourceName = string.Empty;

            var normalizedId = string.IsNullOrWhiteSpace(avatarId) ? "woodbury_scene_auto" : avatarId.Trim().ToLowerInvariant();
            switch (normalizedId)
            {
                case "woodbury_scene_auto":
                    return TryFindSceneAutoAvatar(out candidate, out sourceName);
                case "woodbury_cabin_hiker":
                    return TryFindCabinNonMikeAvatar(out candidate, out sourceName);
                case "woodbury_cabin_host":
                    return TryFindCabinMikeAvatar(out candidate, out sourceName);
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
                return TryFindNamedAvatar(new[] { "BackpackerV2", "Backpacker" }, out candidate, out sourceName) ||
                       TryFindComponentAvatar<PizzeriaHiker>(out candidate, out sourceName) ||
                       TryFindComponentAvatar<Hobo>(out candidate, out sourceName) ||
                       TryFindComponentAvatar<MikePizzeria>(out candidate, out sourceName) ||
                       TryFindNamedAvatar(new[] { "Mike New" }, out candidate, out sourceName);
            }

            if (sceneName.IndexOf("RoadTrip", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return TryFindComponentAvatar<MikeInCar>(out candidate, out sourceName) ||
                       TryFindNamedAvatar(new[] { "MikeInCar", "Mike In Car" }, out candidate, out sourceName);
            }

            if (sceneName.IndexOf("Cabin", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return TryFindCabinNonMikeAvatar(out candidate, out sourceName) ||
                       TryFindCabinMikeAvatar(out candidate, out sourceName);
            }

            return TryFindNonMikeSceneHuman(out candidate, out sourceName) ||
                   TryFindCabinMikeAvatar(out candidate, out sourceName) ||
                   TryFindComponentAvatar<MikePizzeria>(out candidate, out sourceName) ||
                   TryFindComponentAvatar<MikeInCar>(out candidate, out sourceName);
        }

        private static bool TryFindSceneAutoAvatar(out GameObject candidate, out string sourceName)
        {
            candidate = null;
            sourceName = string.Empty;

            var sceneName = SceneManager.GetActiveScene().name ?? string.Empty;
            if (sceneName.IndexOf("Cabin", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return TryFindCabinNonMikeAvatar(out candidate, out sourceName) ||
                       TryFindCabinMikeAvatar(out candidate, out sourceName);
            }

            if (sceneName.IndexOf("Pizzeria", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return TryFindNamedAvatar(new[] { "BackpackerV2", "Backpacker" }, out candidate, out sourceName) ||
                       TryFindComponentAvatar<PizzeriaHiker>(out candidate, out sourceName) ||
                       TryFindComponentAvatar<Hobo>(out candidate, out sourceName) ||
                       TryFindComponentAvatar<MikePizzeria>(out candidate, out sourceName) ||
                       TryFindNamedAvatar(new[] { "Mike New" }, out candidate, out sourceName);
            }

            return TryFindNonMikeSceneHuman(out candidate, out sourceName) ||
                   TryFindCabinMikeAvatar(out candidate, out sourceName) ||
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

        private static bool TryFindCabinNonMikeAvatar(out GameObject candidate, out string sourceName)
        {
            return TryFindComponentAvatar<CabinHiker>(out candidate, out sourceName) ||
                   TryFindNamedAvatar(new[] { "BackpackerV2", "Backpacker", "Hiker", "Nora" }, out candidate, out sourceName);
        }

        private static bool TryFindNonMikeSceneHuman(out GameObject candidate, out string sourceName)
        {
            if (TryFindNamedAvatar(new[] { "BackpackerV2", "Backpacker", "Hiker", "Nora" }, out candidate, out sourceName) &&
                !IsMikePath(GetTransformPath(candidate.transform)))
            {
                return true;
            }

            candidate = null;
            sourceName = string.Empty;
            return false;
        }

        private static bool IsMikePath(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   path.IndexOf("Mike", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool TryFindComponentAvatar<T>(out GameObject candidate, out string sourceName)
            where T : Component
        {
            candidate = null;
            sourceName = string.Empty;

            var components = Resources.FindObjectsOfTypeAll<T>();
            var bestScore = int.MinValue;
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component == null) continue;
                if (!IsSafeAvatarCandidate(component.gameObject, true)) continue;

                var score = ScoreAvatarCandidate(component.gameObject);
                if (score <= bestScore) continue;

                bestScore = score;
                candidate = component.gameObject;
                sourceName = typeof(T).Name + ":" + GetTransformPath(component.transform);
            }

            return candidate != null;
        }

        private static bool TryFindNamedAvatar(string[] names, out GameObject candidate, out string sourceName)
        {
            candidate = null;
            sourceName = string.Empty;
            if (names == null || names.Length == 0) return false;

            var transforms = Resources.FindObjectsOfTypeAll<Transform>();
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

                    if (!IsSafeAvatarCandidate(transform.gameObject, false)) continue;

                    var score = ScoreAvatarCandidate(transform.gameObject);
                    if (candidate != null && score <= ScoreAvatarCandidate(candidate)) continue;

                    candidate = transform.gameObject;
                    sourceName = "Name:" + GetTransformPath(transform);
                }
            }

            return candidate != null;
        }

        private static bool IsSafeAvatarCandidate(GameObject candidate, bool trustedActor)
        {
            if (candidate == null) return false;

            var scene = candidate.scene;
            var activeScene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene != activeScene) return false;

            var path = GetTransformPath(candidate.transform);
            if (IsRejectedSceneAvatarPath(path))
            {
                return false;
            }

            if (path.IndexOf("CoopRemotePlayer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("CoopHostAvatar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("CoopClientAvatar", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            if (path.IndexOf("Trigger", StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("Collider", StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("LookAt", StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("Camera", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            if (candidate.GetComponentInParent<FirstPersonController>() != null ||
                candidate.GetComponentInParent<PlayerController>() != null)
            {
                return false;
            }

            if (CountRenderers(candidate) <= 0)
            {
                return false;
            }

            if (HasOnlyUtilityAvatarRenderers(candidate))
            {
                return false;
            }

            if (!trustedActor && !HasUsableAnimator(candidate, out _))
            {
                return false;
            }

            if (!TryGetRendererBounds(candidate, out var bounds))
            {
                return false;
            }

            var size = bounds.size;
            var horizontalMax = Mathf.Max(size.x, size.z);
            var maxHeight = trustedActor ? 3.2f : 2.5f;
            var maxHorizontal = trustedActor ? 3.25f : 2.2f;
            var maxDepth = trustedActor ? 3.25f : 1.6f;
            return size.y >= 1.1f &&
                   size.y <= maxHeight &&
                   horizontalMax >= 0.2f &&
                   horizontalMax <= maxHorizontal &&
                   size.z <= maxDepth;
        }

        private static int ScoreAvatarCandidate(GameObject candidate)
        {
            if (candidate == null) return int.MinValue;

            var score = 0;
            if (candidate.activeInHierarchy) score += 10000;
            if (candidate.activeSelf) score += 1000;
            score += CountVisibleRenderers(candidate) * 20;
            score += CountRenderers(candidate);
            if (HasUsableAnimator(candidate, out _)) score += 50;

            var path = GetTransformPath(candidate.transform);
            if (!string.IsNullOrEmpty(path) &&
                path.IndexOf("Post Eating", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score -= 100;
            }

            return score;
        }

        private static bool IsRejectedSceneAvatarPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            var leaf = path;
            var slash = leaf.LastIndexOf('/');
            if (slash >= 0 && slash + 1 < leaf.Length)
            {
                leaf = leaf.Substring(slash + 1);
            }

            if (string.Equals(leaf, "Host", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return path.IndexOf("House/Host", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   path.IndexOf("HostDuring", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   path.IndexOf("HostFixing", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   path.IndexOf("HostEnd", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   path.IndexOf("Host Hiding", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static SourceDescriptor CreateCapsuleSource(Settings settings, FirstPersonController fallbackSource, string reason)
        {
            return new SourceDescriptor
            {
                UseSyntheticCapsule = true,
                AllowSyntheticFallbackBody = true,
                InitialPosition = fallbackSource != null ? fallbackSource.transform.position : Vector3.zero,
                InitialRotation = fallbackSource != null ? fallbackSource.transform.rotation : Quaternion.identity,
                RigProfile = ResolveConfiguredRig(settings, "Auto"),
                SourceKind = "Capsule",
                SourceName = "procedural_humanoid",
                FallbackReason = reason ?? string.Empty
            };
        }

        private static SourceDescriptor CreateHiddenSource(Settings settings, FirstPersonController fallbackSource, string reason)
        {
            return new SourceDescriptor
            {
                UseInvisiblePlaceholder = true,
                AllowSyntheticFallbackBody = false,
                InitialPosition = fallbackSource != null ? fallbackSource.transform.position : Vector3.zero,
                InitialRotation = fallbackSource != null ? fallbackSource.transform.rotation : Quaternion.identity,
                RigProfile = ResolveConfiguredRig(settings, "Auto"),
                SourceKind = "Hidden",
                SourceName = "no_visible_avatar",
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

            var animator = _root.GetComponent<Animator>();
            if (animator == null)
            {
                animator = _root.AddComponent<Animator>();
            }
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            CreatePrimitivePart(PrimitiveType.Capsule, "Torso", new Vector3(0f, 0.98f, 0f), new Vector3(0.28f, 0.34f, 0.18f), Quaternion.identity, tint);
            CreatePrimitivePart(PrimitiveType.Sphere, "Head", new Vector3(0f, 1.58f, 0f), new Vector3(0.24f, 0.24f, 0.24f), Quaternion.identity, new Color(0.78f, 0.58f, 0.45f, 1f));
            CreatePrimitivePart(PrimitiveType.Capsule, "LeftArm", new Vector3(-0.34f, 0.9f, 0f), new Vector3(0.08f, 0.32f, 0.08f), Quaternion.identity, new Color(0.78f, 0.58f, 0.45f, 1f));
            CreatePrimitivePart(PrimitiveType.Capsule, "RightArm", new Vector3(0.34f, 0.9f, 0f), new Vector3(0.08f, 0.32f, 0.08f), Quaternion.identity, new Color(0.78f, 0.58f, 0.45f, 1f));
            CreatePrimitivePart(PrimitiveType.Capsule, "LeftLeg", new Vector3(-0.12f, 0.34f, 0f), new Vector3(0.09f, 0.34f, 0.09f), Quaternion.identity, new Color(0.16f, 0.18f, 0.22f, 1f));
            CreatePrimitivePart(PrimitiveType.Capsule, "RightLeg", new Vector3(0.12f, 0.34f, 0f), new Vector3(0.09f, 0.34f, 0.09f), Quaternion.identity, new Color(0.16f, 0.18f, 0.22f, 1f));

            DisableColliders();
            LogWarning(logger, diagnosticsSink, "Remote player avatar fallback body created: procedural humanoid height=1.7 animators=1 colliders=disabled");
            return true;
        }

        private void EnsureAnimatorComponent(ManualLogSource logger, Action<string> diagnosticsSink, bool logIfAdded)
        {
            if (_root == null) return;

            var animator = _root.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = _root.AddComponent<Animator>();
                if (logIfAdded)
                {
                    LogWarning(logger, diagnosticsSink, "Remote player avatar Animator added at runtime for render-only prefab.");
                }
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        private GameObject CreatePrimitivePart(
            PrimitiveType type,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Color color)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(_root.transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = localScale;

            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                UnityEngine.Object.Destroy(collider);
            }

            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            return part;
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

            HideStoryCarryProps();
            HideUtilityAvatarObjects();
            DisableColliders();
        }

        private void HideStoryCarryProps()
        {
            if (_root == null) return;

            var transforms = _root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var child = transforms[i];
                if (child == null || child == _root.transform) continue;
                if (!LooksLikeStoryCarryProp(child.name, GetTransformPath(child))) continue;

                _hiddenStoryCarryPropCount++;
                child.gameObject.SetActive(false);
            }
        }

        private void HideUtilityAvatarObjects()
        {
            if (_root == null) return;

            var transforms = _root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var child = transforms[i];
                if (child == null || child == _root.transform) continue;
                if (!LooksLikeUtilityAvatarObject(child)) continue;

                DisableRenderers(child.gameObject);
                child.gameObject.SetActive(false);
                _hiddenUtilityVisualCount++;
            }
        }

        private static bool LooksLikeStoryCarryProp(string name, string path)
        {
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(path)) return false;

            var text = ((path ?? string.Empty) + "/" + (name ?? string.Empty)).ToLowerInvariant();
            if (text.IndexOf("casserole", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("fishplate", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("fish plate", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("dinnerplate", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("dinner plate", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("marinade", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("veggie", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("vegetable", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("tray", StringComparison.Ordinal) >= 0)
            {
                return true;
            }

            var attachedToHand = text.IndexOf("hand", StringComparison.Ordinal) >= 0 ||
                                 text.IndexOf("hold", StringComparison.Ordinal) >= 0 ||
                                 text.IndexOf("attach", StringComparison.Ordinal) >= 0 ||
                                 text.IndexOf("weapon", StringComparison.Ordinal) >= 0 ||
                                 text.IndexOf("item", StringComparison.Ordinal) >= 0;

            return attachedToHand &&
                   (text.IndexOf("plate", StringComparison.Ordinal) >= 0 ||
                    text.IndexOf("fish", StringComparison.Ordinal) >= 0 ||
                    text.IndexOf("food", StringComparison.Ordinal) >= 0 ||
                    text.IndexOf("dish", StringComparison.Ordinal) >= 0 ||
                    text.IndexOf("bowl", StringComparison.Ordinal) >= 0 ||
                    text.IndexOf("fork", StringComparison.Ordinal) >= 0 ||
                    text.IndexOf("knife", StringComparison.Ordinal) >= 0 ||
                    text.IndexOf("spoon", StringComparison.Ordinal) >= 0);
        }

        private static bool HasOnlyUtilityAvatarRenderers(GameObject candidate)
        {
            if (candidate == null) return false;

            var renderers = candidate.GetComponentsInChildren<Renderer>(true);
            var rendererCount = 0;
            var utilityCount = 0;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null) continue;
                rendererCount++;
                if (LooksLikeUtilityAvatarObject(renderer.transform))
                {
                    utilityCount++;
                }
            }

            return rendererCount > 0 && utilityCount >= rendererCount;
        }

        private static bool LooksLikeUtilityAvatarObject(Transform transform)
        {
            if (transform == null || transform.gameObject == null) return false;

            var name = transform.name ?? string.Empty;
            var path = GetTransformPath(transform);
            var text = ((path ?? string.Empty) + "/" + name).ToLowerInvariant();

            if (text.IndexOf("antiplayerpush", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("anti player push", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("anti_player_push", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("pushcapsule", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("push capsule", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("hitbox", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("hurtbox", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("collider", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("collision", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("trigger", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("bounds", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("blocking", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("occluder", StringComparison.Ordinal) >= 0)
            {
                return HasRendererOrCollider(transform.gameObject);
            }

            var renderer = transform.GetComponent<Renderer>();
            if (renderer == null) return false;
            if (transform.GetComponentInChildren<SkinnedMeshRenderer>(true) != null) return false;

            var hasCollider = transform.GetComponent<Collider>() != null ||
                              transform.GetComponentInChildren<Collider>(true) != null;
            if (!hasCollider) return false;

            var leaf = name.Trim().ToLowerInvariant();
            return leaf == "capsule" ||
                   leaf == "body capsule" ||
                   leaf == "player capsule" ||
                   leaf.IndexOf("capsule collider", StringComparison.Ordinal) >= 0 ||
                   leaf.IndexOf("collision capsule", StringComparison.Ordinal) >= 0 ||
                   leaf.IndexOf("push capsule", StringComparison.Ordinal) >= 0;
        }

        private static bool HasRendererOrCollider(GameObject target)
        {
            if (target == null) return false;
            return target.GetComponent<Renderer>() != null ||
                   target.GetComponentInChildren<Renderer>(true) != null ||
                   target.GetComponent<Collider>() != null ||
                   target.GetComponentInChildren<Collider>(true) != null;
        }

        private static void DisableRenderers(GameObject target)
        {
            if (target == null) return;

            var renderers = target.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null) continue;
                renderer.enabled = false;
            }
        }

        private void LogAvatarDiagnostics(SourceDescriptor source, bool usedFallbackBody, ManualLogSource logger, Action<string> diagnosticsSink)
        {
            if (_root == null) return;

            var renderers = _root.GetComponentsInChildren<Renderer>(true);
            var visibleRenderers = CountVisibleRenderers(_root);
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
                " visibleRenderers=" + visibleRenderers +
                " animators=" + animators.Length +
                " enabledColliders=" + enabledColliders +
                " hiddenStoryProps=" + _hiddenStoryCarryPropCount +
                " hiddenUtilityVisuals=" + _hiddenUtilityVisualCount +
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

        private static bool TryGetRendererBounds(GameObject target, out Bounds bounds)
        {
            bounds = new Bounds(target != null ? target.transform.position : Vector3.zero, Vector3.zero);
            if (target == null) return false;

            var renderers = target.GetComponentsInChildren<Renderer>(true);
            var hasBounds = false;
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

            return hasBounds;
        }

        private static bool HasUsableAnimator(GameObject target, out string reason)
        {
            reason = "no Animator";
            if (target == null) return false;

            var animators = target.GetComponentsInChildren<Animator>(true);
            if (animators == null || animators.Length == 0)
            {
                return false;
            }

            for (var i = 0; i < animators.Length; i++)
            {
                var animator = animators[i];
                if (animator == null) continue;
                if (animator.runtimeAnimatorController != null)
                {
                    reason = string.Empty;
                    return true;
                }
            }

            reason = "Animator has no controller";
            return false;
        }

        private static string ResolveIdleAnimationStateName(Animator animator)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return string.Empty;
            }

            var clips = animator.runtimeAnimatorController.animationClips;
            if (clips == null || clips.Length == 0)
            {
                return string.Empty;
            }

            var fallback = string.Empty;
            for (var i = 0; i < clips.Length; i++)
            {
                var clip = clips[i];
                if (clip == null || string.IsNullOrWhiteSpace(clip.name)) continue;

                var name = clip.name.Trim();
                if (fallback.Length == 0)
                {
                    fallback = name;
                }

                if (name.IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("stand", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("breath", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return name;
                }
            }

            return fallback;
        }

        private static Vector3 ResolveBodyRootPosition(PlayerTransformState state)
        {
            var position = state.Position;
            var cameraPosition = state.CameraPosition;
            var cameraHasPosition = cameraPosition != Vector3.zero;

            if (TryProjectToWalkableGround(position, out var groundedFromBody))
            {
                var verticalOffset = Mathf.Abs(position.y - groundedFromBody.y);
                if (verticalOffset <= BodyGroundProjectionMaxOffset)
                {
                    return groundedFromBody;
                }
            }

            if (cameraHasPosition)
            {
                var cameraBodyEstimate = new Vector3(position.x, cameraPosition.y - CameraFallbackEyeHeight, position.z);
                if (TryProjectToWalkableGround(cameraBodyEstimate, out var groundedFromCamera))
                {
                    return groundedFromCamera;
                }
            }

            if (cameraPosition == Vector3.zero)
            {
                return position;
            }

            var horizontalDelta = new Vector2(position.x - cameraPosition.x, position.z - cameraPosition.z).magnitude;
            var verticalDelta = Mathf.Abs(position.y - cameraPosition.y);
            if (horizontalDelta > 0.45f || verticalDelta > 0.45f)
            {
                return position;
            }

            var origin = new Vector3(position.x, cameraPosition.y + 0.1f, position.z);
            RaycastHit hit;
            if (Physics.Raycast(origin, Vector3.down, out hit, 3.0f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                var eyeHeight = cameraPosition.y - hit.point.y;
                if (eyeHeight >= 1.15f && eyeHeight <= 2.25f)
                {
                    return new Vector3(position.x, hit.point.y, position.z);
                }
            }

            return new Vector3(position.x, cameraPosition.y - CameraFallbackEyeHeight, position.z);
        }

        private static Quaternion ResolveUprightBodyRotation(PlayerTransformState state)
        {
            var forward = state.Rotation * Vector3.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.001f)
            {
                forward = state.CameraRotation * Vector3.forward;
                forward.y = 0f;
            }

            return forward.sqrMagnitude >= 0.001f
                ? Quaternion.LookRotation(forward.normalized, Vector3.up)
                : Quaternion.identity;
        }

        private static bool TryProjectToWalkableGround(Vector3 position, out Vector3 grounded)
        {
            grounded = position;

            NavMeshHit navHit;
            if (NavMesh.SamplePosition(position, out navHit, 3.0f, NavMesh.AllAreas))
            {
                grounded = new Vector3(position.x, navHit.position.y, position.z);
                return true;
            }

            RaycastHit hit;
            var rayOrigin = position + Vector3.up * 1.25f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 4.0f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                grounded = new Vector3(position.x, hit.point.y, position.z);
                return true;
            }

            return false;
        }

        private static void AlignWrappedAvatarToGround(GameObject model, float yOffset, ManualLogSource logger, Action<string> diagnosticsSink)
        {
            if (model == null || model.transform.parent == null) return;

            var parent = model.transform.parent;
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            var minLocalY = float.PositiveInfinity;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null) continue;
                var bounds = renderer.bounds;
                UpdateMinLocalY(parent, bounds, ref minLocalY);
            }

            if (float.IsPositiveInfinity(minLocalY)) return;

            var adjustment = yOffset - minLocalY;
            if (Mathf.Abs(adjustment) > 0.001f)
            {
                model.transform.localPosition += new Vector3(0f, adjustment, 0f);
            }

            LogInfo(logger, diagnosticsSink, "Remote player avatar grounded: bottom=" +
                minLocalY.ToString("0.###") + " target=" + yOffset.ToString("0.###") +
                " adjust=" + adjustment.ToString("0.###"));
        }

        private static void UpdateMinLocalY(Transform parent, Bounds bounds, ref float minLocalY)
        {
            var min = bounds.min;
            var max = bounds.max;
            UpdateMinLocalY(parent, new Vector3(min.x, min.y, min.z), ref minLocalY);
            UpdateMinLocalY(parent, new Vector3(min.x, min.y, max.z), ref minLocalY);
            UpdateMinLocalY(parent, new Vector3(min.x, max.y, min.z), ref minLocalY);
            UpdateMinLocalY(parent, new Vector3(min.x, max.y, max.z), ref minLocalY);
            UpdateMinLocalY(parent, new Vector3(max.x, min.y, min.z), ref minLocalY);
            UpdateMinLocalY(parent, new Vector3(max.x, min.y, max.z), ref minLocalY);
            UpdateMinLocalY(parent, new Vector3(max.x, max.y, min.z), ref minLocalY);
            UpdateMinLocalY(parent, new Vector3(max.x, max.y, max.z), ref minLocalY);
        }

        private static void UpdateMinLocalY(Transform parent, Vector3 worldPoint, ref float minLocalY)
        {
            var localPoint = parent.InverseTransformPoint(worldPoint);
            if (localPoint.y < minLocalY)
            {
                minLocalY = localPoint.y;
            }
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
