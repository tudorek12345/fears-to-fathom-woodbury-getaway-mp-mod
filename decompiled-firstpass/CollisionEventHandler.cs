using Obi;
using UnityEngine;

[RequireComponent(typeof(ObiSolver))]
public class CollisionEventHandler : MonoBehaviour
{
	private ObiSolver solver;

	public int contactCount;

	private ObiCollisionEventArgs frame;

	private void Awake()
	{
		solver = GetComponent<ObiSolver>();
	}

	private void OnEnable()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		solver.OnParticleCollision += new CollisionCallback(Solver_OnCollision);
	}

	private void OnDisable()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		solver.OnParticleCollision -= new CollisionCallback(Solver_OnCollision);
	}

	private void Solver_OnCollision(object sender, ObiCollisionEventArgs e)
	{
		frame = e;
	}

	private void OnDrawGizmos()
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)solver == null || frame == null || frame.contacts == null)
		{
			return;
		}
		Gizmos.matrix = ((Component)(object)solver).transform.localToWorldMatrix;
		contactCount = frame.contacts.Count;
		int num = default(int);
		for (int i = 0; i < frame.contacts.Count; i++)
		{
			Contact val = frame.contacts.Data[i];
			Gizmos.color = ((val.distance <= 0f) ? Color.red : Color.green);
			Vector3 zero = Vector3.zero;
			SimplexCounts simplexCounts = solver.simplexCounts;
			int simplexStartAndSize = ((SimplexCounts)(ref simplexCounts)).GetSimplexStartAndSize(val.bodyB, ref num);
			float num2 = 0f;
			for (int j = 0; j < num; j++)
			{
				zero += (Vector3)((ObiNativeList<Vector4>)(object)solver.positions)[((ObiNativeList<int>)(object)solver.simplices)[simplexStartAndSize + j]] * val.pointB[j];
				num2 += ((ObiNativeList<Vector4>)(object)solver.principalRadii)[((ObiNativeList<int>)(object)solver.simplices)[simplexStartAndSize + j]].x * val.pointB[j];
			}
			Vector3 vector = val.normal;
			Gizmos.DrawSphere(zero + vector * num2, 0.01f);
			Gizmos.DrawRay(zero + vector * num2, vector.normalized * val.distance);
		}
	}
}
