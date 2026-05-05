using Obi;
using UnityEngine;

public class SnakeController : MonoBehaviour
{
	public Transform headReferenceFrame;

	public float headSpeed = 20f;

	public float upSpeed = 40f;

	public float slitherSpeed = 10f;

	private ObiRope rope;

	private ObiSolver solver;

	private float[] traction;

	private Vector3[] surfaceNormal;

	private void Start()
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Expected O, but got Unknown
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Expected O, but got Unknown
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Expected O, but got Unknown
		rope = GetComponent<ObiRope>();
		solver = ((ObiActor)rope).solver;
		traction = new float[((ObiActor)rope).activeParticleCount];
		surfaceNormal = new Vector3[((ObiActor)rope).activeParticleCount];
		solver.OnBeginStep += new SolverStepCallback(ResetSurfaceInfo);
		solver.OnCollision += new CollisionCallback(AnalyzeContacts);
		solver.OnParticleCollision += new CollisionCallback(AnalyzeContacts);
	}

	private void OnDestroy()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Expected O, but got Unknown
		solver.OnBeginStep -= new SolverStepCallback(ResetSurfaceInfo);
		solver.OnCollision -= new CollisionCallback(AnalyzeContacts);
		solver.OnParticleCollision -= new CollisionCallback(AnalyzeContacts);
	}

	private void ResetSurfaceInfo(ObiSolver s, float stepTime)
	{
		for (int i = 0; i < traction.Length; i++)
		{
			traction[i] = 0f;
			surfaceNormal[i] = Vector3.zero;
		}
	}

	private void AnalyzeContacts(object sender, ObiCollisionEventArgs e)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < e.contacts.Count; i++)
		{
			Contact val = e.contacts.Data[i];
			if (val.distance < 0.005f)
			{
				int num = ((ObiNativeList<int>)(object)solver.simplices)[val.bodyA];
				ParticleInActor val2 = solver.particleToActor[num];
				if ((Object)(object)val2.actor == (Object)(object)rope)
				{
					traction[val2.indexInActor] = 1f;
					surfaceNormal[val2.indexInActor] += (Vector3)val.normal;
				}
			}
		}
	}

	public void Update()
	{
		if (Input.GetKey(KeyCode.J))
		{
			for (int i = 1; i < ((ObiActor)rope).activeParticleCount; i++)
			{
				int num = ((ObiActor)rope).solverIndices[i];
				int num2 = ((ObiActor)rope).solverIndices[i - 1];
				Vector4 vector = Vector3.ProjectOnPlane(((ObiNativeList<Vector4>)(object)solver.positions)[num2] - ((ObiNativeList<Vector4>)(object)solver.positions)[num], surfaceNormal[i]).normalized;
				ObiNativeVector4List velocities = solver.velocities;
				int num3 = num;
				((ObiNativeList<Vector4>)(object)velocities)[num3] = ((ObiNativeList<Vector4>)(object)velocities)[num3] + vector * traction[i] * slitherSpeed * Time.deltaTime;
			}
		}
		int num4 = ((ObiActor)rope).solverIndices[0];
		if (headReferenceFrame != null)
		{
			Vector3 zero = Vector3.zero;
			if (Input.GetKey(KeyCode.W))
			{
				zero += headReferenceFrame.forward * headSpeed;
			}
			if (Input.GetKey(KeyCode.A))
			{
				zero += -headReferenceFrame.right * headSpeed;
			}
			if (Input.GetKey(KeyCode.S))
			{
				zero += -headReferenceFrame.forward * headSpeed;
			}
			if (Input.GetKey(KeyCode.D))
			{
				zero += headReferenceFrame.right * headSpeed;
			}
			zero.y = 0f;
			ObiNativeVector4List velocities = solver.velocities;
			int num3 = num4;
			((ObiNativeList<Vector4>)(object)velocities)[num3] = ((ObiNativeList<Vector4>)(object)velocities)[num3] + (Vector4)zero * Time.deltaTime;
		}
		if (Input.GetKey(KeyCode.Space))
		{
			ObiNativeVector4List velocities = solver.velocities;
			int num3 = num4;
			((ObiNativeList<Vector4>)(object)velocities)[num3] = ((ObiNativeList<Vector4>)(object)velocities)[num3] + (Vector4)Vector3.up * Time.deltaTime * upSpeed;
		}
	}
}
