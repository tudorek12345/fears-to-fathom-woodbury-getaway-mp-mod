using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class EntryGroup
{
	public string name;

	public Rect rect;

	public Color color;

	public EntryGroup()
	{
	}

	public EntryGroup(string name, Rect rect)
	{
		this.name = name;
		this.rect = rect;
		color = new Color(1f, 1f, 1f, 0.5f);
	}

	public EntryGroup(EntryGroup source)
	{
		name = source.name;
		rect = source.rect;
		color = source.color;
	}
}
