using System;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class UnityUIQuestTemplateAlternateDescriptions
{
	[Tooltip("(Optional) If set, use if state is success")]
	public Text successDescription;

	[Tooltip("(Optional) If set, use if state is failure")]
	public Text failureDescription;

	public void SetActive(bool value)
	{
		if ((UnityEngine.Object)(object)successDescription != null)
		{
			((Component)(object)successDescription).gameObject.SetActive(value);
		}
		if ((UnityEngine.Object)(object)failureDescription != null)
		{
			((Component)(object)failureDescription).gameObject.SetActive(value);
		}
	}
}
