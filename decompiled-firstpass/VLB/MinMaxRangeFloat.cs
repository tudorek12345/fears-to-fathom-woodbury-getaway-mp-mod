using System;
using UnityEngine;

namespace VLB;

[Serializable]
public struct MinMaxRangeFloat
{
	[SerializeField]
	private float m_MinValue;

	[SerializeField]
	private float m_MaxValue;

	public float minValue => m_MinValue;

	public float maxValue => m_MaxValue;

	public float randomValue => UnityEngine.Random.Range(minValue, maxValue);

	public Vector2 asVector2 => new Vector2(minValue, maxValue);

	public float GetLerpedValue(float lerp01)
	{
		return Mathf.Lerp(minValue, maxValue, lerp01);
	}

	public MinMaxRangeFloat(float min, float max)
	{
		m_MinValue = min;
		m_MaxValue = max;
	}
}
