using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public class VariablePopupAttribute : PropertyAttribute
{
	public bool showReferenceDatabase;

	public VariablePopupAttribute(bool showReferenceDatabase = false)
	{
		this.showReferenceDatabase = showReferenceDatabase;
	}
}
