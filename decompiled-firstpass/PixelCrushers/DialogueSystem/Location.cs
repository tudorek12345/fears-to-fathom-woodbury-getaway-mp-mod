using System;
using PixelCrushers.DialogueSystem.ChatMapper;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class Location : Asset
{
	public Location()
	{
	}

	public Location(Location sourceLocation)
		: base(sourceLocation)
	{
	}

	public Location(PixelCrushers.DialogueSystem.ChatMapper.Location chatMapperLocation)
	{
		Assign(chatMapperLocation);
	}

	public void Assign(PixelCrushers.DialogueSystem.ChatMapper.Location chatMapperLocation)
	{
		if (chatMapperLocation != null)
		{
			Assign(chatMapperLocation.ID, chatMapperLocation.Fields);
		}
	}
}
