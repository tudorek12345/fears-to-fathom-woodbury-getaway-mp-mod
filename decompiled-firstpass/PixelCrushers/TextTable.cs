using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers;

public class TextTable : ScriptableObject, ISerializationCallbackReceiver
{
	protected struct LanguageKeyValuePair(string key, int value)
	{
		public string key = key;

		public int value = value;
	}

	protected struct FieldKeyValuePair(int key, TextTableField value)
	{
		public int key = key;

		public TextTableField value = value;
	}

	private static int s_currentLanguageID = 0;

	private static bool m_useDefaultLanguageForBlankTranslations = true;

	private Dictionary<string, int> m_languages = new Dictionary<string, int>();

	private Dictionary<int, TextTableField> m_fields = new Dictionary<int, TextTableField>();

	[SerializeField]
	private List<string> m_languageKeys = new List<string>();

	[SerializeField]
	private List<int> m_languageValues = new List<int>();

	[SerializeField]
	private List<int> m_fieldKeys = new List<int>();

	[SerializeField]
	private List<TextTableField> m_fieldValues = new List<TextTableField>();

	[SerializeField]
	private int m_nextLanguageID;

	[SerializeField]
	private int m_nextFieldID = 1;

	public static bool useDefaultLanguageForBlankTranslations
	{
		get
		{
			return m_useDefaultLanguageForBlankTranslations;
		}
		set
		{
			m_useDefaultLanguageForBlankTranslations = value;
		}
	}

	public static int currentLanguageID
	{
		get
		{
			return s_currentLanguageID;
		}
		set
		{
			s_currentLanguageID = value;
		}
	}

	public Dictionary<string, int> languages
	{
		get
		{
			return m_languages;
		}
		set
		{
			m_languages = value;
		}
	}

	public Dictionary<int, TextTableField> fields
	{
		get
		{
			return m_fields;
		}
		set
		{
			m_fields = value;
		}
	}

	public int nextLanguageID => m_nextLanguageID;

	public int nextFieldID => m_nextFieldID;

	public void OnBeforeSerialize()
	{
		m_languageKeys.Clear();
		m_languageValues.Clear();
		foreach (KeyValuePair<string, int> language in languages)
		{
			m_languageKeys.Add(language.Key);
			m_languageValues.Add(language.Value);
		}
		m_fieldKeys.Clear();
		m_fieldValues.Clear();
		foreach (KeyValuePair<int, TextTableField> field in fields)
		{
			m_fieldKeys.Add(field.Key);
			m_fieldValues.Add(field.Value);
		}
	}

	public void OnAfterDeserialize()
	{
		languages = new Dictionary<string, int>();
		for (int i = 0; i != Math.Min(m_languageKeys.Count, m_languageValues.Count); i++)
		{
			languages.Add(m_languageKeys[i], m_languageValues[i]);
		}
		fields = new Dictionary<int, TextTableField>();
		for (int j = 0; j != Math.Min(m_fieldKeys.Count, m_fieldValues.Count); j++)
		{
			fields.Add(m_fieldKeys[j], m_fieldValues[j]);
		}
	}

	public bool HasLanguage(string languageName)
	{
		if (!string.IsNullOrEmpty(languageName))
		{
			return languages.ContainsKey(languageName);
		}
		return true;
	}

	public bool HasLanguage(int languageID)
	{
		if (languageID != 0)
		{
			return languages.ContainsValue(languageID);
		}
		return true;
	}

	public string GetLanguageName(int languageID)
	{
		Dictionary<string, int>.Enumerator enumerator = languages.GetEnumerator();
		while (enumerator.MoveNext())
		{
			if (enumerator.Current.Value == languageID)
			{
				return enumerator.Current.Key;
			}
		}
		return string.Empty;
	}

	public int GetLanguageID(string languageName)
	{
		if (!languages.ContainsKey(languageName))
		{
			return 0;
		}
		return languages[languageName];
	}

	public string[] GetLanguageNames()
	{
		string[] array = new string[languages.Count];
		languages.Keys.CopyTo(array, 0);
		return array;
	}

	public int[] GetLanguageIDs()
	{
		int[] array = new int[languages.Count];
		languages.Values.CopyTo(array, 0);
		return array;
	}

	public void AddLanguage(string languageName)
	{
		if (!languages.ContainsKey(languageName))
		{
			languages.Add(languageName, m_nextLanguageID++);
		}
	}

	public void RemoveLanguage(string languageName)
	{
		if (languages.ContainsKey(languageName))
		{
			RemoveLanguageFromFields(languages[languageName]);
			languages.Remove(languageName);
		}
	}

	public void RemoveLanguage(int languageID)
	{
		RemoveLanguage(GetLanguageName(languageID));
	}

	public void RemoveAll()
	{
		fields.Clear();
		languages.Clear();
		languages.Add("Default", 0);
		m_nextLanguageID = 1;
		m_nextFieldID = 1;
		OnBeforeSerialize();
	}

