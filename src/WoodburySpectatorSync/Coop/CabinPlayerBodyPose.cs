using System;
using System.Reflection;
using UnityEngine;

namespace WoodburySpectatorSync.Coop
{
    internal static class CabinPlayerBodyPose
    {
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly string[] VisibleProxyFields =
        {
            "sittingPlayerEating",
            "sittingPlayerOuijaBoardGame",
            "sittingPlayerBoardGame",
            "sittingBedPlayer",
            "sleepingBedPlayer",
            "truckPlayer"
        };

        public static bool TryResolve(
            FirstPersonController firstPersonController,
            PlayerController playerController,
            out Transform bodyTransform,
            out string sourceName)
        {
            bodyTransform = null;
            sourceName = string.Empty;

            var cabinPlayer = playerController as CabinPlayerController ?? UnityEngine.Object.FindObjectOfType<CabinPlayerController>();
            if (cabinPlayer != null)
            {
                for (var i = 0; i < VisibleProxyFields.Length; i++)
                {
                    var fieldName = VisibleProxyFields[i];
                    var proxy = GetFieldValue<GameObject>(cabinPlayer, fieldName);
                    if (proxy == null || !IsUsableProxy(proxy)) continue;

                    bodyTransform = proxy.transform;
                    sourceName = fieldName;
                    return true;
                }
            }

            if (firstPersonController != null)
            {
                bodyTransform = firstPersonController.transform;
                sourceName = firstPersonController.gameObject.activeInHierarchy
                    ? "firstPersonController"
                    : "firstPersonController-inactive";
                return true;
            }

            if (playerController != null)
            {
                bodyTransform = playerController.transform;
                sourceName = "playerController";
                return true;
            }

            return false;
        }

        public static Camera ResolveCamera(FirstPersonController firstPersonController)
        {
            if (IsUsableCamera(firstPersonController != null ? firstPersonController.playerCamera : null))
            {
                return firstPersonController.playerCamera;
            }

            if (IsUsableCamera(Camera.main))
            {
                return Camera.main;
            }

            var cameras = UnityEngine.Object.FindObjectsOfType<Camera>();
            for (var i = 0; i < cameras.Length; i++)
            {
                if (IsUsableCamera(cameras[i]))
                {
                    return cameras[i];
                }
            }

            return cameras.Length > 0 ? cameras[0] : null;
        }

        private static bool IsUsableProxy(GameObject proxy)
        {
            if (proxy == null) return false;
            return proxy.activeInHierarchy;
        }

        private static bool IsUsableCamera(Camera camera)
        {
            return camera != null && camera.enabled && camera.gameObject.activeInHierarchy;
        }

        private static T GetFieldValue<T>(object target, string fieldName) where T : class
        {
            if (target == null || string.IsNullOrEmpty(fieldName)) return null;

            var field = target.GetType().GetField(fieldName, FieldFlags);
            if (field == null) return null;

            try
            {
                return field.GetValue(target) as T;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
