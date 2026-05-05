using System.Xml.Serialization;

namespace PixelCrushers.DialogueSystem.ChatMapper;

[XmlRoot("ChatMapperProject")]
public class ChatMapperProject
{
	[XmlAttribute("Title")]
	public string Title;

	[XmlAttribute("Version")]
	public string Version;

	[XmlAttribute("Author")]
	public string Author;

	[XmlAttribute("EmphasisColor1Label")]
	public string EmphasisColor1Label = string.Empty;

	[XmlAttribute("EmphasisColor1")]
	public string EmphasisColor1;

	[XmlAttribute("EmphasisStyle1")]
	public string EmphasisStyle1;

	[XmlAttribute("EmphasisColor2Label")]
	public string EmphasisColor2Label = string.Empty;

	[XmlAttribute("EmphasisColor2")]
	public string EmphasisColor2;

	[XmlAttribute("EmphasisStyle2")]
	public string EmphasisStyle2;

	[XmlAttribute("EmphasisColor3Label")]
	public string EmphasisColor3Label = string.Empty;

	[XmlAttribute("EmphasisColor3")]
	public string EmphasisColor3;

	[XmlAttribute("EmphasisStyle3")]
	public string EmphasisStyle3;

	[XmlAttribute("EmphasisColor4Label")]
	public string EmphasisColor4Label = string.Empty;

	[XmlAttribute("EmphasisColor4")]
	public string EmphasisColor4;

	[XmlAttribute("EmphasisStyle4")]
	public string EmphasisStyle4;

	public string Description;

	public string UserScript;

	public Assets Assets;

	public DialogueDatabase ToDialogueDatabase()
	{
		return ChatMapperToDialogueDatabase.ConvertToDialogueDatabase(this);
	}
}
