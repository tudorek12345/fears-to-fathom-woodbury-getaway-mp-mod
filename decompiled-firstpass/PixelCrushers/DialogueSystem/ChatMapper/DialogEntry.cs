using System.Collections.Generic;
using System.Xml.Serialization;

namespace PixelCrushers.DialogueSystem.ChatMapper;

public class DialogEntry
{
	[XmlAttribute("ID")]
	public int ID;

	[XmlAttribute("IsRoot")]
	public bool IsRoot;

	[XmlAttribute("IsGroup")]
	public bool IsGroup;

	[XmlAttribute("NodeColor")]
	public string NodeColor;

	[XmlAttribute("DelaySimStatus")]
	public bool DelaySimStatus;

	[XmlAttribute("FalseCondtionAction")]
	public string FalseCondtionAction;

	[XmlAttribute("ConditionPriority")]
	public string ConditionPriority;

	[XmlArray("Fields")]
	[XmlArrayItem("Field")]
	public List<Field> Fields = new List<Field>();

	[XmlArray("OutgoingLinks")]
	[XmlArrayItem("Link")]
	public List<Link> OutgoingLinks = new List<Link>();

	public string ConditionsString;

	public string UserScript;
}
