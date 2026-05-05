using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace RopeToolkit;

public static class PointsExtensions
{
	public static float GetLengthOfCurve(this NativeArray<float3> curve, ref float4x4 transform, bool isLoop = false)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		if (curve.Length == 0)
		{
			return 0f;
		}
		float num = 0f;
		float4 val = math.mul(transform, new float4(curve[0], 1f));
		float3 xyz = ((float4)(ref val)).xyz;
		float3 val2 = xyz;
		for (int i = 1; i < curve.Length; i++)
		{
			val = math.mul(transform, new float4(curve[i], 1f));
			float3 xyz2 = ((float4)(ref val)).xyz;
			num += math.distance(val2, xyz2);
			val2 = xyz2;
		}
		if (isLoop)
		{
			num += math.distance(val2, xyz);
		}
		return num;
	}

	public static float GetLengthOfCurve(this NativeArray<float3> curve, bool isLoop = false)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		float4x4 transform = float4x4.identity;
		return curve.GetLengthOfCurve(ref transform, isLoop);
	}

	public static float GetLengthOfCurve(this IEnumerable<float3> curve, ref float4x4 transform, bool isLoop = false)
	{
		NativeArray<float3> curve2 = new NativeArray<float3>(curve.ToArray(), Allocator.Temp);
		float lengthOfCurve = curve2.GetLengthOfCurve(ref transform, isLoop);
		curve2.Dispose();
		return lengthOfCurve;
	}

	public static float GetLengthOfCurve(this IEnumerable<float3> curve, bool isLoop = false)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		float4x4 transform = float4x4.identity;
		return curve.GetLengthOfCurve(ref transform, isLoop);
	}

	private static void GetPointAlongCurve(this NativeArray<float3> curve, ref float4x4 transform, float distance, out float3 point, ref int currentTargetIndex, ref float accumulatedLength)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		if (curve.Length < 2)
		{
			throw new ArgumentException("curve");
		}
		if (currentTargetIndex < 1 || currentTargetIndex >= curve.Length)
		{
			throw new ArgumentOutOfRangeException("currentTargetIndex");
		}
		float3 val = curve[currentTargetIndex - 1];
		float4 val4;
		while (currentTargetIndex < curve.Length)
		{
			float3 val2 = curve[currentTargetIndex];
			float num = math.distance(val, val2);
			if (distance <= accumulatedLength + num)
			{
				float3 val3 = math.lerp(val, val2, (distance - accumulatedLength) / num);
				val4 = math.mul(transform, new float4(val3, 1f));
				point = ((float4)(ref val4)).xyz;
				return;
			}
			currentTargetIndex++;
			accumulatedLength += num;
			val = val2;
		}
		currentTargetIndex = curve.Length - 1;
		val4 = math.mul(transform, new float4(val, 1f));
		point = ((float4)(ref val4)).xyz;
	}

	public static void GetPointAlongCurve(this NativeArray<float3> curve, ref float4x4 transform, float distance, out float3 point)
	{
		int currentTargetIndex = 1;
		float accumulatedLength = 0f;
		curve.GetPointAlongCurve(ref transform, distance, out point, ref currentTargetIndex, ref accumulatedLength);
	}

	public static void GetPointAlongCurve(this NativeArray<float3> curve, float distance, out float3 point)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		float4x4 transform = float4x4.identity;
		curve.GetPointAlongCurve(ref transform, distance, out point);
	}

	public static void GetPointAlongCurve(this IEnumerable<float3> curve, ref float4x4 transform, float distance, out float3 point)
	{
		NativeArray<float3> curve2 = new NativeArray<float3>(curve.ToArray(), Allocator.Temp);
		curve2.GetPointAlongCurve(ref transform, distance, out point);
		curve2.Dispose();
	}

	public static void GetPointAlongCurve(this IEnumerable<float3> curve, float distance, out float3 point)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		float4x4 transform = float4x4.identity;
		curve.GetPointAlongCurve(ref transform, distance, out point);
	}

	public static void GetPointsAlongCurve(this NativeArray<float3> curve, ref float4x4 transform, float desiredPointDistance, NativeArray<float3> result)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		int currentTargetIndex = 1;
		float accumulatedLength = 0f;
		for (int i = 0; i < result.Length; i++)
		{
			curve.GetPointAlongCurve(ref transform, desiredPointDistance * (float)i, out var point, ref currentTargetIndex, ref accumulatedLength);
			result[i] = point;
		}
	}

	public static void GetPointsAlongCurve(this NativeArray<float3> curve, float desiredPointDistance, NativeArray<float3> result)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		float4x4 transform = float4x4.identity;
		curve.GetPointsAlongCurve(ref transform, desiredPointDistance, result);
	}

	public static void GetPointsAlongCurve(this IEnumerable<float3> curve, ref float4x4 transform, float desiredPointDistance, NativeArray<float3> result)
	{
		NativeArray<float3> curve2 = new NativeArray<float3>(curve.ToArray(), Allocator.Temp);
		curve2.GetPointsAlongCurve(ref transform, desiredPointDistance, result);
		curve2.Dispose();
	}

	public static void GetPointsAlongCurve(this IEnumerable<float3> curve, float desiredPointDistance, NativeArray<float3> result)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		float4x4 transform = float4x4.identity;
		curve.GetPointsAlongCurve(ref transform, desiredPointDistance, result);
	}

	public static void GetClosestPoint(this NativeArray<float3> curve, float3 point, out int index, out float distance)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		index = 0;
		float num = math.distancesq(curve[0], point);
		for (int i = 1; i < curve.Length; i++)
		{
			float num2 = math.distancesq(curve[i], point);
			if (num2 < num)
			{
				index = i;
				num = num2;
			}
		}
		distance = math.sqrt(num);
	}

	public static void GetClosestPoint(this NativeArray<float3> curve, Ray ray, out int index, out float distance, out float distanceAlongRay)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		index = 0;
		float3 val = float3.op_Implicit(ray.origin);
		float3 val2 = math.normalizesafe(float3.op_Implicit(ray.direction), default(float3));
		float num = math.dot(curve[0] - val, val2);
		float num2 = math.distancesq(val + num * val2, curve[0]);
		for (int i = 1; i < curve.Length; i++)
		{
			float3 val3 = curve[i];
			float num3 = math.dot(val3 - val, val2);
			float num4 = math.distancesq(val + num3 * val2, val3);
			if (num4 < num2)
			{
				index = i;
				num = num3;
				num2 = num4;
			}
		}
		distance = math.sqrt(num2);
		distanceAlongRay = num;
	}

	public static void KeepAtDistance(this ref float3 point, ref float3 otherPoint, float distance, float stiffness = 1f)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		float3 val = otherPoint - point;
		float num = math.length(val);
		val = ((!(num > 0f)) ? float3.zero : (val / num));
		val *= (num - distance) * stiffness;
		point += val;
		otherPoint -= val;
	}
}
