using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[Serializable]
public class UnityAlertControls : AbstractUIAlertControls
{
	public GUIControl panel;

	public GUILabel line;

	public GUIButton continueButton;

	public override bool isVisible
	{
		get
		{
			if (line != null)
			{
				return line.gameObject.activeSelf;
			}
			return false;
		}
	}

	public override void SetActive(bool value)
	{
		UnityDialogueUIControls.SetControlActive(line, value);
		UnityDialogueUIControls.SetControlActive(panel, value);
	}

	public override void SetMessage(string message, float duration)
	{
		if (line != null)
		{
			line.SetFormattedText(FormattedText.Parse(message, DialogueManager.masterDatabase.emphasisSettings));
			SetFadeDuration(line.gameObject, duration);
			if (panel != null)
			{
				SetFadeDuration(panel.gameObject, duration);
			}
		}
	}

	private void SetFadeDuration(GameObject go, float duration)
	{
		if (!(go != null))
		{
			return;
		}
		FadeEffect component = go.GetComponent<FadeEffect>();
		if (component != null)
		{
			component.SetFadeDurations(component.fadeInDuration, duration, component.fadeOutDuration);
			m_alertDoneTime = Mathf.Max(m_alertDoneTime, DialogueTime.time + component.fadeInDuration + duration + component.fadeOutDuration);
			if (go.activeInHierarchy)
			{
				component.StopAllCoroutines();
				component.StartCoroutine(component.Play());
			}
		}
	}
}
