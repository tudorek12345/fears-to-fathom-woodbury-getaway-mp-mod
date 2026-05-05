using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public class ActorPopupAttribute : PropertyAttribute
{
	public bool showReferenceDatabase;

	public ActorPopupAttribute(bool showReferenceDatabase = false)
	{
		this.showReferenceDatabase = showReferenceDatabase;
	}
}
