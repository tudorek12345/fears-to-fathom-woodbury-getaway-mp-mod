using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

public class AtticDoor : MonoBehaviour, Iinteractable
{
	public CabinGameManager cabinGameManager;

	public bool stoolSet;

	public bool playerOnStool;

	public bool playerInAttic;

	public AudioSource openDoorAS;

	public MeshRenderer doorMeshRenderer;

	public Material doorNormal;

	public Material doorFade;

	public Vector3 playerPositionOnStool;

	public void Clicked(Action removedFromHand = null)
	{
		if (cabinGameManager.currentCabinSceneType == CabinGameManager.CabinSceneType.CabinScene)
		{
			if (!playerInAttic)
			{
				if (!playerOnStool)
				{
					SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "NeedStepStool"));
					return;
				}
				playerInAttic = true;
				base.gameObject.layer = 1;
				cabinGameManager.MoveToAttic();
			}
			else
			{
				playerInAttic = false;
				base.gameObject.layer = 1;
				cabinGameManager.MoveBackFromAttic();
			}
		}
		else if (!playerInAttic)
		{
			if (cabinGameManager.hostEndGame.gameObject.activeSelf && (cabinGameManager.hostEndGame.state == HostEndGame.State.RunningToBedroomDoor || cabinGameManager.hostEndGame.state == HostEndGame.State.AtBedroomDoor || cabinGameManager.hostEndGame.state == HostEndGame.State.RunningToBedroomDoor))
			{
				SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "AtticBlock1"));
				return;
			}
			if (cabinGameManager.hostEndGame.gameObject.activeSelf && (cabinGameManager.hostEndGame.state == HostEndGame.State.GoingToBedroomLookAtPoint1 || cabinGameManager.hostEndGame.state == HostEndGame.State.AtBedroomLookAtPoint1 || cabinGameManager.hostEndGame.state == HostEndGame.State.GoingToBedroomLookAtPoint2 || cabinGameManager.hostEndGame.state == HostEndGame.State.AtBedroomLookAtPoint2 || cabinGameManager.hostEndGame.state == HostEndGame.State.GoingToBedroomLookAtPoint3 || cabinGameManager.hostEndGame.state == HostEndGame.State.AtBedroomLookAtPoint3 || cabinGameManager.hostEndGame.state == HostEndGame.State.DoorBellRang || cabinGameManager.hostEndGame.state == HostEndGame.State.GoingToBedroomWindowLookAtPoint || cabinGameManager.hostEndGame.state == HostEndGame.State.AtBedroomWindowLookAtPoint))
			{
				SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "AtticBlock2"));
				return;
			}
			if (!playerOnStool)
			{
				SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "NeedStepStool"));
				return;
			}
			playerInAttic = true;
			base.gameObject.layer = 1;
			cabinGameManager.MoveToAttic();
		}
		else
		{
			playerInAttic = false;
			base.gameObject.layer = 1;
			cabinGameManager.MoveBackFromAttic();
		}
	}

	public void SetPlayerOnStool(bool setBool)
	{
		playerOnStool = setBool;
	}

	public void SetDoorMaterial(bool toFade)
	{
		if (toFade)
		{
			doorMeshRenderer.material = doorFade;
		}
		else
		{
			doorMeshRenderer.material = doorNormal;
		}
	}

	public void FadeDoorMaterial(bool fadingToAlpha)
	{
		StopAllCoroutines();
		if (fadingToAlpha)
		{
			StartCoroutine(FadeMaterialToAlpha());
		}
		else
		{
			StartCoroutine(FadeMaterialFromAlpha());
		}
	}

	private IEnumerator FadeMaterialToAlpha()
	{
		float c = doorFade.GetColor("_Color").a;
		float timer = 0f;
		while (timer < 1f)
		{
			timer += Time.deltaTime;
			float a = Mathf.Lerp(c, 0.8f, timer);
			doorFade.SetColor("_Color", new Color(1f, 1f, 1f, a));
			yield return null;
		}
		doorFade.SetColor("_Color", new Color(1f, 1f, 1f, 0.8f));
	}

	private IEnumerator FadeMaterialFromAlpha()
	{
		float c = doorFade.GetColor("_Color").a;
		float timer = 0f;
		while (timer < 1f)
		{
			timer += Time.deltaTime;
			float a = Mathf.Lerp(c, 1f, timer);
			doorFade.SetColor("_Color", new Color(1f, 1f, 1f, a));
			yield return null;
		}
		doorFade.SetColor("_Color", new Color(1f, 1f, 1f, 1f));
	}

	[SpecialName]
	GameObject Iinteractable.get_gameObject()
	{
		return base.gameObject;
	}
}
