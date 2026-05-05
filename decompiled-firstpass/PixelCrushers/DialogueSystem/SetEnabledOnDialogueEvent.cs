using System;
using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class SetEnabledOnDialogueEvent : ActOnDialogueEvent
{
	[Serializable]
	public class SetEnabledAction : Action
	{
		public MonoBehaviour target;

		public Toggle state;
	}

	public SetEnabledAction[] onStart = new SetEnabledAction[0];

	[Tooltip("When the dialogue event starts, wait one frame before processing the On Start list.")]
	public bool waitOneFrameOnStart;

	public SetEnabledAction[] onEnd = new SetEnabledAction[0];

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

	private void TryActions(SetEnabledAction[] actions, Transform actor, bool waitOneFrame)
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

	private void TryActionsNow(SetEnabledAction[] actions, Transform actor)
	{
		foreach (SetEnabledAction setEnabledAction in actions)
		{
			if (setEnabledAction != null && setEnabledAction.condition != null && setEnabledAction.condition.IsTrue(actor))
			{
				DoAction(setEnabledAction, actor);
			}
		}
	}

	private IEnumerator TryActionsAfterOneFrameCoroutine(SetEnabledAction[] actions, Transform actor)
	{
		Debug.Log("Waiting 1 frame");
		yield return CoroutineUtility.endOfFrame;
		yield return null;
		yield return new WaitForSeconds(2f);
		TryActionsNow(actions, actor);
	}

	public void DoAction(SetEnabledAction action, Transform actor)
	{
		if (action == null)
		{
			return;
		}
		MonoBehaviour monoBehaviour = Tools.Select(action.target, this);
		if (!(monoBehaviour == null))
		{
			bool newValue = ToggleUtility.GetNewValue(monoBehaviour.enabled, action.state);
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Trigger: {1}.{2}.enabled = {3}", "Dialogue System", monoBehaviour.name, monoBehaviour.GetType().Name, newValue));
			}
			monoBehaviour.enabled = newValue;
		}
	}
}
