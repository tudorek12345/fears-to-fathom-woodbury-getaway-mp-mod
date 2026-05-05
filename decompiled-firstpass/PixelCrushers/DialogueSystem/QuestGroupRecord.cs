using System;

namespace PixelCrushers.DialogueSystem;

public class QuestGroupRecord : IComparable
{
	public string groupName;

	public string questTitle;

	public QuestGroupRecord()
	{
	}

	public QuestGroupRecord(string groupName, string questTitle)
	{
		this.groupName = groupName;
		this.questTitle = questTitle;
	}

	public int CompareTo(object obj)
	{
		if (!(obj is QuestGroupRecord questGroupRecord))
		{
			return 1;
		}
		if (string.Equals(groupName, questGroupRecord.groupName))
		{
			return string.Compare(questTitle, questGroupRecord.questTitle);
		}
		return string.Compare(groupName, questGroupRecord.groupName);
	}
}
