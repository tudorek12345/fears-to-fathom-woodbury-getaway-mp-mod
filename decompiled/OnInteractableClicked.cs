using System;
using System.Runtime.CompilerServices;
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

	[SpecialName]
	GameObject Iinteractable.get_gameObject()
	{
		return base.gameObject;
	}
}
