using UnityEngine;

namespace PixelCrushers;

public static class MorePhysics2D
{
	public static bool queriesStartInColliders
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	public static int maxRaycastResults
	{
		get
		{
			return 0;
		}
		set
		{
		}
	}

	public static GameObject Raycast2DWithoutSelf(Transform source, Transform destination, LayerMask layerMask)
	{
		LogUsePhysics2DWarning();
		return null;
	}

	public static void LogUsePhysics2DWarning()
	{
		if (Debug.isDebugBuild)
		{
			Debug.LogWarning("To enable Physics2D support for a Pixel Crushers asset, add the Scripting Define Symbol 'USE_PHYSICS2D'.");
		}
	}
}
