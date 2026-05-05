using System;
using Unity.Mathematics;
using UnityEngine;

namespace RopeToolkit;

[RequireComponent(typeof(Rope))]
public class RopeConnection : MonoBehaviour
{
	[Serializable]
	public struct RigidbodySettings
	{
		[Tooltip("The rigidbody to connect to")]
		public Rigidbody body;

		[Tooltip("A measure of the stiffness of the connection. Lower values are usually more stable.")]
		[Range(0f, 1f)]
		public float stiffness;

		[Tooltip("The amount of the rigidbody velocity to remove when the impulse is from the rope is applied to the rigidbody")]
		[Range(0f, 1f)]
		public float damping;
	}

	[Serializable]
	public struct TransformSettings
	{
		[Tooltip("The transform to connect to")]
		public Transform transform;
	}

	protected static readonly Color[] colors = new Color[4]
	{
		new Color(0.69f, 0f, 1f),
		new Color(1f, 0f, 0f),
		new Color(1f, 0f, 0f),
		new Color(1f, 1f, 0f)
	};

	[DisableInPlayMode]
	public RopeConnectionType type;

	[DisableInPlayMode]
	[Range(0f, 1f)]
	public float ropeLocation;

	public bool autoFindRopeLocation;

	public RigidbodySettings rigidbodySettings = new RigidbodySettings
	{
		stiffness = 0.1f,
		damping = 0.1f
	};

	public TransformSettings transformSettings = default(TransformSettings);

	[Tooltip("The point in local object space to connect to")]
	public float3 localConnectionPoint;

	protected Rope rope;

	protected int particleIndex;

	public Component connectedObject
	{
		get
		{
			switch (type)
			{
			case RopeConnectionType.PinRopeToTransform:
			case RopeConnectionType.PinTransformToRope:
				return transformSettings.transform;
			case RopeConnectionType.PullRigidbodyToRope:
			case RopeConnectionType.TwoWayCouplingBetweenRigidbodyAndRope:
				return rigidbodySettings.body;
			default:
				return null;
			}
		}
	}

	public float3 connectionPoint
	{
		get
		{
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			Component component = connectedObject;
			if ((bool)component)
			{
				return float3.op_Implicit(component.transform.TransformPoint(float3.op_Implicit(localConnectionPoint)));
			}
			return float3.zero;
		}
	}

	public void Initialize(bool forceReset)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		if (!rope || forceReset)
		{
			rope = GetComponent<Rope>();
			if (autoFindRopeLocation)
			{
				rope.GetClosestParticle(connectionPoint, out particleIndex, out var _);
				ropeLocation = rope.GetScalarDistanceAt(particleIndex);
			}
			else
			{
				float distance2 = ropeLocation * rope.measurements.realCurveLength;
				particleIndex = rope.GetParticleIndexAt(distance2);
			}
		}
	}

	public void OnRopeSplit(Rope.OnSplitParams p)
	{
		if (autoFindRopeLocation)
		{
			UnityEngine.Object.Destroy(this);
			return;
		}
		int particleIndexAt = p.preSplitMeasurements.GetParticleIndexAt(ropeLocation * p.preSplitMeasurements.realCurveLength);
		if (particleIndexAt < p.minParticleIndex || particleIndexAt > p.maxParticleIndex)
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	public void OnDisable()
	{
		if ((bool)rope && type == RopeConnectionType.PinRopeToTransform)
		{
			rope.SetMassMultiplierAt(particleIndex, 1f);
		}
	}

	protected void EnforceConnection()
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		Initialize(forceReset: false);
		if (!rope || !connectedObject)
		{
			return;
		}
		switch (type)
		{
		case RopeConnectionType.PinRopeToTransform:
			rope.SetMassMultiplierAt(particleIndex, 0f);
			rope.SetPositionAt(particleIndex, connectionPoint);
			break;
		case RopeConnectionType.PinTransformToRope:
		{
			float3 positionAt2 = rope.GetPositionAt(particleIndex, respectInterpolation: true);
			float3 val3 = float3.op_Implicit(transformSettings.transform.TransformPoint(float3.op_Implicit(localConnectionPoint)) - transformSettings.transform.position);
			transformSettings.transform.position = float3.op_Implicit(positionAt2 - val3);
			break;
		}
		case RopeConnectionType.PullRigidbodyToRope:
		{
			float3 positionAt = rope.GetPositionAt(particleIndex);
			float3 val = connectionPoint;
			float3 val2 = positionAt - val;
			float num = math.length(val2);
			if (num > 0f)
			{
				float3 normal = val2 / num;
				float desiredSpeed = num * rigidbodySettings.stiffness / Time.fixedDeltaTime;
				rigidbodySettings.body.SetPointVelocityNow(val, normal, desiredSpeed, rigidbodySettings.damping);
			}
			break;
		}
		case RopeConnectionType.TwoWayCouplingBetweenRigidbodyAndRope:
			rope.RegisterRigidbodyConnection(particleIndex, rigidbodySettings.body, rigidbodySettings.damping, connectionPoint, rigidbodySettings.stiffness);
			break;
		}
	}

	protected bool ShouldEnforceInFixedUpdate()
	{
		bool num = type != RopeConnectionType.PinRopeToTransform && type != RopeConnectionType.PinTransformToRope;
		bool flag = (bool)rope && rope.interpolation != RopeInterpolation.None;
		if (!num)
		{
			return !flag;
		}
		return true;
	}

	public void Update()
	{
		if (!ShouldEnforceInFixedUpdate())
		{
			EnforceConnection();
		}
	}

	public void FixedUpdate()
	{
		if (ShouldEnforceInFixedUpdate())
		{
			EnforceConnection();
		}
	}
}
