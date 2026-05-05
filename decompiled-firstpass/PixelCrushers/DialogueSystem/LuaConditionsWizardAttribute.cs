using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public class LuaConditionsWizardAttribute : PropertyAttribute
{
	public bool showReferenceDatabase;

	public LuaConditionsWizardAttribute(bool showReferenceDatabase = false)
	{
		this.showReferenceDatabase = showReferenceDatabase;
	}
}
