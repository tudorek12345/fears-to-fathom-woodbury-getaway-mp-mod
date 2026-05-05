using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class CustomLuaFunctionInfoRecord
{
	[Tooltip("Use forward slashes to group into submenus.")]
	public string functionName;

	public CustomLuaParameterType[] parameters;

	public CustomLuaReturnType returnValue;
}
