using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public class QuestPopupAttribute : PropertyAttribute
{
	public bool showReferenceDatabase;

	public QuestPopupAttribute(bool showReferenceDatabase = false)
	{
		this.showReferenceDatabase = showReferenceDatabase;
	}
}
