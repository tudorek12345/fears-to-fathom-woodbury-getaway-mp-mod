using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public class TriggerActionOnInteract : MonoBehaviour, Iinteractable
{
	private Action actionToTrigger;

	public void SetActionToTrigger(Action action)
	{
		actionToTrigger = action;
	}

	void Iinteractable.Clicked(Action removedFromHand = null)
	{
		actionToTrigger?.Invoke();
	}

	[SpecialName]
	GameObject Iinteractable.get_gameObject()
	{
		return base.gameObject;
	}
}
