using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PixelCrushers;

public class LocalizedFonts : ScriptableObject
{
	[Serializable]
	public class FontForLanguage
	{
		public string language;

		public Font font;

		public TMP_FontAsset textMeshProFont;

		[Tooltip("Scale this language's font size relative to initial font's size. (0.5 scales to half size, 2.0 scales to double size.)")]
		public float scaleRelativeToInitialFont = 1f;
	}

	public Font defaultFont;

	public TMP_FontAsset defaultTextMeshProFont;

	public List<FontForLanguage> fontsForLanguages = new List<FontForLanguage>();

	public Font GetFont(string language)
	{
		FontForLanguage fontForLanguage = fontsForLanguages.Find((FontForLanguage x) => string.Equals(x.language, language));
		if (fontForLanguage == null || !(fontForLanguage.font != null))
		{
			return defaultFont;
		}
		return fontForLanguage.font;
	}

	public TMP_FontAsset GetTextMeshProFont(string language)
	{
		FontForLanguage fontForLanguage = fontsForLanguages.Find((FontForLanguage x) => string.Equals(x.language, language));
		if (fontForLanguage == null || !((UnityEngine.Object)(object)fontForLanguage.textMeshProFont != null))
		{
			return defaultTextMeshProFont;
		}
		return fontForLanguage.textMeshProFont;
	}

	public float GetFontScale(string language)
	{
		FontForLanguage fontForLanguage = fontsForLanguages.Find((FontForLanguage x) => string.Equals(x.language, language));
		if (fontForLanguage == null || !(fontForLanguage.font != null))
		{
			return 1f;
		}
		return fontForLanguage.scaleRelativeToInitialFont;
	}
}
