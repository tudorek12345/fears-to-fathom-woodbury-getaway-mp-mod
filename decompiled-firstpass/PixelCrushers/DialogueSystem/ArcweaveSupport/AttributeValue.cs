using System;
using Newtonsoft.Json.Linq;

namespace PixelCrushers.DialogueSystem.ArcweaveSupport;

[Serializable]
public class AttributeValue : ArcweaveType
{
	public JToken data;

	public string type;

	public bool plain;
}
