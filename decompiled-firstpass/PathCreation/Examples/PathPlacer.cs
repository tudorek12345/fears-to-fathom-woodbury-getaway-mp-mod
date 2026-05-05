using UnityEngine;

namespace PathCreation.Examples;

[ExecuteInEditMode]
public class PathPlacer : PathSceneTool
{
	public GameObject prefab;

	public GameObject holder;

	public float spacing = 3f;

	private const float minSpacing = 0.1f;

	private void Generate()
	{
		if ((Object)(object)pathCreator != null && prefab != null && holder != null)
		{
			DestroyObjects();
			VertexPath val = pathCreator.path;
			spacing = Mathf.Max(0.1f, spacing);
			for (float num = 0f; num < val.length; num += spacing)
			{
				Vector3 pointAtDistance = val.GetPointAtDistance(num, (EndOfPathInstruction)0);
				Quaternion rotationAtDistance = val.GetRotationAtDistance(num, (EndOfPathInstruction)0);
				Object.Instantiate(prefab, pointAtDistance, rotationAtDistance, holder.transform);
			}
		}
	}

	private void DestroyObjects()
	{
		for (int num = holder.transform.childCount - 1; num >= 0; num--)
		{
			Object.DestroyImmediate(holder.transform.GetChild(num).gameObject, allowDestroyingAssets: false);
		}
	}

	protected override void PathUpdated()
	{
		if ((Object)(object)pathCreator != null)
		{
			Generate();
		}
	}
}
