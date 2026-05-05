using System;
using PixelCrushers.DialogueSystem.ChatMapper;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class Item : Asset
{
	public bool IsItem
	{
		get
		{
			return LookupBool("Is Item");
		}
		set
		{
			Field.SetValue(fields, "Is Item", value);
		}
	}

	public string Group
	{
		get
		{
			return LookupValue("Group");
		}
		set
		{
			Field.SetValue(fields, "Group", value);
		}
	}

	public Item()
	{
	}

	public Item(Item sourceItem)
		: base(sourceItem)
	{
	}

	public Item(PixelCrushers.DialogueSystem.ChatMapper.Item chatMapperItem)
	{
		Assign(chatMapperItem);
	}

	public void Assign(PixelCrushers.DialogueSystem.ChatMapper.Item chatMapperItem)
	{
		if (chatMapperItem != null)
		{
			Assign(chatMapperItem.ID, chatMapperItem.Fields);
		}
	}
}
