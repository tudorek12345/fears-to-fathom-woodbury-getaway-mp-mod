using System;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem.ArcweaveSupport;

[Serializable]
public class ArcweaveProject
{
	public string name;

	public Cover cover;

	public string startingElement;

	public Dictionary<string, Board> boards;

	public Dictionary<string, Note> notes;

	public Dictionary<string, Element> elements;

	public Dictionary<string, Jumper> jumpers;

	public Dictionary<string, Connection> connections;

	public Dictionary<string, Branch> branches;

	public Dictionary<string, ArcweaveComponent> components;

	public Dictionary<string, Attribute> attributes;

	public Dictionary<string, ArcweaveAsset> assets;

	public Dictionary<string, ArcweaveVariable> variables;

	public Dictionary<string, Condition> conditions;
}
