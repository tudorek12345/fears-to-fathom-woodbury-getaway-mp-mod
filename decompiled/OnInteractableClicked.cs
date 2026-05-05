using System;
using UnityEngine;
using UnityEngine.Events;

public class OnInteractableClicked : MonoBehaviour, Iinteractable
{
	[SerializeField]
	private UnityEvent OnClicked;

	[SerializeField]
	private bool onlyOnce;

	public void Clicked(Action removedFromHand = null)
	{
		OnClicked.Invoke();
		if (onlyOnce)
		{
			base.enabled = false;
		}
	}
}
