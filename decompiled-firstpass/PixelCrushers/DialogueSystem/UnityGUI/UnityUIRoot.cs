using System;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[Serializable]
public class UnityUIRoot : AbstractUIRoot
{
	private GUIRoot guiRoot;

	public UnityUIRoot(GUIRoot guiRoot)
	{
		this.guiRoot = guiRoot;
	}

	public override void Show()
	{
		if (guiRoot != null)
		{
			guiRoot.gameObject.SetActive(value: true);
			guiRoot.ManualRefresh();
		}
	}

	public override void Hide()
	{
		if (guiRoot != null)
		{
			guiRoot.gameObject.SetActive(value: false);
		}
	}
}
