using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[AddComponentMenu("")]
public class GUIWindow : GUIVisibleControl
{
	private Vector2 currentChildMousePosition;

	protected override GUIStyle DefaultGUIStyle => GUI.skin.window;

	public override void DrawSelf(Vector2 relativeMousePosition)
	{
		SetGUIStyle();
		ApplyAlphaToGUIColor();
		currentChildMousePosition = new Vector2(relativeMousePosition.x - base.rect.x, relativeMousePosition.y - base.rect.y);
		Rect rect = GUI.Window(0, base.rect, WindowFunction, text, base.GuiStyle);
		RestoreGUIColor();
		base.rect = rect;
	}

	public override void DrawChildren(Vector2 relativeMousePosition)
	{
	}

	private void WindowFunction(int windowID)
	{
		GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
		foreach (GUIControl child in base.Children)
		{
			child.Draw(currentChildMousePosition);
		}
	}
}
