using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PixelCrushers;

[AddComponentMenu("")]
public class UIPanel : MonoBehaviour, IEventSystemUser
{
	public enum StartState
	{
		GameObjectState,
		Open,
		Closed
	}

	public enum PanelState
	{
		Uninitialized,
		Opening,
		Open,
		Closing,
		Closed
	}

	[Tooltip("When enabling the panel, select this if input device is Joystick or Keyboard.")]
	public GameObject firstSelected;

	[Tooltip("If non-zero, seconds between checks to ensure that one of the panel's Selectables is focused when this panel is active and on top.")]
	public float focusCheckFrequency = 0.2f;

	[Tooltip("If non-zero, refresh list of Selectables at this frequency when this panel is active and on top. Use if Selectables are added dynamically.")]
	public float refreshSelectablesFrequency;

	[Tooltip("Reselect previous selectable when disabling this panel.")]
	public bool selectPreviousOnDisable = true;

	[Tooltip("When opening, set this animator trigger.")]
	public string showAnimationTrigger = "Show";

	[Tooltip("When closing, set this animator trigger.")]
	public string hideAnimationTrigger = "Hide";

	[Tooltip("Normally the panel considers itself open at start if the GameObject starts active (GameObjectState). To explicitly specify whether the panel should start open or closed, select Open or Closed from the dropdown.")]
	public StartState startState;

	[Tooltip("Do not set panel state to Open until Show animation has finished.")]
	public bool waitForShowAnimationToSetOpen;

	[Tooltip("Deactivate panel GameObject when panel is closed.")]
	[SerializeField]
	protected bool m_deactivateOnHidden = true;

	public UnityEvent onOpen = new UnityEvent();

	public UnityEvent onClose = new UnityEvent();

	public UnityEvent onClosed = new UnityEvent();

	public UnityEvent onBackButtonDown = new UnityEvent();

	protected GameObject m_previousSelected;

	protected GameObject m_lastSelected;

	protected List<GameObject> selectables = new List<GameObject>();

	private float m_timeNextCheck;

	private float m_timeNextRefresh;

	private int m_frameLastOpened = -1;

	public static bool monitorSelection = true;

	protected static List<UIPanel> panelStack = new List<UIPanel>();

	private PanelState m_panelState;

	private UIAnimatorMonitor m_animatorMonitor;

	private Animator m_animator;

	private EventSystem m_eventSystem;

	public bool deactivateOnHidden
	{
		get
		{
			return m_deactivateOnHidden;
		}
		set
		{
			m_deactivateOnHidden = value;
		}
	}

	protected static UIPanel topPanel
	{
		get
		{
			if (panelStack.Count <= 0)
			{
				return null;
			}
			return panelStack[panelStack.Count - 1];
		}
	}

	public PanelState panelState
	{
		get
		{
			return m_panelState;
		}
		set
		{
			m_panelState = value;
		}
	}

	public virtual bool waitForShowAnimation
	{
		get
		{
			return waitForShowAnimationToSetOpen;
		}
		set
		{
			waitForShowAnimationToSetOpen = value;
		}
	}

	public bool isOpen
	{
		get
		{
			if (panelState != PanelState.Opening && panelState != PanelState.Open)
			{
				if (panelState == PanelState.Uninitialized)
				{
					return base.gameObject.activeInHierarchy;
				}
				return false;
			}
			return true;
		}
	}

	public UIAnimatorMonitor animatorMonitor
	{
		get
		{
			if (m_animatorMonitor == null)
			{
				m_animatorMonitor = new UIAnimatorMonitor(base.gameObject);
			}
			return m_animatorMonitor;
		}
	}

	private Animator myAnimator
	{
		get
		{
			if (m_animator == null)
			{
				m_animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
			}
			return m_animator;
		}
	}

	public EventSystem eventSystem
	{
		get
		{
			if ((Object)(object)m_eventSystem != null)
			{
				return m_eventSystem;
			}
			return EventSystem.current;
		}
		set
		{
			m_eventSystem = value;
		}
	}

