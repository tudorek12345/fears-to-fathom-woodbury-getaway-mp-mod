using System.Collections.Generic;
using System.Xml.Serialization;

namespace PixelCrushers.DialogueSystem.ChatMapper;

public class UserVariable
{
	[XmlArray("Fields")]
	[XmlArrayItem("Field")]
	public List<Field> Fields = new List<Field>();
}
