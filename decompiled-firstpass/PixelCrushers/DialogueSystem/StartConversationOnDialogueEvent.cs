using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class StartConversationOnDialogueEvent : ActOnDialogueEvent
{
	[Serializable]
	public class ConversationAction : Action
	{
		public Transform speaker;

		public Transform listener;

		[ConversationPopup(false, false)]
		public string conversation;

		public bool skipIfNoValidEntries;
	}

	public ConversationAction[] onStart = new ConversationAction[0];

	public ConversationAction[] onEnd = new ConversationAction[0];

	public override void TryStartActions(Transform actor)
	{
		TryActions(onStart, actor);
	}

	public override void TryEndActions(Transform actor)
	{
		TryActions(onEnd, actor);
	}

	private void TryActions(ConversationAction[] actions, Transform actor)
	{
		if (actions == null)
		{
			return;
		}
		foreach (ConversationAction conversationAction in actions)
		{
			if (conversationAction != null && conversationAction.condition != null && conversationAction.condition.IsTrue(actor))
			{
				DoAction(conversationAction, actor);
			}
		}
	}

	public void DoAction(ConversationAction action, Transform actor)
	{
		if (action != null)
		{
			Transform actor2 = Tools.Select(action.speaker, base.transform);
			Transform conversant = Tools.Select(action.listener, actor);
			if (!action.skipIfNoValidEntries || DialogueManager.ConversationHasValidEntry(action.conversation, actor2, conversant))
			{
				DialogueManager.StartConversation(action.conversation, actor2, conversant);
			}
		}
	}
}
