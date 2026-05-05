using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers;

[Serializable]
public class UITextField
{
	[SerializeField]
	private Text m_uiText;

	[SerializeField]
	private TextMeshProUGUI m_textMeshProUGUI;

	public Text uiText
	{
		get
		{
			return m_uiText;
		}
		set
		{
			m_uiText = value;
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

	public string text
	{
		get
		{
			if ((UnityEngine.Object)(object)textMeshProUGUI != null)
			{
				return ((TMP_Text)textMeshProUGUI).text;
			}
			if ((UnityEngine.Object)(object)uiText != null)
			{
				return uiText.text;
			}
			return string.Empty;
		}
		set
		{
			if ((UnityEngine.Object)(object)textMeshProUGUI != null)
			{
				((TMP_Text)textMeshProUGUI).text = value;
			}
			if ((UnityEngine.Object)(object)uiText != null)
			{
				uiText.text = value;
			}
		}
	}

	public bool enabled
	{
		get
		{
			if ((UnityEngine.Object)(object)textMeshProUGUI != null)
			{
				return ((Behaviour)(object)textMeshProUGUI).enabled;
			}
			if ((UnityEngine.Object)(object)uiText != null)
			{
				return ((Behaviour)(object)uiText).enabled;
			}
			return false;
		}
		set
		{
			if ((UnityEngine.Object)(object)textMeshProUGUI != null)
			{
				((Behaviour)(object)textMeshProUGUI).enabled = value;
			}
			if ((UnityEngine.Object)(object)uiText != null)
			{
				((Behaviour)(object)uiText).enabled = value;
			}
		}
	}

	public Color color
	{
		get
		{
			if ((UnityEngine.Object)(object)textMeshProUGUI != null)
			{
				return ((Graphic)textMeshProUGUI).color;
			}
			if ((UnityEngine.Object)(object)uiText != null)
			{
				return ((Graphic)uiText).color;
			}
			return Color.black;
		}
		set
		{
			if ((UnityEngine.Object)(object)textMeshProUGUI != null)
			{
				((Graphic)textMeshProUGUI).color = value;
			}
			if ((UnityEngine.Object)(object)uiText != null)
			{
				((Graphic)uiText).color = value;
			}
		}
	}

	public GameObject gameObject
	{
		get
		{
			if ((UnityEngine.Object)(object)textMeshProUGUI != null)
			{
				return ((Component)(object)textMeshProUGUI).gameObject;
			}
			if (!((UnityEngine.Object)(object)uiText != null))
			{
				return null;
			}
			return ((Component)(object)uiText).gameObject;
		}
	}

	public bool isActiveSelf
	{
		get
		{
			if (!(gameObject != null))
			{
				return false;
			}
			return gameObject.activeSelf;
		}
	}

	public bool activeInHierarchy
	{
		get
		{
			if (!(gameObject != null))
			{
				return false;
			}
			return gameObject.activeInHierarchy;
		}
	}

	public UITextField()
	{
		uiText = null;
		textMeshProUGUI = null;
	}

	public UITextField(Text uiText)
	{
		this.uiText = uiText;
		textMeshProUGUI = null;
	}

	public UITextField(TextMeshProUGUI textMeshProUGUI)
	{
		uiText = null;
		this.textMeshProUGUI = textMeshProUGUI;
	}

	public void SetActive(bool value)
	{
		if ((UnityEngine.Object)(object)uiText != null)
		{
			((Component)(object)uiText).gameObject.SetActive(value);
		}
		if ((UnityEngine.Object)(object)textMeshProUGUI != null)
		{
			((Component)(object)textMeshProUGUI).gameObject.SetActive(value);
		}
	}

	public static bool IsNull(UITextField uiTextField)
	{
		if (uiTextField == null)
		{
			return true;
		}
		if ((UnityEngine.Object)(object)uiTextField.uiText != null)
		{
			return false;
		}
		if ((UnityEngine.Object)(object)uiTextField.textMeshProUGUI != null)
		{
			return false;
		}
		return true;
	}
}
