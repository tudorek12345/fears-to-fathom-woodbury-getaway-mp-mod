using System;
using System.Collections;
using System.Runtime.CompilerServices;
using DG.Tweening;
using UnityEngine;

[Serializable]
public class CabinDoor : MonoBehaviour, Iinteractable
{
	private enum DoorOpeningAxis
	{
		Xaxis,
		Yaxis,
		Zaxis
	}

	private enum DoorPosition
	{
		Outside,
		Inside,
		DeepInside
	}

	[SerializeField]
	public bool isInteractable;

	[SerializeField]
	public bool affectsAudioMixer = true;

	[SerializeField]
	private DoorType doorType;

	[SerializeField]
	private DoorOpeningAxis doorOpeningAxis = DoorOpeningAxis.Yaxis;

	[SerializeField]
	private DoorPosition doorPosition;

	[SerializeField]
	public float doorRotationAngle = -90f;

	[SerializeField]
	private float doorSlideValue = 0.8f;

	[Header("Audio Related")]
	[SerializeField]
	private AudioSource doorAudioSource;

	[SerializeField]
	private float doorOpenCloseVolume = 0.8f;

	[SerializeField]
	private float maxPitch = 1f;

	[SerializeField]
	private float minPitch = 0.5f;

	[SerializeField]
	private OcclusionPortal occlusionPortal;

	private Vector3 originalDoorPosition;

	private Vector3 originalDoorRotation;

	[Header("Main Door")]
	[SerializeField]
	protected CabinGameManager cabinGameManager;

	[SerializeField]
	public bool isLocked;

	[SerializeField]
	public bool mikeIsInside;

	[SerializeField]
	private AudioSource jammedAS;

	[SerializeField]
	private AudioSource keyTurn;

	[HideInInspector]
	public bool hasKey;

	[Header("Bedroom Jumpscare Door")]
	[SerializeField]
	private bool isBedroomJumpscareDoor;

	[HideInInspector]
	public bool isPlayerInsideBedroom;

	[Header("Basement Door")]
	[SerializeField]
	private bool isBasementDoor;

	[SerializeField]
	private bool isBasementEntranceDoor;

	public static bool mikeWillClose = true;

	public static bool hostWillClose = true;

	public static bool hostSetsNonInteractive = false;

	[field: SerializeField]
	public bool IsOpen { get; set; }

	public event Action OnDoorOpened;

	public event Action OnDoorOpenedInstantly;

	public event Action OnDoorOpenedByNPC;

	public event Action OnDoorClosed;

	public event Action OnAfterDoorClosed;

	public event Action<AudioSource, DoorType, DoorInteraction, float> OnDoorPlaySound;

	private void Awake()
	{
		_ = isBasementEntranceDoor;
		if (isBedroomJumpscareDoor)
		{
			OnAfterDoorClosed += SetTvVolume;
			OnDoorOpened += cabinGameManager.cabinHouseManager.usptairsTvVolumeControl.SetVolumeInRoom;
		}
	}

	private void Start()
	{
		mikeWillClose = true;
		hostWillClose = true;
		if (TryGetComponent<OcclusionPortal>(out occlusionPortal))
		{
			occlusionPortal.open = false;
		}
		if (doorType == DoorType.DoubleDoor)
		{
			occlusionPortal = null;
		}
		originalDoorRotation = base.transform.localRotation.eulerAngles;
		originalDoorPosition = base.transform.localPosition;
	}

