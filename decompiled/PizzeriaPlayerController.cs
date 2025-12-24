using System;
using System.Collections;
using PixelCrushers.DialogueSystem;
using UnityEngine;

public class PizzeriaPlayerController : PlayerController
{
	[SerializeField]
	private Camera camera;

	[SerializeField]
	private PizzeriaGameManager pizzeriaGameManager;

	[SerializeField]
	private Phone phone;

	[SerializeField]
	private PizzerriaUIManager pizzerriaUIManager;

	[SerializeField]
	private float turnToMikeSpeed;

	[SerializeField]
	private float zoomToMikeDuration = 2f;

	[SerializeField]
	private Transform mikeTransform;

	[SerializeField]
	private Transform pizzeriaTransform;

	[SerializeField]
	private float mikeZoom = 40f;

	[SerializeField]
	private DrivingCam drivingCam;

	[SerializeField]
	private FovZoom fovZoom;

	[SerializeField]
	public MikePizzeria mike;

	private Transform lookAtObject;

	private float defaultZoom;

	[SerializeField]
	private PerlinCameraShake perlinCameraShake;

	[SerializeField]
	private Camera dialogueCamera;

	private bool zoomIntoTransform;

	private bool returnToDefaultZoom;

	public AudioSource handBreakAS;

	private float lerpSpeed = 3f;

	public Transform lookHere;

	[SerializeField]
	private Camera mainCamera;

	public GameObject playerDrivingParent;

	public GameObject playerDrivingCam;

	public Transform firstPersonCam;

	public Transform lookHereCashier;

	public Transform lookHereMike;

	public Transform lookHereHatGuy;

	public Transform lookHereGreyShirt;

	public Transform lookHereYoungMan;

	public Transform lookHereBlackWoman;

	public Transform lookHereHobo;

	public Transform lookHereHiker;

	[HideInInspector]
	public PizzeriaNPC currentPizzeriaNPC;

	[HideInInspector]
	public bool playerSitting;

	public GameObject playerSitdownPizzeria;

	[SerializeField]
	public Camera sittingDownCamera;

	[SerializeField]
	private SittingCam sittingCamPizzeria;

	[SerializeField]
	public FovZoom fovZoomPizzeria;

	public PizzeriaFoldingGuy boxFoldingGuy;

	public Transform boxFoldingGuyHead;

	public bool guyLookingAtPlayer;

	public bool lookingAtGuy;

	private int frameCheck;

	public GameObject pizzaOnTable;

	public AudioSource trashCan;

	public bool canThrowItem = true;

	[Header("Testing")]
	[SerializeField]
	private Transform testingStartInsidePosition;

	internal override void Awake()
	{
		base.Awake();
		defaultZoom = camera.fieldOfView;
	}

	internal override void Start()
	{
		base.Start();
	}

