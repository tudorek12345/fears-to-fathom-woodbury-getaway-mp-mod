using System;
using UnityEngine;
using UnityEngine.Events;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class Usable : MonoBehaviour
{
	[Serializable]
	public class UsableEvents
	{
		public UnityEvent onSelect = new UnityEvent();

		public UnityEvent onDeselect = new UnityEvent();

		public UnityEvent onUse = new UnityEvent();
	}

	public string overrideName;

	public string overrideUseMessage;

	public float maxUseDistance = 5f;

	public UsableEvents events;

	public event UsableDelegate disabled = delegate
	{
	};

	protected virtual void OnDisable()
	{
		this.disabled(this);
	}

	public virtual void Start()
	{
	}

	public virtual string GetName()
	{
		if (string.IsNullOrEmpty(overrideName))
		{
			return DialogueActor.GetActorName(base.transform);
		}
		if (overrideName.Contains("[lua") || overrideName.Contains("[var"))
		{
			return DialogueManager.GetLocalizedText(FormattedText.Parse(overrideName, DialogueManager.masterDatabase.emphasisSettings).text);
		}
		return DialogueManager.GetLocalizedText(overrideName);
	}

	public virtual void OnSelectUsable()
	{
		if (events != null && events.onSelect != null)
		{
			events.onSelect.Invoke();
		}
	}

	public virtual void OnDeselectUsable()
	{
		if (events != null && events.onDeselect != null)
		{
			events.onDeselect.Invoke();
		}
	}

	public virtual void OnUseUsable()
	{
		if (events != null && events.onUse != null)
		{
			events.onUse.Invoke();
		}
	}
}
