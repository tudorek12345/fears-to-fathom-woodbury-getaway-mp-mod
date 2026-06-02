using System;
using System.Reflection;
using UnityEngine;

namespace WoodburySpectatorSync.Coop
{
    internal static class CabinPlayerBodyPose
    {
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly string[] CabinVisibleProxyFields =
        {
            "sittingPlayerEating",
            "sittingPlayerOuijaBoardGame",
            "sittingPlayerBoardGame",
            "sittingBedPlayer",
            "sleepingBedPlayer",
            "truckPlayer"
        };

        private static readonly string[] PizzeriaVisibleProxyFields =
        {
            "playerSitdownPizzeria",
            "playerDrivingParent"
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
            if (TryResolveProxyFields(cabinPlayer, CabinVisibleProxyFields, "cabin", out bodyTransform, out sourceName))
            {
                return true;
            }

            var pizzeriaPlayer = playerController as PizzeriaPlayerController ?? UnityEngine.Object.FindObjectOfType<PizzeriaPlayerController>();
            if (TryResolveProxyFields(pizzeriaPlayer, PizzeriaVisibleProxyFields, "pizzeria", out bodyTransform, out sourceName))
            {
                return true;
            }

            if (TryResolveOfficeTablePose(out bodyTransform, out sourceName))
            {
                return true;
            }

            if (TryResolveToiletPose(out bodyTransform, out sourceName))
            {
                return true;
            }

            if (firstPersonController != null && firstPersonController.gameObject.activeInHierarchy)
            {
                bodyTransform = firstPersonController.transform;
                sourceName = "firstPersonController";
                return true;
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

        private static bool TryResolveProxyFields(
            object owner,
            string[] fieldNames,
            string sourcePrefix,
            out Transform bodyTransform,
            out string sourceName)
        {
            bodyTransform = null;
            sourceName = string.Empty;
            if (owner == null || fieldNames == null) return false;

            for (var i = 0; i < fieldNames.Length; i++)
            {
                var fieldName = fieldNames[i];
                var proxy = GetFieldValue<GameObject>(owner, fieldName);
                if (proxy == null || !IsUsableProxy(proxy)) continue;

                bodyTransform = proxy.transform;
                sourceName = sourcePrefix + "." + fieldName;
                return true;
            }

            return false;
        }

        private static bool TryResolveOfficeTablePose(out Transform bodyTransform, out string sourceName)
        {
            bodyTransform = null;
            sourceName = string.Empty;

            var tableManagers = UnityEngine.Object.FindObjectsOfType<TableManager>();
            for (var i = 0; i < tableManagers.Length; i++)
            {
                var table = tableManagers[i];
                if (table == null || !table.isSitting) continue;

                var tableCameraHolder = GetFieldValue<GameObject>(table, "tableCameraHolder");
                if (tableCameraHolder == null || !IsUsableProxy(tableCameraHolder)) continue;

                bodyTransform = tableCameraHolder.transform;
                sourceName = "office.tableCameraHolder";
                return true;
            }

            return false;
        }

        private static bool TryResolveToiletPose(out Transform bodyTransform, out string sourceName)
        {
            bodyTransform = null;
            sourceName = string.Empty;

            var toiletManagers = UnityEngine.Object.FindObjectsOfType<ToiletManager>();
            for (var i = 0; i < toiletManagers.Length; i++)
            {
                var toilet = toiletManagers[i];
                if (toilet == null) continue;

                var cameraParent = GetFieldValue<GameObject>(toilet, "sittingCameraParent");
                if (cameraParent == null || !IsUsableProxy(cameraParent)) continue;

                bodyTransform = cameraParent.transform;
                sourceName = "cabin.toiletCameraParent";
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
