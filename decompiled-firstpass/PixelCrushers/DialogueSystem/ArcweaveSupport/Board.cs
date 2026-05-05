using System;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem.ArcweaveSupport;

[Serializable]
public class Board : ArcweaveType
{
	public string name;

	public bool root;

	public List<string> children;

	public List<string> notes;

	public List<string> jumpers;

	public List<string> elements;

	public List<string> connections;

	public List<string> branches;
}
