using UnityEngine;

namespace PixelCrushers;

public class StringFieldTextAreaAttribute : PropertyAttribute
{
	public bool expandHeight;

	public StringFieldTextAreaAttribute(bool expandHeight = true)
	{
		this.expandHeight = expandHeight;
	}
}
