using System;
using UnityEngine;

namespace PixelCrushers;

[Serializable]
public class StringField : IEquatable<StringField>
{
	[Tooltip("The string that holds the value of this string field. Unused if String Asset or Text Table is assigned.")]
	[SerializeField]
	private string m_text;

	[Tooltip("The String Asset that holds the value of this string field. Unused if Text or Text Table is assigned.")]
	[SerializeField]
	private StringAsset m_stringAsset;

	[Tooltip("The Text Table that holds the value of this string field. Unused if Text or String Asset is assigned.")]
	[SerializeField]
	private TextTable m_textTable;

	[Tooltip("The field ID in the Text Table.")]
	[SerializeField]
	private int m_textTableFieldID;

	public static readonly StringField empty = new StringField();

	public string text
	{
		get
		{
			return m_text;
		}
		set
		{
			m_text = value;
		}
	}

	public StringAsset stringAsset
	{
		get
		{
			return m_stringAsset;
		}
		set
		{
			m_stringAsset = value;
		}
	}

	public TextTable textTable
	{
		get
		{
			return m_textTable;
		}
		set
		{
			m_textTable = value;
		}
	}

	public int textTableFieldID
	{
		get
		{
			return m_textTableFieldID;
		}
		set
		{
			m_textTableFieldID = value;
		}
	}

	public string value
	{
		get
		{
			if (textTable != null)
			{
				if (!Application.isPlaying)
				{
					return textTable.GetFieldText(textTableFieldID);
				}
				return textTable.GetFieldTextForLanguage(textTableFieldID, UILocalizationManager.instance.currentLanguage);
			}
			if (stringAsset != null)
			{
				return stringAsset.text;
			}
			return text;
		}
		set
		{
			if (!(textTable != null) && !(stringAsset != null))
			{
				text = value;
			}
		}
	}

	public override string ToString()
	{
		return value;
	}

	public StringField()
	{
		text = string.Empty;
		stringAsset = null;
		textTable = null;
		textTableFieldID = 0;
	}

	public StringField(string text)
	{
		this.text = text;
		stringAsset = null;
		textTable = null;
		textTableFieldID = 0;
	}

	public StringField(StringAsset stringAsset)
	{
		text = string.Empty;
		this.stringAsset = stringAsset;
		textTable = null;
		textTableFieldID = 0;
	}

	public StringField(TextTable textTable, int fieldID)
	{
		text = string.Empty;
		stringAsset = null;
		this.textTable = textTable;
		textTableFieldID = fieldID;
	}

	public StringField(StringField source)
	{
		text = string.Empty;
		stringAsset = null;
		textTable = null;
		textTableFieldID = 0;
		if (!(source == null))
		{
			if (!string.IsNullOrEmpty(source.text))
			{
				text = source.text;
			}
			else if (source.stringAsset != null)
			{
				stringAsset = source.stringAsset;
			}
			else if (source.textTable != null)
			{
				textTable = source.textTable;
				textTableFieldID = source.textTableFieldID;
			}
		}
	}

	public void SetDefaultTextTable(TextTable textTable)
	{
		if (string.IsNullOrEmpty(text) && stringAsset == null && this.textTable == null)
		{
			this.textTable = textTable;
		}
	}

	public static bool operator ==(StringField obj1, StringField obj2)
	{
		if ((object)obj1 == obj2)
		{
			return true;
		}
		if ((object)obj1 == null && (object)obj2 == null)
		{
			return true;
		}
		if ((object)obj1 == null || (object)obj2 == null)
		{
			return false;
		}
		return string.Equals(obj1.value, obj2.value);
	}

	public static bool operator !=(StringField obj1, StringField obj2)
	{
		if ((object)obj1 == obj2)
		{
			return false;
		}
		if ((object)obj1 == null && (object)obj2 == null)
		{
			return false;
		}
		if ((object)obj1 == null || (object)obj2 == null)
		{
			return true;
		}
		return !string.Equals(obj1.value, obj2.value);
	}

	public bool Equals(StringField other)
	{
		if (!(other != null))
		{
			return false;
		}
		return string.Equals(other.value, value);
	}

	public override bool Equals(object obj)
	{
		if (obj is StringField)
		{
			return string.Equals(value, (obj as StringField).value);
		}
		if (obj is StringAsset)
		{
			return string.Equals(value, (obj as StringAsset).text);
		}
		if (obj is string)
		{
			return string.Equals(value, obj as string);
		}
		return base.Equals(obj);
	}

	public override int GetHashCode()
	{
		return value.GetHashCode();
	}

	public static bool IsNullOrEmpty(StringField stringField)
	{
		if (!(stringField == null))
		{
			return string.IsNullOrEmpty(stringField.value);
		}
		return true;
	}

	public static string GetStringValue(StringField stringField)
	{
		if (!(stringField == null))
		{
			return stringField.value;
		}
		return string.Empty;
	}
}
