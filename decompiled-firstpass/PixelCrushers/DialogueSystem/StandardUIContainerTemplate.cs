using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class StandardUIContainerTemplate : StandardUIContentTemplate
{
	[NonSerialized]
	private List<StandardUIContentTemplate> m_instances = new List<StandardUIContentTemplate>();

	public List<StandardUIContentTemplate> instances => m_instances;

	public void AddInstanceToContainer(StandardUIContentTemplate instance)
	{
		instance.gameObject.SetActive(value: true);
		instances.Add(instance);
		instance.transform.SetParent(base.transform, worldPositionStays: false);
	}

	public override void Despawn()
	{
		instances.ForEach(delegate(StandardUIContentTemplate instance)
		{
			instance.Despawn();
		});
		instances.Clear();
		base.Despawn();
	}
}
