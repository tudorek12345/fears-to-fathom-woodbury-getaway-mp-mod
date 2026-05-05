using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class UIAnimationTransitions
{
	[Tooltip("To show the panel, play this state/trigger.")]
	public string showTrigger = "Show";

	[Tooltip("To hide the panel, play this state/trigger.")]
	public string hideTrigger = "Hide";

	[Tooltip("Specifies whether Show Trigger and Hide Trigger are animator states or trigger parameters.")]
	public UIShowHideController.TransitionMode transitionMode;

	public bool debug;

	public void ClearTriggers(UIShowHideController showHideController)
	{
		if (showHideController != null && transitionMode == UIShowHideController.TransitionMode.Trigger)
		{
			showHideController.ClearTrigger(showTrigger);
			showHideController.ClearTrigger(hideTrigger);
		}
	}
}
