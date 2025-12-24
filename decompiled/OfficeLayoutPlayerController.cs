using System;
using System.Collections;
using UnityEngine;

public class OfficeLayoutPlayerController : PlayerController
{
	[SerializeField]
	private OfficeLayoutUIManager officeLayoutUIManager;

	[SerializeField]
	private OfficeLayoutGameManager officeLayoutGameManager;

	[SerializeField]
	private Camera mainCamera;

	[SerializeField]
	private OfficeJanitor officeJanitor;

	[SerializeField]
	private OfficeWorker officeWorker;

	public Transform lookHereOfficeWorker;

	public Transform lookHereOfficeJanitor;

	private Transform lookHere;

	[SerializeField]
	private float cameraLerpSpeed = 3f;

	private float defaultZoom;

	[Tooltip("it is to animate the sip animation")]
	public Animator handAnimator;

	[Header("Player Sounds")]
	[SerializeField]
	private AudioClip sippingSound;

	private const string sip = "Sip";

	private bool madeCoffee;

	public int sipParam = Animator.StringToHash("Sip");

	private AudioSource currentAudioSource;

	[SerializeField]
	private int sipLimit = 5;

	public int currentSip;

	public bool sipping;

	public float delayTimer;

	private const float delayTime = 1f;

	[TextArea(3, 30)]
	[SerializeField]
	private string doneWithSipText;

	public Action DrankCoffeeEvent;

	[SerializeField]
	private GameObject hallwayRestroomSubTrigger;

	[SerializeField]
	private AllOfficeToiletsManager allOfficeToiletsManager;

	public GameObject phoneTrigger;

	public ComputerManager computerManager;

	public TableManager table;

	public AudioSource sipAS;

	public AudioClip[] sips;

	public float cupRotX;

	public bool canThrowItem = true;

	public bool playerhasRecentlyThrown;

	public Holdable lastThrowHoldable;

	private Coroutine recentThrowCoroutine;

	public float CameraLerpSpeed => cameraLerpSpeed;

	internal override void Awake()
	{
		base.Awake();
		currentSip = sipLimit;
		currentAudioSource = GetComponent<AudioSource>();
		officeLayoutUIManager.ClearControlsText();
		hallwayRestroomSubTrigger.SetActive(value: false);
		defaultZoom = mainCamera.fieldOfView;
		sipParam = Animator.StringToHash("Sip");
	}

	internal override void Start()
	{
		base.Start();
		handAnimator = handPosition.gameObject.GetComponent<Animator>();
	}

