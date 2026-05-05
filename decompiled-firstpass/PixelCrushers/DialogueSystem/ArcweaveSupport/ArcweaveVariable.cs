using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace PixelCrushers.DialogueSystem.ArcweaveSupport;

[Serializable]
public class ArcweaveVariable : ArcweaveType
{
	public bool root;

	public List<string> children;

	public string name;

	public string type;

	public JToken value;
}
