using Unity.Mathematics;
using UnityEngine;

namespace RopeToolkit;

public static class RigidbodyExtensions
{
	public static void GetLocalInertiaTensor(this Rigidbody rb, out float3x3 localInertiaTensor)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		float3x3 val = default(float3x3);
		((float3x3)(ref val))._002Ector(quaternion.op_Implicit(rb.inertiaTensorRotation));
		float3x3 val2 = math.transpose(val);
		localInertiaTensor = math.mul(math.mul(val, float3x3.Scale(float3.op_Implicit(rb.inertiaTensor))), val2);
	}

	public static void GetInertiaTensor(this Rigidbody rb, out float3x3 inertiaTensor)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		rb.GetLocalInertiaTensor(out var localInertiaTensor);
		float3x3 val = default(float3x3);
		((float3x3)(ref val))._002Ector(quaternion.op_Implicit(rb.rotation));
		float3x3 val2 = math.transpose(val);
		inertiaTensor = math.mul(math.mul(val, localInertiaTensor), val2);
	}

	public static void GetInvInertiaTensor(this Rigidbody rb, out float3x3 invInertiaTensor)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		rb.GetLocalInertiaTensor(out var localInertiaTensor);
		float3x3 val = float3x3.zero;
		if (math.determinant(localInertiaTensor) != 0f)
		{
			val = math.inverse(localInertiaTensor);
		}
		float3x3 val2 = default(float3x3);
		((float3x3)(ref val2))._002Ector(quaternion.op_Implicit(rb.rotation));
		float3x3 val3 = math.transpose(val2);
		invInertiaTensor = math.mul(math.mul(val2, val), val3);
	}

	public static void ApplyImpulseNow(this Rigidbody rb, ref float3x3 invInertiaTensor, float3 point, float3 impulse)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		if (rb.mass != 0f)
		{
			float3 val = math.cross(point - float3.op_Implicit(rb.worldCenterOfMass), impulse);
			float3 val2 = math.mul(invInertiaTensor, val);
			rb.velocity += float3.op_Implicit(impulse) / rb.mass;
			rb.angularVelocity += float3.op_Implicit(val2);
		}
	}

	public static void ApplyImpulseNow(this Rigidbody rb, float3 point, float3 impulse)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		rb.GetInvInertiaTensor(out var invInertiaTensor);
		rb.ApplyImpulseNow(ref invInertiaTensor, point, impulse);
	}

	public static void SetPointVelocityNow(this Rigidbody rb, ref float3x3 invInertiaTensor, float3 point, float3 normal, float desiredSpeed, float damping = 1f)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		if (rb.mass != 0f)
		{
			float num = desiredSpeed - math.dot(float3.op_Implicit(rb.GetPointVelocity(float3.op_Implicit(point))), normal) * damping;
			float3 val = point - float3.op_Implicit(rb.worldCenterOfMass);
			float num2 = 1f / rb.mass + math.dot(math.cross(math.mul(invInertiaTensor, math.cross(val, normal)), val), normal);
			if (num2 != 0f)
			{
				float num3 = num / num2;
				rb.ApplyImpulseNow(ref invInertiaTensor, point, num3 * normal);
			}
		}
	}

	public static void SetPointVelocityNow(this Rigidbody rb, float3 point, float3 normal, float desiredSpeed, float damping = 1f)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		rb.GetInvInertiaTensor(out var invInertiaTensor);
		rb.SetPointVelocityNow(ref invInertiaTensor, point, normal, desiredSpeed, damping);
	}
}
