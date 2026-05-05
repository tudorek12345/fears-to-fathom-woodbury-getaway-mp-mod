using System;

namespace PixelCrushers.DialogueSystem.ArcweaveSupport;

[Serializable]
public class Connection : ArcweaveType
{
	public string type;

	public string label;

	public string theme;

	public string sourceid;

	public string targetid;

	public string sourcetype;

	public string targettype;
}
