using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UI.ThreeDimensional;

public static class UIObject3DUtilities
{
	private static Dictionary<UIObject3D, Vector3> targetContainers = new Dictionary<UIObject3D, Vector3>();

	public static Vector3 NormalizeRotation(Vector3 rotation)
	{
		return new Vector3(NormalizeAngle(rotation.x), NormalizeAngle(rotation.y), NormalizeAngle(rotation.z));
	}

	public static float NormalizeAngle(float value)
	{
		value %= 360f;
		if (value < 0f)
		{
			value += 360f;
		}
		return value;
	}

	internal static void RegisterTargetContainerPosition(UIObject3D uiObject3D, Vector3 position)
	{
		if (!targetContainers.ContainsKey(uiObject3D))
		{
			targetContainers.Add(uiObject3D, position);
		}
	}

	internal static Vector3 GetTargetContainerPosition(UIObject3D uiObject3d)
	{
		if (targetContainers.ContainsKey(uiObject3d))
		{
			return targetContainers[uiObject3d];
		}
		return GetNextFreeTargetContainerPosition();
	}

	internal static Vector3 GetNextFreeTargetContainerPosition()
	{
		if (!targetContainers.Any())
		{
			return Vector3.zero;
		}
		return new Vector3(targetContainers.Max((KeyValuePair<UIObject3D, Vector3> v) => v.Value.x) + 250f, 0f, 0f);
	}

	internal static void UnRegisterTargetContainer(UIObject3D uiObject3D)
	{
		targetContainers.Remove(uiObject3D);
	}
}
