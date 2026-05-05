using UnityEngine;

namespace EPOOutline;

public static class CameraUtility
{
	public static int GetMSAA(Camera camera)
	{
		if (camera.targetTexture != null)
		{
			return camera.targetTexture.antiAliasing;
		}
		int result = Mathf.Max(GetRenderPipelineMSAA(), 1);
		if (!camera.allowMSAA)
		{
			result = 1;
		}
		if (camera.actualRenderingPath != RenderingPath.Forward && camera.actualRenderingPath != RenderingPath.VertexLit)
		{
			result = 1;
		}
		return result;
	}

	private static int GetRenderPipelineMSAA()
	{
		return QualitySettings.antiAliasing;
	}
}
