using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class StartSequenceOnDialogueEvent : ActOnDialogueEvent
{
	[Serializable]
	public class SequenceAction : Action
	{
		public Transform actor;

		public Transform otherParticipant;

		[Multiline]
		public string sequence;
	}

	public SequenceAction[] onStart = new SequenceAction[0];

	public SequenceAction[] onEnd = new SequenceAction[0];

	public override void TryStartActions(Transform actor)
	{
		TryActions(onStart, actor);
	}

	public override void TryEndActions(Transform actor)
	{
		TryActions(onEnd, actor);
	}

	private void TryActions(SequenceAction[] actions, Transform actor)
	{
		if (actions == null)
		{
			return;
		}
		foreach (SequenceAction sequenceAction in actions)
		{
			if (sequenceAction != null && sequenceAction.condition != null && sequenceAction.condition.IsTrue(actor))
			{
				DoAction(sequenceAction, actor);
			}
		}
	}

	public void DoAction(SequenceAction action, Transform actor)
	{
		if (action != null)
		{
			Transform speaker = Tools.Select(action.actor, base.transform);
			Transform listener = Tools.Select(action.otherParticipant, actor);
			DialogueManager.PlaySequence(action.sequence, speaker, listener);
		}
	}
}
