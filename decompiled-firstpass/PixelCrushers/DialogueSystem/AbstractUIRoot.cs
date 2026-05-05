using System;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public abstract class AbstractUIRoot
{
	public abstract void Show();

	public abstract void Hide();
}
