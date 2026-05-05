using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class StandardUIQTEControls : AbstractUIQTEControls
{
	[Tooltip("(Optional) Quick Time Event (QTE) indicators. Typically graphics such as images or sprites.")]
	public GameObject[] QTEIndicators;

	private int m_numVisibleQTEIndicators;

	public override bool areVisible => m_numVisibleQTEIndicators > 0;

	public override void SetActive(bool value)
	{
		if (!value)
		{
			HideImmediate();
		}
	}

	public void HideImmediate()
	{
		m_numVisibleQTEIndicators = 0;
		GameObject[] qTEIndicators = QTEIndicators;
		for (int i = 0; i < qTEIndicators.Length; i++)
		{
			Tools.SetGameObjectActive(qTEIndicators[i], value: false);
		}
	}

	public override void ShowIndicator(int index)
	{
		if (!IsQTEIndicatorVisible(index))
		{
			Tools.SetGameObjectActive(QTEIndicators[index], value: true);
			m_numVisibleQTEIndicators++;
		}
	}

	public override void HideIndicator(int index)
	{
		if (IsValidQTEIndex(index) && IsQTEIndicatorVisible(index))
		{
			Tools.SetGameObjectActive(QTEIndicators[index], value: false);
			m_numVisibleQTEIndicators--;
		}
	}

	private bool IsQTEIndicatorVisible(int index)
	{
		if (!IsValidQTEIndex(index))
		{
			return false;
		}
		return QTEIndicators[index].gameObject.activeSelf;
	}

	private bool IsValidQTEIndex(int index)
	{
		if (0 <= index)
		{
			return index < QTEIndicators.Length;
		}
		return false;
	}
}
