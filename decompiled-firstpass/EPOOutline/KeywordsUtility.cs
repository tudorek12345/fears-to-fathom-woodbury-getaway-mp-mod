using System;
using System.Collections.Generic;

namespace EPOOutline;

public static class KeywordsUtility
{
	private static Dictionary<BlurType, string> BlurTypes = new Dictionary<BlurType, string>
	{
		{
			BlurType.Anisotropic,
			"ANISOTROPIC_BLUR"
		},
		{
			BlurType.Box,
			"BOX_BLUR"
		},
		{
			BlurType.Gaussian5x5,
			"GAUSSIAN5X5"
		},
		{
			BlurType.Gaussian9x9,
			"GAUSSIAN9X9"
		},
		{
			BlurType.Gaussian13x13,
			"GAUSSIAN13X13"
		}
	};

	private static Dictionary<DilateQuality, string> DilateQualityKeywords = new Dictionary<DilateQuality, string>
	{
		{
			DilateQuality.Base,
			"BASE_QALITY_DILATE"
		},
		{
			DilateQuality.High,
			"HIGH_QUALITY_DILATE"
		},
		{
			DilateQuality.Ultra,
			"ULTRA_QUALITY_DILATE"
		}
	};

	public static string GetBackKeyword(ComplexMaskingMode mode)
	{
		return mode switch
		{
			ComplexMaskingMode.None => string.Empty, 
			ComplexMaskingMode.ObstaclesMode => "BACK_OBSTACLE_RENDERING", 
			ComplexMaskingMode.MaskingMode => "BACK_MASKING_RENDERING", 
			_ => throw new ArgumentException("Unknown rendering mode"), 
		};
	}

	public static string GetTextureArrayCutoutKeyword()
	{
		return "TEXARRAY_CUTOUT";
	}

	public static string GetDilateQualityKeyword(DilateQuality quality)
	{
		return quality switch
		{
			DilateQuality.Base => "BASE_QALITY_DILATE", 
			DilateQuality.High => "HIGH_QUALITY_DILATE", 
			DilateQuality.Ultra => "ULTRA_QUALITY_DILATE", 
			_ => throw new Exception("Unknown dilate quality level"), 
		};
	}

	public static string GetEnabledInfoBufferKeyword()
	{
		return "USE_INFO_BUFFER";
	}

	public static string GetEdgeMaskKeyword()
	{
		return "EDGE_MASK";
	}

	public static string GetInfoBufferStageKeyword()
	{
		return "INFO_BUFFER_STAGE";
	}

	public static string GetBlurKeyword(BlurType type)
	{
		return BlurTypes[type];
	}

	public static string GetCutoutKeyword()
	{
		return "USE_CUTOUT";
	}

	public static void GetAllBlurKeywords(List<string> list)
	{
		list.Clear();
		foreach (KeyValuePair<BlurType, string> blurType in BlurTypes)
		{
			list.Add(blurType.Value);
		}
	}

	public static void GetAllDilateKeywords(List<string> list)
	{
		list.Clear();
		foreach (KeyValuePair<DilateQuality, string> dilateQualityKeyword in DilateQualityKeywords)
		{
			list.Add(dilateQualityKeyword.Value);
		}
	}
}