	public void SortLanguages()
	{
		if (m_languageKeys.Count != 0)
		{
			string item = m_languageKeys[0];
			m_languageKeys.RemoveAt(0);
			int item2 = m_languageValues[0];
			m_languageValues.RemoveAt(0);
			List<LanguageKeyValuePair> list = new List<LanguageKeyValuePair>();
			for (int i = 0; i < m_languageKeys.Count; i++)
			{
				list.Add(new LanguageKeyValuePair(m_languageKeys[i], m_languageValues[i]));
			}
			list.Sort((LanguageKeyValuePair a, LanguageKeyValuePair b) => a.key.CompareTo(b.key));
			m_languageKeys.Clear();
			m_languageValues.Clear();
			m_languageKeys.Add(item);
			m_languageValues.Add(item2);
			for (int num = 0; num < list.Count; num++)
			{
				m_languageKeys.Add(list[num].key);
				m_languageValues.Add(list[num].value);
			}
			OnAfterDeserialize();
		}
	}

	public bool HasField(int fieldID)
	{
		return fields.ContainsKey(fieldID);
	}

	public bool HasField(string fieldName)
	{
		return GetField(fieldName) != null;
	}

	public TextTableField GetField(int fieldID)
	{
		if (!fields.ContainsKey(fieldID))
		{
			return null;
		}
		return fields[fieldID];
	}

	public TextTableField GetField(string fieldName)
	{
		return GetField(GetFieldID(fieldName));
	}

	public int GetFieldID(string fieldName)
	{
		Dictionary<int, TextTableField>.Enumerator enumerator = fields.GetEnumerator();
		while (enumerator.MoveNext())
		{
			if (enumerator.Current.Value != null && string.Equals(enumerator.Current.Value.fieldName, fieldName))
			{
				return enumerator.Current.Key;
			}
		}
		return 0;
	}

	public string GetFieldName(int fieldID)
	{
		if (!fields.ContainsKey(fieldID))
		{
			return string.Empty;
		}
		return fields[fieldID].fieldName;
	}

	public bool HasFieldTextForLanguage(int fieldID, int languageID)
	{
		return GetField(fieldID)?.HasTextForLanguage(languageID) ?? false;
	}

	public bool HasFieldTextForLanguage(int fieldID, string languageName)
	{
		return HasFieldTextForLanguage(fieldID, GetLanguageID(languageName));
	}

	public bool HasFieldTextForLanguage(string fieldName, int languageID)
	{
		return HasFieldTextForLanguage(GetFieldID(fieldName), languageID);
	}

	public bool HasFieldTextForLanguage(string fieldName, string languageName)
	{
		return HasFieldTextForLanguage(GetFieldID(fieldName), GetLanguageID(languageName));
	}

	public string GetFieldTextForLanguage(int fieldID, int languageID)
	{
		TextTableField field = GetField(fieldID);
		if (field == null)
		{
			return GetFieldName(fieldID);
		}
		string text;
		if (field.HasTextForLanguage(languageID))
		{
			text = field.GetTextForLanguage(languageID).Replace("\\n", "\n");
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
		}
		string textForLanguage = field.GetTextForLanguage(0);
		text = ((!string.IsNullOrEmpty(textForLanguage) && useDefaultLanguageForBlankTranslations) ? textForLanguage : GetFieldName(fieldID));
		return text.Replace("\\n", "\n");
	}

	public string GetFieldTextForLanguage(int fieldID, string languageName)
	{
		return GetFieldTextForLanguage(fieldID, GetLanguageID(languageName));
	}

	public string GetFieldTextForLanguage(string fieldName, int languageID)
	{
		TextTableField field = GetField(fieldName);
		if (field == null)
		{
			return fieldName;
		}
		string text;
		if (field.HasTextForLanguage(languageID))
		{
			text = field.GetTextForLanguage(languageID).Replace("\\n", "\n");
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
		}
		string textForLanguage = field.GetTextForLanguage(0);
		text = ((!string.IsNullOrEmpty(textForLanguage) && useDefaultLanguageForBlankTranslations) ? textForLanguage : fieldName);
		return text.Replace("\\n", "\n");
	}

	public string GetFieldTextForLanguage(string fieldName, string languageName)
	{
		return GetFieldTextForLanguage(fieldName, GetLanguageID(languageName));
	}

	public string GetFieldText(int fieldID)
	{
		return GetFieldTextForLanguage(fieldID, currentLanguageID);
	}

	public string GetFieldText(string fieldName)
	{
		return GetFieldTextForLanguage(fieldName, currentLanguageID);
	}

	public int[] GetFieldIDs()
	{
		int[] array = new int[fields.Count];
		fields.Keys.CopyTo(array, 0);
		return array;
	}

	public string[] GetFieldNames()
	{
		string[] array = new string[fields.Count];
		int num = 0;
		Dictionary<int, TextTableField>.Enumerator enumerator = fields.GetEnumerator();
		while (enumerator.MoveNext())
		{
			array[num++] = ((enumerator.Current.Value != null) ? enumerator.Current.Value.fieldName : string.Empty);
		}
		return array;
	}

