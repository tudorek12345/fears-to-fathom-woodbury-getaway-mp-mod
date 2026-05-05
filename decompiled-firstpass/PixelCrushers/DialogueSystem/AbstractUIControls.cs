using System;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public abstract class AbstractUIControls
{
	public abstract void SetActive(bool value);

	public void Show()
	{
		SetActive(value: true);
	}

	public void Hide()
	{
		SetActive(value: false);
	}
}
