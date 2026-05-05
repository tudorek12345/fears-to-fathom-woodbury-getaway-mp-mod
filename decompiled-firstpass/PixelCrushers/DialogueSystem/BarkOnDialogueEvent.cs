using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class BarkOnDialogueEvent : ActOnDialogueEvent
{
	[Serializable]
	public class BarkAction : Action
	{
		public Transform speaker;

		public Transform listener;

		[ConversationPopup(false, false)]
		public string conversation;
	}

	public BarkAction[] onStart = new BarkAction[0];

	public BarkAction[] onEnd = new BarkAction[0];

	[Tooltip("The order in which to bark dialogue entries.")]
	public BarkOrder barkOrder;

	private BarkHistory barkHistory;

	public Sequencer sequencer { get; private set; }

	private void Awake()
	{
		barkHistory = new BarkHistory(barkOrder);
		sequencer = null;
	}

	public override void TryStartActions(Transform actor)
	{
		TryActions(onStart, actor);
	}

	public override void TryEndActions(Transform actor)
	{
		TryActions(onEnd, actor);
	}

	private void TryActions(BarkAction[] actions, Transform actor)
	{
		if (actions == null)
		{
			return;
		}
		foreach (BarkAction barkAction in actions)
		{
			if (barkAction != null && barkAction.condition != null && barkAction.condition.IsTrue(actor))
			{
				DoAction(barkAction, actor);
			}
		}
	}

	public void DoAction(BarkAction action, Transform actor)
	{
		if (action != null)
		{
			Transform speaker = Tools.Select(action.speaker, base.transform);
			Transform listener = Tools.Select(action.listener, actor);
			DialogueManager.Bark(action.conversation, speaker, listener, barkHistory);
			sequencer = BarkController.LastSequencer;
		}
	}
}
