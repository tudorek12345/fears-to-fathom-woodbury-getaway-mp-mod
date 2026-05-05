using Obi;
using UnityEngine;

public class RopeBetweenTwoPoints : MonoBehaviour
{
	public Transform start;

	public Transform end;

	public ObiSolver solver;

	private void Start()
	{
		Generate();
	}

	private void Generate()
	{
		if (start != null && end != null)
		{
			base.transform.position = (start.position + end.position) / 2f;
			base.transform.rotation = Quaternion.FromToRotation(Vector3.right, end.position - start.position);
			Vector3 vector = base.transform.InverseTransformPoint(start.position);
			Vector3 vector2 = base.transform.InverseTransformPoint(end.position);
			Vector3 normalized = (vector2 - vector).normalized;
			ObiRopeBlueprint val = ScriptableObject.CreateInstance<ObiRopeBlueprint>();
			int num = ObiUtils.MakeFilter(65535, 0);
			((ObiRopeBlueprintBase)val).path.AddControlPoint(vector, -normalized, normalized, Vector3.up, 0.1f, 0.1f, 1f, num, Color.white, "start");
			((ObiRopeBlueprintBase)val).path.AddControlPoint(vector2, -normalized, normalized, Vector3.up, 0.1f, 0.1f, 1f, num, Color.white, "end");
			((ObiRopeBlueprintBase)val).path.FlushEvents();
			((ObiActorBlueprint)val).GenerateImmediate();
			ObiRope obj = base.gameObject.AddComponent<ObiRope>();
			ObiRopeExtrudedRenderer obj2 = base.gameObject.AddComponent<ObiRopeExtrudedRenderer>();
			ObiParticleAttachment val2 = base.gameObject.AddComponent<ObiParticleAttachment>();
			ObiParticleAttachment val3 = base.gameObject.AddComponent<ObiParticleAttachment>();
			obj2.section = Resources.Load<ObiRopeSection>("DefaultRopeSection");
			obj.ropeBlueprint = val;
			val2.target = start;
			val3.target = end;
			val2.particleGroup = ((ObiActorBlueprint)val).groups[0];
			val3.particleGroup = ((ObiActorBlueprint)val).groups[1];
			base.transform.SetParent(((Component)(object)solver).transform);
		}
	}
}
