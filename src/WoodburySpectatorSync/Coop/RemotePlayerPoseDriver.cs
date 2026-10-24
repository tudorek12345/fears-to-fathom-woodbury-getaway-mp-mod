using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using WoodburySpectatorSync.Net;

namespace WoodburySpectatorSync.Coop
{
    // Runs after Animator evaluation so head/hand poses survive native NPC controllers.
    internal sealed class RemotePlayerPoseDriver : MonoBehaviour
    {
        private sealed class Bone
        {
            public Transform Transform;
            public Quaternion Before;
            public Vector3 Position;
            public Quaternion Rest;
            public Vector3 RestPosition;
        }
        private readonly List<Bone> _bones = new List<Bone>();
        private Transform _head, _chest, _hips, _leftArm, _leftElbow, _leftHand, _rightArm, _rightElbow, _rightHand;
        private Transform _leftLeg, _leftKnee, _rightLeg, _rightKnee;
        private Transform _leftFoot, _rightFoot;
        private PlayerTransformState _state;
        private float _receivedAt, _phase, _speed, _pitch, _yaw, _crouch, _leftWeight, _rightWeight, _sit;
        private float _air, _run, _idleTime;
        private Vector3 _velocity;
        private bool _hasState, _applied, _procedural, _nativeSeated, _nativeCrouch;
        private Vector3 _leftTarget, _rightTarget;
        private Quaternion _leftWristRest, _rightWristRest;
        private bool _captureActive;
        private PlayerTransformState _captureLatest;
        private int _pendingCapture = -1;
        private float _appearanceCaptureAt = -1;
        public void ScheduleAppearanceCapture()
        { if(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WSS_CAPTURE_AVATARS"))) _appearanceCaptureAt=Time.realtimeSinceStartup+.5f; }
        public Action<PlayerTransformState, bool> Animate;
        public string RigSummary { get; private set; }

        public void Initialize(Animator animator, bool procedural, bool nativeSeated, bool nativeCrouch)
        {
            _procedural = procedural; _nativeSeated = nativeSeated; _nativeCrouch = nativeCrouch;
            foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true)) renderer.updateWhenOffscreen = true;
            _head = Find(animator, HumanBodyBones.Head, "head");
            _chest = Find(animator, HumanBodyBones.Chest, "spine2", "spine02", "chest", "spine1");
            _hips = Find(animator, HumanBodyBones.Hips, "hips", "pelvis");
            _leftArm = Find(animator, HumanBodyBones.LeftUpperArm, "leftarm", "leftupperarm", "lupperarm", "upperarml");
            _leftElbow = Find(animator, HumanBodyBones.LeftLowerArm, "leftforearm", "leftlowerarm", "lforearm", "lowerarml");
            _leftHand = Find(animator, HumanBodyBones.LeftHand, "lefthand", "lhand", "handl");
            _rightArm = Find(animator, HumanBodyBones.RightUpperArm, "rightarm", "rightupperarm", "rupperarm", "upperarmr");
            _rightElbow = Find(animator, HumanBodyBones.RightLowerArm, "rightforearm", "rightlowerarm", "rforearm", "lowerarmr");
            _rightHand = Find(animator, HumanBodyBones.RightHand, "righthand", "rhand", "handr");
            _leftLeg = Find(animator, HumanBodyBones.LeftUpperLeg, "leftupleg", "leftthigh", "lthigh", "thighl");
            _leftKnee = Find(animator, HumanBodyBones.LeftLowerLeg, "leftleg", "leftcalf", "lcalf", "calfl");
            _rightLeg = Find(animator, HumanBodyBones.RightUpperLeg, "rightupleg", "rightthigh", "rthigh", "thighr");
            _rightKnee = Find(animator, HumanBodyBones.RightLowerLeg, "rightleg", "rightcalf", "rcalf", "calfr");
            _leftFoot = Find(animator, HumanBodyBones.LeftFoot, "leftfoot", "lfoot", "footl");
            _rightFoot = Find(animator, HumanBodyBones.RightFoot, "rightfoot", "rfoot", "footr");
            if (_procedural)
            {
                // Scene NPCs can be cloned while sitting, holding a phone or crossing their arms.
                // Establish a standing base from bone directions before adding locomotion curves.
                Align(_leftLeg, _leftKnee, new Vector3(0f, -1f, -0.025f));
                Align(_rightLeg, _rightKnee, new Vector3(0f, -1f, -0.025f));
                Align(_leftKnee, _leftFoot, Vector3.down);
                Align(_rightKnee, _rightFoot, Vector3.down);
                Align(_leftArm, _leftElbow, new Vector3(-0.13f, -1f, 0f));
                Align(_rightArm, _rightElbow, new Vector3(0.13f, -1f, 0f));
                Align(_leftElbow, _leftHand, new Vector3(-0.05f, -1f, 0.02f));
                Align(_rightElbow, _rightHand, new Vector3(0.05f, -1f, 0.02f));
            }
            foreach (var bone in _bones) { bone.Rest = bone.Transform.localRotation; bone.RestPosition = bone.Transform.localPosition; }
            if (_procedural && animator != null) animator.enabled = false;
            _leftWristRest = _leftHand != null ? Quaternion.Inverse(transform.rotation) * _leftHand.rotation : Quaternion.identity;
            _rightWristRest = _rightHand != null ? Quaternion.Inverse(transform.rotation) * _rightHand.rotation : Quaternion.identity;
            RigSummary = "head=" + (_head != null) + " torso=" + (_chest != null) +
                " leftArm=" + (_leftArm != null && _leftElbow != null && _leftHand != null) +
                " rightArm=" + (_rightArm != null && _rightElbow != null && _rightHand != null) + " procedural=" + procedural +
                " legs=" + (_leftLeg != null && _rightLeg != null && _leftKnee != null && _rightKnee != null) +
                " feet=" + (_leftFoot != null && _rightFoot != null) + " animation=directional-v2";
        }

