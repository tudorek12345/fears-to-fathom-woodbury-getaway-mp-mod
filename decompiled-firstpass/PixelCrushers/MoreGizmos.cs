using UnityEngine;

namespace PixelCrushers;

public static class MoreGizmos
{
	public static void DrawArrow(Vector3 from, Vector3 direction, float arrowheadLength = 0.2f, float arrowheadAngle = 30f)
	{
		if (!Mathf.Approximately(direction.magnitude, 0f))
		{
			Vector3 vector = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, 180f + arrowheadAngle, 0f) * Vector3.forward;
			Vector3 vector2 = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, 180f - arrowheadAngle, 0f) * Vector3.forward;
			Gizmos.DrawRay(from, direction);
			Gizmos.DrawRay(from + direction, vector * arrowheadLength);
			Gizmos.DrawRay(from + direction, vector2 * arrowheadLength);
		}
	}
}
