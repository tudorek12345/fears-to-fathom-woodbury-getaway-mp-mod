using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class StandardUIAlertControls : AbstractUIAlertControls
{
	[Tooltip("Main alert panel (optional).")]
	public UIPanel panel;

	[Tooltip("Alert text.")]
	public UITextField alertText;

	[Tooltip("Wait for previous alerts to finish before showing new alert; if unticked, new alerts replace old.")]
	public bool queueAlerts;

	[Tooltip("If a message is already queued to display, don't queue another.")]
	public bool dontQueueDuplicates;

	[Tooltip("Wait for the previous alert's Hide animation to finish before showing the next queued alert.")]
	public bool waitForHideAnimation;

	[Tooltip("If message contains [f], show immediately instead of queueing.")]
	public bool allowForceImmediate;

	private bool m_initializedAnimator;

	public override bool isVisible
	{
		get
		{
			if (!(panel != null))
			{
				if (alertText != null)
				{
					return alertText.activeInHierarchy;
				}
				return false;
			}
			return panel.isOpen;
		}
	}

	public bool isHiding
	{
		get
		{
			if (panel != null)
			{
				return string.Equals(panel.animatorMonitor.currentTrigger, panel.hideAnimationTrigger);
			}
			return false;
		}
	}

	public override void SetActive(bool value)
	{
		if (panel != null)
		{
			if (!m_initializedAnimator && !value)
			{
				if (panel.deactivateOnHidden)
				{
					panel.gameObject.SetActive(value: false);
				}
			}
			else
			{
				panel.SetOpen(value);
			}
		}
		m_initializedAnimator = true;
		if (value || panel == null)
		{
			alertText.SetActive(value: true);
		}
	}

	public void HideImmediate()
	{
		alertText.SetActive(value: false);
	}

	public override void SetMessage(string message, float duration)
	{
		alertText.text = FormattedText.Parse(message).text;
	}
}
