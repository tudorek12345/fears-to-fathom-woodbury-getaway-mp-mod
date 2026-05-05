using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers;

[AddComponentMenu("")]
public class LocalizeUI : MonoBehaviour
{
	[Tooltip("Overrides the global text table.")]
	[SerializeField]
	private TextTable m_textTable;

	[Tooltip("Overrides the UILocalizationManager's Localized Fonts.")]
	[SerializeField]
	private LocalizedFonts m_localizedFonts;

	[Tooltip("(Optional) If assigned, use this instead of the UI element's text's value as the field lookup value.")]
	[SerializeField]
	private string m_fieldName = string.Empty;

	private bool m_started;

	private List<string> m_fieldNames = new List<string>();

	private List<string> m_tmpFieldNames = new List<string>();

	private Text m_text;

	private Dropdown m_dropdown;

	private TextMeshPro m_textMeshPro;

	private TextMeshProUGUI m_textMeshProUGUI;

	private TMP_Dropdown m_textMeshProDropdown;

	private bool m_lookedForTMP;

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

	public string fieldName
	{
		get
		{
			if (!string.IsNullOrEmpty(m_fieldName))
			{
				return m_fieldName;
			}
			return null;
		}
		set
		{
			m_fieldName = value;
		}
	}

	protected bool started
	{
		get
		{
			return m_started;
		}
		private set
		{
			m_started = value;
		}
	}

	protected List<string> fieldNames
	{
		get
		{
			return m_fieldNames;
		}
		set
		{
			m_fieldNames = value;
		}
	}

	protected List<string> tmpFieldNames
	{
		get
		{
			return m_tmpFieldNames;
		}
		set
		{
			m_tmpFieldNames = value;
		}
	}

	public Text text
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

	public Dropdown dropdown
	{
		get
		{
			return m_dropdown;
		}
		set
		{
			m_dropdown = value;
		}
	}

	public TextMeshPro textMeshPro
	{
		get
		{
			return m_textMeshPro;
		}
		set
		{
			m_textMeshPro = value;
		}
	}

	public TextMeshProUGUI textMeshProUGUI
	{
		get
		{
			return m_textMeshProUGUI;
		}
		set
		{
			m_textMeshProUGUI = value;
		}
	}

	public TMP_Dropdown textMeshProDropdown
	{
		get
		{
			return m_textMeshProDropdown;
		}
		set
		{
			m_textMeshProDropdown = value;
		}
	}

	protected virtual void Start()
	{
		started = true;
		UpdateText();
	}

	protected virtual void OnEnable()
	{
		if (started)
		{
			UpdateText();
		}
	}

