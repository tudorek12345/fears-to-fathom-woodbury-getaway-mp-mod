using System;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem.ChatMapper;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class Asset
{
	public int id;

	public List<Field> fields;

	public string Name
	{
		get
		{
			return Field.LookupValue(fields, "Name");
		}
		set
		{
			Field.SetValue(fields, "Name", value);
		}
	}

	public string localizedName => Field.LookupLocalizedValue(fields, "Name");

	public string Description
	{
		get
		{
			return Field.LookupValue(fields, "Description");
		}
		set
		{
			Field.SetValue(fields, "Description", value);
		}
	}

	public string LocalizedName => localizedName;

	public Asset()
	{
	}

	public Asset(Asset sourceAsset)
	{
		id = sourceAsset.id;
		fields = Field.CopyFields(sourceAsset.fields);
	}

	public Asset(int chatMapperID, List<PixelCrushers.DialogueSystem.ChatMapper.Field> chatMapperFields)
	{
		Assign(chatMapperID, chatMapperFields);
	}

	public void Assign(int chatMapperID, List<PixelCrushers.DialogueSystem.ChatMapper.Field> chatMapperFields)
	{
		id = chatMapperID;
		fields = Field.CreateListFromChatMapperFields(chatMapperFields);
	}

	public bool FieldExists(string title)
	{
		return Field.FieldExists(fields, title);
	}

	public string LookupValue(string title)
	{
		return Field.LookupValue(fields, title);
	}

	public string LookupLocalizedValue(string title)
	{
		return Field.LookupLocalizedValue(fields, title);
	}

	public int LookupInt(string title)
	{
		return Field.LookupInt(fields, title);
	}

	public float LookupFloat(string title)
	{
		return Field.LookupFloat(fields, title);
	}

	public bool LookupBool(string title)
	{
		return Field.LookupBool(fields, title);
	}

	public bool IsFieldAssigned(string title)
	{
		return Field.IsFieldAssigned(fields, title);
	}

	public Field AssignedField(string title)
	{
		return Field.AssignedField(fields, title);
	}
}
