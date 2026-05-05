using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public class LuaScriptWizardAttribute : PropertyAttribute
{
	public bool showReferenceDatabase;

	public LuaScriptWizardAttribute(bool showReferenceDatabase = false)
	{
		this.showReferenceDatabase = showReferenceDatabase;
	}
}
