using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public class StandardPickable : Holdable, Iinteractable, ICoffeeSequenceItem
{
	public bool isLeftObject;

	private ICoffeeSequenceItem.CoffeeItem CoffeeItem;

	[SerializeField]
	private Vector3 holdingPosition;

	[SerializeField]
	private Vector3 holdingRotation;

	[SerializeField]
	private Vector3 holdingScale;

	[SerializeField]
	private UIManager currentSceneUImanager;

	public ICoffeeSequenceItem.CoffeeItem coffeeItem
	{
		get
		{
			return CoffeeItem;
		}
		set
		{
			CoffeeItem = value;
		}
	}

	void Iinteractable.Clicked(Action removedFromHand)
	{
		if ((object)playerController == null)
		{
			playerController = PlayerController.GetInstance();
		}
		Holdable holdingObject = playerController.GetHoldingObject();
		Holdable holdingObjectLeft = playerController.GetHoldingObjectLeft();
		if ((bool)holdingObject && !isLeftObject)
		{
			if (playerController.GetHoldingObject().GetComponent<CasseroleIngredientObject>() != null && playerController.GetHoldingObject().GetComponent<CasseroleIngredientObject>().IngredientType == CasseroleRecipie.StackablePlate)
			{
				playerController.HoldObject(this, isThrowable: true);
				Debug.Log("HoldingStackable Object");
			}
			else
			{
				SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "HandsFull"));
			}
		}
		else if (isLeftObject && (bool)holdingObjectLeft)
		{
			SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "HandsFull"));
		}
		else if (playerController.firstPersonController.gameObject.activeSelf)
		{
			if (!isLeftObject)
			{
				playerController.HoldObject(this, isThrowable: true);
			}
			else
			{
				playerController.HoldObjectLeft(this, isThrowable: true);
			}
		}
	}

	private new void Start()
	{
		CoffeeItem = ICoffeeSequenceItem.CoffeeItem.IsTea;
	}

	public override void SetForUse()
	{
		base.SetForUse();
		base.gameObject.transform.parent = null;
		base.gameObject.SetActive(value: false);
	}

	public override void GoToPosition(Transform parentTransform)
	{
		base.GoToPosition(parentTransform);
		base.transform.localPosition = holdingPosition;
		base.transform.localRotation = Quaternion.Euler(holdingRotation);
		base.transform.localScale = holdingScale;
	}

	[SpecialName]
	GameObject Iinteractable.get_gameObject()
	{
		return base.gameObject;
	}
}
