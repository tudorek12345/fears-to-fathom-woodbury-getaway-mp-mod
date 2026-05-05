using System;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem.ChatMapper;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public static class ChatMapperToDialogueDatabase
{
	public static DialogueDatabase ConvertToDialogueDatabase(ChatMapperProject chatMapperProject)
	{
		DialogueDatabase dialogueDatabase = DatabaseUtility.CreateDialogueDatabaseInstance();
		if (dialogueDatabase == null)
		{
			if (DialogueDebug.logErrors)
			{
				Debug.LogError(string.Format("{0}: Couldn't convert Chat Mapper project '{1}'.", new object[2] { "Dialogue System", chatMapperProject.Title }));
			}
		}
		else
		{
			ConvertProjectAttributes(chatMapperProject, dialogueDatabase);
			ConvertActors(chatMapperProject, dialogueDatabase);
			ConvertItems(chatMapperProject, dialogueDatabase);
			ConvertLocations(chatMapperProject, dialogueDatabase);
			ConvertVariables(chatMapperProject, dialogueDatabase);
			ConvertConversations(chatMapperProject, dialogueDatabase);
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Converted Chat Mapper project '{1}' containing {2} actors, {3} conversations, {4} items (quests), {5} variables, and {6} locations.", "Dialogue System", chatMapperProject.Title, dialogueDatabase.actors.Count, dialogueDatabase.conversations.Count, dialogueDatabase.items.Count, dialogueDatabase.variables.Count, dialogueDatabase.locations.Count));
			}
		}
		return dialogueDatabase;
	}

	private static void ConvertProjectAttributes(ChatMapperProject chatMapperProject, DialogueDatabase database)
	{
		database.version = chatMapperProject.Version;
		database.author = chatMapperProject.Author;
		database.description = chatMapperProject.Description;
		database.emphasisSettings = new EmphasisSetting[4];
		database.emphasisSettings[0] = new EmphasisSetting(chatMapperProject.EmphasisColor1, chatMapperProject.EmphasisStyle1);
		database.emphasisSettings[1] = new EmphasisSetting(chatMapperProject.EmphasisColor2, chatMapperProject.EmphasisStyle2);
		database.emphasisSettings[2] = new EmphasisSetting(chatMapperProject.EmphasisColor3, chatMapperProject.EmphasisStyle3);
		database.emphasisSettings[3] = new EmphasisSetting(chatMapperProject.EmphasisColor4, chatMapperProject.EmphasisStyle4);
	}

	private static void ConvertActors(ChatMapperProject chatMapperProject, DialogueDatabase database)
	{
		database.actors = new List<Actor>();
		foreach (PixelCrushers.DialogueSystem.ChatMapper.Actor actor in chatMapperProject.Assets.Actors)
		{
			database.actors.Add(new Actor(actor));
		}
	}

	private static void ConvertItems(ChatMapperProject chatMapperProject, DialogueDatabase database)
	{
		database.items = new List<Item>();
		foreach (PixelCrushers.DialogueSystem.ChatMapper.Item item in chatMapperProject.Assets.Items)
		{
			database.items.Add(new Item(item));
		}
	}

	private static void ConvertLocations(ChatMapperProject chatMapperProject, DialogueDatabase database)
	{
		database.locations = new List<Location>();
		foreach (PixelCrushers.DialogueSystem.ChatMapper.Location location in chatMapperProject.Assets.Locations)
		{
			database.locations.Add(new Location(location));
		}
	}

	private static void ConvertVariables(ChatMapperProject chatMapperProject, DialogueDatabase database)
	{
		database.variables = new List<Variable>();
		int num = 0;
		foreach (UserVariable userVariable in chatMapperProject.Assets.UserVariables)
		{
			Variable variable = new Variable(userVariable);
			variable.id = num;
			num++;
			database.variables.Add(variable);
		}
	}

	private static void ConvertConversations(ChatMapperProject chatMapperProject, DialogueDatabase database)
	{
		database.conversations = new List<Conversation>();
		foreach (PixelCrushers.DialogueSystem.ChatMapper.Conversation conversation2 in chatMapperProject.Assets.Conversations)
		{
			Conversation conversation = new Conversation(conversation2);
			SetConversationStartCutsceneToNone(conversation);
			ConvertAudioFilesToSequences(conversation);
			ConvertOverridesFieldsToDisplaySettingsOverrides(conversation);
			foreach (DialogueEntry dialogueEntry in conversation.dialogueEntries)
			{
				foreach (Link outgoingLink in dialogueEntry.outgoingLinks)
				{
					if (outgoingLink.destinationConversationID == 0)
					{
						outgoingLink.destinationConversationID = conversation.id;
					}
					if (outgoingLink.originConversationID == 0)
					{
						outgoingLink.originConversationID = conversation.id;
					}
				}
			}
			database.conversations.Add(conversation);
		}
		FixConversationsLinkedToFirstEntry(database);
	}

	private static void SetConversationStartCutsceneToNone(Conversation conversation)
	{
		DialogueEntry firstDialogueEntry = conversation.GetFirstDialogueEntry();
		if (firstDialogueEntry == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Conversation '{1}' doesn't have a START dialogue entry.", new object[2] { "Dialogue System", conversation.Title }));
			}
		}
		else if (string.IsNullOrEmpty(firstDialogueEntry.currentSequence))
		{
			if (Field.FieldExists(firstDialogueEntry.fields, "Sequence"))
			{
				firstDialogueEntry.currentSequence = "None()";
			}
			else
			{
				firstDialogueEntry.fields.Add(new Field("Sequence", "None()", FieldType.Text));
			}
		}
	}

	public static void FixConversationsLinkedToFirstEntry(DialogueDatabase database, bool resetNodePositions = false)
	{
		try
		{
			List<int> list = new List<int>();
			foreach (Conversation conversation in database.conversations)
			{
				foreach (DialogueEntry dialogueEntry in conversation.dialogueEntries)
				{
					if (resetNodePositions)
					{
						dialogueEntry.canvasRect = new Rect(0f, 0f, 160f, 30f);
					}
					foreach (Link outgoingLink in dialogueEntry.outgoingLinks)
					{
						if (outgoingLink.destinationDialogueID == 0 && !list.Contains(outgoingLink.destinationConversationID))
						{
							list.Add(outgoingLink.destinationConversationID);
						}
					}
				}
			}
			foreach (int item in list)
			{
				DialogueEntry firstDialogueEntry = database.GetConversation(item).GetFirstDialogueEntry();
				int actorID = firstDialogueEntry.ActorID;
				firstDialogueEntry.ActorID = firstDialogueEntry.ConversantID;
				firstDialogueEntry.ConversantID = actorID;
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("Error fixing up linked conversation: " + ex.Message);
		}
	}

	public static void ConvertAudioFilesToSequences(Conversation conversation)
	{
		if (conversation == null || conversation.dialogueEntries == null)
		{
			return;
		}
		foreach (DialogueEntry dialogueEntry in conversation.dialogueEntries)
		{
			string audioFiles = dialogueEntry.AudioFiles;
			if (!string.IsNullOrEmpty(audioFiles) && !string.Equals("[]", audioFiles))
			{
				string text = audioFiles.Substring(1, audioFiles.IndexOfAny(new char[2] { ';', ']' }) - 1);
				text = text.Substring(0, text.LastIndexOf('.'));
				text = text.Replace("\\", "/");
				if (text.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase))
				{
					text = text.Substring(10);
				}
				string value = $"AudioWait({text})";
				if (dialogueEntry.currentSequence != null && !dialogueEntry.currentSequence.Contains(value))
				{
					dialogueEntry.currentSequence = $"AudioWait({text}); {dialogueEntry.currentSequence}";
				}
			}
		}
	}

	public static void ConvertOverridesFieldsToDisplaySettingsOverrides(Conversation conversation)
	{
		if (conversation == null)
		{
			return;
		}
		Field field = conversation.fields.Find((Field field2) => field2.title == "Overrides");
		if (field != null && !string.IsNullOrEmpty(field.value))
		{
			ConversationOverrideDisplaySettings conversationOverrideDisplaySettings = JsonUtility.FromJson<ConversationOverrideDisplaySettings>(field.value);
			if (conversationOverrideDisplaySettings != null)
			{
				conversation.overrideSettings = conversationOverrideDisplaySettings;
				conversation.fields.Remove(field);
			}
		}
	}
}
