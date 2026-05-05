using System.Xml.Serialization;

namespace PixelCrushers.DialogueSystem.ChatMapper;

public class Field
{
	[XmlAttribute("Hint")]
	public string Hint;

	[XmlAttribute("Type")]
	public string Type;

	public string Title;

	public string Value;
}