	protected virtual void Start()
	{
		if (panelState != PanelState.Uninitialized)
		{
			return;
		}
		switch (startState)
		{
		case StartState.Open:
			panelState = PanelState.Opening;
			base.gameObject.SetActive(value: true);
			RefreshSelectablesList();
			animatorMonitor.SetTrigger(showAnimationTrigger, OnVisible, wait: false);
			return;
		case StartState.Closed:
			panelState = PanelState.Closed;
			if (deactivateOnHidden)
			{
				base.gameObject.SetActive(value: false);
			}
			return;
		}
		if (base.gameObject.activeInHierarchy)
		{
			panelState = PanelState.Opening;
			RefreshSelectablesList();
			animatorMonitor.SetTrigger(showAnimationTrigger, OnVisible, wait: false);
		}
		else
		{
			panelState = PanelState.Closed;
		}
	}

	public void RefreshSelectablesList()
	{
		selectables.Clear();
		Selectable[] componentsInChildren = GetComponentsInChildren<Selectable>();
		foreach (Selectable val in componentsInChildren)
		{
			if (((UIBehaviour)val).IsActive() && val.IsInteractable())
			{
				selectables.Add(((Component)(object)val).gameObject);
			}
		}
	}

	public void RefreshAfterOneFrame()
	{
		StartCoroutine(RefreshAfterOneFrameCoroutine());
	}

	private IEnumerator RefreshAfterOneFrameCoroutine()
	{
		yield return null;
		RefreshSelectablesList();
	}

	protected void PushToPanelStack()
	{
		if (panelStack.Contains(this))
		{
			panelStack.Remove(this);
		}
		panelStack.Add(this);
	}

	protected void PopFromPanelStack()
	{
		panelStack.Remove(this);
	}

	public void TakeFocus()
	{
		PushToPanelStack();
		RefreshSelectablesList();
		CheckFocus();
	}

	protected virtual void OnEnable()
	{
		PushToPanelStack();
		RefreshAfterOneFrame();
	}

	protected virtual void OnDisable()
	{
		StopAllCoroutines();
		if (monitorSelection && selectPreviousOnDisable && InputDeviceManager.autoFocus && (Object)(object)eventSystem != null && m_previousSelected != null && !selectables.Contains(m_previousSelected))
		{
			eventSystem.SetSelectedGameObject(m_previousSelected);
		}
		PopFromPanelStack();
	}

	public virtual void Open()
	{
		if (panelState != PanelState.Open && panelState != PanelState.Opening)
		{
			if (panelState == PanelState.Closing)
			{
				animatorMonitor.CancelCurrentAnimation();
			}
			m_frameLastOpened = Time.frameCount;
			panelState = PanelState.Opening;
			base.gameObject.SetActive(value: true);
			onOpen.Invoke();
			if (myAnimator != null && myAnimator.isInitialized && !string.IsNullOrEmpty(hideAnimationTrigger))
			{
				myAnimator.ResetTrigger(hideAnimationTrigger);
			}
			animatorMonitor.SetTrigger(showAnimationTrigger, OnVisible, waitForShowAnimation);
			PushToPanelStack();
		}
	}

	public virtual void Close()
	{
		PopFromPanelStack();
		if (base.gameObject == null)
		{
			return;
		}
		if (base.gameObject.activeInHierarchy)
		{
			CancelInvoke();
		}
		if (panelState != PanelState.Closed && panelState != PanelState.Closing)
		{
			panelState = PanelState.Closing;
			onClose.Invoke();
			if (myAnimator != null && myAnimator.isInitialized && !string.IsNullOrEmpty(showAnimationTrigger))
			{
				myAnimator.ResetTrigger(showAnimationTrigger);
			}
			animatorMonitor.SetTrigger(hideAnimationTrigger, OnHidden);
			if ((Object)(object)eventSystem != null && selectables.Contains(eventSystem.currentSelectedGameObject))
			{
				eventSystem.SetSelectedGameObject((GameObject)null);
			}
		}
	}

	public virtual void SetOpen(bool value)
	{
		if (value)
		{
			Open();
		}
		else
		{
			Close();
		}
	}

	public virtual void Toggle()
	{
		if (isOpen)
		{
			Close();
		}
		else
		{
			Open();
		}
	}

