using System;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem.ChatMapper;
using UnityEngine;
using UnityEngine.Events;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class DialogueEntry
{
	public int id;

	public List<Field> fields;

	public int conversationID;

	public bool isRoot;

	public bool isGroup;

	public string nodeColor;

	public bool delaySimStatus;

	public string falseConditionAction;

	public ConditionPriority conditionPriority = ConditionPriority.Normal;

	public List<Link> outgoingLinks = new List<Link>();

	public string conditionsString;

	public string userScript;

	public UnityEvent onExecute = new UnityEvent();

	public const float CanvasRectWidth = 160f;

	public const float CanvasRectHeight = 30f;

	public Rect canvasRect = new Rect(0f, 0f, 160f, 30f);

	public const string SceneEventGuidFieldName = "EventGuid";

	public int ActorID
	{
		get
		{
			return Field.LookupInt(fields, "Actor");
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
			return Field.LookupInt(fields, "Conversant");
		}
		set
		{
			Field.SetValue(fields, "Conversant", value.ToString(), FieldType.Actor);
		}
	}

	public string Title
	{
		get
		{
			return Field.LookupLocalizedValue(fields, "Title");
		}
		set
		{
			Field.SetValue(fields, "Title", value);
		}
	}

	public string currentMenuText
	{
		get
		{
			return Field.FieldValue(GetCurrentMenuTextField());
		}
		set
		{
			Field currentMenuTextField = GetCurrentMenuTextField();
			if (currentMenuTextField != null)
			{
				currentMenuTextField.value = value;
			}
		}
	}

	public string MenuText
	{
		get
		{
			return Field.LookupValue(fields, "Menu Text");
		}
		set
		{
			Field.SetValue(fields, "Menu Text", value);
		}
	}

	public string currentLocalizedMenuText
	{
		get
		{
			return Field.LookupValue(fields, Field.LocalizedTitle("Menu Text"));
		}
		set
		{
			Field.SetValue(fields, Field.LocalizedTitle("Menu Text"), value);
		}
	}

	public string currentDialogueText
	{
		get
		{
			return Field.FieldValue(GetCurrentDialogueTextField());
		}
		set
		{
			Field currentDialogueTextField = GetCurrentDialogueTextField();
			if (currentDialogueTextField != null)
			{
				currentDialogueTextField.value = value;
			}
		}
	}

	public string DialogueText
	{
		get
		{
			return Field.LookupValue(fields, "Dialogue Text");
		}
		set
		{
			Field.SetValue(fields, "Dialogue Text", value);
		}
	}

	public string currentLocalizedDialogueText
	{
		get
		{
			return Field.LookupValue(fields, Localization.language);
		}
		set
		{
			Field.SetValue(fields, Localization.language, value);
		}
	}

	public string subtitleText
	{
		get
		{
			if (!string.IsNullOrEmpty(currentDialogueText))
			{
				return currentDialogueText;
			}
			return currentMenuText;
		}
	}

	public string responseButtonText
	{
		get
		{
			if (!string.IsNullOrEmpty(currentMenuText))
			{
				return currentMenuText;
			}
			return currentDialogueText;
		}
	}

	public string currentSequence
	{
		get
		{
			return Field.FieldValue(GetCurrentSequenceField());
		}
		set
		{
			SetCurrentSequenceField(value);
		}
	}

	public string Sequence
	{
		get
		{
			return Field.LookupValue(fields, "Sequence");
		}
		set
		{
			SetTextField("Sequence", value);
		}
	}

	public string currentLocalizedSequence
	{
		get
		{
			return Field.LookupValue(fields, Field.LocalizedTitle("Sequence"));
		}
		set
		{
			SetTextField(Field.LocalizedTitle("Sequence"), value);
		}
	}

	public string sceneEventGuid
	{
		get
		{
			return Field.LookupValue(fields, "EventGuid");
		}
		set
		{
			SetTextField("EventGuid", value);
		}
	}

	public string currentResponseMenuSequence
	{
		get
		{
			return Field.FieldValue(GetCurrentResponseMenuSequenceField());
		}
		set
		{
			SetCurrentResponseMenuSequenceField(value);
		}
	}

	public string ResponseMenuSequence
	{
		get
		{
			return Field.LookupValue(fields, "Response Menu Sequence");
		}
		set
		{
			SetTextField("Response Menu Sequence", value);
		}
	}

	public string currentLocalizedResponseMenuSequence
	{
		get
		{
			return Field.LookupValue(fields, Field.LocalizedTitle("Response Menu Sequence"));
		}
		set
		{
			SetTextField(Field.LocalizedTitle("Response Menu Sequence"), value);
		}
	}

	public string VideoFile
	{
		get
		{
			return Field.LookupValue(fields, "Video File");
		}
		set
		{
			Field.SetValue(fields, "Video File", value);
		}
	}

	public string AudioFiles
	{
		get
		{
			return Field.LookupValue(fields, "Audio Files");
		}
		set
		{
			Field.SetValue(fields, "Audio Files", value);
		}
	}

	public string AnimationFiles
	{
		get
		{
			return Field.LookupValue(fields, "Animation Files");
		}
		set
		{
			Field.SetValue(fields, "Animation Files", value);
		}
	}

	public string LipsyncFiles
	{
		get
		{
			return Field.LookupValue(fields, "Lipsync Files");
		}
		set
		{
			Field.SetValue(fields, "Lipsync Files", value);
		}
	}

	private Field GetCurrentMenuTextField()
	{
		return Field.AssignedField(fields, Field.LocalizedTitle("Menu Text")) ?? Field.Lookup(fields, "Menu Text");
	}

	private Field GetCurrentDialogueTextField()
	{
		if (string.IsNullOrEmpty(Localization.language))
		{
			return Field.Lookup(fields, "Dialogue Text");
		}
		return Field.AssignedField(fields, Localization.language) ?? Field.Lookup(fields, "Dialogue Text");
	}

	private Field GetCurrentSequenceField()
	{
		return Field.AssignedField(fields, Field.LocalizedTitle("Sequence")) ?? Field.Lookup(fields, "Sequence");
	}

	private void SetCurrentSequenceField(string value)
	{
		Field currentSequenceField = GetCurrentSequenceField();
		if (currentSequenceField == null && Localization.isDefaultLanguage)
		{
			fields.Add(new Field("Sequence", value, FieldType.Text));
		}
		else if (currentSequenceField != null)
		{
			currentSequenceField.value = value;
		}
	}

	private void SetTextField(string title, string value)
	{
		Field field = Field.Lookup(fields, title);
		if (field != null)
		{
			field.value = value;
		}
		else
		{
			fields.Add(new Field(title, value, FieldType.Text));
		}
	}

	public bool HasResponseMenuSequence()
	{
		return Field.FieldExists(fields, "Response Menu Sequence");
	}

	private Field GetCurrentResponseMenuSequenceField()
	{
		return Field.AssignedField(fields, Field.LocalizedTitle("Response Menu Sequence")) ?? Field.Lookup(fields, "Response Menu Sequence");
	}

	private void SetCurrentResponseMenuSequenceField(string value)
	{
		Field currentSequenceField = GetCurrentSequenceField();
		if (currentSequenceField == null && Localization.isDefaultLanguage)
		{
			fields.Add(new Field("Response Menu Sequence", value, FieldType.Text));
		}
		else if (currentSequenceField != null)
		{
			currentSequenceField.value = value;
		}
	}

	public DialogueEntry()
	{
	}

	public DialogueEntry(DialogEntry chatMapperDialogEntry)
	{
		if (chatMapperDialogEntry == null)
		{
			return;
		}
		id = chatMapperDialogEntry.ID;
		fields = Field.CreateListFromChatMapperFields(chatMapperDialogEntry.Fields);
		UseCanvasRectField();
		isRoot = chatMapperDialogEntry.IsRoot;
		isGroup = chatMapperDialogEntry.IsGroup;
		if (isGroup)
		{
			Sequence = "";
		}
		nodeColor = chatMapperDialogEntry.NodeColor;
		delaySimStatus = chatMapperDialogEntry.DelaySimStatus;
		falseConditionAction = chatMapperDialogEntry.FalseCondtionAction;
		conditionPriority = ConditionPriorityUtility.StringToConditionPriority(chatMapperDialogEntry.ConditionPriority);
		foreach (PixelCrushers.DialogueSystem.ChatMapper.Link outgoingLink in chatMapperDialogEntry.OutgoingLinks)
		{
			outgoingLinks.Add(new Link(outgoingLink));
		}
		conditionsString = chatMapperDialogEntry.ConditionsString;
		userScript = chatMapperDialogEntry.UserScript;
	}

	public void UseCanvasRectField()
	{
		Field field = Field.Lookup(fields, "canvasRect");
		if (field != null && !string.IsNullOrEmpty(field.value))
		{
			string[] array = field.value.Split(';');
			float num = ((array.Length >= 1) ? Tools.StringToFloat(array[0]) : 0f);
			float num2 = ((array.Length >= 2) ? Tools.StringToFloat(array[1]) : 0f);
			if (num > 0f && num2 > 0f)
			{
				canvasRect = new Rect(num, num2, canvasRect.width, canvasRect.height);
			}
			fields.Remove(field);
		}
	}

	public DialogueEntry(DialogueEntry sourceEntry)
	{
		id = sourceEntry.id;
		fields = Field.CopyFields(sourceEntry.fields);
		conversationID = sourceEntry.conversationID;
		isRoot = sourceEntry.isRoot;
		isGroup = sourceEntry.isGroup;
		nodeColor = sourceEntry.nodeColor;
		delaySimStatus = sourceEntry.delaySimStatus;
		falseConditionAction = sourceEntry.falseConditionAction;
		conditionPriority = ConditionPriority.Normal;
		outgoingLinks = CopyLinks(sourceEntry.outgoingLinks);
		conditionsString = sourceEntry.conditionsString;
		userScript = sourceEntry.userScript;
		canvasRect = sourceEntry.canvasRect;
	}

	private List<Link> CopyLinks(List<Link> sourceLinks)
	{
		List<Link> list = new List<Link>();
		foreach (Link sourceLink in sourceLinks)
		{
			list.Add(new Link(sourceLink));
		}
		return list;
	}
}
