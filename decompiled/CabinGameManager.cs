using System;
using System.Collections;
using System.Collections.Generic;
using NOT_Lonely.Weatherade;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class CabinGameManager : GameManager
{
	public enum CabinSceneType
	{
		CabinScene,
		CabinSceneDark
	}

	public enum PlayerState
	{
		Driving,
		Normal,
		Talking,
		TalkingDriving,
		LockBox,
		SittingAtSittablePlace,
		OnToilet,
		FishingMinigame,
		FishingSitting,
		PlayingOuija,
		TalkingCouch,
		TalkingBasementTable,
		TalkingBed,
		LookingAtThudSound,
		Sink,
		LookAtObject,
		LookAtSignBoard,
		Hugging
	}

	public enum CurrentMike
	{
		Prefishing,
		Fishing,
		PostFishing,
		PostEating
	}

	[Header("Player State")]
	public CabinSceneType currentCabinSceneType;

	public SequenceType CurrentSequence;

	public PlayerState currentPlayerState;

	public CurrentMike currentMike;

	[Header("Main Managers and Class References")]
	[SerializeField]
	public CabinHouseManager cabinHouseManager;

	[SerializeField]
	public MikeCabin mikeCabin;

	[SerializeField]
	public interactableObjectUI interactible;

	[SerializeField]
	internal InputManager inputManager;

	public TruckController truckController;

	public CabinPlayerController cabinPlayerController;

	private CabinUIManager cabinUIManager;

	[Header("Eating Related")]
	[SerializeField]
	private InteractableTV livingRoomTV;

	[SerializeField]
	private EatableFish cookedFish;

	[Header("Jenga Board Game Related")]
	[SerializeField]
	private JengaController jengaController;

	[SerializeField]
	private JengaDragMiniGameController jengaMiniGameController;

	[Header("Ouija Board Game Related")]
	[SerializeField]
	private LightSwitch basementLightSwitch;

	[SerializeField]
	private GameObject basementTablePointLight;

	[SerializeField]
	private CabinDoor basementDoor;

	[SerializeField]
	private OuijaController ouijaController;

	[SerializeField]
	private AudioSource thudSoundAS;

	[SerializeField]
	private Transform thudSoundLookatPoint;

	[SerializeField]
	private GameObject endBasementConvoWhileRunningTrigger;

	[SerializeField]
	private TriggerActionOnInteract triggerSubTurnOffLights;

	private bool hasHadJengaConvo;

	private bool hasHadTurnOffLightsConvo;

	private bool isBasementLightTurnedOff;

	private bool mikeHasPlacedBasementTable;

	private bool isPlayingOuija;

	[Header("Dialog Effects Related")]
	[SerializeField]
	private GameObject dialogManager;

	private DialogTextEffect dialogTextEffectComponent;

	private List<DialogTextEffect> optionTextEffectComponents;

	private AudioSource dialogHoverAS;

	private float originalDialogHoverPitch = 3f;

	[Header("Cooking Related")]
	[SerializeField]
	private MikeCabinCookController mikeController;

	[SerializeField]
	private MarinadeDish marinadeController;

	[SerializeField]
	private Oven oven;

	[SerializeField]
	private CasseroleDish casserole;

	[SerializeField]
	private IceBoxCookingController iceBox;

	[SerializeField]
	private CabinDoor iceBoxLid;

	[SerializeField]
	private GameObject iceBoxConvoTrigger;

	[SerializeField]
	private GameObject fishesInBucket;

	[SerializeField]
	private ParticleSystem waterPourTap;

	[SerializeField]
	private CabinDoor fridgeDoor;

	private bool playerHasFish;

	private bool mikeHasFish;

	[HideInInspector]
	public bool fishingInfoBoardOpen;

	public MikeFishing mikeFishing;

	public GameObject fishingSitDownTrigger;

	public bool isSittingFishing;

	[HideInInspector]
	public bool fishingDone;

	[Header("Post Eating Related")]
	public MikePostEating mikePostEating;

	public Transform mikeBedroomJumpscareTransform;

	[Header("Hide and seek Related")]
	[SerializeField]
	private GameObject atticParent;

	[SerializeField]
	public AtticDoor atticDoor;

	[SerializeField]
	private Transform playerAttic;

	[SerializeField]
	public bool playerInAttic;

	public CabinHiker cabinHiker;

	public bool cabinHikerWalkDone;

	private float ambBeforeAttic;

	private bool micWasOn;

	public micProcess microphone;

	public GameObject closetLight;

	public HostDuringHiding hostHiding;

	public HostFixingSink hostFixingSink;

	public MikeAfterHiding mikeAfterHiding;

	[Header("Rizz Sequence Related")]
	[SerializeField]
	private WhiteIntroManager whiteIntroManager;

	[SerializeField]
	private YellowIntroManager yellowIntroManager;

	[SerializeField]
	private MikeRizzlerController mikeRizzlerController;

	[SerializeField]
	private CabinSittablePlace sittableBed;

	[SerializeField]
	private InteractableTV bedroomTV;

	[SerializeField]
	private VoiceNoteUI noraVoiceNote;

	[SerializeField]
	public CabinDoor bedroomDoorSydney;

	[SerializeField]
	private CabinDoor mainDoor;

	[SerializeField]
	private Light[] truckLights;

	[SerializeField]
	private MikeTruckGoingToNora truckCabinSceneDark;

	[SerializeField]
	private AudioSource snoringAS;

	[Header("Hiker Sequence")]
	[SerializeField]
	private GameObject cabinTruck;

	[SerializeField]
	private HikerCabinController hikerCabinController;

	[SerializeField]
	private Transform hikerConvoLookatPoint;

	[SerializeField]
	public TriggerActionOnInteract hikerConvoTrigger;

	[SerializeField]
	private GameObject sinisterAudioTrigger;

	[Header("End Host Sequence")]
	[SerializeField]
	private GameObject sittingCouch;

	[SerializeField]
	private GameObject[] sittingDiningTable;

	[SerializeField]
	private GameObject[] snoringTriggers;

	[SerializeField]
	private GameObject[] porchBlockers;

	[SerializeField]
	private GameObject[] backyardBlockers;

	[SerializeField]
	private MeshRenderer glassWindow;

	[SerializeField]
	private Material snowBallOnWindowMaterial;

	[SerializeField]
	private Transform cellarDoor1;

	[SerializeField]
	private Transform cellarDoor2;

	[SerializeField]
	public HostEndGame hostEndGame;

	[SerializeField]
	public GameObject triggerDoorCrouch;

	[SerializeField]
	public TriggerEventOnInteract hostConvoTrigger;

	[SerializeField]
	public TriggerEventOnInteract subUnsafeDoorTrigger;

	[SerializeField]
	public CabinDoor basementHiddenDoor;

	[SerializeField]
	public CabinDarkHiddenDoor cabinDarkHiddenDoor;

	[SerializeField]
	public GameObject startCallTrigger;

	[SerializeField]
	public GameObject noraTextsTrigger;

	[SerializeField]
	public Transform basementDoorPoint;

	[SerializeField]
	public BreakDoor breakDoor;

	[SerializeField]
	public GameObject truck;

	[SerializeField]
	public Nora noraEnd;

	[SerializeField]
	public MikeEndGame mikeEnd;

	[SerializeField]
	public GameObject[] porchLightTrigger;

	[SerializeField]
	public GameObject outsideLight;

	[SerializeField]
	public Transform stool;

	[SerializeField]
	public CatEndGame catEndGame;

	[SerializeField]
	public GameObject mainDoorBlocker;

	[SerializeField]
	public GameObject backDoorBlocker;

	[SerializeField]
	public GameObject endGameMusicTrigger;

	[SerializeField]
	public GameObject disableSpirintTrigger;

	[SerializeField]
	public GameObject mikeStartBreakingDoorTrigger;

	[SerializeField]
	public GameObject mikeEndBreakingDoorTrigger;

	[SerializeField]
	public GameObject mainDoorParent;

	[SerializeField]
	public GameObject mainDoorBreakingParent;

	[SerializeField]
	public Stool stoolScript;

	[SerializeField]
	public GameObject atticBlocker;

	[Header("Phone Call")]
	public bool isPhoneRinging;

	[SerializeField]
	private EndCallCabin endCallCabin;

	[SerializeField]
	private float automaticCallHangUpTime = 5f;

	[SerializeField]
	private AudioSource hangUpAS;

	[Header("Other Stuff")]
	[SerializeField]
	public AudioMixer audioMixer;

	private bool phoneUIState;

	public GameObject keysUI;

	public AudioSource keysAS;

	private bool mikeStartsTalkingAsPassenger;

	private bool inConversation;

	private float firstConvoTimer;

	private bool coldNightSub;

	[SerializeField]
	private Camera fpsCamera;

	[SerializeField]
	private Camera carCamera;

	[HideInInspector]
	public bool playerTriedCrossingMapEnd;

	public bool carIsInParkingLot;

	public SRS_ParticleSystem sRS_ParticleSystem;

	public GameObject playerRoadBlocker;

	private bool hasFrozenSittingCam;

	public GameObject mikeObject;

	public GameObject hostObject;

	[Header("Testing")]
	[SerializeField]
	private Transform playerOutside;

	[SerializeField]
	private Transform playerInsideEntrance;

	[SerializeField]
	private Transform playerInFrontOfFridge;

	[SerializeField]
	private Transform playerInFrontBedroomDoor;

	[SerializeField]
	private Transform playerAfterTour;

	[SerializeField]
	private Transform playerAfterShower;

	[SerializeField]
	private Transform playerFishing;

	[SerializeField]
	private Transform playerMikeBedroom;

	[SerializeField]
	private Transform playerMikeBedroomAfterCatJumpscare;

	private bool forcedTurnedOnBasementLights;

	private bool playerSittingOnBed;

	private bool hasPlayedEerieHitForHiker;

	private bool hikerIsVisibleToPlayer;

	private bool hasShownHikerRealizationSub;

	private bool hasTalkedWithHiker;

	private Coroutine showSleepSubAfterMikeLeavesCoroutine;

	private Coroutine showSleepSubAfterHikerLeavesCoroutine;

	private bool stopShowingPostHikerSleepSub;

	private bool mikeSaidTurnOffLight;

	private bool mikeHasSatOnSofaWithFishPlate;

	public PlayerPositionInHouse CurrentPlayerPositionInHouse { get; private set; }

	private void Awake()
	{
		cabinPlayerController = playerController as CabinPlayerController;
		cabinUIManager = uiManager as CabinUIManager;
		if (currentCabinSceneType == CabinSceneType.CabinScene)
		{
			CabinDoor.mikeWillClose = true;
		}
		_ = currentCabinSceneType;
		_ = 1;
	}

	private void OnEnable()
	{
		if (currentCabinSceneType == CabinSceneType.CabinScene)
		{
			SubscribeCabinSceneActions();
		}
		if (currentCabinSceneType == CabinSceneType.CabinSceneDark)
		{
			SubscribeCabinSceneDarkActions();
		}
	}

	private void OnDisable()
	{
		if (currentCabinSceneType == CabinSceneType.CabinScene)
		{
			UnsubscribeCabinSceneActions();
		}
		if (currentCabinSceneType == CabinSceneType.CabinSceneDark)
		{
			UnsubscribeCabinSceneDarkActions();
		}
	}

	private void UnsubscribeCabinSceneDarkActions()
	{
		inputManager.OnInteractPhone -= InteractPhone;
	}

	private void SubscribeCabinSceneActions()
	{
		cabinPlayerController.OnTakeCasseroleFishInPlate += CabinPlayerController_OnTakeCasseroleFishInPlate;
		cabinPlayerController.OnPlayerPositionInHouseChanged += CabinPlayerController_OnPlayerPositionInHouseChanged;
		cabinPlayerController.OnSit += CabinPlayerController_OnSit;
		cabinPlayerController.OnSitOnCouch += CabinPlayerController_OnSitOnCouch;
		cabinPlayerController.OnSitOnBasementTable += CabinPlayerController_OnSitOnBasementTable;
		cabinPlayerController.OnGetUp += CabinPlayerController_OnGetUp;
		cabinPlayerController.OnGetUpFromCouch += CabinPlayerController_OnGetUpFromCouch;
		cabinPlayerController.OnGetUpAfterOuijaGameEnded += CabinPlayerController_OnGetUpAfterOuijaGameEnded;
		cabinPlayerController.OnGetUpAfterJengaGameEnded += CabinPlayerController_OnGetUpAfterJengaGameEnded;
		cabinPlayerController.OnHoldObject += CabinPlayerController_OnHoldObject;
		cabinPlayerController.OnThrowObject += CabinPlayerController_OnThrowObject;
		cabinPlayerController.OnEnableJengaOnDiningTable += CabinPlayerController_OnEnableJengaOnDiningTable;
		cabinPlayerController.OnEnableOuijaOnBasementTable += CabinPlayerController_OnEnableOuijaOnBasementTable;
		cabinPlayerController.OnStartPlayingOuija += CabinPlayerController_OnStartPlayingOuija;
		if (mikeCabin != null)
		{
			mikeCabin.OnMikeSitOnCouchAfterHostLeaves += MikeCabin_OnMikeSitOnCouchAfterHostLeaves;
			mikeCabin.OnMikeGoesToPostShowerPointWithFishingRod += MikeCabin_OnMikeGoesToPostShowerPointWithFishingRod;
		}
		if (mikeController != null)
		{
			mikeController.OnStartConvo += MikeController_OnStartConvo;
			mikeController.OnEndConvo += MikeController_OnEndConvo;
			mikeController.OnMikePickupBucket += MikeController_OnMikePickupBucket;
			mikeController.OnWashingFishesStarted += MikeController_OnWashingFishesStarted;
			mikeController.OnWashingFishesCompleted += MikeController_OnWashingFishesCompleted;
			mikeController.OnMikeSaysAddFishesInCasserole += MikeController_OnMikeSaysAddFishesInCasserole;
			mikeController.OnMikeSitOnSofaWhileEating += MikeController_OnMikeSitOnSofaWhileEating;
			mikeController.OnMikeRunOutOfBasementAndStartPanting += MikeController_OnMikeRunOutOfBasementAndStartPanting;
			mikeController.OnMikeGoToOven += MikeController_OnMikeGoToOven;
			mikeController.OnMikeTakeOutCasseroleAndPlaceItOnCounter += MikeController_OnMikeTakeOutCasseroleAndPlaceItOnCounter;
			mikeController.OnTakeCasseroleFishInPlate += MikeController_OnTakeCasseroleFishInPlate;
			mikeController.OnJengaConvoComplete += MikeController_OnJengaConvoComplete;
			mikeController.OnWheneverReadyConvoStarted += MikeController_OnWheneverReadyConvoStarted;
			mikeController.OnJengaGameEnded += MikeController_OnJengaGameEnded;
			mikeController.OnOuijaStartConvoStarted += MikeController_OnOuijaStartConvoStarted;
			mikeController.OnMikeSittingJengaConvoStarted += MikeController_OnMikeSittingJengaConvoStarted;
			mikeController.OnMikeSaysFollowMe += MikeController_OnMikeSaysFollowMe;
			mikeController.OnPlaceBasementTable += MikeController_OnPlaceBasementTable;
			mikeController.OnTurnOffLightsConvoStarted += MikeController_OnTurnOffLightsConvoStarted;
			mikeController.OnMikeSaysHolyCrapAndRunsAway += MikeController_OnMikeSaysHolyCrapAndRunsAway;
		}
		else
		{
			Debug.Log("Mike Controller is Null");
		}
		if (marinadeController != null)
		{
			marinadeController.OnRecipeCompleted += MarinadeController_OnRecipeCompleted;
			marinadeController.OnIngredientDestroyed += MarinadeController_OnIngredientDestroyed;
		}
		if (livingRoomTV != null)
		{
			livingRoomTV.OnLivingRoomTVTurnedOn += InteractibleTV_OnTVTurnedOn;
			livingRoomTV.OnTVTurnedOff += InteractibleTV_OnTVTurnedOff;
		}
		if (cookedFish != null)
		{
			cookedFish.OnStartEating += CookedFish_OnStartEating;
			cookedFish.OnFinishEating += CookedFish_OnFinishEating;
		}
		if (oven != null)
		{
			oven.OnCasserolePlacedInOven += Oven_OnCasserolePlacedInOven;
			oven.OnOvenTurnedOn += Oven_OnOvenTurnedOn;
		}
		if (jengaController != null)
		{
			jengaController.OnMikesTurnStarted += JengaController_OnMikesTurnStarted;
			jengaController.OnMikesTurnEnded += JengaController_OnMikesTurnEnded;
			jengaController.OnPlayersTurnEnded += JengaController_OnPlayersTurnEnded;
			jengaController.OnPieceSelected += JengaController_OnPieceSelected;
		}
		if (jengaMiniGameController != null)
		{
			jengaMiniGameController.OnMiniGameStarted += JengaMiniGameController_OnMiniGameStarted;
			jengaMiniGameController.OnMiniGameLost += JengaMiniGameController_OnMiniGameLost;
			jengaMiniGameController.OnMiniGameWon += JengaMiniGameController_OnMiniGameWon;
		}
		if (basementLightSwitch != null)
		{
			basementLightSwitch.OnLightTurnedOn += BasementLight_OnLightTurnedOn;
			basementLightSwitch.OnLightTurnedOff += BasementLight_OnLightTurnedOff;
		}
		if (ouijaController != null)
		{
			ouijaController.OnReachingYesPoint += OuijaController_OnReachingYesPoint;
			ouijaController.OnRoundEnd += OuijaController_OnRoundEnd;
		}
		inputManager.OnInteractPhone += InteractPhone;
	}

	private void MikeController_OnMikeSitOnSofaWhileEating()
	{
		mikeHasSatOnSofaWithFishPlate = true;
	}

	private void MikeController_OnMikeTakeOutCasseroleAndPlaceItOnCounter()
	{
		casserole.SetInteractable(value: true);
	}

	private void MikeController_OnMikeRunOutOfBasementAndStartPanting()
	{
		endBasementConvoWhileRunningTrigger.SetActive(value: false);
	}

	private void MikeCabin_OnMikeGoesToPostShowerPointWithFishingRod()
	{
		GenericAudioReferences.instance.FadeToZeroHostLeft();
		livingRoomTV.SetCanBeTurnedOff(value: true, isEatingSeq: false);
	}

	private void MikeCabin_OnMikeSitOnCouchAfterHostLeaves()
	{
		livingRoomTV.SetCanBeTurnedOff(value: false, isEatingSeq: false);
	}

	private void UnsubscribeCabinSceneActions()
	{
		cabinPlayerController.OnTakeCasseroleFishInPlate -= CabinPlayerController_OnTakeCasseroleFishInPlate;
		cabinPlayerController.OnPlayerPositionInHouseChanged -= CabinPlayerController_OnPlayerPositionInHouseChanged;
		cabinPlayerController.OnSit -= CabinPlayerController_OnSit;
		cabinPlayerController.OnGetUp -= CabinPlayerController_OnGetUp;
		cabinPlayerController.OnHoldObject -= CabinPlayerController_OnHoldObject;
		cabinPlayerController.OnThrowObject += CabinPlayerController_OnThrowObject;
		if (mikeController != null)
		{
			mikeController.OnStartConvo -= MikeController_OnStartConvo;
			mikeController.OnEndConvo -= MikeController_OnEndConvo;
			mikeController.OnJengaConvoComplete -= MikeController_OnJengaConvoComplete;
			mikeController.OnWheneverReadyConvoStarted -= MikeController_OnWheneverReadyConvoStarted;
		}
		if (marinadeController != null)
		{
			marinadeController.OnRecipeCompleted -= MarinadeController_OnRecipeCompleted;
			marinadeController.OnIngredientDestroyed -= MarinadeController_OnIngredientDestroyed;
		}
		if (livingRoomTV != null)
		{
			livingRoomTV.OnLivingRoomTVTurnedOn -= InteractibleTV_OnTVTurnedOn;
		}
		if (cookedFish != null)
		{
			cookedFish.OnStartEating -= CookedFish_OnStartEating;
		}
		if (oven != null)
		{
			oven.OnCasserolePlacedInOven -= Oven_OnCasserolePlacedInOven;
		}
		if (jengaMiniGameController != null)
		{
			jengaMiniGameController.OnMiniGameStarted -= JengaMiniGameController_OnMiniGameStarted;
			jengaMiniGameController.OnMiniGameLost -= JengaMiniGameController_OnMiniGameLost;
		}
		inputManager.OnInteractPhone -= InteractPhone;
		UIManager.OpenPhone = (Action)Delegate.Remove(UIManager.OpenPhone, new Action(SendMikeTexts1));
	}

	private void MikeController_OnMikeSaysHolyCrapAndRunsAway()
	{
		cabinPlayerController.SetDiningTableInteractable(value: false);
	}

	private void MikeController_OnJengaGameEnded()
	{
		cabinUIManager.SetCrossHairCanvasActive(value: true);
		HideCursor();
	}

	private void MikeController_OnMikePickupBucket()
	{
		fishesInBucket.SetActive(value: true);
		cabinPlayerController.SetDiningTableInteractable(value: false);
	}

	private void MikeController_OnWashingFishesCompleted()
	{
		cabinUIManager.ClearAndWipeLoadText();
		fishesInBucket.SetActive(value: false);
	}

	private void MikeController_OnMikeGoToOven()
	{
		casserole.SetInteractable(value: false);
		cabinPlayerController.SetDiningTableInteractable(value: false);
	}

	private void MikeController_OnMikeSaysAddFishesInCasserole()
	{
		iceBoxLid.SetInteractable(value: true);
		casserole.SetInteractable(value: true);
	}

	private void MikeController_OnWashingFishesStarted()
	{
		fishesInBucket.SetActive(value: true);
		iceBoxConvoTrigger.SetActive(value: true);
		marinadeController.SetInteractable(value: true);
		CurrentSequence = SequenceType.Cooking;
	}

	private void MikeRizzlerController_OnMikeSitInTruck()
	{
		StartCoroutine(DelayedAction());
		IEnumerator DelayedAction()
		{
			truckCabinSceneDark.SetHasStartedDrivingTrue();
			yield return new WaitForSeconds(2f);
			Light[] array = truckLights;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = true;
			}
		}
	}

	private void CabinPlayerController_OnGetUpAfterOuijaGameEnded()
	{
		ChangePlayerState(PlayerState.Normal);
		mikeController.SetPlayerIsSeated(value: false);
		mikeController.SetInteractable(value: true);
	}

	private void CabinPlayerController_OnStartPlayingOuija()
	{
		mikeController.LookAwayFromOuijaPoints();
	}

	private void CabinPlayerController_OnSitOnBasementTable()
	{
		mikeController.LookAtPlayer();
		mikeController.SetPlayerIsSeated(value: true);
	}

	private void CabinPlayerController_OnGetUpFromCouch()
	{
		mikeController.SetInteractable(value: true);
	}

	private void CabinPlayerController_OnGetUpAfterJengaGameEnded()
	{
		jengaMiniGameController.gameObject.SetActive(value: false);
		jengaController.enabled = false;
		mikeController.StandUpAfterBoardGameEnd();
		mikeController.SetBarkOnPlayerGetUpAfterPlayingJengaAfterTime();
		mikeController.ChangeConvoType(ConversationType.CookingAndEatingConvo);
		cabinPlayerController.OnGetUpAfterJengaGameEnded -= CabinPlayerController_OnGetUpAfterJengaGameEnded;
		interactible.ChangeCamera(fpsCamera);
	}

	private void CabinPlayerController_OnEnableJengaOnDiningTable()
	{
		mikeController.CanStartJengaConvo = true;
		mikeController.SetConvoIndexToJengaConvo();
	}

	private void CabinPlayerController_OnEnableOuijaOnBasementTable()
	{
		cabinPlayerController.FreezeSittingCamScriptsAndResetRotation(freezeCams: true, resetRotation: false);
		mikeController.SetConvoIndexToOuijaConvo();
		mikeController.CanStartOuijaConvo = true;
		mikeController.SetMikeTransformToCloserSitPoint();
		isPlayingOuija = true;
		StartCoroutine(DelayedAction());
		IEnumerator DelayedAction()
		{
			yield return new WaitForSeconds(2f);
			mikeController.SetBarkDoYouKnowHowToOuija();
		}
	}

	private void CabinPlayerController_OnThrowObject()
	{
		if (cabinPlayerController.currentHoldingObject != null && cabinPlayerController.GetCurrentHoldingBoardGame() != BoardGameType.None)
		{
			inputManager.OnThrow += mikeController.OnBoardGameThrown;
		}
		if (cabinPlayerController.GetCurrentHoldingBoardGame() == BoardGameType.None)
		{
			if (hasHadJengaConvo)
			{
				cabinPlayerController.SetTriggerSubGetJengaActive(value: true);
			}
			else if (hasHadTurnOffLightsConvo || mikeHasPlacedBasementTable)
			{
				cabinPlayerController.SetTriggerSubGetOuijaActive(value: true);
			}
		}
	}

	private void CabinPlayerController_OnHoldObject()
	{
		BoardGameType currentHoldingBoardGame = cabinPlayerController.GetCurrentHoldingBoardGame();
		if (currentHoldingBoardGame == BoardGameType.None)
		{
			return;
		}
		mikeController.SetBarkRespondToBoardGame(playerHasBoardGame: true, currentHoldingBoardGame);
		inputManager.OnThrow += mikeController.OnBoardGameThrown;
		if (currentHoldingBoardGame == BoardGameType.Jenga)
		{
			cabinPlayerController.SetTriggerSubGetJengaActive(value: false);
		}
		if (currentHoldingBoardGame == BoardGameType.Ouija)
		{
			cabinPlayerController.SetTriggerSubGetOuijaActive(value: false);
			if (mikeHasPlacedBasementTable)
			{
				cabinPlayerController.SetBasementTableInteractable(value: true);
			}
		}
	}

	private void CabinPlayerController_OnGetUp()
	{
		ChangePlayerState(PlayerState.Normal);
		if (mikeController.isActiveAndEnabled)
		{
			mikeController.SetPlayerIsSeated(value: false);
		}
	}

	private void CabinPlayerController_OnSitOnCouch()
	{
		mikeController.LookAwayFromPlayer();
	}

	private void CabinPlayerController_OnSit()
	{
		ChangePlayerState(PlayerState.SittingAtSittablePlace);
		mikeController.SetPlayerIsSeated(value: true);
	}

	private void CabinPlayerController_OnPlayerPositionInHouseChanged(PlayerPositionInHouse playerPositionInHouse)
	{
		CurrentPlayerPositionInHouse = playerPositionInHouse;
		switch (CurrentPlayerPositionInHouse)
		{
		case PlayerPositionInHouse.InLivingRoom:
			if (playerHasFish && mikeHasFish)
			{
				mikeController.SetBarkWannaTurnOnTVAfterTime();
			}
			else if (mikeHasFish && !playerHasFish)
			{
				mikeController.SetBarkHurryUpAfterTime();
			}
			break;
		case PlayerPositionInHouse.NotInAnyRoom:
		case PlayerPositionInHouse.InKitchen:
			break;
		}
	}

	private void CabinPlayerController_OnTakeCasseroleFishInPlate()
	{
		playerHasFish = true;
		mikeController.PlayerHasFish = true;
	}

	private void OuijaController_OnRoundEnd(int roundIndex)
	{
		switch (roundIndex)
		{
		case 0:
			ChangePlayerState(PlayerState.Talking);
			mikeController.StartConvoOuijaPlayerAskQuestion();
			break;
		case 1:
			ChangePlayerState(PlayerState.Talking);
			mikeController.StartConvoOuijaMyTurn();
			break;
		}
	}

	private void OuijaController_OnReachingYesPoint()
	{
		StartCoroutine(DelayedAction());
		IEnumerator DelayedAction()
		{
			yield return new WaitForSeconds(2f);
			ChangePlayerState(PlayerState.Talking);
			mikeController.StartConvoOuijaAreYouMovingIt();
		}
	}

	private void MikeController_OnPlaceBasementTable()
	{
		mikeHasPlacedBasementTable = true;
		cabinPlayerController.SetBasementTableInteractable(value: true);
		if (!isBasementLightTurnedOff)
		{
			cabinPlayerController.SetTriggerTurnOffLightsActive(value: true);
		}
		else if (cabinPlayerController.GetCurrentHoldingBoardGame() != BoardGameType.Ouija)
		{
			cabinPlayerController.SetTriggerSubGetOuijaActive(value: true);
		}
	}

	private void MikeController_OnTurnOffLightsConvoStarted()
	{
		hasHadTurnOffLightsConvo = true;
	}

	private void MikeController_OnMikeSaysFollowMe()
	{
		mikeController.SetPlayerTransform(cabinPlayerController.firstPersonController.transform);
		if (!forcedTurnedOnBasementLights)
		{
			basementLightSwitch.TurnOn(playSound: false);
		}
		forcedTurnedOnBasementLights = true;
	}

	private void MikeController_OnMikeSittingJengaConvoStarted()
	{
		cabinPlayerController.FreezeSittingCameraScripts(freezeCams: true);
		cabinPlayerController.ResetSittingCamRotation();
	}

	private void MikeController_OnWheneverReadyConvoStarted()
	{
		cabinPlayerController.UnAssignDiningTableGetUpCallbackFromInputManager();
	}

	private void MikeController_OnOuijaStartConvoStarted()
	{
		CurrentSequence = SequenceType.PlayingOuija;
		cabinPlayerController.UnAssignGetUpCallbacksFromInputManager();
		cabinPlayerController.FreezeSittingCamScriptsAndResetRotation(freezeCams: true);
	}

	private void MikeController_OnJengaConvoComplete()
	{
		CurrentSequence = SequenceType.PlayingJenga;
		hasHadJengaConvo = true;
		cabinPlayerController.CanPlaceJenga = true;
		cabinPlayerController.SetDiningTableInteractable();
	}

	private void MikeController_OnTakeCasseroleFishInPlate()
	{
		mikeHasFish = true;
	}

	private void MikeController_OnEndConvo()
	{
		uiManager.controlsText.gameObject.SetActive(value: true);
		cabinUIManager.SetCrossHairCanvasActive(value: true);
		cabinPlayerController.SetCanSitOnCouch(value: true);
		cabinPlayerController.EndConvoWithMike();
		if (CurrentSequence == SequenceType.Cooking)
		{
			fridgeDoor.SetInteractable(value: true);
		}
		if (cabinPlayerController.GetCurrentHoldingBoardGame() != BoardGameType.None)
		{
			cabinPlayerController.currentHoldingObject.SetInteractable(value: true);
		}
		if (hasFrozenSittingCam)
		{
			if (!isPlayingOuija)
			{
				ShowGetUpControlsText();
			}
			hasFrozenSittingCam = false;
			cabinPlayerController.FreezeSittingCamScriptsAndResetRotation(freezeCams: false);
			cabinPlayerController.SetSittingCameraFOV(CameraFOVPreset.Normal);
		}
	}

	private void MikeController_OnStartConvo()
	{
		uiManager.controlsText.gameObject.SetActive(value: false);
		cabinPlayerController.SetCanSitOnCouch(value: false);
		if (uiManager.phoneUI.isPaused)
		{
			uiManager.phoneUI.ClosePhone();
		}
		if (currentPlayerState == PlayerState.SittingAtSittablePlace)
		{
			cabinPlayerController.FreezeSittingCamScriptsAndResetRotation(freezeCams: true);
			cabinPlayerController.SetSittingCameraFOV(CameraFOVPreset.InConversation);
			hasFrozenSittingCam = true;
		}
		if (cabinPlayerController.GetCurrentHoldingBoardGame() != BoardGameType.None)
		{
			cabinPlayerController.currentHoldingObject.SetInteractable(value: false);
		}
		ChangePlayerState(PlayerState.Talking);
		if (CurrentSequence == SequenceType.Cooking)
		{
			fridgeDoor.SetInteractable(value: false);
		}
		if (currentPlayerState != PlayerState.PlayingOuija)
		{
			cabinPlayerController.StartConvoWithMike(mikeController.mikeHead);
		}
	}

	private void BasementLight_OnLightTurnedOn()
	{
		if (CurrentSequence == SequenceType.GoingToPlayOuija)
		{
			mikeController.SetBasementLightOn(value: true);
			isBasementLightTurnedOff = false;
			cabinPlayerController.SetTriggerTurnOffLightsActive(value: true);
		}
	}

	private void BasementLight_OnLightTurnedOff()
	{
		if (!GenericAudioReferences.instance.ouijaAmbientPiano.isPlaying && CurrentSequence == SequenceType.PlayingOuija && mikeHasPlacedBasementTable)
		{
			StartCoroutine(DelayedAction());
		}
		if (CurrentSequence == SequenceType.GoingToPlayOuija)
		{
			mikeController.SetBasementLightOn(value: false);
			isBasementLightTurnedOff = true;
			cabinPlayerController.SetTriggerTurnOffLightsActive(value: false);
		}
		static IEnumerator DelayedAction()
		{
			yield return new WaitForSeconds(2f);
			GenericAudioReferences.instance.ouijaAmbientPiano.Play();
		}
	}

	private void JengaController_OnMikesTurnStarted()
	{
		mikeController.TakeOutJengaBlock();
	}

	private void JengaController_OnMikesTurnEnded(bool isTowerStable)
	{
		mikeController.RespondToTurnEnd(isTowerStable);
	}

	private void JengaController_OnPlayersTurnEnded(bool isTowerStable)
	{
		if (isTowerStable)
		{
			mikeController.ReactToPlayerWinJengaMiniGame();
			StopAllCoroutines();
			StartCoroutine(DelayedAction());
		}
		else
		{
			mikeController.ReactToPlayerLoseJenga();
			StopAllCoroutines();
			StartCoroutine(DelayedAction2());
		}
		IEnumerator DelayedAction()
		{
			yield return new WaitForSeconds(1.5f);
			mikeController.SetBarkOnPlayerWinJengaMinigame();
		}
		IEnumerator DelayedAction2()
		{
			yield return new WaitForSeconds(1.5f);
			mikeController.SetBarkOnPlayerLoseJenga();
		}
	}

	private void JengaController_OnPieceSelected()
	{
		mikeController.LookAwayFromPlayer();
	}

	private void JengaMiniGameController_OnMiniGameStarted()
	{
		cabinPlayerController.SetDiningTableInteractable(value: false);
	}

	private void JengaMiniGameController_OnMiniGameWon()
	{
		cabinPlayerController.FreezeSittingCamScriptsAndResetRotation(freezeCams: true, resetRotation: false);
		mikeController.ReactToPlayerWinJengaMiniGame();
		StartCoroutine(DelayedAction());
		IEnumerator DelayedAction()
		{
			yield return new WaitForSeconds(1.5f);
			mikeController.SetBarkOnPlayerWinJengaMinigame();
		}
	}

	private void JengaMiniGameController_OnMiniGameLost()
	{
		cabinPlayerController.FreezeSittingCamScriptsAndResetRotation(freezeCams: true, resetRotation: false);
		mikeController.ReactToPlayerLoseJenga();
		StartCoroutine(DelayedAction());
		IEnumerator DelayedAction()
		{
			yield return new WaitForSeconds(1.5f);
			mikeController.SetBarkOnPlayerLoseJenga();
		}
	}

	private void Oven_OnCasserolePlacedInOven()
	{
		marinadeController.SetInteractable(value: false);
		mikeController.OnCasserolePlacedInOven();
	}

	private void Oven_OnOvenTurnedOn()
	{
		if (ChecklistAccess.IsGlobalTestingEnabled())
		{
			CabinSceneChecklist instance = CabinSceneChecklist.GetInstance();
			if ((object)instance == null || instance.testingMode != CabinSceneChecklist.TestingMode.StartOnBasementTableAfterOvenStart)
			{
				CabinSceneChecklist instance2 = CabinSceneChecklist.GetInstance();
				if ((object)instance2 == null || instance2.testingMode != CabinSceneChecklist.TestingMode.StartOnDiningTableAfterOvenStart)
				{
					CabinSceneChecklist instance3 = CabinSceneChecklist.GetInstance();
					if ((object)instance3 == null || instance3.testingMode != CabinSceneChecklist.TestingMode.StartInKitchenAfterOvenComplete)
					{
						mikeController.EndConvo();
						mikeController.SetInteractable(value: false);
						mikeController.TurnToKitchenWaitPointThenGoToKitchenWaitPoint();
						mikeController.ChangeConvoType(ConversationType.BoardGameConvo);
					}
				}
			}
		}
		else
		{
			mikeController.EndConvo();
			mikeController.SetInteractable(value: false);
			mikeController.TurnToKitchenWaitPointThenGoToKitchenWaitPoint();
			mikeController.ChangeConvoType(ConversationType.BoardGameConvo);
		}
		casserole.cookingStarted = false;
		casserole.marinade.cookingStarted = false;
	}

	private void MarinadeController_OnIngredientDestroyed()
	{
		(uiManager as CabinUIManager).ClearControlsText();
	}

	private void MarinadeController_OnRecipeCompleted()
	{
		mikeController.TurnToFridge();
		iceBox?.EnableAllFishes();
		iceBoxConvoTrigger.SetActive(value: false);
		waterPourTap.Stop();
	}

	private void CookedFish_OnFinishEating()
	{
		(playerController as CabinPlayerController).EnableLastPlate();
		mikeController.SetPlayerHasFinishedEating(value: true);
		livingRoomTV.SetCanBeTurnedOff(value: true);
		if (mikeController.gameObject.activeInHierarchy)
		{
			mikeController.GetUpFromSofa();
		}
	}

	private void CookedFish_OnStartEating()
	{
		(playerController as CabinPlayerController).UnassignCouchGetUpCallbackFromInputManager();
		if (mikeHasSatOnSofaWithFishPlate)
		{
			mikeController.PlayEatingSFX();
		}
		else
		{
			mikeController.GoToLivingRoomSofa();
		}
		mikeController.SetPlayerHasFinishedEating(value: false);
	}

	private void InteractibleTV_OnTVTurnedOn()
	{
		cookedFish.HasTurnedOnTV = true;
	}

	private void InteractibleTV_OnTVTurnedOff()
	{
		cookedFish.HasTurnedOnTV = false;
	}

	private void SubscribeCabinSceneDarkActions()
	{
		mikeRizzlerController.OnStartConvo += MikeRizzlerController_OnStartConvo;
		mikeRizzlerController.OnEndConvo += MikeRizzlerController_OnEndConvo;
		mikeRizzlerController.OnMikeRevealComplete += MikeRizzlerController_OnMikeRevealComplete;
		mikeRizzlerController.OnMikeWalkingToBed += MikeRizzlerController_OnMikeWalkingToBed;
		mikeRizzlerController.OnMikeSitOnBed += MikeRizzlerController_OnMikeSitOnBed;
		mikeRizzlerController.OnMikeGoingToFrontDoor += MikeRizzlerController_OnMikeGoingToFrontDoor;
		mikeRizzlerController.OnMikeGoingToTruck += MikeRizzlerController_OnMikeGoingToTruck;
		mikeRizzlerController.OnMikeSitInTruck += MikeRizzlerController_OnMikeSitInTruck;
		cabinPlayerController.OnSitOnBed += CabinPlayerController_OnSitOnBed;
		cabinPlayerController.OnGetUpFromBedAfterSitting += CabinPlayerController_OnGetUpFromBed;
		cabinPlayerController.OnHugStart += CabinPlayerController_OnStartHug;
		cabinPlayerController.OnSleepOnBed += CabinPlayerController_OnSleepOnBed;
		bedroomDoorSydney.OnDoorOpened += BedroomDoorSydney_OnDoorOpened;
		bedroomDoorSydney.OnDoorClosed += BedroomDoorSydney_OnDoorClosed;
		mainDoor.OnDoorOpened += MainDoor_OnDoorOpened;
		mainDoor.OnDoorClosed += MainDoor_OnDoorClosed;
		noraVoiceNote.OnPlayedVoiceNoteTill5Seconds += NoraVoiceNote_OnPlayedVoiceNoteTill5Seconds;
		hikerCabinController.OnHikerVisible += HikerCabinController_OnHikerVisible;
		hikerCabinController.OnHikerNotVisible += HikerCabinController_OnHikerNotVisible;
		hikerCabinController.OnStartConvo += HikerCabinController_OnStartConvo;
		hikerCabinController.OnEndConvo += HikerCabinController_OnEndConvo;
		hikerCabinController.OnStartKickingCellarDoor += HikerCabinController_OnStartKickingCellarDoor;
		hikerCabinController.OnStartGoingAwayToCellarDoor += HikerCabinController_OnStartGoingAwayToCellarDoor;
		cabinPlayerController.firstPersonController.OnCrouch += FirstPersonController_OnCrouch;
		microphone.OnShoutNoUI += Microphone_OnShout;
		inputManager.OnInteractPhone += InteractPhone;
	}

	private void CabinPlayerController_OnStartHug(float seconds)
	{
		StartCoroutine(mikeRizzlerController.HugPlayer(seconds));
	}

	private void HikerCabinController_OnStartGoingAwayToCellarDoor()
	{
		cabinPlayerController.SetCanSleepOnBed(value: true);
		sinisterAudioTrigger.SetActive(value: false);
		cabinPlayerController.SetHasTalkedWithHiker(value: true);
		hasTalkedWithHiker = true;
		GenericAudioReferences.instance.FadeSinisterToCustomValue(0f);
		showSleepSubAfterHikerLeavesCoroutine = StartCoroutine(DelayedAction());
		IEnumerator DelayedAction()
		{
			yield return new WaitForSeconds(5f);
			SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "ManIrrelevant"));
			yield return new WaitForSeconds(5f);
			SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "GetSomeSleep"));
			while (!stopShowingPostHikerSleepSub)
			{
				yield return new WaitForSeconds(20f);
				SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "GetSomeSleep"));
			}
		}
	}

	private void FirstPersonController_OnCrouch(bool isCrouching)
	{
		hikerCabinController.SetPlayerIsCrouching(isCrouching);
	}

	private void HikerCabinController_OnStartKickingCellarDoor()
	{
		StopSnoring();
	}

	private void HikerCabinController_OnEndConvo()
	{
		uiManager.controlsText.gameObject.SetActive(value: true);
		cabinPlayerController.EndConvoWithMike();
		playerTalking = false;
		ChangePlayerState(PlayerState.Normal);
	}

	private void HikerCabinController_OnStartConvo()
	{
		uiManager.controlsText.gameObject.SetActive(value: false);
		playerTalking = true;
		ChangePlayerState(PlayerState.Talking);
		cabinPlayerController.StartConvoWithMike(hikerConvoLookatPoint);
		SubTextManager.GetInstance().HideSubText();
	}

	private void HikerCabinController_OnHikerNotVisible()
	{
		hikerIsVisibleToPlayer = false;
		microphone.SetCanCheckMicWithoutUI(value: false);
	}

	private void HikerCabinController_OnHikerVisible()
	{
		hikerIsVisibleToPlayer = true;
		microphone.SetCanCheckMicWithoutUI(value: true);
	}

	public void EnableMicrophoneDetectionForHikerSequence()
	{
		hikerCabinController.SetCheckIfVisibleToPlayer(value: true);
		if (CurrentSequence == SequenceType.HikerSequence && cabinPlayerController.firstPersonController.isCrouching)
		{
			microphone.SetCanCheckMicWithoutUI(value: true);
		}
	}

	public void DisableMicrophoneDetectionForHikerSequence()
	{
		hikerCabinController.SetCheckIfVisibleToPlayer(value: false);
		if (CurrentSequence == SequenceType.HikerSequence)
		{
			microphone.SetCanCheckMicWithoutUI(value: false);
		}
	}

	private void Microphone_OnShout()
	{
		Debug.Log("On Mic Shout called");
		if (CurrentSequence == SequenceType.HikerSequence && !hasPlayedEerieHitForHiker && hikerCabinController.canSuddenlyLookAtPlayer && cabinPlayerController.firstPersonController.isCrouching)
		{
			hikerCabinController.LookAtPlayerSuddenly();
			hasPlayedEerieHitForHiker = true;
			GenericAudioReferences.instance.eerieHit.Play();
		}
	}

	public void ShowSubHorrific()
	{
		if (CurrentSequence == SequenceType.HikerSequence && !hasShownHikerRealizationSub && cabinPlayerController.firstPersonController.isCrouching && hikerIsVisibleToPlayer)
		{
			StartCoroutine(DelayedAction());
		}
		IEnumerator DelayedAction()
		{
			yield return new WaitForSeconds(1f);
			GenericAudioReferences.instance.eerieStrings.Play();
			if (!playerTalking)
			{
				SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "RealizeSomeoneOut"));
			}
			hasShownHikerRealizationSub = true;
		}
	}

	private void MainDoor_OnDoorClosed()
	{
		cabinPlayerController.SetMainDoorLocked(value: true);
	}

	private void MainDoor_OnDoorOpened()
	{
		cabinPlayerController.SetMainDoorLocked(value: false);
	}

	private void BedroomDoorSydney_OnDoorClosed()
	{
		cabinPlayerController.SetBedroomDoorClosed(value: true);
	}

	private void BedroomDoorSydney_OnDoorOpened()
	{
		sittableBed.SetCollidersActive(value: false);
		sittableBed.SetInteractable(value: false);
		cabinPlayerController.SetBedroomDoorClosed(value: false);
		StartCoroutine(DelayedAction());
		IEnumerator DelayedAction()
		{
			yield return new WaitForSeconds(2f);
			sittableBed.SetCollidersActive(value: true);
			sittableBed.SetInteractable(value: true);
		}
	}

	public void ShowFreezingColdSub()
	{
		SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "FreezingCold"));
	}

	private void NoraVoiceNote_OnPlayedVoiceNoteTill5Seconds()
	{
		mikeRizzlerController.StandUpNearBed();
	}

	private void CabinPlayerController_OnSleepOnBed()
	{
		if (CurrentSequence == SequenceType.RizzSequence)
		{
			CurrentSequence = SequenceType.HikerSequence;
			StopCoroutine(showSleepSubAfterMikeLeavesCoroutine);
			AudioMixerManager.FadeGameSoundToCustom(-80f, 0.5f, this);
		}
		if (CurrentSequence == SequenceType.HikerSequence && hasTalkedWithHiker)
		{
			stopShowingPostHikerSleepSub = true;
			StopCoroutine(showSleepSubAfterHikerLeavesCoroutine);
			StartCoroutine(DelayedAction());
		}
		IEnumerator DelayedAction()
		{
			StopSnoring();
			yield return new WaitForSeconds(8f);
			hikerCabinController.PlayCellarDoorOpenSFX();
			cellarDoor1.transform.localEulerAngles = new Vector3(347.5f, 145.5f, 8.4f);
			cellarDoor2.transform.localEulerAngles = new Vector3(348.2f, 219f, 350.6f);
			yield return new WaitForSeconds(2f);
			hikerCabinController.ScreamAndRunAway();
			GenericAudioReferences.instance.deepBreaths.Play();
			yield return new WaitForSeconds(9f);
			GenericAudioReferences.instance.snowballHit.volume = 8f;
			GenericAudioReferences.instance.snowballHit.Play();
			glassWindow.material = snowBallOnWindowMaterial;
			yield return new WaitForSeconds(1f);
			GenericAudioReferences.instance.scaryViolin.Play();
			yield return new WaitForSeconds(2f);
			cabinPlayerController.SetHasWokeUpToScream(value: true);
			cabinPlayerController.AssignBedGetUpCallbackToInputManager();
			hikerCabinController.gameObject.SetActive(value: false);
			yield return new WaitForSeconds(8f);
			GenericAudioReferences.instance.ambTextSent.volume = 0f;
			GenericAudioReferences.instance.ambTextSent.Play();
			StartCoroutine(FadeAudioSource.StartFade(GenericAudioReferences.instance.ambTextSent, 4f, 1f));
		}
	}

	private void CabinPlayerController_OnGetUpFromBed()
	{
		mikeRizzlerController.SetPlayerIsSeated(value: false);
		mikeRizzlerController.GoToDoorEndThenSayEmoStuff();
		playerSittingOnBed = false;
	}

	private void CabinPlayerController_OnSitOnBed()
	{
		ChangePlayerState(PlayerState.SittingAtSittablePlace);
		mikeRizzlerController.SetInteractable(value: false);
		mikeRizzlerController.EndConvoSetMikeInteractable();
		mikeRizzlerController.SetPlayerIsSeated(value: true);
		playerSittingOnBed = true;
		StartCoroutine(DelayedAction());
		IEnumerator DelayedAction()
		{
			yield return new WaitForSeconds(2f);
			cabinPlayerController.SetCurrentCamToBedCam();
			mikeRizzlerController.StartRizzConvo();
		}
	}

	public void MikeRizzlerController_OnMikeSaysHeWillGetNora()
	{
		cabinPlayerController.AssignBedGetUpCallbackToInputManager();
	}

	private void MikeRizzlerController_OnMikeGoingToTruck()
	{
		cabinPlayerController.SetCanSleepOnBed(value: true);
		showSleepSubAfterMikeLeavesCoroutine = StartCoroutine(DelayedAction());
		static IEnumerator DelayedAction()
		{
			yield return new WaitForSeconds(20f);
			SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "GettingSleep"));
		}
	}

	private void MikeRizzlerController_OnMikeGoingToFrontDoor()
	{
		cabinPlayerController.SetMainDoorLocked(value: false);
		sittableBed.ChangeType(SitPlaceType.LayingBed);
		cabinPlayerController.SetMikeInRoom(value: false);
	}

	private void MikeRizzlerController_OnMikeWalkingToBed()
	{
		sittableBed.SetCollidersActive(value: false);
		sittableBed.SetInteractable(value: false);
	}

	private void MikeRizzlerController_OnMikeSitOnBed()
	{
		sittableBed.ChangeType(SitPlaceType.SittingBed);
	}

	public void MikeRizzlerController_OnEndConvoSitOnBed()
	{
		cabinPlayerController.SetCanSitOnBed(value: true);
		sittableBed.SetCollidersActive(value: true);
		sittableBed.SetInteractable(value: true);
	}

	public void SendNoraNotifsAfterTime()
	{
		StartCoroutine(DelayedAction());
		IEnumerator DelayedAction()
		{
			if (cabinUIManager.dockManager != null)
			{
				cabinUIManager.dockManager.OpenChatWindow(ChatWindowType.Group);
			}
			yield return new WaitForSeconds(1.2f);
			cabinUIManager.phoneUI.notifSystem.CreateNotif(0, 0, F2FLocalizedText.GetLocalizedText("ep5_chats", "VoiceMsg"));
			yield return new WaitForSeconds(2f);
			cabinUIManager.phoneUI.notifSystem.CreateNotif(0, 1, F2FLocalizedText.GetLocalizedText("ep5_chats", "SOS"));
			yield return new WaitForSeconds(1f);
			mikeRizzlerController.LookAwayFromPlayer();
			mikeRizzlerController.TakeOutPhone();
		}
	}

	private void MikeRizzlerController_OnMikeRevealComplete()
	{
		cabinPlayerController.SetIsKnockerRevealed(value: true);
		cabinPlayerController.SetMikeInRoom(value: true);
		cabinUIManager.phoneUI.allowPhone = true;
	}

	private void MikeRizzlerController_OnEndConvo()
	{
		sittableBed.SetInteractable(value: true);
		uiManager.controlsText.gameObject.SetActive(value: true);
		cabinPlayerController.EndConvoWithMike();
		playerTalking = false;
		if (playerSittingOnBed)
		{
			cabinPlayerController.SetBedSittingCamActive();
			cabinPlayerController.SetSittingCameraFOV(CameraFOVPreset.NormalInstant);
			ChangePlayerState(PlayerState.SittingAtSittablePlace);
		}
		else
		{
			ChangePlayerState(PlayerState.Normal);
		}
		bedroomDoorSydney.SetInteractable(value: true);
	}

	private void MikeRizzlerController_OnStartConvo()
	{
		sittableBed.SetInteractable(value: false);
		mikeRizzlerController.SetInteractable(value: false);
		uiManager.controlsText.gameObject.SetActive(value: false);
		playerTalking = true;
		if (uiManager.phoneUI.isPaused)
		{
			uiManager.phoneUI.ClosePhone();
		}
		if (playerSittingOnBed)
		{
			cabinPlayerController.SetCurrentCamToBedCam();
			ChangePlayerState(PlayerState.TalkingBed);
		}
		else
		{
			ChangePlayerState(PlayerState.Talking);
		}
		cabinPlayerController.StartConvoWithMike(mikeRizzlerController.mikeHead);
		bedroomDoorSydney.SetInteractable(value: false);
	}

	public override IEnumerator StartGame()
	{
		cabinUIManager.fadeCanvas.gameObject.SetActive(value: true);
		if (currentCabinSceneType == CabinSceneType.CabinScene)
		{
			PlayerPrefs.SetInt(PlayerPrefKeys.WELCOME_TO_WOODBURY, 1);
			CurrentSequence = SequenceType.DrivingToCabin;
			if (PlayerPrefs.GetInt(PlayerPrefKeys.FROM_MENU) == 1)
			{
				PlayerPrefs.SetInt(PlayerPrefKeys.FROM_MENU, 0);
				if (ChecklistAccess.IsGlobalTestingEnabled())
				{
					CabinSceneChecklist.GetInstance().DisableIntro = false;
					CabinSceneChecklist.GetInstance().MikeLosesJenga = false;
					CabinSceneChecklist.GetInstance().LockBoxInTestMode = false;
				}
				CabinSceneSequences cabinSceneSequences = (CabinSceneSequences)PlayerPrefs.GetInt(PlayerPrefKeys.START_SEQ);
				switch (cabinSceneSequences)
				{
				case CabinSceneSequences.StartAtStart:
					CurrentSequence = SequenceType.DrivingToCabin;
					break;
				case CabinSceneSequences.StartAfterShower:
					cabinUIManager.disableIntro = true;
					yield return new WaitForEndOfFrame();
					cabinUIManager.ClearControlsText();
					TestingModeStartAfterShower();
					break;
				case CabinSceneSequences.StartInKitchenAfterOvenStart:
					cabinUIManager.disableIntro = true;
					yield return new WaitForEndOfFrame();
					cabinUIManager.ClearControlsText();
					TestingModeStartInKitchenAfterOvenStart();
					break;
				case CabinSceneSequences.StartHiding:
					cabinUIManager.disableIntro = true;
					yield return new WaitForEndOfFrame();
					cabinUIManager.ClearControlsText();
					TestingModeStartHiding();
					break;
				default:
					Debug.Log("Testing Mode Called with wrong case" + cabinSceneSequences);
					break;
				}
			}
			else
			{
				CabinSceneChecklist.GetInstance()?.TestingSetup();
			}
			yield return StartCoroutine(cabinUIManager.StartCabinSceneUI());
		}
		if (currentCabinSceneType != CabinSceneType.CabinSceneDark)
		{
			yield break;
		}
		PlayerPrefs.SetInt(PlayerPrefKeys.MIDNIGHT, 1);
		CurrentSequence = SequenceType.RizzSequence;
		if (PlayerPrefs.GetInt(PlayerPrefKeys.FROM_MENU) == 1)
		{
			PlayerPrefs.SetInt(PlayerPrefKeys.FROM_MENU, 0);
			if (ChecklistAccess.IsGlobalTestingEnabled())
			{
				CabinSceneDarkChecklist.GetInstance().DisableIntro = false;
				CabinSceneDarkChecklist.GetInstance().DisableHikerIntro = false;
			}
			CabinSceneDarkSequences startSequence = (CabinSceneDarkSequences)PlayerPrefs.GetInt(PlayerPrefKeys.START_SEQ);
			Debug.Log(startSequence.ToString());
			switch (startSequence)
			{
			case CabinSceneDarkSequences.StartWhenHikerKnocks:
				cabinUIManager.disableIntro = true;
				break;
			case CabinSceneDarkSequences.StartHostAtDoor:
			case CabinSceneDarkSequences.StartHostInBasement:
				cabinUIManager.disableIntro = true;
				cabinUIManager.disableHikerIntro = true;
				break;
			}
			yield return StartCoroutine(cabinUIManager.StartCabinSceneDarkUI());
			switch (startSequence)
			{
			case CabinSceneDarkSequences.StartOnBed:
				currentPlayerState = PlayerState.SittingAtSittablePlace;
				TestingModeStartOnBed();
				break;
			case CabinSceneDarkSequences.StartWhenHikerKnocks:
				currentPlayerState = PlayerState.SittingAtSittablePlace;
				TestingModeStartWhenHikerKnocks();
				break;
			case CabinSceneDarkSequences.StartHostAtDoor:
				currentPlayerState = PlayerState.Normal;
				TestingModeStartHostAtDoor();
				break;
			case CabinSceneDarkSequences.StartHostInBasement:
				currentPlayerState = PlayerState.Normal;
				TestingModeStartHostInBasement();
				break;
			default:
				Debug.Log("Testing Mode Called with wrong case" + startSequence);
				break;
			}
			yield break;
		}
		if (ChecklistAccess.IsGlobalTestingEnabled())
		{
			if (CabinSceneDarkChecklist.GetInstance().testingMode == CabinSceneDarkChecklist.TestingMode.StartWhenHikerKnocks)
			{
				cabinUIManager.disableIntro = true;
			}
		}
		else if (CurrentSequence == SequenceType.RizzSequence)
		{
			cabinUIManager.disableIntro = false;
			TestingModeStartOnBed();
		}
		yield return StartCoroutine(cabinUIManager.StartCabinSceneDarkUI());
		CabinSceneDarkChecklist.GetInstance()?.TestingSetup();
		if (ES3.Load("flashLightPosition", Vector3.zero) != Vector3.zero)
		{
			cabinHouseManager.flashLight.transform.position = ES3.Load("flashLightPosition", Vector3.zero);
			cabinHouseManager.flashLight.transform.localEulerAngles = ES3.Load("flashLightRotation", Vector3.zero);
		}
		if (ES3.Load("flashLightInHand", defaultValue: false))
		{
			cabinHouseManager.flashLight.PutInHand();
		}
	}

	internal override void Start()
	{
		StartCharacterMovement();
		StartCoroutine(StartGame());
		cabinUIManager.dockManager.CloseChatWindow(ChatWindowType.Nora);
		cabinUIManager.dockManager.CloseChatWindow(ChatWindowType.Mike);
		if (currentCabinSceneType == CabinSceneType.CabinScene)
		{
			triggerSubTurnOffLights?.SetActionToTrigger(MikeSaysTurnOffLightOrShowSubTurnOffLight);
			cabinPlayerController.SetBasementTableInteractable(value: false);
			mikeController.SetPlayerTransform(cabinPlayerController.firstPersonController.transform);
			cabinPlayerController.SetBasementTableInteractable(value: false);
			AudioMixerManager.PlayerInside();
			AudioMixerManager.SetGameVolume(-80f);
		}
		if (currentCabinSceneType == CabinSceneType.CabinSceneDark)
		{
			AudioMixerManager.SetGameVolume(-80f);
			mikeRizzlerController.SetPlayerTransform(cabinPlayerController.firstPersonController.transform);
		}
	}

	private void MikeSaysTurnOffLightOrShowSubTurnOffLight()
	{
		if (!hasHadTurnOffLightsConvo)
		{
			mikeController.StartConvoBasementTurnOffLights();
			hasHadTurnOffLightsConvo = true;
		}
		else
		{
			mikeController.StartConvoBasementTurnOffLightsNode2();
		}
	}

	public void ChangePlayerState(PlayerState playerState)
	{
		currentPlayerState = playerState;
	}

	public void StartJengaMiniGame()
	{
		mikeController.CanStartJengaConvo = false;
		mikeController.SetInteractable(value: false);
		mikeController.LookAtPlayer();
		cabinUIManager.SetCrossHairCanvasActive(value: false);
		cabinPlayerController.FreezeSittingCamScriptsAndResetRotation(freezeCams: true, resetRotation: false);
		jengaController.enabled = true;
		cabinPlayerController.UnAssignDiningTableGetUpCallbackFromInputManager();
		(uiManager as CabinUIManager).ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "JengaSelect"));
	}

	public void ResetJengaBoardGame()
	{
		mikeController.EndConvo();
		jengaController.enabled = false;
		jengaController.SetInteractable(value: false);
		cabinPlayerController.FreezeSittingCamScriptsAndResetRotation(freezeCams: true);
		StartCoroutine(RequestFadeInAndFadeOut(0.5f, 0.5f, 1.5f, delegate
		{
			jengaController.HandleTowerReset();
			StartJengaMiniGame();
		}));
	}

	public void StartOuijaMiniGame()
	{
		StartCoroutine(DelayedAction());
		IEnumerator DelayedAction()
		{
			yield return new WaitForEndOfFrame();
			cabinPlayerController.EndConvoWithMike();
			cabinPlayerController.StartPlayingOuija();
			mikeController.SetOuijaRigActive();
			ChangePlayerState(PlayerState.PlayingOuija);
		}
	}

	public void ContinueOuijaMinigame()
	{
		StartCoroutine(DelayedAction());
		IEnumerator DelayedAction()
		{
			yield return new WaitForEndOfFrame();
			cabinPlayerController.EndConvoWithMike();
			cabinPlayerController.StartPlayingOuija(isContinuing: true);
			mikeController.SetOuijaRigActive();
			ChangePlayerState(PlayerState.PlayingOuija);
		}
	}

	public void BoxFallsAndMikeRunsUp()
	{
		cabinPlayerController.StartPlayingOuija(isContinuing: true);
		StartCoroutine(DelayedAction());
		IEnumerator DelayedAction()
		{
			yield return new WaitForSeconds(6f);
			GenericAudioReferences.instance.ouijaAmbientPiano.Stop();
			GenericAudioReferences.instance.PlayRandomMetalDragSound();
			cabinPlayerController.SetLookAtTransform(thudSoundLookatPoint);
			Debug.Log("Setting Basement End COnvo Trigger Active");
			endBasementConvoWhileRunningTrigger.SetActive(value: true);
			ouijaController.StopMoving();
			mikeController.LookAtThudSound();
			ouijaController.StopMoving();
			StartCoroutine(DelayedAction2());
		}
		IEnumerator DelayedAction2()
		{
			yield return new WaitForSeconds(2f);
			thudSoundAS.Play();
			GenericAudioReferences.instance.thudBell.Play();
			cabinPlayerController.StartConvoWithMike(mikeController.mikeHead);
			mikeController.StartConvoHolyCrapAndRunAway();
			GenericAudioReferences.instance.creepiesPiano.Play();
			CurrentSequence = SequenceType.Eating;
			cabinPlayerController.AssignBasementGetUpCallbackToInputManager();
		}
	}

	public void GetAndResetDialogEffectsInScene()
	{
		AudioSource[] componentsInChildren = dialogManager.GetComponentsInChildren<AudioSource>();
		foreach (AudioSource audioSource in componentsInChildren)
		{
			if (audioSource.name == "Button Hover AS")
			{
				dialogHoverAS = audioSource;
			}
		}
		dialogHoverAS.pitch = originalDialogHoverPitch;
		Debug.Log("!Setting pitch to " + originalDialogHoverPitch);
		if (!(dialogManager.GetComponentInChildren<DialogTextEffect>() != null))
		{
			return;
		}
		DialogTextEffect[] componentsInChildren2 = dialogManager.GetComponentsInChildren<DialogTextEffect>();
		optionTextEffectComponents = new List<DialogTextEffect>();
		DialogTextEffect[] array = componentsInChildren2;
		foreach (DialogTextEffect dialogTextEffect in array)
		{
			dialogTextEffect.CurrentEffectType = TextEffectType.None;
			if (dialogTextEffect.CurrentTextType == TextType.DialogText)
			{
				dialogTextEffectComponent = dialogTextEffect;
				dialogTextEffectComponent.CurrentEffectType = TextEffectType.None;
			}
			else
			{
				optionTextEffectComponents.Add(dialogTextEffect);
			}
			dialogTextEffect.enabled = false;
		}
		dialogTextEffectComponent.enabled = false;
	}

	public void ResetDialogEffectsInScene()
	{
		StartCoroutine(DelayedAction());
		IEnumerator DelayedAction()
		{
			yield return new WaitForEndOfFrame();
			GetAndResetDialogEffectsInScene();
		}
	}

	public void ApplyWhisperEffectOnDialogText()
	{
		StartCoroutine(DelayedAction());
		IEnumerator DelayedAction()
		{
			yield return new WaitForEndOfFrame();
			GetAndResetDialogEffectsInScene();
			dialogTextEffectComponent.CurrentEffectType = TextEffectType.Whisper;
			dialogTextEffectComponent.enabled = true;
		}
	}

	public void ApplyScreamEffectOnDialogText()
	{
		StartCoroutine(DelayedAction());
		IEnumerator DelayedAction()
		{
			yield return new WaitForEndOfFrame();
			GetAndResetDialogEffectsInScene();
			dialogTextEffectComponent.CurrentEffectType = TextEffectType.Scream;
			dialogTextEffectComponent.enabled = true;
		}
	}

	public void ApplyWhisperEffectOnOptionsText()
	{
		StartCoroutine(DelayedAction());
		IEnumerator DelayedAction()
		{
			yield return new WaitForEndOfFrame();
			GetAndResetDialogEffectsInScene();
			Debug.Log("!Setting pitch to " + 2);
			foreach (DialogTextEffect optionTextEffectComponent in optionTextEffectComponents)
			{
				optionTextEffectComponent.enabled = true;
				optionTextEffectComponent.CurrentEffectType = TextEffectType.Whisper;
			}
			dialogHoverAS.pitch = 2f;
		}
	}

	public void ShowGetUpControlsText()
	{
		(uiManager as CabinUIManager).ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "GetUp"));
	}

	public void EndOfTheMap()
	{
		SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "DrovePastCabin"));
		playerTriedCrossingMapEnd = true;
		truckController.SlowTruck();
	}

	public void ReEnterMap()
	{
		truckController.FastTruck();
	}

	private void InteractPhone()
	{
		if (!playerTalking)
		{
			phoneUIState = !phoneUIState;
			if (uiManager.phoneUI.isPaused)
			{
				cabinPlayerController.ToggleCameraInput(turnOff: false);
			}
			else
			{
				cabinPlayerController.ToggleCameraInput(turnOff: true);
			}
			cabinUIManager.InteractPhoneUI();
			if (mikeRizzlerController != null)
			{
				mikeRizzlerController.SetPlayerHasPhoneOpen(phoneUIState);
			}
		}
	}

	public void TalkToMike()
	{
		(playerController as CabinPlayerController).SetCameraForPlayerInTruck(delegate
		{
			mikeCabin.PlayerTalkToMike();
		});
		uiManager.ClearControlsText();
		if (uiManager.phoneUI.isPaused)
		{
			uiManager.phoneUI.ClosePhone();
		}
	}

	public Vector3 GetRVPosition()
	{
		return truckController.gameObject.transform.position;
	}

	public void PhoneNotOpenable()
	{
		if (phoneUIState)
		{
			InteractPhone();
		}
		(uiManager as CabinUIManager).phoneUI.allowPhone = false;
	}

	public void PhoneOpenable()
	{
		(uiManager as CabinUIManager).phoneUI.allowPhone = true;
	}

	public void LockBoxClicked()
	{
		if (currentPlayerState == PlayerState.Normal && !playerTalking)
		{
			(playerController as CabinPlayerController).MoveToLockBox();
		}
	}

	public void TakeKeys()
	{
		StartCoroutine(RequestFadeInAndFadeOut(0.5f, 0.5f, 1f, delegate
		{
			keysAS.Play();
			keysUI.SetActive(value: true);
		}));
	}

	public void MoveToTruck()
	{
		StartCoroutine(RequestFadeInAndFadeOut(0.5f, 0.5f, 1f, delegate
		{
			interactible.ChangeCamera(carCamera);
			mikeCabin.canLookAtPlayerPeriodically = true;
			mikeCabin.LookAtPlayerPeriodicallyWhileInTruck();
			if (!mikeCabin.leftTruck)
			{
				mikeCabin.HarperGotIn();
			}
			AudioMixerManager.PlayerInside();
			(playerController as CabinPlayerController).firstPersonController.gameObject.SetActive(value: false);
			(playerController as CabinPlayerController).firstPersonController.transform.eulerAngles = new Vector3((playerController as CabinPlayerController).firstPersonController.transform.eulerAngles.x, (playerController as CabinPlayerController).firstPersonController.transform.eulerAngles.y, 0f);
			(playerController as CabinPlayerController).truckPlayer.SetActive(value: true);
			inputManager.OnGetUp += MoveToFPC;
			(uiManager as CabinUIManager).ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "GetUp"));
			(playerController as CabinPlayerController).firstPersonController.transform.parent = (playerController as CabinPlayerController).truck;
			truckController.rvState = TruckController.RVState.StoppedRV;
			truckController.TurnOnCar();
			truckController.insidePointLight.SetActive(value: true);
			ChangePlayerState(PlayerState.Driving);
			(playerController as CabinPlayerController).FreezeCameraScripts(value: false);
			truckController.doorCollider.SetActive(value: false);
			sRS_ParticleSystem.followTarget = cabinPlayerController.carCamera.transform;
			cabinHouseManager.cabinSuitcase.gameObject.layer = 1;
		}));
	}

	public void MoveToFPC()
	{
		if (currentPlayerState == PlayerState.Normal || currentPlayerState == PlayerState.Talking || truckController.rvState != TruckController.RVState.StoppedRV || playerTalking)
		{
			return;
		}
		mikeCabin.canLookAtPlayerPeriodically = false;
		StartCoroutine(RequestFadeInAndFadeOut(0.5f, 0.5f, 1f, delegate
		{
			interactible.ChangeCamera(fpsCamera);
			AudioMixerManager.PlayerOutside();
			(uiManager as CabinUIManager).ClearControlsText();
			inputManager.OnGetUp -= MoveToFPC;
			if (!mikeCabin.leftTruck)
			{
				mikeCabin.HarperGotOut();
			}
			(playerController as CabinPlayerController).firstPersonController.gameObject.SetActive(value: true);
			(playerController as CabinPlayerController).firstPersonController.transform.parent = null;
			(playerController as CabinPlayerController).firstPersonController.transform.eulerAngles = new Vector3(0f, (playerController as CabinPlayerController).firstPersonController.transform.eulerAngles.y, 0f);
			(playerController as CabinPlayerController).truckPlayer.SetActive(value: false);
			truckController.rvState = TruckController.RVState.GotUp;
			if (!coldNightSub)
			{
				StartCoroutine(ColdNightSub());
			}
			truckController.TurnOffCar(carIsInParkingLot);
			truckController.insidePointLight.SetActive(value: false);
			ChangePlayerState(PlayerState.Normal);
			(playerController as CabinPlayerController).firstPersonController.PlayerStatus(status: true);
			truckController.doorCollider.SetActive(value: true);
			sRS_ParticleSystem.followTarget = playerController.cameraTransform;
			if (carIsInParkingLot)
			{
				if (!GenericAudioReferences.instance.reachedCabin.isPlaying)
				{
					GenericAudioReferences.instance.reachedCabin.Play();
				}
				cabinHouseManager.cabinSuitcase.gameObject.layer = 0;
			}
		}));
	}

	public Vector3 PlayerPosition()
	{
		return (playerController as CabinPlayerController).firstPersonController.transform.position;
	}

	public Vector3 HostPosition()
	{
		return cabinHouseManager.host.transform.position;
	}

	public void CarParkedInParkingZone()
	{
		carIsInParkingLot = true;
	}

	public void CarLeftParkingZone()
	{
		carIsInParkingLot = false;
	}

	public void CarStopped()
	{
		if (carIsInParkingLot && mikeCabin.state == MikeCabin.State.SittingInTruck && !mikeCabin.leftTruck)
		{
			(playerController as CabinPlayerController).SetCameraForPlayerInTruck(delegate
			{
				mikeCabin.PlayerTalkToMike(mikeStartsConvo: true);
			}, lookAtMike: true, carCamera: true);
			uiManager.ClearControlsText();
			if (uiManager.phoneUI.isPaused)
			{
				uiManager.phoneUI.ClosePhone();
			}
		}
	}

	public IEnumerator ColdNightSub()
	{
		yield return new WaitForSeconds(3f);
		coldNightSub = true;
		SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "ColdNight"));
	}

	public void OpenFishingBoardUI()
	{
		fishingInfoBoardOpen = true;
		(uiManager as CabinUIManager).OpenFishingInfoBoard();
		(uiManager as CabinUIManager).phoneUI.allowPhone = false;
		(uiManager as CabinUIManager).crossHairCanvas.SetActive(value: false);
		playerController.firstPersonController.enabled = false;
		inputManager.OnEsc += CloseFishingBoardUI;
		CursorModeUtility.ShowCursor();
		uiManager.ClearAndWipeLoadText();
	}

	public void CloseFishingBoardUI()
	{
		fishingInfoBoardOpen = false;
		(uiManager as CabinUIManager).CloseFishingInfoBoard();
		(uiManager as CabinUIManager).crossHairCanvas.SetActive(value: true);
		playerController.firstPersonController.enabled = true;
		inputManager.OnEsc -= CloseFishingBoardUI;
		CursorModeUtility.HideCursor();
		StartCoroutine(EnablePhone());
		IEnumerator EnablePhone()
		{
			yield return new WaitForEndOfFrame();
			(uiManager as CabinUIManager).phoneUI.allowPhone = true;
			playerController.firstPersonController.enabled = true;
		}
	}

	public void CloseFishingBoardUIForConvo()
	{
		fishingInfoBoardOpen = false;
		(uiManager as CabinUIManager).CloseFishingInfoBoard();
		(uiManager as CabinUIManager).crossHairCanvas.SetActive(value: true);
		inputManager.OnEsc -= CloseFishingBoardUI;
		CursorModeUtility.HideCursor();
		StartCoroutine(EnablePhone());
		IEnumerator EnablePhone()
		{
			yield return new WaitForEndOfFrame();
			(uiManager as CabinUIManager).phoneUI.allowPhone = true;
		}
	}

	public void MikeGetUpFromFishing()
	{
		StartCoroutine(_MikeGetUpFromFishing());
		IEnumerator _MikeGetUpFromFishing()
		{
			mikeFishing.gameObject.layer = 2;
			cabinHouseManager.cabinFridge.fridgeDoor.OpenAjar();
			cabinHouseManager.basementDoor.OpenDoorNPC();
			cabinHouseManager.cabinFridge.milkParent2.gameObject.SetActive(value: false);
			cabinHouseManager.cabinFridge.MakeYogurtPickable();
			cabinHouseManager.milkInBasement.SetActive(value: true);
			yield return new WaitForSeconds(0.5f);
			mikeFishing.gameObject.SetActive(value: false);
			mikeController.gameObject.SetActive(value: true);
			mikeController.gameObject.layer = 2;
			livingRoomTV.TurnOff();
			yield return new WaitForSeconds(0.5f);
			mikeController.GoToBucket();
			fishingDone = true;
		}
	}

	public void SetHouseAfterFishing()
	{
		cabinHouseManager.cabinFridge.fridgeDoor.OpenAjar();
		cabinHouseManager.basementDoor.OpenDoor(playSFX: false);
		if (cabinHouseManager.cabinFridge.milkParent2 != null)
		{
			cabinHouseManager.cabinFridge.milkParent2.gameObject.SetActive(value: false);
		}
	}

	public void SetMikePostEating()
	{
		StartCoroutine(_SetMikePostEating());
		IEnumerator _SetMikePostEating()
		{
			Debug.Log("SetMikePostEating");
			mikePostEating.gameObject.SetActive(value: true);
			mikeController.gameObject.SetActive(value: false);
			yield return new WaitForSeconds(0.5f);
			MikPostEatingTalks();
			cabinHouseManager.ColliderStairsAfterEating.SetActive(value: true);
			cabinPlayerController.currentHoldingObject.GetComponent<CasseroleIngredientObject>().IngredientType = CasseroleRecipie.StackablePlate;
			cabinPlayerController.sink.gameObject.SetActive(value: true);
		}
	}

	public void MikeTalks()
	{
		if (mikeCabin.gameObject.activeSelf && !playerTalking)
		{
			(playerController as CabinPlayerController).firstPersonController.enabled = false;
			playerTalking = true;
			(playerController as CabinPlayerController).SetCameraForPlayerInTruck(delegate
			{
				mikeCabin.PlayerTalkToMike(mikeStartsConvo: true);
			}, lookAtMike: true);
			uiManager.ClearControlsText();
			if (fishingInfoBoardOpen)
			{
				CloseFishingBoardUIForConvo();
			}
			if (uiManager.phoneUI.isPaused)
			{
				uiManager.phoneUI.ClosePhone();
			}
		}
	}

	public void HostTalks()
	{
		(playerController as CabinPlayerController).firstPersonController.enabled = false;
		playerTalking = true;
		cabinHouseManager.host.PlayerTalkToHost();
		(playerController as CabinPlayerController).SetCameraForPlayerConversation();
		uiManager.ClearControlsText();
		if (fishingInfoBoardOpen)
		{
			CloseFishingBoardUIForConvo();
		}
		if (uiManager.phoneUI.isPaused)
		{
			uiManager.phoneUI.ClosePhone();
		}
	}

	public void MikeFishingTalks()
	{
		if (mikeFishing.gameObject.activeSelf && !playerTalking)
		{
			(playerController as CabinPlayerController).firstPersonController.enabled = false;
			playerTalking = true;
			(playerController as CabinPlayerController).SetCameraForPlayerInTruck(delegate
			{
				mikeFishing.PlayerTalkToMike(mikeStartsConvo: true);
			}, lookAtMike: true);
			uiManager.ClearControlsText();
			if (fishingInfoBoardOpen)
			{
				CloseFishingBoardUIForConvo();
			}
			if (uiManager.phoneUI.isPaused)
			{
				uiManager.phoneUI.ClosePhone();
			}
			if (isSittingFishing)
			{
				(playerController as CabinPlayerController).lockCameraMovement.enabled = false;
				inputManager.OnGetUp -= (playerController as CabinPlayerController).MoveBackFromFishingSitting;
			}
		}
	}

	public void MikPostEatingTalks()
	{
		if (mikePostEating.gameObject.activeSelf && !playerTalking)
		{
			(playerController as CabinPlayerController).firstPersonController.enabled = false;
			playerTalking = true;
			(playerController as CabinPlayerController).SetCameraForPlayerInTruck(delegate
			{
				mikePostEating.PlayerTalkToMike();
			}, lookAtMike: true);
			uiManager.ClearControlsText();
			if (uiManager.phoneUI.isPaused)
			{
				uiManager.phoneUI.ClosePhone();
			}
			if (fishingInfoBoardOpen)
			{
				CloseFishingBoardUIForConvo();
			}
		}
	}

	public void MikeAfterHidingTalks()
	{
		if (mikeAfterHiding.gameObject.activeSelf && !uiManager.inCoversation)
		{
			(playerController as CabinPlayerController).firstPersonController.enabled = false;
			playerTalking = true;
			(playerController as CabinPlayerController).SetCameraForPlayerInTruck(delegate
			{
				mikeAfterHiding.PlayerTalkToMike();
			}, lookAtMike: true);
			uiManager.ClearControlsText();
			if (uiManager.phoneUI.isPaused)
			{
				uiManager.phoneUI.ClosePhone();
			}
			if (fishingInfoBoardOpen)
			{
				CloseFishingBoardUIForConvo();
			}
		}
	}

	public void HostFixingSinkTalks()
	{
		if (!uiManager.inCoversation)
		{
			(playerController as CabinPlayerController).firstPersonController.enabled = false;
			playerTalking = true;
			hostFixingSink.PlayerTalkToHost();
			(playerController as CabinPlayerController).SetCameraForPlayerConversation();
			uiManager.ClearControlsText();
			if (fishingInfoBoardOpen)
			{
				CloseFishingBoardUIForConvo();
			}
			if (uiManager.phoneUI.isPaused)
			{
				uiManager.phoneUI.ClosePhone();
			}
		}
	}

	public void HostEndTalks()
	{
		if (!playerTalking && !uiManager.inCoversation)
		{
			(playerController as CabinPlayerController).firstPersonController.enabled = false;
			playerTalking = true;
			hostEndGame.PlayerTalkToHost();
			(playerController as CabinPlayerController).SetCameraForPlayerConversation();
			uiManager.ClearControlsText();
			if (uiManager.phoneUI.isPaused)
			{
				uiManager.phoneUI.ClosePhone();
			}
		}
	}

	public void NoraTalks()
	{
		if (!playerTalking)
		{
			(playerController as CabinPlayerController).firstPersonController.enabled = false;
			playerTalking = true;
			noraEnd.PlayerTalkToNora();
			(playerController as CabinPlayerController).SetCameraForPlayerConversation();
			uiManager.ClearControlsText();
			if (uiManager.phoneUI.isPaused)
			{
				uiManager.phoneUI.ClosePhone();
			}
		}
	}

	public void ShowRandomCrashSub()
	{
		StartCoroutine(ShowRandomSubAfterTime());
		static IEnumerator ShowRandomSubAfterTime()
		{
			yield return new WaitForSeconds(2f);
			switch (UnityEngine.Random.Range(0, 3))
			{
			case 0:
				SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "TruckCollide1"));
				break;
			case 1:
				SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "TruckCollide2"));
				break;
			case 2:
				SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "TruckCollide3"));
				break;
			}
		}
	}

	public void ConvoOver()
	{
		playerTalking = false;
		uiManager.inCoversation = false;
		mikeCabin.EndConvo(delegate
		{
		});
		if (!isSittingFishing)
		{
			(playerController as CabinPlayerController).ResumeCameraControl();
			(playerController as CabinPlayerController).firstPersonController.enabled = true;
			(playerController as CabinPlayerController).firstPersonController.canZoomIn = true;
			if (currentPlayerState == PlayerState.Talking)
			{
				ChangePlayerState(PlayerState.Normal);
			}
			else
			{
				ChangePlayerState(PlayerState.Driving);
			}
		}
		else
		{
			(playerController as CabinPlayerController).lockCameraMovement.enabled = true;
			(playerController as CabinPlayerController).lockCameraMovement.disableFov = false;
			ChangePlayerState(PlayerState.FishingSitting);
		}
		HideCursor();
	}

	public void ConvoOverHost()
	{
		playerTalking = false;
		uiManager.inCoversation = false;
		mikeCabin.EndConvo(delegate
		{
		});
		cabinHouseManager.host.EndConvo(delegate
		{
		});
		(playerController as CabinPlayerController).ResumeCameraControl();
		(playerController as CabinPlayerController).firstPersonController.enabled = true;
		(playerController as CabinPlayerController).firstPersonController.canZoomIn = true;
		if (currentPlayerState == PlayerState.Talking)
		{
			ChangePlayerState(PlayerState.Normal);
		}
		else
		{
			ChangePlayerState(PlayerState.Driving);
		}
		HideCursor();
	}

	public void ConvoOverFishing()
	{
		playerTalking = false;
		uiManager.inCoversation = false;
		mikeFishing.EndConvo(delegate
		{
		});
		if (!isSittingFishing)
		{
			(playerController as CabinPlayerController).ResumeCameraControl();
			(playerController as CabinPlayerController).firstPersonController.enabled = true;
			(playerController as CabinPlayerController).firstPersonController.canZoomIn = true;
			if (currentPlayerState == PlayerState.Talking)
			{
				ChangePlayerState(PlayerState.Normal);
			}
			else
			{
				ChangePlayerState(PlayerState.Driving);
			}
		}
		else
		{
			(playerController as CabinPlayerController).lockCameraMovement.enabled = true;
			(playerController as CabinPlayerController).lockCameraMovement.disableFov = false;
			ChangePlayerState(PlayerState.FishingSitting);
			inputManager.OnGetUp += (playerController as CabinPlayerController).MoveBackFromFishingSitting;
			playerController.firstPersonController.playerCamera.fieldOfView = 60f;
		}
		(playerController as CabinPlayerController).inFishCaught = false;
		HideCursor();
	}

	public void ConvoOverPostEating()
	{
		playerTalking = false;
		uiManager.inCoversation = false;
		mikePostEating.EndConvo(delegate
		{
		});
		(playerController as CabinPlayerController).ResumeCameraControl();
		if (currentPlayerState == PlayerState.TalkingCouch)
		{
			(playerController as CabinPlayerController).sittingCam.enabled = true;
			(playerController as CabinPlayerController).sittingCamFov.enabled = true;
			(playerController as CabinPlayerController).sittingCamFov.dontZoom = false;
			ChangePlayerState(PlayerState.SittingAtSittablePlace);
		}
		else
		{
			(playerController as CabinPlayerController).firstPersonController.enabled = true;
			ChangePlayerState(PlayerState.Normal);
		}
		HideCursor();
	}

	public void ConvoOverAfterHiding()
	{
		playerTalking = false;
		uiManager.inCoversation = false;
		mikeAfterHiding.EndConvo(delegate
		{
		});
		(playerController as CabinPlayerController).ResumeCameraControl();
		if (currentPlayerState == PlayerState.TalkingCouch)
		{
			(playerController as CabinPlayerController).sittingCam.enabled = true;
			(playerController as CabinPlayerController).sittingCamFov.enabled = true;
			(playerController as CabinPlayerController).sittingCamFov.dontZoom = false;
			ChangePlayerState(PlayerState.SittingAtSittablePlace);
		}
		else
		{
			(playerController as CabinPlayerController).firstPersonController.enabled = true;
			ChangePlayerState(PlayerState.Normal);
		}
		HideCursor();
	}

	public void ConvoOverHostFixingSink()
	{
		playerTalking = false;
		uiManager.inCoversation = false;
		mikeAfterHiding.EndConvo(delegate
		{
		});
		hostFixingSink.EndConvo(delegate
		{
		});
		(playerController as CabinPlayerController).ResumeCameraControl();
		(playerController as CabinPlayerController).firstPersonController.enabled = true;
		if (currentPlayerState == PlayerState.Talking)
		{
			ChangePlayerState(PlayerState.Normal);
		}
		else
		{
			ChangePlayerState(PlayerState.Driving);
		}
		HideCursor();
	}

	public void ConvoOverHostEnd()
	{
		playerTalking = false;
		uiManager.inCoversation = false;
		hostEndGame.EndConvo(delegate
		{
		});
		(playerController as CabinPlayerController).ResumeCameraControl();
		(playerController as CabinPlayerController).firstPersonController.enabled = true;
		ChangePlayerState(PlayerState.Normal);
		HideCursor();
	}

	public void ConvoOverNora()
	{
		playerTalking = false;
		uiManager.inCoversation = false;
		noraEnd.EndConvo(delegate
		{
		});
		(playerController as CabinPlayerController).ResumeCameraControl();
		(playerController as CabinPlayerController).firstPersonController.enabled = true;
		ChangePlayerState(PlayerState.Normal);
		HideCursor();
	}

	public void HideCursor()
	{
		StartCoroutine(_HideCursor());
		static IEnumerator _HideCursor()
		{
			yield return new WaitForEndOfFrame();
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked;
		}
	}

	public void TriggerMikeTexts()
	{
		StartCoroutine(_TriggerMikeTexts());
		IEnumerator _TriggerMikeTexts()
		{
			if (cabinUIManager.dockManager != null)
			{
				cabinUIManager.dockManager.OpenChatWindow(ChatWindowType.Mike);
			}
			cabinHouseManager.ColliderStairsAfterEating.SetActive(value: false);
			cabinHouseManager.ColliderStairsTexting.SetActive(value: true);
			yield return new WaitForSeconds(10f);
			(uiManager as CabinUIManager).phoneUI.notifSystem.CreateNotif(1, 0);
			UIManager.OpenPhone = (Action)Delegate.Combine(UIManager.OpenPhone, new Action(SendMikeTexts1));
			cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "Esc"));
		}
	}

	private void SendMikeTexts1()
	{
		StartCoroutine(_SendMessageMike());
		IEnumerator _SendMessageMike()
		{
			cabinUIManager.ClearControlsText();
			if (cabinUIManager.dockManager != null)
			{
				cabinUIManager.dockManager.OpenChatWindow(ChatWindowType.Mike);
			}
			cabinHouseManager.ColliderStairsTexting.SetActive(value: false);
			cabinHouseManager.triggerMikeTexts2.SetActive(value: true);
			cabinHouseManager.mikeBedroomJumpscare = true;
			cabinHouseManager.mikeBedroomDoor.OnDoorOpened += MikeBedroomDoorOpened;
			cabinHouseManager.mikeBedroomDoor.affectsAudioMixer = false;
			UIManager.OpenPhone = (Action)Delegate.Remove(UIManager.OpenPhone, new Action(SendMikeTexts1));
			yield return new WaitForSeconds(0.5f);
			(uiManager as CabinUIManager).phoneUI.notifSystem.CreateNotif(1, 1);
			yield return new WaitForSeconds(1.5f);
			GenericAudioReferences.instance.SomeonesUnderTheBed.Play();
			yield return new WaitForSeconds(0.5f);
			(uiManager as CabinUIManager).phoneUI.notifSystem.CreateNotif(1, 2);
		}
	}

	public void SendMikeTexts2()
	{
		StartCoroutine(_SendMessageMike());
		IEnumerator _SendMessageMike()
		{
			if (cabinUIManager.dockManager != null)
			{
				cabinUIManager.dockManager.OpenChatWindow(ChatWindowType.Mike);
			}
			(uiManager as CabinUIManager).phoneUI.notifSystem.CreateNotif(1, 3, F2FLocalizedText.GetLocalizedText("ep5_chats", "DontSound"));
			GenericAudioReferences.instance.dontMakeSounds.Play();
			yield return new WaitForSeconds(1f);
			(uiManager as CabinUIManager).phoneUI.notifSystem.CreateNotif(1, 4, F2FLocalizedText.GetLocalizedText("ep5_chats", "WalkSlow"));
		}
	}

	public void MikeBedroomDoorOpened()
	{
		cabinHouseManager.mikeBedroomDoor.OnDoorOpened -= MikeBedroomDoorOpened;
		GenericAudioReferences.instance.SomeonesUnderTheBed.Stop();
		GenericAudioReferences.instance.dontMakeSounds.Stop();
		audioMixer.SetFloat("AmbienceVolume", -80f);
		cabinHouseManager.cabinFridge.SetFridgeVolumeToZero();
	}

	public void SetMikeForBedroomJumpscare()
	{
		mikePostEating.transform.position = mikeBedroomJumpscareTransform.position;
		mikePostEating.transform.localEulerAngles = mikeBedroomJumpscareTransform.localEulerAngles;
		mikePostEating.state = MikePostEating.State.Idle;
		mikePostEating.gameObject.SetActive(value: true);
		mikePostEating.LightingTurnOff();
		mikePostEating.gameObject.layer = 1;
	}

	public void MoveToAttic()
	{
		if (DeathManager.instance != null)
		{
			if (DeathManager.instance.hostIsHaunting || DeathManager.instance.playerCaught)
			{
				return;
			}
			if (DeathManager.instance.hostIsChasing)
			{
				EndCall();
				StartCoroutine(FadeAudioSource.FadeToZeroAndReset(GenericAudioReferences.instance.chase2, 1f));
			}
		}
		atticDoor.openDoorAS.Play();
		(playerController as CabinPlayerController).firstPersonController.enabled = false;
		StartCoroutine(RequestFadeInAndFadeOut(0.5f, 0.5f, 2f, delegate
		{
			atticDoor.SetDoorMaterial(toFade: true);
			atticParent.SetActive(value: true);
			(playerController as CabinPlayerController).firstPersonController.gameObject.SetActive(value: false);
			atticDoor.playerPositionOnStool = (playerController as CabinPlayerController).firstPersonController.transform.position;
			(playerController as CabinPlayerController).firstPersonController.transform.position = playerAttic.position;
			(playerController as CabinPlayerController).firstPersonController.transform.localEulerAngles = playerAttic.localEulerAngles;
			(playerController as CabinPlayerController).firstPersonController.gameObject.SetActive(value: true);
			(playerController as CabinPlayerController).firstPersonController.enabled = true;
			atticDoor.gameObject.layer = 0;
			audioMixer.GetFloat("AmbienceVolume", out ambBeforeAttic);
			AudioMixerManager.PlayerDeepInside();
			if ((uiManager as CabinUIManager).micUI.activeSelf)
			{
				micWasOn = true;
				(uiManager as CabinUIManager).micUI.SetActive(value: false);
				cabinHouseManager.closetDoor1.playerHidingInside = false;
				cabinHouseManager.closetDoor2.playerHidingInside = false;
			}
			else
			{
				micWasOn = false;
			}
			cabinHouseManager.usptairsTvVolumeControl.SetVolumeOutsideRoom();
			closetLight.SetActive(value: true);
			playerInAttic = true;
			if (CabinPosition.instance != null)
			{
				CabinPosition.instance.playerArea = CabinPosition.Area.Attic;
				CabinPosition.instance.playerGeneralPosition = CabinPosition.GeneralPosition.Attic;
			}
		}));
		if (mikePostEating != null && mikePostEating.waitOutsideCloset && mikePostEating.state != MikePostEating.State.Counting2)
		{
			mikePostEating.PlayerWentToAttic();
		}
	}

	public void MoveBackFromAttic()
	{
		atticDoor.openDoorAS.Play();
		(playerController as CabinPlayerController).firstPersonController.enabled = false;
		StartCoroutine(RequestFadeInAndFadeOut(0.5f, 0.5f, 2f, delegate
		{
			atticDoor.SetDoorMaterial(toFade: false);
			(playerController as CabinPlayerController).firstPersonController.gameObject.SetActive(value: false);
			(playerController as CabinPlayerController).firstPersonController.transform.position = atticDoor.playerPositionOnStool;
			(playerController as CabinPlayerController).firstPersonController.enabled = true;
			(playerController as CabinPlayerController).firstPersonController.gameObject.SetActive(value: true);
			atticDoor.gameObject.layer = 0;
			audioMixer.SetFloat("AmbienceVolume", ambBeforeAttic);
			if (micWasOn && !cabinHouseManager.closetDoor1.isOpen)
			{
				(uiManager as CabinUIManager).micUI.SetActive(value: true);
				cabinHouseManager.closetDoor1.playerHidingInside = true;
				cabinHouseManager.closetDoor2.playerHidingInside = true;
			}
			closetLight.SetActive(value: false);
			playerInAttic = false;
			if (CabinPosition.instance != null)
			{
				CabinPosition.instance.playerArea = CabinPosition.Area.Closet;
				CabinPosition.instance.playerGeneralPosition = CabinPosition.GeneralPosition.Upstairs;
			}
		}));
		if (mikePostEating != null && mikePostEating.waitOutsideCloset && mikePostEating.state != MikePostEating.State.Counting2)
		{
			mikePostEating.PlayerCameBackForAttic();
		}
	}

	public void MakeHikerWalk()
	{
		cabinHiker.gameObject.SetActive(value: true);
		cabinHiker.WalkAcrossBridge();
	}

	public void MicHidingSetup()
	{
		microphone.OnShout += mikePostEating.PlayerDetectedUnderstairs;
	}

	public void MicHidingDisable()
	{
		microphone.OnShout -= mikePostEating.PlayerDetectedUnderstairs;
	}

	public void ShowCountingControlText()
	{
		StartCoroutine(_ShowCountingControlText());
		IEnumerator _ShowCountingControlText()
		{
			yield return new WaitForSeconds(3f);
			if (!playerTalking)
			{
				cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "Counting"));
				inputManager.OnGetUp += StartPlayerCounting;
			}
		}
	}

	public void ShowCountingControlTextShort()
	{
		StartCoroutine(_ShowCountingControlText());
		IEnumerator _ShowCountingControlText()
		{
			yield return new WaitForSeconds(1f);
			if (!playerTalking)
			{
				cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "Counting"));
				inputManager.OnGetUp += StartPlayerCounting;
			}
		}
	}

	public void StartPlayerCounting()
	{
		if (uiManager.phoneUI.isPaused)
		{
			uiManager.phoneUI.ClosePhone();
		}
		uiManager.phoneUI.allowPhone = false;
		StartCoroutine(_TriggerPlayerCounting());
		IEnumerator _TriggerPlayerCounting()
		{
			inputManager.OnGetUp -= StartPlayerCounting;
			cabinUIManager.ClearControlsText();
			SubTextManager.GetInstance().ShowSubText("");
			playerController.firstPersonController.enabled = false;
			playerTalking = true;
			uiManager.inCoversation = true;
			yield return new WaitForSeconds(0.5f);
			(uiManager as CabinUIManager).eyesUI.CloseEyes();
			(uiManager as CabinUIManager).crossHairCanvas.SetActive(value: false);
			yield return new WaitForSeconds(3f);
			uiManager.phoneUI.ClosePhone();
			if (uiManager.phoneUI.isPaused)
			{
				uiManager.phoneUI.ClosePhoneFromConversation();
			}
			(playerController as CabinPlayerController).dialogueCamera.gameObject.SetActive(value: true);
			DialogueLua.SetVariable("HideSeekConvoIndex", 7);
			DialogueManager.StartConversation("Hide n Seek");
		}
	}

	public void OpenEyes()
	{
		if (mikePostEating.hidingSeq1)
		{
			mikePostEating.TeleportToCloset();
		}
		else
		{
			mikePostEating.gameObject.SetActive(value: false);
			hostFixingSink.gameObject.SetActive(value: true);
			cabinHouseManager.upstairsToiletDoor.OnDoorOpenedInstantly += PlayerFoundHostToilet;
		}
		StartCoroutine(_OpenEyes());
		IEnumerator _EndTalking()
		{
			yield return new WaitForEndOfFrame();
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
			uiManager.phoneUI.allowPhone = true;
		}
		IEnumerator _OpenEyes()
		{
			(playerController as CabinPlayerController).dialogueCamera.gameObject.SetActive(value: false);
			yield return new WaitForSeconds(1f);
			GenericAudioReferences.instance.hideAndSeek2.Play();
			(uiManager as CabinUIManager).eyesUI.OpenEyes();
			(uiManager as CabinUIManager).crossHairCanvas.SetActive(value: true);
			yield return new WaitForSeconds(1f);
			playerController.firstPersonController.enabled = true;
			cabinHouseManager.understairsDoor.blockStairs.SetActive(value: false);
			cabinHouseManager.triggerMikeHiding.SetActive(value: false);
			playerTalking = false;
			uiManager.inCoversation = false;
			StartCoroutine(_EndTalking());
		}
	}

	public void PlayerFoundHostToilet()
	{
		cabinHouseManager.upstairsToiletDoor.OnDoorOpenedInstantly -= PlayerFoundHostToilet;
		StartCoroutine(_PlayerFoundHostToilet());
		IEnumerator _PlayerFoundHostToilet()
		{
			yield return new WaitForSeconds(0.5f);
			StartCoroutine((playerController as CabinPlayerController).cameraShake.ShakeWithZ(0.5f, 0.025f));
			GenericAudioReferences.instance.hideAndSeek2.Stop();
			GenericAudioReferences.instance.womanScream.Play();
			GenericAudioReferences.instance.hostInToilet.Play();
			hostFixingSink.FoundByPlayer();
			yield return new WaitForSeconds(0.2f);
			mikeAfterHiding.gameObject.SetActive(value: true);
			mikeAfterHiding.RunToPlayer();
			yield return new WaitForSeconds(2.7f);
			(playerController as CabinPlayerController).LookAtMikeAfterHiding();
		}
	}

	public void LoadSceneAsyncAfterFade(string sceneName)
	{
		StartCoroutine(_EndScene());
		IEnumerator _EndScene()
		{
			StartCoroutine(RequestFadeOut(1f));
			StartCoroutine(FadeMixerGroup.StartFade(audioMixer, "GameVolume", 1f, -80f));
			SubTextManager.GetInstance().HideSubText();
			yield return new WaitForSeconds(2f);
			SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "WeDecidedToGoBack"));
			yield return new WaitForSeconds(6f);
			LoadSceneAsync(sceneName);
		}
	}

	public void EndScene()
	{
		cabinUIManager.ForceClosePhone();
		StartCoroutine(_EndScene());
		IEnumerator _EndScene()
		{
			yield return new WaitForSeconds(2f);
			StartCoroutine(RequestFadeOut(3f));
			StartCoroutine(FadeMixerGroup.StartFade(audioMixer, "GameVolume", 3f, -80f));
			yield return new WaitForSeconds(3f);
			SaveObjectPositions();
			LoadSceneAsync("CabinSceneDark");
		}
	}

	public void SaveObjectPositions()
	{
		ES3.Save("flashLightPosition", cabinHouseManager.flashLight.transform.position);
		ES3.Save("flashLightRotation", cabinHouseManager.flashLight.transform.localEulerAngles);
		bool value = false;
		if (cabinPlayerController.currentHoldingObjectLeft != null && cabinPlayerController.currentHoldingObjectLeft == cabinHouseManager.flashLight)
		{
			value = true;
		}
		ES3.Save("flashLightInHand", value);
	}

	public void DisableSitting()
	{
		sittingCouch.layer = 1;
		GameObject[] array = sittingDiningTable;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].layer = 1;
		}
	}

	public void StopSnoring()
	{
		GameObject[] array = snoringTriggers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
		GenericAudioReferences.instance.snoring.Stop();
	}

	public void SubForUnsafeDoor()
	{
		SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "StillNotSafeOpenDoor"));
	}

	public void MakePorchAreaAccessible()
	{
		subUnsafeDoorTrigger.gameObject.SetActive(value: false);
		GameObject[] array = porchBlockers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
		mainDoor.SetInteractable(value: true);
	}

	public void MakeBackyardAccessible()
	{
		GameObject[] array = backyardBlockers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
	}

	public void SendRickTexts1()
	{
		StartCoroutine(_SendRickTexts());
		IEnumerator _SendRickTexts()
		{
			hostEndGame.state = HostEndGame.State.StareAtPlayer;
			yield return new WaitForSeconds(5f);
			(uiManager as CabinUIManager).phoneUI.notifSystem.CreateNotifRentACabin(1, 3);
			hostEndGame.LookAtPlayerDelay(0.25f);
			if (uiManager.phoneUI.isPaused)
			{
				if (cabinUIManager.dockManager != null)
				{
					cabinUIManager.dockManager.OpenRickWindow();
				}
				SendRickTexts2();
			}
			else
			{
				UIManager.OpenPhone = (Action)Delegate.Combine(UIManager.OpenPhone, new Action(SendRickTexts2));
				cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "Esc"));
			}
		}
	}

	public void SendRickTexts2()
	{
		StartCoroutine(_SendRickTexts());
		IEnumerator _SendRickTexts()
		{
			StartCoroutine(FadeAudioSource.FadeToZeroAndReset(GenericAudioReferences.instance.ambTextSent, 1f));
			UIManager.OpenPhone = (Action)Delegate.Remove(UIManager.OpenPhone, new Action(SendRickTexts2));
			if (cabinUIManager.dockManager != null)
			{
				cabinUIManager.dockManager.OpenRickWindow();
			}
			cabinUIManager.ClearControlsText();
			GenericAudioReferences.instance.PlayRealisation();
			yield return new WaitForSeconds(0.8f);
			(uiManager as CabinUIManager).phoneUI.notifSystem.CreateNotifRentACabin(1, 4);
			yield return new WaitForSeconds(0.5f);
			(uiManager as CabinUIManager).phoneUI.notifSystem.CreateNotifRentACabin(1, 5);
			yield return new WaitForSeconds(1f);
			(uiManager as CabinUIManager).phoneUI.notifSystem.CreateNotifRentACabin(1, 6);
			UIManager.OpenPhone = (Action)Delegate.Combine(UIManager.OpenPhone, new Action(HostWalkToPlayer));
		}
	}

	public void HostWalkToPlayer()
	{
		UIManager.OpenPhone = (Action)Delegate.Remove(UIManager.OpenPhone, new Action(HostWalkToPlayer));
		hostEndGame.GoToPlayer2();
	}

	public void LookAtHost(float waitTime = 1.5f, float lerpSpeed = 8f)
	{
		StartCoroutine(_LookAtHost(waitTime));
		IEnumerator _LookAtHost(float seconds)
		{
			currentPlayerState = PlayerState.LookAtObject;
			(playerController as CabinPlayerController).lerpSpeedLookAtObject = lerpSpeed;
			(playerController as CabinPlayerController).lookAt = hostEndGame.playerLookHere;
			(playerController as CabinPlayerController).firstPersonController.enabled = false;
			yield return new WaitForSeconds(seconds);
			currentPlayerState = PlayerState.Normal;
			(playerController as CabinPlayerController).firstPersonController.enabled = true;
			(playerController as CabinPlayerController).lerpSpeedLookAtObject = 3f;
		}
	}

	public void LookAtBasementDoor(float waitTime, float lerpSpeed)
	{
		StartCoroutine(_LookAtHost(waitTime));
		IEnumerator _LookAtHost(float seconds)
		{
			currentPlayerState = PlayerState.LookAtObject;
			(playerController as CabinPlayerController).lerpSpeedLookAtObject = lerpSpeed;
			(playerController as CabinPlayerController).lookAt = basementDoorPoint;
			(playerController as CabinPlayerController).firstPersonController.enabled = false;
			yield return new WaitForSeconds(seconds);
			currentPlayerState = PlayerState.Normal;
			(playerController as CabinPlayerController).firstPersonController.enabled = true;
			(playerController as CabinPlayerController).lerpSpeedLookAtObject = 3f;
			Debug.Log("LookAtBasementDoor Done");
		}
	}

	public void LookAtObject(Transform lookAt, float waitTime = 1.5f, float lerpSpeed = 8f)
	{
		StartCoroutine(_LookAtHost(waitTime));
		IEnumerator _LookAtHost(float seconds)
		{
			currentPlayerState = PlayerState.LookAtObject;
			(playerController as CabinPlayerController).lerpSpeedLookAtObject = lerpSpeed;
			(playerController as CabinPlayerController).lookAt = lookAt;
			(playerController as CabinPlayerController).firstPersonController.enabled = false;
			yield return new WaitForSeconds(seconds);
			currentPlayerState = PlayerState.Normal;
			(playerController as CabinPlayerController).firstPersonController.enabled = true;
			(playerController as CabinPlayerController).lerpSpeedLookAtObject = 3f;
		}
	}

	public void MicHidingSetupEndgameBasement()
	{
		microphone.OnShout += hostEndGame.MicShouted;
	}

	public void MicHidingDisableEndgameBasement()
	{
		microphone.OnShout -= hostEndGame.MicShouted;
	}

	public void MicHidingSetupEndgameCloset()
	{
		microphone.OnShout += hostEndGame.MicShouted;
	}

	public void MicHidingDisableEndgameCloset()
	{
		microphone.OnShout -= hostEndGame.MicShouted;
	}

	public void SendNoraTexts()
	{
		StartCoroutine(_SendNoraTexts());
		IEnumerator _SendNoraTexts()
		{
			EndCall();
			if (cabinUIManager.dockManager != null)
			{
				cabinUIManager.dockManager.OpenChatWindow(ChatWindowType.Nora);
			}
			yield return new WaitForSeconds(0.5f);
			(uiManager as CabinUIManager).phoneUI.notifSystem.CreateNotif(2, 0, F2FLocalizedText.GetLocalizedText("ep5_chats", "SleepyHead"));
			yield return new WaitForSeconds(1f);
			(uiManager as CabinUIManager).phoneUI.notifSystem.CreateNotif(2, 1, F2FLocalizedText.GetLocalizedText("ep5_chats", "AlmostThere"));
		}
	}

	public void StartCall()
	{
		if (uiManager.phoneUI.isPaused)
		{
			uiManager.phoneUI.ClosePhoneFromConversation();
			if (!uiManager.inCoversation)
			{
				playerController.firstPersonController.enabled = true;
				StartCoroutine(HideAndLockCursor());
			}
		}
		uiManager.phoneUI.allowPhone = false;
		uiManager.phoneUI.CallingLeanPulse();
		StartCoroutine(HandlePhoneControl());
	}

	private IEnumerator HandlePhoneControl()
	{
		if (isPhoneRinging)
		{
			yield break;
		}
		float timeOutTimer = 0f;
		isPhoneRinging = true;
		while (isPhoneRinging && timeOutTimer < automaticCallHangUpTime)
		{
			if (!uiManager.inCoversation && isPhoneRinging)
			{
				uiManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "EscToHangUp"));
			}
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				hangUpAS.Play();
				EndCall();
				yield break;
			}
			timeOutTimer += Time.deltaTime;
			yield return null;
		}
		if (isPhoneRinging)
		{
			EndCall();
		}
	}

	public void EndCall()
	{
		isPhoneRinging = false;
		endCallCabin.HangUpPhone();
		uiManager.ClearControlsText();
		if (!uiManager.inCoversation)
		{
			uiManager.phoneUI.allowPhone = true;
			playerController.firstPersonController.enabled = true;
		}
	}

	public void EndCallDontAffectPlayer()
	{
		isPhoneRinging = false;
		endCallCabin.HangUpPhone();
		uiManager.ClearControlsText();
		uiManager.phoneUI.allowPhone = true;
	}

	private IEnumerator HideAndLockCursor()
	{
		yield return new WaitForEndOfFrame();
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	public void SendRickTexts3()
	{
		StartCoroutine(_SendRickTexts());
		IEnumerator _SendRickTexts()
		{
			StartCoroutine(FadeAudioSource.FadeToZeroAndReset(GenericAudioReferences.instance.ambTextSent, 1f));
			if (cabinUIManager.dockManager != null)
			{
				cabinUIManager.dockManager.OpenRickWindow();
			}
			(uiManager as CabinUIManager).phoneUI.notifSystem.CreateNotifRentACabin(1, 7, F2FLocalizedText.GetLocalizedText("ep5_chats", "AreYouAlright"));
			yield return new WaitForSeconds(1.5f);
			(uiManager as CabinUIManager).phoneUI.notifSystem.CreateNotifRentACabin(1, 8, F2FLocalizedText.GetLocalizedText("ep5_chats", "Hello"));
			yield return new WaitForSeconds(0.5f);
			(uiManager as CabinUIManager).phoneUI.notifSystem.CreateNotifRentACabin(1, 9, F2FLocalizedText.GetLocalizedText("ep5_chats", "911"));
		}
	}

	public void DisableSprinting()
	{
		playerController.firstPersonController.canSprint = false;
		playerController.firstPersonController.GroundTypes[0].walkSpeed = 0.6f;
		playerController.firstPersonController.GroundTypes[1].walkSpeed = 0.6f;
		playerController.firstPersonController.GroundTypes[2].walkSpeed = 0.6f;
		playerController.firstPersonController.GroundTypes[3].walkSpeed = 0.6f;
		playerController.firstPersonController.GroundTypes[4].walkSpeed = 0.6f;
		playerController.firstPersonController.GroundTypes[5].walkSpeed = 0.6f;
		playerController.firstPersonController.GroundTypes[6].walkSpeed = 0.6f;
		playerController.firstPersonController.walkSpeed = 0.6f;
		playerController.firstPersonController.crouchSpeed = 0.6f;
		playerController.firstPersonController.crouchBobSpeed = 0f;
		playerController.firstPersonController.crouchBobAmount = 0f;
	}

	public void PlayEndSceneMusic()
	{
		DontDestroyCreditsMusic.Instance.audioSource.volume = 0f;
		DontDestroyCreditsMusic.Instance.audioSource.Play();
		StartCoroutine(FadeAudioSource.StartFade(DontDestroyCreditsMusic.Instance.audioSource, 10f, 1f));
		StartCoroutine(FadeMixerGroup.StartFade(audioMixer, "GameVolume", 4f, -80f));
	}

	public void SubForBrokenHandle()
	{
		SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "ManBrokeHandle"));
	}

	public void EndDarkScene()
	{
		StartCoroutine(_EndScene());
		IEnumerator _EndScene()
		{
			yield return new WaitForSeconds(3f);
			StartCoroutine((uiManager as CabinUIManager).FadeOutBlackScreen2(5f));
			inConversation = true;
			playerTalking = true;
			Debug.Log("!Credits Music being faded to 0.2 now");
			StartCoroutine(FadeAudioSource.StartFade(DontDestroyCreditsMusic.Instance.audioSource, 5f, 0.2f));
			yield return new WaitForSeconds(9f);
			playerController.firstPersonController.enabled = false;
			mikeEnd.gameObject.SetActive(value: false);
			catEndGame.gameObject.SetActive(value: false);
			CursorModeUtility.ShowCursor();
			cabinUIManager.epilogueUIManager.StartEpilogue();
		}
	}

	public void LoadSceneAsync(string sceneName)
	{
		StartCoroutine(_LoadSceneAsync(sceneName));
	}

	private IEnumerator _LoadSceneAsync(string sceneName)
	{
		Debug.Log("Starting to load scene: " + sceneName);
		AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
		asyncLoad.allowSceneActivation = false;
		while (!asyncLoad.isDone)
		{
			Debug.Log("Loading progress: " + asyncLoad.progress * 100f + "%");
			if (asyncLoad.progress >= 0.9f)
			{
				Debug.Log("Scene loading complete. Activating scene...");
				asyncLoad.allowSceneActivation = true;
			}
			yield return null;
		}
		Debug.Log("Scene fully loaded and activated.");
	}

	public void TestingModeStartOutsideAwake()
	{
		CurrentSequence = SequenceType.OpeningLockBox;
		(playerController as CabinPlayerController).firstPersonController.transform.parent = null;
		(playerController as CabinPlayerController).firstPersonController.transform.position = playerOutside.position;
		(playerController as CabinPlayerController).firstPersonController.gameObject.SetActive(value: true);
		truckController.TestingModeStartOutsideAwake();
		interactible.ChangeCamera(fpsCamera);
		AudioMixerManager.PlayerOutside();
		(uiManager as CabinUIManager).ClearControlsText();
		inputManager.OnGetUp -= MoveToFPC;
		carIsInParkingLot = true;
		mikeCabin.HarperGotOut();
		(playerController as CabinPlayerController).truckPlayer.SetActive(value: false);
		truckController.rvState = TruckController.RVState.GotUp;
		if (!coldNightSub)
		{
			StartCoroutine(ColdNightSub());
		}
		truckController.TurnOffCar(completely: true);
		ChangePlayerState(PlayerState.Normal);
		(playerController as CabinPlayerController).firstPersonController.PlayerStatus(status: true);
		truckController.doorCollider.SetActive(value: true);
		AudioMixerManager.PlayerOutside();
		sRS_ParticleSystem.followTarget = playerController.cameraTransform;
		AudioMixerManager.SetGameVolume(0f);
		mikeCabin.canLookAtPlayerPeriodically = false;
		GenericAudioReferences.instance.reachedCabin.Play();
		cabinHouseManager.cabinSuitcase.gameObject.layer = 0;
		playerRoadBlocker.SetActive(value: true);
	}

	public void TestingModeStartInsideBeforeFridgeStocking()
	{
		CurrentSequence = SequenceType.StockingFridge;
		(playerController as CabinPlayerController).firstPersonController.transform.parent = null;
		(playerController as CabinPlayerController).firstPersonController.transform.position = playerInsideEntrance.position;
		(playerController as CabinPlayerController).firstPersonController.gameObject.SetActive(value: true);
		carIsInParkingLot = true;
		truckController.TestingModeStartOutsideAwake();
		interactible.ChangeCamera(fpsCamera);
		AudioMixerManager.PlayerOutside();
		(uiManager as CabinUIManager).ClearControlsText();
		inputManager.OnGetUp -= MoveToFPC;
		(playerController as CabinPlayerController).truckPlayer.SetActive(value: false);
		truckController.rvState = TruckController.RVState.GotUp;
		truckController.TurnOffCar(completely: true, playSFX: false);
		ChangePlayerState(PlayerState.Normal);
		(playerController as CabinPlayerController).firstPersonController.PlayerStatus(status: true);
		truckController.doorCollider.SetActive(value: true);
		cabinHouseManager.mainDoor.isLocked = false;
		AudioMixerManager.PlayerInside();
		mikeCabin.TestingSetAtPuttingDownCooler();
		cabinHouseManager.TestingModeStartInsideBeforeFridgeStocking();
		CabinPlayerController.playerLocation = CabinPlayerController.PlayerLocation.PlayerInside;
		sRS_ParticleSystem.followTarget = playerController.cameraTransform;
		AudioMixerManager.SetGameVolume(0f);
		mikeCabin.canLookAtPlayerPeriodically = false;
		GenericAudioReferences.instance.reachedCabin.Play();
		mikeCabin.pickedUpCooler = true;
		mikeCabin.leftTruck = true;
		cabinHouseManager.cabinSuitcase.gameObject.layer = 0;
		mikeCabin.colliderToilet.gameObject.SetActive(value: true);
		playerRoadBlocker.SetActive(value: true);
	}

	public void TestingModeStartInsideAfterFridgeStocking()
	{
		(playerController as CabinPlayerController).firstPersonController.transform.parent = null;
		(playerController as CabinPlayerController).firstPersonController.transform.position = playerInFrontOfFridge.position;
		(playerController as CabinPlayerController).firstPersonController.gameObject.SetActive(value: true);
		carIsInParkingLot = true;
		truckController.TestingModeStartOutsideAwake();
		interactible.ChangeCamera(fpsCamera);
		AudioMixerManager.PlayerOutside();
		(uiManager as CabinUIManager).ClearControlsText();
		inputManager.OnGetUp -= MoveToFPC;
		(playerController as CabinPlayerController).truckPlayer.SetActive(value: false);
		truckController.rvState = TruckController.RVState.GotUp;
		truckController.TurnOffCar(completely: true, playSFX: false);
		ChangePlayerState(PlayerState.Normal);
		(playerController as CabinPlayerController).firstPersonController.PlayerStatus(status: true);
		truckController.doorCollider.SetActive(value: true);
		cabinHouseManager.mainDoor.isLocked = false;
		AudioMixerManager.PlayerInside();
		mikeCabin.TestingStartInToilet();
		cabinHouseManager.TestingModeStartInsideAfterFridgeStocking();
		CabinPlayerController.playerLocation = CabinPlayerController.PlayerLocation.PlayerInside;
		sRS_ParticleSystem.followTarget = playerController.cameraTransform;
		AudioMixerManager.SetGameVolume(0f);
		mikeCabin.canLookAtPlayerPeriodically = false;
		GenericAudioReferences.instance.reachedCabin.Play();
		mikeCabin.pickedUpCooler = true;
		mikeCabin.leftTruck = true;
		cabinHouseManager.cabinSuitcase.gameObject.layer = 0;
		playerRoadBlocker.SetActive(value: true);
	}

	public void TestingModeStartInsideBeforeHostScare()
	{
		(playerController as CabinPlayerController).firstPersonController.transform.parent = null;
		(playerController as CabinPlayerController).firstPersonController.transform.position = playerInFrontBedroomDoor.position;
		(playerController as CabinPlayerController).firstPersonController.transform.localEulerAngles = playerInFrontBedroomDoor.localEulerAngles;
		(playerController as CabinPlayerController).firstPersonController.gameObject.SetActive(value: true);
		carIsInParkingLot = true;
		truckController.TestingModeStartOutsideAwake();
		interactible.ChangeCamera(fpsCamera);
		AudioMixerManager.PlayerOutside();
		(uiManager as CabinUIManager).ClearControlsText();
		inputManager.OnGetUp -= MoveToFPC;
		(playerController as CabinPlayerController).truckPlayer.SetActive(value: false);
		truckController.rvState = TruckController.RVState.GotUp;
		truckController.TurnOffCar(completely: true, playSFX: false);
		ChangePlayerState(PlayerState.Normal);
		(playerController as CabinPlayerController).firstPersonController.PlayerStatus(status: true);
		truckController.doorCollider.SetActive(value: true);
		cabinHouseManager.mainDoor.isLocked = false;
		AudioMixerManager.PlayerInside();
		mikeCabin.TestingBeforeHostScare();
		cabinHouseManager.TestingModeStartInsideBeforeHostScare();
		CabinPlayerController.playerLocation = CabinPlayerController.PlayerLocation.PlayerInside;
		sRS_ParticleSystem.followTarget = playerController.cameraTransform;
		AudioMixerManager.SetGameVolume(0f);
		mikeCabin.canLookAtPlayerPeriodically = false;
		GenericAudioReferences.instance.reachedCabin.Play();
		mikeCabin.pickedUpCooler = true;
		mikeCabin.leftTruck = true;
		playerRoadBlocker.SetActive(value: true);
	}

	public void TestingModeStartAfterHostTour()
	{
		(playerController as CabinPlayerController).firstPersonController.transform.parent = null;
		(playerController as CabinPlayerController).firstPersonController.transform.position = playerAfterTour.position;
		(playerController as CabinPlayerController).firstPersonController.transform.localEulerAngles = playerAfterTour.localEulerAngles;
		(playerController as CabinPlayerController).firstPersonController.gameObject.SetActive(value: true);
		GenericAudioReferences.instance.hostHasLeft.Play();
		carIsInParkingLot = true;
		truckController.TestingModeStartOutsideAwake();
		interactible.ChangeCamera(fpsCamera);
		AudioMixerManager.PlayerOutside();
		(uiManager as CabinUIManager).ClearControlsText();
		inputManager.OnGetUp -= MoveToFPC;
		(playerController as CabinPlayerController).truckPlayer.SetActive(value: false);
		truckController.rvState = TruckController.RVState.GotUp;
		truckController.TurnOffCar(completely: true, playSFX: false);
		ChangePlayerState(PlayerState.Normal);
		(playerController as CabinPlayerController).firstPersonController.PlayerStatus(status: true);
		truckController.doorCollider.SetActive(value: true);
		cabinHouseManager.mainDoor.isLocked = false;
		AudioMixerManager.PlayerInside();
		mikeCabin.TestingAfterHostTour();
		cabinHouseManager.TestingModeAfterHostTour();
		CabinPlayerController.playerLocation = CabinPlayerController.PlayerLocation.PlayerInside;
		sRS_ParticleSystem.followTarget = playerController.cameraTransform;
		bedroomTV.TurnOff(playSound: false);
		AudioMixerManager.SetGameVolume(0f);
		mikeCabin.canLookAtPlayerPeriodically = false;
		mikeCabin.pickedUpCooler = true;
		mikeCabin.leftTruck = true;
		cabinHouseManager.triggerShowerSub.SetActive(value: true);
		playerRoadBlocker.SetActive(value: true);
	}

	public void TestingModeStartAfterShower()
	{
		CurrentSequence = SequenceType.StartAfterShower;
		iceBoxLid.SetInteractable(value: false);
		cabinPlayerController.firstPersonController.transform.parent = null;
		cabinPlayerController.firstPersonController.transform.position = playerAfterShower.position;
		cabinPlayerController.firstPersonController.transform.localEulerAngles = playerAfterShower.localEulerAngles;
		cabinPlayerController.firstPersonController.gameObject.SetActive(value: true);
		carIsInParkingLot = true;
		truckController.TestingModeStartOutsideAwake();
		interactible.ChangeCamera(fpsCamera);
		AudioMixerManager.PlayerOutside();
		(uiManager as CabinUIManager).ClearControlsText();
		inputManager.OnGetUp -= MoveToFPC;
		cabinPlayerController.truckPlayer.SetActive(value: false);
		truckController.rvState = TruckController.RVState.GotUp;
		truckController.TurnOffCar(completely: true, playSFX: false);
		ChangePlayerState(PlayerState.Normal);
		cabinPlayerController.firstPersonController.PlayerStatus(status: true);
		truckController.doorCollider.SetActive(value: true);
		cabinHouseManager.mainDoor.isLocked = false;
		AudioMixerManager.PlayerInside();
		mikeCabin.TestingAfterShower();
		cabinHouseManager.TestingModeAfterShower();
		CabinPlayerController.playerLocation = CabinPlayerController.PlayerLocation.PlayerInside;
		sRS_ParticleSystem.followTarget = playerController.cameraTransform;
		bedroomTV.TurnOff(playSound: false);
		AudioMixerManager.SetGameVolume(0f);
		mikeCabin.canLookAtPlayerPeriodically = false;
		mikeCabin.pickedUpCooler = true;
		mikeCabin.leftTruck = true;
		playerRoadBlocker.SetActive(value: true);
	}

	public void TestingModeStartFishing()
	{
		CurrentSequence = SequenceType.Fishing;
		GenericAudioReferences.instance.fishingMusic.Play();
		GenericAudioReferences.instance.ReduceCabinAmbienceWhilePlayingFishingMusic();
		(playerController as CabinPlayerController).firstPersonController.transform.parent = null;
		(playerController as CabinPlayerController).firstPersonController.transform.position = playerFishing.position;
		(playerController as CabinPlayerController).firstPersonController.transform.localEulerAngles = playerFishing.localEulerAngles;
		(playerController as CabinPlayerController).firstPersonController.gameObject.SetActive(value: true);
		carIsInParkingLot = true;
		truckController.TestingModeStartOutsideAwake();
		interactible.ChangeCamera(fpsCamera);
		AudioMixerManager.PlayerOutside();
		(uiManager as CabinUIManager).ClearControlsText();
		inputManager.OnGetUp -= MoveToFPC;
		(playerController as CabinPlayerController).truckPlayer.SetActive(value: false);
		truckController.rvState = TruckController.RVState.GotUp;
		truckController.TurnOffCar(completely: true, playSFX: false);
		ChangePlayerState(PlayerState.Normal);
		(playerController as CabinPlayerController).firstPersonController.PlayerStatus(status: true);
		truckController.doorCollider.SetActive(value: true);
		cabinHouseManager.mainDoor.isLocked = false;
		AudioMixerManager.PlayerInside();
		mikeCabin.TestingFishing();
		cabinHouseManager.TestingModeStartFishing();
		CabinPlayerController.playerLocation = CabinPlayerController.PlayerLocation.PlayerInside;
		cabinHouseManager.shedManager.rod1.PickUp();
		sRS_ParticleSystem.followTarget = playerController.cameraTransform;
		bedroomTV.TurnOff(playSound: false);
		AudioMixerManager.SetGameVolume(0f);
		mikeCabin.canLookAtPlayerPeriodically = false;
		mikeCabin.pickedUpCooler = true;
		mikeCabin.leftTruck = true;
		playerRoadBlocker.SetActive(value: true);
		cabinPlayerController.lockBox.gameObject.layer = 1;
	}

	public void TestingModeStartNearFishingArea()
	{
		CurrentSequence = SequenceType.Fishing;
		GenericAudioReferences.instance.fishingMusic.Play();
		GenericAudioReferences.instance.ReduceCabinAmbienceWhilePlayingFishingMusic();
		(playerController as CabinPlayerController).firstPersonController.transform.parent = null;
		(playerController as CabinPlayerController).firstPersonController.transform.position = playerFishing.position;
		(playerController as CabinPlayerController).firstPersonController.transform.localEulerAngles = playerFishing.localEulerAngles;
		(playerController as CabinPlayerController).firstPersonController.gameObject.SetActive(value: true);
		carIsInParkingLot = true;
		truckController.TestingModeStartOutsideAwake();
		interactible.ChangeCamera(fpsCamera);
		AudioMixerManager.PlayerOutside();
		(uiManager as CabinUIManager).ClearControlsText();
		inputManager.OnGetUp -= MoveToFPC;
		(playerController as CabinPlayerController).truckPlayer.SetActive(value: false);
		truckController.rvState = TruckController.RVState.GotUp;
		truckController.TurnOffCar(completely: true, playSFX: false);
		ChangePlayerState(PlayerState.Normal);
		(playerController as CabinPlayerController).firstPersonController.PlayerStatus(status: true);
		truckController.doorCollider.SetActive(value: true);
		cabinHouseManager.mainDoor.isLocked = false;
		AudioMixerManager.PlayerInside();
		mikeCabin.TestingAfterFishing();
		cabinHouseManager.TestingModeAfterFishing();
		CabinPlayerController.playerLocation = CabinPlayerController.PlayerLocation.PlayerInside;
		cabinHouseManager.shedManager.rod1.PickUp();
		FishingRod.carpCaught = 4;
		fishingDone = true;
		sRS_ParticleSystem.followTarget = playerController.cameraTransform;
		bedroomTV.TurnOff(playSound: false);
		AudioMixerManager.SetGameVolume(0f);
		mikeCabin.pickedUpCooler = true;
		mikeCabin.leftTruck = true;
		playerRoadBlocker.SetActive(value: true);
		cabinPlayerController.lockBox.gameObject.layer = 1;
	}

	public void TestingModeStartInKitchen()
	{
		CurrentSequence = SequenceType.Cooking;
		if (currentCabinSceneType == CabinSceneType.CabinScene)
		{
			cabinPlayerController.firstPersonController.gameObject.SetActive(value: true);
			carIsInParkingLot = true;
			truckController.TestingModeStartOutsideAwake();
			interactible.ChangeCamera(fpsCamera);
			(uiManager as CabinUIManager).ClearControlsText();
			inputManager.OnGetUp -= MoveToFPC;
			cabinPlayerController.truckPlayer.SetActive(value: false);
			truckController.rvState = TruckController.RVState.GotUp;
			truckController.TurnOffCar(completely: true, playSFX: false);
			cabinPlayerController.firstPersonController.PlayerStatus(status: true);
			truckController.doorCollider.SetActive(value: true);
			cabinHouseManager.mainDoor.isLocked = false;
			mikeCabin.gameObject.SetActive(value: false);
			CabinPlayerController.playerLocation = CabinPlayerController.PlayerLocation.PlayerInside;
			cabinHouseManager.TestingModeAfterShower();
			mikeController.gameObject.SetActive(value: true);
			SetHouseAfterFishing();
			fishingDone = true;
			(playerController as CabinPlayerController).firstPersonController.transform.parent = null;
		}
		mikeController.TestingModeStartInKitchen();
		cabinPlayerController.TestingModeStartInKitchen();
		AudioMixerManager.PlayerInside();
		sRS_ParticleSystem.followTarget = playerController.cameraTransform;
		bedroomTV.TurnOff(playSound: false);
		AudioMixerManager.SetGameVolume(0f);
		mikeCabin.pickedUpCooler = true;
		mikeCabin.leftTruck = true;
		playerRoadBlocker.SetActive(value: true);
		cabinPlayerController.lockBox.gameObject.layer = 1;
	}

	public void TestingModeStartInKitchenAfterOvenStart()
	{
		iceBox.EnableFish1();
		iceBoxLid.SetInteractable(value: true);
		cabinPlayerController.SetDiningTableInteractable(value: false);
		CurrentSequence = SequenceType.PickingBoardGame;
		if (currentCabinSceneType == CabinSceneType.CabinScene)
		{
			carIsInParkingLot = true;
			truckController.TestingModeStartOutsideAwake();
			interactible.ChangeCamera(fpsCamera);
			(uiManager as CabinUIManager).ClearControlsText();
			inputManager.OnGetUp -= MoveToFPC;
			cabinPlayerController.truckPlayer.SetActive(value: false);
			truckController.rvState = TruckController.RVState.GotUp;
			truckController.TurnOffCar(completely: true, playSFX: false);
			ChangePlayerState(PlayerState.Normal);
			cabinPlayerController.firstPersonController.PlayerStatus(status: true);
			truckController.doorCollider.SetActive(value: true);
			cabinHouseManager.mainDoor.isLocked = false;
			AudioMixerManager.PlayerInside();
			mikeCabin.gameObject.SetActive(value: false);
			CabinPlayerController.playerLocation = CabinPlayerController.PlayerLocation.PlayerInside;
			cabinHouseManager.TestingModeAfterShower();
			SetHouseAfterFishing();
			fishingDone = true;
			mikeController.gameObject.SetActive(value: true);
			(playerController as CabinPlayerController).firstPersonController.transform.parent = null;
		}
		mikeController.TestingModeStartInKitchenAfterOvenStart();
		cabinPlayerController.firstPersonController.gameObject.SetActive(value: false);
		cabinPlayerController.TestingModeStartInKitchenAfterOvenStart();
		cabinPlayerController.firstPersonController.gameObject.SetActive(value: true);
		casserole.EnableAllIngredients();
		oven.TestingModeStartInKitchenAfterOvenStart(casserole.transform);
		AudioMixerManager.PlayerInside();
		sRS_ParticleSystem.followTarget = playerController.cameraTransform;
		bedroomTV.TurnOff(playSound: false);
		AudioMixerManager.SetGameVolume(0f);
		mikeCabin.pickedUpCooler = true;
		mikeCabin.leftTruck = true;
		cabinUIManager.ClearControlsText();
		playerRoadBlocker.SetActive(value: true);
		cabinPlayerController.lockBox.gameObject.layer = 1;
	}

	public void TestingModeStartOnDiningTableAfterOvenStart()
	{
		iceBoxLid.SetInteractable(value: true);
		CurrentSequence = SequenceType.PlayingJenga;
		if (currentCabinSceneType == CabinSceneType.CabinScene)
		{
			CursorModeUtility.HideCursor();
			cabinPlayerController.firstPersonController.gameObject.SetActive(value: true);
			carIsInParkingLot = true;
			truckController.TestingModeStartOutsideAwake();
			interactible.ChangeCamera(fpsCamera);
			(uiManager as CabinUIManager).ClearControlsText();
			inputManager.OnGetUp -= MoveToFPC;
			cabinPlayerController.truckPlayer.SetActive(value: false);
			truckController.rvState = TruckController.RVState.GotUp;
			truckController.TurnOffCar(completely: true);
			cabinPlayerController.firstPersonController.PlayerStatus(status: true);
			truckController.doorCollider.SetActive(value: true);
			cabinHouseManager.mainDoor.isLocked = false;
			mikeCabin.gameObject.SetActive(value: false);
			CabinPlayerController.playerLocation = CabinPlayerController.PlayerLocation.PlayerInside;
			cabinHouseManager.TestingModeAfterShower();
			fishingDone = true;
			(playerController as CabinPlayerController).firstPersonController.transform.parent = null;
		}
		mikeController.gameObject.SetActive(value: true);
		ChangePlayerState(PlayerState.SittingAtSittablePlace);
		mikeController.TestingModeStartOnDiningTableAfterOvenStart();
		cabinPlayerController.TestingModeStartOnDiningTableAfterOvenStart();
		casserole.EnableAllIngredients();
		oven.TestingModeStartInKitchenAfterOvenStart(casserole.transform);
		AudioMixerManager.PlayerInside();
		sRS_ParticleSystem.followTarget = playerController.cameraTransform;
		bedroomTV.TurnOff(playSound: false);
		AudioMixerManager.SetGameVolume(0f);
		mikeCabin.pickedUpCooler = true;
		mikeCabin.leftTruck = true;
		playerRoadBlocker.SetActive(value: true);
		cabinPlayerController.lockBox.gameObject.layer = 1;
	}

	public void TestingModeStartOnBasementTable()
	{
		iceBoxLid.SetInteractable(value: true);
		CurrentSequence = SequenceType.PlayingOuija;
		if (currentCabinSceneType == CabinSceneType.CabinScene)
		{
			carIsInParkingLot = true;
			truckController.TestingModeStartOutsideAwake();
			interactible.ChangeCamera(fpsCamera);
			(uiManager as CabinUIManager).ClearControlsText();
			inputManager.OnGetUp -= MoveToFPC;
			cabinPlayerController.truckPlayer.SetActive(value: false);
			truckController.rvState = TruckController.RVState.GotUp;
			truckController.TurnOffCar(completely: true);
			cabinPlayerController.firstPersonController.PlayerStatus(status: true);
			truckController.doorCollider.SetActive(value: true);
			cabinHouseManager.mainDoor.isLocked = false;
			mikeCabin.gameObject.SetActive(value: false);
			CabinPlayerController.playerLocation = CabinPlayerController.PlayerLocation.PlayerInside;
			cabinHouseManager.TestingModeAfterShower();
			(playerController as CabinPlayerController).firstPersonController.transform.parent = null;
		}
		GenericAudioReferences.instance.ouijaAmbientPiano.Play();
		mikeController.gameObject.SetActive(value: true);
		oven.SetOvenVolumeToBasementLevel();
		ChangePlayerState(PlayerState.SittingAtSittablePlace);
		mikeController.TestingModeStartOnBasementTable();
		cabinPlayerController.TestingModeStartOnBasementTable();
		isPlayingOuija = true;
		casserole.EnableAllIngredients();
		oven.TestingModeStartInKitchenAfterOvenStart(casserole.transform);
		AudioMixerManager.PlayerInside();
		sRS_ParticleSystem.followTarget = playerController.cameraTransform;
		bedroomTV.TurnOff(playSound: false);
		AudioMixerManager.SetGameVolume(0f);
		mikeCabin.pickedUpCooler = true;
		mikeCabin.leftTruck = true;
		playerRoadBlocker.SetActive(value: true);
		cabinPlayerController.lockBox.gameObject.layer = 1;
	}

	public void TestingModeStartInKitchenAfterOvenComplete()
	{
		iceBoxLid.SetInteractable(value: true);
		casserole.SetInteractable(value: true);
		CurrentSequence = SequenceType.Eating;
		if (currentCabinSceneType == CabinSceneType.CabinScene)
		{
			cabinPlayerController.firstPersonController.gameObject.SetActive(value: true);
			carIsInParkingLot = true;
			truckController.TestingModeStartOutsideAwake();
			interactible.ChangeCamera(fpsCamera);
			(uiManager as CabinUIManager).ClearControlsText();
			inputManager.OnGetUp -= MoveToFPC;
			cabinPlayerController.truckPlayer.SetActive(value: false);
			truckController.rvState = TruckController.RVState.GotUp;
			truckController.TurnOffCar(completely: true);
			cabinPlayerController.firstPersonController.PlayerStatus(status: true);
			truckController.doorCollider.SetActive(value: true);
			cabinHouseManager.mainDoor.isLocked = false;
			mikeCabin.gameObject.SetActive(value: false);
			CabinPlayerController.playerLocation = CabinPlayerController.PlayerLocation.PlayerInside;
			cabinHouseManager.TestingModeAfterShower();
			mikeController.gameObject.SetActive(value: true);
			fishingDone = true;
			(playerController as CabinPlayerController).firstPersonController.transform.parent = null;
		}
		mikeController.TestingModeStartInKitchenAfterOvenComplete();
		(playerController as CabinPlayerController).TestingModeStartInKitchenAfterOvenComplete();
		casserole.EnableAllIngredients();
		oven.TestingModeStartInKitchenAfterOvenComplete(casserole.transform);
		AudioMixerManager.PlayerInside();
		sRS_ParticleSystem.followTarget = playerController.cameraTransform;
		bedroomTV.TurnOff(playSound: false);
		AudioMixerManager.SetGameVolume(0f);
		mikeCabin.pickedUpCooler = true;
		mikeCabin.leftTruck = true;
		playerRoadBlocker.SetActive(value: true);
		cabinPlayerController.lockBox.gameObject.layer = 1;
	}

	public void TestingModeStartBeforeMikeJumpscare()
	{
		iceBoxLid.SetInteractable(value: true);
		cabinPlayerController.firstPersonController.gameObject.SetActive(value: true);
		carIsInParkingLot = true;
		truckController.TestingModeStartOutsideAwake();
		interactible.ChangeCamera(fpsCamera);
		(uiManager as CabinUIManager).ClearControlsText();
		inputManager.OnGetUp -= MoveToFPC;
		cabinPlayerController.truckPlayer.SetActive(value: false);
		truckController.rvState = TruckController.RVState.GotUp;
		truckController.TurnOffCar(completely: true);
		cabinPlayerController.firstPersonController.PlayerStatus(status: true);
		truckController.doorCollider.SetActive(value: true);
		cabinHouseManager.mainDoor.isLocked = false;
		mikeCabin.gameObject.SetActive(value: false);
		CabinPlayerController.playerLocation = CabinPlayerController.PlayerLocation.PlayerInside;
		cabinHouseManager.TestingModeAfterShower();
		fishingDone = true;
		mikeController.gameObject.SetActive(value: false);
		AudioMixerManager.PlayerInside();
		(playerController as CabinPlayerController).firstPersonController.transform.parent = null;
		(playerController as CabinPlayerController).firstPersonController.transform.position = playerAfterTour.position;
		(playerController as CabinPlayerController).firstPersonController.transform.localEulerAngles = playerAfterTour.localEulerAngles;
		(playerController as CabinPlayerController).firstPersonController.gameObject.SetActive(value: true);
		currentPlayerState = PlayerState.Normal;
		cabinUIManager.dockManager.OpenChatWindow(ChatWindowType.Mike);
		(uiManager as CabinUIManager).phoneUI.notifSystem.CreateNotif(1, 0);
		UIManager.OpenPhone = (Action)Delegate.Combine(UIManager.OpenPhone, new Action(SendMikeTexts1));
		cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "Esc"));
		cabinHouseManager.ColliderStairsTexting.SetActive(value: true);
		sRS_ParticleSystem.followTarget = playerController.cameraTransform;
		bedroomTV.TurnOff(playSound: false);
		AudioMixerManager.SetGameVolume(0f);
		mikeCabin.pickedUpCooler = true;
		mikeCabin.leftTruck = true;
		playerRoadBlocker.SetActive(value: true);
		cabinPlayerController.lockBox.gameObject.layer = 1;
	}

	public void TestingModeStartHiding()
	{
		iceBoxLid.SetInteractable(value: true);
		carIsInParkingLot = true;
		truckController.TestingModeStartOutsideAwake();
		interactible.ChangeCamera(fpsCamera);
		(uiManager as CabinUIManager).ClearControlsText();
		inputManager.OnGetUp -= MoveToFPC;
		cabinPlayerController.truckPlayer.SetActive(value: false);
		truckController.rvState = TruckController.RVState.GotUp;
		truckController.TurnOffCar(completely: true, playSFX: false);
		cabinPlayerController.firstPersonController.PlayerStatus(status: true);
		truckController.doorCollider.SetActive(value: true);
		cabinHouseManager.mainDoor.isLocked = false;
		mikeCabin.gameObject.SetActive(value: false);
		CabinPlayerController.playerLocation = CabinPlayerController.PlayerLocation.PlayerInside;
		cabinHouseManager.TestingModeAfterShower();
		fishingDone = true;
		mikeController.gameObject.SetActive(value: false);
		AudioMixerManager.PlayerInside();
		cabinPlayerController.firstPersonController.gameObject.SetActive(value: false);
		(playerController as CabinPlayerController).firstPersonController.transform.parent = null;
		(playerController as CabinPlayerController).firstPersonController.transform.position = playerAfterTour.position;
		(playerController as CabinPlayerController).firstPersonController.transform.localEulerAngles = playerAfterTour.localEulerAngles;
		(playerController as CabinPlayerController).firstPersonController.gameObject.SetActive(value: true);
		cabinPlayerController.firstPersonController.gameObject.SetActive(value: true);
		currentPlayerState = PlayerState.Normal;
		mikePostEating.gameObject.SetActive(value: true);
		mikePostEating.TestingModeStartHiding();
		sRS_ParticleSystem.followTarget = playerController.cameraTransform;
		bedroomTV.TurnOff(playSound: false);
		AudioMixerManager.SetGameVolume(0f);
		mikeCabin.pickedUpCooler = true;
		mikeCabin.leftTruck = true;
		playerRoadBlocker.SetActive(value: true);
		cabinHouseManager.carpIcebox3.SetActive(value: true);
		cabinPlayerController.lockBox.gameObject.layer = 1;
	}

	public void TestingModeMikeHiding()
	{
		iceBoxLid.SetInteractable(value: true);
		carIsInParkingLot = true;
		truckController.TestingModeStartOutsideAwake();
		interactible.ChangeCamera(fpsCamera);
		(uiManager as CabinUIManager).ClearControlsText();
		inputManager.OnGetUp -= MoveToFPC;
		cabinPlayerController.truckPlayer.SetActive(value: false);
		truckController.rvState = TruckController.RVState.GotUp;
		truckController.TurnOffCar(completely: true);
		cabinPlayerController.firstPersonController.PlayerStatus(status: true);
		truckController.doorCollider.SetActive(value: true);
		cabinHouseManager.mainDoor.isLocked = false;
		mikeCabin.gameObject.SetActive(value: false);
		CabinPlayerController.playerLocation = CabinPlayerController.PlayerLocation.PlayerInside;
		fishingDone = true;
		mikeController.gameObject.SetActive(value: false);
		cabinPlayerController.firstPersonController.gameObject.SetActive(value: false);
		(playerController as CabinPlayerController).firstPersonController.transform.parent = null;
		(playerController as CabinPlayerController).firstPersonController.transform.position = playerMikeBedroom.position;
		(playerController as CabinPlayerController).firstPersonController.transform.localEulerAngles = playerMikeBedroom.localEulerAngles;
		(playerController as CabinPlayerController).firstPersonController.gameObject.SetActive(value: true);
		cabinPlayerController.firstPersonController.gameObject.SetActive(value: true);
		currentPlayerState = PlayerState.Normal;
		mikePostEating.gameObject.SetActive(value: true);
		mikePostEating.TestingModeStartMikeHiding();
		cabinHouseManager.TestingModeInsideBedroom();
		sRS_ParticleSystem.followTarget = playerController.cameraTransform;
		AudioMixerManager.PlayerDeepInside();
		bedroomTV.TurnOff(playSound: false);
		AudioMixerManager.SetGameVolume(0f);
		mikeCabin.pickedUpCooler = true;
		mikeCabin.leftTruck = true;
		playerRoadBlocker.SetActive(value: true);
		cabinPlayerController.lockBox.gameObject.layer = 1;
	}

	public void TestingModeAfterCatJumpscare()
	{
		iceBoxLid.SetInteractable(value: true);
		cabinPlayerController.firstPersonController.gameObject.SetActive(value: true);
		carIsInParkingLot = true;
		truckController.TestingModeStartOutsideAwake();
		interactible.ChangeCamera(fpsCamera);
		(uiManager as CabinUIManager).ClearControlsText();
		inputManager.OnGetUp -= MoveToFPC;
		cabinPlayerController.truckPlayer.SetActive(value: false);
		truckController.rvState = TruckController.RVState.GotUp;
		truckController.TurnOffCar(completely: true);
		cabinPlayerController.firstPersonController.PlayerStatus(status: true);
		truckController.doorCollider.SetActive(value: true);
		cabinHouseManager.mainDoor.isLocked = false;
		mikeCabin.gameObject.SetActive(value: false);
		CabinPlayerController.playerLocation = CabinPlayerController.PlayerLocation.PlayerInside;
		fishingDone = true;
		mikeController.gameObject.SetActive(value: false);
		(playerController as CabinPlayerController).firstPersonController.transform.parent = null;
		(playerController as CabinPlayerController).firstPersonController.transform.position = playerMikeBedroomAfterCatJumpscare.position;
		(playerController as CabinPlayerController).firstPersonController.transform.localEulerAngles = playerMikeBedroomAfterCatJumpscare.localEulerAngles;
		(playerController as CabinPlayerController).firstPersonController.gameObject.SetActive(value: true);
		currentPlayerState = PlayerState.Normal;
		mikePostEating.gameObject.SetActive(value: true);
		mikePostEating.TestingModeAfterCatJumpscare();
		cabinHouseManager.TestingModeInsideBedroomCatJumpscare();
		cabinHouseManager.EnableIceBoxAndCarps();
		sRS_ParticleSystem.followTarget = playerController.cameraTransform;
		AudioMixerManager.PlayerDeepInside();
		bedroomTV.TurnOff(playSound: false);
		AudioMixerManager.SetGameVolume(0f);
		mikeCabin.pickedUpCooler = true;
		mikeCabin.leftTruck = true;
		playerRoadBlocker.SetActive(value: true);
		cabinPlayerController.lockBox.gameObject.layer = 1;
	}

	public void TestingModeStartOnBed()
	{
		Debug.Log("!Start on bed called");
		CurrentSequence = SequenceType.RizzSequence;
		hikerCabinController.gameObject.SetActive(value: false);
		sinisterAudioTrigger.SetActive(value: false);
		carIsInParkingLot = true;
		interactible.ChangeCamera(fpsCamera);
		cabinUIManager.ClearControlsText();
		cabinPlayerController.truckPlayer.SetActive(value: false);
		cabinPlayerController.firstPersonController.PlayerStatus(status: true);
		if (mikeRizzlerController != null)
		{
			mikeRizzlerController.SetPlayerTransform(cabinPlayerController.firstPersonController.transform);
			mikeRizzlerController.TestingModeStartOnBed();
		}
		Light[] array = truckLights;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = false;
		}
		CabinPlayerController.playerLocation = CabinPlayerController.PlayerLocation.PlayerDeepInside;
		cabinHouseManager.TestingModeStartOnBed();
		cabinPlayerController.TestingModeStartOnBed();
		AudioMixerManager.PlayerDeepInside();
		bedroomTV.TurnOff(playSound: false);
		if (ChecklistAccess.IsGlobalTestingEnabled())
		{
			if (CabinSceneDarkChecklist.GetInstance().DisableIntro)
			{
				AudioMixerManager.FadeGameSoundToCustom(0f, 0.5f, this);
			}
		}
		else if (cabinUIManager.disableIntro)
		{
			AudioMixerManager.FadeGameSoundToCustom(0f, 0.5f, this);
		}
		StartCoroutine(DelayedAction());
		IEnumerator DelayedAction()
		{
			yield return new WaitForSeconds(12f);
			cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "GetUp"));
			inputManager.OnGetUp += cabinPlayerController.GetUpFromBed;
		}
	}

	public void TestingModeStartWhenMikeGoesToTruck()
	{
		CurrentSequence = SequenceType.RizzSequence;
		hikerCabinController.gameObject.SetActive(value: false);
		sinisterAudioTrigger.SetActive(value: false);
		carIsInParkingLot = true;
		interactible.ChangeCamera(fpsCamera);
		cabinUIManager.ClearControlsText();
		cabinPlayerController.truckPlayer.SetActive(value: false);
		cabinPlayerController.firstPersonController.PlayerStatus(status: true);
		cabinPlayerController.SetMikeInRoom(value: false);
		cabinPlayerController.SetIsKnockerRevealed(value: true);
		cabinPlayerController.SetMainDoorLocked(value: false);
		truckController.doorCollider.SetActive(value: true);
		cabinHouseManager.TestingModeStartOnBed();
		if (mikeRizzlerController != null)
		{
			mikeRizzlerController.SetPlayerTransform(cabinPlayerController.firstPersonController.transform);
			mikeRizzlerController.TestingModeStartWhenMikeGoesToTruck();
		}
		if (!CabinSceneDarkChecklist.GetInstance().DisableIntro && ChecklistAccess.IsGlobalTestingEnabled())
		{
			AudioMixerManager.FadeGameSoundToCustom(0f, 9f, this);
		}
		else
		{
			AudioMixerManager.SetGameVolume(0f);
		}
		AudioMixerManager.PlayerDeepInside();
		cabinPlayerController.TestingModeStartWhenMikeGoesToTruck();
	}

	public void TestingModeStartWhenHikerKnocks()
	{
		CurrentSequence = SequenceType.HikerSequence;
		cabinUIManager.ShowHikerSequenceMonologueAfterTime(delegate
		{
			StartCoroutine(DelayedAction());
		});
		cabinTruck.SetActive(value: false);
		mikeRizzlerController.TestingModeStartWhenHikerKnocks();
		cabinPlayerController.TestingModeStartWhenHikerKnocks();
		hikerCabinController.gameObject.SetActive(value: true);
		hikerCabinController.TestingModeStartWhenHikerKnocks();
		sinisterAudioTrigger.SetActive(value: true);
		AudioMixerManager.PlayerDeepInside();
		cabinHouseManager.EnableBlinds();
		mainDoor.SetInteractable(value: false);
		if (ChecklistAccess.IsGlobalTestingEnabled())
		{
			if (!CabinSceneDarkChecklist.GetInstance().DisableIntro)
			{
				AudioMixerManager.FadeGameSoundToCustom(0f, 9f, this);
			}
			else
			{
				AudioMixerManager.FadeGameSoundToCustom(0f, 0.5f, this);
			}
		}
		else
		{
			_ = cabinUIManager.disableIntro;
		}
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(0, 0, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(0, 1, playsfx: false);
		IEnumerator DelayedAction()
		{
			yield return new WaitForSeconds(2f);
			cabinUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "GetUp"));
			inputManager.OnGetUp += cabinPlayerController.GetUpFromBed;
			yield return new WaitForSeconds(8f);
			SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", "SomeoneAtTheDoor"));
		}
	}

	public void TestingModeStartHostAtDoor()
	{
		CurrentSequence = SequenceType.HostAtDoor;
		cabinTruck.SetActive(value: false);
		mikeRizzlerController.TestingModeStartWhenHikerKnocks();
		cabinPlayerController.TestingModeStartWhenHostAtDoor();
		sinisterAudioTrigger.SetActive(value: true);
		AudioMixerManager.PlayerDeepInside();
		cabinHouseManager.EnableBlinds();
		mainDoor.SetInteractable(value: false);
		hostEndGame.gameObject.SetActive(value: true);
		hostEndGame.TestingModeStartHostAtDoor();
		hostConvoTrigger.gameObject.SetActive(value: true);
		GenericAudioReferences.instance.scaryViolin.Stop();
		cellarDoor1.transform.localEulerAngles = new Vector3(347.5f, 145.5f, 8.4f);
		cellarDoor2.transform.localEulerAngles = new Vector3(348.2f, 219f, 350.6f);
		hikerCabinController.gameObject.SetActive(value: false);
		GenericAudioReferences.instance.ambTextSent.Play();
		glassWindow.material = snowBallOnWindowMaterial;
		triggerDoorCrouch.SetActive(value: true);
		if (!CabinSceneDarkChecklist.GetInstance().DisableIntro && ChecklistAccess.IsGlobalTestingEnabled())
		{
			AudioMixerManager.FadeGameSoundToCustom(0f, 9f, this);
		}
		else
		{
			AudioMixerManager.FadeGameSoundToCustom(0f, 0.5f, this);
		}
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(1, 0, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(1, 1, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(1, 2, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(0, 0, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(0, 1, playsfx: false);
		currentPlayerState = PlayerState.Normal;
	}

	public void TestingModeStartHostHittingDoor()
	{
		CurrentSequence = SequenceType.HostHittingDoor;
		cabinTruck.SetActive(value: false);
		mikeRizzlerController.TestingModeStartWhenHikerKnocks();
		cabinPlayerController.TestingModeStartWhenHostAtDoor();
		sinisterAudioTrigger.SetActive(value: false);
		AudioMixerManager.PlayerDeepInside();
		cabinHouseManager.EnableBlinds();
		mainDoor.SetInteractable(value: false);
		hostEndGame.gameObject.SetActive(value: true);
		hostEndGame.TestingModeStartHostHittingDoor();
		hostConvoTrigger.gameObject.SetActive(value: false);
		GenericAudioReferences.instance.hostEndConvoScary.Play();
		GenericAudioReferences.instance.ambTextSent.Stop();
		GenericAudioReferences.instance.rickText3Recieved.Stop();
		cellarDoor1.transform.localEulerAngles = new Vector3(347.5f, 145.5f, 8.4f);
		cellarDoor2.transform.localEulerAngles = new Vector3(348.2f, 219f, 350.6f);
		hikerCabinController.gameObject.SetActive(value: false);
		glassWindow.material = snowBallOnWindowMaterial;
		triggerDoorCrouch.SetActive(value: true);
		if (!CabinSceneDarkChecklist.GetInstance().DisableIntro && ChecklistAccess.IsGlobalTestingEnabled())
		{
			AudioMixerManager.FadeGameSoundToCustom(0f, 9f, this);
		}
		else
		{
			AudioMixerManager.FadeGameSoundToCustom(0f, 0.5f, this);
		}
		DisableSitting();
		MakeBackyardAccessible();
		AudioMixerManager.PlayerInside();
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(0, 0, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(0, 1, playsfx: false);
		currentPlayerState = PlayerState.Normal;
	}

	public void TestingModeStartHostInBasement()
	{
		CurrentSequence = SequenceType.HostHittingDoor;
		cabinTruck.SetActive(value: false);
		mikeRizzlerController.TestingModeStartWhenHikerKnocks();
		cabinPlayerController.TestingModeStartHidingInBasement();
		UnderStairsDoorDark.instance.TestingPlayerInside();
		sinisterAudioTrigger.SetActive(value: false);
		AudioMixerManager.PlayerDeepInside();
		cabinHouseManager.EnableBlinds();
		mainDoor.SetInteractable(value: false);
		hostEndGame.gameObject.SetActive(value: true);
		hostEndGame.TestingModeStartHostInBasement();
		hostConvoTrigger.gameObject.SetActive(value: false);
		GenericAudioReferences.instance.hostEndConvoScary.Stop();
		GenericAudioReferences.instance.ambTextSent.Stop();
		GenericAudioReferences.instance.rickText3Recieved.Stop();
		cellarDoor1.transform.localEulerAngles = new Vector3(347.5f, 145.5f, 8.4f);
		cellarDoor2.transform.localEulerAngles = new Vector3(348.2f, 219f, 350.6f);
		hikerCabinController.gameObject.SetActive(value: false);
		glassWindow.material = snowBallOnWindowMaterial;
		triggerDoorCrouch.SetActive(value: true);
		if (!CabinSceneDarkChecklist.GetInstance().DisableIntro && ChecklistAccess.IsGlobalTestingEnabled())
		{
			AudioMixerManager.FadeGameSoundToCustom(0f, 9f, this);
		}
		else
		{
			AudioMixerManager.FadeGameSoundToCustom(0f, 0.5f, this);
		}
		basementDoor.OpenDoor();
		DisableSitting();
		MakePorchAreaAccessible();
		MakeBackyardAccessible();
		AudioMixerManager.PlayerDeepInside();
		GenericAudioReferences.instance.BreakHandleStopAS();
		GenericAudioReferences.instance.heartBeatDavidBasement.volume = 0f;
		GenericAudioReferences.instance.heartBeatDavidBasement.Play();
		StartCoroutine(FadeAudioSource.StartFade(GenericAudioReferences.instance.heartBeatDavidBasement, 2f, 0.25f));
		mainDoorBlocker.SetActive(value: true);
		hostEndGame.diningTableFallen.SetActive(value: true);
		hostEndGame.diningTableNormal.SetActive(value: false);
		hostEndGame.tableFlipped = true;
		hostEndGame.sofaMoved.SetActive(value: true);
		hostEndGame.sofaNormal.SetActive(value: false);
		backDoorBlocker.SetActive(value: true);
		currentPlayerState = PlayerState.Normal;
		hostEndGame.doorHandle.enabled = true;
		hostEndGame.doorHandle.GetComponent<DoorHandle>().MuteDoorHandleAudio();
		hostEndGame.doorHandle.SetTrigger("Break");
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(1, 0, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(1, 1, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(1, 2, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(1, 3, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(1, 4, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(1, 5, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(1, 6, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(0, 0, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(0, 1, playsfx: false);
		stoolScript.darkSceneHintLight.SetActive(value: true);
	}

	public void TestingModeStartHostBreakingDoor()
	{
		CurrentSequence = SequenceType.HostHittingDoor;
		cabinTruck.SetActive(value: false);
		mikeRizzlerController.TestingModeStartWhenHikerKnocks();
		cabinPlayerController.TestingModeStartInBedroom();
		sinisterAudioTrigger.SetActive(value: false);
		cabinHouseManager.EnableBlinds();
		mainDoor.SetInteractable(value: false);
		hostEndGame.gameObject.SetActive(value: true);
		hostEndGame.TestingModeStartBreakingDoor();
		hostConvoTrigger.gameObject.SetActive(value: false);
		GenericAudioReferences.instance.hostEndConvoScary.Stop();
		GenericAudioReferences.instance.ambTextSent.Stop();
		GenericAudioReferences.instance.rickText3Recieved.Stop();
		cellarDoor1.transform.localEulerAngles = new Vector3(347.5f, 145.5f, 8.4f);
		cellarDoor2.transform.localEulerAngles = new Vector3(348.2f, 219f, 350.6f);
		hikerCabinController.gameObject.SetActive(value: false);
		glassWindow.material = snowBallOnWindowMaterial;
		triggerDoorCrouch.SetActive(value: true);
		if (!CabinSceneDarkChecklist.GetInstance().DisableIntro && ChecklistAccess.IsGlobalTestingEnabled())
		{
			AudioMixerManager.FadeGameSoundToCustom(0f, 9f, this);
		}
		else
		{
			AudioMixerManager.FadeGameSoundToCustom(0f, 0.5f, this);
		}
		DisableSitting();
		MakePorchAreaAccessible();
		MakeBackyardAccessible();
		AudioMixerManager.PlayerDeepInside();
		mainDoorBlocker.SetActive(value: true);
		currentPlayerState = PlayerState.Normal;
		hostEndGame.doorHandle.enabled = true;
		hostEndGame.doorHandle.GetComponent<DoorHandle>().MuteDoorHandleAudio();
		hostEndGame.doorHandle.SetTrigger("Break");
		hostEndGame.diningTableFallen.SetActive(value: true);
		hostEndGame.diningTableNormal.SetActive(value: false);
		hostEndGame.tableFlipped = true;
		hostEndGame.sofaMoved.SetActive(value: true);
		hostEndGame.sofaNormal.SetActive(value: false);
		backDoorBlocker.SetActive(value: true);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(1, 0, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(1, 1, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(1, 2, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(1, 3, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(1, 4, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(1, 5, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(1, 6, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(2, 0, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(2, 1, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(0, 0, playsfx: false);
		(uiManager as CabinUIManager).phoneUI.notifSystem.LoadReply(0, 1, playsfx: false);
		stoolScript.darkSceneHintLight.SetActive(value: true);
	}
}