	internal override void Update()
	{
		base.Update();
		if (!firstPersonController.gameObject.activeSelf)
		{
			return;
		}
		if (officeLayoutGameManager.currentPlayerState == OfficeLayoutGameManager.PlayerState.Talking && officeJanitor.canTalk)
		{
			mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, 40f, Time.deltaTime * cameraLerpSpeed);
			Vector3 forward = lookHere.position - mainCamera.transform.position;
			mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, Quaternion.LookRotation(forward), Time.deltaTime * cameraLerpSpeed);
		}
		if (officeLayoutGameManager.currentPlayerState == OfficeLayoutGameManager.PlayerState.Talking && officeWorker.canTalk)
		{
			mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, 40f, Time.deltaTime * cameraLerpSpeed);
			Vector3 forward2 = lookHere.position - mainCamera.transform.position;
			mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, Quaternion.LookRotation(forward2), Time.deltaTime * cameraLerpSpeed);
		}
		if (currentHoldingObject != null)
		{
			ICoffeeSequenceItem component = currentHoldingObject.GetComponent<ICoffeeSequenceItem>();
			if (component != null && component.coffeeItem == ICoffeeSequenceItem.CoffeeItem.HasWater)
			{
				SwayTheCup();
			}
		}
		if (currentHoldingObject is CoffeeCup && !currentHoldingObject.GetComponent<CoffeeCup>().enabled)
		{
			return;
		}
		if (delayTimer > 0f)
		{
			delayTimer -= Time.deltaTime;
		}
		if (IsAnimationStatePlaying("CoffeeSip") && sipping && delayTimer <= 0f)
		{
			sipping = false;
			handAnimator.SetBool(sipParam, value: false);
			if (currentSip > 0)
			{
				officeLayoutUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "Sip"));
			}
			else
			{
				DrankCoffee();
			}
		}
	}

	private void DrankCoffee()
	{
		officeLayoutUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "Throw"));
		currentHoldingObject.GetComponent<ICoffeeSequenceItem>().coffeeItem = ICoffeeSequenceItem.CoffeeItem.DrankCoffee;
		firstPersonController.canZoomIn = true;
		if (DrankCoffeeEvent != null)
		{
			DrankCoffeeEvent();
		}
	}

	private void SwayTheCup()
	{
		if (cameraTransform.localEulerAngles.x > 330f || cameraTransform.localEulerAngles.x < 35f)
		{
			Vector3 eulerAngles = new Vector3(0f, currentHoldingObject.transform.eulerAngles.y, currentHoldingObject.transform.eulerAngles.z);
			currentHoldingObject.transform.eulerAngles = eulerAngles;
		}
		else if (cameraTransform.localEulerAngles.x < 330f && cameraTransform.localEulerAngles.x > 250f)
		{
			currentHoldingObject.transform.localEulerAngles = new Vector3(10.857828f, 335.79114f, 335.9093f);
		}
		else if (cameraTransform.localEulerAngles.x > 35f && cameraTransform.localEulerAngles.x < 100f)
		{
			currentHoldingObject.transform.localEulerAngles = new Vector3(60.540413f, 302.35193f, 316.659f);
		}
	}

	internal override void OnZoom()
	{
		base.OnZoom();
		if (firstPersonController.gameObject.activeSelf && (!(currentHoldingObject is CoffeeCup) || currentHoldingObject.GetComponent<CoffeeCup>().enabled))
		{
			if (madeCoffee && !handAnimator.GetBool(sipParam) && currentSip > 0 && !sipping)
			{
				delayTimer = 1f;
				sipping = true;
				currentSip--;
				officeLayoutUIManager.ClearControlsText();
				handAnimator.SetBool(sipParam, value: true);
				sipAS.clip = sips[currentSip];
				sipAS.Play();
			}
			if (currentHoldingObject != null && currentHoldingObject.GetComponent<ICoffeeSequenceItem>() != null && currentHoldingObject.GetComponent<ICoffeeSequenceItem>().coffeeItem == ICoffeeSequenceItem.CoffeeItem.DrankCoffee)
			{
				SubTextManager.GetInstance().ShowSubText(F2FLocalizedText.GetLocalizedText("ep5_subs", doneWithSipText));
			}
		}
	}

	private bool IsAnimationStatePlaying(string stateName)
	{
		int layerIndex = 0;
		AnimatorStateInfo currentAnimatorStateInfo = handAnimator.GetCurrentAnimatorStateInfo(layerIndex);
		if (currentAnimatorStateInfo.IsName(stateName))
		{
			return currentAnimatorStateInfo.normalizedTime >= 1f;
		}
		return false;
	}

	internal override void Throw()
	{
		if (!canThrowItem || !firstPersonController.gameObject.activeSelf || !(currentHoldingObject != null))
		{
			return;
		}
		if ((bool)currentHoldingObject.GetComponent<SodaPickable>())
		{
			if (!currentHoldingObject.GetComponent<SodaPickable>().Sipping)
			{
				lastThrowHoldable = currentHoldingObject;
				base.Throw();
				officeLayoutUIManager.ClearControlsText();
				officeLayoutUIManager.ResetRememberedText();
				if (recentThrowCoroutine != null)
				{
					StopCoroutine(recentThrowCoroutine);
				}
				recentThrowCoroutine = StartCoroutine(EnableRecentThrowBoolForThreeSeconds());
			}
			return;
		}
		CoffeeCup component = currentHoldingObject.GetComponent<CoffeeCup>();
		if (component != null)
		{
			if (component.coffeeItem != ICoffeeSequenceItem.CoffeeItem.HasLid)
			{
				lastThrowHoldable = currentHoldingObject;
				base.Throw();
				officeLayoutUIManager.ClearControlsText();
				officeLayoutUIManager.ResetRememberedText();
				if (recentThrowCoroutine != null)
				{
					StopCoroutine(recentThrowCoroutine);
				}
				recentThrowCoroutine = StartCoroutine(EnableRecentThrowBoolForThreeSeconds());
			}
		}
		else
		{
			lastThrowHoldable = currentHoldingObject;
			base.Throw();
			officeLayoutUIManager.ClearControlsText();
			officeLayoutUIManager.ResetRememberedText();
			if (recentThrowCoroutine != null)
			{
				StopCoroutine(recentThrowCoroutine);
			}
			recentThrowCoroutine = StartCoroutine(EnableRecentThrowBoolForThreeSeconds());
		}
	}

	private IEnumerator EnableRecentThrowBoolForThreeSeconds()
	{
		playerhasRecentlyThrown = true;
		yield return new WaitForSeconds(3f);
		playerhasRecentlyThrown = false;
		lastThrowHoldable = null;
	}

	internal override void HoldObject(Holdable holdable, bool isThrowable = false)
	{
		if (!madeCoffee)
		{
			ICoffeeSequenceItem component = holdable.GetComponent<ICoffeeSequenceItem>();
			if (component != null && component.coffeeItem == ICoffeeSequenceItem.CoffeeItem.HasLid)
			{
				officeLayoutUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "Sip"));
				madeCoffee = true;
				firstPersonController.canZoomIn = false;
				computerManager.coffeeDone = true;
				table.coffeeDone = true;
				hallwayRestroomSubTrigger.SetActive(value: true);
				allOfficeToiletsManager.EnableAllToilets();
			}
			else
			{
				officeLayoutUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "Throw"));
			}
		}
		if (!(holdable is SodaPickable))
		{
			CoffeeCup coffeeCup = holdable as CoffeeCup;
			if (coffeeCup != null && coffeeCup.coffeeItem == ICoffeeSequenceItem.CoffeeItem.HasLid)
			{
				base.HoldObject(holdable);
			}
			else
			{
				base.HoldObject(holdable, isThrowable: true);
			}
		}
		else
		{
			base.HoldObject(holdable);
		}
	}

	public void LookHereJanitor()
	{
		lookHere = lookHereOfficeJanitor;
	}

	public void LookHereOfficeWorker()
	{
		lookHere = lookHereOfficeWorker;
	}

	public void ResetToDefaultZoom()
	{
		mainCamera.fieldOfView = defaultZoom;
	}

	public void StartTalking()
	{
		officeLayoutGameManager.ChangePlayerState(OfficeLayoutGameManager.PlayerState.Talking);
	}

	public void ResetCoffeeSipping()
	{
		if (sipping)
		{
			handAnimator.SetBool(sipParam, value: false);
			sipping = false;
			delayTimer = 0f;
			if (currentSip <= 0)
			{
				currentSip = 1;
			}
			officeLayoutUIManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "Sip"));
		}
	}
}
