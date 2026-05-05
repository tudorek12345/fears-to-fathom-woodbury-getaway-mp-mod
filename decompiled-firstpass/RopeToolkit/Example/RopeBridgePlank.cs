using Unity.Mathematics;
using UnityEngine;

namespace RopeToolkit.Example;

[RequireComponent(typeof(Rigidbody))]
public class RopeBridgePlank : MonoBehaviour
{
	public Rope ropeLeft;

	public Rope ropeRight;

	public float extentLeft = -0.5f;

	public float extentRight = 0.5f;

	public float extentPivot = 0.5f;

	[Tooltip("A measure of the longitudal stiffness of the plank. That is, how quickly should the particles on the opposite ropes move to the correct distance between them.")]
	[Range(0f, 1f)]
	public float longitudalStiffness = 0.25f;

	public float restingRigidbodyMassMultiplier = 5f;

	protected Rigidbody rb;

	protected int particleLeft;

	protected int particleRight;

	protected int particlePivotLeft;

	protected int particlePivotRight;

	protected float distance;

	protected float frameTotalMass;

	public void Start()
	{
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		rb = GetComponent<Rigidbody>();
		if (rb != null)
		{
			rb.isKinematic = true;
		}
		Vector3 vector = base.transform.TransformPoint(Vector3.right * extentLeft);
		Vector3 vector2 = base.transform.TransformPoint(Vector3.right * extentRight);
		Vector3 vector3 = base.transform.TransformPoint(Vector3.forward * extentPivot);
		if (ropeLeft != null)
		{
			ropeLeft.GetClosestParticle(float3.op_Implicit(vector), out particleLeft, out var num);
			ropeLeft.GetClosestParticle(float3.op_Implicit(vector3), out particlePivotLeft, out num);
		}
		if (ropeRight != null)
		{
			ropeRight.GetClosestParticle(float3.op_Implicit(vector2), out particleRight, out var num2);
			ropeRight.GetClosestParticle(float3.op_Implicit(vector3), out particlePivotRight, out num2);
		}
		if (ropeLeft != null && ropeRight != null)
		{
			distance = math.distance(ropeLeft.GetPositionAt(particleLeft), ropeRight.GetPositionAt(particleRight));
		}
	}

	public void FixedUpdate()
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		if (rb == null)
		{
			return;
		}
		if (ropeLeft == null || ropeRight == null)
		{
			rb.isKinematic = false;
			return;
		}
		float3 point = ropeLeft.GetPositionAt(particleLeft);
		float3 otherPoint = ropeRight.GetPositionAt(particleRight);
		float3 val = (ropeLeft.GetPositionAt(particlePivotLeft) + ropeRight.GetPositionAt(particlePivotRight)) * 0.5f;
		point.KeepAtDistance(ref otherPoint, distance, longitudalStiffness);
		float3 val2 = (point + otherPoint) * 0.5f;
		rb.MoveRotation(Quaternion.LookRotation(float3.op_Implicit(val - val2), Vector3.Cross(float3.op_Implicit(val - val2), float3.op_Implicit(otherPoint - point))));
		rb.MovePosition(float3.op_Implicit(val2) - base.transform.TransformVector(Vector3.right * (extentLeft + extentRight) * 0.5f));
		ropeLeft.SetPositionAt(particleLeft, point);
		ropeRight.SetPositionAt(particleRight, otherPoint);
		float value = 1f + frameTotalMass * restingRigidbodyMassMultiplier;
		frameTotalMass = 0f;
		if (ropeLeft.GetMassMultiplierAt(particleLeft) > 0f)
		{
			ropeLeft.SetMassMultiplierAt(particleLeft, value);
		}
		if (ropeRight.GetMassMultiplierAt(particleRight) > 0f)
		{
			ropeRight.SetMassMultiplierAt(particleRight, value);
		}
	}

	public void OnCollisionStay(Collision collision)
	{
		if (collision.rigidbody != null)
		{
			frameTotalMass += collision.rigidbody.mass;
		}
	}
}
