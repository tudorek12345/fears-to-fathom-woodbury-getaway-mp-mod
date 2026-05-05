using System;

namespace PixelCrushers.DialogueSystem.ArcweaveSupport;

[Serializable]
public class Branch : ArcweaveType
{
	public string theme;

	public Conditions conditions;
}