	public void AddField(string fieldName)
	{
		if (!HasField(fieldName))
		{
			fields.Add(m_nextFieldID++, new TextTableField(fieldName));
		}
	}

	public void SetFieldTextForLanguage(int fieldID, int languageID, string text)
	{
		if (!HasLanguage(languageID))
		{
			if (Debug.isDebugBuild)
			{
				Debug.LogWarning("TextTable.SetLanguageText(" + fieldID + ", " + languageID + ", \"" + text + "\") failed: Language doesn't exist. Use Text Table Editor or AddLanguage() to add the language first.", this);
			}
			return;
		}
		TextTableField field = GetField(fieldID);
		if (field == null)
		{
			if (Debug.isDebugBuild)
			{
				Debug.LogWarning("TextTable.SetLanguageText(" + fieldID + ", " + languageID + ", \"" + text + "\") failed: Field doesn't exist. Use Text Table Editor or AddField() to add the field first.", this);
			}
		}
		else
		{
			field.SetTextForLanguage(languageID, text);
		}
	}

	public void SetFieldTextForLanguage(string fieldName, int languageID, string text)
	{
		SetFieldTextForLanguage(GetFieldID(fieldName), languageID, text);
	}

	public void SetFieldTextForLanguage(int fieldID, string languageName, string text)
	{
		SetFieldTextForLanguage(fieldID, GetLanguageID(languageName), text);
	}

	public void SetFieldTextForLanguage(string fieldName, string languageName, string text)
	{
		SetFieldTextForLanguage(GetFieldID(fieldName), GetLanguageID(languageName), text);
	}

	public void RemoveField(int fieldID)
	{
		fields.Remove(fieldID);
	}

	public void RemoveField(string fieldName)
	{
		fields.Remove(GetFieldID(fieldName));
	}

	private void RemoveLanguageFromFields(int languageID)
	{
		Dictionary<int, TextTableField>.Enumerator enumerator = fields.GetEnumerator();
		while (enumerator.MoveNext())
		{
			if (enumerator.Current.Value != null)
			{
				enumerator.Current.Value.RemoveLanguage(languageID);
			}
		}
	}

	public void RemoveAllFields()
	{
		fields.Clear();
		m_nextFieldID = 1;
		OnBeforeSerialize();
	}

	public void InsertField(int index, string fieldName)
	{
		if (!HasField(fieldName))
		{
			OnBeforeSerialize();
			int item = m_nextFieldID++;
			m_fieldKeys.Insert(index, item);
			TextTableField textTableField = new TextTableField(fieldName);
			textTableField.texts.Add(0, string.Empty);
			m_fieldValues.Insert(index, textTableField);
			OnAfterDeserialize();
		}
	}

	public void SortFields()
	{
		List<FieldKeyValuePair> list = new List<FieldKeyValuePair>();
		for (int i = 0; i < m_fieldKeys.Count; i++)
		{
			list.Add(new FieldKeyValuePair(m_fieldKeys[i], m_fieldValues[i]));
		}
		list.Sort((FieldKeyValuePair a, FieldKeyValuePair b) => a.value.fieldName.CompareTo(b.value.fieldName));
		m_fieldKeys.Clear();
		m_fieldValues.Clear();
		for (int num = 0; num < list.Count; num++)
		{
			m_fieldKeys.Add(list[num].key);
			m_fieldValues.Add(list[num].value);
		}
		OnAfterDeserialize();
	}

	public void ReorderFields(List<string> order)
	{
		if (order == null)
		{
			return;
		}
		OnBeforeSerialize();
		List<int> list = new List<int>();
		List<TextTableField> list2 = new List<TextTableField>();
		int i;
		for (i = 0; i < order.Count; i++)
		{
			int num = m_fieldValues.FindIndex((TextTableField x) => string.Equals(x.fieldName, order[i]));
			if (num != -1)
			{
				list.Add(m_fieldKeys[num]);
				list2.Add(m_fieldValues[num]);
				m_fieldKeys.RemoveAt(num);
				m_fieldValues.RemoveAt(num);
			}
		}
		list.AddRange(m_fieldKeys);
		list2.AddRange(m_fieldValues);
		m_fieldKeys = list;
		m_fieldValues = list2;
		OnAfterDeserialize();
	}

	public void ImportOtherTextTable(TextTable other)
	{
		if (other == null || other == this)
		{
			return;
		}
		foreach (string key in other.languages.Keys)
		{
			if (!HasLanguage(key))
			{
				AddLanguage(key);
			}
		}
		foreach (TextTableField value in other.fields.Values)
		{
			AddField(value.fieldName);
			foreach (string key2 in other.languages.Keys)
			{
				SetFieldTextForLanguage(value.fieldName, key2, other.GetFieldTextForLanguage(value.fieldName, key2));
			}
		}
	}
}
