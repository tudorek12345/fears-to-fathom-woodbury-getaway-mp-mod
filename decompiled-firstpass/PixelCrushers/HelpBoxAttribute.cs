using UnityEngine;

namespace PixelCrushers;

public class HelpBoxAttribute : PropertyAttribute
{
	public string text;

	public HelpBoxMessageType messageType;

	public HelpBoxAttribute(string text, HelpBoxMessageType messageType = HelpBoxMessageType.None)
	{
		this.text = text;
		this.messageType = messageType;
	}
}