        private Transform Find(Animator animator, HumanBodyBones id, params string[] aliases)
        {
            Transform result = null;
            if (animator != null && animator.avatar != null && animator.avatar.isValid && animator.isHuman)
                result = animator.GetBoneTransform(id);
            if (result == null)
            {
                foreach (var candidate in GetComponentsInChildren<Transform>(true))
                {
                    string name = candidate.name.ToLowerInvariant().Replace("_", "").Replace(" ", "").Replace(".", "").Replace(":", "");
                    foreach (string prefix in new[] { "mixamorig", "bip001", "bip01", "ccbase" })
                        if (name.StartsWith(prefix, StringComparison.Ordinal)) name = name.Substring(prefix.Length);
                    if (Array.IndexOf(aliases, name) >= 0) { result = candidate; break; }
                }
            }
            if (result != null && !_bones.Exists(b => b.Transform == result)) _bones.Add(new Bone { Transform = result });
            return result;
        }

        public void Receive(PlayerTransformState state, bool seated)
        {
            if (!PlayerPoseRules.IsValid(state)) return;
            if (_captureActive)
            {
                _captureLatest = state;
                if (seated) _captureLatest.PoseFlags |= PlayerPoseFlags.Seated;
                return;
            }
            bool snap = !_hasState || _state.SessionId != state.SessionId || _state.Generation != state.Generation ||
                Vector3.Distance(_state.Position, state.Position) > 4f;
            _state = state;
            if (seated) _state.PoseFlags |= PlayerPoseFlags.Seated;
            _receivedAt = Time.realtimeSinceStartup;
            if (snap)
            {
                _speed = _phase = _air = _run = _leftWeight = _rightWeight = 0f; _velocity = Vector3.zero;
                _sit = PlayerPoseRules.Has(_state.PoseFlags, PlayerPoseFlags.Seated) ? 1f : 0f;
                _crouch = PlayerPoseRules.Has(_state.PoseFlags, PlayerPoseFlags.Crouching) && _sit == 0f ? 1f : 0f;
                _leftTarget = state.LeftHandPosition; _rightTarget = state.RightHandPosition;
            }
            _hasState = true;
        }

