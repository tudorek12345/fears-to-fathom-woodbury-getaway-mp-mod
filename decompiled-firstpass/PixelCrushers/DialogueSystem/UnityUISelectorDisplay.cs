using System;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class UnityUISelectorDisplay : MonoBehaviour
{
	[Serializable]
	public class AnimationTransitions
	{
		public string showTrigger = "Show";

		public string hideTrigger = "Hide";
	}

	public Graphic mainGraphic;

	public Text nameText;

	public Text useMessageText;

	public Color inRangeColor = Color.yellow;

	public Color outOfRangeColor = Color.gray;

	public Graphic reticleInRange;

	public Graphic reticleOutOfRange;

	public AnimationTransitions animationTransitions = new AnimationTransitions();

	private Selector selector;

	private ProximitySelector proximitySelector;

	private string defaultUseMessage = string.Empty;

	private Usable usable;

	private bool lastInRange;

	private UsableUnityUI usableUnityUI;

	private Animator animator;

	private bool previousUseDefaultGUI;

	private bool started;

	protected float CurrentDistance
	{
		get
		{
			if (!(selector != null))
			{
				return 0f;
			}
			return selector.CurrentDistance;
		}
	}

	private void Awake()
	{
		Tools.DeprecationWarning(this);
	}

	public void Start()
	{
		started = true;
		FindUIElements();
		ConnectDelegates();
		DeactivateControls();
	}

	public void FindUIElements()
	{
		UnityUISelectorElements unityUISelectorElements = UnityUISelectorElements.instance;
		if (unityUISelectorElements == null)
		{
			unityUISelectorElements = SearchForElements(DialogueManager.instance.transform);
		}
		if (unityUISelectorElements != null)
		{
			UnityUISelectorElements.instance = unityUISelectorElements;
		}
		if ((UnityEngine.Object)(object)mainGraphic == null && (UnityEngine.Object)(object)nameText == null && (UnityEngine.Object)(object)reticleInRange == null)
		{
			if (unityUISelectorElements == null)
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning("Dialogue System: UnityUISelectorDisplay can't find UI elements", this);
				}
			}
			else
			{
				if ((UnityEngine.Object)(object)mainGraphic == null)
				{
					mainGraphic = unityUISelectorElements.mainGraphic;
				}
				if ((UnityEngine.Object)(object)nameText == null)
				{
					nameText = unityUISelectorElements.nameText;
				}
				if ((UnityEngine.Object)(object)useMessageText == null)
				{
					useMessageText = unityUISelectorElements.useMessageText;
				}
				inRangeColor = unityUISelectorElements.inRangeColor;
				outOfRangeColor = unityUISelectorElements.outOfRangeColor;
				if ((UnityEngine.Object)(object)reticleInRange == null)
				{
					reticleInRange = unityUISelectorElements.reticleInRange;
				}
				if ((UnityEngine.Object)(object)reticleOutOfRange == null)
				{
					reticleOutOfRange = unityUISelectorElements.reticleOutOfRange;
				}
				animationTransitions = unityUISelectorElements.animationTransitions;
			}
		}
		if ((UnityEngine.Object)(object)mainGraphic != null)
		{
			animator = ((Component)(object)mainGraphic).GetComponentInChildren<Animator>();
		}
	}

	private UnityUISelectorElements SearchForElements(Transform t)
	{
		if (t == null)
		{
			return null;
		}
		UnityUISelectorElements component = t.GetComponent<UnityUISelectorElements>();
		if (component != null)
		{
			return component;
		}
		foreach (Transform item in t)
		{
			component = SearchForElements(item);
			if (component != null)
			{
				return component;
			}
		}
		return null;
	}

	public void OnEnable()
	{
		if (started)
		{
			ConnectDelegates();
		}
	}

	public void OnDisable()
	{
		DisconnectDelegates();
	}

	private void ConnectDelegates()
	{
		DisconnectDelegates();
		selector = GetComponent<Selector>();
		if (selector != null)
		{
			previousUseDefaultGUI = selector.useDefaultGUI;
			selector.useDefaultGUI = false;
			selector.SelectedUsableObject += OnSelectedUsable;
			selector.DeselectedUsableObject += OnDeselectedUsable;
			defaultUseMessage = selector.defaultUseMessage;
		}
		proximitySelector = GetComponent<ProximitySelector>();
		if (proximitySelector != null)
		{
			previousUseDefaultGUI = proximitySelector.useDefaultGUI;
			proximitySelector.useDefaultGUI = false;
			proximitySelector.SelectedUsableObject += OnSelectedUsable;
			proximitySelector.DeselectedUsableObject += OnDeselectedUsable;
			if (string.IsNullOrEmpty(defaultUseMessage))
			{
				defaultUseMessage = proximitySelector.defaultUseMessage;
			}
		}
	}

	private void DisconnectDelegates()
	{
		selector = GetComponent<Selector>();
		if (selector != null)
		{
			selector.useDefaultGUI = previousUseDefaultGUI;
			selector.SelectedUsableObject -= OnSelectedUsable;
			selector.DeselectedUsableObject -= OnDeselectedUsable;
		}
		proximitySelector = GetComponent<ProximitySelector>();
		if (proximitySelector != null)
		{
			proximitySelector.useDefaultGUI = previousUseDefaultGUI;
			proximitySelector.SelectedUsableObject -= OnSelectedUsable;
			proximitySelector.DeselectedUsableObject -= OnDeselectedUsable;
		}
		HideControls();
	}

	private void OnSelectedUsable(Usable usable)
	{
		this.usable = usable;
		usableUnityUI = ((usable != null) ? usable.GetComponentInChildren<UsableUnityUI>() : null);
		if (usableUnityUI != null)
		{
			usableUnityUI.Show(GetUseMessage());
		}
		else
		{
			ShowControls();
		}
		lastInRange = !IsUsableInRange();
		UpdateDisplay(!lastInRange);
	}

	private void OnDeselectedUsable(Usable usable)
	{
		if (usableUnityUI != null)
		{
			usableUnityUI.Hide();
			usableUnityUI = null;
		}
		else
		{
			HideControls();
		}
		this.usable = null;
	}

	private string GetUseMessage()
	{
		if (!string.IsNullOrEmpty(usable.overrideUseMessage))
		{
			return usable.overrideUseMessage;
		}
		return defaultUseMessage;
	}

	private void ShowControls()
	{
		if (!(usable == null))
		{
			Tools.SetGameObjectActive((Component)(object)mainGraphic, value: true);
			Tools.SetGameObjectActive((Component)(object)nameText, value: true);
			Tools.SetGameObjectActive((Component)(object)useMessageText, value: true);
			if ((UnityEngine.Object)(object)nameText != null)
			{
				nameText.text = usable.GetName();
			}
			if ((UnityEngine.Object)(object)useMessageText != null)
			{
				useMessageText.text = GetUseMessage();
			}
			if (CanTriggerAnimations() && !string.IsNullOrEmpty(animationTransitions.showTrigger))
			{
				animator.SetTrigger(animationTransitions.showTrigger);
			}
		}
	}

	private void HideControls()
	{
		if (CanTriggerAnimations() && !string.IsNullOrEmpty(animationTransitions.hideTrigger))
		{
			animator.SetTrigger(animationTransitions.hideTrigger);
		}
		else
		{
			DeactivateControls();
		}
	}

	private void DeactivateControls()
	{
		Tools.SetGameObjectActive((Component)(object)nameText, value: false);
		Tools.SetGameObjectActive((Component)(object)useMessageText, value: false);
		Tools.SetGameObjectActive((Component)(object)reticleInRange, value: false);
		Tools.SetGameObjectActive((Component)(object)reticleOutOfRange, value: false);
		Tools.SetGameObjectActive((Component)(object)mainGraphic, value: false);
	}

	private bool IsUsableInRange()
	{
		if (usable != null)
		{
			return CurrentDistance <= usable.maxUseDistance;
		}
		return false;
	}

	public void Update()
	{
		if (usable != null)
		{
			UpdateDisplay(IsUsableInRange());
		}
	}

	public void OnConversationStart(Transform actor)
	{
		HideControls();
	}

	public void OnConversationEnd(Transform actor)
	{
		ShowControls();
	}

	private void UpdateDisplay(bool inRange)
	{
		if (usable != null && inRange != lastInRange)
		{
			lastInRange = inRange;
			if (usableUnityUI != null)
			{
				usableUnityUI.UpdateDisplay(inRange);
				return;
			}
			UpdateText(inRange);
			UpdateReticle(inRange);
		}
	}

	private void UpdateText(bool inRange)
	{
		Color color = (inRange ? inRangeColor : outOfRangeColor);
		if ((UnityEngine.Object)(object)nameText != null)
		{
			((Graphic)nameText).color = color;
		}
		if ((UnityEngine.Object)(object)useMessageText != null)
		{
			((Graphic)useMessageText).color = color;
		}
	}

	private void UpdateReticle(bool inRange)
	{
		Tools.SetGameObjectActive((Component)(object)reticleInRange, inRange);
		Tools.SetGameObjectActive((Component)(object)reticleOutOfRange, !inRange);
	}

	private bool CanTriggerAnimations()
	{
		if (animator != null)
		{
			return animationTransitions != null;
		}
		return false;
	}
}
