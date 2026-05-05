using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers;

[Serializable]
public class TextTableField : ISerializationCallbackReceiver
{
	[SerializeField]
	private string m_fieldName;

	private Dictionary<int, string> m_texts = new Dictionary<int, string>();

	[SerializeField]
	private List<int> m_keys = new List<int>();

	[SerializeField]
	private List<string> m_values = new List<string>();

	public string fieldName
	{
		get
		{
			return m_fieldName;
		}
		set
		{
			m_fieldName = value;
		}
	}

	public Dictionary<int, string> texts
	{
		get
		{
			return m_texts;
		}
		set
		{
			m_texts = value;
		}
	}

	public TextTableField()
	{
	}

	public TextTableField(string fieldName)
	{
		m_fieldName = fieldName;
	}

	public void OnBeforeSerialize()
	{
		m_keys.Clear();
		m_values.Clear();
		foreach (KeyValuePair<int, string> text in texts)
		{
			m_keys.Add(text.Key);
			m_values.Add(text.Value);
		}
	}

	public void OnAfterDeserialize()
	{
		texts = new Dictionary<int, string>();
		for (int i = 0; i != Math.Min(m_keys.Count, m_values.Count); i++)
		{
			texts.Add(m_keys[i], m_values[i]);
		}
	}

	public bool HasTextForLanguage(int languageID)
	{
		if (texts.ContainsKey(languageID))
		{
			return !string.IsNullOrEmpty(texts[languageID]);
		}
		return false;
	}

	public string GetTextForLanguage(int languageID)
	{
		if (!texts.ContainsKey(languageID))
		{
			return string.Empty;
		}
		return texts[languageID];
	}

	public void SetTextForLanguage(int languageID, string text)
	{
		if (texts.ContainsKey(languageID))
		{
			texts[languageID] = text;
		}
		else
		{
			texts.Add(languageID, text);
		}
	}

	public void RemoveLanguage(int languageID)
	{
		texts.Remove(languageID);
	}
}
