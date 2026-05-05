using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class SendMessageOnDialogueEvent : ActOnDialogueEvent
{
	[Serializable]
	public class SendMessageAction : Action
	{
		public Transform target;

		public string methodName;

		public string parameter;
	}

	public SendMessageAction[] onStart = new SendMessageAction[0];

	public SendMessageAction[] onEnd = new SendMessageAction[0];

	public override void TryStartActions(Transform actor)
	{
		TryActions(onStart, actor);
	}

	public override void TryEndActions(Transform actor)
	{
		TryActions(onEnd, actor);
	}

	private void TryActions(SendMessageAction[] actions, Transform actor)
	{
		if (actions == null)
		{
			return;
		}
		foreach (SendMessageAction sendMessageAction in actions)
		{
			if (sendMessageAction != null && sendMessageAction.condition != null && sendMessageAction.condition.IsTrue(actor))
			{
				DoAction(sendMessageAction, actor);
			}
		}
	}

	private void DoAction(SendMessageAction action, Transform actor)
	{
		if (action != null)
		{
			Transform transform = Tools.Select(action.target, base.transform);
			string text = (string.IsNullOrEmpty(action.parameter) ? null : action.parameter);
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Sending message '{1}' to {2} (parameter={3}).", "Dialogue System", action.methodName, transform, text), this);
			}
			transform.BroadcastMessage(action.methodName, text, SendMessageOptions.DontRequireReceiver);
		}
	}
}
