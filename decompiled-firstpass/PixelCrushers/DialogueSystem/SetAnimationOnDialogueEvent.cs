using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class SetAnimationOnDialogueEvent : ActOnDialogueEvent
{
	[Serializable]
	public class SetAnimationAction : Action
	{
		public Transform target;

		public AnimationClip animationClip;
	}

	public SetAnimationAction[] onStart = new SetAnimationAction[0];

	public SetAnimationAction[] onEnd = new SetAnimationAction[0];

	public override void TryStartActions(Transform actor)
	{
		TryActions(onStart, actor);
	}

	public override void TryEndActions(Transform actor)
	{
		TryActions(onEnd, actor);
	}

	private void TryActions(SetAnimationAction[] actions, Transform actor)
	{
		if (actions == null)
		{
			return;
		}
		foreach (SetAnimationAction setAnimationAction in actions)
		{
			if (setAnimationAction != null && setAnimationAction.condition != null && setAnimationAction.condition.IsTrue(actor))
			{
				DoAction(setAnimationAction, actor);
			}
		}
	}

	public void DoAction(SetAnimationAction action, Transform actor)
	{
		if (action == null)
		{
			return;
		}
		Transform transform = Tools.Select(action.target, base.transform);
		Animation componentInChildren = transform.GetComponentInChildren<Animation>();
		if (componentInChildren == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.Log(string.Format("{0}: Trigger: {1}.SetAnimation() can't find Animation component", new object[2] { "Dialogue System", transform.name }));
			}
			return;
		}
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Trigger: {1}.SetAnimation({2})", new object[3] { "Dialogue System", transform.name, action.animationClip }));
		}
		componentInChildren.CrossFade(action.animationClip.name);
	}
}
