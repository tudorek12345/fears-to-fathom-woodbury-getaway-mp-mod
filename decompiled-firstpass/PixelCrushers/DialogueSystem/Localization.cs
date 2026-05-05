using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public static class Localization
{
	private static string m_language = string.Empty;

	private static int m_currentLanguageID = -1;

	private static bool m_useDefaultIfUndefined = true;

	public static string language
	{
		get
		{
			return m_language;
		}
		set
		{
			m_language = value;
			m_currentLanguageID = -1;
			if (DialogueManager.instance != null)
			{
				UILocalizationManager uILocalizationManager = DialogueManager.instance.GetComponent<UILocalizationManager>();
				if (uILocalizationManager == null)
				{
					uILocalizationManager = DialogueManager.instance.gameObject.AddComponent<UILocalizationManager>();
					uILocalizationManager.textTable = DialogueManager.instance.displaySettings.localizationSettings.textTable;
				}
				uILocalizationManager.currentLanguage = value;
			}
		}
	}

	public static bool isDefaultLanguage => string.IsNullOrEmpty(language);

	public static bool useDefaultIfUndefined
	{
		get
		{
			return m_useDefaultIfUndefined;
		}
		set
		{
			m_useDefaultIfUndefined = value;
		}
	}

	public static string Language
	{
		get
		{
			return language;
		}
		set
		{
			language = value;
		}
	}

	public static bool IsDefaultLanguage => isDefaultLanguage;

	public static bool UseDefaultIfUndefined
	{
		get
		{
			return useDefaultIfUndefined;
		}
		set
		{
			useDefaultIfUndefined = value;
		}
	}

	public static int GetCurrentLanguageID(TextTable textTable)
	{
		if (m_currentLanguageID == -1 && textTable != null)
		{
			m_currentLanguageID = textTable.GetLanguageID(language);
		}
		if (m_currentLanguageID != -1)
		{
			return m_currentLanguageID;
		}
		return 0;
	}

	public static string GetLanguage(SystemLanguage systemLanguage)
	{
		return systemLanguage switch
		{
			SystemLanguage.Afrikaans => "af", 
			SystemLanguage.Arabic => "ar", 
			SystemLanguage.Basque => "eu", 
			SystemLanguage.Belarusian => "be", 
			SystemLanguage.Bulgarian => "bg", 
			SystemLanguage.Catalan => "ca", 
			SystemLanguage.Chinese => "zh", 
			SystemLanguage.Czech => "cs", 
			SystemLanguage.Danish => "da", 
			SystemLanguage.Dutch => "nl", 
			SystemLanguage.English => "en", 
			SystemLanguage.Estonian => "et", 
			SystemLanguage.Faroese => "fo", 
			SystemLanguage.Finnish => "fi", 
			SystemLanguage.French => "fr", 
			SystemLanguage.German => "de", 
			SystemLanguage.Greek => "el", 
			SystemLanguage.Hebrew => "he", 
			SystemLanguage.Hungarian => "hu", 
			SystemLanguage.Icelandic => "is", 
			SystemLanguage.Indonesian => "id", 
			SystemLanguage.Italian => "it", 
			SystemLanguage.Japanese => "ja", 
			SystemLanguage.Korean => "ko", 
			SystemLanguage.Latvian => "lv", 
			SystemLanguage.Lithuanian => "lt", 
			SystemLanguage.Norwegian => "no", 
			SystemLanguage.Polish => "pl", 
			SystemLanguage.Portuguese => "pt", 
			SystemLanguage.Romanian => "ro", 
			SystemLanguage.Russian => "ru", 
			SystemLanguage.SerboCroatian => "sr", 
			SystemLanguage.Slovak => "sk", 
			SystemLanguage.Slovenian => "sl", 
			SystemLanguage.Spanish => "es", 
			SystemLanguage.Swedish => "sv", 
			SystemLanguage.Thai => "th", 
			SystemLanguage.Turkish => "tr", 
			SystemLanguage.Ukrainian => "uk", 
			SystemLanguage.Vietnamese => "vi", 
			_ => null, 
		};
	}
}
