using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem;

public static class LinkUtility
{
	public class PrioritySorter : IComparer<Link>
	{
		public int Compare(Link link1, Link link2)
		{
			if (link1 == null || link2 == null)
			{
				return 0;
			}
			return link2.priority.CompareTo(link1.priority);
		}
	}

	public static void SortOutgoingLinks(DialogueDatabase database, Conversation conversation)
	{
		if (conversation == null)
		{
			return;
		}
		foreach (DialogueEntry dialogueEntry in conversation.dialogueEntries)
		{
			SortOutgoingLinks(database, dialogueEntry);
		}
	}

	public static void SortOutgoingLinks(DialogueDatabase database, DialogueEntry dialogueEntry)
	{
		if (!(database != null) || dialogueEntry == null)
		{
			return;
		}
		foreach (Link outgoingLink in dialogueEntry.outgoingLinks)
		{
			DialogueEntry dialogueEntry2 = database.GetDialogueEntry(outgoingLink);
			if (dialogueEntry2 != null)
			{
				outgoingLink.priority = dialogueEntry2.conditionPriority;
			}
		}
		dialogueEntry.outgoingLinks.Sort(new PrioritySorter());
	}

	public static bool IsPassthroughOnFalse(DialogueEntry entry)
	{
		return string.Equals(entry.falseConditionAction, "Passthrough");
	}
}
