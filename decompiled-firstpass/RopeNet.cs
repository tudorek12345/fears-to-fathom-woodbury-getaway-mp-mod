using Obi;
using UnityEngine;

public class RopeNet : MonoBehaviour
{
	public Material material;

	public Vector2Int resolution = new Vector2Int(5, 5);

	public Vector2 size = new Vector2(0.5f, 0.5f);

	public float nodeSize = 0.2f;

	private void Awake()
	{
		GameObject obj = new GameObject("solver", typeof(ObiSolver), typeof(ObiFixedUpdater));
		ObiSolver component = obj.GetComponent<ObiSolver>();
		ObiFixedUpdater component2 = obj.GetComponent<ObiFixedUpdater>();
		component2.substeps = 2;
		component.particleCollisionConstraintParameters.enabled = false;
		component.distanceConstraintParameters.iterations = 8;
		component.pinConstraintParameters.iterations = 4;
		component.parameters.sleepThreshold = 0.001f;
		component.PushSolverParameters();
		((ObiUpdater)component2).solvers.Add(component);
		CreateNet(component);
	}

	private void CreateNet(ObiSolver solver)
	{
		ObiCollider[,] array = new ObiCollider[resolution.x + 1, resolution.y + 1];
		for (int i = 0; i <= resolution.x; i++)
		{
			for (int j = 0; j <= resolution.y; j++)
			{
				GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
				gameObject.AddComponent<Rigidbody>();
				gameObject.transform.position = new Vector3(i, j, 0f) * size;
				gameObject.transform.localScale = new Vector3(nodeSize, nodeSize, nodeSize);
				array[i, j] = gameObject.AddComponent<ObiCollider>();
				((ObiColliderBase)array[i, j]).Filter = 1;
			}
		}
		((Component)(object)array[0, resolution.y]).GetComponent<Rigidbody>().isKinematic = true;
		((Component)(object)array[resolution.x, resolution.y]).GetComponent<Rigidbody>().isKinematic = true;
		for (int k = 0; k <= resolution.x; k++)
		{
			for (int l = 0; l <= resolution.y; l++)
			{
				Vector3 vector = new Vector3(k, l, 0f) * size;
				if (k < resolution.x)
				{
					Vector3 vector2 = new Vector3(nodeSize * 0.5f, 0f, 0f);
					ObiRope val = CreateRope(vector + vector2, vector + new Vector3(size.x, 0f, 0f) - vector2);
					((Component)(object)val).transform.parent = ((Component)(object)solver).transform;
					PinRope(val, array[k, l], array[k + 1, l], new Vector3(0.5f, 0f, 0f), -new Vector3(0.5f, 0f, 0f));
				}
				if (l < resolution.y)
				{
					Vector3 vector3 = new Vector3(0f, nodeSize * 0.5f, 0f);
					ObiRope val2 = CreateRope(vector + vector3, vector + new Vector3(0f, size.y, 0f) - vector3);
					((Component)(object)val2).transform.parent = ((Component)(object)solver).transform;
					PinRope(val2, array[k, l], array[k, l + 1], new Vector3(0f, 0.5f, 0f), -new Vector3(0f, 0.5f, 0f));
				}
			}
		}
	}

	private void PinRope(ObiRope rope, ObiCollider bodyA, ObiCollider bodyB, Vector3 offsetA, Vector3 offsetB)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		ObiConstraints<ObiPinConstraintsBatch> obj = ((ObiActor)rope).GetConstraintsByType((ConstraintType)8) as ObiConstraints<ObiPinConstraintsBatch>;
		obj.Clear();
		ObiPinConstraintsBatch val = new ObiPinConstraintsBatch((ObiPinConstraintsData)null);
		val.AddConstraint(((ObiActor)rope).solverIndices[0], (ObiColliderBase)(object)bodyA, offsetA, Quaternion.identity, 0f, 999f, float.PositiveInfinity);
		val.AddConstraint(((ObiActor)rope).solverIndices[((ObiActor)rope).activeParticleCount - 1], (ObiColliderBase)(object)bodyB, offsetB, Quaternion.identity, 0f, 999f, float.PositiveInfinity);
		((ObiConstraintsBatch)val).activeConstraintCount = 2;
		obj.AddBatch(val);
	}

	private ObiRope CreateRope(Vector3 pointA, Vector3 pointB)
	{
		GameObject obj = new GameObject("solver", typeof(ObiRope), typeof(ObiRopeLineRenderer));
		ObiRope component = obj.GetComponent<ObiRope>();
		ObiRopeLineRenderer component2 = obj.GetComponent<ObiRopeLineRenderer>();
		((Component)(object)component).GetComponent<MeshRenderer>().material = material;
		((Component)(object)component).GetComponent<ObiPathSmoother>().decimation = 0.1f;
		component2.uvScale = new Vector2(1f, 5f);
		ObiRopeBlueprint val = ScriptableObject.CreateInstance<ObiRopeBlueprint>();
		((ObiRopeBlueprintBase)val).resolution = 0.15f;
		((ObiRopeBlueprintBase)val).thickness = 0.02f;
		val.pooledParticles = 0;
		pointA = ((Component)(object)component).transform.InverseTransformPoint(pointA);
		pointB = ((Component)(object)component).transform.InverseTransformPoint(pointB);
		Vector3 vector = (pointB - pointA) * 0.25f;
		((ObiRopeBlueprintBase)val).path.Clear();
		((ObiRopeBlueprintBase)val).path.AddControlPoint(pointA, -vector, vector, Vector3.up, 0.1f, 0.1f, 1f, 1, Color.white, "A");
		((ObiRopeBlueprintBase)val).path.AddControlPoint(pointB, -vector, vector, Vector3.up, 0.1f, 0.1f, 1f, 1, Color.white, "B");
		((ObiRopeBlueprintBase)val).path.FlushEvents();
		component.ropeBlueprint = val;
		return component;
	}
}
