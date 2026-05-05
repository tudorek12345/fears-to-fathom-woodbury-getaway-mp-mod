using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

public static class UnityGUITools
{
	public static void DrawText(Rect rect, string text, GUIStyle guiStyle, TextStyle textStyle = TextStyle.None)
	{
		if (textStyle == TextStyle.Outline || textStyle == TextStyle.Shadow)
		{
			Color textColor = guiStyle.normal.textColor;
			guiStyle.normal.textColor = new Color(0f, 0f, 0f, guiStyle.normal.textColor.a);
			GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, guiStyle);
			if (textStyle == TextStyle.Outline)
			{
				GUI.Label(new Rect(rect.x + 1f, rect.y - 1f, rect.width, rect.height), text, guiStyle);
				GUI.Label(new Rect(rect.x - 1f, rect.y + 1f, rect.width, rect.height), text, guiStyle);
				GUI.Label(new Rect(rect.x - 1f, rect.y - 1f, rect.width, rect.height), text, guiStyle);
			}
			guiStyle.normal.textColor = textColor;
		}
		GUI.Label(rect, text, guiStyle);
	}

	public static void DrawText(Rect rect, string text, GUIStyle guiStyle, TextStyle textStyle, Color textStyleColor)
	{
		if (textStyle == TextStyle.Outline || textStyle == TextStyle.Shadow)
		{
			Color textColor = guiStyle.normal.textColor;
			guiStyle.normal.textColor = new Color(textStyleColor.r, textStyleColor.g, textStyleColor.b, guiStyle.normal.textColor.a);
			GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, guiStyle);
			if (textStyle == TextStyle.Outline)
			{
				GUI.Label(new Rect(rect.x + 1f, rect.y - 1f, rect.width, rect.height), text, guiStyle);
				GUI.Label(new Rect(rect.x - 1f, rect.y + 1f, rect.width, rect.height), text, guiStyle);
				GUI.Label(new Rect(rect.x - 1f, rect.y - 1f, rect.width, rect.height), text, guiStyle);
			}
			guiStyle.normal.textColor = textColor;
		}
		GUI.Label(rect, text, guiStyle);
	}

	public static GUISkin GetValidGUISkin(GUISkin guiSkin)
	{
		if (!(guiSkin != null))
		{
			return GetDialogueManagerGUISkin();
		}
		return guiSkin;
	}

	public static GUISkin GetDialogueManagerGUISkin()
	{
		UnityDialogueUI unityDialogueUI = DialogueManager.dialogueUI as UnityDialogueUI;
		if (!(unityDialogueUI != null) || !(unityDialogueUI.guiRoot != null) || !(unityDialogueUI.guiRoot.guiSkin != null))
		{
			return GUI.skin;
		}
		return unityDialogueUI.guiRoot.guiSkin;
	}

	public static GUIStyle GetGUIStyle(string guiStyleName, GUIStyle defaultGUIStyle)
	{
		return new GUIStyle((string.IsNullOrEmpty(guiStyleName) ? null : GUI.skin.FindStyle(guiStyleName)) ?? defaultGUIStyle);
	}

	public static Color ColorWithAlpha(Color color, float alpha)
	{
		return new Color(color.r, color.g, color.b, alpha);
	}

	public static FontStyle ApplyBold(FontStyle fontStyle)
	{
		if (fontStyle != FontStyle.Italic)
		{
			return FontStyle.Bold;
		}
		return FontStyle.BoldAndItalic;
	}

	public static FontStyle ApplyItalic(FontStyle fontStyle)
	{
		if (fontStyle != FontStyle.Bold)
		{
			return FontStyle.Italic;
		}
		return FontStyle.BoldAndItalic;
	}

	public static GUIStyle ApplyFormatting(FormattedText formattingToApply, GUIStyle guiStyle)
	{
		if (guiStyle != null && formattingToApply != null)
		{
			if (formattingToApply.italic)
			{
				guiStyle.fontStyle = ApplyItalic(guiStyle.fontStyle);
			}
			if (formattingToApply.emphases != null && formattingToApply.emphases.Length != 0)
			{
				guiStyle.normal.textColor = formattingToApply.emphases[0].color;
				if (formattingToApply.emphases[0].bold)
				{
					guiStyle.fontStyle = ApplyBold(guiStyle.fontStyle);
				}
				if (formattingToApply.emphases[0].italic)
				{
					guiStyle.fontStyle = ApplyItalic(guiStyle.fontStyle);
				}
			}
		}
		return guiStyle;
	}
}
