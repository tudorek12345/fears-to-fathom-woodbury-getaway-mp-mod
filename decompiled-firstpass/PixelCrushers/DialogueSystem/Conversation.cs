using System;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem.ChatMapper;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class Conversation : Asset
{
	public ConversationOverrideDisplaySettings overrideSettings = new ConversationOverrideDisplaySettings();

	public string nodeColor;

	public List<DialogueEntry> dialogueEntries = new List<DialogueEntry>();

	public List<EntryGroup> entryGroups = new List<EntryGroup>();

	[HideInInspector]
	public Vector2 canvasScrollPosition = Vector2.zero;

	[HideInInspector]
	public float canvasZoom = 1f;

	public string Title
	{
		get
		{
			return LookupValue("Title");
		}
		set
		{
			Field.SetValue(fields, "Title", value);
		}
	}

	public int ActorID
	{
		get
		{
			return LookupInt("Actor");
		}
		set
		{
			Field.SetValue(fields, "Actor", value.ToString(), FieldType.Actor);
		}
	}

	public int ConversantID
	{
		get
		{
			return LookupInt("Conversant");
		}
		set
		{
			Field.SetValue(fields, "Conversant", value.ToString(), FieldType.Actor);
		}
	}

	public Conversation()
	{
	}

	public Conversation(Conversation sourceConversation)
		: base(sourceConversation)
	{
		nodeColor = sourceConversation.nodeColor;
		overrideSettings = sourceConversation.overrideSettings;
		dialogueEntries = CopyDialogueEntries(sourceConversation.dialogueEntries);
		entryGroups = CopyEntryGroups(sourceConversation.entryGroups);
	}

	public Conversation(PixelCrushers.DialogueSystem.ChatMapper.Conversation chatMapperConversation, bool putEndSequenceOnLastSplit = true)
	{
		Assign(chatMapperConversation, putEndSequenceOnLastSplit);
	}

	public void Assign(PixelCrushers.DialogueSystem.ChatMapper.Conversation chatMapperConversation, bool putEndSequenceOnLastSplit = true)
	{
		if (chatMapperConversation == null)
		{
			return;
		}
		Assign(chatMapperConversation.ID, chatMapperConversation.Fields);
		nodeColor = chatMapperConversation.NodeColor;
		foreach (DialogEntry dialogEntry in chatMapperConversation.DialogEntries)
		{
			AddConversationDialogueEntry(dialogEntry);
		}
		SplitPipesIntoEntries(putEndSequenceOnLastSplit);
		foreach (DialogueEntry dialogueEntry2 in dialogueEntries)
		{
			foreach (Link outgoingLink in dialogueEntry2.outgoingLinks)
			{
				if (outgoingLink.destinationConversationID == id)
				{
					DialogueEntry dialogueEntry = GetDialogueEntry(outgoingLink.destinationDialogueID);
					if (dialogueEntry != null)
					{
						outgoingLink.priority = dialogueEntry.conditionPriority;
					}
				}
			}
		}
	}

	private void AddConversationDialogueEntry(DialogEntry chatMapperEntry)
	{
		DialogueEntry dialogueEntry = new DialogueEntry(chatMapperEntry);
		dialogueEntry.conversationID = id;
		dialogueEntries.Add(dialogueEntry);
	}

	public DialogueEntry GetDialogueEntry(string title)
	{
		return dialogueEntries.Find((DialogueEntry e) => string.Equals(e.Title, title));
	}

	public DialogueEntry GetDialogueEntry(int dialogueEntryID)
	{
		return dialogueEntries.Find((DialogueEntry e) => e.id == dialogueEntryID);
	}

	public DialogueEntry GetFirstDialogueEntry()
	{
		return dialogueEntries.Find((DialogueEntry e) => string.Equals(e.Title, "START"));
	}

	public void SplitPipesIntoEntries(bool putEndSequenceOnLastSplit = true, bool trimWhitespace = false, string uniqueFieldTitle = null)
	{
		if (dialogueEntries == null)
		{
			return;
		}
		int count = dialogueEntries.Count;
		for (int i = 0; i < count; i++)
		{
			string dialogueText = dialogueEntries[i].DialogueText;
			if (!string.IsNullOrEmpty(dialogueText) && dialogueText.Contains("|"))
			{
				SplitEntryAtPipes(i, dialogueText, putEndSequenceOnLastSplit, trimWhitespace, uniqueFieldTitle);
			}
		}
	}

	private void SplitEntryAtPipes(int originalEntryIndex, string dialogueText, bool putEndSequenceOnLastSplit, bool trimWhitespace, string uniqueFieldTitle = null)
	{
		string[] array = dialogueText.Split(new char[1] { '|' });
		DialogueEntry dialogueEntry = dialogueEntries[originalEntryIndex];
		dialogueEntry.DialogueText = (trimWhitespace ? array[0].Trim() : array[0]);
		List<Link> outgoingLinks = dialogueEntry.outgoingLinks;
		ConditionPriority priority = ((outgoingLinks != null && outgoingLinks.Count > 0) ? outgoingLinks[0].priority : ConditionPriority.Normal);
		DialogueEntry dialogueEntry2 = dialogueEntry;
		List<DialogueEntry> list = new List<DialogueEntry>();
		list.Add(dialogueEntry2);
		string[] array2 = ((dialogueEntry != null && dialogueEntry.MenuText != null) ? dialogueEntry.MenuText : string.Empty).Split(new char[1] { '|' });
		string audioFiles = dialogueEntry.AudioFiles;
		audioFiles = ((audioFiles != null && audioFiles.Length >= 2) ? audioFiles.Substring(1, audioFiles.Length - 2) : string.Empty);
		string[] array3 = audioFiles.Split(new char[1] { ';' });
		dialogueEntry2.AudioFiles = $"[{((array3.Length != 0) ? array3[0] : string.Empty)}]";
		bool flag = !string.IsNullOrEmpty(uniqueFieldTitle);
		string arg = (flag ? Field.LookupValue(dialogueEntry2.fields, uniqueFieldTitle) : string.Empty);
		for (int i = 1; i < array.Length; i++)
		{
			string text = array[i];
			string text2 = ((i < array2.Length) ? array2[i] : string.Empty);
			if (trimWhitespace)
			{
				text = text.Trim();
				text2 = text2.Trim();
			}
			DialogueEntry dialogueEntry3 = AddNewDialogueEntry(dialogueEntry, text, i, trimWhitespace);
			dialogueEntry3.canvasRect = new Rect(dialogueEntry.canvasRect.x + (float)(i * 20), dialogueEntry.canvasRect.y + (float)(i * 10), dialogueEntry.canvasRect.width, dialogueEntry.canvasRect.height);
			dialogueEntry3.currentMenuText = text2;
			dialogueEntry3.AudioFiles = $"[{((i < array3.Length) ? array3[i] : string.Empty)}]";
			if (flag)
			{
				Field.SetValue(dialogueEntry3.fields, uniqueFieldTitle, $"{arg}-{i}");
			}
			dialogueEntry2.outgoingLinks = new List<Link> { NewLink(dialogueEntry2, dialogueEntry3, priority) };
			dialogueEntry2 = dialogueEntry3;
			list.Add(dialogueEntry3);
		}
		dialogueEntry2.outgoingLinks = outgoingLinks;
		foreach (Field field in dialogueEntry.fields)
		{
			if (string.IsNullOrEmpty(field.title))
			{
				continue;
			}
			string text3 = ((field.value != null) ? field.value : string.Empty);
			bool flag2 = field.title.StartsWith("Sequence");
			bool flag3 = field.type == FieldType.Localization;
			bool flag4 = text3.Contains("|");
			if ((flag2 || flag3) && !string.IsNullOrEmpty(field.value) && flag4)
			{
				array = field.value.Split(new char[1] { '|' });
				if (array.Length > 1)
				{
					text3 = (trimWhitespace ? array[0].Trim() : array[0]);
					field.value = text3;
				}
			}
			else if (flag2 && putEndSequenceOnLastSplit && !flag4 && !string.IsNullOrEmpty(field.value) && field.value.Contains("{{end}}"))
			{
				PutEndSequenceOnLastSplit(list, field);
			}
		}
	}

	private void PutEndSequenceOnLastSplit(List<DialogueEntry> entries, Field field)
	{
		string[] array = field.value.Split(new char[1] { ';' });
		for (int i = 0; i < entries.Count; i++)
		{
			Field field2 = Field.Lookup(entries[i].fields, field.title);
			field2.value = string.Empty;
			if (i == 0)
			{
				string[] array2 = array;
				foreach (string text in array2)
				{
					if (!text.Contains("{{end}}"))
					{
						field2.value = field2.value + text.Trim() + "; ";
					}
				}
				field2.value += "Delay({{end}})";
			}
			else if (i == entries.Count - 1)
			{
				string[] array2 = array;
				foreach (string text2 in array2)
				{
					if (text2.Contains("{{end}}"))
					{
						field2.value = field2.value + text2.Trim() + "; ";
					}
				}
			}
			else
			{
				field2.value = "Delay({{end}})";
			}
		}
	}

	private DialogueEntry AddNewDialogueEntry(DialogueEntry originalEntry, string dialogueText, int partNum, bool trimWhitespace)
	{
		DialogueEntry dialogueEntry = new DialogueEntry();
		dialogueEntry.id = GetHighestDialogueEntryID() + 1;
		dialogueEntry.conversationID = originalEntry.conversationID;
		dialogueEntry.isRoot = originalEntry.isRoot;
		dialogueEntry.isGroup = originalEntry.isGroup;
		dialogueEntry.nodeColor = originalEntry.nodeColor;
		dialogueEntry.delaySimStatus = originalEntry.delaySimStatus;
		dialogueEntry.falseConditionAction = originalEntry.falseConditionAction;
		dialogueEntry.conditionsString = (string.Equals(originalEntry.falseConditionAction, "Passthrough") ? originalEntry.conditionsString : string.Empty);
		dialogueEntry.userScript = string.Empty;
		dialogueEntry.fields = new List<Field>();
		foreach (Field field in originalEntry.fields)
		{
			if (string.IsNullOrEmpty(field.title))
			{
				continue;
			}
			string value = field.value;
			if ((field.title.StartsWith("Sequence") || field.type == FieldType.Localization) && !string.IsNullOrEmpty(field.value) && field.value.Contains("|"))
			{
				string[] array = field.value.Split(new char[1] { '|' });
				if (partNum < array.Length)
				{
					value = (trimWhitespace ? array[partNum].Trim() : array[partNum].Trim());
				}
			}
			dialogueEntry.fields.Add(new Field(field.title, value, field.type));
		}
		dialogueEntry.DialogueText = dialogueText;
		dialogueEntries.Add(dialogueEntry);
		return dialogueEntry;
	}

	private int GetHighestDialogueEntryID()
	{
		int num = 0;
		foreach (DialogueEntry dialogueEntry in dialogueEntries)
		{
			num = Mathf.Max(dialogueEntry.id, num);
		}
		return num;
	}

	private Link NewLink(DialogueEntry origin, DialogueEntry destination, ConditionPriority priority = ConditionPriority.Normal)
	{
		return new Link
		{
			originConversationID = origin.conversationID,
			originDialogueID = origin.id,
			destinationConversationID = destination.conversationID,
			destinationDialogueID = destination.id,
			isConnector = (origin.conversationID != destination.conversationID),
			priority = priority
		};
	}

	private List<DialogueEntry> CopyDialogueEntries(List<DialogueEntry> sourceEntries)
	{
		List<DialogueEntry> list = new List<DialogueEntry>();
		foreach (DialogueEntry sourceEntry in sourceEntries)
		{
			list.Add(new DialogueEntry(sourceEntry));
		}
		return list;
	}

	private List<EntryGroup> CopyEntryGroups(List<EntryGroup> sourceGroups)
	{
		List<EntryGroup> list = new List<EntryGroup>();
		foreach (EntryGroup sourceGroup in sourceGroups)
		{
			list.Add(new EntryGroup(sourceGroup));
		}
		return list;
	}
}
