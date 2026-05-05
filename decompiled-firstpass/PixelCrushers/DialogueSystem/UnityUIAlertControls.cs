using System;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class UnityUIAlertControls : AbstractUIAlertControls
{
	[Tooltip("Optional panel containing the alert line; can contain other doodads and effects, too")]
	public Graphic panel;

	[Tooltip("Shows the alert message text")]
	public Text line;

	[Tooltip("Optional continue button; configure OnClick to invoke dialogue UI's OnContinue method")]
	public Button continueButton;

	[Tooltip("Wait for previous alerts to finish before showing new alert; if unticked, new alerts replace old")]
	public bool queueAlerts;

	[Tooltip("Wait for the previous alert's Hide animation to finish before showing the next queued alert")]
	public bool waitForHideAnimation;

	[Tooltip("Optional animation transitions; panel should have an Animator")]
	public UIAnimationTransitions animationTransitions = new UIAnimationTransitions();

	private UIShowHideController m_showHideController;

	public override bool isVisible => showHideController.state != UIShowHideController.State.Hidden;

	public bool IsHiding => showHideController.state == UIShowHideController.State.Hiding;

	private UIShowHideController showHideController
	{
		get
		{
			if (m_showHideController == null)
			{
				m_showHideController = new UIShowHideController(null, (Component)(object)panel, animationTransitions.transitionMode, animationTransitions.debug);
			}
			return m_showHideController;
		}
	}

	public override void SetActive(bool value)
	{
		if (value)
		{
			if (showHideController.state != UIShowHideController.State.Showing)
			{
				ShowPanel();
			}
		}
		else if (showHideController.state != UIShowHideController.State.Hiding)
		{
			HidePanel();
		}
	}

	private void ShowPanel()
	{
		ActivateUIElements();
		animationTransitions.ClearTriggers(showHideController);
		showHideController.Show(animationTransitions.showTrigger, pauseAfterAnimation: false, null);
	}

	private void HidePanel()
	{
		animationTransitions.ClearTriggers(showHideController);
		showHideController.Hide(animationTransitions.hideTrigger, DeactivateUIElements);
	}

	public void ActivateUIElements()
	{
		Tools.SetGameObjectActive((Component)(object)panel, value: true);
		Tools.SetGameObjectActive((Component)(object)line, value: true);
	}

	public void DeactivateUIElements()
	{
		Tools.SetGameObjectActive((Component)(object)panel, value: false);
		Tools.SetGameObjectActive((Component)(object)line, value: false);
	}

	public override void SetMessage(string message, float duration)
	{
		if ((UnityEngine.Object)(object)line != null)
		{
			line.text = FormattedText.Parse(message, DialogueManager.masterDatabase.emphasisSettings).text;
		}
	}

	public void AutoFocus(bool allowStealFocus = true)
	{
		UITools.Select((Selectable)(object)continueButton, allowStealFocus);
	}
}
