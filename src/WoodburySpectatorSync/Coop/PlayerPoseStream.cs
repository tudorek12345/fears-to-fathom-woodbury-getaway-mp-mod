using System;
using UnityEngine;
using WoodburySpectatorSync.Net;

namespace WoodburySpectatorSync.Coop
{
    // Shared by both peers. Never compare remote wall clocks to order poses.
    internal sealed class PlayerPoseStream
    {
        private uint _sequence;
        private bool _received;
        private int _session, _generation;

        public bool Accept(PlayerTransformState state, byte playerId, int session, int generation)
        {
            if (state.PlayerId != playerId || state.SessionId != session || state.Generation != generation ||
                session <= 0 || generation <= 0 || !PlayerPoseRules.IsValid(state)) return false;
            if (_session != session || _generation != generation)
            {
                _received = false;
                _session = session;
                _generation = generation;
            }
            if (_received && unchecked((int)(state.Sequence - _sequence)) <= 0) return false;
            _sequence = state.Sequence;
            _received = true;
            return true;
        }

        public void Reset() { _received = false; _session = _generation = 0; }
    }

    internal static class PlayerPoseRules
    {
        public static bool Has(PlayerPoseFlags flags, PlayerPoseFlags bit) => (flags & bit) != 0;
        public static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        private static bool VectorValid(Vector3 v, float limit) => Finite(v.x) && Finite(v.y) && Finite(v.z) &&
            Math.Abs(v.x) <= limit && Math.Abs(v.y) <= limit && Math.Abs(v.z) <= limit;
        private static bool RotationValid(Quaternion q)
        {
            if (!Finite(q.x) || !Finite(q.y) || !Finite(q.z) || !Finite(q.w)) return false;
            float square = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
            return square > 0.9f && square < 1.1f;
        }
        public static bool IsValid(PlayerTransformState p) =>
            (p.PoseFlags & ~PlayerPoseFlags.All) == 0 && Has(p.PoseFlags, PlayerPoseFlags.Valid) &&
            VectorValid(p.Position, 100000f) && VectorValid(p.CameraPosition, 100000f) &&
            RotationValid(p.Rotation) && RotationValid(p.CameraRotation) && VectorValid(p.LocalVelocity, 30f) &&
            (!Has(p.PoseFlags, PlayerPoseFlags.LeftHand) || (VectorValid(p.LeftHandPosition, 3f) && RotationValid(p.LeftHandRotation))) &&
            (!Has(p.PoseFlags, PlayerPoseFlags.RightHand) || (VectorValid(p.RightHandPosition, 3f) && RotationValid(p.RightHandRotation)));

        // Hysteresis adapted from the author's MWCMP PlayerPoseAnimation.
        public static string SelectMotion(PlayerPoseFlags flags, float speed, string previous)
        {
            if (Has(flags, PlayerPoseFlags.Seated)) return "Sit";
            if (Has(flags, PlayerPoseFlags.Airborne)) return "Jump";
            bool moving = speed > (previous == "Idle" || previous == "Crouch" || previous == null ? 0.2f : 0.1f);
            if (Has(flags, PlayerPoseFlags.Crouching)) return moving ? "CrouchWalk" : "Crouch";
            if (!moving) return "Idle";
            return Has(flags, PlayerPoseFlags.Sprinting) || speed > (previous == "Run" ? 3f : 3.8f) ? "Run" : "Walk";
        }
    }
}
