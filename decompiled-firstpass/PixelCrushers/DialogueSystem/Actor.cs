using System;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem.ChatMapper;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class Actor : Asset
{
	public delegate void AssignSpriteDelegate(Sprite sprite);

	public Texture2D portrait;

	public Sprite spritePortrait;

	public List<Texture2D> alternatePortraits = new List<Texture2D>();

	public List<Sprite> spritePortraits = new List<Sprite>();

	public bool IsPlayer
	{
		get
		{
			return LookupBool("IsPlayer");
		}
		set
		{
			Field.SetValue(fields, "IsPlayer", value);
		}
	}

	public string textureName
	{
		get
		{
			return LookupTextureName();
		}
		set
		{
			SetTextureName(value);
		}
	}

	public string TextureName
	{
		get
		{
			return textureName;
		}
		set
		{
			textureName = value;
		}
	}

	public Actor()
	{
	}

	public Actor(Actor sourceActor)
		: base(sourceActor)
	{
		portrait = sourceActor.portrait;
		alternatePortraits = new List<Texture2D>(sourceActor.alternatePortraits);
		spritePortrait = sourceActor.spritePortrait;
		spritePortraits = new List<Sprite>(sourceActor.spritePortraits);
	}

	public Actor(PixelCrushers.DialogueSystem.ChatMapper.Actor chatMapperActor)
	{
		Assign(chatMapperActor);
	}

	public void Assign(PixelCrushers.DialogueSystem.ChatMapper.Actor chatMapperActor)
	{
		if (chatMapperActor != null)
		{
			Assign(chatMapperActor.ID, chatMapperActor.Fields);
		}
	}

	public Sprite GetPortraitSprite(int i)
	{
		if (i == 1)
		{
			return UITools.GetSprite(portrait, spritePortrait);
		}
		int num = i - 2;
		return UITools.GetSprite((0 <= num && num < alternatePortraits.Count) ? alternatePortraits[num] : null, (0 <= num && num < spritePortraits.Count) ? spritePortraits[num] : null);
	}

	public Sprite GetPortraitSprite()
	{
		DialogueDebug.DebugLevel level = DialogueDebug.level;
		DialogueDebug.level = DialogueDebug.DebugLevel.Warning;
		string asString = DialogueLua.GetActorField(base.Name, "Current Portrait").asString;
		DialogueDebug.level = level;
		if (string.IsNullOrEmpty(asString))
		{
			return GetPortraitSprite(1);
		}
		if (asString.StartsWith("pic="))
		{
			return GetPortraitSprite(Tools.StringToInt(asString.Substring("pic=".Length)));
		}
		Sprite portraitSprite = GetPortraitSprite(asString);
		if (!(portraitSprite != null))
		{
			return UITools.CreateSprite(DialogueManager.LoadAsset(asString) as Texture2D);
		}
		return portraitSprite;
	}

	public Sprite GetPortraitSprite(string imageName)
	{
		if (string.IsNullOrEmpty(imageName))
		{
			return null;
		}
		if (spritePortrait != null && spritePortrait.name == imageName)
		{
			return spritePortrait;
		}
		if (portrait != null && portrait.name == imageName)
		{
			return UITools.CreateSprite(portrait);
		}
		Sprite sprite = spritePortraits.Find((Sprite x) => x != null && x.name == imageName);
		if (sprite != null)
		{
			return sprite;
		}
		Texture2D texture2D = alternatePortraits.Find((Texture2D x) => x != null && x.name == imageName);
		if (texture2D != null)
		{
			return UITools.CreateSprite(texture2D);
		}
		return null;
	}

	public void AssignPortraitSprite(AssignSpriteDelegate assignSprite)
	{
		DialogueDebug.DebugLevel level = DialogueDebug.level;
		DialogueDebug.level = DialogueDebug.DebugLevel.Warning;
		string asString = DialogueLua.GetActorField(base.Name, "Current Portrait").asString;
		DialogueDebug.level = level;
		if (string.IsNullOrEmpty(asString))
		{
			assignSprite(GetPortraitSprite(1));
			return;
		}
		if (asString.StartsWith("pic="))
		{
			assignSprite(GetPortraitSprite(Tools.StringToInt(asString.Substring("pic=".Length))));
			return;
		}
		DialogueManager.LoadAsset(asString, typeof(Texture2D), delegate(UnityEngine.Object asset)
		{
			assignSprite(UITools.CreateSprite(asset as Texture2D));
		});
	}

	private string LookupTextureName()
	{
		Field field = Field.Lookup(fields, "Pictures");
		if (field == null || field.value == null)
		{
			return null;
		}
		string[] array = field.value.Split('[', ';', ']');
		if (array.Length < 2)
		{
			return null;
		}
		return array[1];
	}

	private void SetTextureName(string value)
	{
		Field.SetValue(fields, "Pictures", "[" + value + "]");
	}
}
