using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class SoftBackdrop : MonoBehaviour
{
	protected const int ParticleCount = 32;

	protected const float Size = 10f;

	public Material speckMaterial;

	public Mesh speckMesh;

	public float speckScale = 0.5f;

	public float speckMaxSpeed = 1f;

	public Material sphereMaterial;

	public Mesh sphereMesh;

	protected Mesh invertedSphereMesh;

	protected Vector3[] positions;

	protected Vector3[] velocities;

	protected Matrix4x4[] transforms;

	public void Start()
	{
		Init();
	}

	public void OnValidate()
	{
		Init();
	}

	public void Init()
	{
		positions = new Vector3[32];
		velocities = new Vector3[32];
		transforms = new Matrix4x4[32];
		for (int i = 0; i < positions.Length; i++)
		{
			positions[i] = (new Vector3(Random.value, Random.value, Random.value) * 2f - new Vector3(1f, 1f, 1f)) * 10f;
			velocities[i] = Random.insideUnitSphere * Random.value * speckMaxSpeed;
		}
		if (!(sphereMesh != null))
		{
			return;
		}
		Vector3[] normals = sphereMesh.normals;
		for (int j = 0; j < normals.Length; j++)
		{
			normals[j] = -normals[j];
		}
		int[] triangles = sphereMesh.triangles;
		for (int k = 0; k < triangles.Length / 3; k++)
		{
			int num = triangles[k * 3 + 1];
			triangles[k * 3 + 1] = triangles[k * 3 + 2];
			triangles[k * 3 + 2] = num;
		}
		if (invertedSphereMesh != null)
		{
			if (Application.isPlaying)
			{
				Object.Destroy(invertedSphereMesh);
			}
			else
			{
				Object.DestroyImmediate(invertedSphereMesh);
			}
		}
		invertedSphereMesh = new Mesh
		{
			vertices = sphereMesh.vertices,
			normals = normals,
			triangles = triangles
		};
	}

	public void OnDestroy()
	{
		if (invertedSphereMesh != null)
		{
			if (Application.isPlaying)
			{
				Object.Destroy(invertedSphereMesh);
			}
			else
			{
				Object.DestroyImmediate(invertedSphereMesh);
			}
		}
	}

	public void Update()
	{
		if (positions == null)
		{
			return;
		}
		for (int i = 0; i < positions.Length; i++)
		{
			Vector3 vector = positions[i];
			Vector3 vector2 = velocities[i];
			vector += vector2 * Time.deltaTime;
			if (vector.magnitude > 10f)
			{
				vector = -vector.normalized * 10f;
			}
			positions[i] = vector;
		}
		for (int j = 0; j < positions.Length; j++)
		{
			Vector3 normalized = positions[j].normalized;
			float num = 1f - Mathf.Pow(Mathf.Clamp01(positions[j].magnitude / 10f), 4f);
			num *= speckScale;
			transforms[j] = Matrix4x4.TRS(base.transform.TransformPoint(normalized * 0.3f), Quaternion.LookRotation(normalized), base.transform.lossyScale * num);
		}
		if (invertedSphereMesh != null && sphereMaterial != null)
		{
			Graphics.DrawMesh(invertedSphereMesh, base.transform.localToWorldMatrix, sphereMaterial, 0, null, 0, null, castShadows: false, receiveShadows: false);
		}
		if (speckMesh != null && speckMaterial != null)
		{
			speckMaterial.enableInstancing = true;
			Graphics.DrawMeshInstanced(speckMesh, 0, speckMaterial, transforms, transforms.Length, null, ShadowCastingMode.Off, receiveShadows: false);
		}
	}

	public void OnDrawGizmos()
	{
		if (positions != null)
		{
			Gizmos.color = Color.red;
			for (int i = 0; i < positions.Length; i++)
			{
				Gizmos.DrawWireSphere(base.transform.TransformPoint(positions[i]), 0.5f);
			}
		}
	}
}
