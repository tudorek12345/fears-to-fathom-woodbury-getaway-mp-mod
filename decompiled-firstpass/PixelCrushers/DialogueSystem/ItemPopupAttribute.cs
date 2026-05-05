using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public class ItemPopupAttribute : PropertyAttribute
{
	public bool showReferenceDatabase;

	public ItemPopupAttribute(bool showReferenceDatabase = false)
	{
		this.showReferenceDatabase = showReferenceDatabase;
	}
}
