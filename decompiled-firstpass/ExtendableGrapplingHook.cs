using System.Collections;
using Obi;
using UnityEngine;

public class ExtendableGrapplingHook : MonoBehaviour
{
	public ObiSolver solver;

	public ObiCollider character;

	public Material material;

	public ObiRopeSection section;

	[Range(0f, 1f)]
	public float hookResolution = 0.5f;

	public float hookExtendRetractSpeed = 2f;

	public float hookShootSpeed = 30f;

	public int particlePoolSize = 100;

	private ObiRope rope;

	private ObiRopeBlueprint blueprint;

	private ObiRopeExtrudedRenderer ropeRenderer;

	private ObiRopeCursor cursor;

	private RaycastHit hookAttachment;

	private void Awake()
	{
		rope = base.gameObject.AddComponent<ObiRope>();
		ropeRenderer = base.gameObject.AddComponent<ObiRopeExtrudedRenderer>();
		ropeRenderer.section = section;
		ropeRenderer.uvScale = new Vector2(1f, 4f);
		ropeRenderer.normalizeV = false;
		ropeRenderer.uvAnchor = 1f;
		((Component)(object)rope).GetComponent<MeshRenderer>().material = material;
		blueprint = ScriptableObject.CreateInstance<ObiRopeBlueprint>();
		((ObiRopeBlueprintBase)blueprint).resolution = 0.5f;
		blueprint.pooledParticles = particlePoolSize;
		rope.maxBending = 0.02f;
		cursor = ((Component)(object)rope).gameObject.AddComponent<ObiRopeCursor>();
		cursor.cursorMu = 0f;
		cursor.direction = true;
	}

	private void OnDestroy()
	{
		Object.DestroyImmediate((Object)(object)blueprint);
	}

	private void LaunchHook()
	{
		Vector3 mousePosition = Input.mousePosition;
		mousePosition.z = base.transform.position.z - Camera.main.transform.position.z;
		Vector3 vector = Camera.main.ScreenToWorldPoint(mousePosition);
		if (Physics.Raycast(new Ray(base.transform.position, vector - base.transform.position), out hookAttachment))
		{
			StartCoroutine(AttachHook());
		}
	}

	private IEnumerator AttachHook()
	{
		yield return null;
		ObiConstraints<ObiPinConstraintsBatch> pinConstraints = ((ObiActor)rope).GetConstraintsByType((ConstraintType)8) as ObiConstraints<ObiPinConstraintsBatch>;
		pinConstraints.Clear();
		Vector3 vector = ((Component)(object)rope).transform.InverseTransformPoint(hookAttachment.point);
		int num = ObiUtils.MakeFilter(65535, 0);
		((ObiRopeBlueprintBase)blueprint).path.Clear();
		((ObiRopeBlueprintBase)blueprint).path.AddControlPoint(Vector3.zero, Vector3.zero, Vector3.zero, Vector3.up, 0.1f, 0.1f, 1f, num, Color.white, "Hook start");
		((ObiRopeBlueprintBase)blueprint).path.AddControlPoint(vector.normalized * 0.5f, Vector3.zero, Vector3.zero, Vector3.up, 0.1f, 0.1f, 1f, num, Color.white, "Hook end");
		((ObiRopeBlueprintBase)blueprint).path.FlushEvents();
		yield return ((ObiActorBlueprint)blueprint).Generate();
		rope.ropeBlueprint = blueprint;
		yield return null;
		((Component)(object)rope).GetComponent<MeshRenderer>().enabled = true;
		for (int i = 0; i < ((ObiActor)rope).activeParticleCount; i++)
		{
			((ObiNativeList<float>)(object)solver.invMasses)[((ObiActor)rope).solverIndices[i]] = 0f;
		}
		float currentLength = 0f;
		float magnitude;
		while (true)
		{
			Vector3 vector2 = ((Component)(object)solver).transform.InverseTransformPoint(((Component)(object)rope).transform.position);
			Vector3 vector3 = hookAttachment.point - vector2;
			magnitude = vector3.magnitude;
			vector3.Normalize();
			currentLength += hookShootSpeed * Time.deltaTime;
			if (currentLength >= magnitude)
			{
				break;
			}
			cursor.ChangeLength(Mathf.Min(magnitude, currentLength));
			float num2 = 0f;
			for (int j = 0; j < ((ObiRopeBase)rope).elements.Count; j++)
			{
				((ObiNativeList<Vector4>)(object)solver.positions)[((ObiRopeBase)rope).elements[j].particle1] = vector2 + vector3 * num2;
				((ObiNativeList<Vector4>)(object)solver.positions)[((ObiRopeBase)rope).elements[j].particle2] = vector2 + vector3 * (num2 + ((ObiRopeBase)rope).elements[j].restLength);
				num2 += ((ObiRopeBase)rope).elements[j].restLength;
			}
			yield return null;
		}
		cursor.ChangeLength(magnitude);
		for (int k = 0; k < ((ObiActor)rope).activeParticleCount; k++)
		{
			((ObiNativeList<float>)(object)solver.invMasses)[((ObiActor)rope).solverIndices[k]] = 10f;
		}
		ObiPinConstraintsBatch val = new ObiPinConstraintsBatch((ObiPinConstraintsData)null);
		val.AddConstraint(((ObiRopeBase)rope).elements[0].particle1, (ObiColliderBase)(object)character, base.transform.localPosition, Quaternion.identity, 0f, 0f, float.PositiveInfinity);
		val.AddConstraint(((ObiRopeBase)rope).elements[((ObiRopeBase)rope).elements.Count - 1].particle2, hookAttachment.collider.GetComponent<ObiColliderBase>(), hookAttachment.collider.transform.InverseTransformPoint(hookAttachment.point), Quaternion.identity, 0f, 0f, float.PositiveInfinity);
		((ObiConstraintsBatch)val).activeConstraintCount = 2;
		pinConstraints.AddBatch(val);
		((ObiActor)rope).SetConstraintsDirty((ConstraintType)8);
	}

	private void DetachHook()
	{
		rope.ropeBlueprint = null;
		((Component)(object)rope).GetComponent<MeshRenderer>().enabled = false;
	}

	private void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			if (!((ObiActor)rope).isLoaded)
			{
				LaunchHook();
			}
			else
			{
				DetachHook();
			}
		}
		if (((ObiActor)rope).isLoaded)
		{
			if (Input.GetKey(KeyCode.W))
			{
				cursor.ChangeLength(((ObiRopeBase)rope).restLength - hookExtendRetractSpeed * Time.deltaTime);
			}
			if (Input.GetKey(KeyCode.S))
			{
				cursor.ChangeLength(((ObiRopeBase)rope).restLength + hookExtendRetractSpeed * Time.deltaTime);
			}
		}
	}
}
