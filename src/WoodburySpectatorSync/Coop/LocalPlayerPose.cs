using System.Reflection;
using UnityEngine;
using WoodburySpectatorSync.Net;

namespace WoodburySpectatorSync.Coop
{
    internal sealed class LocalPlayerPose
    {
        private static readonly FieldInfo RightHand = typeof(PlayerController).GetField("handPosition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo LeftHand = typeof(PlayerController).GetField("handPositionFlashLight", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private Transform _body;
        private readonly PlayerMotionSampler _motion = new PlayerMotionSampler();
        private readonly PlayerAirborneSampler _airborne = new PlayerAirborneSampler();
        private int _session, _generation;
        private uint _sequence;

        public void Capture(ref PlayerTransformState state, FirstPersonController fpc, PlayerController player,
            Transform body, string bodySource, int session, int generation)
        {
            float now = Time.realtimeSinceStartup;
            if (bodySource == "roadtrip.passengerPivot")
                state.Position -= Vector3.up * 1.15f; // Passenger pivot is at seated eye height.
            if (fpc != null && body == fpc.transform && fpc.characterController != null && fpc.characterController.enabled)
                state.Position = body.TransformPoint(fpc.characterController.center - Vector3.up * (fpc.characterController.height * 0.5f));
            bool sameSource = _body == body && _session == session && _generation == generation;
            state.LocalVelocity = Quaternion.Inverse(state.Rotation) * _motion.Sample(state.Position, now, !sameSource);
            state.SessionId = session;
            state.Generation = generation;
            state.Sequence = ++_sequence;
            state.PoseFlags = PlayerPoseFlags.Valid;
            bool seated = !string.IsNullOrEmpty(bodySource) &&
                (bodySource.StartsWith("cabin.") || bodySource.StartsWith("pizzeria.") || bodySource.StartsWith("office.") || bodySource.StartsWith("roadtrip."));
            if (seated)
            {
                state.PoseFlags |= PlayerPoseFlags.Seated;
                state.LocalVelocity = Vector3.zero; // Moving vehicles are not walking players.
            }
            else if (fpc != null && fpc.enabled && fpc.gameObject.activeInHierarchy)
            {
                if (fpc.isCrouching) state.PoseFlags |= PlayerPoseFlags.Crouching;
                if (fpc.characterController != null && fpc.characterController.enabled &&
                    _airborne.Sample(fpc.characterController.isGrounded, fpc.CanMove && (player == null || !player.IsPlayerPaused()),
                        state.LocalVelocity.y, !sameSource))
                    state.PoseFlags |= PlayerPoseFlags.Airborne;
                if (!fpc.isCrouching && fpc.canSprint && Input.GetKey(KeyCode.LeftShift) && state.LocalVelocity.sqrMagnitude > 0.04f)
                    state.PoseFlags |= PlayerPoseFlags.Sprinting;
            }
            if (seated || fpc == null || !fpc.enabled || !fpc.gameObject.activeInHierarchy || fpc.characterController == null || !fpc.characterController.enabled)
                _airborne.Sample(true, false, 0f, true);
            if (player != null)
            {
                var right = player.GetHoldingObject();
                var left = player.GetHoldingObjectLeft();
                if (right != null && right.gameObject.activeInHierarchy)
                    CaptureHand(ref state, RightHand?.GetValue(player) as Transform ?? right.transform, false);
                if (left != null && left.gameObject.activeInHierarchy)
                    CaptureHand(ref state, LeftHand?.GetValue(player) as Transform ?? left.transform, true);
            }
            _body = body; _session = session; _generation = generation;
        }

        private static void CaptureHand(ref PlayerTransformState state, Transform hand, bool left)
        {
            if (hand == null) return;
            // Body-local coordinates remain stable while the receiving root interpolates.
            var inverse = Quaternion.Inverse(state.Rotation);
            var position = inverse * (hand.position - state.Position);
            if (position.sqrMagnitude > 9f) return; // Stale/inactive cinematic hand anchor.
            var rotation = inverse * hand.rotation;
            state.PoseFlags |= left ? PlayerPoseFlags.LeftHand : PlayerPoseFlags.RightHand;
            if (left) { state.LeftHandPosition = position; state.LeftHandRotation = rotation; }
            else { state.RightHandPosition = position; state.RightHandRotation = rotation; }
        }
    }
}
