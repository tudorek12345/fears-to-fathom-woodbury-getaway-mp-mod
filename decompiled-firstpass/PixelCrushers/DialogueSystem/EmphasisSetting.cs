using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class EmphasisSetting
{
	public Color color = Color.black;

	public bool bold;

	public bool italic;

	public bool underline;

	public bool IsEmpty
	{
		get
		{
			if (color == Color.black && !bold && !italic)
			{
				return !underline;
			}
			return false;
		}
	}

	public EmphasisSetting(Color color, bool bold, bool italic, bool underline)
	{
		this.color = color;
		this.bold = bold;
		this.italic = italic;
		this.underline = underline;
	}

	public EmphasisSetting(string colorCode, string styleCode)
	{
		color = Tools.WebColor(colorCode);
		bold = !string.IsNullOrEmpty(styleCode) && styleCode.Length > 0 && styleCode[0] == 'b';
		italic = !string.IsNullOrEmpty(styleCode) && styleCode.Length > 1 && styleCode[1] == 'i';
		underline = !string.IsNullOrEmpty(styleCode) && styleCode.Length > 2 && styleCode[2] == 'u';
	}
}
