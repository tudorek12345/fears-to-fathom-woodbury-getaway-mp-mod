using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class UnityUIBarkSubtitleDialogueUI : UnityUIDialogueUI
{
	public override void Awake()
	{
		base.Awake();
		Tools.DeprecationWarning(this);
	}

	public override void ShowSubtitle(Subtitle subtitle)
	{
		UnityUIBarkUI componentInChildren = subtitle.speakerInfo.transform.GetComponentInChildren<UnityUIBarkUI>();
		if (componentInChildren == null)
		{
			Debug.LogWarning("Dialogue System: Speaker (" + subtitle.speakerInfo.transform?.ToString() + ") doesn't have a bark UI: " + subtitle.formattedText.text, subtitle.speakerInfo.transform);
		}
		else
		{
			componentInChildren.Bark(subtitle);
		}
		HideResponses();
	}
}
