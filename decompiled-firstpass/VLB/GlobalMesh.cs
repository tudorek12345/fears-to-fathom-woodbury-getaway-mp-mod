using UnityEngine;

namespace VLB;

public static class GlobalMesh
{
	private static Mesh ms_Mesh;

	private static bool ms_DoubleSided;

	public static Mesh Get()
	{
		bool requiresDoubleSidedMesh = Config.Instance.requiresDoubleSidedMesh;
		if (ms_Mesh == null || ms_DoubleSided != requiresDoubleSidedMesh)
		{
			Destroy();
			ms_Mesh = MeshGenerator.GenerateConeZ_Radius(1f, 1f, 1f, Config.Instance.sharedMeshSides, Config.Instance.sharedMeshSegments, cap: true, requiresDoubleSidedMesh);
			ms_Mesh.hideFlags = Consts.Internal.ProceduralObjectsHideFlags;
			ms_DoubleSided = requiresDoubleSidedMesh;
		}
		return ms_Mesh;
	}

	public static void Destroy()
	{
		if (ms_Mesh != null)
		{
			Object.DestroyImmediate(ms_Mesh);
			ms_Mesh = null;
		}
	}
}