	void Iinteractable.Clicked(Action removedFromHand)
	{
		if (isLocked)
		{
			if (hasKey)
			{
				keyTurn.Play();
				isLocked = false;
				cabinGameManager.keysUI.SetActive(value: false);
			}
			else if (!jammedAS.isPlaying)
			{
				DoorJammed();
			}
		}
		else if (mikeIsInside)
		{
			if (!jammedAS.isPlaying)
			{
				DoorJammed();
			}
		}
		else if (isBasementDoor)
		{
			if (!jammedAS.isPlaying)
			{
				jammedAS.Play();
				base.transform.DOLocalRotate(new Vector3(base.transform.localEulerAngles.x, 0.25f, base.transform.localEulerAngles.z), 0.1f);
				base.transform.DOLocalRotate(new Vector3(base.transform.localEulerAngles.x, -0.25f, base.transform.localEulerAngles.z), 0.2f).SetDelay(0.1f);
				base.transform.DOLocalRotate(new Vector3(base.transform.localEulerAngles.x, 0f, base.transform.localEulerAngles.z), 0.1f).SetDelay(0.3f);
				base.transform.DOLocalRotate(new Vector3(base.transform.localEulerAngles.x, 0.25f, base.transform.localEulerAngles.z), 0.1f).SetDelay(0.4f);
				base.transform.DOLocalRotate(new Vector3(base.transform.localEulerAngles.x, 0f, base.transform.localEulerAngles.z), 0.1f).SetDelay(0.5f);
			}
			if (!cabinGameManager.cabinHouseManager.basementDoorDialogueDone && cabinGameManager.cabinHouseManager.isMikeTouring)
			{
				SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "CantOpenDoor"));
				cabinGameManager.cabinHouseManager.doBasementDoorDialogue = true;
				cabinGameManager.cabinHouseManager.dobasementDialogue = false;
			}
			else
			{
				SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "CantOpenDoor"));
			}
		}
		else if (isBasementEntranceDoor)
		{
			if (isInteractable)
			{
				if (IsOpen)
				{
					CloseDoor();
				}
				else
				{
					OpenDoor();
				}
			}
		}
		else
		{
			CheckInteraction();
		}
	}

	public void SetMikeWillClose(bool value)
	{
		mikeWillClose = value;
	}

	public void SetInteractable(bool value)
	{
		isInteractable = value;
		base.gameObject.layer = (value ? LayerMask.NameToLayer("Default") : LayerMask.NameToLayer("Ignore Raycast"));
	}

	public virtual void OpenDoor(bool playSFX = true)
	{
		if (!IsOpen)
		{
			IsOpen = true;
			this.OnDoorOpenedInstantly?.Invoke();
			SetInteractable(value: false);
			if (occlusionPortal != null)
			{
				occlusionPortal.open = true;
			}
			switch (doorType)
			{
			case DoorType.Rotate:
			case DoorType.DoubleDoor:
			case DoorType.FenceDoor:
			case DoorType.ShedDoor:
			case DoorType.FridgeDoor:
			case DoorType.OvenDoor:
			case DoorType.MiniCoolerLid:
			case DoorType.FrontRotate:
			case DoorType.ShowerDoor:
			case DoorType.BasementStairsDoor:
				OpenRotatingDoor(setInteractible: true, playSFX);
				break;
			case DoorType.Slide:
				OpenSlidingDoor(setInteractible: true, playSFX);
				break;
			}
			this.OnDoorOpened?.Invoke();
			SetAmbienceToOutside();
		}
	}

	public void OpenDoorNotInteractible()
	{
		IsOpen = true;
		this.OnDoorOpenedInstantly?.Invoke();
		SetInteractable(value: false);
		if (occlusionPortal != null)
		{
			occlusionPortal.open = true;
		}
		switch (doorType)
		{
		case DoorType.Rotate:
		case DoorType.DoubleDoor:
		case DoorType.FridgeDoor:
		case DoorType.OvenDoor:
		case DoorType.MiniCoolerLid:
		case DoorType.FrontRotate:
		case DoorType.ShowerDoor:
		case DoorType.BasementStairsDoor:
			OpenRotatingDoor(setInteractible: false);
			break;
		case DoorType.Slide:
			OpenSlidingDoor(setInteractible: false);
			break;
		}
		this.OnDoorOpened?.Invoke();
		SetAmbienceToOutside();
	}

	public void CloseDoor()
	{
		if (IsOpen)
		{
			IsOpen = false;
			SetInteractable(value: false);
			switch (doorType)
			{
			case DoorType.Rotate:
			case DoorType.DoubleDoor:
			case DoorType.FenceDoor:
			case DoorType.ShedDoor:
			case DoorType.FridgeDoor:
			case DoorType.OvenDoor:
			case DoorType.MiniCoolerLid:
			case DoorType.FrontRotate:
			case DoorType.ShowerDoor:
			case DoorType.BasementStairsDoor:
				CloseRotatingDoor();
				StartCoroutine(_Close());
				break;
			case DoorType.Slide:
				CloseSlidingDoor();
				this.OnDoorPlaySound?.Invoke(doorAudioSource, doorType, DoorInteraction.Close, doorOpenCloseVolume);
				this.OnDoorClosed?.Invoke();
				break;
			}
		}
		IEnumerator _Close()
		{
			yield return new WaitForSeconds(0.2f);
			this.OnDoorPlaySound?.Invoke(doorAudioSource, doorType, DoorInteraction.Close, doorOpenCloseVolume);
			yield return new WaitForSeconds(0.3f);
			if (occlusionPortal != null && !IsOpen)
			{
				occlusionPortal.open = false;
			}
			this.OnAfterDoorClosed?.Invoke();
		}
	}

	private void OpenSlidingDoor(bool setInteractible = true, bool playSFX = true)
	{
		TweenCallback onComplete = (setInteractible ? new TweenCallback(SetInteractibleTrue) : null);
		switch (doorOpeningAxis)
		{
		case DoorOpeningAxis.Xaxis:
			base.transform.DOLocalMoveX(originalDoorPosition.x + doorSlideValue, 0.5f).onComplete = onComplete;
			break;
		case DoorOpeningAxis.Yaxis:
			base.transform.DOLocalMoveY(originalDoorPosition.y + doorSlideValue, 0.5f).onComplete = onComplete;
			break;
		case DoorOpeningAxis.Zaxis:
			base.transform.DOLocalMoveZ(originalDoorPosition.z + doorSlideValue, 0.5f).onComplete = onComplete;
			break;
		}
		if (playSFX)
		{
			this.OnDoorPlaySound?.Invoke(doorAudioSource, doorType, DoorInteraction.Open, doorOpenCloseVolume);
		}
	}

	private void OpenRotatingDoor(bool setInteractible = true, bool playSFX = true)
	{
		TweenCallback onComplete = (setInteractible ? new TweenCallback(SetInteractibleTrue) : null);
		Tween tween = null;
		switch (doorOpeningAxis)
		{
		case DoorOpeningAxis.Xaxis:
			tween = base.transform.DOLocalRotate(originalDoorRotation + Vector3.right * doorRotationAngle, 0.5f).SetEase(Ease.OutSine);
			break;
		case DoorOpeningAxis.Yaxis:
			tween = base.transform.DOLocalRotate(originalDoorRotation + Vector3.up * doorRotationAngle, 0.5f).SetEase(Ease.OutSine);
			break;
		case DoorOpeningAxis.Zaxis:
			tween = base.transform.DOLocalRotate(originalDoorRotation + Vector3.forward * doorRotationAngle, 0.5f).SetEase(Ease.OutSine);
			break;
		}
		tween.onComplete = onComplete;
		if (playSFX)
		{
			this.OnDoorPlaySound?.Invoke(doorAudioSource, doorType, DoorInteraction.Open, doorOpenCloseVolume);
		}
	}

	private void CloseSlidingDoor(bool byPlayer = true)
	{
		switch (doorOpeningAxis)
		{
		case DoorOpeningAxis.Xaxis:
		{
			Tween tween = base.transform.DOLocalMoveX(originalDoorPosition.x, 0.5f);
			Tween tween4 = tween;
			tween4.onComplete = (TweenCallback)Delegate.Combine(tween4.onComplete, new TweenCallback(SetInteractibleTrue));
			if (byPlayer)
			{
				Tween tween5 = tween;
				tween5.onComplete = (TweenCallback)Delegate.Combine(tween5.onComplete, new TweenCallback(SetAmbience));
			}
			break;
		}
		case DoorOpeningAxis.Yaxis:
		{
			Tween tween = base.transform.DOLocalMoveY(originalDoorPosition.y, 0.5f);
			Tween tween6 = tween;
			tween6.onComplete = (TweenCallback)Delegate.Combine(tween6.onComplete, new TweenCallback(SetInteractibleTrue));
			if (byPlayer)
			{
				Tween tween7 = tween;
				tween7.onComplete = (TweenCallback)Delegate.Combine(tween7.onComplete, new TweenCallback(SetAmbience));
			}
			break;
		}
		case DoorOpeningAxis.Zaxis:
		{
			Tween tween = base.transform.DOLocalMoveZ(originalDoorPosition.z, 0.5f);
			Tween tween2 = tween;
			tween2.onComplete = (TweenCallback)Delegate.Combine(tween2.onComplete, new TweenCallback(SetInteractibleTrue));
			if (byPlayer)
			{
				Tween tween3 = tween;
				tween3.onComplete = (TweenCallback)Delegate.Combine(tween3.onComplete, new TweenCallback(SetAmbience));
			}
			break;
		}
		}
	}

	private void CloseRotatingDoor(bool byPlayer = true)
	{
		Tween tween = base.transform.DOLocalRotate(originalDoorRotation, 0.5f).SetEase(Ease.InSine);
		tween.onComplete = (TweenCallback)Delegate.Combine(tween.onComplete, new TweenCallback(SetInteractibleTrue));
		if (byPlayer)
		{
			tween.onComplete = (TweenCallback)Delegate.Combine(tween.onComplete, new TweenCallback(SetAmbience));
		}
		tween.onComplete = (TweenCallback)Delegate.Combine(tween.onComplete, (TweenCallback)delegate
		{
			this.OnDoorClosed?.Invoke();
		});
	}

	private void CheckInteraction()
	{
		if (isInteractable)
		{
			if (IsOpen)
			{
				CloseDoor();
			}
			else
			{
				OpenDoor();
			}
		}
	}

	private void DoorJammed()
	{
		jammedAS.Play();
		base.transform.DOLocalRotate(new Vector3(base.transform.localEulerAngles.x, 0.25f, base.transform.localEulerAngles.z), 0.1f);
		base.transform.DOLocalRotate(new Vector3(base.transform.localEulerAngles.x, -0.25f, base.transform.localEulerAngles.z), 0.2f).SetDelay(0.1f);
		base.transform.DOLocalRotate(new Vector3(base.transform.localEulerAngles.x, 0f, base.transform.localEulerAngles.z), 0.1f).SetDelay(0.3f);
		base.transform.DOLocalRotate(new Vector3(base.transform.localEulerAngles.x, 0.25f, base.transform.localEulerAngles.z), 0.1f).SetDelay(0.4f);
		base.transform.DOLocalRotate(new Vector3(base.transform.localEulerAngles.x, 0f, base.transform.localEulerAngles.z), 0.1f).SetDelay(0.5f);
		if (mikeIsInside)
		{
			SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "MikeInside"));
		}
		else
		{
			SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "DoorLocked"));
		}
	}

	public void OpenDoorNPC()
	{
		if (!IsOpen)
		{
			IsOpen = true;
			SetInteractable(value: false);
			if (occlusionPortal != null)
			{
				occlusionPortal.open = true;
			}
			switch (doorType)
			{
			case DoorType.Rotate:
			case DoorType.DoubleDoor:
			case DoorType.FenceDoor:
			case DoorType.ShedDoor:
			case DoorType.FridgeDoor:
			case DoorType.OvenDoor:
			case DoorType.MiniCoolerLid:
			case DoorType.FrontRotate:
			case DoorType.ShowerDoor:
			case DoorType.BasementStairsDoor:
				OpenRotatingDoor();
				break;
			case DoorType.Slide:
				OpenSlidingDoor();
				break;
			}
			this.OnDoorOpenedByNPC?.Invoke();
		}
	}

	public void CloseDoorMike()
	{
		if (IsOpen && mikeWillClose)
		{
			IsOpen = false;
			SetInteractable(value: false);
			switch (doorType)
			{
			case DoorType.Rotate:
			case DoorType.DoubleDoor:
			case DoorType.FenceDoor:
			case DoorType.ShedDoor:
			case DoorType.FridgeDoor:
			case DoorType.OvenDoor:
			case DoorType.MiniCoolerLid:
			case DoorType.FrontRotate:
			case DoorType.ShowerDoor:
			case DoorType.BasementStairsDoor:
				CloseRotatingDoor(byPlayer: false);
				StartCoroutine(_Close());
				break;
			case DoorType.Slide:
				CloseSlidingDoor(byPlayer: false);
				this.OnDoorPlaySound?.Invoke(doorAudioSource, doorType, DoorInteraction.Close, doorOpenCloseVolume);
				this.OnDoorClosed?.Invoke();
				break;
			}
		}
		IEnumerator _Close()
		{
			yield return new WaitForSeconds(0.2f);
			this.OnDoorPlaySound?.Invoke(doorAudioSource, doorType, DoorInteraction.Close, doorOpenCloseVolume);
			this.OnDoorClosed?.Invoke();
			yield return new WaitForSeconds(0.3f);
			if (occlusionPortal != null && !IsOpen)
			{
				occlusionPortal.open = false;
			}
		}
	}

	public void CloseDoorHost()
	{
		if (IsOpen && hostWillClose)
		{
			IsOpen = false;
			SetInteractable(value: false);
			switch (doorType)
			{
			case DoorType.Rotate:
			case DoorType.DoubleDoor:
			case DoorType.FenceDoor:
			case DoorType.ShedDoor:
			case DoorType.FridgeDoor:
			case DoorType.OvenDoor:
			case DoorType.MiniCoolerLid:
			case DoorType.FrontRotate:
			case DoorType.ShowerDoor:
			case DoorType.BasementStairsDoor:
				CloseRotatingDoor(byPlayer: false);
				StartCoroutine(_Close());
				break;
			case DoorType.Slide:
				CloseSlidingDoor(byPlayer: false);
				this.OnDoorPlaySound?.Invoke(doorAudioSource, doorType, DoorInteraction.Close, doorOpenCloseVolume);
				this.OnDoorClosed?.Invoke();
				break;
			}
		}
		IEnumerator _Close()
		{
			yield return new WaitForSeconds(0.2f);
			this.OnDoorPlaySound?.Invoke(doorAudioSource, doorType, DoorInteraction.Close, doorOpenCloseVolume);
			this.OnDoorClosed?.Invoke();
			yield return new WaitForSeconds(0.3f);
			if (occlusionPortal != null && !IsOpen)
			{
				occlusionPortal.open = false;
			}
			if (hostSetsNonInteractive)
			{
				SetInteractable(value: false);
			}
		}
	}

	public void SetPlayerInsideBedroomBool(bool isPlayerInsideBedroom)
	{
		this.isPlayerInsideBedroom = isPlayerInsideBedroom;
	}

	public void SetTvVolume()
	{
		if (!isPlayerInsideBedroom)
		{
			cabinGameManager.cabinHouseManager.usptairsTvVolumeControl.SetVolumeInstantOutsideRoom();
		}
	}

	public void SetAmbience()
	{
		if (affectsAudioMixer)
		{
			switch (CabinPlayerController.playerLocation)
			{
			case CabinPlayerController.PlayerLocation.PlayerOutside:
				AudioMixerManager.PlayerOutside();
				break;
			case CabinPlayerController.PlayerLocation.PlayerInside:
				AudioMixerManager.PlayerInside();
				break;
			case CabinPlayerController.PlayerLocation.PlayerDeepInside:
				AudioMixerManager.PlayerDeepInside();
				break;
			}
		}
	}

	public void SetAmbienceToOutside()
	{
		if (!affectsAudioMixer)
		{
			return;
		}
		switch (CabinPlayerController.playerLocation)
		{
		case CabinPlayerController.PlayerLocation.PlayerOutside:
			AudioMixerManager.PlayerOutside();
			break;
		case CabinPlayerController.PlayerLocation.PlayerInside:
			if (doorPosition == DoorPosition.Inside)
			{
				AudioMixerManager.PlayerInside();
			}
			else
			{
				AudioMixerManager.PlayerOutside();
			}
			break;
		case CabinPlayerController.PlayerLocation.PlayerDeepInside:
			if (doorPosition == DoorPosition.Inside)
			{
				AudioMixerManager.PlayerInside();
			}
			else
			{
				AudioMixerManager.PlayerDeepInside();
			}
			break;
		}
	}

	public void CheckDoorOpenAndSetAmb(bool isDeepInside = false)
	{
		if (IsOpen)
		{
			if (isDeepInside)
			{
				AudioMixerManager.PlayerInside();
			}
			else
			{
				AudioMixerManager.PlayerOutside();
			}
		}
		else if (isDeepInside)
		{
			AudioMixerManager.PlayerDeepInside();
		}
		else
		{
			AudioMixerManager.PlayerInside();
		}
	}

	public void SetAmbience(int state)
	{
		switch (state)
		{
		case 0:
			AudioMixerManager.PlayerOutside();
			break;
		case 1:
			AudioMixerManager.PlayerInside();
			break;
		case 2:
			AudioMixerManager.PlayerDeepInside();
			break;
		}
	}

	public void SetInteractibleTrue()
	{
		SetInteractable(value: true);
	}

	[SpecialName]
	GameObject Iinteractable.get_gameObject()
	{
		return base.gameObject;
	}
}
