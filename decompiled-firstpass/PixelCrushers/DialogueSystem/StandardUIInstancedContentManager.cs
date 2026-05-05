using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public class StandardUIInstancedContentManager
{
	protected List<StandardUIContentTemplate> instances = new List<StandardUIContentTemplate>();

	public List<StandardUIContentTemplate> instancedContent => instances;

	public void Clear()
	{
		for (int i = 0; i < instances.Count; i++)
		{
			instances[i].Despawn();
		}
		instances.Clear();
	}

	public T Instantiate<T>(T template) where T : StandardUIContentTemplate
	{
		if (template == null)
		{
			return null;
		}
		return Object.Instantiate(template);
	}

	public void Add(StandardUIContentTemplate instance, RectTransform container)
	{
		if (container == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning("Dialogue System: Container isn't assigned to hold instance of UI template.", instance);
			}
		}
		else
		{
			instance.gameObject.SetActive(value: true);
			instances.Add(instance);
			instance.transform.SetParent(container, worldPositionStays: false);
		}
	}

	public void Remove(StandardUIContentTemplate instance)
	{
		instances.Remove(instance);
		instance.Despawn();
	}

	public StandardUIContentTemplate GetLastAdded()
	{
		if (instances.Count <= 0)
		{
			return null;
		}
		return instances[instances.Count - 1];
	}
}
