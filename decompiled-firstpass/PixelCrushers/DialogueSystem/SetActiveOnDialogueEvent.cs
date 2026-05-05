using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class SetActiveOnDialogueEvent : ActOnDialogueEvent
{
	[Serializable]
	public class SetActiveAction : Action
	{
		public Transform target;

		public Toggle state;
	}

	public SetActiveAction[] onStart = new SetActiveAction[0];

	public SetActiveAction[] onEnd = new SetActiveAction[0];

	public override void TryStartActions(Transform actor)
	{
		TryActions(onStart, actor);
	}

	public override void TryEndActions(Transform actor)
	{
		TryActions(onEnd, actor);
	}

	private void TryActions(SetActiveAction[] actions, Transform actor)
	{
		if (actions == null)
		{
			return;
		}
		foreach (SetActiveAction setActiveAction in actions)
		{
			if (setActiveAction != null && setActiveAction.condition != null && setActiveAction.condition.IsTrue(actor))
			{
				DoAction(setActiveAction, actor);
			}
		}
	}

	public void DoAction(SetActiveAction action, Transform actor)
	{
		if (action != null)
		{
			Transform transform = Tools.Select(action.target, base.transform);
			bool newValue = ToggleUtility.GetNewValue(transform.gameObject.activeSelf, action.state);
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Trigger: {1}.SetActive({2})", new object[3] { "Dialogue System", transform.name, newValue }));
			}
			transform.gameObject.SetActive(newValue);
		}
	}
}
