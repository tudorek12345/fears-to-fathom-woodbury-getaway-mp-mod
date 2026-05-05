using System;
using UnityEngine;
using UnityEngine.Events;

public class TriggerEventOnInteract : MonoBehaviour, Iinteractable
{
	public UnityEvent triggerEvent;

	void Iinteractable.Clicked(Action removedFromHand = null)
	{
		triggerEvent.Invoke();
	}
}
