using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[Serializable]
public class AutoSize
{
	public bool autoSizeWidth;

	public bool autoSizeHeight;

	public ScaledValue maxWidth = new ScaledValue(ScaledValue.max);

	public ScaledValue maxHeight = new ScaledValue(ScaledValue.max);

	public RectOffset padding;

	public bool IsEnabled
	{
		get
		{
			if (!autoSizeWidth)
			{
				return autoSizeHeight;
			}
			return true;
		}
	}
}
