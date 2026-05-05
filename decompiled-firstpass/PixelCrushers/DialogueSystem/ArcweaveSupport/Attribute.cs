using System;

namespace PixelCrushers.DialogueSystem.ArcweaveSupport;

[Serializable]
public class Attribute : ArcweaveType
{
	public string name;

	public AttributeValue value;
}
