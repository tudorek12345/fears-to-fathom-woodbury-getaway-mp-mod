using System;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem.ArcweaveSupport;

[Serializable]
public class ArcweaveComponent : ArcweaveType
{
	public string name;

	public bool root;

	public List<string> children;

	public Assets assets;

	public List<string> attributes;
}