        private void Update()
        {
            Restore();
            if (!_hasState) return;
            Animate?.Invoke(_state, Time.realtimeSinceStartup - _receivedAt > 0.5f);
        }

        private void Restore()
        {
            if (!_applied) return;
            foreach (var bone in _bones)
                if (bone.Transform != null) { bone.Transform.localRotation = bone.Before; bone.Transform.localPosition = bone.Position; }
            _applied = false;
        }

        private void OnDisable()
        {
            Restore(); _hasState = false; _speed = _leftWeight = _rightWeight = _phase = _air = _run = 0f; _velocity = Vector3.zero;
            _captureActive = false; _pendingCapture = -1;
        }

        private void LateUpdate()
        {
            if (!_hasState) return;
            if (_procedural)
                foreach (var bone in _bones)
                    if (bone.Transform != null) { bone.Transform.localRotation = bone.Rest; bone.Transform.localPosition = bone.RestPosition; }
            foreach (var bone in _bones)
                if (bone.Transform != null) { bone.Before = bone.Transform.localRotation; bone.Position = bone.Transform.localPosition; }
            _applied = true;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            float blend = 1f - Mathf.Exp(-14f * dt);
            bool fresh = Time.realtimeSinceStartup - _receivedAt <= 0.5f;
            var flags = _state.PoseFlags;
            bool seated = PlayerPoseRules.Has(flags, PlayerPoseFlags.Seated);
            float speed = fresh && !seated ? new Vector2(_state.LocalVelocity.x, _state.LocalVelocity.z).magnitude : 0f;
            _speed = Mathf.Lerp(_speed, speed, blend);
            _crouch = Mathf.Lerp(_crouch, PlayerPoseRules.Has(flags, PlayerPoseFlags.Crouching) && !seated ? 1f : 0f, blend);
            _sit = Mathf.Lerp(_sit, seated ? 1f : 0f, blend);
            _air = Mathf.Lerp(_air, fresh && !seated && PlayerPoseRules.Has(flags, PlayerPoseFlags.Airborne) ? 1f : 0f, blend);
            _run = Mathf.Lerp(_run, fresh && !seated && (speed > 3.8f || PlayerPoseRules.Has(flags, PlayerPoseFlags.Sprinting)) ? 1f : 0f, blend);
            _velocity = Vector3.Lerp(_velocity, fresh && !seated ? _state.LocalVelocity : Vector3.zero, blend);
            _phase = (_phase + dt * CharacterAnimation.CycleRate(_speed) * (1f - _sit) * (1f - _air)) % (Mathf.PI * 2f);
            _idleTime += dt;
            var look = Quaternion.Inverse(transform.rotation) * _state.CameraRotation;
            _pitch = Mathf.LerpAngle(_pitch, Mathf.Clamp(Mathf.DeltaAngle(0f, look.eulerAngles.x), -75f, 75f), blend);
            _yaw = Mathf.LerpAngle(_yaw, Mathf.Clamp(Mathf.DeltaAngle(0f, look.eulerAngles.y), -80f, 80f), blend);
            if (_procedural || (!_nativeSeated && _sit > 0.001f) || (!_nativeCrouch && _crouch > 0.001f))
            {
                ApplyFrame(CharacterAnimation.Evaluate(_procedural ? _velocity.z : 0f, _procedural ? _velocity.x : 0f,
                    _phase, _idleTime, _nativeCrouch ? 0f : _crouch, _nativeSeated ? 0f : _sit, _procedural ? _air : 0f, _run));
            }
            Rotate(_chest, Quaternion.Euler(_pitch * 0.2f, _yaw * 0.25f, 0f));
            Rotate(_head, Quaternion.Euler(_pitch * 0.8f, _yaw * 0.75f, 0f));

            _leftWeight = Mathf.Lerp(_leftWeight, fresh && PlayerPoseRules.Has(flags, PlayerPoseFlags.LeftHand) ? 1f : 0f, blend);
            _rightWeight = Mathf.Lerp(_rightWeight, fresh && PlayerPoseRules.Has(flags, PlayerPoseFlags.RightHand) ? 1f : 0f, blend);
            _leftTarget = Vector3.Lerp(_leftTarget, _state.LeftHandPosition, blend);
            _rightTarget = Vector3.Lerp(_rightTarget, _state.RightHandPosition, blend);
            PoseArm(_leftArm, _leftElbow, _leftHand, _leftTarget, -1f, _leftWeight);
            PoseArm(_rightArm, _rightElbow, _rightHand, _rightTarget, 1f, _rightWeight);
            if (_leftHand != null && _leftWeight > 0.001f && PlayerPoseRules.Has(flags, PlayerPoseFlags.LeftHand))
                _leftHand.rotation = Quaternion.Slerp(_leftHand.rotation, transform.rotation * _state.LeftHandRotation * _leftWristRest, _leftWeight);
            if (_rightHand != null && _rightWeight > 0.001f && PlayerPoseRules.Has(flags, PlayerPoseFlags.RightHand))
                _rightHand.rotation = Quaternion.Slerp(_rightHand.rotation, transform.rotation * _state.RightHandRotation * _rightWristRest, _rightWeight);
            if (_pendingCapture >= 0) { int index = _pendingCapture; _pendingCapture = -1; CaptureProbeImage(index); }
            if (_appearanceCaptureAt>=0 && Time.realtimeSinceStartup>=_appearanceCaptureAt) { _appearanceCaptureAt=-1; CaptureProbeImage(7); }
        }

