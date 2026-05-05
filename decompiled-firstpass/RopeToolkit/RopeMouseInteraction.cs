using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace RopeToolkit;

public class RopeMouseInteraction : MonoBehaviour
{
	public Mesh indicatorMesh;

	public Material indicatorMaterial;

	public List<Rope> ropes;

	protected bool ready;

	protected Rope pulledRope;

	protected int pulledParticle;

	protected float pulledDistance;

	protected float3 currentPosition;

	protected float3 targetPosition;

	protected Rope GetClosestRope(Ray ray, out int closestParticleIndex, out float closestDistanceAlongRay)
	{
		closestParticleIndex = -1;
		closestDistanceAlongRay = 0f;
		int num = -1;
		float num2 = 0f;
		for (int i = 0; i < ropes.Count; i++)
		{
			ropes[i].GetClosestParticle(ray, out var particleIndex, out var distance, out var distanceAlongRay);
			if (distance < num2 || num == -1)
			{
				num = i;
				closestParticleIndex = particleIndex;
				num2 = distance;
				closestDistanceAlongRay = distanceAlongRay;
			}
		}
		if (num == -1)
		{
			return null;
		}
		return ropes[num];
	}

	public void FixedUpdate()
	{
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (Input.GetMouseButton(0))
		{
			if (ready && pulledRope == null)
			{
				int closestParticleIndex;
				float closestDistanceAlongRay;
				Rope closestRope = GetClosestRope(ray, out closestParticleIndex, out closestDistanceAlongRay);
				if (closestRope != null && closestParticleIndex != -1 && closestRope.GetMassMultiplierAt(closestParticleIndex) > 0f)
				{
					pulledRope = closestRope;
					pulledParticle = closestParticleIndex;
					pulledDistance = closestDistanceAlongRay;
					ready = false;
				}
			}
		}
		else if (pulledRope != null)
		{
			pulledRope.SetMassMultiplierAt(pulledParticle, 1f);
			pulledRope = null;
		}
		if (!(pulledRope != null))
		{
			return;
		}
		pulledDistance += Input.mouseScrollDelta.y * 2f;
		currentPosition = pulledRope.GetPositionAt(pulledParticle);
		targetPosition = float3.op_Implicit(ray.GetPoint(pulledDistance));
		pulledRope.SetPositionAt(pulledParticle, targetPosition);
		pulledRope.SetVelocityAt(pulledParticle, float3.zero);
		pulledRope.SetMassMultiplierAt(pulledParticle, 0f);
		if (Input.GetKey(KeyCode.Space))
		{
			ropes.Remove(pulledRope);
			Rope[] array = new Rope[2];
			pulledRope.SplitAt(pulledParticle, array);
			if (array[0] != null)
			{
				ropes.Add(array[0]);
			}
			if (array[1] != null)
			{
				ropes.Add(array[1]);
			}
			pulledRope = null;
		}
	}

	public void Update()
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		if (!Input.GetMouseButton(0))
		{
			ready = true;
		}
		if (!(indicatorMesh == null) && !(indicatorMaterial == null) && pulledRope != null)
		{
			Graphics.DrawMesh(indicatorMesh, Matrix4x4.TRS(float3.op_Implicit(currentPosition), Quaternion.identity, Vector3.one * 0.25f), indicatorMaterial, 0);
		}
	}
}
