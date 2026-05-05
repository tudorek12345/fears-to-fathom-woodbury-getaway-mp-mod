using System;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class DialogueEntrySceneEvent
{
	public string guid = string.Empty;

	public GameObjectUnityEvent onExecute = new GameObjectUnityEvent();
}
