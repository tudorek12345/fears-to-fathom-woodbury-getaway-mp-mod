using System.Collections;
using Obi;
using UnityEngine;

public class GrapplingHook : MonoBehaviour
{
	public ObiSolver solver;

	public ObiCollider character;

	public float hookExtendRetractSpeed = 2f;

	public Material material;

	public ObiRopeSection section;

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
		yield return 0;
		Vector3 vector = ((Component)(object)rope).transform.InverseTransformPoint(hookAttachment.point);
		int num = ObiUtils.MakeFilter(65535, 0);
		((ObiRopeBlueprintBase)blueprint).path.Clear();
		((ObiRopeBlueprintBase)blueprint).path.AddControlPoint(Vector3.zero, -vector.normalized, vector.normalized, Vector3.up, 0.1f, 0.1f, 1f, num, Color.white, "Hook start");
		((ObiRopeBlueprintBase)blueprint).path.AddControlPoint(vector, -vector.normalized, vector.normalized, Vector3.up, 0.1f, 0.1f, 1f, num, Color.white, "Hook end");
		((ObiRopeBlueprintBase)blueprint).path.FlushEvents();
		yield return ((ObiActorBlueprint)blueprint).Generate();
		rope.ropeBlueprint = blueprint;
		((Component)(object)rope).GetComponent<MeshRenderer>().enabled = true;
		ObiConstraints<ObiPinConstraintsBatch> obj = ((ObiActor)rope).GetConstraintsByType((ConstraintType)8) as ObiConstraints<ObiPinConstraintsBatch>;
		obj.Clear();
		ObiPinConstraintsBatch val = new ObiPinConstraintsBatch((ObiPinConstraintsData)null);
		val.AddConstraint(((ObiActor)rope).solverIndices[0], (ObiColliderBase)(object)character, base.transform.localPosition, Quaternion.identity, 0f, 0f, float.PositiveInfinity);
		val.AddConstraint(((ObiActor)rope).solverIndices[((ObiActorBlueprint)blueprint).activeParticleCount - 1], hookAttachment.collider.GetComponent<ObiColliderBase>(), hookAttachment.collider.transform.InverseTransformPoint(hookAttachment.point), Quaternion.identity, 0f, 0f, float.PositiveInfinity);
		((ObiConstraintsBatch)val).activeConstraintCount = 2;
		obj.AddBatch(val);
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
