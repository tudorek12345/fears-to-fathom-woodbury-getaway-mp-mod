using UnityEngine;

namespace WoodburySpectatorSync.Coop
{
    internal static class LimbPoseMath
    {
        // Adapted from tudorek12345's MWCMP: solve in world space, independent of imported bone axes.
        internal static Vector3 JointPosition(Vector3 root, ref Vector3 target, Vector3 pole, float upper, float lower)
        {
            Vector3 direction = target - root;
            float distance = direction.magnitude;
            direction = distance > 0.0001f ? direction / distance : Vector3.forward;
            distance = Mathf.Clamp(distance, Mathf.Abs(upper - lower) + 0.0001f, upper + lower - 0.0001f);
            target = root + direction * distance;
            Vector3 bend = pole - root;
            bend -= direction * Vector3.Dot(bend, direction);
            if (bend.sqrMagnitude < 0.000001f) bend = Vector3.Cross(direction, Vector3.up);
            if (bend.sqrMagnitude < 0.000001f) bend = Vector3.Cross(direction, Vector3.right);
            bend.Normalize();
            float along = (upper * upper - lower * lower + distance * distance) / (2f * distance);
            float height = Mathf.Sqrt(Mathf.Max(0f, upper * upper - along * along));
            return root + direction * along + bend * height;
        }
    }
}
