using System;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class UnityUIQTEControls : AbstractUIQTEControls
{
	public Graphic[] qteIndicators;

	private int numVisibleQTEIndicators;

	public override bool areVisible => numVisibleQTEIndicators > 0;

	public UnityUIQTEControls(Graphic[] qteIndicators)
	{
		this.qteIndicators = qteIndicators;
	}

	public override void SetActive(bool value)
	{
		if (!value)
		{
			numVisibleQTEIndicators = 0;
			Graphic[] array = qteIndicators;
			for (int i = 0; i < array.Length; i++)
			{
				Tools.SetGameObjectActive((Component)(object)array[i], value: false);
			}
		}
	}

	public override void ShowIndicator(int index)
	{
		if (IsValidQTEIndex(index) && !IsQTEIndicatorVisible(index))
		{
			Tools.SetGameObjectActive((Component)(object)qteIndicators[index], value: true);
			numVisibleQTEIndicators++;
		}
	}

	public override void HideIndicator(int index)
	{
		if (IsValidQTEIndex(index) && IsQTEIndicatorVisible(index))
		{
			Tools.SetGameObjectActive((Component)(object)qteIndicators[index], value: false);
			numVisibleQTEIndicators--;
		}
	}

	private bool IsQTEIndicatorVisible(int index)
	{
		if (!IsValidQTEIndex(index))
		{
			return false;
		}
		return ((Component)(object)qteIndicators[index]).gameObject.activeSelf;
	}

	private bool IsValidQTEIndex(int index)
	{
		if (0 <= index)
		{
			return index < qteIndicators.Length;
		}
		return false;
	}
}
