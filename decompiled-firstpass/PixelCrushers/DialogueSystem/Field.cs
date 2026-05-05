using System;
using System.Collections.Generic;
using System.Globalization;
using PixelCrushers.DialogueSystem.ChatMapper;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class Field
{
	public string title;

	public string value;

	public FieldType type;

	public string typeString = string.Empty;

	private static readonly List<string> filenameFields = new List<string> { "Pictures", "Texture Files", "Model Files", "Audio Files", "Lipsync Files", "Animation Files" };

	public Field()
	{
	}

	public Field(PixelCrushers.DialogueSystem.ChatMapper.Field chatMapperField)
	{
		Assign(chatMapperField);
	}

	public Field(string title, string value, FieldType type)
	{
		this.title = title;
		this.value = ((filenameFields.Contains(title) && value != null) ? value.Replace('\\', '/') : value);
		this.type = type;
		typeString = GetTypeString(type);
	}

	public Field(string title, string value, FieldType type, string typeString)
	{
		this.title = title;
		this.value = ((filenameFields.Contains(title) && value != null) ? value.Replace('\\', '/') : value);
		this.type = type;
		this.typeString = typeString;
	}

	public Field(Field sourceField)
	{
		title = sourceField.title;
		value = sourceField.value;
		type = sourceField.type;
		typeString = sourceField.typeString;
	}

	public void Assign(PixelCrushers.DialogueSystem.ChatMapper.Field chatMapperField)
	{
		if (chatMapperField != null)
		{
			title = chatMapperField.Title;
			value = ((filenameFields.Contains(title) && chatMapperField.Value != null) ? chatMapperField.Value.Replace('\\', '/') : chatMapperField.Value);
			type = StringToFieldType(chatMapperField.Type);
			typeString = GetTypeString(type);
		}
	}

	public static FieldType StringToFieldType(string chatMapperType)
	{
		if (string.Equals(chatMapperType, "Text"))
		{
			return FieldType.Text;
		}
		if (string.Equals(chatMapperType, "Number"))
		{
			return FieldType.Number;
		}
		if (string.Equals(chatMapperType, "Boolean"))
		{
			return FieldType.Boolean;
		}
		if (string.Equals(chatMapperType, "Files"))
		{
			return FieldType.Files;
		}
		if (string.Equals(chatMapperType, "Localization"))
		{
			return FieldType.Localization;
		}
		if (string.Equals(chatMapperType, "Actor"))
		{
			return FieldType.Actor;
		}
		if (string.Equals(chatMapperType, "Item"))
		{
			return FieldType.Item;
		}
		if (string.Equals(chatMapperType, "Location"))
		{
			return FieldType.Location;
		}
		if (string.Equals(chatMapperType, "Multiline"))
		{
			return FieldType.Text;
		}
		if (DialogueDebug.logWarnings)
		{
			Debug.LogError(string.Format("{0}: Unrecognized Chat Mapper type: {1}", new object[2] { "Dialogue System", chatMapperType }));
		}
		return FieldType.Text;
	}

	public static List<Field> CreateListFromChatMapperFields(List<PixelCrushers.DialogueSystem.ChatMapper.Field> chatMapperFields)
	{
		List<Field> list = new List<Field>();
		if (chatMapperFields != null)
		{
			foreach (PixelCrushers.DialogueSystem.ChatMapper.Field chatMapperField in chatMapperFields)
			{
				list.Add(new Field(chatMapperField));
			}
		}
		return list;
	}

	public static List<Field> CopyFields(List<Field> sourceFields)
	{
		List<Field> list = new List<Field>();
		foreach (Field sourceField in sourceFields)
		{
			list.Add(new Field(sourceField));
		}
		return list;
	}

	public static bool FieldExists(List<Field> fields, string title)
	{
		return Lookup(fields, title) != null;
	}

	public static Field Lookup(List<Field> fields, string title)
	{
		return fields?.Find((Field f) => string.Equals(f.title, title));
	}

	public static string LookupValue(List<Field> fields, string title)
	{
		return Lookup(fields, title)?.value;
	}

	public static string LookupLocalizedValue(List<Field> fields, string title)
	{
		if (Localization.isDefaultLanguage)
		{
			return LookupValue(fields, title);
		}
		string result = LookupValue(fields, title + " " + Localization.language);
		if (!string.IsNullOrEmpty(result))
		{
			return result;
		}
		result = LookupValue(fields, title + "_" + Localization.language);
		if (!string.IsNullOrEmpty(result))
		{
			return result;
		}
		return LookupValue(fields, title);
	}

	public static int LookupInt(List<Field> fields, string title)
	{
		return Tools.StringToInt(LookupValue(fields, title));
	}

	public static float LookupFloat(List<Field> fields, string title)
	{
		return Tools.StringToFloat(LookupValue(fields, title));
	}

	public static bool LookupBool(List<Field> fields, string title)
	{
		return Tools.StringToBool(LookupValue(fields, title));
	}

	public static void SetValue(List<Field> fields, string title, string value, FieldType type)
	{
		Field field = Lookup(fields, title);
		if (field != null)
		{
			field.value = value;
			field.type = type;
		}
		else
		{
			fields.Add(new Field(title, value, type));
		}
	}

	public static void SetValue(List<Field> fields, string title, string value)
	{
		SetValue(fields, title, value, FieldType.Text);
	}

	public static void SetValue(List<Field> fields, string title, float value)
	{
		SetValue(fields, title, value.ToString(CultureInfo.InvariantCulture), FieldType.Number);
	}

	public static void SetValue(List<Field> fields, string title, int value)
	{
		SetValue(fields, title, value.ToString(), FieldType.Number);
	}

	public static void SetValue(List<Field> fields, string title, bool value)
	{
		SetValue(fields, title, value.ToString(), FieldType.Boolean);
	}

	public static bool IsFieldAssigned(List<Field> fields, string title)
	{
		return AssignedField(fields, title) != null;
	}

	public static Field AssignedField(List<Field> fields, string title)
	{
		Field field = Lookup(fields, title);
		if (field == null || string.IsNullOrEmpty(field.value))
		{
			return null;
		}
		return field;
	}

	public static string FieldValue(Field field)
	{
		return field?.value;
	}

	public static string LocalizedTitle(string title)
	{
		if (!Localization.isDefaultLanguage)
		{
			return string.Format("{0} {1}", new object[2]
			{
				title,
				Localization.language
			});
		}
		return title;
	}

	public static string GetTypeString(FieldType type)
	{
		return type switch
		{
			FieldType.Actor => "CustomFieldType_Actor", 
			FieldType.Boolean => "CustomFieldType_Boolean", 
			FieldType.Files => "CustomFieldType_Files", 
			FieldType.Item => "CustomFieldType_Item", 
			FieldType.Localization => "CustomFieldType_Localization", 
			FieldType.Location => "CustomFieldType_Location", 
			FieldType.Number => "CustomFieldType_Number", 
			_ => string.Empty, 
		};
	}
}
