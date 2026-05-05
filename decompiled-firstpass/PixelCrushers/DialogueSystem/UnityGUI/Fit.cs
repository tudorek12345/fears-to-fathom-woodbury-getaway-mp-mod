using System;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[Serializable]
public class Fit
{
	public GUIControl above;

	public GUIControl below;

	public GUIControl leftOf;

	public GUIControl rightOf;

	public bool expandToFit = true;

	public bool IsSpecified
	{
		get
		{
			if (!(above != null) && !(below != null) && !(leftOf != null))
			{
				return rightOf != null;
			}
			return true;
		}
	}
}
