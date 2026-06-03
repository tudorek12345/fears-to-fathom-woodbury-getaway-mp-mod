using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using WoodburySpectatorSync.Config;

namespace WoodburySpectatorSync.Coop
{
    internal static class SceneDiscoveryDump
    {
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        public static void LogIfEnabled(
            Settings settings,
            string side,
            Scene scene,
            ManualLogSource logger,
            Action<string> sessionLogWrite)
        {
            LogIfEnabled(settings, side, scene, logger, sessionLogWrite, manual: false);
        }

        public static void LogManualIfEnabled(
            Settings settings,
            string side,
            Scene scene,
            ManualLogSource logger,
            Action<string> sessionLogWrite)
        {
            LogIfEnabled(settings, side, scene, logger, sessionLogWrite, manual: true);
        }

        private static void LogIfEnabled(
            Settings settings,
            string side,
            Scene scene,
            ManualLogSource logger,
            Action<string> sessionLogWrite,
            bool manual)
        {
            if (settings == null ||
                settings.SceneDiscoveryDump == null ||
                !settings.SceneDiscoveryDump.Value ||
                !scene.IsValid() ||
                !scene.isLoaded)
            {
                return;
            }

            try
            {
                LogScene(side, scene, logger, sessionLogWrite, manual);
            }
            catch (Exception ex)
            {
                Log(Prefix("Error", manual) + "|role=" + Escape(side) +
                    "|scene=" + Escape(scene.name) +
                    "|errorType=" + Escape(ex.GetType().Name) +
                    "|message=" + Escape(ex.Message), logger, sessionLogWrite);
            }
        }

