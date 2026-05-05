using System;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[Serializable]
public class ScaledValue
{
	public static readonly ScaledValue zero = new ScaledValue(ValueScale.Pixel, 0f);

	public static readonly ScaledValue max = new ScaledValue(ValueScale.Normalized, 1f);

	public ValueScale scale;

	public float value;

	public ScaledValue(ValueScale scale, float value)
	{
		this.scale = scale;
		this.value = value;
	}

	public ScaledValue(ScaledValue source)
	{
		if (source != null)
		{
			scale = source.scale;
			value = source.value;
		}
	}

	public ScaledValue()
	{
	}

	public float GetPixelValue(float windowSize)
	{
		if (scale == ValueScale.Pixel)
		{
			return value;
		}
		return value * windowSize;
	}

	public static ScaledValue FromPixelValue(float value)
	{
		return new ScaledValue(ValueScale.Pixel, value);
	}

	public static ScaledValue FromNormalizedValue(float value)
	{
		return new ScaledValue(ValueScale.Normalized, value);
	}
}
