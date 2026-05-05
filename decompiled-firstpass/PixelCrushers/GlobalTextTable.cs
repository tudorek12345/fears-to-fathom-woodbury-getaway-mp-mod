using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class GlobalTextTable : MonoBehaviour
{
	[Tooltip("The global TextTable.")]
	[SerializeField]
	private TextTable m_textTable;

	protected static GlobalTextTable s_instance;

	public static GlobalTextTable instance => s_instance;

	public static TextTable textTable
	{
		get
		{
			if (!(instance != null))
			{
				return null;
			}
			return instance.m_textTable;
		}
		set
		{
			if (instance != null)
			{
				instance.m_textTable = value;
				if (UILocalizationManager.instance != null)
				{
					UILocalizationManager.instance.UpdateUIs(currentLanguage);
				}
			}
		}
	}

	public static string currentLanguage
	{
		get
		{
			if (!(UILocalizationManager.instance != null))
			{
				return string.Empty;
			}
			return UILocalizationManager.instance.currentLanguage;
		}
		set
		{
			if (UILocalizationManager.instance != null)
			{
				UILocalizationManager.instance.currentLanguage = value;
			}
		}
	}

	protected virtual void Awake()
	{
		if (s_instance == null)
		{
			s_instance = this;
		}
	}

	protected virtual void OnDestroy()
	{
		if (s_instance == this)
		{
			s_instance = null;
		}
	}

	public static string Lookup(StringField fieldName)
	{
		if (fieldName == null)
		{
			return string.Empty;
		}
		return Lookup(fieldName.value);
	}

	public static string Lookup(string fieldName)
	{
		if (string.IsNullOrEmpty(fieldName))
		{
			return string.Empty;
		}
		if (textTable == null)
		{
			return fieldName;
		}
		return textTable.GetFieldTextForLanguage(fieldName, currentLanguage);
	}
}
