using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[ExecuteInEditMode]
[AddComponentMenu("")]
public class GUIRoot : GUIControl
{
	public GUISkin guiSkin;

	public void OnGUI()
	{
		UseGUISkin();
		if (!Application.isPlaying)
		{
			ManualRefresh();
		}
		Vector2 relativeMousePosition = new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y);
		Draw(relativeMousePosition);
	}

	public void ManualRefresh()
	{
		Refresh(new Vector2(Screen.width, Screen.height));
	}

	private void UseGUISkin()
	{
		if (guiSkin != null)
		{
			GUI.skin = guiSkin;
		}
	}
}
