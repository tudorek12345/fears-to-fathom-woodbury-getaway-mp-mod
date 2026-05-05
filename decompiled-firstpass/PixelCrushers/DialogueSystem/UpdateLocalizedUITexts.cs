using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class UpdateLocalizedUITexts : MonoBehaviour
{
	public string languagePlayerPrefsKey = "Language";

	private void Awake()
	{
		Tools.DeprecationWarning(this, "Use UILocalizationManager and LocalizeUI instead.");
	}

	private IEnumerator Start()
	{
		yield return null;
		string text = string.Empty;
		if (!string.IsNullOrEmpty(languagePlayerPrefsKey) && PlayerPrefs.HasKey(languagePlayerPrefsKey))
		{
			text = PlayerPrefs.GetString(languagePlayerPrefsKey);
		}
		if (string.IsNullOrEmpty(text))
		{
			text = DialogueManager.displaySettings.localizationSettings.language;
		}
		UpdateTexts(text);
	}

	public void UpdateTexts(string languageCode)
	{
		if (DialogueDebug.logInfo)
		{
			Debug.Log("Dialogue System: Setting language to '" + languageCode + "'.", this);
		}
		DialogueManager.displaySettings.localizationSettings.useSystemLanguage = false;
		DialogueManager.displaySettings.localizationSettings.language = languageCode;
		Localization.language = languageCode;
		if (!string.IsNullOrEmpty(languagePlayerPrefsKey))
		{
			PlayerPrefs.SetString(languagePlayerPrefsKey, languageCode);
		}
		LocalizeUI[] array = GameObjectUtility.FindObjectsByType<LocalizeUI>();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].UpdateText();
		}
	}
}
