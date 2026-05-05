using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class SetAnimatorStateOnDialogueEvent : ActOnDialogueEvent
{
	[Serializable]
	public class SetAnimatorStateAction : Action
	{
		public Transform target;

		public string stateName;

		public float crossFadeDuration = 0.3f;
	}

	public SetAnimatorStateAction[] onStart = new SetAnimatorStateAction[0];

	public SetAnimatorStateAction[] onEnd = new SetAnimatorStateAction[0];

	public override void TryStartActions(Transform actor)
	{
		TryActions(onStart, actor);
	}

	public override void TryEndActions(Transform actor)
	{
		TryActions(onEnd, actor);
	}

	private void TryActions(SetAnimatorStateAction[] actions, Transform actor)
	{
		if (actions == null)
		{
			return;
		}
		foreach (SetAnimatorStateAction setAnimatorStateAction in actions)
		{
			if (setAnimatorStateAction != null && setAnimatorStateAction.condition != null && setAnimatorStateAction.condition.IsTrue(actor))
			{
				DoAction(setAnimatorStateAction, actor);
			}
		}
	}

	public void DoAction(SetAnimatorStateAction action, Transform actor)
	{
		if (action == null)
		{
			return;
		}
		Transform transform = Tools.Select(action.target, base.transform);
		Animator componentInChildren = transform.GetComponentInChildren<Animator>();
		if (componentInChildren == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.Log(string.Format("{0}: Trigger: {1}.SetAnimatorState() can't find Animator", new object[2] { "Dialogue System", transform.name }));
			}
			return;
		}
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Trigger: {1}.SetAnimatorState({2})", new object[3] { "Dialogue System", transform.name, action.stateName }));
		}
		componentInChildren.CrossFade(action.stateName, action.crossFadeDuration);
	}
}
