using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class QuestStateIndicator : MonoBehaviour
{
	[Tooltip("GameObject such as a world space canvas element associated with each indicator level. A typical use is to associate indicator level 0 = nothing (unassigned), level 1 = question mark, and level 2 = exclamation mark.")]
	public GameObject[] indicators = new GameObject[0];

	private List<List<QuestStateListener>> m_currentIndicatorCount = new List<List<QuestStateListener>>();

	private void Awake()
	{
		InitializeCurrentIndicatorCount();
	}

	private void Start()
	{
		UpdateIndicator();
	}

	private void InitializeCurrentIndicatorCount()
	{
		m_currentIndicatorCount.Clear();
		for (int i = 0; i < indicators.Length; i++)
		{
			m_currentIndicatorCount.Add(new List<QuestStateListener>());
		}
	}

	public void SetIndicatorLevel(QuestStateListener listener, int indicatorLevel)
	{
		if (DialogueDebug.logInfo)
		{
			Debug.Log("Dialogue System: " + base.name + ": SetIndicatorLevel(" + listener?.ToString() + ", " + indicatorLevel + ")", listener);
		}
		for (int i = 0; i < indicators.Length; i++)
		{
			if (m_currentIndicatorCount[i].Contains(listener))
			{
				m_currentIndicatorCount[i].Remove(listener);
				break;
			}
		}
		if (0 <= indicatorLevel && indicatorLevel < indicators.Length)
		{
			m_currentIndicatorCount[indicatorLevel].Add(listener);
		}
		UpdateIndicator();
	}

	public void UpdateIndicator()
	{
		for (int i = 0; i < indicators.Length; i++)
		{
			if (indicators[i] != null)
			{
				indicators[i].SetActive(value: false);
			}
		}
		for (int num = indicators.Length - 1; num >= 0; num--)
		{
			if (m_currentIndicatorCount[num].Count > 0)
			{
				if (indicators[num] != null)
				{
					indicators[num].SetActive(value: true);
					if (DialogueDebug.logInfo)
					{
						Debug.Log("Dialogue System: " + base.name + ": Activating GameObject associated with indicator level " + num, this);
					}
				}
				break;
			}
		}
	}
}