        private void ApplyFrame(CharacterAnimationFrame frame)
        {
            float baseline = _leftFoot != null && _rightFoot != null ? Mathf.Min(FootHeight(_leftFoot), FootHeight(_rightFoot)) : 0f;
            Rotate(_leftLeg, Quaternion.Euler(frame.LeftThigh)); Rotate(_rightLeg, Quaternion.Euler(frame.RightThigh));
            Rotate(_leftKnee, Quaternion.Euler(frame.LeftKnee)); Rotate(_rightKnee, Quaternion.Euler(frame.RightKnee));
            Rotate(_leftArm, Quaternion.Euler(frame.LeftArm)); Rotate(_rightArm, Quaternion.Euler(frame.RightArm));
            Rotate(_leftElbow, Quaternion.Euler(frame.LeftElbow)); Rotate(_rightElbow, Quaternion.Euler(frame.RightElbow));
            Rotate(_leftFoot, Quaternion.Euler(frame.LeftFoot)); Rotate(_rightFoot, Quaternion.Euler(frame.RightFoot));
            Rotate(_chest, Quaternion.Euler(frame.Chest));
            if (_hips != null) _hips.position += transform.rotation * frame.HipOffset;
            if (_procedural && _hips != null && _leftFoot != null && _rightFoot != null)
                _hips.position += transform.up * CharacterAnimation.GroundCorrection(baseline, FootHeight(_leftFoot), FootHeight(_rightFoot), frame.GroundedWeight);
        }

        private float FootHeight(Transform foot) => Vector3.Dot(foot.position - transform.position, transform.up);

        private void Align(Transform bone, Transform child, Vector3 localDirection)
        {
            if (bone == null || child == null || Vector3.Distance(bone.position, child.position) < 0.001f) return;
            bone.rotation = Quaternion.FromToRotation(child.position - bone.position, transform.rotation * localDirection) * bone.rotation;
        }

