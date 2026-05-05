using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers;

[Serializable]
public class UIInputField
{
	[SerializeField]
	private InputField m_uiInputField;

	[SerializeField]
	private TMP_InputField m_textMeshProInputField;

	public InputField uiInputField
	{
		get
		{
			return m_uiInputField;
		}
		set
		{
			m_uiInputField = value;
		}
	}

	public TMP_InputField textMeshProInputField
	{
		get
		{
			return m_textMeshProInputField;
		}
		set
		{
			m_textMeshProInputField = value;
		}
	}

	public string text
	{
		get
		{
			if ((UnityEngine.Object)(object)textMeshProInputField != null)
			{
				return textMeshProInputField.text;
			}
			if ((UnityEngine.Object)(object)uiInputField != null)
			{
				return uiInputField.text;
			}
			return string.Empty;
		}
		set
		{
			if ((UnityEngine.Object)(object)textMeshProInputField != null)
			{
				textMeshProInputField.text = value;
			}
			if ((UnityEngine.Object)(object)uiInputField != null)
			{
				uiInputField.text = value;
			}
		}
	}

	public int characterLimit
	{
		get
		{
			if ((UnityEngine.Object)(object)textMeshProInputField != null)
			{
				return textMeshProInputField.characterLimit;
			}
			if ((UnityEngine.Object)(object)uiInputField != null)
			{
				return uiInputField.characterLimit;
			}
			return 0;
		}
		set
		{
			if ((UnityEngine.Object)(object)textMeshProInputField != null)
			{
				textMeshProInputField.characterLimit = value;
			}
			if ((UnityEngine.Object)(object)uiInputField != null)
			{
				uiInputField.characterLimit = value;
			}
		}
	}

	public bool enabled
	{
		get
		{
			if ((UnityEngine.Object)(object)textMeshProInputField != null)
			{
				return ((Behaviour)(object)textMeshProInputField).enabled;
			}
			if ((UnityEngine.Object)(object)uiInputField != null)
			{
				return ((Behaviour)(object)uiInputField).enabled;
			}
			return false;
		}
		set
		{
			if ((UnityEngine.Object)(object)textMeshProInputField != null)
			{
				((Behaviour)(object)textMeshProInputField).enabled = value;
			}
			if ((UnityEngine.Object)(object)uiInputField != null)
			{
				((Behaviour)(object)uiInputField).enabled = value;
			}
		}
	}

	public GameObject gameObject
	{
		get
		{
			if ((UnityEngine.Object)(object)textMeshProInputField != null)
			{
				return ((Component)(object)textMeshProInputField).gameObject;
			}
			if (!((UnityEngine.Object)(object)uiInputField != null))
			{
				return null;
			}
			return ((Component)(object)uiInputField).gameObject;
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

	public UIInputField()
	{
		uiInputField = null;
		textMeshProInputField = null;
	}

	public UIInputField(InputField uiInputField)
	{
		this.uiInputField = uiInputField;
		textMeshProInputField = null;
	}

	public UIInputField(TMP_InputField textMeshProInputField)
	{
		uiInputField = null;
		this.textMeshProInputField = textMeshProInputField;
	}

	public void SetActive(bool value)
	{
		if ((UnityEngine.Object)(object)uiInputField != null)
		{
			((Component)(object)uiInputField).gameObject.SetActive(value);
		}
		if ((UnityEngine.Object)(object)textMeshProInputField != null)
		{
			((Component)(object)textMeshProInputField).gameObject.SetActive(value);
		}
	}

	public void ActivateInputField()
	{
		if ((UnityEngine.Object)(object)uiInputField != null)
		{
			uiInputField.ActivateInputField();
		}
		if ((UnityEngine.Object)(object)textMeshProInputField != null)
		{
			textMeshProInputField.ActivateInputField();
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
