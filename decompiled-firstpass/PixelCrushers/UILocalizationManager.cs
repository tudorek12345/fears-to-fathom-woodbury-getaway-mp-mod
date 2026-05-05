using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class UILocalizationManager : MonoBehaviour
{
	[Tooltip("The PlayerPrefs key to store the player's selected language code.")]
	[SerializeField]
	private string m_currentLanguagePlayerPrefsKey = "Language";

	[Tooltip("Overrides the global text table.")]
	[SerializeField]
	private TextTable m_textTable;

	[Tooltip("Any additional text tables.")]
	[SerializeField]
	private List<TextTable> m_additionalTextTables;

	[Tooltip("Table of fonts to use for specific languages.")]
	[SerializeField]
	private LocalizedFonts m_localizedFonts;

	[Tooltip("When starting, set current language to value saved in PlayerPrefs.")]
	[SerializeField]
	private bool m_saveLanguageInPlayerPrefs = true;

	[Tooltip("When updating UIs, perform longer search that also finds LocalizeUI components on inactive GameObjects.")]
	[SerializeField]
	private bool m_alsoUpdateInactiveLocalizeUI = true;

	[Tooltip("If a language's field value is blank, use default language's field value.")]
	[SerializeField]
	private bool m_useDefaultLanguageForBlankTranslations = true;

	private string m_currentLanguage = string.Empty;

	private static UILocalizationManager s_instance;

	private static bool s_isQuitting;

	public LocalizedFonts localizedFonts
	{
		get
		{
			return m_localizedFonts;
		}
		set
		{
			m_localizedFonts = value;
		}
	}

	public static UILocalizationManager instance
	{
		get
		{
			if (s_instance == null && !s_isQuitting)
			{
				s_instance = GameObjectUtility.FindFirstObjectByType<UILocalizationManager>();
				if (s_instance == null && Application.isPlaying)
				{
					GlobalTextTable globalTextTable = GameObjectUtility.FindFirstObjectByType<GlobalTextTable>();
					s_instance = ((globalTextTable != null) ? globalTextTable.gameObject.AddComponent<UILocalizationManager>() : new GameObject("UILocalizationManager").AddComponent<UILocalizationManager>());
				}
			}
			return s_instance;
		}
		set
		{
			s_instance = value;
		}
	}

	public TextTable textTable
	{
		get
		{
			if (!(m_textTable != null))
			{
				return GlobalTextTable.textTable;
			}
			return m_textTable;
		}
		set
		{
			m_textTable = value;
		}
	}

	public List<TextTable> additionalTextTables
	{
		get
		{
			return m_additionalTextTables;
		}
		set
		{
			m_additionalTextTables = value;
		}
	}

	public string currentLanguage
	{
		get
		{
			return instance.m_currentLanguage;
		}
		set
		{
			instance.m_currentLanguage = value;
			instance.UpdateUIs(value);
		}
	}

	public string currentLanguagePlayerPrefsKey
	{
		get
		{
			return m_currentLanguagePlayerPrefsKey;
		}
		set
		{
			m_currentLanguagePlayerPrefsKey = value;
		}
	}

	public bool saveLanguageInPlayerPrefs
	{
		get
		{
			return m_saveLanguageInPlayerPrefs;
		}
		set
		{
			m_saveLanguageInPlayerPrefs = value;
		}
	}

	public bool useDefaultLanguageForBlankTranslations
	{
		get
		{
			return m_useDefaultLanguageForBlankTranslations;
		}
		set
		{
			m_useDefaultLanguageForBlankTranslations = value;
			TextTable.useDefaultLanguageForBlankTranslations = value;
		}
	}

	public static event Action<string> languageChanged;

	private void OnApplicationQuit()
	{
		s_isQuitting = true;
	}

	private void Awake()
	{
		if (s_instance == null)
		{
			s_instance = this;
		}
		Initialize();
	}

	public void Initialize()
	{
		if (saveLanguageInPlayerPrefs && !string.IsNullOrEmpty(currentLanguagePlayerPrefsKey) && PlayerPrefs.HasKey(currentLanguagePlayerPrefsKey))
		{
			m_currentLanguage = PlayerPrefs.GetString(currentLanguagePlayerPrefsKey);
			UILocalizationManager.languageChanged?.Invoke(currentLanguage);
		}
		TextTable.useDefaultLanguageForBlankTranslations = m_useDefaultLanguageForBlankTranslations;
	}

	private IEnumerator Start()
	{
		yield return CoroutineUtility.endOfFrame;
		UpdateUIs(currentLanguage);
	}

	public bool HasLanguage(string language)
	{
		if (textTable != null && textTable.HasLanguage(language))
		{
			return true;
		}
		if (additionalTextTables != null)
		{
			foreach (TextTable additionalTextTable in additionalTextTables)
			{
				if (additionalTextTable != null && additionalTextTable.HasLanguage(language))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool HasField(string fieldName)
	{
		if (textTable != null && textTable.HasField(fieldName))
		{
			return true;
		}
		if (additionalTextTables != null)
		{
			foreach (TextTable additionalTextTable in additionalTextTables)
			{
				if (additionalTextTable != null && additionalTextTable.HasField(fieldName))
				{
					return true;
				}
			}
		}
		return false;
	}

	public string GetFieldTextForLanguage(string fieldName, string language)
	{
		if (textTable != null && textTable.HasField(fieldName))
		{
			return textTable.GetFieldTextForLanguage(fieldName, language);
		}
		if (additionalTextTables != null)
		{
			foreach (TextTable additionalTextTable in additionalTextTables)
			{
				if (additionalTextTable != null && additionalTextTable.HasField(fieldName))
				{
					return additionalTextTable.GetFieldTextForLanguage(fieldName, language);
				}
			}
		}
		return string.Empty;
	}

	public string GetLocalizedText(string fieldName)
	{
		return GetFieldTextForLanguage(fieldName, GlobalTextTable.currentLanguage);
	}

	public void UpdateUIs(string language)
	{
		m_currentLanguage = language;
		UILocalizationManager.languageChanged?.Invoke(language);
		if (saveLanguageInPlayerPrefs && !string.IsNullOrEmpty(currentLanguagePlayerPrefsKey))
		{
			PlayerPrefs.SetString(currentLanguagePlayerPrefsKey, language);
		}
		LocalizeUI[] array = (m_alsoUpdateInactiveLocalizeUI ? GameObjectUtility.FindObjectsOfTypeAlsoInactive<LocalizeUI>() : GameObjectUtility.FindObjectsByType<LocalizeUI>());
		for (int i = 0; i < array.Length; i++)
		{
			array[i].UpdateText();
		}
	}
}
