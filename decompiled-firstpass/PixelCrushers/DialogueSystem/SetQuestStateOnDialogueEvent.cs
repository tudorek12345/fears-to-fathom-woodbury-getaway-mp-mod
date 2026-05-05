using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class SetQuestStateOnDialogueEvent : ActOnDialogueEvent
{
	[Serializable]
	public class SetQuestStateAction : Action
	{
		[QuestPopup(false)]
		public string questName;

		[QuestState]
		public QuestState questState;

		public string alertMessage;
	}

	public SetQuestStateAction[] onStart = new SetQuestStateAction[0];

	public SetQuestStateAction[] onEnd = new SetQuestStateAction[0];

	public override void TryStartActions(Transform actor)
	{
		TryActions(onStart, actor);
	}

	public override void TryEndActions(Transform actor)
	{
		TryActions(onEnd, actor);
	}

	private void TryActions(SetQuestStateAction[] actions, Transform actor)
	{
		if (actions == null)
		{
			return;
		}
		foreach (SetQuestStateAction setQuestStateAction in actions)
		{
			if (setQuestStateAction != null && setQuestStateAction.condition != null && setQuestStateAction.condition.IsTrue(actor))
			{
				DoAction(setQuestStateAction, actor);
			}
		}
	}

	public void DoAction(SetQuestStateAction action, Transform actor)
	{
		if (action != null && !string.IsNullOrEmpty(action.questName))
		{
			QuestLog.SetQuestState(action.questName, action.questState);
			if (!string.IsNullOrEmpty(action.alertMessage))
			{
				DialogueManager.ShowAlert(action.alertMessage);
			}
			DialogueManager.SendUpdateTracker();
		}
	}
}