        // Opt-in runtime regression: apply real animation curves to each scene's cloned skeleton,
        // verify actual bone motion, then restore every transform before network playback begins.
        internal string Probe()
        {
            if (_leftFoot == null || _rightFoot == null || _head == null || _hips == null || _leftHand == null || _rightHand == null) return "SKIP incomplete-rig";
            var rotations = _bones.ConvertAll(b => b.Transform.localRotation);
            var positions = _bones.ConvertAll(b => b.Transform.localPosition);
            Action restore = () => { for (int i = 0; i < _bones.Count; i++) { _bones[i].Transform.localRotation = rotations[i]; _bones[i].Transform.localPosition = positions[i]; } };
            bool ok = true;
            var problems = new List<string>(); int sampleIndex = 0;
            bool skinnedLeft = false, skinnedRight = false, skinnedHead = false;
            foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skinnedLeft |= Array.IndexOf(renderer.bones, _leftLeg) >= 0;
                skinnedRight |= Array.IndexOf(renderer.bones, _rightLeg) >= 0;
                skinnedHead |= Array.IndexOf(renderer.bones, _head) >= 0;
            }
            ok &= skinnedLeft && skinnedRight && skinnedHead;
            if (!ok) problems.Add("skinned-bone-bindings");
            try
            {
                foreach (var sample in new[] {
                    CharacterAnimation.Evaluate(2,0,1.57f,0,0,0,0,0), CharacterAnimation.Evaluate(5,0,1.57f,0,0,0,0,1),
                    CharacterAnimation.Evaluate(0,2,1.57f,0,0,0,0,0), CharacterAnimation.Evaluate(-2,0,1.57f,0,0,0,0,0),
                    CharacterAnimation.Evaluate(0,0,0,0,1,0,0,0), CharacterAnimation.Evaluate(0,0,0,0,0,1,0,0),
                    CharacterAnimation.Evaluate(0,0,0,0,0,0,1,0) })
                {
                    restore(); Quaternion thigh = _leftLeg.rotation, arm = _leftElbow.rotation;
                    ApplyFrame(sample);
                    // A grounded crouch deliberately keeps feet planted. Verify articulation,
                    // not foot translation, so fixed feet cannot be mistaken for a frozen rig.
                    float thighAngle = Quaternion.Angle(thigh, _leftLeg.rotation), armAngle = Quaternion.Angle(arm, _leftElbow.rotation);
                    bool moved = thighAngle > 2f && armAngle > 2f;
                    ok &= moved;
                    if (!moved) problems.Add("motion" + sampleIndex + ":thigh=" + thighAngle.ToString("F2") + ":arm=" + armAngle.ToString("F2"));
                    sampleIndex++;
                }
                restore(); Quaternion head = _head.rotation; Rotate(_head, Quaternion.Euler(30, 20, 0)); ok &= Quaternion.Angle(head, _head.rotation) > 10;
                restore(); Vector3 wrist = _leftHand.position;
                PoseArm(_leftArm, _leftElbow, _leftHand, new Vector3(-0.25f, 1.2f, 0.35f), -1, 1);
                ok &= Vector3.Distance(wrist, _leftHand.position) > 0.02f;
                restore();
                for (int i = 0; i < 120; i++) { ApplyFrame(CharacterAnimation.Evaluate(2,0,i*0.1f,0,0,0,0,0)); restore(); }
                for (int i = 0; i < _bones.Count; i++) ok &= Quaternion.Angle(rotations[i], _bones[i].Transform.localRotation) < 0.05f && Vector3.Distance(positions[i], _bones[i].Transform.localPosition) < 0.0001f;
                return (ok ? "PASS" : "FAIL") + " walk/run/strafe/backward/crouch/sit/jump/head/hand/restore" + (problems.Count == 0 ? "" : " details=" + string.Join(",", problems));
            }
            finally { restore(); }
        }

