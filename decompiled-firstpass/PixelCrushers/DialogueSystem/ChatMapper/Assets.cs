using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace PixelCrushers.DialogueSystem.ChatMapper;

[Serializable]
public class Assets
{
	[XmlArray("Actors")]
	[XmlArrayItem("Actor")]
	public List<Actor> Actors = new List<Actor>();

	[XmlArray("Items")]
	[XmlArrayItem("Item")]
	public List<Item> Items = new List<Item>();

	[XmlArray("Locations")]
	[XmlArrayItem("Location")]
	public List<Location> Locations = new List<Location>();

	[XmlArray("Conversations")]
	[XmlArrayItem("Conversation")]
	public List<Conversation> Conversations = new List<Conversation>();

	[XmlArray("UserVariables")]
	[XmlArrayItem("UserVariable")]
	public List<UserVariable> UserVariables = new List<UserVariable>();
}
