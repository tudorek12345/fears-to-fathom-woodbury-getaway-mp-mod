using System;
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
}
