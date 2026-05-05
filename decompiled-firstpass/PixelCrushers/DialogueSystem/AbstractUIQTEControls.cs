using System;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public abstract class AbstractUIQTEControls : AbstractUIControls
{
	public abstract bool areVisible { get; }

	public abstract void ShowIndicator(int index);

	public abstract void HideIndicator(int index);
}
