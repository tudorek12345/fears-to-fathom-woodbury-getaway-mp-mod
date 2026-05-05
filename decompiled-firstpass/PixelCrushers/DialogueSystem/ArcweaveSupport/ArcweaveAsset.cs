using System;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem.ArcweaveSupport;

[Serializable]
public class ArcweaveAsset : ArcweaveType
{
	public string name;

	public string type;

	public bool root;

	public List<string> children;
}