	private new void Update()
	{
		base.Update();
		if (zoomIntoTransform)
		{
			camera.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, mikeZoom, Time.deltaTime * turnToMikeSpeed);
			Vector3 forward = lookAtObject.position - camera.transform.position;
			camera.transform.rotation = Quaternion.Lerp(camera.transform.rotation, Quaternion.LookRotation(forward), Time.deltaTime * turnToMikeSpeed);
		}
		if (returnToDefaultZoom)
		{
			camera.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, defaultZoom, Time.deltaTime * turnToMikeSpeed);
		}
		if (pizzeriaGameManager.currentPlayerState == PizzeriaGameManager.PlayerState.Talking)
		{
			mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, 40f, Time.deltaTime * lerpSpeed);
			Vector3 forward2 = lookHere.position - mainCamera.transform.position;
			mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, Quaternion.LookRotation(forward2), Time.deltaTime * lerpSpeed);
		}
		if (pizzeriaGameManager.currentPlayerState == PizzeriaGameManager.PlayerState.TalkingSitting)
		{
			sittingDownCamera.fieldOfView = Mathf.Lerp(sittingDownCamera.fieldOfView, 40f, Time.deltaTime * lerpSpeed);
			Vector3 forward3 = lookHere.transform.position - sittingDownCamera.transform.parent.position;
			sittingCamPizzeria.transform.rotation = Quaternion.Lerp(sittingCamPizzeria.transform.rotation, Quaternion.LookRotation(forward3), Time.deltaTime * lerpSpeed);
		}
		if (pizzeriaGameManager.currentPlayerState == PizzeriaGameManager.PlayerState.Consuming)
		{
			sittingDownCamera.fieldOfView = Mathf.Lerp(sittingDownCamera.fieldOfView, 40f, Time.deltaTime * lerpSpeed);
			Vector3 forward4 = lookHere.transform.position - sittingDownCamera.transform.parent.position;
			sittingCamPizzeria.transform.rotation = Quaternion.Lerp(sittingCamPizzeria.transform.rotation, Quaternion.LookRotation(forward4), Time.deltaTime * lerpSpeed);
		}
		frameCheck++;
		if (!playerSitdownPizzeria.activeSelf || frameCheck < 10)
		{
			return;
		}
		frameCheck = 0;
		Vector3 vector = sittingDownCamera.WorldToViewportPoint(boxFoldingGuyHead.position);
		if (vector.x > 0f && vector.x < 1f && vector.y > 0f && vector.y < 1f && vector.z > 0f)
		{
			if (sittingCamPizzeria.xRotation > -18f && sittingCamPizzeria.xRotation < 18f && sittingCamPizzeria.yRotation < 165f && sittingCamPizzeria.yRotation > 91f)
			{
				lookingAtGuy = true;
				if (guyLookingAtPlayer)
				{
					guyLookingAtPlayer = false;
					boxFoldingGuy.LookAwayAndDown();
				}
			}
			else
			{
				lookingAtGuy = false;
				if (!guyLookingAtPlayer)
				{
					guyLookingAtPlayer = true;
					boxFoldingGuy.LookAtPlayerSitting();
				}
			}
		}
		else
		{
			lookingAtGuy = false;
			if (!guyLookingAtPlayer)
			{
				guyLookingAtPlayer = true;
				boxFoldingGuy.LookAtPlayerSitting();
			}
		}
	}

	public void LookHereMike()
	{
		lookHere = lookHereMike;
		mike.LookAtPlayer();
	}

	public void LookHereCashier()
	{
		lookHere = lookHereCashier;
		mike.LookReset();
	}

	public void LookAtMike_MikeAtCashier()
	{
		lookHere = lookHereMike;
		mike.LookReset();
	}

	public void LookHereHatGuy()
	{
		lookHere = lookHereHatGuy;
	}

	public void LookHereGreyShirt()
	{
		lookHere = lookHereGreyShirt;
	}

	public void LookHereYoungMan()
	{
		lookHere = lookHereYoungMan;
	}

	public void LookHereBlackWoman()
	{
		lookHere = lookHereBlackWoman;
	}

	public void LookHereHobo()
	{
		lookHere = lookHereHobo;
	}

	public void LookHereHiker()
	{
		lookHere = lookHereHiker;
	}

	public void SetCameraForPlayer(LookAtFromCar.CameraLookAtItems cameraLookAtItem, Action cameraSetupCompleteCallBack = null, bool stopCameraLookAt = false, float? time = 2f, bool? dialogue = true)
	{
		perlinCameraShake.StopTrauma();
		switch (cameraLookAtItem)
		{
		case LookAtFromCar.CameraLookAtItems.Mike:
			lookAtObject = mikeTransform;
			break;
		case LookAtFromCar.CameraLookAtItems.Pizzeria:
			lookAtObject = pizzeriaTransform;
			break;
		}
		returnToDefaultZoom = false;
		FreezeCameraScripts(value: true);
		dialogueCamera.gameObject.SetActive(dialogue.Value);
		perlinCameraShake.RestartTrauma();
		cameraSetupCompleteCallBack?.Invoke();
		zoomIntoTransform = true;
		if (cameraLookAtItem == LookAtFromCar.CameraLookAtItems.Pizzeria)
		{
			phone.ClosePhone();
			Debug.Log("Close it!!");
		}
		if (stopCameraLookAt)
		{
			StartCoroutine(StopLookAtForObjectIn(cameraLookAtItem, time.Value));
		}
	}

	private IEnumerator StopLookAtForObjectIn(LookAtFromCar.CameraLookAtItems cameraLookAt, float seconds)
	{
		yield return new WaitForSeconds(seconds);
		ResumeCameraControlFrom(cameraLookAt);
	}

	public void ResumeCameraControlFrom(LookAtFromCar.CameraLookAtItems lookAtItem)
	{
		ResumeCameraInput();
		switch (lookAtItem)
		{
		case LookAtFromCar.CameraLookAtItems.Mike:
			drivingCam.ResumeRotationFromMike();
			break;
		case LookAtFromCar.CameraLookAtItems.Pizzeria:
			drivingCam.ResumeRotationFromPizzeria();
			break;
		}
		FreezeCameraScripts(value: false);
	}

	public void ResumeCameraInput()
	{
		camera.transform.localRotation = Quaternion.identity;
		dialogueCamera.gameObject.SetActive(value: false);
		returnToDefaultZoom = true;
		zoomIntoTransform = false;
	}

	public void ToggleCameraInput(bool turnOff)
	{
		drivingCam.FreezeCam = turnOff;
		fovZoom.disableFov = turnOff;
		sittingCamPizzeria.enabled = !turnOff;
		fovZoomPizzeria.disableFov = turnOff;
		firstPersonController.enabled = !turnOff;
	}

	private void FreezeCameraScripts(bool value)
	{
		drivingCam.FreezeCam = value;
		fovZoom.disableFov = value;
		fovZoomPizzeria.disableFov = value;
	}

	public void LookAtPizzeria()
	{
		DialogueManager.StopConversation();
		LookAtFromCar.CameraLookAtItems cameraLookAtItem = LookAtFromCar.CameraLookAtItems.Pizzeria;
		SetCameraForPlayer(cameraLookAtItem, null, stopCameraLookAt: true, 4f, false);
	}

	public void GetOutOfTruck()
	{
		handBreakAS.Play();
		StartCoroutine(pizzeriaGameManager.RequestFadeInAndFadeOut(0.5f, 0.5f, 1f, delegate
		{
			AudioMixerManager.PlayerOutside();
			mike.GetOut();
			firstPersonController.gameObject.SetActive(value: true);
			playerDrivingParent.SetActive(value: false);
			playerDrivingCam.SetActive(value: false);
			StartCoroutine(SetParticleFollow());
			phone.allowPhone = true;
			Debug.Log("Phone allowed");
		}));
	}

	public IEnumerator SetParticleFollow()
	{
		yield return new WaitForSeconds(1f);
		Debug.Log("SetParticleFollow");
		pizzeriaGameManager.sRS_ParticleSystem.followTarget = firstPersonCam;
	}

	public void StartTalking()
	{
		if (pizzeriaGameManager.uiManager.phoneUI.isPaused)
		{
			pizzeriaGameManager.uiManager.phoneUI.ClosePhoneFromConversation();
		}
		pizzeriaGameManager.ChangePlayerState(PizzeriaGameManager.PlayerState.Talking);
	}

	public void EndTalking()
	{
		pizzeriaGameManager.ChangePlayerState(PizzeriaGameManager.PlayerState.Normal);
		StartCoroutine(_EndTalking());
		static IEnumerator _EndTalking()
		{
			yield return new WaitForEndOfFrame();
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
	}

	public void TriggerTalkingSitting()
	{
		if (pizzeriaGameManager.uiManager.phoneUI.isPaused)
		{
			pizzeriaGameManager.uiManager.phoneUI.ClosePhoneFromConversation();
		}
		pizzeriaGameManager.uiManager.ClearControlsText();
		sittingCamPizzeria.enabled = false;
		fovZoomPizzeria.enabled = false;
		pizzeriaGameManager.ChangePlayerState(PizzeriaGameManager.PlayerState.TalkingSitting);
	}

	public void TriggerTalkingSittingCashier()
	{
		if (pizzeriaGameManager.uiManager.phoneUI.isPaused)
		{
			pizzeriaGameManager.uiManager.phoneUI.ClosePhoneFromConversation();
		}
		pizzeriaGameManager.uiManager.ClearControlsText();
		sittingCamPizzeria.enabled = false;
		fovZoomPizzeria.enabled = false;
		pizzeriaGameManager.ChangePlayerState(PizzeriaGameManager.PlayerState.TalkingSitting);
		lookHere = lookHereCashier;
	}

	public void EndTalkingSitting()
	{
		pizzeriaGameManager.ChangePlayerState(PizzeriaGameManager.PlayerState.Sitting);
		sittingCamPizzeria.enabled = true;
		fovZoomPizzeria.enabled = true;
		fovZoomPizzeria.disableFov = false;
		fovZoomPizzeria.dontZoom = false;
		FreezeCameraScripts(value: false);
		StartCoroutine(_EndTalking());
		static IEnumerator _EndTalking()
		{
			yield return new WaitForEndOfFrame();
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
	}

	public void TriggerEating(Transform lookat)
	{
		pizzeriaGameManager.uiManager.ClearControlsText();
		sittingCamPizzeria.enabled = false;
		fovZoomPizzeria.enabled = false;
		pizzeriaGameManager.ChangePlayerState(PizzeriaGameManager.PlayerState.Consuming);
		lookHere = lookat;
		phone.ClosePhone();
		phone.allowPhone = false;
	}

	public void ExitEating()
	{
		pizzeriaGameManager.ChangePlayerState(PizzeriaGameManager.PlayerState.Sitting);
		sittingCamPizzeria.enabled = true;
		fovZoomPizzeria.enabled = true;
		fovZoomPizzeria.disableFov = false;
		fovZoomPizzeria.dontZoom = false;
		FreezeCameraScripts(value: false);
		if (!pizzerriaUIManager.inCoversation)
		{
			phone.allowPhone = true;
		}
	}

	public void TestingModeStartOutsideAwake()
	{
		firstPersonController.gameObject.SetActive(value: true);
		playerDrivingParent.SetActive(value: false);
		playerDrivingCam.SetActive(value: false);
		AudioMixerManager.PlayerOutside();
		mike.TestingModeStartOutsideAwake();
	}

	public void TestingModeStartInsideWithoutMike()
	{
		firstPersonController.transform.SetPositionAndRotation(testingStartInsidePosition.position, testingStartInsidePosition.rotation);
		firstPersonController.gameObject.SetActive(value: true);
		playerDrivingParent.SetActive(value: false);
		playerDrivingCam.SetActive(value: false);
		AudioMixerManager.PlayerInside();
		mike.TestingModeStartOutsideAwake();
	}

	public void ThrowPizzaBox()
	{
		if (canThrowItem && currentHoldingObject != null)
		{
			currentHoldingObject.gameObject.SetActive(value: false);
			currentHoldingObject = null;
			trashCan.Play();
			StartCoroutine(TriggerMikeKeyConversation());
		}
	}

	internal override void Throw()
	{
		if (canThrowItem && currentHoldingObject != null && (bool)currentHoldingObject.GetComponent<SodaPickable>() && !currentHoldingObject.GetComponent<SodaPickable>().Sipping)
		{
			currentHoldingObject.Throw(mainCamera.transform);
			currentHoldingObject = null;
			pizzerriaUIManager.ClearControlsText();
			pizzerriaUIManager.ResetRememberedText();
		}
	}

	public IEnumerator TriggerMikeKeyConversation()
	{
		yield return new WaitForSeconds(2f);
		mike.TriggerKeyConversation();
	}
}