	protected virtual void OnVisible()
	{
		panelState = PanelState.Open;
		RefreshSelectablesList();
		m_previousSelected = (((Object)(object)eventSystem != null) ? eventSystem.currentSelectedGameObject : null);
		if (InputDeviceManager.autoFocus && firstSelected != null && m_previousSelected != null && !selectables.Contains(m_previousSelected))
		{
			Selectable component = m_previousSelected.GetComponent<Selectable>();
			if ((Object)(object)component != null)
			{
				component.OnDeselect((BaseEventData)null);
			}
		}
	}

	protected virtual void OnHidden()
	{
		panelState = PanelState.Closed;
		if (deactivateOnHidden)
		{
			base.gameObject.SetActive(value: false);
		}
		onClosed.Invoke();
	}

	protected virtual void Update()
	{
		if (!isOpen || !(topPanel == this))
		{
			return;
		}
		if (InputDeviceManager.isBackButtonDown)
		{
			if (Time.frameCount != m_frameLastOpened)
			{
				onBackButtonDown.Invoke();
			}
			return;
		}
		EventSystem val = eventSystem;
		if ((Object)(object)val != null)
		{
			GameObject currentSelectedGameObject = val.currentSelectedGameObject;
			if (currentSelectedGameObject != null && selectables.Contains(currentSelectedGameObject))
			{
				m_lastSelected = currentSelectedGameObject;
			}
			if (Time.realtimeSinceStartup >= m_timeNextCheck && focusCheckFrequency > 0f && topPanel == this && InputDeviceManager.autoFocus)
			{
				m_timeNextCheck = Time.realtimeSinceStartup + focusCheckFrequency;
				CheckFocus();
			}
			if (Time.realtimeSinceStartup >= m_timeNextRefresh && refreshSelectablesFrequency > 0f && topPanel == this && InputDeviceManager.autoFocus)
			{
				m_timeNextRefresh = Time.realtimeSinceStartup + refreshSelectablesFrequency;
				RefreshSelectablesList();
			}
		}
	}

	public virtual void SetFocus(GameObject selectable)
	{
		firstSelected = null;
		m_lastSelected = selectable;
		if (InputDeviceManager.autoFocus)
		{
			if ((Object)(object)eventSystem != null)
			{
				eventSystem.SetSelectedGameObject((GameObject)null);
			}
			if (m_lastSelected != null)
			{
				UIUtility.Select(m_lastSelected.GetComponent<Selectable>(), allowStealFocus: true, eventSystem);
			}
			CheckFocus();
		}
		else if ((Object)(object)eventSystem != null)
		{
			Selectable val = ((selectable != null) ? selectable.GetComponent<Selectable>() : null);
			if ((Object)(object)val != null)
			{
				UIUtility.Select(val, allowStealFocus: true, eventSystem);
			}
			else
			{
				eventSystem.SetSelectedGameObject(selectable);
			}
		}
	}

	public virtual void CheckFocus()
	{
		if (!monitorSelection || !InputDeviceManager.autoFocus || (Object)(object)eventSystem == null || topPanel != this)
		{
			return;
		}
		GameObject currentSelectedGameObject = eventSystem.currentSelectedGameObject;
		if (currentSelectedGameObject == null || !selectables.Contains(currentSelectedGameObject))
		{
			GameObject gameObject = null;
			if (m_lastSelected != null && selectables.Contains(m_lastSelected))
			{
				gameObject = m_lastSelected;
			}
			else
			{
				Selectable val = ((firstSelected != null) ? firstSelected.GetComponent<Selectable>() : null);
				gameObject = (((Object)(object)val != null && ((UIBehaviour)val).IsActive() && val.IsInteractable()) ? firstSelected : GetFirstInteractableButton());
			}
			if (gameObject != null)
			{
				eventSystem.SetSelectedGameObject(gameObject);
			}
		}
	}

	protected GameObject GetFirstInteractableButton()
	{
		Selectable[] componentsInChildren = GetComponentsInChildren<Selectable>();
		foreach (Selectable val in componentsInChildren)
		{
			if (val.interactable)
			{
				return ((Component)(object)val).gameObject;
			}
		}
		return null;
	}
}
