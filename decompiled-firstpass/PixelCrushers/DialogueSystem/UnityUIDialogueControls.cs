using System;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class UnityUIDialogueControls : AbstractDialogueUIControls
{
	[Tooltip("Panel containing the entire conversation UI")]
	public Graphic panel;

	[Tooltip("Optional animation transitions; panel should have an Animator")]
	public UIAnimationTransitions animationTransitions = new UIAnimationTransitions();

	public UnityUISubtitleControls npcSubtitle;

	public UnityUISubtitleControls pcSubtitle;

	public UnityUIResponseMenuControls responseMenu;

	private UIShowHideController m_showHideController;

	public override AbstractUISubtitleControls npcSubtitleControls => npcSubtitle;

	public override AbstractUISubtitleControls pcSubtitleControls => pcSubtitle;

	public override AbstractUIResponseMenuControls responseMenuControls => responseMenu;

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
			ShowPanel();
		}
		else
		{
			HidePanel();
		}
	}

	public override void ShowPanel()
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
	}

	public void DeactivateUIElements()
	{
		Tools.SetGameObjectActive((Component)(object)panel, value: false);
		base.SetActive(false);
	}
}
