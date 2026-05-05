using System;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[Serializable]
public class UnityQTEControls : AbstractUIQTEControls
{
	public GUIControl[] qteIndicators;

	private int numVisibleQTEIndicators;

	public override bool areVisible => numVisibleQTEIndicators > 0;

	public UnityQTEControls(GUIControl[] qteIndicators)
	{
		this.qteIndicators = qteIndicators;
	}

	public override void SetActive(bool value)
	{
		if (!value)
		{
			numVisibleQTEIndicators = 0;
			GUIControl[] array = qteIndicators;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].gameObject.SetActive(value: false);
			}
		}
	}

	public override void ShowIndicator(int index)
	{
		if (IsValidQTEIndex(index) && !IsQTEIndicatorVisible(index))
		{
			qteIndicators[index].gameObject.SetActive(value: true);
			numVisibleQTEIndicators++;
		}
	}

	public override void HideIndicator(int index)
	{
		if (IsValidQTEIndex(index) && IsQTEIndicatorVisible(index))
		{
			qteIndicators[index].gameObject.SetActive(value: false);
			numVisibleQTEIndicators--;
		}
	}

	private bool IsQTEIndicatorVisible(int index)
	{
		if (!IsValidQTEIndex(index))
		{
			return false;
		}
		return qteIndicators[index].gameObject.activeSelf;
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
