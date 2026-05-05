using Obi;
using UnityEngine;

[ExecuteInEditMode]
public class SpiralCurve : MonoBehaviour
{
	public float radius = 0.25f;

	public float radialStep = 0.8f;

	public float heightStep = 0.04f;

	public float points = 30f;

	public float rotationalMass = 1f;

	public float thickness = 1f;

	private void Awake()
	{
		Generate();
	}

	public void Generate()
	{
		ObiRopeBase component = GetComponent<ObiRopeBase>();
		if ((Object)(object)component == null)
		{
			return;
		}
		ObiActorBlueprint sourceBlueprint = ((ObiActor)component).sourceBlueprint;
		ObiRopeBlueprintBase val = (ObiRopeBlueprintBase)(object)((sourceBlueprint is ObiRopeBlueprintBase) ? sourceBlueprint : null);
		if (!((Object)(object)val == null))
		{
			val.path.Clear();
			float num = 0f;
			float num2 = 0f;
			for (int i = 0; (float)i < points; i++)
			{
				Vector3 vector = new Vector3(Mathf.Cos(num) * radius, num2, Mathf.Sin(num) * radius);
				Vector3 vector2 = new Vector3(0f - vector.z, heightStep, vector.x).normalized * 1.3333334f * Mathf.Tan(radialStep / 4f) * radius;
				val.path.AddControlPoint(vector, -vector2, vector2, Vector3.up, 1f, rotationalMass, thickness, 1, Color.white, "control point " + i);
				num += radialStep;
				num2 += heightStep;
			}
			val.path.FlushEvents();
		}
	}
}
