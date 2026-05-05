using System;

namespace PixelCrushers.DialogueSystem;

[AttributeUsage(AttributeTargets.Class)]
public class SequencerCommandGroupAttribute : Attribute
{
	public string submenu;

	public SequencerCommandGroupAttribute(string submenu)
	{
		this.submenu = submenu;
	}
}
