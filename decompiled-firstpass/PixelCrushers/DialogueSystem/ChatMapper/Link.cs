using System.Xml.Serialization;

namespace PixelCrushers.DialogueSystem.ChatMapper;

public class Link
{
	[XmlAttribute("ConversationID")]
	public int ConversationID;

	[XmlAttribute("OriginConvoID")]
	public int OriginConvoID;

	[XmlAttribute("DestinationConvoID")]
	public int DestinationConvoID;

	[XmlAttribute("OriginDialogID")]
	public int OriginDialogID;

	[XmlAttribute("DestinationDialogID")]
	public int DestinationDialogID;

	[XmlAttribute("IsConnector")]
	public bool IsConnector;
}
