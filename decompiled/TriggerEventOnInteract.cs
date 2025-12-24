using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

public class TriggerEventOnInteract : MonoBehaviour, Iinteractable
{
	public UnityEvent triggerEvent;

	void Iinteractable.Clicked(Action removedFromHand = null)
	{
		triggerEvent.Invoke();
	}

	[SpecialName]
	GameObject Iinteractable.get_gameObject()
	{
		return base.gameObject;
	}
}
