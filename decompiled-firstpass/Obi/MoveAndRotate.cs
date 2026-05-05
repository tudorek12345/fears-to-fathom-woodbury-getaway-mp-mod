using System;
using UnityEngine;

namespace Obi;

public class MoveAndRotate : MonoBehaviour
{
	[Serializable]
	public class Vector3andSpace
	{
		public Vector3 value;

		public Space space = Space.Self;
	}

	public Vector3andSpace moveUnitsPerSecond;

	public Vector3andSpace rotateDegreesPerSecond;

	public bool ignoreTimescale;

	private float m_LastRealTime;

	private void Start()
	{
		m_LastRealTime = Time.realtimeSinceStartup;
	}

	private void FixedUpdate()
	{
		float num = Time.fixedDeltaTime;
		if (ignoreTimescale)
		{
			num = Time.realtimeSinceStartup - m_LastRealTime;
			m_LastRealTime = Time.realtimeSinceStartup;
		}
		base.transform.Translate(moveUnitsPerSecond.value * num, moveUnitsPerSecond.space);
		base.transform.Rotate(rotateDegreesPerSecond.value * num, rotateDegreesPerSecond.space);
	}
}
