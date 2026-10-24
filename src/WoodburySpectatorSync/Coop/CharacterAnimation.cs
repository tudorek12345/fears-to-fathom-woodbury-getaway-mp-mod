using System;
using UnityEngine;

namespace WoodburySpectatorSync.Coop
{
    internal struct CharacterAnimationFrame
    {
        public Vector3 LeftThigh, RightThigh, LeftKnee, RightKnee, LeftArm, RightArm, LeftElbow, RightElbow;
        public Vector3 LeftFoot, RightFoot, Chest, HipOffset;
        public float GroundedWeight;
    }

    // Shared, deterministic animation curves. Inputs come from the validated network pose,
    // never from this machine's keyboard or the NPC's story animator.
    internal static class CharacterAnimation
    {
        internal static float CycleRate(float speed) => Math.Min(15f, Math.Max(0f, speed) * 3.6f);
        internal static CharacterAnimationFrame Evaluate(float forward, float strafe, float phase, float idleTime,
            float crouch, float seated, float airborne, float run)
        {
            crouch = Unit(crouch) * (1f - Unit(seated)); seated = Unit(seated); airborne = Unit(airborne) * (1f - seated);
            run = Unit(run);
            float speed = (float)Math.Sqrt(forward * forward + strafe * strafe);
            float moving = Unit(speed / 1.5f) * (1f - seated) * (1f - airborne);
            float wave = (float)Math.Sin(phase), liftL = Math.Max(0f, wave), liftR = Math.Max(0f, -wave);
            float stride = moving * (29f + 17f * run) * (1f - crouch * 0.55f);
            float direction = speed > 0.01f ? forward / speed : 0f;
            float side = speed > 0.01f ? strafe / speed : 0f;
            float left = wave * stride, right = -left;
            float tuck = airborne * 25f;
            float breath = (float)Math.Sin(idleTime * 1.8f);
            return new CharacterAnimationFrame {
                LeftThigh = new Vector3(left * direction - crouch * 34f - seated * 78f - tuck, 0, -left * side),
                RightThigh = new Vector3(right * direction - crouch * 34f - seated * 78f - tuck * 0.7f, 0, -right * side),
                LeftKnee = new Vector3(liftR * moving * (35f + run * 30f) + crouch * 65f + seated * 82f + airborne * 55f, 0, 0),
                RightKnee = new Vector3(liftL * moving * (35f + run * 30f) + crouch * 65f + seated * 82f + airborne * 42f, 0, 0),
                LeftArm = new Vector3(-left * 0.65f - seated * 20f - airborne * 22f, 0, -airborne * 16f),
                RightArm = new Vector3(-right * 0.65f - seated * 20f - airborne * 22f, 0, airborne * 16f),
                LeftElbow = new Vector3(-12f - run * moving * 48f - seated * 55f - crouch * 15f, 0, 0),
                RightElbow = new Vector3(-12f - run * moving * 48f - seated * 55f - crouch * 15f, 0, 0),
                LeftFoot = new Vector3(-liftR * moving * 15f - crouch * 20f, 0, 0),
                RightFoot = new Vector3(-liftL * moving * 15f - crouch * 20f, 0, 0),
                Chest = new Vector3(crouch * 14f + run * moving * 8f + breath * 0.65f, wave * moving * 3f, -side * moving * 3f),
                GroundedWeight = (1f - seated) * (1f - airborne),
                HipOffset = new Vector3(0, -crouch * 0.28f + (float)Math.Cos(phase * 2f) * moving * (0.014f + run * 0.012f) + breath * 0.0025f, 0)
            };
        }
        internal static float GroundCorrection(float baseline, float left, float right, float weight) =>
            (baseline - Math.Min(left, right)) * Unit(weight);
        private static float Unit(float value) => Math.Max(0f, Math.Min(1f, value));
    }
}
