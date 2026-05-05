using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class Template
{
	public bool treatItemsAsQuests = true;

	public List<Field> actorFields = new List<Field>();

	public List<Field> itemFields = new List<Field>();

	public List<Field> questFields = new List<Field>();

	public List<Field> locationFields = new List<Field>();

	public List<Field> variableFields = new List<Field>();

	public List<Field> conversationFields = new List<Field>();

	public List<Field> dialogueEntryFields = new List<Field>();

	public List<string> actorPrimaryFieldTitles = new List<string>();

	public List<string> itemPrimaryFieldTitles = new List<string>();

	public List<string> questPrimaryFieldTitles = new List<string>();

	public List<string> locationPrimaryFieldTitles = new List<string>();

	public List<string> variablePrimaryFieldTitles = new List<string>();

	public List<string> conversationPrimaryFieldTitles = new List<string>();

	public List<string> dialogueEntryPrimaryFieldTitles = new List<string>();

	public Color npcLineColor = Color.red;

	public Color pcLineColor = Color.blue;

	public Color repeatLineColor = Color.gray;

	public static Template FromDefault()
	{
		Template template = new Template();
		template.actorFields.Clear();
		template.actorFields.Add(new Field("Name", string.Empty, FieldType.Text));
		template.actorFields.Add(new Field("Pictures", "[]", FieldType.Files));
		template.actorFields.Add(new Field("Description", string.Empty, FieldType.Text));
		template.actorFields.Add(new Field("IsPlayer", "False", FieldType.Boolean));
		template.itemFields.Clear();
		template.itemFields.Add(new Field("Name", string.Empty, FieldType.Text));
		template.itemFields.Add(new Field("Pictures", "[]", FieldType.Files));
		template.itemFields.Add(new Field("Description", string.Empty, FieldType.Text));
		template.itemFields.Add(new Field("Is Item", "True", FieldType.Boolean));
		template.questFields.Clear();
		template.questFields.Add(new Field("Name", string.Empty, FieldType.Text));
		template.questFields.Add(new Field("Pictures", "[]", FieldType.Files));
		template.questFields.Add(new Field("Description", string.Empty, FieldType.Text));
		template.questFields.Add(new Field("Success Description", string.Empty, FieldType.Text));
		template.questFields.Add(new Field("Failure Description", string.Empty, FieldType.Text));
		template.questFields.Add(new Field("State", "unassigned", FieldType.Text));
		template.questFields.Add(new Field("Is Item", "False", FieldType.Boolean));
		template.locationFields.Clear();
		template.locationFields.Add(new Field("Name", string.Empty, FieldType.Text));
		template.locationFields.Add(new Field("Description", string.Empty, FieldType.Text));
		template.variableFields.Add(new Field("Name", string.Empty, FieldType.Text));
		template.variableFields.Add(new Field("Initial Value", string.Empty, FieldType.Text));
		template.variableFields.Add(new Field("Description", string.Empty, FieldType.Text));
		template.conversationFields.Add(new Field("Title", string.Empty, FieldType.Text));
		template.conversationFields.Add(new Field("Description", string.Empty, FieldType.Text));
		template.conversationFields.Add(new Field("Actor", "0", FieldType.Actor));
		template.conversationFields.Add(new Field("Conversant", "0", FieldType.Actor));
		template.dialogueEntryFields.Add(new Field("Title", string.Empty, FieldType.Text));
		template.dialogueEntryFields.Add(new Field("Description", string.Empty, FieldType.Text));
		template.dialogueEntryFields.Add(new Field("Actor", string.Empty, FieldType.Actor));
		template.dialogueEntryFields.Add(new Field("Conversant", string.Empty, FieldType.Actor));
		template.dialogueEntryFields.Add(new Field("Menu Text", string.Empty, FieldType.Text));
		template.dialogueEntryFields.Add(new Field("Dialogue Text", string.Empty, FieldType.Text));
		template.dialogueEntryFields.Add(new Field("Sequence", string.Empty, FieldType.Text));
		return template;
	}

	public Actor CreateActor(int id, string name, bool isPlayer)
	{
		return new Actor
		{
			fields = CreateFields(actorFields),
			id = id,
			Name = name,
			IsPlayer = isPlayer
		};
	}

	public Item CreateItem(int id, string name)
	{
		return new Item
		{
			id = id,
			fields = CreateFields(itemFields),
			Name = name
		};
	}

	public Item CreateQuest(int id, string name)
	{
		return new Item
		{
			id = id,
			fields = CreateFields(questFields),
			Name = name
		};
	}

	public Location CreateLocation(int id, string name)
	{
		return new Location
		{
			id = id,
			fields = CreateFields(locationFields),
			Name = name
		};
	}

	public Variable CreateVariable(int id, string name, string value)
	{
		return new Variable
		{
			fields = CreateFields(variableFields),
			id = id,
			Name = name,
			InitialValue = value
		};
	}

	public Variable CreateVariable(int id, string name, string value, FieldType type)
	{
		return new Variable
		{
			fields = CreateFields(variableFields),
			id = id,
			Name = name,
			InitialValue = value,
			Type = type
		};
	}

	public Conversation CreateConversation(int id, string title)
	{
		return new Conversation
		{
			id = id,
			fields = CreateFields(conversationFields),
			Title = title
		};
	}

	public DialogueEntry CreateDialogueEntry(int id, int conversationID, string title)
	{
		return new DialogueEntry
		{
			fields = CreateFields(dialogueEntryFields),
			id = id,
			conversationID = conversationID,
			Title = title
		};
	}

	public List<Field> CreateFields(List<Field> templateFields)
	{
		List<Field> list = new List<Field>();
		foreach (Field templateField in templateFields)
		{
			list.Add(new Field(templateField.title, templateField.value, templateField.type, templateField.typeString));
		}
		return list;
	}

	public int GetNextActorID(DialogueDatabase database)
	{
		if (!(database != null))
		{
			return 0;
		}
		return GetNextAssetID(database.baseID, database.actors);
	}

	public int GetNextItemID(DialogueDatabase database)
	{
		if (!(database != null))
		{
			return 0;
		}
		return GetNextAssetID(database.baseID, database.items);
	}

	public int GetNextQuestID(DialogueDatabase database)
	{
		return GetNextItemID(database);
	}

	public int GetNextLocationID(DialogueDatabase database)
	{
		if (!(database != null))
		{
			return 0;
		}
		return GetNextAssetID(database.baseID, database.locations);
	}

	public int GetNextVariableID(DialogueDatabase database)
	{
		if (!(database != null))
		{
			return 0;
		}
		return GetNextAssetID(database.baseID, database.variables);
	}

	public int GetNextConversationID(DialogueDatabase database)
	{
		if (!(database != null))
		{
			return 0;
		}
		return GetNextAssetID(database.baseID, database.conversations);
	}

	private int GetNextAssetID<T>(int baseID, List<T> assets) where T : Asset
	{
		int num = baseID - 1;
		for (int i = 0; i < assets.Count; i++)
		{
			num = Mathf.Max(num, assets[i].id);
		}
		return num + 1;
	}

	public int GetNextDialogueEntryID(Conversation conversation)
	{
		if (conversation == null)
		{
			return 0;
		}
		int num = -1;
		for (int i = 0; i < conversation.dialogueEntries.Count; i++)
		{
			num = Mathf.Max(num, conversation.dialogueEntries[i].id);
		}
		return num + 1;
	}
}
