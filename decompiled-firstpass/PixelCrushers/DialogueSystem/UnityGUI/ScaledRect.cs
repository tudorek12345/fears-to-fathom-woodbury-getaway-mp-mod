using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[Serializable]
public class ScaledRect
{
	public static readonly ScaledRect empty = new ScaledRect(ScaledRectAlignment.TopLeft, ScaledRectAlignment.TopLeft, ScaledValue.zero, ScaledValue.zero, ScaledValue.zero, ScaledValue.zero);

	public static readonly ScaledRect wholeScreen = new ScaledRect(ScaledRectAlignment.TopLeft, ScaledRectAlignment.TopLeft, ScaledValue.zero, ScaledValue.zero, ScaledValue.max, ScaledValue.max);

	public ScaledRectAlignment origin;

	public ScaledRectAlignment alignment;

	public ScaledValue x;

	public ScaledValue y;

	public ScaledValue width;

	public ScaledValue height;

	public float minPixelWidth;

	public float minPixelHeight;

	public ScaledRect(ScaledRectAlignment origin, ScaledRectAlignment alignment, ScaledValue x, ScaledValue y, ScaledValue width, ScaledValue height, float minPixelWidth = 0f, float minPixelHeight = 0f)
	{
		this.origin = origin;
		this.alignment = alignment;
		this.x = x;
		this.y = y;
		this.width = width;
		this.height = height;
		this.minPixelWidth = minPixelWidth;
		this.minPixelHeight = minPixelHeight;
	}

	public ScaledRect(ScaledRect source)
	{
		if (source != null)
		{
			origin = source.origin;
			alignment = source.alignment;
			x = new ScaledValue(source.x);
			y = new ScaledValue(source.y);
			width = new ScaledValue(source.width);
			height = new ScaledValue(source.height);
			minPixelWidth = source.minPixelWidth;
			minPixelHeight = source.minPixelHeight;
		}
	}

	public ScaledRect()
	{
	}

	public static ScaledRect FromOrigin(ScaledRectAlignment origin, ScaledValue width, ScaledValue height, float minPixelWidth = 0f, float minPixelHeight = 0f)
	{
		return new ScaledRect(origin, origin, ScaledValue.zero, ScaledValue.zero, width, height, minPixelWidth, minPixelHeight);
	}

	public Rect GetPixelRect()
	{
		return GetPixelRect(new Vector2(Screen.width, Screen.height), Vector2.zero);
	}

	public Rect GetPixelRect(Vector2 windowSize)
	{
		return GetPixelRect(windowSize, Vector2.zero);
	}

	public Rect GetPixelRect(Vector2 windowSize, Vector2 defaultSize)
	{
		float num = Mathf.Max(width.GetPixelValue(windowSize.x), minPixelWidth);
		float num2 = Mathf.Max(height.GetPixelValue(windowSize.y), minPixelHeight);
		Vector2 pixelOrigin = GetPixelOrigin(windowSize);
		Vector2 alignmentFactor = GetAlignmentFactor();
		float num3 = pixelOrigin.x + num * alignmentFactor.x + x.GetPixelValue(windowSize.x);
		float num4 = pixelOrigin.y + num2 * alignmentFactor.y + y.GetPixelValue(windowSize.y);
		return new Rect(num3, num4, num, num2);
	}

	private Vector2 GetPixelOrigin(Vector2 windowSize)
	{
		return origin switch
		{
			ScaledRectAlignment.TopLeft => Vector2.zero, 
			ScaledRectAlignment.TopCenter => new Vector2(0.5f * windowSize.x, 0f), 
			ScaledRectAlignment.TopRight => new Vector2(windowSize.x, 0f), 
			ScaledRectAlignment.MiddleLeft => new Vector2(0f, 0.5f * windowSize.y), 
			ScaledRectAlignment.MiddleCenter => new Vector2(0.5f * windowSize.x, 0.5f * windowSize.y), 
			ScaledRectAlignment.MiddleRight => new Vector2(windowSize.x, 0.5f * windowSize.y), 
			ScaledRectAlignment.BottomLeft => new Vector2(0f, windowSize.y), 
			ScaledRectAlignment.BottomCenter => new Vector2(0.5f * windowSize.x, windowSize.y), 
			ScaledRectAlignment.BottomRight => windowSize, 
			_ => Vector2.zero, 
		};
	}

	private Vector2 GetAlignmentFactor()
	{
		return alignment switch
		{
			ScaledRectAlignment.TopLeft => Vector2.zero, 
			ScaledRectAlignment.TopCenter => new Vector2(-0.5f, 0f), 
			ScaledRectAlignment.TopRight => new Vector2(-1f, 0f), 
			ScaledRectAlignment.MiddleLeft => new Vector2(0f, -0.5f), 
			ScaledRectAlignment.MiddleCenter => new Vector2(-0.5f, -0.5f), 
			ScaledRectAlignment.MiddleRight => new Vector2(-1f, -0.5f), 
			ScaledRectAlignment.BottomLeft => new Vector2(0f, -1f), 
			ScaledRectAlignment.BottomCenter => new Vector2(-0.5f, -1f), 
			ScaledRectAlignment.BottomRight => new Vector2(-1f, -1f), 
			_ => Vector2.zero, 
		};
	}
}
