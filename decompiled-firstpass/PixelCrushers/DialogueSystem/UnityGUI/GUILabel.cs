using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[AddComponentMenu("")]
public class GUILabel : GUIVisibleControl
{
	public TextStyle textStyle;

	public Color textStyleColor = Color.black;

	public Color imageColor = Color.white;

	public Texture2D image;

	public ImageAnimation imageAnimation = new ImageAnimation();

	private List<string> closureTags = new List<string>();

	private bool useSubstring;

	private int substringLength;

	private int substringLengthLastGetRichTextClosedString;

	private string richTextClosedString = string.Empty;

	public int currentLength
	{
		get
		{
			return substringLength;
		}
		set
		{
			substringLength = value;
			useSubstring = !string.IsNullOrEmpty(text) && substringLength < text.Length;
		}
	}

	public override void Awake()
	{
		base.Awake();
		ResetClosureTags();
	}

	public void ResetClosureTags()
	{
		closureTags.Clear();
	}

	public void PushClosureTag(string tag)
	{
		closureTags.Add(tag);
	}

	public void PopClosureTag()
	{
		if (closureTags.Count > 0)
		{
			closureTags.RemoveAt(closureTags.Count - 1);
		}
	}

	public override void SetFormattedText(FormattedText formattedText)
	{
		base.SetFormattedText(formattedText);
		ResetClosureTags();
	}

	public override void DrawSelf(Vector2 relativeMousePosition)
	{
		ApplyAlphaToGUIColor();
		if (image != null)
		{
			DrawImage();
		}
		if (!string.IsNullOrEmpty(text))
		{
			if (useSubstring)
			{
				DrawSubstring();
			}
			else
			{
				UnityGUITools.DrawText(base.rect, text, base.GuiStyle, textStyle, textStyleColor);
			}
		}
		RestoreGUIColor();
	}

	private void DrawSubstring()
	{
		if (IsLeftAligned(base.GuiStyle.alignment))
		{
			DrawSubstringLeftAligned();
		}
		else
		{
			DrawSubstringNotLeftAligned();
		}
	}

	private bool IsLeftAligned(TextAnchor textAnchor)
	{
		if (textAnchor != TextAnchor.LowerLeft && textAnchor != TextAnchor.MiddleLeft)
		{
			return textAnchor == TextAnchor.UpperLeft;
		}
		return true;
	}

	private bool IsCenterAligned(TextAnchor textAnchor)
	{
		if (textAnchor != TextAnchor.LowerCenter && textAnchor != TextAnchor.MiddleCenter)
		{
			return textAnchor == TextAnchor.UpperCenter;
		}
		return true;
	}

	private TextAnchor GetLeftAlignedVersion(TextAnchor textAnchor)
	{
		switch (textAnchor)
		{
		case TextAnchor.LowerCenter:
		case TextAnchor.LowerRight:
			return TextAnchor.LowerLeft;
		case TextAnchor.MiddleCenter:
		case TextAnchor.MiddleRight:
			return TextAnchor.MiddleLeft;
		case TextAnchor.UpperCenter:
		case TextAnchor.UpperRight:
			return TextAnchor.UpperLeft;
		default:
			return textAnchor;
		}
	}

	private void DrawSubstringLeftAligned()
	{
		string text = string.Empty;
		int num = 0;
		int num2 = substringLength;
		while (num2 > 0)
		{
			string nextLine = GetNextLine(base.text, num);
			string text2 = nextLine.Substring(0, Mathf.Min(nextLine.Length, num2));
			num += nextLine.Length;
			num2 -= nextLine.Length;
			text = ((!string.IsNullOrEmpty(text)) ? (text + "\n" + text2) : text2);
		}
		UnityGUITools.DrawText(base.rect, GetRichTextClosedText(text), base.GuiStyle, textStyle, textStyleColor);
	}

	private void DrawSubstringNotLeftAligned()
	{
		TextAnchor alignment = base.GuiStyle.alignment;
		bool wordWrap = base.GuiStyle.wordWrap;
		base.GuiStyle.alignment = GetLeftAlignedVersion(base.GuiStyle.alignment);
		base.GuiStyle.wordWrap = false;
		float y = base.GuiStyle.CalcSize(new GUIContent(base.text)).y;
		float num = base.rect.y;
		int num2 = 0;
		int num3 = substringLength;
		while (num3 > 0)
		{
			string nextLine = GetNextLine(base.text, num2);
			string text = nextLine.Substring(0, Mathf.Min(nextLine.Length, num3));
			num2 += nextLine.Length;
			num3 -= nextLine.Length;
			float x = base.GuiStyle.CalcSize(new GUIContent(nextLine.Trim())).x;
			UnityGUITools.DrawText(new Rect(IsCenterAligned(alignment) ? (Mathf.Ceil(base.rect.x + 0.5f * base.rect.width - 0.5f * x) + 0.5f) : (base.rect.x + base.rect.width - x), num, base.rect.width, y), GetRichTextClosedText(text.Trim()), base.GuiStyle, textStyle, textStyleColor);
			num += base.GuiStyle.lineHeight;
		}
		base.GuiStyle.alignment = alignment;
		base.GuiStyle.wordWrap = wordWrap;
	}

	private string GetNextLine(string text, int start)
	{
		string text2 = text.Substring(start);
		int num = 0;
		if (base.GuiStyle.CalcSize(new GUIContent(text2.Trim())).x > base.rect.width)
		{
			for (int i = 1; start + i < text.Length; i++)
			{
				string text3 = text.Substring(start, i + 1);
				if (text[start + i] == ' ')
				{
					num = i;
				}
				if (!(base.GuiStyle.CalcSize(new GUIContent(text3.Trim())).x < base.rect.width))
				{
					int length = text2.Length;
					if (num > 0)
					{
						return text.Substring(start, Mathf.Max(1, Mathf.Min(num, length)));
					}
					return text.Substring(start, Mathf.Max(1, Mathf.Min(i - 1, length)));
				}
			}
		}
		return text2;
	}

	private string GetRichTextClosedText(string s)
	{
		if (closureTags.Count == 0)
		{
			return s;
		}
		if (substringLength != substringLengthLastGetRichTextClosedString)
		{
			substringLengthLastGetRichTextClosedString = substringLength;
			StringBuilder stringBuilder = new StringBuilder(s);
			for (int num = closureTags.Count - 1; num >= 0; num--)
			{
				stringBuilder.Append(closureTags[num]);
			}
			richTextClosedString = stringBuilder.ToString();
		}
		return richTextClosedString;
	}

	private void DrawImage()
	{
		Color color = GUI.color;
		GUI.color = imageColor;
		if (imageAnimation.animate)
		{
			imageAnimation.DrawAnimation(base.rect, image);
		}
		else
		{
			GUI.Label(base.rect, image, base.GuiStyle);
		}
		GUI.color = color;
	}

	public override void Refresh()
	{
		base.Refresh();
		if (imageAnimation.animate)
		{
			imageAnimation.RefreshAnimation(image);
		}
	}
}