        private static void LogScene(string side, Scene scene, ManualLogSource logger, Action<string> sessionLogWrite, bool manual)
        {
            var components = FindManagerLikeComponents(scene);
            var fieldCount = 0;
            for (var i = 0; i < components.Count; i++)
            {
                fieldCount += GetSerializableFields(components[i].GetType()).Count;
            }

            Log(Prefix("Begin", manual) + "|role=" + Escape(side) +
                "|scene=" + Escape(scene.name) +
                "|sceneHandle=" + scene.handle.ToString(CultureInfo.InvariantCulture) +
                "|components=" + components.Count.ToString(CultureInfo.InvariantCulture) +
                "|fields=" + fieldCount.ToString(CultureInfo.InvariantCulture), logger, sessionLogWrite);

            for (var componentIndex = 0; componentIndex < components.Count; componentIndex++)
            {
                var component = components[componentIndex];
                if (component == null || component.gameObject == null) continue;

                var componentType = component.GetType();
                var path = NetPath.GetPath(component.transform);
                var fields = GetSerializableFields(componentType);
                for (var fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
                {
                    var field = fields[fieldIndex];
                    var fieldValue = ReadFieldValue(component, field);
                    Log(Prefix("Field", manual) + "|role=" + Escape(side) +
                        "|scene=" + Escape(scene.name) +
                        "|componentIndex=" + componentIndex.ToString(CultureInfo.InvariantCulture) +
                        "|componentType=" + Escape(componentType.FullName ?? componentType.Name) +
                        "|componentPath=" + Escape(path) +
                        "|componentActive=" + BoolText(component.gameObject.activeSelf) +
                        "|componentActiveInHierarchy=" + BoolText(component.gameObject.activeInHierarchy) +
                        "|componentEnabled=" + BoolText(component.enabled) +
                        "|fieldIndex=" + fieldIndex.ToString(CultureInfo.InvariantCulture) +
                        "|fieldDeclaringType=" + Escape(field.DeclaringType != null ? field.DeclaringType.FullName : string.Empty) +
                        "|fieldName=" + Escape(field.Name) +
                        "|fieldType=" + Escape(GetTypeName(field.FieldType)) +
                        "|fieldValue=" + Escape(fieldValue), logger, sessionLogWrite);
                }
            }

            Log(Prefix("End", manual) + "|role=" + Escape(side) +
                "|scene=" + Escape(scene.name) +
                "|components=" + components.Count.ToString(CultureInfo.InvariantCulture) +
                "|fields=" + fieldCount.ToString(CultureInfo.InvariantCulture), logger, sessionLogWrite);

            LogSceneSummary(side, scene, logger, sessionLogWrite, manual);
        }

        private static string Prefix(string suffix, bool manual)
        {
            return manual ? "SceneDiscoveryDumpManual" + suffix : "SceneDiscoveryDump" + suffix;
        }

        private static void LogSceneSummary(string side, Scene scene, ManualLogSource logger, Action<string> sessionLogWrite, bool manual)
        {
            if (scene.name == null ||
                scene.name.IndexOf("Pizzeria", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            try
            {
                var manager = UnityEngine.Object.FindObjectOfType<PizzeriaGameManager>();
                var player = UnityEngine.Object.FindObjectOfType<PizzeriaPlayerController>();
                var driving = manager != null ? ReadField<MikeDrivingInPizzeriaScene>(manager, "mikeDriving") : null;
                if (driving == null) driving = UnityEngine.Object.FindObjectOfType<MikeDrivingInPizzeriaScene>();
                var mike = manager != null && manager.mikePizzeria != null
                    ? manager.mikePizzeria
                    : UnityEngine.Object.FindObjectOfType<MikePizzeria>();

                Log(Prefix("Summary", manual) +
                    "|role=" + Escape(side) +
                    "|scene=" + Escape(scene.name) +
                    "|managerState=" + Escape(manager != null ? manager.currentPlayerState.ToString() : "-") +
                    "|playerSitting=" + BoolText(player != null && player.playerSitting) +
                    "|playerSitdown=" + BoolText(IsActive(player != null ? player.playerSitdownPizzeria : null)) +
                    "|drivingParent=" + BoolText(IsActive(player != null ? player.playerDrivingParent : null)) +
                    "|drivingCam=" + BoolText(IsActive(player != null ? player.playerDrivingCam : null)) +
                    "|pizzaOnTable=" + BoolText(IsActive(player != null ? player.pizzaOnTable : null)) +
                    "|mikeState=" + Escape(mike != null ? mike.state.ToString() : "-") +
                    "|mikeActive=" + BoolText(mike != null && mike.gameObject.activeSelf) +
                    "|mikeVisibleRenderers=" + CountVisibleRenderers(mike != null ? mike.gameObject : null).ToString(CultureInfo.InvariantCulture) +
                    "|mikeMoving=" + BoolText(mike != null && mike.moving) +
                    "|mikePizzaBox=" + BoolText(IsActive(mike != null ? mike.pizzaBox : null)) +
                    "|mikePizzaSlice=" + BoolText(IsActive(mike != null ? mike.pizzaSlice : null)) +
                    "|drivingActive=" + BoolText(driving != null && driving.gameObject.activeInHierarchy) +
                    "|drivingSpeed=" + FloatText(ReadField<float>(driving, "speed")) +
                    "|drivingSlowDown=" + BoolText(ReadField<bool>(driving, "startSlowDown")) +
                    "|truckDoorSwitched=" + BoolText(manager != null && ReadField<SwitchObjectLayer>(manager, "truckDoorLayer") != null && ReadField<SwitchObjectLayer>(manager, "truckDoorLayer").switched) +
                    "|keysUI=" + BoolText(IsActive(manager != null ? manager.keysUI : null)),
                    logger,
                    sessionLogWrite);
            }
            catch (Exception ex)
            {
                Log(Prefix("SummaryError", manual) +
                    "|role=" + Escape(side) +
                    "|scene=" + Escape(scene.name) +
                    "|errorType=" + Escape(ex.GetType().Name) +
                    "|message=" + Escape(ex.Message), logger, sessionLogWrite);
            }
        }

        private static List<MonoBehaviour> FindManagerLikeComponents(Scene scene)
        {
            var components = new List<MonoBehaviour>();
            var behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null || behaviour.gameObject == null) continue;

                var objectScene = behaviour.gameObject.scene;
                if (!objectScene.IsValid() || !objectScene.isLoaded || objectScene.handle != scene.handle) continue;

                var typeName = behaviour.GetType().Name;
                if (!IsManagerLikeTypeName(typeName)) continue;

                components.Add(behaviour);
            }

            components.Sort((left, right) =>
            {
                var typeCompare = string.CompareOrdinal(
                    left != null ? left.GetType().FullName : string.Empty,
                    right != null ? right.GetType().FullName : string.Empty);
                if (typeCompare != 0) return typeCompare;

                var pathCompare = string.CompareOrdinal(
                    left != null ? NetPath.GetPath(left.transform) : string.Empty,
                    right != null ? NetPath.GetPath(right.transform) : string.Empty);
                if (pathCompare != 0) return pathCompare;

                return (left != null ? left.GetInstanceID() : 0).CompareTo(right != null ? right.GetInstanceID() : 0);
            });

            return components;
        }

        private static bool IsManagerLikeTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return false;
            return typeName.IndexOf("Manager", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   typeName.IndexOf("GameManager", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   typeName.IndexOf("Controller", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static List<FieldInfo> GetSerializableFields(Type type)
        {
            var fields = new List<FieldInfo>();
            var current = type;
            while (current != null &&
                   current != typeof(MonoBehaviour) &&
                   current != typeof(Behaviour) &&
                   current != typeof(Component) &&
                   current != typeof(UnityEngine.Object) &&
                   current != typeof(object))
            {
                var declared = current.GetFields(FieldFlags);
                for (var i = 0; i < declared.Length; i++)
                {
                    if (declared[i] == null || declared[i].IsStatic) continue;
                    fields.Add(declared[i]);
                }

                current = current.BaseType;
            }

            fields.Sort((left, right) =>
            {
                var typeCompare = string.CompareOrdinal(
                    left.DeclaringType != null ? left.DeclaringType.FullName : string.Empty,
                    right.DeclaringType != null ? right.DeclaringType.FullName : string.Empty);
                if (typeCompare != 0) return typeCompare;
                return string.CompareOrdinal(left.Name, right.Name);
            });

            return fields;
        }

        private static string ReadFieldValue(object target, FieldInfo field)
        {
            try
            {
                return FormatValue(field.GetValue(target), field.FieldType);
            }
            catch (Exception ex)
            {
                return "<read-error:" + ex.GetType().Name + ":" + ex.Message + ">";
            }
        }

        private static string FormatValue(object value, Type declaredType)
        {
            if (value == null) return "null";

            var type = value.GetType();
            if (type == typeof(bool)) return BoolText((bool)value);
            if (type.IsEnum) return Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture) + ":" + value;
            if (type == typeof(string)) return "\"" + value + "\"";
            if (type == typeof(char)) return "'" + value + "'";
            if (type == typeof(byte) || type == typeof(sbyte) ||
                type == typeof(short) || type == typeof(ushort) ||
                type == typeof(int) || type == typeof(uint) ||
                type == typeof(long) || type == typeof(ulong))
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            }

            if (type == typeof(float)) return ((float)value).ToString("R", CultureInfo.InvariantCulture);
            if (type == typeof(double)) return ((double)value).ToString("R", CultureInfo.InvariantCulture);
            if (type == typeof(decimal)) return ((decimal)value).ToString(CultureInfo.InvariantCulture);
            if (type == typeof(Vector2)) return FormatVector2((Vector2)value);
            if (type == typeof(Vector3)) return FormatVector3((Vector3)value);
            if (type == typeof(Vector4)) return FormatVector4((Vector4)value);
            if (type == typeof(Quaternion)) return FormatQuaternion((Quaternion)value);
            if (type == typeof(Color)) return FormatColor((Color)value);
            if (type == typeof(Color32)) return FormatColor32((Color32)value);
            if (type == typeof(Rect)) return FormatRect((Rect)value);

            var unityObject = value as UnityEngine.Object;
            if (unityObject != null)
            {
                return FormatUnityObject(unityObject);
            }

            var enumerable = value as IEnumerable;
            if (!(value is string) && enumerable != null)
            {
                return FormatEnumerable(enumerable);
            }

            return value.ToString();
        }

        private static string FormatEnumerable(IEnumerable enumerable)
        {
            var sb = new StringBuilder();
            var count = 0;
            sb.Append("[");
            foreach (var item in enumerable)
            {
                if (count > 0) sb.Append(";");
                sb.Append(count.ToString(CultureInfo.InvariantCulture));
                sb.Append("=");
                sb.Append(FormatCollectionItem(item));
                count++;
            }

            sb.Append("]");
            sb.Append("(count=");
            sb.Append(count.ToString(CultureInfo.InvariantCulture));
            sb.Append(")");
            return sb.ToString();
        }

        private static string FormatCollectionItem(object item)
        {
            if (item == null) return "null";

            var unityObject = item as UnityEngine.Object;
            if (unityObject != null)
            {
                return FormatUnityObject(unityObject);
            }

            var type = item.GetType();
            if (type == typeof(bool)) return BoolText((bool)item);
            if (type.IsEnum) return Convert.ToInt64(item, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture) + ":" + item;
            if (type == typeof(string)) return "\"" + item + "\"";
            if (type == typeof(Vector3)) return FormatVector3((Vector3)item);
            if (type == typeof(Quaternion)) return FormatQuaternion((Quaternion)item);
            if (type.IsPrimitive) return Convert.ToString(item, CultureInfo.InvariantCulture);
            return item.ToString();
        }

        private static string FormatUnityObject(UnityEngine.Object unityObject)
        {
            if (unityObject == null) return "null";

            var gameObject = unityObject as GameObject;
            if (gameObject != null)
            {
                return "GameObject(type=" + GetTypeName(gameObject.GetType()) +
                       ",name=" + gameObject.name +
                       ",path=" + NetPath.GetPath(gameObject.transform) +
                       ",active=" + BoolText(gameObject.activeSelf) +
                       ",activeInHierarchy=" + BoolText(gameObject.activeInHierarchy) + ")";
            }

            var component = unityObject as Component;
            if (component != null && component.gameObject != null)
            {
                return "Component(type=" + GetTypeName(component.GetType()) +
                       ",name=" + component.name +
                       ",path=" + NetPath.GetPath(component.transform) +
                       ",active=" + BoolText(component.gameObject.activeSelf) +
                       ",activeInHierarchy=" + BoolText(component.gameObject.activeInHierarchy) +
                       ",enabled=" + ComponentEnabledText(component) + ")";
            }

            return "UnityObject(type=" + GetTypeName(unityObject.GetType()) +
                   ",name=" + unityObject.name +
                   ",id=" + unityObject.GetInstanceID().ToString(CultureInfo.InvariantCulture) + ")";
        }

        private static string ComponentEnabledText(Component component)
        {
            var behaviour = component as Behaviour;
            if (behaviour != null) return BoolText(behaviour.enabled);
            var renderer = component as Renderer;
            if (renderer != null) return BoolText(renderer.enabled);
            var collider = component as Collider;
            if (collider != null) return BoolText(collider.enabled);
            return "n/a";
        }

        private static string FormatVector2(Vector2 value)
        {
            return "(" + FloatText(value.x) + "," + FloatText(value.y) + ")";
        }

        private static string FormatVector3(Vector3 value)
        {
            return "(" + FloatText(value.x) + "," + FloatText(value.y) + "," + FloatText(value.z) + ")";
        }

        private static string FormatVector4(Vector4 value)
        {
            return "(" + FloatText(value.x) + "," + FloatText(value.y) + "," + FloatText(value.z) + "," + FloatText(value.w) + ")";
        }

        private static string FormatQuaternion(Quaternion value)
        {
            return "(" + FloatText(value.x) + "," + FloatText(value.y) + "," + FloatText(value.z) + "," + FloatText(value.w) + ")";
        }

        private static string FormatColor(Color value)
        {
            return "(" + FloatText(value.r) + "," + FloatText(value.g) + "," + FloatText(value.b) + "," + FloatText(value.a) + ")";
        }

        private static string FormatColor32(Color32 value)
        {
            return "(" + value.r.ToString(CultureInfo.InvariantCulture) + "," +
                   value.g.ToString(CultureInfo.InvariantCulture) + "," +
                   value.b.ToString(CultureInfo.InvariantCulture) + "," +
                   value.a.ToString(CultureInfo.InvariantCulture) + ")";
        }

        private static string FormatRect(Rect value)
        {
            return "(" + FloatText(value.x) + "," + FloatText(value.y) + "," + FloatText(value.width) + "," + FloatText(value.height) + ")";
        }

        private static string FloatText(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string BoolText(bool value)
        {
            return value ? "1" : "0";
        }

        private static T ReadField<T>(object target, string fieldName)
        {
            if (target == null || string.IsNullOrEmpty(fieldName)) return default(T);
            try
            {
                var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null) return default(T);
                var value = field.GetValue(target);
                if (value is T typed) return typed;
            }
            catch (Exception)
            {
            }
            return default(T);
        }

        private static bool IsActive(UnityEngine.Object target)
        {
            var go = target as GameObject;
            if (go != null) return go.activeSelf;
            var component = target as Component;
            return component != null && component.gameObject != null && component.gameObject.activeSelf;
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

        private static string GetTypeName(Type type)
        {
            if (type == null) return string.Empty;
            return type.FullName ?? type.Name;
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            return value
                .Replace("%", "%25")
                .Replace("|", "%7C")
                .Replace("\r", "%0D")
                .Replace("\n", "%0A");
        }

        private static void Log(string message, ManualLogSource logger, Action<string> sessionLogWrite)
        {
            logger?.LogInfo(message);
            sessionLogWrite?.Invoke(message);
        }
    }
}
