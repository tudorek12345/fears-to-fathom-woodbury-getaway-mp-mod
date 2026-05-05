using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers;

[Serializable]
public class UIDropdownField
{
	[SerializeField]
	private Dropdown m_uiDropdown;

	[SerializeField]
	private TMP_Dropdown m_tmpDropdown;

	public Dropdown uiDropdown
	{
		get
		{
			return m_uiDropdown;
		}
		set
		{
			m_uiDropdown = value;
		}
	}

	public TMP_Dropdown tmpDropdown
	{
		get
		{
			return m_tmpDropdown;
		}
		set
		{
			m_tmpDropdown = value;
		}
	}

	public int value
	{
		get
		{
			if ((UnityEngine.Object)(object)m_tmpDropdown != null)
			{
				return m_tmpDropdown.value;
			}
			if ((UnityEngine.Object)(object)m_uiDropdown != null)
			{
				return m_uiDropdown.value;
			}
			return 0;
		}
		set
		{
			if ((UnityEngine.Object)(object)m_tmpDropdown != null)
			{
				m_tmpDropdown.value = value;
			}
			if ((UnityEngine.Object)(object)m_uiDropdown != null)
			{
				m_uiDropdown.value = value;
			}
		}
	}

	public bool enabled
	{
		get
		{
			if ((UnityEngine.Object)(object)m_tmpDropdown != null)
			{
				return ((Behaviour)(object)m_tmpDropdown).enabled;
			}
			if ((UnityEngine.Object)(object)m_uiDropdown != null)
			{
				return ((Behaviour)(object)m_uiDropdown).enabled;
			}
			return false;
		}
		set
		{
			if ((UnityEngine.Object)(object)m_tmpDropdown != null)
			{
				((Behaviour)(object)m_tmpDropdown).enabled = value;
			}
			if ((UnityEngine.Object)(object)m_uiDropdown != null)
			{
				((Behaviour)(object)m_uiDropdown).enabled = value;
			}
		}
	}

	public GameObject gameObject
	{
		get
		{
			if ((UnityEngine.Object)(object)tmpDropdown != null)
			{
				return ((Component)(object)tmpDropdown).gameObject;
			}
			if (!((UnityEngine.Object)(object)uiDropdown != null))
			{
				return null;
			}
			return ((Component)(object)uiDropdown).gameObject;
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

	public UIDropdownField()
	{
		uiDropdown = null;
		m_tmpDropdown = null;
	}

	public UIDropdownField(Dropdown uiDropdown)
	{
		this.uiDropdown = uiDropdown;
		m_tmpDropdown = null;
	}

	public UIDropdownField(TMP_Dropdown tmpDropdown)
	{
		uiDropdown = null;
		m_tmpDropdown = tmpDropdown;
	}

	public void SetActive(bool value)
	{
		if ((UnityEngine.Object)(object)uiDropdown != null)
		{
			((Component)(object)uiDropdown).gameObject.SetActive(value);
		}
		if ((UnityEngine.Object)(object)tmpDropdown != null)
		{
			((Component)(object)tmpDropdown).gameObject.SetActive(value);
		}
	}

	public void ClearOptions()
	{
		if ((UnityEngine.Object)(object)uiDropdown != null)
		{
			uiDropdown.ClearOptions();
		}
		if ((UnityEngine.Object)(object)tmpDropdown != null)
		{
			tmpDropdown.ClearOptions();
		}
	}

	public void AddOption(string text)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		if ((UnityEngine.Object)(object)uiDropdown != null)
		{
			uiDropdown.options.Add(new OptionData(text));
		}
		if ((UnityEngine.Object)(object)tmpDropdown != null)
		{
			tmpDropdown.options.Add(new OptionData(text));
		}
	}

	public static bool IsNull(UIDropdownField uiDropdownField)
	{
		if (uiDropdownField == null)
		{
			return true;
		}
		if ((UnityEngine.Object)(object)uiDropdownField.uiDropdown != null)
		{
			return false;
		}
		if ((UnityEngine.Object)(object)uiDropdownField.tmpDropdown != null)
		{
			return false;
		}
		return true;
	}
}
