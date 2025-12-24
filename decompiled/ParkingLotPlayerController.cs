using System.Collections;
using DG.Tweening;
using PixelCrushers.DialogueSystem;
using UnityEngine;

public class ParkingLotPlayerController : PlayerController
{
	[SerializeField]
	private Camera mainCamera;

	[Header("Script References")]
	[SerializeField]
	private ParkingLotUIManager parkingLotUIManager;

	[SerializeField]
	private ParkingLotGameManager parkingLotGameManager;

	[SerializeField]
	private CameraShake cameraShake;

	private float lerpSpeed = 3f;

	[HideInInspector]
	public Transform lookAt;

	public MikeParkingLot mike;

	public Transform mikeHug;

	public bool canThrowItem = true;

	internal override void Awake()
	{
		base.Awake();
		parkingLotUIManager.ClearControlsText();
	}

	internal override void Start()
	{
		base.Start();
		ScreenShake();
	}

	internal override void Update()
	{
		base.Update();
		if (parkingLotGameManager.currentPlayerState == ParkingLotGameManager.PlayerState.Talking)
		{
			mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, 40f, Time.deltaTime * lerpSpeed);
			Vector3 forward = lookAt.position - mainCamera.transform.position;
			mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, Quaternion.LookRotation(forward), Time.deltaTime * lerpSpeed);
		}
	}

	internal override void OnZoom()
	{
		base.OnZoom();
	}

	internal override void Throw()
	{
		if (canThrowItem && currentHoldingObject != null)
		{
			base.Throw();
			parkingLotUIManager.EnableItemUse();
			parkingLotUIManager.ClearControlsText();
		}
	}

	internal override void HoldObject(Holdable holdable, bool isThrowable = false)
	{
		parkingLotUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "Throw"));
		base.HoldObject(holdable);
	}

	public void ScreenShake()
	{
		StartCoroutine(cameraShake.Shake(10f, 0.0005f));
	}

	public void ScreenShakeBottom()
	{
		StartCoroutine(CameraShakeDelay());
	}

	private IEnumerator CameraShakeDelay()
	{
		yield return new WaitForSeconds(0.28f);
		cameraShake.StopAllCoroutines();
		StartCoroutine(cameraShake.Shake(0.1f, 0.005f));
	}

	public void Hug()
	{
		StartCoroutine(_Hug());
		IEnumerator _Hug()
		{
			StartCoroutine(RigUtility.UpdateHeadRig(mike.headRigPlayer, 0f, 1f));
			DialogueManager.StopConversation();
			parkingLotGameManager.currentPlayerState = ParkingLotGameManager.PlayerState.Hugging;
			mainCamera.transform.DOMove(mikeHug.transform.position, 1f).SetEase(Ease.InOutSine);
			StartCoroutine(mike.HugPlayer(1.5f));
			yield return new WaitForSeconds(1.5f);
			mainCamera.transform.DOLocalMove(new Vector3(0f, 0f, 0f), 1f).SetEase(Ease.InOutSine);
			parkingLotGameManager.currentPlayerState = ParkingLotGameManager.PlayerState.Talking;
			StartCoroutine(RigUtility.UpdateHeadRig(mike.headRigPlayer, 1f, 1f));
			DialogueManager.StartConversation("Mike Parking Lot", base.transform, base.transform, 3);
		}
	}

	public void LongHug()
	{
		StartCoroutine(_LongHug());
		IEnumerator _LongHug()
		{
			StartCoroutine(RigUtility.UpdateHeadRig(mike.headRigPlayer, 0f, 1f));
			DialogueManager.StopConversation();
			parkingLotGameManager.currentPlayerState = ParkingLotGameManager.PlayerState.Hugging;
			mainCamera.transform.DOMove(mikeHug.transform.position, 1f).SetEase(Ease.InOutSine);
			StartCoroutine(mike.HugPlayer(2f));
			yield return new WaitForSeconds(2f);
			mainCamera.transform.DOLocalMove(new Vector3(0f, 0f, 0f), 1f).SetEase(Ease.InOutSine);
			parkingLotGameManager.currentPlayerState = ParkingLotGameManager.PlayerState.Talking;
			StartCoroutine(RigUtility.UpdateHeadRig(mike.headRigPlayer, 1f, 1f));
			DialogueManager.StartConversation("Mike Parking Lot", base.transform, base.transform, 8);
		}
	}
}