	public virtual void UpdateText()
	{
		string text = ((UILocalizationManager.instance != null) ? UILocalizationManager.instance.currentLanguage : string.Empty);
		if (textTable == null && (UILocalizationManager.instance == null || UILocalizationManager.instance.textTable == null))
		{
			Debug.LogWarning("No localized text table is assigned to " + base.name + " or a UI Localized Manager component.", this);
			return;
		}
		if (!HasLanguage(text))
		{
			Debug.LogWarning("Text table does not have a language '" + text + "'.", textTable);
		}
		LocalizedFonts localizedFonts = ((m_localizedFonts != null) ? m_localizedFonts : UILocalizationManager.instance.localizedFonts);
		Font font = ((localizedFonts != null) ? localizedFonts.GetFont(text) : null);
		if ((Object)(object)this.text == null && (Object)(object)dropdown == null)
		{
			this.text = GetComponent<Text>();
			dropdown = GetComponent<Dropdown>();
		}
		bool num = (Object)(object)this.text != null || (Object)(object)dropdown != null;
		TMP_FontAsset val = ((localizedFonts != null) ? localizedFonts.GetTextMeshProFont(text) : null);
		if (!m_lookedForTMP)
		{
			m_lookedForTMP = true;
			textMeshPro = GetComponent<TextMeshPro>();
			textMeshProUGUI = GetComponent<TextMeshProUGUI>();
			textMeshProDropdown = GetComponent<TMP_Dropdown>();
		}
		if (!num && !((Object)(object)textMeshPro != null) && !((Object)(object)textMeshProUGUI != null) && !((Object)(object)textMeshProDropdown != null))
		{
			Debug.LogWarning("Localize UI didn't find a localizable UI component on " + base.name + ".", this);
			return;
		}
		if (string.IsNullOrEmpty(fieldName))
		{
			fieldName = (((Object)(object)this.text != null) ? this.text.text : string.Empty);
		}
		if ((Object)(object)dropdown != null && fieldNames.Count != dropdown.options.Count)
		{
			fieldNames.Clear();
			dropdown.options.ForEach(delegate(OptionData opt)
			{
				fieldNames.Add(opt.text);
			});
		}
		if ((Object)(object)this.text != null)
		{
			if (!HasField(fieldName))
			{
				Debug.LogWarning("Text table does not have a field '" + fieldName + "'.", textTable);
			}
			else
			{
				this.text.text = GetLocalizedText(fieldName);
				if (font != null)
				{
					this.text.font = font;
				}
			}
		}
		if ((Object)(object)dropdown != null)
		{
			for (int num2 = 0; num2 < dropdown.options.Count; num2++)
			{
				if (num2 < fieldNames.Count)
				{
					dropdown.options[num2].text = GetLocalizedText(fieldNames[num2]);
				}
			}
			dropdown.captionText.text = GetLocalizedText(fieldNames[dropdown.value]);
			if (font != null)
			{
				dropdown.captionText.font = font;
				dropdown.itemText.font = font;
			}
		}
		if (!m_lookedForTMP)
		{
			m_lookedForTMP = true;
			textMeshPro = GetComponent<TextMeshPro>();
			textMeshProUGUI = GetComponent<TextMeshProUGUI>();
		}
		if ((Object)(object)textMeshPro != null)
		{
			if (string.IsNullOrEmpty(fieldName))
			{
				fieldName = (((Object)(object)textMeshPro != null) ? ((TMP_Text)textMeshPro).text : string.Empty);
			}
			if (!HasField(fieldName))
			{
				Debug.LogWarning("Text table does not have a field '" + fieldName + "'.", textTable);
			}
			else
			{
				((TMP_Text)textMeshPro).text = GetLocalizedText(fieldName);
				if ((Object)(object)val != null)
				{
					((TMP_Text)textMeshPro).font = val;
				}
			}
		}
		if ((Object)(object)textMeshProUGUI != null)
		{
			if (string.IsNullOrEmpty(fieldName))
			{
				fieldName = (((Object)(object)textMeshProUGUI != null) ? ((TMP_Text)textMeshProUGUI).text : string.Empty);
			}
			if (!HasField(fieldName))
			{
				Debug.LogWarning("Text table does not have a field '" + fieldName + "'.", textTable);
			}
			else
			{
				((TMP_Text)textMeshProUGUI).text = GetLocalizedText(fieldName);
				if ((Object)(object)val != null)
				{
					((TMP_Text)textMeshProUGUI).font = val;
				}
			}
		}
		if (!((Object)(object)textMeshProDropdown != null))
		{
			return;
		}
		if (tmpFieldNames.Count != textMeshProDropdown.options.Count)
		{
			tmpFieldNames.Clear();
			textMeshProDropdown.options.ForEach(delegate(OptionData opt)
			{
				tmpFieldNames.Add(opt.text);
			});
		}
		for (int num3 = 0; num3 < textMeshProDropdown.options.Count; num3++)
		{
			if (num3 < tmpFieldNames.Count)
			{
				textMeshProDropdown.options[num3].text = GetLocalizedText(tmpFieldNames[num3]);
			}
		}
		textMeshProDropdown.captionText.text = GetLocalizedText(tmpFieldNames[textMeshProDropdown.value]);
		if ((Object)(object)val != null)
		{
			textMeshProDropdown.captionText.font = val;
			textMeshProDropdown.itemText.font = val;
		}
	}

	protected virtual bool HasLanguage(string language)
	{
		if (!(textTable != null) || !textTable.HasLanguage(language))
		{
			return UILocalizationManager.instance.HasLanguage(language);
		}
		return true;
	}

	protected virtual bool HasField(string fieldName)
	{
		if (!(textTable != null) || !textTable.HasField(fieldName))
		{
			return UILocalizationManager.instance.HasField(fieldName);
		}
		return true;
	}

	protected virtual string GetLocalizedText(string fieldName)
	{
		if (!(textTable != null) || !textTable.HasField(fieldName))
		{
			return UILocalizationManager.instance.GetLocalizedText(fieldName);
		}
		return textTable.GetFieldTextForLanguage(fieldName, GlobalTextTable.currentLanguage);
	}

	public virtual void SetFieldName(string newFieldName = "")
	{
		if ((Object)(object)text == null)
		{
			text = GetComponent<Text>();
		}
		fieldName = ((string.IsNullOrEmpty(newFieldName) && (Object)(object)text != null) ? text.text : newFieldName);
	}

	public virtual void UpdateDropdownOptions()
	{
		fieldNames.Clear();
		tmpFieldNames.Clear();
		UpdateText();
	}
}
