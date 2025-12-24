using System;
using System.Collections;
using DG.Tweening;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CabinPlayerController : PlayerController
{
	public enum PlayerLocation
	{
		PlayerOutside,
		PlayerInside,
		PlayerDeepInside
	}

	[Header("Transform Points")]
	[SerializeField]
	private Transform kitchenStartPoint;

	[SerializeField]
	private Transform fishingStartPoint;

	[SerializeField]
	private Transform boardGameStartPoint;

	[SerializeField]
	private Transform basementStartPoint;

	[SerializeField]
	private Transform bedroomStandPoint;

	[SerializeField]
	private Transform bedroomGetUpAfterSittingPoint;

	[SerializeField]
	private Transform playerTransform;

	[SerializeField]
	private Transform ouijaLookPoint;

	[SerializeField]
	private Transform mikeHugPointCabinDarkScene;

	[SerializeField]
	private Transform signBoardLookAtPoint;

	[Header("Eating Related")]
	[SerializeField]
	private GameObject sittingPlayerEating;

	[SerializeField]
	private BoxCollider couchCollider;

	[SerializeField]
	private CabinSittablePlace cabinCouch;

	[SerializeField]
	public Camera couchSittingCamera;

	[SerializeField]
	public SittingCam sittingCam;

	[SerializeField]
	public FovZoom sittingCamFov;

	[SerializeField]
	private GameObject foodParentInSittingPlayer;

	[SerializeField]
	private GameObject[] platePresets;

	[Header("Jenga Board Game Related")]
	[SerializeField]
	private GameObject sittingPlayerBoardGame;

	[SerializeField]
	private SittingCam sittingCamJengaBoardGame;

	[SerializeField]
	private FovZoom fovZoomJengaBoardGame;

	[SerializeField]
	private BoxCollider diningTableMikeChairCollider;

	[SerializeField]
	private GameObject diningTableLight;

	[SerializeField]
	private BoxCollider[] diningTableColliders;

	[SerializeField]
	private CabinSittablePlace diningTable;

	[SerializeField]
	private GameObject jengaPieces;

	[SerializeField]
	private TriggerActionOnInteract triggerSubGetJenga;

	[Header("Ouija Board Game Related")]
	[SerializeField]
	private GameObject sittingPlayerOuijaBoardGame;

	[SerializeField]
	private SittingCam sittingCamOuijaBoardGame;

	[SerializeField]
	private FovZoom fovZoomOuijaBoardGame;

	private Camera ouijaCam;

	[SerializeField]
	private Collider basementTableCollider;

	[SerializeField]
	private Transform ouijaPlanchetteTransform;

	[SerializeField]
	private GameObject basementTableLight;

	[SerializeField]
	private OuijaController ouijaBoardGame;

	[SerializeField]
	private CabinSittablePlace basementTable;

	[SerializeField]
	private TriggerActionOnInteract triggerSubGetOuija;

	[SerializeField]
	private TriggerActionOnInteract triggerSubTurnOffLights;

	[Header("Rizz Sequence Related")]
	[SerializeField]
	private CabinSittablePlace sittableBed;

	[SerializeField]
	private GameObject sleepingBedPlayer;

	[SerializeField]
	private SittingCam sittingCamSleepingOnBed;

	[SerializeField]
	private FovZoom fovZoomBedSleepingCam;

	[SerializeField]
	private GameObject sittingBedPlayer;

	[SerializeField]
	private SittingCam sittingCamSittingOnBed;

	[SerializeField]
	private FovZoom fovZoomBedSittingCam;

	[Header("Host End Related")]
	[SerializeField]
	private Transform entranceStandPoint;

	[SerializeField]
	private Transform basementHiddenPoint;

	[SerializeField]
	private Transform bedroomPoint;

	private Camera sittingOnBedCam;

	[Header("Cabin Specific")]
	[SerializeField]
	private bool isInKitchen;

	[SerializeField]
	public Camera carCamera;

	[SerializeField]
	private CabinGameManager cabinGameManager;

	[SerializeField]
	private CabinUIManager cabinUIManager;

	[SerializeField]
	private DrivingCam drivingCam;

	[SerializeField]
	private FovZoom fovZoom;

	[SerializeField]
	public CameraShake cameraShake;

	[SerializeField]
	public Camera dialogueCamera;

	[SerializeField]
	private Camera mainCamera;

	[SerializeField]
	private float mikeZoom = 40f;

	[SerializeField]
	private float turnToMikeSpeed;

	[SerializeField]
	private float zoomToMikeDuration = 2f;

	[SerializeField]
	private Transform mikeTransform;

	[SerializeField]
	private PerlinCameraShake perlinCameraShake;

	[SerializeField]
	public Camera truckCamera;

	[SerializeField]
	private DrivingCam drivingCamTruck;

	[SerializeField]
	public FovZoom fovZoomTruck;

	public LockBox lockBox;

	public Transform truck;

	public GameObject truckPlayer;

	public FishingRod fishingRod;

	public GameObject[] fishingItems;

	public Transform lookAt;

	private float defaultZoom = 60f;

	private float lerpSpeed = 3f;

	[HideInInspector]
	public float lerpSpeedLookAtObject = 3f;

	private Vector3 preLockBoxCameraRotation;

	private Vector3 preCouchSitCameraRotation;

	private bool zoomIntoTransform;

	private bool returnToDefaultZoom;

	private bool lookAtMikeInCar;

	[SerializeField]
	private Camera currentCamera;

	public Transform lookAtMike;

	public Transform lookAtHost;

	public Transform lookAtHostFixingToilet;

	public Transform lookAtMikeAfterHiding;

	public Transform lookAtMikeEnd;

	public Transform lookAtNora;

	public Transform fishingSitting;

	private Vector3 preFishingSittingCameraRotation;

	public LockCameraMovement lockCameraMovement;

	public static PlayerLocation playerLocation;

	private bool playerHasFullFoodPlate;

	private bool playerHasEmptyFoodPlate;

	private int platesIndex;

	[SerializeField]
	private SittingCam sittingCamToilet;

	[SerializeField]
	private FovZoom fovZoomToilet;

	private bool playerIsSeatedWithJenga;

	public GameObject ropePrefab;

	public Transform itemHere;

	public Transform tip;

	private RopePrefab ropePrefabScript;

	[HideInInspector]
	public Vector3 fishingHitPoint;

	private bool hasWonJenga;

	private BoardGameType currentHoldingBoardGameType;

	private bool holdingStackedPlates;

	[SerializeField]
	internal Holdable currentSecondHoldingObject;

	public Sink sink;

	private Vector3 presinkCameraRotation;

	private bool playerCanSleepOnBed;

	private bool playerCanSitOnBed;

	private bool hasPlayerOpenedBedroomDoor;

	private bool knockerIsMikeRevealed;

	private bool mainDoorIsLocked;

	private bool bedroomDoorClosed;

	private bool mikeInBedroom;

	private bool hasShownHikerRealizationSub;

	private bool hasTalkedWithHiker;

	private bool wokeUpToScream;

	public bool inTransition;

	public bool inFishCaught;

	public bool CanPlaceJenga { get; set; }

	public event Action OnTakeCasseroleFishInPlate;

	public event Action<BoardGameType> OnTakeBoardGame;

	public event Action<PlayerPositionInHouse> OnPlayerPositionInHouseChanged;

	public event Action OnEnableJengaOnDiningTable;

	public event Action OnEnableOuijaOnBasementTable;

	public event Action OnSit;

	public event Action OnSitOnCouch;

	public event Action OnSitOnBasementTable;

	public event Action OnSitOnBed;

	public event Action OnSleepOnBed;

	public event Action OnGetUp;

	public event Action OnGetUpFromCouch;

	public event Action OnGetUpAfterOuijaGameEnded;

	public event Action OnGetUpAfterJengaGameEnded;

	public event Action OnGetUpFromBedAfterSitting;

	public event Action OnStartPlayingOuija;

	public event Action<float> OnHugStart;

	internal override void Awake()
	{
		base.Awake();
	}

	internal override void Start()
	{
		base.Start();
		if (SceneManager.GetActiveScene().name == "CabinScene")
		{
			CabinSceneSpecificSetup();
		}
		if (SceneManager.GetActiveScene().name == "CabinSceneDark")
		{
			CabinSceneDarkSpecificSetup();
		}
	}

	private void CabinSceneDarkSpecificSetup()
	{
		sittingOnBedCam = sittingCamSittingOnBed.GetComponent<Camera>();
		if (firstPersonController == null)
		{
			firstPersonController = UnityEngine.Object.FindObjectOfType<FirstPersonController>();
		}
	}

	private void CabinSceneSpecificSetup()
	{
		triggerSubGetJenga?.SetActionToTrigger(ShowSubGetJenga);
		triggerSubGetOuija?.SetActionToTrigger(ShowSubGetOuija);
		if (sittingCamOuijaBoardGame != null)
		{
			ouijaCam = sittingCamOuijaBoardGame.GetComponent<Camera>();
		}
	}

	internal override void Update()
	{
		base.Update();
		HandlePlayerStates();
	}

	private void HandlePlayerStates()
	{
		if (cabinGameManager.currentPlayerState == CabinGameManager.PlayerState.Driving)
		{
			HandleTruckMikeLookBehaviour();
		}
		if (cabinGameManager.currentPlayerState == CabinGameManager.PlayerState.TalkingBed && lookAt != null)
		{
			currentCamera.fieldOfView = Mathf.Lerp(currentCamera.fieldOfView, 40f, Time.deltaTime * lerpSpeed);
			Vector3 forward = lookAt.position - currentCamera.transform.position;
			currentCamera.transform.rotation = Quaternion.Lerp(currentCamera.transform.rotation, Quaternion.LookRotation(forward), Time.deltaTime * lerpSpeed);
		}
		if (cabinGameManager.currentPlayerState == CabinGameManager.PlayerState.PlayingOuija)
		{
			SetSittingCameraFOV(CameraFOVPreset.PlayingBoardGame);
			Vector3 forward2 = lookAt.position - currentCamera.transform.position;
			currentCamera.transform.rotation = Quaternion.Lerp(currentCamera.transform.rotation, Quaternion.LookRotation(forward2), Time.deltaTime * lerpSpeed);
		}
		if (cabinGameManager.currentPlayerState == CabinGameManager.PlayerState.TalkingBasementTable)
		{
			SetSittingCameraFOV(CameraFOVPreset.InConversation);
			Vector3 forward3 = lookAt.position - currentCamera.transform.position;
			currentCamera.transform.rotation = Quaternion.Lerp(currentCamera.transform.rotation, Quaternion.LookRotation(forward3), Time.deltaTime * lerpSpeed);
		}
		if (cabinGameManager.currentPlayerState == CabinGameManager.PlayerState.Talking && lookAt != null)
		{
			mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, 40f, Time.deltaTime * lerpSpeed);
			Vector3 forward4 = lookAt.position - mainCamera.transform.position;
			mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, Quaternion.LookRotation(forward4), Time.deltaTime * lerpSpeed);
		}
		else if (cabinGameManager.currentPlayerState == CabinGameManager.PlayerState.TalkingDriving)
		{
			truckCamera.fieldOfView = Mathf.Lerp(truckCamera.fieldOfView, 40f, Time.deltaTime * lerpSpeed);
			Vector3 forward5 = lookAt.transform.position - truckCamera.transform.parent.position;
			truckCamera.transform.rotation = Quaternion.Lerp(truckCamera.transform.rotation, Quaternion.LookRotation(forward5), Time.deltaTime * lerpSpeed);
		}
		else if (cabinGameManager.currentPlayerState == CabinGameManager.PlayerState.FishingMinigame)
		{
			mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, 40f, Time.deltaTime * 4f);
			Vector3 forward6 = fishingHitPoint - mainCamera.transform.position;
			mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, Quaternion.LookRotation(forward6), Time.deltaTime * 4f);
		}
		else if (cabinGameManager.currentPlayerState == CabinGameManager.PlayerState.TalkingCouch)
		{
			couchSittingCamera.fieldOfView = Mathf.Lerp(couchSittingCamera.fieldOfView, 40f, Time.deltaTime * lerpSpeed);
			Vector3 forward7 = lookAt.transform.position - couchSittingCamera.transform.parent.position;
			couchSittingCamera.transform.rotation = Quaternion.Lerp(couchSittingCamera.transform.rotation, Quaternion.LookRotation(forward7), Time.deltaTime * lerpSpeed);
		}
		else if (cabinGameManager.currentPlayerState == CabinGameManager.PlayerState.LookAtObject && lookAt != null)
		{
			mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, 40f, Time.deltaTime * lerpSpeedLookAtObject);
			Vector3 forward8 = lookAt.position - mainCamera.transform.position;
			mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, Quaternion.LookRotation(forward8), Time.deltaTime * lerpSpeedLookAtObject);
		}
		else if (cabinGameManager.currentPlayerState == CabinGameManager.PlayerState.LookAtSignBoard && lookAt != null)
		{
			carCamera.fieldOfView = Mathf.Lerp(carCamera.fieldOfView, 40f, Time.deltaTime * lerpSpeedLookAtObject);
			Vector3 forward9 = lookAt.position - carCamera.transform.position;
			carCamera.transform.rotation = Quaternion.Lerp(carCamera.transform.rotation, Quaternion.LookRotation(forward9), Time.deltaTime * lerpSpeedLookAtObject);
		}
	}

	public void StartConvoWithMike(Transform mikeHead)
	{
		lookAt = mikeHead;
		firstPersonController.enabled = false;
	}

	public void EndConvoWithMike()
	{
		lookAt = null;
		firstPersonController.enabled = true;
	}

	public void SetDiningTableInteractable(bool value = true)
	{
		diningTable.SetInteractable(value);
	}

	public void StartPlayingOuija(bool isContinuing = false)
	{
		this.OnStartPlayingOuija?.Invoke();
		basementTableCollider.enabled = false;
		SetSittingCameraFOV(CameraFOVPreset.Normal);
		sittingCamOuijaBoardGame.enabled = false;
		fovZoomOuijaBoardGame.enabled = false;
		lookAt = ouijaPlanchetteTransform;
		if (!isContinuing)
		{
			ouijaBoardGame.StartPlaying();
		}
		else
		{
			ouijaBoardGame.ContinuePlaying();
		}
	}

	public void EnableBasementSittingCam()
	{
		sittingCamOuijaBoardGame.enabled = true;
		SetSittingCameraFOV(CameraFOVPreset.Normal);
	}

	public void SetBedLayingCam(bool value = true)
	{
		sleepingBedPlayer.SetActive(value);
		sittingCamSleepingOnBed.enabled = value;
		sittingCamSleepingOnBed.gameObject.SetActive(value);
	}

	public void EnableBedSittingCam()
	{
		sittingCamSittingOnBed.enabled = true;
	}

	private void HandleTruckMikeLookBehaviour()
	{
		if (zoomIntoTransform)
		{
			currentCamera.fieldOfView = Mathf.Lerp(currentCamera.fieldOfView, mikeZoom, Time.deltaTime * turnToMikeSpeed);
			if (lookAtMikeInCar)
			{
				Vector3 forward = lookAt.position - currentCamera.transform.position;
				currentCamera.transform.rotation = Quaternion.Lerp(currentCamera.transform.rotation, Quaternion.LookRotation(forward), Time.deltaTime * turnToMikeSpeed);
			}
		}
		if (returnToDefaultZoom)
		{
			carCamera.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, defaultZoom, Time.deltaTime * turnToMikeSpeed);
		}
	}

	public void ToggleCameraInput(bool turnOff)
	{
		if (cabinGameManager.currentPlayerState == CabinGameManager.PlayerState.Driving)
		{
			drivingCam.FreezeCam = turnOff;
			fovZoom.disableFov = turnOff;
		}
		else if (cabinGameManager.currentPlayerState != CabinGameManager.PlayerState.LockBox && firstPersonController != null)
		{
			firstPersonController.enabled = !turnOff;
		}
		if (sittingCam != null)
		{
			sittingCam.enabled = !turnOff;
		}
		if (sittingCamFov != null)
		{
			sittingCamFov.enabled = !turnOff;
		}
		if (sittingCamJengaBoardGame != null)
		{
			sittingCamJengaBoardGame.enabled = !turnOff;
		}
		if (sittingCamOuijaBoardGame != null)
		{
			sittingCamOuijaBoardGame.enabled = !turnOff;
		}
		if (fovZoomOuijaBoardGame != null)
		{
			fovZoomOuijaBoardGame.enabled = !turnOff;
		}
		if (fovZoomJengaBoardGame != null)
		{
			fovZoomJengaBoardGame.enabled = !turnOff;
		}
		if (sittingCamSittingOnBed != null)
		{
			sittingCamSittingOnBed.enabled = !turnOff;
		}
		if (fovZoomBedSittingCam != null)
		{
			fovZoomBedSittingCam.enabled = !turnOff;
		}
		if (sittingCamSleepingOnBed != null)
		{
			sittingCamSleepingOnBed.enabled = !turnOff;
		}
		if (fovZoomBedSleepingCam != null)
		{
			fovZoomBedSleepingCam.enabled = !turnOff;
		}
		if (sittingCamToilet != null)
		{
			sittingCamToilet.enabled = !turnOff;
		}
		if (fovZoomToilet != null)
		{
			fovZoomToilet.disableFov = turnOff;
		}
	}

	public void FreezeCameraScripts(bool value)
	{
		drivingCam.FreezeCam = value;
		fovZoom.disableFov = value;
	}

	public void SetSittingCameraFOV(CameraFOVPreset cameraFOVPreset)
	{
		SittingCam sittingCam = null;
		if (currentHoldingBoardGameType == BoardGameType.Ouija)
		{
			sittingCam = sittingCamOuijaBoardGame;
		}
		if (currentHoldingBoardGameType == BoardGameType.Jenga)
		{
			sittingCam = sittingCamJengaBoardGame;
		}
		if (playerCanSitOnBed)
		{
			sittingCam = sittingCamSittingOnBed;
		}
		if (sittingCam != null)
		{
			switch (cameraFOVPreset)
			{
			case CameraFOVPreset.Normal:
				sittingCam.GetComponent<Camera>().DOFieldOfView(60f, 1f);
				break;
			case CameraFOVPreset.NormalInstant:
				sittingCam.GetComponent<Camera>().fieldOfView = 60f;
				break;
			case CameraFOVPreset.PlayingBoardGame:
				sittingCam.GetComponent<Camera>().DOFieldOfView(46f, 1f);
				break;
			case CameraFOVPreset.DraggingOutJenga:
				sittingCam.GetComponent<Camera>().DOFieldOfView(40f, 0.5f);
				break;
			case CameraFOVPreset.InConversation:
				sittingCam.GetComponent<Camera>().DOFieldOfView(40f, 1f);
				break;
			}
		}
	}

	public void FreezeSittingCamScriptsAndResetRotation(bool freezeCams, bool resetRotation = true)
	{
		if (freezeCams && resetRotation)
		{
			ResetSittingCamRotation();
		}
		else
		{
			CursorModeUtility.HideCursor();
		}
		FreezeSittingCameraScripts(freezeCams);
	}

	public void FreezeSittingCameraScripts(bool freezeCams)
	{
		sittingCam.enabled = !freezeCams;
		fovZoom.disableFov = freezeCams;
		if (currentHoldingBoardGameType == BoardGameType.Ouija)
		{
			sittingCamOuijaBoardGame.enabled = !freezeCams;
			fovZoomOuijaBoardGame.disableFov = freezeCams;
		}
		if (currentHoldingBoardGameType == BoardGameType.Jenga)
		{
			sittingCamJengaBoardGame.enabled = !freezeCams;
			fovZoomJengaBoardGame.disableFov = freezeCams;
			fovZoomJengaBoardGame.enabled = !freezeCams;
			Debug.Log("Set FOV zooom to " + freezeCams);
		}
		if (!freezeCams)
		{
			fovZoomJengaBoardGame.disableFov = freezeCams;
			CursorModeUtility.HideCursor();
		}
	}

	public void ResetSittingCamRotation()
	{
		sittingCam.ResetRotation();
		sittingCamJengaBoardGame.ResetRotation();
		if (sittingCamOuijaBoardGame != null)
		{
			sittingCamOuijaBoardGame.ResetRotation();
		}
	}

	public void StartTalking()
	{
		cabinGameManager.ChangePlayerState(CabinGameManager.PlayerState.Talking);
	}

	public void EndTalking(CabinGameManager.PlayerState state)
	{
		cabinGameManager.ChangePlayerState(state);
		StartCoroutine(_EndTalking());
		static IEnumerator _EndTalking()
		{
			yield return new WaitForEndOfFrame();
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
	}

	public void SetFoodInSittingPlayerActive(bool value)
	{
		foodParentInSittingPlayer.SetActive(value);
	}

	public IEnumerator SetFishingRodItems()
	{
		yield return new WaitForEndOfFrame();
		fishingRod?.rope.ResetParticles();
		GameObject[] array = fishingItems;
		foreach (GameObject obj in array)
		{
			obj.transform.parent = handPosition;
			obj.SetActive(value: false);
		}
	}

	public void CreateRopeAndItems()
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(ropePrefab, itemHere);
		ropePrefabScript = gameObject.GetComponent<RopePrefab>();
		ropePrefabScript.particleAttachment.target = tip;
		gameObject.transform.parent = null;
		Transform[] children = ropePrefabScript.children;
		for (int i = 0; i < children.Length; i++)
		{
			children[i].parent = null;
		}
		fishingRod.lureTransform = ropePrefabScript.lureTransform;
		fishingRod.lureRb = ropePrefabScript.lureRb;
		fishingRod.floater = ropePrefabScript.floater;
		fishingRod.floaterTransform = ropePrefabScript.floaterTransform;
		fishingRod.floaterRb = ropePrefabScript.floaterRb;
		fishingRod.rope = ropePrefabScript.rope;
		fishingRod.solver = ropePrefabScript.solver;
		fishingRod.hook = ropePrefabScript.hook;
		fishingRod.lure = ropePrefabScript.lure;
		fishingRod.maggots = ropePrefabScript.maggots;
		fishingRod.corn = ropePrefabScript.corn;
		fishingRod.cheese = ropePrefabScript.cheese;
		fishingRod.prawn = ropePrefabScript.prawn;
		fishingRod.fishLureNorthernPike = ropePrefabScript.fishLureNorthernPike;
		fishingRod.fishLureLargeMouthBass = ropePrefabScript.fishLureLargeMouthBass;
		fishingRod.fishLureSmallMouthBass = ropePrefabScript.fishLureSmallMouthBass;
		fishingRod.fishLureYellowPerch = ropePrefabScript.fishLureYellowPerch;
		fishingRod.fishLureWalleye = ropePrefabScript.fishLureWalleye;
		fishingRod.fishHookGoldfish = ropePrefabScript.fishHookGoldfish;
		fishingRod.fishHookCommonDace = ropePrefabScript.fishHookCommonDace;
		fishingRod.fishHookCommonCarp = ropePrefabScript.fishHookCommonCarp;
		fishingRod.fishHookChub = ropePrefabScript.fishHookChub;
		fishingRod.fishHookBrookTrout = ropePrefabScript.fishHookBrookTrout;
		fishingRod.fishHookBluegill = ropePrefabScript.fishHookBluegill;
		fishingRod.fishHookNorthernPike = ropePrefabScript.fishHookNorthernPike;
		fishingRod.fishHookLargemouthBass = ropePrefabScript.fishHookLargemouthBass;
		fishingRod.fishHookSmallmouthBass = ropePrefabScript.fishHookSmallmouthBass;
		fishingRod.fishHookYellowPerch = ropePrefabScript.fishHookYellowPerch;
		fishingRod.fishHookWalleye = ropePrefabScript.fishHookWalleye;
		fishingRod.floater.stream = fishingRod.stream;
	}

	public void HideRope()
	{
		Transform[] children = ropePrefabScript.children;
		foreach (Transform obj in children)
		{
			obj.transform.parent = base.transform;
			obj.gameObject.SetActive(value: false);
		}
		ropePrefabScript.transform.parent = base.transform;
		ropePrefabScript.gameObject.SetActive(value: false);
		ropePrefabScript = null;
	}

	public void SetCanSitOnCouch(bool value)
	{
		couchCollider.enabled = value;
		couchCollider.gameObject.layer = (value ? LayerMask.NameToLayer("Default") : LayerMask.NameToLayer("Ignore Raycast"));
	}

	public void MoveToCouch(Transform sitPoint)
	{
		cabinGameManager.inputManager.OnThrow -= Throw;
		StartCoroutine(cabinGameManager.RequestFadeInAndFadeOut(0.5f, 0.5f, 1.5f, delegate
		{
			firstPersonController.gameObject.SetActive(value: false);
			sittingPlayerEating.transform.SetPositionAndRotation(sitPoint.position, sitPoint.rotation);
			sittingPlayerEating.SetActive(value: true);
			cabinGameManager.ChangePlayerState(CabinGameManager.PlayerState.SittingAtSittablePlace);
			AssignGetUpCallbackToInputManager();
			sittingCam.enabled = true;
			fovZoom.enabled = true;
			couchCollider.enabled = false;
			this.OnSit?.Invoke();
			this.OnSitOnCouch?.Invoke();
			if (currentHoldingObject != null && currentHoldingObject.transform.childCount >= 1 && currentHoldingObject.transform.GetChild(0).CompareTag("FishPlatePreset"))
			{
				playerHasFullFoodPlate = currentHoldingObject.transform.GetChild(0).CompareTag("FishPlatePreset");
				if (playerHasFullFoodPlate || playerHasEmptyFoodPlate)
				{
					SetFoodInSittingPlayerActive(value: true);
				}
			}
			else
			{
				SetFoodInSittingPlayerActive(value: false);
			}
		}));
	}

	public void MoveBackFromCouch()
	{
		StartCoroutine(cabinGameManager.RequestFadeInAndFadeOut(0.5f, 0.5f, 1f, delegate
		{
			cabinGameManager.inputManager.OnThrow += Throw;
			firstPersonController.gameObject.SetActive(value: true);
			UnassignCouchGetUpCallbackFromInputManager();
			sittingPlayerEating.SetActive(value: false);
			DisableFoodInSittingPlayer();
			cabinGameManager.ChangePlayerState(CabinGameManager.PlayerState.Normal);
			firstPersonController.enabled = true;
			couchCollider.enabled = true;
			this.OnGetUp?.Invoke();
			this.OnGetUpFromCouch?.Invoke();
		}));
	}

	public void MoveToDiningTable(Transform sitPoint)
	{
		StartCoroutine(cabinGameManager.RequestFadeInAndFadeOut(0.5f, 0.5f, 1.5f, delegate
		{
			cabinGameManager.inputManager.OnThrow -= Throw;
			firstPersonController.gameObject.SetActive(value: false);
			sittingPlayerBoardGame.transform.SetPositionAndRotation(sitPoint.position, sitPoint.rotation);
			sittingPlayerBoardGame.SetActive(value: true);
			if (!playerIsSeatedWithJenga && cabinGameManager.CurrentSequence == SequenceType.PlayingJenga)
			{
				triggerSubGetJenga.gameObject.SetActive(value: true);
			}
			cabinGameManager.ChangePlayerState(CabinGameManager.PlayerState.SittingAtSittablePlace);
			cabinGameManager.inputManager.OnGetUp += MoveBackFromDiningTable;
			cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "GetUp"));
			if (GetCurrentHoldingBoardGame() == BoardGameType.Jenga && cabinGameManager.CurrentSequence == SequenceType.PlayingJenga)
			{
				playerIsSeatedWithJenga = true;
				TryEnableJengaPieces();
				triggerSubGetJenga.gameObject.SetActive(value: false);
			}
			diningTableMikeChairCollider.enabled = false;
			diningTable.SetCollidersActive(value: false);
			sittingCamJengaBoardGame.enabled = true;
			fovZoomJengaBoardGame.enabled = true;
			BoxCollider[] array = diningTableColliders;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = false;
			}
			diningTableLight.SetActive(value: true);
			this.OnSit?.Invoke();
		}));
	}

	public void SetCurrentCamToOuijaCam()
	{
		currentCamera = ouijaCam;
	}

	public void MoveToBasementTable(Transform sitPoint = null)
	{
		StartCoroutine(cabinGameManager.RequestFadeInAndFadeOut(0.5f, 0.5f, 1.5f, delegate
		{
			cabinGameManager.inputManager.OnThrow -= Throw;
			firstPersonController.gameObject.SetActive(value: false);
			sittingPlayerOuijaBoardGame.SetActive(value: true);
			sittingPlayerOuijaBoardGame.transform.SetPositionAndRotation(sitPoint.position, sitPoint.rotation);
			currentCamera = ouijaCam;
			cabinGameManager.ChangePlayerState(CabinGameManager.PlayerState.SittingAtSittablePlace);
			if (GetCurrentHoldingBoardGame() == BoardGameType.Ouija)
			{
				triggerSubGetJenga.gameObject.SetActive(value: false);
				ouijaBoardGame.gameObject.SetActive(value: true);
				currentHoldingBoardGameType = BoardGameType.Ouija;
				this.OnEnableOuijaOnBasementTable?.Invoke();
				currentHoldingObject.gameObject.SetActive(value: false);
			}
			basementTableLight.SetActive(value: true);
			Debug.Log("!Setting ouija Sitting Cam to: " + true);
			sittingCamOuijaBoardGame.enabled = true;
			basementTableCollider.enabled = false;
			this.OnSit?.Invoke();
			this.OnSitOnBasementTable?.Invoke();
		}));
	}

	public void MoveBackFromBasementTable()
	{
		this.OnGetUpAfterOuijaGameEnded?.Invoke();
		StartCoroutine(cabinGameManager.RequestFadeInAndFadeOut(0.5f, 0.5f, 1f, delegate
		{
			UnityEngine.Object.Destroy(currentHoldingObject.gameObject);
			currentHoldingObject = null;
			currentHoldingBoardGameType = BoardGameType.None;
			cabinUIManager.ClearControlsText();
			cabinGameManager.inputManager.OnThrow += Throw;
			firstPersonController.gameObject.SetActive(value: true);
			cabinGameManager.inputManager.OnGetUp -= MoveBackFromBasementTable;
			sittingCamOuijaBoardGame.gameObject.SetActive(value: false);
			cabinGameManager.ChangePlayerState(CabinGameManager.PlayerState.Normal);
			firstPersonController.enabled = true;
			if (currentHoldingObject != null)
			{
				cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "Throw"));
			}
			else
			{
				cabinUIManager.ClearControlsText();
			}
			basementTableLight.SetActive(value: false);
		}));
	}

	public void SitOnBed(Transform sitPoint = null)
	{
		if (playerCanSitOnBed)
		{
			StartCoroutine(cabinGameManager.RequestFadeInAndFadeOut(0.5f, 0.5f, 1.5f, delegate
			{
				firstPersonController.gameObject.SetActive(value: false);
				sittingBedPlayer.SetActive(value: true);
				sittingOnBedCam.gameObject.SetActive(value: true);
				ClearControlsText();
				sittableBed.SetCollidersActive(value: false);
				this.OnSitOnBed?.Invoke();
			}));
		}
		else if (knockerIsMikeRevealed)
		{
			SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "MikeInRoom"));
		}
		else
		{
			SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "CouldntSleepKnocks"));
		}
	}

	public void SleepOnBed()
	{
		if (cabinGameManager.CurrentSequence == SequenceType.HikerSequence)
		{
			mainDoorIsLocked = true;
			if (playerCanSleepOnBed && bedroomDoorClosed)
			{
				hasTalkedWithHiker = true;
				StartCoroutine(cabinGameManager.RequestFadeInAndFadeOut(0.5f, 0.5f, 1.5f, delegate
				{
					firstPersonController.gameObject.SetActive(value: false);
					ClearControlsText();
					SetBedLayingCam();
					UnassignBedGetUpCallbackFromInputManager();
					this.OnSleepOnBed?.Invoke();
				}));
			}
			else
			{
				if (wokeUpToScream)
				{
					SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "CallForHelp"));
				}
				if (!playerCanSleepOnBed && !wokeUpToScream)
				{
					SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "SomeoneAtTheDoor"));
				}
				else if (!mainDoorIsLocked)
				{
					SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "BedLockDoor"));
				}
				else if (!bedroomDoorClosed)
				{
					SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "BedroomDoor"));
				}
			}
		}
		if (cabinGameManager.CurrentSequence == SequenceType.RizzSequence)
		{
			if (playerCanSleepOnBed && mainDoorIsLocked && bedroomDoorClosed)
			{
				cabinUIManager.ForceClosePhone();
				StartCoroutine(cabinGameManager.RequestFadeOut(0.5f));
				GenericAudioReferences.instance.FadeRizzAmbienceToCustomValue(0f);
				StartCoroutine(DelayedAction());
			}
			else if (knockerIsMikeRevealed && mikeInBedroom)
			{
				SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "MikeInRoom"));
			}
			else if (!knockerIsMikeRevealed)
			{
				SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "CouldntSleepKnocks"));
			}
			else if (!mainDoorIsLocked)
			{
				SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "BedLockDoor"));
			}
			else if (!bedroomDoorClosed)
			{
				SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "BedroomDoor"));
			}
		}
		IEnumerator DelayedAction()
		{
			yield return new WaitForSeconds(1f);
			firstPersonController.gameObject.SetActive(value: false);
			sittingCamSleepingOnBed.gameObject.SetActive(value: true);
			sittingCamSleepingOnBed.enabled = true;
			if (cabinGameManager.CurrentSequence == SequenceType.RizzSequence)
			{
				AudioMixerManager.FadeGameSoundToCustom(-80f, 3f, this);
				cabinGameManager.TestingModeStartWhenHikerKnocks();
				PlayerPrefs.SetInt(PlayerPrefKeys.SOMEONE_AT_DOOR, 1);
			}
			SetBedLayingCam();
			UnassignBedGetUpCallbackFromInputManager();
			this.OnSleepOnBed?.Invoke();
		}
	}

	public void GetUpFromBed()
	{
		UnassignBedGetUpCallbackFromInputManager();
		if (playerCanSitOnBed)
		{
			this.OnGetUpFromBedAfterSitting?.Invoke();
		}
		StartCoroutine(cabinGameManager.RequestFadeInAndFadeOut(0.5f, 0.5f, 1f, delegate
		{
			firstPersonController.gameObject.SetActive(value: true);
			firstPersonController.enabled = true;
			cabinGameManager.inputManager.OnGetUp -= GetUpFromBed;
			cabinGameManager.ChangePlayerState(CabinGameManager.PlayerState.Normal);
			if (cabinGameManager.CurrentSequence == SequenceType.RizzSequence)
			{
				if (playerCanSitOnBed)
				{
					playerTransform.SetPositionAndRotation(bedroomGetUpAfterSittingPoint.position, bedroomGetUpAfterSittingPoint.rotation);
				}
				else
				{
					playerTransform.SetPositionAndRotation(bedroomStandPoint.position, bedroomStandPoint.rotation);
				}
			}
			else if (cabinGameManager.CurrentSequence == SequenceType.HikerSequence)
			{
				firstPersonController.transform.SetPositionAndRotation(bedroomStandPoint.position, bedroomStandPoint.rotation);
			}
			sittingCamSleepingOnBed.gameObject.SetActive(value: false);
			sittingCamSittingOnBed.gameObject.SetActive(value: false);
			cabinUIManager.ClearControlsText();
			playerCanSleepOnBed = false;
			if (!playerCanSitOnBed && cabinGameManager.CurrentSequence == SequenceType.RizzSequence)
			{
				GenericAudioReferences.instance.riserTension.Play();
			}
			if (cabinGameManager.CurrentSequence == SequenceType.HikerSequence)
			{
				if (!hasTalkedWithHiker)
				{
					StartCoroutine(DelayedAction());
				}
				if (wokeUpToScream)
				{
					StartCoroutine(DelayedAction2());
				}
			}
			playerCanSleepOnBed = false;
		}));
		static IEnumerator DelayedAction()
		{
			yield return new WaitForSeconds(5f);
			SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "SomeoneAtTheDoor"));
		}
		IEnumerator DelayedAction2()
		{
			yield return new WaitForSeconds(5f);
			SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "CallForHelp"));
			yield return new WaitForSeconds(0.5f);
			cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "TextRick"));
			cabinUIManager.StoreControlsText();
			UIManager.OpenPhone = (Action)Delegate.Combine(UIManager.OpenPhone, new Action(TextRickForHelp));
		}
	}

	private void TextRickForHelp()
	{
		if (cabinGameManager.uiManager.dockManager != null)
		{
			cabinGameManager.uiManager.dockManager.OpenRickWindow();
		}
		cabinUIManager.ClearStoredControlsText();
		cabinUIManager.ClearControlsText();
		UIManager.OpenPhone = (Action)Delegate.Remove(UIManager.OpenPhone, new Action(TextRickForHelp));
		StartCoroutine(SendRickTextsForHelp());
		IEnumerator SendRickTextsForHelp()
		{
			yield return new WaitForSeconds(0.5f);
			if (cabinGameManager.uiManager.dockManager != null)
			{
				cabinGameManager.uiManager.dockManager.OpenRickWindow();
			}
			cabinUIManager.phoneUI.notifSystem.LoadReply(1, 0);
			yield return new WaitForSeconds(1.2f);
			if (cabinGameManager.uiManager.dockManager != null)
			{
				cabinGameManager.uiManager.dockManager.OpenRickWindow();
			}
			cabinUIManager.phoneUI.notifSystem.LoadReply(1, 1);
			yield return new WaitForSeconds(0.7f);
			if (cabinGameManager.uiManager.dockManager != null)
			{
				cabinGameManager.uiManager.dockManager.OpenRickWindow();
			}
			cabinUIManager.phoneUI.notifSystem.LoadReply(1, 2);
			yield return new WaitForSeconds(18f);
			cabinGameManager.hikerConvoTrigger.gameObject.SetActive(value: false);
			cabinGameManager.triggerDoorCrouch.SetActive(value: true);
			cabinGameManager.hostEndGame.gameObject.SetActive(value: true);
			cabinGameManager.hostEndGame.GoToMainDoor();
		}
	}

	public void ShowSitOnBedControlsText()
	{
		if (!wokeUpToScream)
		{
			cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "Sit"));
		}
	}

	public void ShowSleepOnBedControlsText()
	{
		if (!wokeUpToScream)
		{
			cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "Sleep"));
		}
	}

	public void ClearControlsText()
	{
		if (!wokeUpToScream)
		{
			cabinUIManager.ClearControlsText();
		}
	}

	private void ShowSubGetJenga()
	{
		SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "GetJenga"));
	}

	private void ShowSubGetOuija()
	{
		if (!cabinGameManager.playerTalking)
		{
			SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "GetOuija"));
		}
	}

	private void ShowSubTurnOffLight()
	{
		if (!cabinGameManager.playerTalking)
		{
			SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "TurnOffLight"));
		}
	}

	public void Hug()
	{
		StartCoroutine(_Hug());
		IEnumerator _Hug()
		{
			this.OnHugStart?.Invoke(1.5f);
			DialogueManager.StopConversation();
			cabinGameManager.ChangePlayerState(CabinGameManager.PlayerState.Hugging);
			mainCamera.transform.DOMove(mikeHugPointCabinDarkScene.position, 1f).SetEase(Ease.InOutSine);
			yield return new WaitForSeconds(1.5f);
			mainCamera.transform.DOLocalMove(new Vector3(0f, 0f, 0f), 1f).SetEase(Ease.InOutSine);
			cabinGameManager.ChangePlayerState(CabinGameManager.PlayerState.Talking);
			DialogueManager.StartConversation("Rizz Seq", base.transform, base.transform, 225);
		}
	}

	public void MoveBackFromDiningTable()
	{
		if (cabinGameManager.playerTalking)
		{
			return;
		}
		this.OnGetUp?.Invoke();
		if (hasWonJenga && cabinGameManager.CurrentSequence == SequenceType.PlayingJenga)
		{
			this.OnGetUpAfterJengaGameEnded?.Invoke();
		}
		StartCoroutine(cabinGameManager.RequestFadeInAndFadeOut(0.5f, 0.5f, 1f, delegate
		{
			cabinGameManager.inputManager.OnThrow += Throw;
			firstPersonController.gameObject.SetActive(value: true);
			cabinGameManager.inputManager.OnGetUp -= MoveBackFromDiningTable;
			sittingPlayerBoardGame.SetActive(value: false);
			cabinGameManager.ChangePlayerState(CabinGameManager.PlayerState.Normal);
			firstPersonController.enabled = true;
			playerTransform.SetPositionAndRotation(boardGameStartPoint.position, boardGameStartPoint.rotation);
			BoxCollider[] array = diningTableColliders;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = true;
			}
			diningTableMikeChairCollider.enabled = true;
			if (currentHoldingObject != null)
			{
				cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "Throw"));
			}
			else
			{
				cabinUIManager.ClearControlsText();
			}
			diningTableLight.SetActive(value: false);
		}));
	}

	public void TryEnableJengaPieces()
	{
		if (playerIsSeatedWithJenga && GetCurrentHoldingBoardGame() == BoardGameType.Jenga && CanPlaceJenga)
		{
			jengaPieces.SetActive(value: true);
			UnityEngine.Object.Destroy(currentHoldingObject.gameObject);
			currentHoldingObject = null;
			this.OnEnableJengaOnDiningTable?.Invoke();
			currentHoldingBoardGameType = BoardGameType.Jenga;
			triggerSubGetJenga.gameObject.SetActive(value: false);
		}
	}

	public void AssignBedGetUpCallbackToInputManager()
	{
		cabinGameManager.inputManager.OnGetUp += GetUpFromBed;
		cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "GetUp"));
		cabinUIManager.controlsText.gameObject.SetActive(value: true);
	}

	public void AssignBasementGetUpCallbackToInputManager()
	{
		cabinGameManager.inputManager.OnGetUp += MoveBackFromBasementTable;
		cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "GetUp"));
	}

	public void AssignGetUpCallbackToInputManager()
	{
		cabinGameManager.inputManager.OnGetUp += MoveBackFromCouch;
		cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "GetUp"));
	}

	public void AssignTruckGetUpCallBack()
	{
		cabinGameManager.inputManager.OnGetUp += cabinGameManager.MoveToFPC;
		cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "GetUp"));
	}

	private void DisableFoodInSittingPlayer()
	{
		playerHasEmptyFoodPlate = false;
		playerHasFullFoodPlate = false;
		SetFoodInSittingPlayerActive(value: false);
		cabinGameManager.inputManager.OnGetUp -= DisableFoodInSittingPlayer;
	}

	public void UnassignCouchGetUpCallbackFromInputManager()
	{
		cabinGameManager.inputManager.OnGetUp -= MoveBackFromCouch;
		cabinUIManager.ClearControlsText();
	}

	public void UnassignBedGetUpCallbackFromInputManager()
	{
		cabinGameManager.inputManager.OnGetUp -= GetUpFromBed;
		cabinUIManager.ClearControlsText();
	}

	public void UnAssignDiningTableGetUpCallbackFromInputManager()
	{
		cabinGameManager.inputManager.OnGetUp -= MoveBackFromDiningTable;
		cabinGameManager.inputManager.OnGetUp -= MoveBackFromCouch;
		cabinUIManager.ClearControlsText();
	}

	public void UnAssignGetUpCallbacksFromInputManager()
	{
		cabinGameManager.inputManager.OnGetUp -= MoveBackFromBasementTable;
		cabinGameManager.inputManager.OnGetUp -= MoveBackFromDiningTable;
		cabinGameManager.inputManager.OnGetUp -= MoveBackFromCouch;
		cabinUIManager.ClearControlsText();
	}

	public void SetHasWonJenga(bool value)
	{
		hasWonJenga = value;
	}

	public void SetHasTalkedWithHiker(bool value)
	{
		hasTalkedWithHiker = value;
	}

	public void SetHasWokeUpToScream(bool value)
	{
		wokeUpToScream = value;
	}

	public void SetCanSitOnBed(bool value)
	{
		playerCanSitOnBed = value;
	}

	public void SetCanSleepOnBed(bool value)
	{
		playerCanSleepOnBed = value;
	}

	public void SetIsKnockerRevealed(bool value)
	{
		knockerIsMikeRevealed = value;
	}

	public void SetMikeInRoom(bool value)
	{
		mikeInBedroom = value;
	}

	public void SetMainDoorLocked(bool value)
	{
		mainDoorIsLocked = value;
	}

	public void SetBedroomDoorClosed(bool value)
	{
		bedroomDoorClosed = value;
	}

	public void LookAtSignBoard()
	{
		FreezeCameraScripts(value: true);
		lookAt = signBoardLookAtPoint;
		cabinGameManager.ChangePlayerState(CabinGameManager.PlayerState.LookAtSignBoard);
		StartCoroutine(DelayedAction());
		IEnumerator DelayedAction()
		{
			yield return new WaitForSeconds(2f);
			StopLookingAtSignBoard();
		}
	}

	public void ShowSubMissedTurn()
	{
		SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "MissedTurn"));
	}

	public void StopLookingAtSignBoard()
	{
		ResumeCameraControl();
		lookAt = null;
		cabinGameManager.ChangePlayerState(CabinGameManager.PlayerState.Driving);
	}

	public void AssignDiningTableGetUpCallbackToInputManager()
	{
		cabinGameManager.inputManager.OnGetUp += MoveBackFromDiningTable;
		cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "GetUp"));
	}

	public void MoveToLockBox()
	{
		StartCoroutine(_MoveToLockBox());
		IEnumerator _MoveToLockBox()
		{
			cabinGameManager.currentPlayerState = CabinGameManager.PlayerState.LockBox;
			firstPersonController.enabled = false;
			mainCamera.transform.DOMove(lockBox.cameraMovePoint.position, 1f).SetEase(Ease.InOutSine);
			preLockBoxCameraRotation = mainCamera.transform.localEulerAngles;
			mainCamera.transform.DORotate(lockBox.cameraMovePoint.eulerAngles, 1f).SetEase(Ease.InOutSine);
			lockBox.boxCollider.enabled = false;
			lockBox.PointLightSetIntensity(0.3f, 1f);
			if (lockBox.openLight.gameObject.activeSelf)
			{
				lockBox.OpenLightSetIntensity(0.28f, 1f);
			}
			yield return new WaitForSeconds(1f);
			CursorModeUtility.ShowCursor();
			cabinGameManager.inputManager.OnGetUp += MoveBackFromLockBox;
			cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "HoldRMBSpace"));
			cabinUIManager.crossHairCanvas.SetActive(value: false);
		}
	}

	public void MoveBackFromLockBox()
	{
		StartCoroutine(_MoveBackFromLockBox());
		IEnumerator _MoveBackFromLockBox()
		{
			CursorModeUtility.HideCursor();
			cabinUIManager.ClearControlsText();
			cabinGameManager.inputManager.OnGetUp -= MoveBackFromLockBox;
			mainCamera.transform.DOLocalMove(new Vector3(0f, 0f, 0f), 1f).SetEase(Ease.InOutSine);
			mainCamera.transform.DOLocalRotate(preLockBoxCameraRotation, 1f).SetEase(Ease.InOutSine);
			lockBox.PointLightSetIntensity(0.2f, 1f);
			if (lockBox.openLight.gameObject.activeSelf)
			{
				lockBox.OpenLightSetIntensity(0f, 1f);
			}
			yield return new WaitForSeconds(1f);
			cabinGameManager.currentPlayerState = CabinGameManager.PlayerState.Normal;
			cabinUIManager.crossHairCanvas.SetActive(value: true);
			firstPersonController.enabled = true;
			lockBox.boxCollider.enabled = true;
		}
	}

	private bool IsPlayerHoldingBaitInLeftHand()
	{
		Holdable holdingObjectLeft = GetHoldingObjectLeft();
		if (holdingObjectLeft != null)
		{
			return holdingObjectLeft.GetComponent<Bait>();
		}
		return false;
	}

	public void MoveToFishingSitting()
	{
		if (fishingRod.isKinematic || cabinGameManager.playerTalking || cabinGameManager.uiManager.phoneUI.isPaused || inTransition || !fishingRod.canCast || fishingRod.lureReached || inFishCaught)
		{
			return;
		}
		inTransition = true;
		firstPersonController.enabled = false;
		cabinGameManager.uiManager.phoneUI.allowPhone = false;
		StartCoroutine(cabinGameManager.RequestFadeInAndFadeOut(0.5f, 0.5f, 1f, delegate
		{
			mainCamera.transform.position = fishingSitting.position;
			preFishingSittingCameraRotation = mainCamera.transform.localEulerAngles;
			mainCamera.transform.localEulerAngles = fishingSitting.position;
			cabinGameManager.currentPlayerState = CabinGameManager.PlayerState.FishingSitting;
			if (currentHoldingObject is FishingRodPickable)
			{
				if (!IsPlayerHoldingBaitInLeftHand())
				{
					cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "CastGetUp"));
				}
				else
				{
					cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "CastGetUpBait"));
				}
			}
			else
			{
				cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "GetUp"));
			}
			cabinGameManager.inputManager.OnGetUp += MoveBackFromFishingSitting;
			cabinGameManager.fishingSitDownTrigger.SetActive(value: false);
			lockCameraMovement.enabled = true;
			lockCameraMovement.disableFov = false;
			cabinGameManager.isSittingFishing = true;
		}));
		StartCoroutine(ToggleInTransition());
		IEnumerator ToggleInTransition()
		{
			yield return new WaitForSeconds(2f);
			inTransition = false;
		}
	}

	public void MoveBackFromFishingSitting()
	{
		if (fishingRod.isKinematic || cabinGameManager.playerTalking || cabinGameManager.uiManager.phoneUI.isPaused || inTransition || !fishingRod.canCast || fishingRod.lureReached || inFishCaught)
		{
			return;
		}
		if (currentHoldingObject != null && currentHoldingObject is FishingRodPickable)
		{
			if (!IsPlayerHoldingBaitInLeftHand())
			{
				cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "Cast"));
			}
			else
			{
				cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "ThowBait"));
			}
		}
		else
		{
			cabinUIManager.ClearControlsText();
		}
		inTransition = true;
		StartCoroutine(cabinGameManager.RequestFadeInAndFadeOut(0.5f, 0.5f, 1f, delegate
		{
			cabinUIManager.ClearControlsText();
			cabinGameManager.inputManager.OnGetUp -= MoveBackFromFishingSitting;
			mainCamera.transform.DOLocalMove(new Vector3(0f, 0f, 0f), 1f).SetEase(Ease.InOutSine);
			mainCamera.transform.DOLocalRotate(preLockBoxCameraRotation, 1f).SetEase(Ease.InOutSine);
			cabinGameManager.currentPlayerState = CabinGameManager.PlayerState.Normal;
			firstPersonController.enabled = true;
			cabinGameManager.fishingSitDownTrigger.SetActive(value: true);
			lockCameraMovement.enabled = false;
			lockCameraMovement.disableFov = false;
			cabinGameManager.isSittingFishing = false;
		}));
		StartCoroutine(ToggleInTransition());
		IEnumerator ToggleInTransition()
		{
			yield return new WaitForSeconds(2f);
			inTransition = false;
			cabinGameManager.uiManager.phoneUI.allowPhone = true;
		}
	}

	public void MoveToSink()
	{
		StartCoroutine(_MoveToSink());
		IEnumerator _MoveToSink()
		{
			cabinGameManager.currentPlayerState = CabinGameManager.PlayerState.Sink;
			firstPersonController.enabled = false;
			mainCamera.transform.DOMove(sink.cameraMovePoint.position, 1f).SetEase(Ease.InOutSine);
			presinkCameraRotation = mainCamera.transform.localEulerAngles;
			mainCamera.transform.DORotate(sink.cameraMovePoint.eulerAngles, 1f).SetEase(Ease.InOutSine);
			sink.sinkClickable.gameObject.SetActive(value: false);
			yield return new WaitForSeconds(1f);
			(cabinGameManager.uiManager as CabinUIManager).dishWashingSlider.Slider(16f);
			currentHoldingObject.gameObject.SetActive(value: false);
			currentSecondHoldingObject.gameObject.SetActive(value: false);
			currentHoldingObject = null;
			currentSecondHoldingObject = null;
			sink.StartWashing();
			StartCoroutine(sink.HoverPlates());
			yield return new WaitForSeconds(16f);
			MoveBackFromSink();
			sink.StopWashing();
			sink.PutDownPlates();
			cabinGameManager.TriggerMikeTexts();
		}
	}

	public void MoveBackFromSink()
	{
		StartCoroutine(_MoveBackFromSink());
		IEnumerator _MoveBackFromSink()
		{
			CursorModeUtility.HideCursor();
			cabinUIManager.ClearControlsText();
			mainCamera.transform.DOLocalMove(new Vector3(0f, 0f, 0f), 1f).SetEase(Ease.InOutSine);
			mainCamera.transform.DOLocalRotate(presinkCameraRotation, 1f).SetEase(Ease.InOutSine);
			yield return new WaitForSeconds(1f);
			cabinGameManager.currentPlayerState = CabinGameManager.PlayerState.Normal;
			firstPersonController.enabled = true;
		}
	}

	public void SetCameraForPlayerInTruck(Action cameraSetupCompleteCallBack = null, bool lookAtMike = false, bool carCamera = false)
	{
		lookAt = mikeTransform;
		if (carCamera)
		{
			currentCamera = this.carCamera;
		}
		else
		{
			currentCamera = mainCamera;
		}
		returnToDefaultZoom = false;
		FreezeCameraScripts(value: true);
		dialogueCamera.gameObject.SetActive(value: true);
		cameraSetupCompleteCallBack?.Invoke();
		lookAtMikeInCar = lookAtMike;
		zoomIntoTransform = true;
	}

	public void SetCurrentCamToBedCam()
	{
		if (currentCamera != sittingOnBedCam)
		{
			currentCamera = sittingOnBedCam;
		}
		sittingCamSittingOnBed.enabled = false;
		fovZoomBedSittingCam.enabled = false;
	}

	public void SetBedSittingCamActive()
	{
		sittingCamSittingOnBed.enabled = true;
		fovZoomBedSittingCam.enabled = true;
		sittingCamSittingOnBed.ResetRotation();
		CursorModeUtility.HideCursor();
	}

	public void SetCameraForPlayerConversation(Transform lookAtTransform = null)
	{
		if (lookAtTransform != null)
		{
			lookAt = lookAtTransform;
		}
		if ((bool)carCamera)
		{
			currentCamera = carCamera;
		}
		else
		{
			currentCamera = mainCamera;
		}
		returnToDefaultZoom = false;
		FreezeCameraScripts(value: true);
		dialogueCamera.gameObject.SetActive(value: true);
		zoomIntoTransform = true;
	}

	public void SetLookAtTransform(Transform lookAtTransform)
	{
		lookAt = lookAtTransform;
	}

	public void SetLookAtToOuijaLookPoint()
	{
		SetLookAtTransform(ouijaLookPoint);
	}

	public void LookATMike()
	{
		lookAt = lookAtMike;
	}

	public void LookAtHost()
	{
		lookAt = lookAtHost;
	}

	public void LookAtHostFixingToilet()
	{
		lookAt = lookAtHostFixingToilet;
	}

	public void LookAtMikeAfterHiding()
	{
		lookAt = lookAtMikeAfterHiding;
	}

	public void LookAtMikeEnd()
	{
		lookAt = lookAtMikeEnd;
	}

	public void LookAtNora()
	{
		lookAt = lookAtNora;
	}

	public MarinadeIngredientObject GetCurrentHoldingMarinadeIngredient()
	{
		if (currentHoldingObject != null && currentHoldingObject.TryGetComponent<MarinadeIngredientObject>(out var component))
		{
			return component;
		}
		return null;
	}

	public CasseroleIngredientObject GetCurrentHoldingCasseroleIngredient()
	{
		if (currentHoldingObject != null && currentHoldingObject.TryGetComponent<CasseroleIngredientObject>(out var component))
		{
			return component;
		}
		return null;
	}

	public void EnableFishPlateFull()
	{
		platesIndex = 0;
		EnablePlate(platesIndex);
		playerHasFullFoodPlate = true;
	}

	public void EnableLastPlate()
	{
		playerHasFullFoodPlate = false;
		playerHasEmptyFoodPlate = true;
		platesIndex = platePresets.Length - 1;
		currentHoldingObject.transform.GetChild(0).gameObject.SetActive(value: false);
		EnablePlate(platesIndex);
	}

	private void DisableAllPlates()
	{
		GameObject[] array = platePresets;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
	}

	public void SetTriggerSubGetJengaActive(bool value)
	{
		triggerSubGetJenga.gameObject.SetActive(value);
	}

	public void SetTriggerSubGetOuijaActive(bool value)
	{
		triggerSubGetOuija.gameObject.SetActive(value);
	}

	public void SetTriggerTurnOffLightsActive(bool value)
	{
		triggerSubTurnOffLights.gameObject.SetActive(value);
	}

	public void SetBasementTableInteractable(bool value)
	{
		if (basementTable != null)
		{
			basementTable.SetInteractable(value);
			basementTableCollider.enabled = value;
		}
	}

	private void EnablePlate(int index)
	{
		DisableAllPlates();
		platePresets[index].SetActive(value: true);
		platePresets[index].transform.SetParent(currentHoldingObject.transform);
		this.OnTakeCasseroleFishInPlate?.Invoke();
	}

	public BoardGameType GetCurrentHoldingBoardGame()
	{
		if (currentHoldingObject != null && currentHoldingObject.TryGetComponent<BoardGame>(out var component))
		{
			this.OnTakeBoardGame?.Invoke(component.type);
			currentHoldingBoardGameType = component.type;
			return currentHoldingBoardGameType;
		}
		this.OnTakeBoardGame?.Invoke(BoardGameType.None);
		return BoardGameType.None;
	}

	public void ResumeCameraControl()
	{
		ResumeCameraInput();
		FreezeCameraScripts(value: false);
	}

	public void ResumeCameraInput()
	{
		if (lookAtMikeInCar)
		{
			lookAtMikeInCar = false;
			carCamera.transform.localRotation = Quaternion.identity;
		}
		carCamera.transform.localRotation = Quaternion.identity;
		truckCamera.fieldOfView = 60f;
		dialogueCamera.gameObject.SetActive(value: false);
		zoomIntoTransform = false;
		returnToDefaultZoom = true;
	}

	internal override void HoldObject(Holdable holdable, bool isThrowable = false)
	{
		if (currentHoldingObject == null)
		{
			if (holdable.CompareTag("Ball"))
			{
				base.HoldObject(holdable, isThrowable: true);
				return;
			}
			base.HoldObject(holdable);
			if (holdable is FishingRodPickable && cabinGameManager.currentPlayerState == CabinGameManager.PlayerState.FishingSitting)
			{
				cabinGameManager.uiManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "CastGetUp"));
			}
			return;
		}
		if (currentHoldingObject != null && currentHoldingObject.GetComponent<CasseroleIngredientObject>() != null && holdable.GetComponent<CasseroleIngredientObject>() != null)
		{
			bool flag = holdable.GetComponent<CasseroleIngredientObject>().IngredientType == CasseroleRecipie.StackablePlate;
			bool flag2 = currentHoldingObject.GetComponent<CasseroleIngredientObject>().IngredientType == CasseroleRecipie.StackablePlate;
			Debug.Log("holdable object is stackable plate:" + flag);
			Debug.Log("current holding object is stackable plate:" + flag2);
			if (holdable.GetComponent<CasseroleIngredientObject>().IngredientType == CasseroleRecipie.StackablePlate && currentHoldingObject.GetComponent<CasseroleIngredientObject>().IngredientType == CasseroleRecipie.StackablePlate)
			{
				currentSecondHoldingObject = holdable;
				holdable.GoToPosition(handPosition);
				holdable.transform.localPosition += new Vector3(0.002f, 0.0219f, 0.019f) * 1.5f;
				if (currentSecondHoldingObject.GetComponent<MikePlate>() != null)
				{
					currentSecondHoldingObject.GetComponent<MikePlate>().plateAs.Play();
				}
				return;
			}
		}
		else if (currentHoldingObject != null)
		{
			return;
		}
		Debug.Log("HandsFullSubTriggered");
		SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "HandsFull"));
	}

	public void ThrowRod()
	{
		fishingRod.gameObject.SetActive(value: false);
		fishingRod.onRod = FishingRod.OnRod.Empty;
		GameObject[] array = fishingItems;
		foreach (GameObject obj in array)
		{
			obj.transform.parent = handPosition;
			obj.SetActive(value: false);
		}
		currentHoldingObject.gameObject.SetActive(value: true);
		currentHoldingObject.Throw(cameraTransform);
		currentHoldingObject = null;
		HideRope();
		(cabinGameManager.uiManager as CabinUIManager).HideBaitUI();
	}

	internal override void Throw()
	{
		if (currentSecondHoldingObject != null)
		{
			currentSecondHoldingObject.Throw(cameraTransform);
			currentSecondHoldingObject = null;
		}
		else if (currentHoldingObject != null)
		{
			if ((bool)(currentHoldingObject as MiniCooler))
			{
				base.Throw();
			}
			else if ((bool)(currentHoldingObject as CabinSuitcase))
			{
				base.Throw();
				cabinUIManager.ClearControlsText();
			}
			else
			{
				base.Throw();
				cabinUIManager.ClearControlsText();
			}
		}
		else if (currentHoldingObjectLeft != null)
		{
			base.Throw();
			cabinUIManager.ClearControlsText();
		}
	}

	public void PlayerInsideTrigger()
	{
		playerLocation = PlayerLocation.PlayerInside;
	}

	public void PlayerOutsideTrigger()
	{
		playerLocation = PlayerLocation.PlayerOutside;
	}

	public void PlayerDeepInsideTrigger()
	{
		playerLocation = PlayerLocation.PlayerDeepInside;
	}

	public void OnEnterKitchenTrigger()
	{
		this.OnPlayerPositionInHouseChanged?.Invoke(PlayerPositionInHouse.InKitchen);
	}

	public void OnExitKitchenTrigger()
	{
		this.OnPlayerPositionInHouseChanged?.Invoke(PlayerPositionInHouse.NotInAnyRoom);
	}

	public void OnEnterLivingRoomTrigger()
	{
		this.OnPlayerPositionInHouseChanged?.Invoke(PlayerPositionInHouse.InLivingRoom);
	}

	public void OnExitLivingRoomTrigger()
	{
		this.OnPlayerPositionInHouseChanged?.Invoke(PlayerPositionInHouse.NotInAnyRoom);
	}

	public void TestingModeStartInKitchen()
	{
		playerTransform.SetPositionAndRotation(kitchenStartPoint.position, kitchenStartPoint.localRotation);
	}

	public void TestingModeStartInKitchenAfterOvenStart()
	{
		playerTransform.SetPositionAndRotation(kitchenStartPoint.position, kitchenStartPoint.localRotation);
	}

	public void TestingModeStartInKitchenAfterOvenComplete()
	{
		playerTransform.SetPositionAndRotation(kitchenStartPoint.position, kitchenStartPoint.localRotation);
	}

	public void TestingModeStartNearFishingArea()
	{
		playerTransform.SetPositionAndRotation(fishingStartPoint.position, fishingStartPoint.localRotation);
	}

	public void TestingModeStartOnDiningTableAfterOvenStart()
	{
		playerIsSeatedWithJenga = true;
		playerTransform.SetPositionAndRotation(boardGameStartPoint.position, boardGameStartPoint.localRotation);
		diningTable.TestingModeStartOnDiningTableAfterOvenStart();
		currentHoldingBoardGameType = BoardGameType.Jenga;
		jengaPieces.SetActive(value: true);
		triggerSubGetJenga.gameObject.SetActive(value: false);
	}

	public void TestingModeStartOnBasementTable()
	{
		ouijaCam = sittingCamOuijaBoardGame.GetComponent<Camera>();
		SetCurrentCamToOuijaCam();
		playerTransform.SetPositionAndRotation(basementStartPoint.position, basementStartPoint.localRotation);
		ouijaBoardGame.gameObject.SetActive(value: true);
		currentHoldingBoardGameType = BoardGameType.Ouija;
		triggerSubGetOuija.gameObject.SetActive(value: false);
		triggerSubTurnOffLights.gameObject.SetActive(value: false);
		basementTable.TestingModeStartOnBasementTableAfterOvenStart();
	}

	public void TestingModeStartOnBed()
	{
		playerTransform.SetPositionAndRotation(bedroomStandPoint.position, bedroomStandPoint.localRotation);
		firstPersonController.enabled = false;
		sittingCamSleepingOnBed.gameObject.SetActive(value: true);
	}

	public void TestingModeStartWhenMikeGoesToTruck()
	{
		playerTransform.SetPositionAndRotation(bedroomGetUpAfterSittingPoint.position, bedroomGetUpAfterSittingPoint.rotation);
		SetBedLayingCam(value: false);
		firstPersonController.enabled = true;
		firstPersonController.gameObject.SetActive(value: true);
	}

	public void TestingModeStartWhenHikerKnocks()
	{
		playerCanSitOnBed = false;
		playerCanSleepOnBed = false;
		playerTransform.SetPositionAndRotation(bedroomStandPoint.position, bedroomStandPoint.rotation);
	}

	public void TestingModeStartWhenHostAtDoor()
	{
		SetBedLayingCam(value: false);
		playerCanSitOnBed = false;
		playerCanSleepOnBed = false;
		playerTransform.SetPositionAndRotation(entranceStandPoint.position, entranceStandPoint.rotation);
		firstPersonController.enabled = true;
		firstPersonController.gameObject.SetActive(value: true);
	}

	public void TestingModeStartHostHittingDoor()
	{
		SetBedLayingCam(value: false);
		playerCanSitOnBed = false;
		playerCanSleepOnBed = false;
		playerTransform.SetPositionAndRotation(entranceStandPoint.position, entranceStandPoint.rotation);
		firstPersonController.enabled = true;
		firstPersonController.gameObject.SetActive(value: true);
	}

	public void TestingModeStartHidingInBasement()
	{
		SetBedLayingCam(value: false);
		playerCanSitOnBed = false;
		playerCanSleepOnBed = false;
		playerTransform.SetPositionAndRotation(basementHiddenPoint.position, basementHiddenPoint.rotation);
		firstPersonController.enabled = true;
		firstPersonController.gameObject.SetActive(value: true);
	}

	public void TestingModeStartInBedroom()
	{
		SetBedLayingCam(value: false);
		playerCanSitOnBed = false;
		playerCanSleepOnBed = false;
		playerTransform.SetPositionAndRotation(bedroomPoint.position, bedroomPoint.rotation);
		firstPersonController.enabled = true;
		firstPersonController.gameObject.SetActive(value: true);
	}
}
