using System;
using System.Collections;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
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
				ShortcutExtensions.DOLocalRotate(base.transform, new Vector3(base.transform.localEulerAngles.x, 0.25f, base.transform.localEulerAngles.z), 0.1f, (RotateMode)0);
				TweenSettingsExtensions.SetDelay<TweenerCore<Quaternion, Vector3, QuaternionOptions>>(ShortcutExtensions.DOLocalRotate(base.transform, new Vector3(base.transform.localEulerAngles.x, -0.25f, base.transform.localEulerAngles.z), 0.2f, (RotateMode)0), 0.1f);
				TweenSettingsExtensions.SetDelay<TweenerCore<Quaternion, Vector3, QuaternionOptions>>(ShortcutExtensions.DOLocalRotate(base.transform, new Vector3(base.transform.localEulerAngles.x, 0f, base.transform.localEulerAngles.z), 0.1f, (RotateMode)0), 0.3f);
				TweenSettingsExtensions.SetDelay<TweenerCore<Quaternion, Vector3, QuaternionOptions>>(ShortcutExtensions.DOLocalRotate(base.transform, new Vector3(base.transform.localEulerAngles.x, 0.25f, base.transform.localEulerAngles.z), 0.1f, (RotateMode)0), 0.4f);
				TweenSettingsExtensions.SetDelay<TweenerCore<Quaternion, Vector3, QuaternionOptions>>(ShortcutExtensions.DOLocalRotate(base.transform, new Vector3(base.transform.localEulerAngles.x, 0f, base.transform.localEulerAngles.z), 0.1f, (RotateMode)0), 0.5f);
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
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		TweenCallback onComplete = ((!setInteractible) ? ((TweenCallback)null) : new TweenCallback(SetInteractibleTrue));
		switch (doorOpeningAxis)
		{
		case DoorOpeningAxis.Xaxis:
			((Tween)ShortcutExtensions.DOLocalMoveX(base.transform, originalDoorPosition.x + doorSlideValue, 0.5f, false)).onComplete = onComplete;
			break;
		case DoorOpeningAxis.Yaxis:
			((Tween)ShortcutExtensions.DOLocalMoveY(base.transform, originalDoorPosition.y + doorSlideValue, 0.5f, false)).onComplete = onComplete;
			break;
		case DoorOpeningAxis.Zaxis:
			((Tween)ShortcutExtensions.DOLocalMoveZ(base.transform, originalDoorPosition.z + doorSlideValue, 0.5f, false)).onComplete = onComplete;
			break;
		}
		if (playSFX)
		{
			this.OnDoorPlaySound?.Invoke(doorAudioSource, doorType, DoorInteraction.Open, doorOpenCloseVolume);
		}
	}

	private void OpenRotatingDoor(bool setInteractible = true, bool playSFX = true)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		TweenCallback onComplete = ((!setInteractible) ? ((TweenCallback)null) : new TweenCallback(SetInteractibleTrue));
		Tween val = null;
		switch (doorOpeningAxis)
		{
		case DoorOpeningAxis.Xaxis:
			val = (Tween)(object)TweenSettingsExtensions.SetEase<TweenerCore<Quaternion, Vector3, QuaternionOptions>>(ShortcutExtensions.DOLocalRotate(base.transform, originalDoorRotation + Vector3.right * doorRotationAngle, 0.5f, (RotateMode)0), (Ease)3);
			break;
		case DoorOpeningAxis.Yaxis:
			val = (Tween)(object)TweenSettingsExtensions.SetEase<TweenerCore<Quaternion, Vector3, QuaternionOptions>>(ShortcutExtensions.DOLocalRotate(base.transform, originalDoorRotation + Vector3.up * doorRotationAngle, 0.5f, (RotateMode)0), (Ease)3);
			break;
		case DoorOpeningAxis.Zaxis:
			val = (Tween)(object)TweenSettingsExtensions.SetEase<TweenerCore<Quaternion, Vector3, QuaternionOptions>>(ShortcutExtensions.DOLocalRotate(base.transform, originalDoorRotation + Vector3.forward * doorRotationAngle, 0.5f, (RotateMode)0), (Ease)3);
			break;
		}
		val.onComplete = onComplete;
		if (playSFX)
		{
			this.OnDoorPlaySound?.Invoke(doorAudioSource, doorType, DoorInteraction.Open, doorOpenCloseVolume);
		}
	}

	private void CloseSlidingDoor(bool byPlayer = true)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected O, but got Unknown
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected O, but got Unknown
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Expected O, but got Unknown
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Expected O, but got Unknown
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Expected O, but got Unknown
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Expected O, but got Unknown
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Expected O, but got Unknown
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Expected O, but got Unknown
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Expected O, but got Unknown
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Expected O, but got Unknown
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Expected O, but got Unknown
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Expected O, but got Unknown
		switch (doorOpeningAxis)
		{
		case DoorOpeningAxis.Xaxis:
		{
			Tween val = (Tween)(object)ShortcutExtensions.DOLocalMoveX(base.transform, originalDoorPosition.x, 0.5f, false);
			Tween obj3 = val;
			obj3.onComplete = (TweenCallback)Delegate.Combine((Delegate?)(object)obj3.onComplete, (Delegate?)new TweenCallback(SetInteractibleTrue));
			if (byPlayer)
			{
				Tween obj4 = val;
				obj4.onComplete = (TweenCallback)Delegate.Combine((Delegate?)(object)obj4.onComplete, (Delegate?)new TweenCallback(SetAmbience));
			}
			break;
		}
		case DoorOpeningAxis.Yaxis:
		{
			Tween val = (Tween)(object)ShortcutExtensions.DOLocalMoveY(base.transform, originalDoorPosition.y, 0.5f, false);
			Tween obj5 = val;
			obj5.onComplete = (TweenCallback)Delegate.Combine((Delegate?)(object)obj5.onComplete, (Delegate?)new TweenCallback(SetInteractibleTrue));
			if (byPlayer)
			{
				Tween obj6 = val;
				obj6.onComplete = (TweenCallback)Delegate.Combine((Delegate?)(object)obj6.onComplete, (Delegate?)new TweenCallback(SetAmbience));
			}
			break;
		}
		case DoorOpeningAxis.Zaxis:
		{
			Tween val = (Tween)(object)ShortcutExtensions.DOLocalMoveZ(base.transform, originalDoorPosition.z, 0.5f, false);
			Tween obj = val;
			obj.onComplete = (TweenCallback)Delegate.Combine((Delegate?)(object)obj.onComplete, (Delegate?)new TweenCallback(SetInteractibleTrue));
			if (byPlayer)
			{
				Tween obj2 = val;
				obj2.onComplete = (TweenCallback)Delegate.Combine((Delegate?)(object)obj2.onComplete, (Delegate?)new TweenCallback(SetAmbience));
			}
			break;
		}
		}
	}

	private void CloseRotatingDoor(bool byPlayer = true)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected O, but got Unknown
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Expected O, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Expected O, but got Unknown
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Expected O, but got Unknown
		Tween val = (Tween)(object)TweenSettingsExtensions.SetEase<TweenerCore<Quaternion, Vector3, QuaternionOptions>>(ShortcutExtensions.DOLocalRotate(base.transform, originalDoorRotation, 0.5f, (RotateMode)0), (Ease)2);
		val.onComplete = (TweenCallback)Delegate.Combine((Delegate?)(object)val.onComplete, (Delegate?)new TweenCallback(SetInteractibleTrue));
		if (byPlayer)
		{
			val.onComplete = (TweenCallback)Delegate.Combine((Delegate?)(object)val.onComplete, (Delegate?)new TweenCallback(SetAmbience));
		}
		val.onComplete = (TweenCallback)Delegate.Combine((Delegate?)(object)val.onComplete, (Delegate?)(TweenCallback)delegate
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
		ShortcutExtensions.DOLocalRotate(base.transform, new Vector3(base.transform.localEulerAngles.x, 0.25f, base.transform.localEulerAngles.z), 0.1f, (RotateMode)0);
		TweenSettingsExtensions.SetDelay<TweenerCore<Quaternion, Vector3, QuaternionOptions>>(ShortcutExtensions.DOLocalRotate(base.transform, new Vector3(base.transform.localEulerAngles.x, -0.25f, base.transform.localEulerAngles.z), 0.2f, (RotateMode)0), 0.1f);
		TweenSettingsExtensions.SetDelay<TweenerCore<Quaternion, Vector3, QuaternionOptions>>(ShortcutExtensions.DOLocalRotate(base.transform, new Vector3(base.transform.localEulerAngles.x, 0f, base.transform.localEulerAngles.z), 0.1f, (RotateMode)0), 0.3f);
		TweenSettingsExtensions.SetDelay<TweenerCore<Quaternion, Vector3, QuaternionOptions>>(ShortcutExtensions.DOLocalRotate(base.transform, new Vector3(base.transform.localEulerAngles.x, 0.25f, base.transform.localEulerAngles.z), 0.1f, (RotateMode)0), 0.4f);
		TweenSettingsExtensions.SetDelay<TweenerCore<Quaternion, Vector3, QuaternionOptions>>(ShortcutExtensions.DOLocalRotate(base.transform, new Vector3(base.transform.localEulerAngles.x, 0f, base.transform.localEulerAngles.z), 0.1f, (RotateMode)0), 0.5f);
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
}
