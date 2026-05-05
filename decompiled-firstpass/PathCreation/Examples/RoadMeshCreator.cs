using UnityEngine;

namespace PathCreation.Examples;

public class RoadMeshCreator : PathSceneTool
{
	[Header("Road settings")]
	public float roadWidth = 0.4f;

	[Range(0f, 0.5f)]
	public float thickness = 0.15f;

	public bool flattenSurface;

	[Header("Material settings")]
	public Material roadMaterial;

	public Material undersideMaterial;

	public float textureTiling = 1f;

	[SerializeField]
	[HideInInspector]
	private GameObject meshHolder;

	private MeshFilter meshFilter;

	private MeshRenderer meshRenderer;

	private Mesh mesh;

	protected override void PathUpdated()
	{
		if ((Object)(object)pathCreator != null)
		{
			AssignMeshComponents();
			AssignMaterials();
			CreateRoadMesh();
		}
	}

	private void CreateRoadMesh()
	{
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		Vector3[] array = new Vector3[base.path.NumPoints * 8];
		Vector2[] array2 = new Vector2[array.Length];
		Vector3[] array3 = new Vector3[array.Length];
		int num = 2 * (base.path.NumPoints - 1) + (base.path.isClosedLoop ? 2 : 0);
		int[] array4 = new int[num * 3];
		int[] array5 = new int[num * 3];
		int[] array6 = new int[num * 2 * 3];
		int num2 = 0;
		int num3 = 0;
		int[] array7 = new int[6] { 0, 8, 1, 1, 8, 9 };
		int[] array8 = new int[12]
		{
			4, 6, 14, 12, 4, 14, 5, 15, 7, 13,
			15, 5
		};
		bool flag = (int)base.path.space != 0 || !flattenSurface;
		for (int i = 0; i < base.path.NumPoints; i++)
		{
			Vector3 vector = (flag ? Vector3.Cross(base.path.GetTangent(i), base.path.GetNormal(i)) : base.path.up);
			Vector3 vector2 = (flag ? base.path.GetNormal(i) : Vector3.Cross(vector, base.path.GetTangent(i)));
			Vector3 vector3 = base.path.GetPoint(i) - vector2 * Mathf.Abs(roadWidth);
			Vector3 vector4 = base.path.GetPoint(i) + vector2 * Mathf.Abs(roadWidth);
			array[num2] = vector3;
			array[num2 + 1] = vector4;
			array[num2 + 2] = vector3 - vector * thickness;
			array[num2 + 3] = vector4 - vector * thickness;
			array[num2 + 4] = array[num2];
			array[num2 + 5] = array[num2 + 1];
			array[num2 + 6] = array[num2 + 2];
			array[num2 + 7] = array[num2 + 3];
			array2[num2] = new Vector2(0f, base.path.times[i]);
			array2[num2 + 1] = new Vector2(1f, base.path.times[i]);
			array3[num2] = vector;
			array3[num2 + 1] = vector;
			array3[num2 + 2] = -vector;
			array3[num2 + 3] = -vector;
			array3[num2 + 4] = -vector2;
			array3[num2 + 5] = vector2;
			array3[num2 + 6] = -vector2;
			array3[num2 + 7] = vector2;
			if (i < base.path.NumPoints - 1 || base.path.isClosedLoop)
			{
				for (int j = 0; j < array7.Length; j++)
				{
					array4[num3 + j] = (num2 + array7[j]) % array.Length;
					array5[num3 + j] = (num2 + array7[array7.Length - 1 - j] + 2) % array.Length;
				}
				for (int k = 0; k < array8.Length; k++)
				{
					array6[num3 * 2 + k] = (num2 + array8[k]) % array.Length;
				}
			}
			num2 += 8;
			num3 += 6;
		}
		mesh.Clear();
		mesh.vertices = array;
		mesh.uv = array2;
		mesh.normals = array3;
		mesh.subMeshCount = 3;
		mesh.SetTriangles(array4, 0);
		mesh.SetTriangles(array5, 1);
		mesh.SetTriangles(array6, 2);
		mesh.RecalculateBounds();
	}

	private void AssignMeshComponents()
	{
		if (meshHolder == null)
		{
			meshHolder = new GameObject("Road Mesh Holder");
		}
		meshHolder.transform.rotation = Quaternion.identity;
		meshHolder.transform.position = Vector3.zero;
		meshHolder.transform.localScale = Vector3.one;
		if (!meshHolder.gameObject.GetComponent<MeshFilter>())
		{
			meshHolder.gameObject.AddComponent<MeshFilter>();
		}
		if (!meshHolder.GetComponent<MeshRenderer>())
		{
			meshHolder.gameObject.AddComponent<MeshRenderer>();
		}
		meshRenderer = meshHolder.GetComponent<MeshRenderer>();
		meshFilter = meshHolder.GetComponent<MeshFilter>();
		if (mesh == null)
		{
			mesh = new Mesh();
		}
		meshFilter.sharedMesh = mesh;
	}

	private void AssignMaterials()
	{
		if (roadMaterial != null && undersideMaterial != null)
		{
			meshRenderer.sharedMaterials = new Material[3] { roadMaterial, undersideMaterial, undersideMaterial };
			meshRenderer.sharedMaterials[0].mainTextureScale = new Vector3(1f, textureTiling);
		}
	}
}