        internal void BeginGraphicsProbe()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WSS_CAPTURE_AVATARS")) && _leftFoot != null && _rightFoot != null)
                StartCoroutine(CaptureSequence());
        }

        private IEnumerator CaptureSequence()
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!_hasState && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_hasState) yield break;
            _captureLatest = _state; _captureActive = true;
            try
            {
                for (int i = 0; i < 7; i++)
                {
                    _state = _captureLatest; _state.PoseFlags = PlayerPoseFlags.Valid;
                    _state.CameraRotation = transform.rotation;
                    _state.LocalVelocity = i == 0 ? new Vector3(0,0,2) : i == 1 ? new Vector3(0,0,5) : i == 2 ? new Vector3(2,0,0) : i == 3 ? new Vector3(0,0,-2) : Vector3.zero;
                    if (i == 1) _state.PoseFlags |= PlayerPoseFlags.Sprinting;
                    if (i == 4) _state.PoseFlags |= PlayerPoseFlags.Crouching;
                    if (i == 5) _state.PoseFlags |= PlayerPoseFlags.Seated;
                    if (i == 6) _state.PoseFlags |= PlayerPoseFlags.Airborne;
                    _receivedAt = Time.realtimeSinceStartup;
                    // Let the real Update/LateUpdate and Unity skinning run across frames.
                    yield return new WaitForSecondsRealtime(0.35f);
                    _pendingCapture = i;
                    while (_pendingCapture >= 0) yield return null;
                }
            }
            finally { _captureActive = false; Receive(_captureLatest, PlayerPoseRules.Has(_captureLatest.PoseFlags, PlayerPoseFlags.Seated)); }
        }

        private void CaptureProbeImage(int index)
        {
            string directory = Environment.GetEnvironmentVariable("WSS_CAPTURE_AVATARS");
            if (string.IsNullOrEmpty(directory)) return;
            var encode = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule")?.GetMethod("EncodeToPNG",
                BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(Texture2D) }, null);
            if (encode == null) return;
            var children = GetComponentsInChildren<Transform>(true);
            var layers = Array.ConvertAll(children, t => t.gameObject.layer);
            var cameraRoot = new GameObject("Coop animation capture camera");
            var camera = cameraRoot.AddComponent<Camera>();
            var light = cameraRoot.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.5f; light.cullingMask = 1 << 31;
            var target = new RenderTexture(512, 768, 24);
            var texture = new Texture2D(512, 768, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                foreach (var child in children) child.gameObject.layer = 31;
                camera.enabled = false; camera.cullingMask = 1 << 31; camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.16f, 0.17f, 0.2f); camera.fieldOfView = 45; camera.nearClipPlane = 0.05f;
                var renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var bounds = new Bounds(transform.position + transform.up, new Vector3(0.6f, 1.8f, 0.5f));
                if (renderers.Length > 0) { bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds); }
                var centre = bounds.center;
                camera.transform.position = centre + transform.rotation * new Vector3(0.75f, 0.2f, Mathf.Max(3.1f, bounds.size.y * 1.7f));
                camera.transform.LookAt(centre); camera.targetTexture = target;
                camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 512, 768), 0, 0); texture.Apply();
                Directory.CreateDirectory(directory);
                string filename = gameObject.scene.name + "-" + System.Diagnostics.Process.GetCurrentProcess().Id + "-" + index + ".png";
                File.WriteAllBytes(Path.Combine(directory, filename), (byte[])encode.Invoke(null, new object[] { texture }));
            }
            finally
            {
                for (int i = 0; i < children.Length; i++) children[i].gameObject.layer = layers[i];
                RenderTexture.active = previous; camera.targetTexture = null; target.Release();
                Destroy(cameraRoot); Destroy(target); Destroy(texture);
            }
        }

        private void Rotate(Transform bone, Quaternion offset)
        {
            if (bone != null) bone.rotation = transform.rotation * offset * Quaternion.Inverse(transform.rotation) * bone.rotation;
        }

        private void PoseArm(Transform upper, Transform elbow, Transform hand, Vector3 localTarget, float side, float weight)
        {
            if (upper == null || elbow == null || hand == null || weight < 0.001f) return;
            Vector3 target = Vector3.Lerp(hand.position, transform.position + transform.rotation * localTarget, weight);
            float a = Vector3.Distance(upper.position, elbow.position), b = Vector3.Distance(elbow.position, hand.position);
            if (a < 0.001f || b < 0.001f) return;
            Vector3 joint = LimbPoseMath.JointPosition(upper.position, ref target,
                upper.position + transform.rotation * new Vector3(side * 0.5f, -0.45f, -0.25f), a, b);
            upper.rotation = Quaternion.FromToRotation(elbow.position - upper.position, joint - upper.position) * upper.rotation;
            elbow.rotation = Quaternion.FromToRotation(hand.position - elbow.position, target - elbow.position) * elbow.rotation;
        }
    }

}
