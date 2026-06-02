using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WoodburySpectatorSync.Coop
{
    internal static class CoopVehiclePassengerSeat
    {
        public enum SeatSide
        {
            Auto = 0,
            BackLeft = 1,
            BackRight = 2
        }

        public struct SeatPose
        {
            public bool Active;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Velocity;
            public float Speed;
            public string VehicleName;
            public string SeatName;
            public bool FallbackPose;
        }

        private const float SeatRootHeight = 0.38f;
        private const float ExplicitSeatRootDrop = 0.52f;
        private const float BackSeatZ = -0.88f;
        private const float BackSeatX = 0.38f;
        private const float LookUpPitch = 0f;

        private static readonly Dictionary<string, FieldInfo> FieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);

        public static bool TryResolve(string role, SeatSide requestedSide, out SeatPose pose)
        {
            pose = default;
            var sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName) ||
                sceneName.IndexOf("RoadTrip", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            var truck = UnityEngine.Object.FindObjectOfType<MikeTruckInLoopScene>();
            if (truck == null || truck.transform == null || !truck.gameObject.activeInHierarchy)
            {
                return false;
            }

            var side = ResolveSide(role, requestedSide);
            var root = truck.transform;
            var velocity = ResolveVehicleVelocity(truck, root);
            var speed = velocity.magnitude;
            var explicitSeat = FindExplicitSeat(root, side);
            if (explicitSeat != null)
            {
                pose = new SeatPose
                {
                    Active = true,
                    Position = explicitSeat.position - (explicitSeat.up * ExplicitSeatRootDrop),
                    Rotation = ResolveSeatRotation(explicitSeat.rotation),
                    Velocity = velocity,
                    Speed = speed,
                    VehicleName = root.name,
                    SeatName = explicitSeat.name,
                    FallbackPose = false
                };
                return true;
            }

            var localOffset = new Vector3(side == SeatSide.BackLeft ? -BackSeatX : BackSeatX, SeatRootHeight, BackSeatZ);
            pose = new SeatPose
            {
                Active = true,
                Position = root.TransformPoint(localOffset),
                Rotation = ResolveSeatRotation(root.rotation),
                Velocity = velocity,
                Speed = speed,
                VehicleName = root.name,
                SeatName = side == SeatSide.BackLeft ? "back-left" : "back-right",
                FallbackPose = true
            };
            return true;
        }

        public static SeatSide ResolveSide(string role, SeatSide requestedSide)
        {
            if (requestedSide == SeatSide.BackLeft || requestedSide == SeatSide.BackRight)
            {
                return requestedSide;
            }

            if (!string.IsNullOrEmpty(role) &&
                role.IndexOf("CLIENT", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return SeatSide.BackRight;
            }

            return SeatSide.BackLeft;
        }

        private static Transform FindExplicitSeat(Transform root, SeatSide side)
        {
            if (root == null) return null;

            var children = root.GetComponentsInChildren<Transform>(true);
            Transform fallback = null;
            for (var i = 0; i < children.Length; i++)
            {
                var child = children[i];
                if (child == null || child == root) continue;

                var name = child.name ?? string.Empty;
                if (name.IndexOf("seat", StringComparison.OrdinalIgnoreCase) < 0 &&
                    name.IndexOf("passenger", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                var wantsLeft = side == SeatSide.BackLeft;
                var hasLeft = name.IndexOf("left", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              name.IndexOf("_l", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              name.IndexOf(".l", StringComparison.OrdinalIgnoreCase) >= 0;
                var hasRight = name.IndexOf("right", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               name.IndexOf("_r", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               name.IndexOf(".r", StringComparison.OrdinalIgnoreCase) >= 0;
                var hasBack = name.IndexOf("back", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              name.IndexOf("rear", StringComparison.OrdinalIgnoreCase) >= 0;

                if (hasBack && ((wantsLeft && hasLeft) || (!wantsLeft && hasRight)))
                {
                    return child;
                }

                if (fallback == null && ((wantsLeft && hasLeft) || (!wantsLeft && hasRight)))
                {
                    fallback = child;
                }
            }

            return fallback;
        }

        private static Quaternion ResolveSeatRotation(Quaternion baseRotation)
        {
            var forward = baseRotation * Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                return Quaternion.identity;
            }

            return Quaternion.LookRotation(forward.normalized, Vector3.up) * Quaternion.Euler(LookUpPitch, 0f, 0f);
        }

        private static Vector3 ResolveVehicleVelocity(MikeTruckInLoopScene truck, Transform root)
        {
            if (truck == null || root == null)
            {
                return Vector3.zero;
            }

            var speed = 0f;
            if (!TryReadTruckFloat(truck, "speed", out speed))
            {
                return Vector3.zero;
            }

            speed = Mathf.Clamp(speed, -40f, 40f);
            return root.forward * speed;
        }

        public static bool TryReadTruckFloat(MikeTruckInLoopScene truck, string fieldName, out float value)
        {
            value = 0f;
            if (truck == null || string.IsNullOrEmpty(fieldName)) return false;

            var field = GetField(truck.GetType(), fieldName);
            if (field == null || field.FieldType != typeof(float)) return false;

            try
            {
                value = (float)field.GetValue(truck);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static FieldInfo GetField(Type type, string name)
        {
            var key = (type != null ? type.FullName : string.Empty) + "." + name;
            if (FieldCache.TryGetValue(key, out var field))
            {
                return field;
            }

            var current = type;
            while (current != null)
            {
                field = current.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    FieldCache[key] = field;
                    return field;
                }

                current = current.BaseType;
            }

            FieldCache[key] = null;
            return null;
        }
    }
}
