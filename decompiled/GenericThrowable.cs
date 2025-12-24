using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public class GenericThrowable : Holdable, Iinteractable
{
	[SerializeField]
	private Vector3 holdPos;

	[SerializeField]
	private Vector3 holdRot;

	public void Clicked(Action removedFromHand)
	{
		if ((object)playerController == null)
		{
			playerController = PlayerController.GetInstance();
		}
		playerController.HoldObject(this);
	}

	public override void SetForUse()
	{
		base.SetForUse();
		GetComponent<Collider>().enabled = false;
	}

	public override void GoToPosition(Transform parentTransform)
	{
		base.GoToPosition(parentTransform);
		base.transform.localPosition = holdPos;
		base.transform.localRotation = Quaternion.Euler(holdRot);
		base.transform.localScale = Vector3.one;
	}

	[SpecialName]
	GameObject Iinteractable.get_gameObject()
	{
		return base.gameObject;
	}
}
