using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class QuestCondition
{
	public string questName = string.Empty;

	[Tooltip("The allowable quest states for the condition to be true.")]
	[BitMask(typeof(QuestState))]
	[QuestState]
	public QuestState questState;

	[Tooltip("Check quest entry state.")]
	public bool checkQuestEntry;

	[QuestEntryPopup]
	public int entryNumber;

	[Tooltip("If quest entry is specified, the allowable quest entry states for the condition to be true.")]
	[BitMask(typeof(QuestState))]
	[QuestState]
	public QuestState questEntryState;

	public bool IsTrue
	{
		get
		{
			if (!string.IsNullOrEmpty(questName))
			{
				if (QuestLog.IsQuestInStateMask(questName, questState))
				{
					if (checkQuestEntry)
					{
						return QuestLog.IsQuestEntryInStateMask(questName, entryNumber, questEntryState);
					}
					return true;
				}
				return false;
			}
			return true;
		}
	}
}
