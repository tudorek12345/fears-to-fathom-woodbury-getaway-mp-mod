using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[AddComponentMenu("")]
public class GUITextField : GUIVisibleControl
{
	public int maxLength;

	private bool takeFocus;

	protected override GUIStyle DefaultGUIStyle => GUI.skin.textField;

	public void TakeFocus()
	{
		takeFocus = true;
	}

	public override void DrawSelf(Vector2 relativeMousePosition)
	{
		SetGUIStyle();
		if (takeFocus)
		{
			GUI.SetNextControlName(base.FullName);
		}
		if (text == null)
		{
			text = string.Empty;
		}
		if (maxLength == 0)
		{
			text = GUI.TextField(base.rect, text, base.GuiStyle);
		}
		else
		{
			text = GUI.TextField(base.rect, text, maxLength, base.GuiStyle);
		}
		if (takeFocus)
		{
			GUI.FocusControl(base.FullName);
			takeFocus = false;
		}
	}
}
