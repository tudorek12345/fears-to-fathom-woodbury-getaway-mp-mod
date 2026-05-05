using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class SelectorUseStandardUIElements : MonoBehaviour
{
	[Serializable]
	public class TagInfo
	{
		[Tooltip("Use the UI elements below for usables with this tag. Tags take precedence over layers.")]
		public string tag;

		public string defaultUseMessage;

		public StandardUISelectorElements UIElements;
	}

	[Serializable]
	public class LayerInfo
	{
		[Tooltip("Use the UI elements below for usables in these layers.")]
		public LayerMask layerMask;

		public string defaultUseMessage;

		public StandardUISelectorElements UIElements;
	}

	public List<TagInfo> tagSpecificElements = new List<TagInfo>();

	public List<LayerInfo> layerSpecificElements = new List<LayerInfo>();

	private Selector selector;

	private ProximitySelector proximitySelector;

	private string defaultUseMessage = string.Empty;

	private Usable usable;

	private bool lastInRange;

	private AbstractUsableUI usableUI;

	private bool started;

	private string originalDefaultUseMessage;

	private bool previousUseDefaultGUI;

	private StandardUISelectorElements m_elements;

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

	public StandardUISelectorElements elements
	{
		get
		{
			return m_elements;
		}
		protected set
		{
			m_elements = value;
		}
	}

	private void Start()
	{
		if (StandardUISelectorElements.instances.Count == 0)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning("Dialogue System: SelectorUseStandardUIElements can't find a StandardUISelectorElements component in the scene.", this);
			}
			base.enabled = false;
			return;
		}
		started = true;
		ConnectDelegates();
		for (int num = StandardUISelectorElements.instances.Count - 1; num >= 0; num--)
		{
			elements = StandardUISelectorElements.instances[num];
			if (elements != null)
			{
				DeactivateControls();
			}
		}
	}

	private void OnEnable()
	{
		if (started)
		{
			ConnectDelegates();
		}
	}

	private void OnDisable()
	{
		DisconnectDelegates();
	}

	public void ConnectDelegates()
	{
		DisconnectDelegates();
		selector = GetComponent<Selector>();
		if (selector != null)
		{
			previousUseDefaultGUI = selector.useDefaultGUI;
			selector.useDefaultGUI = false;
			selector.Enabled += OnSelectorEnabled;
			selector.Disabled += OnSelectorDisabled;
			selector.SelectedUsableObject += OnSelectedUsable;
			selector.DeselectedUsableObject += OnDeselectedUsable;
			defaultUseMessage = selector.defaultUseMessage;
		}
		proximitySelector = GetComponent<ProximitySelector>();
		if (proximitySelector != null)
		{
			previousUseDefaultGUI = proximitySelector.useDefaultGUI;
			proximitySelector.useDefaultGUI = false;
			proximitySelector.Enabled += OnSelectorEnabled;
			proximitySelector.Disabled += OnSelectorDisabled;
			proximitySelector.SelectedUsableObject += OnSelectedUsable;
			proximitySelector.DeselectedUsableObject += OnDeselectedUsable;
			defaultUseMessage = proximitySelector.defaultUseMessage;
		}
		originalDefaultUseMessage = defaultUseMessage;
	}

	public void DisconnectDelegates()
	{
		selector = GetComponent<Selector>();
		if (selector != null)
		{
			selector.useDefaultGUI = previousUseDefaultGUI;
			selector.Enabled -= OnSelectorEnabled;
			selector.Disabled -= OnSelectorDisabled;
			selector.SelectedUsableObject -= OnSelectedUsable;
			selector.DeselectedUsableObject -= OnDeselectedUsable;
		}
		proximitySelector = GetComponent<ProximitySelector>();
		if (proximitySelector != null)
		{
			proximitySelector.useDefaultGUI = previousUseDefaultGUI;
			proximitySelector.Enabled -= OnSelectorEnabled;
			proximitySelector.Disabled -= OnSelectorDisabled;
			proximitySelector.SelectedUsableObject -= OnSelectedUsable;
			proximitySelector.DeselectedUsableObject -= OnDeselectedUsable;
		}
		HideControls();
	}

	private void SetElementsForUsable(Usable usable)
	{
		for (int i = 0; i < tagSpecificElements.Count; i++)
		{
			TagInfo tagInfo = tagSpecificElements[i];
			if (usable != null && usable.CompareTag(tagInfo.tag))
			{
				defaultUseMessage = tagInfo.defaultUseMessage;
				elements = tagInfo.UIElements ?? StandardUISelectorElements.instance;
				return;
			}
		}
		for (int j = 0; j < layerSpecificElements.Count; j++)
		{
			LayerInfo layerInfo = layerSpecificElements[j];
			if (usable != null && ((1 << usable.gameObject.layer) & layerInfo.layerMask.value) != 0)
			{
				defaultUseMessage = layerInfo.defaultUseMessage;
				elements = layerInfo.UIElements ?? StandardUISelectorElements.instance;
				return;
			}
		}
		defaultUseMessage = originalDefaultUseMessage;
		if (layerSpecificElements.Count > 0 || tagSpecificElements.Count > 0)
		{
			for (int k = 0; k < StandardUISelectorElements.instances.Count; k++)
			{
				StandardUISelectorElements instance = StandardUISelectorElements.instances[k];
				if (layerSpecificElements.Find((LayerInfo x) => x.UIElements == instance) == null && tagSpecificElements.Find((TagInfo x) => x.UIElements == instance) == null)
				{
					elements = instance;
					return;
				}
			}
		}
		elements = StandardUISelectorElements.instance;
	}

	private void OnSelectedUsable(Usable usable)
	{
		this.usable = usable;
		if (usableUI != null)
		{
			usableUI.Hide();
		}
		usableUI = ((usable != null) ? usable.GetComponentInChildren<AbstractUsableUI>() : null);
		if (usableUI != null)
		{
			usableUI.Show(GetUseMessage());
			HideControls();
		}
		else
		{
			StandardUISelectorElements standardUISelectorElements = elements;
			SetElementsForUsable(usable);
			if (standardUISelectorElements != elements)
			{
				StandardUISelectorElements standardUISelectorElements2 = elements;
				elements = standardUISelectorElements;
				HideControls();
				elements = standardUISelectorElements2;
			}
			ShowControls();
		}
		lastInRange = !IsUsableInRange();
		UpdateDisplay(!lastInRange);
	}

	private void OnDeselectedUsable(Usable usable)
	{
		if (usableUI != null)
		{
			usableUI.Hide();
			usableUI = null;
		}
		HideControls();
		this.usable = null;
	}

	private string GetUseMessage()
	{
		return DialogueManager.GetLocalizedText(string.IsNullOrEmpty(usable.overrideUseMessage) ? defaultUseMessage : usable.overrideUseMessage);
	}

	private void ShowControls()
	{
		if (!(usable == null) && !(elements == null))
		{
			Tools.SetGameObjectActive((Component)(object)elements.mainGraphic, value: true);
			elements.nameText.SetActive(value: true);
			elements.useMessageText.SetActive(value: true);
			elements.nameText.text = usable.GetName();
			elements.useMessageText.text = GetUseMessage();
			Tools.SetGameObjectActive((Component)(object)elements.reticleInRange, IsUsableInRange());
			Tools.SetGameObjectActive((Component)(object)elements.reticleOutOfRange, !IsUsableInRange());
			if (CanTriggerAnimations() && !string.IsNullOrEmpty(elements.animationTransitions.showTrigger))
			{
				elements.animator.ResetTrigger(elements.animationTransitions.hideTrigger);
				elements.animator.SetTrigger(elements.animationTransitions.showTrigger);
			}
		}
	}

	private void HideControls()
	{
		if (CanTriggerAnimations() && elements != null && !string.IsNullOrEmpty(elements.animationTransitions.hideTrigger))
		{
			elements.animator.ResetTrigger(elements.animationTransitions.showTrigger);
			elements.animator.SetTrigger(elements.animationTransitions.hideTrigger);
		}
		else
		{
			DeactivateControls();
		}
	}

	private void DeactivateControls()
	{
		if (!(elements == null))
		{
			elements.nameText.SetActive(value: false);
			elements.useMessageText.SetActive(value: false);
			Tools.SetGameObjectActive((Component)(object)elements.reticleInRange, value: false);
			Tools.SetGameObjectActive((Component)(object)elements.reticleOutOfRange, value: false);
			Tools.SetGameObjectActive((Component)(object)elements.mainGraphic, value: false);
		}
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

	protected void OnSelectorEnabled()
	{
		ShowControlsOrUsableUI();
	}

	protected void OnSelectorDisabled()
	{
		HideControls();
	}

	public void OnConversationStart(Transform actor)
	{
		HideControls();
	}

	public void OnConversationEnd(Transform actor)
	{
		ShowControlsOrUsableUI();
	}

	protected void ShowControlsOrUsableUI()
	{
		if (usableUI != null)
		{
			usableUI.Show(GetUseMessage());
		}
		else
		{
			ShowControls();
		}
	}

	private void UpdateDisplay(bool inRange)
	{
		if (usable != null && inRange != lastInRange)
		{
			lastInRange = inRange;
			if (usableUI != null)
			{
				usableUI.UpdateDisplay(inRange);
				return;
			}
			UpdateText(inRange);
			UpdateReticle(inRange);
		}
	}

	private void UpdateText(bool inRange)
	{
		if (!(elements == null) && elements.useRangeColors)
		{
			Color color = (inRange ? elements.inRangeColor : elements.outOfRangeColor);
			if (elements.nameText != null)
			{
				elements.nameText.color = color;
			}
			if (elements.useMessageText != null)
			{
				elements.useMessageText.color = color;
			}
		}
	}

	private void UpdateReticle(bool inRange)
	{
		if (!(elements == null))
		{
			Tools.SetGameObjectActive((Component)(object)elements.reticleInRange, inRange);
			Tools.SetGameObjectActive((Component)(object)elements.reticleOutOfRange, !inRange);
		}
	}

	private bool CanTriggerAnimations()
	{
		if (elements != null && elements.animator != null)
		{
			return elements.animationTransitions != null;
		}
		return false;
	}
}
