using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace PixelCrushers.DialogueSystem.ChatMapper;

[Serializable]
public class Actor
{
	[XmlAttribute("ID")]
	public int ID;

	[XmlArray("Fields")]
	[XmlArrayItem("Field")]
	public List<Field> Fields = new List<Field>();
}
