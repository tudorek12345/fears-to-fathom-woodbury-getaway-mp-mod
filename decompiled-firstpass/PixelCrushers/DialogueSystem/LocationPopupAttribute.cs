using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public class LocationPopupAttribute : PropertyAttribute
{
	public bool showReferenceDatabase;

	public LocationPopupAttribute(bool showReferenceDatabase = false)
	{
		this.showReferenceDatabase = showReferenceDatabase;
	}
}
