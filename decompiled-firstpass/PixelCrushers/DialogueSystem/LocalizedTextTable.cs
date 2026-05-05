using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class LocalizedTextTable : ScriptableObject
{
	[Serializable]
	public class LocalizedTextField
	{
		public string name = string.Empty;

		public List<string> values = new List<string>();
	}

	public List<string> languages = new List<string>();

	public List<LocalizedTextField> fields = new List<LocalizedTextField>();

	private const int LanguageNotFound = -1;

	public string this[string fieldName] => GetText(fieldName);

	public bool ContainsField(string fieldName)
	{
		return fields.Find((LocalizedTextField f) => string.Equals(f.name, fieldName)) != null;
	}

	private string GetText(string fieldName)
	{
		return GetTextInLanguage(fieldName, GetLanguageIndex());
	}

	private string GetTextInLanguage(string fieldName, int languageIndex)
	{
		if (languageIndex != -1)
		{
			foreach (LocalizedTextField field in fields)
			{
				if (string.Equals(field.name, fieldName))
				{
					if (languageIndex < field.values.Count && !string.IsNullOrEmpty(field.values[languageIndex]))
					{
						return field.values[languageIndex];
					}
					return (Localization.useDefaultIfUndefined && field.values.Count > 0) ? field.values[0] : string.Empty;
				}
			}
		}
		if (!Localization.useDefaultIfUndefined || languageIndex <= 0)
		{
			return string.Empty;
		}
		return GetTextInLanguage(fieldName, 0);
	}

	private int GetLanguageIndex()
	{
		if (Localization.isDefaultLanguage)
		{
			return 0;
		}
		for (int i = 0; i < languages.Count; i++)
		{
			if (string.Equals(languages[i], Localization.language))
			{
				return i;
			}
		}
		return -1;
	}
}
