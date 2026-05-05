using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class LODManager : MonoBehaviour
{
	[Serializable]
	public class LOD
	{
		[Tooltip("The minimum distance for this LOD.")]
		[SerializeField]
		private float m_minDistance;

		[Tooltip("The max distance for this LOD.")]
		[SerializeField]
		private float m_maxDistance = float.PositiveInfinity;

		public float minDistance
		{
			get
			{
				return m_minDistance;
			}
			set
			{
				m_minDistance = value;
			}
		}

		public float maxDistance
		{
			get
			{
				return m_maxDistance;
			}
			set
			{
				m_maxDistance = value;
			}
		}

		public bool Contains(float distance)
		{
			if (minDistance <= distance)
			{
				return distance <= maxDistance;
			}
			return false;
		}
	}

	[Tooltip("The LODs (levels of detail).")]
	[SerializeField]
	private LOD[] m_levels;

	[Tooltip("The frequency at which to check distance from the player and update the current LOD if necessary.")]
	[SerializeField]
	private float m_monitorFrequency = 5f;

	private int m_currentLevel;

	private WaitForSeconds m_currentWaitForSeconds = new WaitForSeconds(60f);

	private float m_currentWaitForSecondsValue;

	public LOD[] levels
	{
		get
		{
			return m_levels;
		}
		set
		{
			m_levels = value;
		}
	}

	public float monitorFrequency
	{
		get
		{
			return m_monitorFrequency;
		}
		set
		{
			m_monitorFrequency = value;
		}
	}

	public Transform player { get; set; }

	private void Start()
	{
		FindPlayer();
		StartCoroutine(MonitorLOD());
	}

	public void FindPlayer()
	{
		GameObject gameObject = GameObject.FindWithTag("Player");
		player = ((gameObject != null) ? gameObject.transform : null);
	}

	private IEnumerator MonitorLOD()
	{
		yield return new WaitForSeconds(UnityEngine.Random.value);
		m_currentWaitForSecondsValue = -1f;
		while (true)
		{
			CheckLOD();
			if (monitorFrequency != m_currentWaitForSecondsValue)
			{
				m_currentWaitForSecondsValue = monitorFrequency;
				m_currentWaitForSeconds = new WaitForSeconds(monitorFrequency);
			}
			yield return m_currentWaitForSeconds;
		}
	}

	public void CheckLOD()
	{
		if (player == null || levels == null || levels.Length == 0)
		{
			return;
		}
		float distance = Vector3.Distance(base.transform.position, player.position);
		if (levels[m_currentLevel].Contains(distance))
		{
			return;
		}
		for (int i = 0; i < levels.Length; i++)
		{
			if (levels[i].Contains(distance))
			{
				m_currentLevel = i;
				BroadcastMessage("OnLOD", i, SendMessageOptions.DontRequireReceiver);
				break;
			}
		}
	}

	public static List<string> ZonePluginActivator()
	{
		return new List<string> { "monitorFrequency|System.Single|0|99999|1|The frequency at which to check distance from the player and update the current LOD if necessary." };
	}
}
