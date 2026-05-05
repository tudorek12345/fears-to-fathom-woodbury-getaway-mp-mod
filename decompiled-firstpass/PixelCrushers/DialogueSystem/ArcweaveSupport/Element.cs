using System;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem.ArcweaveSupport;

[Serializable]
public class Element : ArcweaveType
{
	public string theme;

	public string title;

	public Assets assets;

	public string content;

	public List<string> outputs;

	public List<string> components;

	public List<string> attributes;

	public string linkedBoard;
}
