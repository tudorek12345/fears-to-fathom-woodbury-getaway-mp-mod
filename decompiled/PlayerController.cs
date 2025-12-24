using System;
using UnityEngine;

public abstract class PlayerController : MonoBehaviour
{
	private static PlayerController instance;

	[Header("Script References")]
	[SerializeField]
	private interactableObjectUI interactObject;

	[SerializeField]
	private InputManager inputManager;

	[SerializeField]
	internal FirstPersonController firstPersonController;

	[SerializeField]
	private UIManager uiManager;

	[Header("Transforms and other stuff")]
	[SerializeField]
	internal Transform handPosition;

	[SerializeField]
	internal Transform handPositionFlashLight;

	[SerializeField]
	internal Transform cameraTransform;

	internal bool isPaused;

	internal Iinteractable objectToInteract;

	[SerializeField]
	internal Holdable currentHoldingObject;

	[SerializeField]
	internal Holdable currentHoldingObjectLeft;

	private const float clickSpamTime = 0.1f;

	private float clickSpamTimer = 0.1f;

	public event Action OnHoldObject;

	public event Action OnThrowObject;

	public static PlayerController GetInstance()
	{
		return instance;
	}

	public bool IsPlayerPaused()
	{
		return isPaused;
	}

	public Holdable GetHoldingObject()
	{
		return currentHoldingObject;
	}

	public Holdable GetHoldingObjectLeft()
	{
		return currentHoldingObjectLeft;
	}

	internal virtual void Awake()
	{
		instance = this;
	}

	internal virtual void Start()
	{
		isPaused = true;
	}

	internal virtual void OnEnable()
	{
		interactObject.FoundInteractObject += FoundInteractObject;
		inputManager.OnInteract += Interact;
		inputManager.OnThrow += Throw;
		inputManager.OnZoom += OnZoom;
	}

	internal virtual void OnDisable()
	{
		interactObject.FoundInteractObject -= FoundInteractObject;
		inputManager.OnInteract -= Interact;
		inputManager.OnThrow -= Throw;
		inputManager.OnZoom -= OnZoom;
	}

	internal virtual void Update()
	{
		if (clickSpamTimer > 0f)
		{
			clickSpamTimer -= Time.deltaTime;
		}
	}

	internal virtual void OnZoom()
	{
	}

	internal virtual void StartPlayer()
	{
		isPaused = false;
	}

	private void FoundInteractObject(Iinteractable iinteractable)
	{
		objectToInteract = iinteractable;
	}

	private void Interact()
	{
		if (!uiManager.inCoversation && (!(uiManager.phoneUI != null) || !uiManager.phoneUI.isPaused) && objectToInteract != null && clickSpamTimer <= 0f)
		{
			clickSpamTimer = 0.1f;
			objectToInteract.Clicked(delegate
			{
				currentHoldingObject = null;
			});
		}
	}

	internal virtual void RemoveHandObject()
	{
		uiManager.ClearControlsText();
		currentHoldingObject = null;
	}

	internal virtual void Throw()
	{
		if (currentHoldingObjectLeft != null)
		{
			if (currentHoldingObjectLeft.GetComponent<Bait>() != null)
			{
				firstPersonController.canZoomIn = true;
				(this as CabinPlayerController).lockCameraMovement.disableFov = false;
			}
			currentHoldingObjectLeft.Throw(cameraTransform);
			currentHoldingObjectLeft = null;
			uiManager.ClearControlsText();
		}
		else
		{
			if (!(currentHoldingObject != null))
			{
				return;
			}
			if (currentHoldingObject is FishingRodPickable)
			{
				if (!(this as CabinPlayerController).fishingRod.lureReached && (this as CabinPlayerController).fishingRod.canCast && !(this as CabinPlayerController).fishingRod.isKinematic)
				{
					(this as CabinPlayerController).ThrowRod();
					uiManager.ClearControlsText();
					uiManager.ClearStoredControlsText();
				}
			}
			else
			{
				currentHoldingObject.Throw(cameraTransform);
				currentHoldingObject = null;
				uiManager.ClearControlsText();
			}
			this.OnThrowObject?.Invoke();
		}
	}

	internal virtual void HoldObject(Holdable holdable, bool isThrowable = false)
	{
		if (currentHoldingObject == null)
		{
			currentHoldingObject = holdable;
			if (currentHoldingObject is FishingRodPickable)
			{
				uiManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "Cast"));
				uiManager.StoreControlsText();
			}
			holdable.GoToPosition(handPosition);
			if (isThrowable)
			{
				Debug.Log("Throw Controls Activated");
				uiManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "Throw"));
			}
			this.OnHoldObject?.Invoke();
		}
	}

	internal virtual void HoldObjectLeft(Holdable holdable, bool isThrowable = false)
	{
		if (currentHoldingObjectLeft == null)
		{
			currentHoldingObjectLeft = holdable;
			holdable.GoToPosition(handPosition);
			if (isThrowable && holdable.GetComponent<Bait>() == null)
			{
				uiManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "Throw"));
			}
			else if (isThrowable && holdable.GetComponent<Bait>() != null)
			{
				Debug.Log("ThowBait");
				uiManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "ThowBait"));
			}
		}
		if (currentHoldingObjectLeft is Bait)
		{
			uiManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "ThowBait"));
			uiManager.StoreControlsText();
		}
	}

	internal virtual void HoldObjectFlashLight(Holdable holdable, bool isThrowable = false, bool controlText = true)
	{
		if (currentHoldingObjectLeft == null)
		{
			currentHoldingObjectLeft = holdable;
			holdable.GoToPosition(handPositionFlashLight);
			if (isThrowable && controlText)
			{
				uiManager.ShowControlsText(F2FLocalizedText.GetLocalizedText("ep5_controls", "ThrowUse"));
			}
		}
	}

	internal virtual void StartPlayerTransition()
	{
	}

	internal virtual void CompletedPlayerTransition()
	{
	}
}
