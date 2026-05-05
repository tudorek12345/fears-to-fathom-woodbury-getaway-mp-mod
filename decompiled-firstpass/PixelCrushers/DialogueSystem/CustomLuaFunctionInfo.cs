using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public class CustomLuaFunctionInfo : ScriptableObject
{
	[HelpBox("If you want your own custom Lua functions to appear in Conditions and Script dropdowns, add their info to this asset.", HelpBoxMessageType.None)]
	public CustomLuaFunctionInfoRecord[] conditionFunctions;

	public CustomLuaFunctionInfoRecord[] scriptFunctions;
}
