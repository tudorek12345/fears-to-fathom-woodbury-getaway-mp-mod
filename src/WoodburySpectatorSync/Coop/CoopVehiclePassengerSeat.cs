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
        private const float PizzeriaTruckBedY = 0.62f;
        private const float PizzeriaTruckBedZ = -1.34f;
        private const float LookUpPitch = 0f;

        private static readonly Dictionary<string, FieldInfo> FieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);

        public static bool TryResolve(string role, SeatSide requestedSide, out SeatPose pose)
        {
            pose = default;
            var sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName))
            {
                return false;
            }

            if (sceneName.IndexOf("RoadTrip", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return TryResolveRoadTripSeat(role, requestedSide, out pose);
            }

            if (sceneName.IndexOf("Pizzeria", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return TryResolvePizzeriaSeat(role, requestedSide, out pose);
            }

            return false;
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

        private static bool TryResolveRoadTripSeat(string role, SeatSide requestedSide, out SeatPose pose)
        {
            pose = default;
            var truck = UnityEngine.Object.FindObjectOfType<MikeTruckInLoopScene>();
            if (truck == null || truck.transform == null || !truck.gameObject.activeInHierarchy)
            {
                return false;
            }

            var speed = 0f;
            TryReadFloat(truck, "speed", out speed);
            return BuildSeatPose(role, requestedSide, truck.transform, speed, SeatRootHeight, BackSeatZ, "back", true, out pose);
        }

        private static bool TryResolvePizzeriaSeat(string role, SeatSide requestedSide, out SeatPose pose)
        {
            pose = default;
            var manager = UnityEngine.Object.FindObjectOfType<PizzeriaGameManager>();
            var driving = GetFieldValue<MikeDrivingInPizzeriaScene>(manager, "mikeDriving") ??
                          UnityEngine.Object.FindObjectOfType<MikeDrivingInPizzeriaScene>();
            if (driving == null || driving.transform == null || !driving.gameObject.activeInHierarchy)
            {
                return false;
            }

            var player = UnityEngine.Object.FindObjectOfType<PizzeriaPlayerController>();
            var drivingCameraActive = IsObjectActive(player != null ? player.playerDrivingParent : null) ||
                                      IsObjectActive(player != null ? player.playerDrivingCam : null);
            var mike = manager != null && manager.mikePizzeria != null
                ? manager.mikePizzeria
                : UnityEngine.Object.FindObjectOfType<MikePizzeria>();
            var speed = 0f;
            TryReadFloat(driving, "speed", out speed);
            var startSlowDown = GetFieldValue<bool>(driving, "startSlowDown");
            var pushBreak = GetFieldValue<bool>(driving, "pushBreak");
            var mikeStillInIntroCar = mike != null &&
                                      mike.state == MikePizzeria.State.InCar &&
                                      (Mathf.Abs(speed) > 0.02f || !startSlowDown || !pushBreak);

            if (!drivingCameraActive && !mikeStillInIntroCar)
            {
                return false;
            }

            return BuildSeatPose(role, requestedSide, driving.transform, speed, PizzeriaTruckBedY, PizzeriaTruckBedZ, "truck-bed", false, out pose);
        }

        private static bool BuildSeatPose(
            string role,
            SeatSide requestedSide,
            Transform root,
            float rawSpeed,
            float fallbackHeight,
            float fallbackZ,
            string fallbackName,
            bool allowExplicitSeat,
            out SeatPose pose)
        {
            pose = default;
            if (root == null)
            {
                return false;
            }

            var side = ResolveSide(role, requestedSide);
            var speed = Mathf.Clamp(rawSpeed, -40f, 40f);
            var velocity = root.forward * speed;
            var poseSpeed = velocity.magnitude;
            var explicitSeat = allowExplicitSeat ? FindExplicitSeat(root, side) : null;
            if (explicitSeat != null)
            {
                pose = new SeatPose
                {
                    Active = true,
                    Position = explicitSeat.position - (explicitSeat.up * ExplicitSeatRootDrop),
                    Rotation = ResolveSeatRotation(explicitSeat.rotation),
                    Velocity = velocity,
                    Speed = poseSpeed,
                    VehicleName = root.name,
                    SeatName = explicitSeat.name,
                    FallbackPose = false
                };
                return true;
            }

            var localOffset = new Vector3(side == SeatSide.BackLeft ? -BackSeatX : BackSeatX, fallbackHeight, fallbackZ);
            pose = new SeatPose
            {
                Active = true,
                Position = root.TransformPoint(localOffset),
                Rotation = ResolveSeatRotation(root.rotation),
                Velocity = velocity,
                Speed = poseSpeed,
                VehicleName = root.name,
                SeatName = fallbackName + (side == SeatSide.BackLeft ? "-left" : "-right"),
                FallbackPose = true
            };
            return true;
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

        public static bool TryReadTruckFloat(MikeTruckInLoopScene truck, string fieldName, out float value)
        {
            return TryReadFloat(truck, fieldName, out value);
        }

        private static bool TryReadFloat(object target, string fieldName, out float value)
        {
            value = 0f;
            if (target == null || string.IsNullOrEmpty(fieldName)) return false;

            var field = GetField(target.GetType(), fieldName);
            if (field == null || field.FieldType != typeof(float)) return false;

            try
            {
                value = (float)field.GetValue(target);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsObjectActive(object target)
        {
            var go = GetGameObject(target);
            return go != null && go.activeSelf;
        }

        private static GameObject GetGameObject(object target)
        {
            if (target is GameObject go) return go;
            if (target is Component component) return component.gameObject;
            return null;
        }

        private static T GetFieldValue<T>(object target, string fieldName)
        {
            if (target == null || string.IsNullOrEmpty(fieldName)) return default(T);
            var field = GetField(target.GetType(), fieldName);
            if (field == null) return default(T);

            try
            {
                var value = field.GetValue(target);
                if (value is T typed) return typed;
            }
            catch
            {
            }

            return default(T);
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
