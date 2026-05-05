using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class StandardUIToggleTemplate : StandardUIContentTemplate
{
	[Tooltip("Toggle UI element.")]
	public Toggle toggle;

	protected object m_data;

	public event ToggleChangedDelegate onToggleChanged = delegate
	{
	};

	public virtual void Awake()
	{
		if ((UnityEngine.Object)(object)toggle == null && DialogueDebug.logWarnings)
		{
			Debug.LogWarning("Dialogue System: UI Toggle is unassigned.", this);
		}
	}

	public virtual void Assign(bool isVisible, bool isOn, object data, ToggleChangedDelegate toggleDelegate)
	{
		m_data = data;
		if ((UnityEngine.Object)(object)toggle != null)
		{
			if (isVisible)
			{
				toggle.isOn = isOn;
				((UnityEvent<bool>)(object)toggle.onValueChanged).AddListener((UnityAction<bool>)OnToggleChanged);
				onToggleChanged += toggleDelegate;
			}
			else
			{
				base.gameObject.SetActive(value: false);
			}
		}
	}

	protected virtual void OnToggleChanged(bool value)
	{
		try
		{
			this.onToggleChanged(value, m_data);
		}
		catch (Exception exception)
		{
			if (Debug.isDebugBuild)
			{
				Debug.LogException(exception);
			}
		}
	}
}
