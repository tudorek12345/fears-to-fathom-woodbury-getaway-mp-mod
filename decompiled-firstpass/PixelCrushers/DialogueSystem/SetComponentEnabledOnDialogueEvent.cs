using System;
using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class SetComponentEnabledOnDialogueEvent : ActOnDialogueEvent
{
	[Serializable]
	public class SetComponentEnabledAction : Action
	{
		public Component target;

		public Toggle state;
	}

	public SetComponentEnabledAction[] onStart = new SetComponentEnabledAction[0];

	[Tooltip("When the dialogue event starts, wait one frame before processing the On Start list.")]
	public bool waitOneFrameOnStart;

	public SetComponentEnabledAction[] onEnd = new SetComponentEnabledAction[0];

	[Tooltip("When the dialogue event starts, wait one frame before processing the On End list.")]
	public bool waitOneFrameOnEnd;

	public override void TryStartActions(Transform actor)
	{
		TryActions(onStart, actor, waitOneFrameOnStart);
	}

	public override void TryEndActions(Transform actor)
	{
		TryActions(onEnd, actor, waitOneFrameOnEnd);
	}

	private void TryActions(SetComponentEnabledAction[] actions, Transform actor, bool waitOneFrame)
	{
		if (actions != null)
		{
			if (waitOneFrame)
			{
				StartCoroutine(TryActionsAfterOneFrameCoroutine(actions, actor));
			}
			else
			{
				TryActionsNow(actions, actor);
			}
		}
	}

	private void TryActionsNow(SetComponentEnabledAction[] actions, Transform actor)
	{
		foreach (SetComponentEnabledAction setComponentEnabledAction in actions)
		{
			if (setComponentEnabledAction != null && setComponentEnabledAction.condition != null && setComponentEnabledAction.condition.IsTrue(actor))
			{
				DoAction(setComponentEnabledAction, actor);
			}
		}
	}

	private IEnumerator TryActionsAfterOneFrameCoroutine(SetComponentEnabledAction[] actions, Transform actor)
	{
		yield return CoroutineUtility.endOfFrame;
		yield return null;
		TryActionsNow(actions, actor);
	}

	public void DoAction(SetComponentEnabledAction action, Transform actor)
	{
		if (action != null && action.target != null)
		{
			Tools.SetComponentEnabled(action.target, action.state);
		}
	}
}
