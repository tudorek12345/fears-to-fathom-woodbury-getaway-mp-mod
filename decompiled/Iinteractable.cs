using System;
using UnityEngine;

public interface Iinteractable
{
	GameObject gameObject { get; }

	void Clicked(Action removedFromHand = null);
}
