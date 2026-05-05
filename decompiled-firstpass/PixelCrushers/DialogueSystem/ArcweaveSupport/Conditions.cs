using System;
using Newtonsoft.Json.Linq;

namespace PixelCrushers.DialogueSystem.ArcweaveSupport;

[Serializable]
public class Conditions : ArcweaveType
{
	public string ifCondition;

	public JToken elseIfConditions;

	public string elseCondition;
}
