using System.Collections.Generic;
using System.Xml.Serialization;

namespace PixelCrushers.DialogueSystem.ChatMapper;

public class Conversation
{
	[XmlAttribute("ID")]
	public int ID;

	[XmlAttribute("NodeColor")]
	public string NodeColor;

	[XmlAttribute("LockedMode")]
	public string LockedMode;

	[XmlArray("Fields")]
	[XmlArrayItem("Field")]
	public List<Field> Fields = new List<Field>();

	[XmlArray("DialogEntries")]
	[XmlArrayItem("DialogEntry")]
	public List<DialogEntry> DialogEntries = new List<DialogEntry>();
}
