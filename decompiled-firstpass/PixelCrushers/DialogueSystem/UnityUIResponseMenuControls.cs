using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class UnityUIResponseMenuControls : AbstractUIResponseMenuControls
{
	[Tooltip("The panel containing the response menu controls. A panel is optional, but you may want one so you can include a background image, panel-wide effects, etc.")]
	public Graphic panel;

	[Tooltip("The PC portrait image to show during the response menu.")]
	public Image pcImage;

	[Tooltip("The label that will show the PC name.")]
	public Text pcName;

	[Tooltip("The reminder of the last subtitle.")]
	public UnityUISubtitleControls subtitleReminder;

	[Tooltip("The (optional) timer.")]
	public Slider timer;

	[Tooltip("Select the currently-focused response on timeout.")]
	public bool selectCurrentOnTimeout;

	[Tooltip("Design-time positioned response buttons")]
	public UnityUIResponseButton[] buttons;

	[Tooltip("Template from which to instantiate response buttons; optional to use instead of positioned buttons above")]
	public UnityUIResponseButton buttonTemplate;

	[Tooltip("If using Button Template, instantiated buttons are parented under this GameObject")]
	public Graphic buttonTemplateHolder;

	[Tooltip("Optional scrollbar if the instantiated button holder is in a scroll rect")]
	public Scrollbar buttonTemplateScrollbar;

	[Tooltip("Reset the scroll bar to this value when preparing the response menu")]
	public float buttonTemplateScrollbarResetValue = 1f;

	[Tooltip("Automatically set up explicit navigation for instantiated template buttons instead of using Automatic navigation")]
	public bool explicitNavigationForTemplateButtons = true;

	[Tooltip("If explicit navigation is enabled, loop around when navigating past end of menu")]
	public bool loopExplicitNavigation;

	public UIAutonumberSettings autonumber = new UIAutonumberSettings();

	public UIAnimationTransitions animationTransitions = new UIAnimationTransitions();

	public UnityEvent onContentChanged = new UnityEvent();

	[HideInInspector]
	public List<GameObject> instantiatedButtons = new List<GameObject>();

	public Action TimeoutHandler;

	private UIShowHideController m_showHideController;

	private UnityUITimer unityUITimer;

	private Sprite pcPortraitSprite;

	private string pcPortraitName;

	private Animator animator;

	private bool lookedForAnimator;

	public bool isVisible
	{
		get
		{
			if (!((UnityEngine.Object)(object)panel != null))
			{
				return false;
			}
			return ((Component)(object)panel).gameObject.activeInHierarchy;
		}
	}

	public UIShowHideController showHideController
	{
		get
		{
			if (m_showHideController == null)
			{
				m_showHideController = new UIShowHideController(null, (Component)(object)panel, animationTransitions.transitionMode, animationTransitions.debug);
				m_showHideController.state = UIShowHideController.State.Hidden;
			}
			return m_showHideController;
		}
	}

	public override AbstractUISubtitleControls subtitleReminderControls => subtitleReminder;

	public override void SetPCPortrait(Sprite portraitSprite, string portraitName)
	{
		pcPortraitSprite = portraitSprite;
		pcPortraitName = portraitName;
	}

	public override void SetActorPortraitSprite(string actorName, Sprite portraitSprite)
	{
		if (string.Equals(actorName, pcPortraitName))
		{
			Sprite sprite = (pcPortraitSprite = AbstractDialogueUI.GetValidPortraitSprite(actorName, portraitSprite));
			if ((UnityEngine.Object)(object)pcImage != null && DialogueManager.masterDatabase.IsPlayer(actorName))
			{
				pcImage.sprite = sprite;
			}
		}
	}

	public override void SetActive(bool value)
	{
		subtitleReminder.SetActive(value && subtitleReminder.HasText);
		Tools.SetGameObjectActive(buttonTemplate, value: false);
		UnityUIResponseButton[] array = buttons;
		foreach (UnityUIResponseButton unityUIResponseButton in array)
		{
			if (unityUIResponseButton != null)
			{
				if (value)
				{
					Tools.SetGameObjectActive(unityUIResponseButton, unityUIResponseButton.visible);
				}
				else
				{
					unityUIResponseButton.clickable = false;
				}
			}
		}
		Tools.SetGameObjectActive((Component)(object)timer, value: false);
		Tools.SetGameObjectActive((Component)(object)pcName, value);
		Tools.SetGameObjectActive((Component)(object)pcImage, value);
		if (value)
		{
			if ((UnityEngine.Object)(object)pcImage != null && pcPortraitSprite != null)
			{
				pcImage.sprite = pcPortraitSprite;
			}
			if ((UnityEngine.Object)(object)pcName != null && pcPortraitName != null)
			{
				pcName.text = pcPortraitName;
			}
			Tools.SetGameObjectActive((Component)(object)panel, value: true);
			animationTransitions.ClearTriggers(showHideController);
			showHideController.Show(animationTransitions.showTrigger, pauseAfterAnimation: false, null);
			if (explicitNavigationForTemplateButtons)
			{
				SetupTemplateButtonNavigation();
			}
		}
		else if (isVisible && CanTriggerAnimation(animationTransitions.hideTrigger))
		{
			animationTransitions.ClearTriggers(showHideController);
			showHideController.Hide(animationTransitions.hideTrigger, DeactivateUIElements);
		}
		else if ((UnityEngine.Object)(object)panel != null)
		{
			Tools.SetGameObjectActive((Component)(object)panel, value: false);
		}
	}

	private void DeactivateUIElements()
	{
		if ((UnityEngine.Object)(object)panel != null)
		{
			Tools.SetGameObjectActive((Component)(object)panel, value: false);
		}
		ClearResponseButtons();
	}

	protected override void ClearResponseButtons()
	{
		DestroyInstantiatedButtons();
		if (buttons == null)
		{
			return;
		}
		for (int i = 0; i < buttons.Length; i++)
		{
			if (!(buttons[i] == null))
			{
				buttons[i].Reset();
				buttons[i].visible = showUnusedButtons;
			}
		}
	}

	protected override void SetResponseButtons(Response[] responses, Transform target)
	{
		DestroyInstantiatedButtons();
		if (buttons != null && responses != null)
		{
			int num = 0;
			for (int i = 0; i < responses.Length; i++)
			{
				if (responses[i].formattedText.position != -1)
				{
					int position = responses[i].formattedText.position;
					if (0 <= position && position < buttons.Length && buttons[position] != null)
					{
						SetResponseButton(buttons[position], responses[i], target, num++);
					}
					else
					{
						Debug.LogWarning("Dialogue System: Buttons list doesn't contain a button for position " + position);
					}
				}
			}
			if (buttonTemplate != null && (UnityEngine.Object)(object)buttonTemplateHolder != null)
			{
				if ((UnityEngine.Object)(object)buttonTemplateScrollbar != null)
				{
					buttonTemplateScrollbar.value = buttonTemplateScrollbarResetValue;
				}
				for (int j = 0; j < responses.Length; j++)
				{
					if (responses[j].formattedText.position != -1)
					{
						continue;
					}
					GameObject gameObject = UnityEngine.Object.Instantiate(buttonTemplate.gameObject);
					if (gameObject == null)
					{
						Debug.LogError(string.Format("{0}: Couldn't instantiate response button template", "Dialogue System"));
						continue;
					}
					instantiatedButtons.Add(gameObject);
					gameObject.transform.SetParent(((Component)(object)buttonTemplateHolder).transform, worldPositionStays: false);
					gameObject.SetActive(value: true);
					UnityUIResponseButton component = gameObject.GetComponent<UnityUIResponseButton>();
					SetResponseButton(component, responses[j], target, num++);
					if (component != null)
					{
						gameObject.name = "Response: " + component.Text;
					}
				}
			}
			else if (buttonAlignment == ResponseButtonAlignment.ToFirst)
			{
				for (int k = 0; k < Mathf.Min(buttons.Length, responses.Length); k++)
				{
					if (responses[k].formattedText.position == -1)
					{
						int num2 = Mathf.Clamp(GetNextAvailableResponseButtonPosition(0, 1), 0, buttons.Length - 1);
						SetResponseButton(buttons[num2], responses[k], target, num++);
					}
				}
			}
			else
			{
				for (int num3 = Mathf.Min(buttons.Length, responses.Length) - 1; num3 >= 0; num3--)
				{
					if (responses[num3].formattedText.position == -1)
					{
						int num4 = Mathf.Clamp(GetNextAvailableResponseButtonPosition(buttons.Length - 1, -1), 0, buttons.Length - 1);
						SetResponseButton(buttons[num4], responses[num3], target, num++);
					}
				}
			}
		}
		NotifyContentChanged();
	}

	private void SetResponseButton(UnityUIResponseButton button, Response response, Transform target, int buttonNumber)
	{
		if (!(button != null))
		{
			return;
		}
		button.visible = true;
		button.clickable = response.enabled;
		button.target = target;
		if (response != null)
		{
			button.SetFormattedText(response.formattedText);
		}
		button.response = response;
		if (!autonumber.enabled)
		{
			return;
		}
		button.Text = string.Format(autonumber.format, buttonNumber + 1, button.Text);
		UIButtonKeyTrigger uIButtonKeyTrigger = button.GetComponent<UIButtonKeyTrigger>();
		if (autonumber.regularNumberHotkeys)
		{
			if (uIButtonKeyTrigger == null)
			{
				uIButtonKeyTrigger = button.gameObject.AddComponent<UIButtonKeyTrigger>();
			}
			uIButtonKeyTrigger.key = (KeyCode)(49 + buttonNumber);
		}
		if (autonumber.numpadHotkeys)
		{
			if (autonumber.regularNumberHotkeys || uIButtonKeyTrigger == null)
			{
				uIButtonKeyTrigger = button.gameObject.AddComponent<UIButtonKeyTrigger>();
			}
			uIButtonKeyTrigger.key = (KeyCode)(257 + buttonNumber);
		}
	}

	private int GetNextAvailableResponseButtonPosition(int start, int direction)
	{
		if (buttons != null)
		{
			for (int i = start; 0 <= i && i < buttons.Length; i += direction)
			{
				if (!buttons[i].visible || buttons[i].response == null)
				{
					return i;
				}
			}
		}
		return 5;
	}

	public void SetupTemplateButtonNavigation()
	{
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		if (instantiatedButtons != null && instantiatedButtons.Count != 0)
		{
			for (int i = 0; i < instantiatedButtons.Count; i++)
			{
				Button button = instantiatedButtons[i].GetComponent<UnityUIResponseButton>().button;
				Button val = ((i == 0) ? (loopExplicitNavigation ? instantiatedButtons[instantiatedButtons.Count - 1].GetComponent<UnityUIResponseButton>().button : null) : instantiatedButtons[i - 1].GetComponent<UnityUIResponseButton>().button);
				Button val2 = ((i == instantiatedButtons.Count - 1) ? (loopExplicitNavigation ? instantiatedButtons[0].GetComponent<UnityUIResponseButton>().button : null) : instantiatedButtons[i + 1].GetComponent<UnityUIResponseButton>().button);
				Navigation navigation = default(Navigation);
				((Navigation)(ref navigation)).mode = (Mode)4;
				((Navigation)(ref navigation)).selectOnUp = (Selectable)(object)val;
				((Navigation)(ref navigation)).selectOnLeft = (Selectable)(object)val;
				((Navigation)(ref navigation)).selectOnDown = (Selectable)(object)val2;
				((Navigation)(ref navigation)).selectOnRight = (Selectable)(object)val2;
				((Selectable)button).navigation = navigation;
			}
		}
	}

	public void DestroyInstantiatedButtons()
	{
		foreach (GameObject instantiatedButton in instantiatedButtons)
		{
			UnityEngine.Object.Destroy(instantiatedButton);
		}
		instantiatedButtons.Clear();
		NotifyContentChanged();
	}

	public void NotifyContentChanged()
	{
		onContentChanged.Invoke();
	}

	public override void StartTimer(float timeout)
	{
		if (!((UnityEngine.Object)(object)timer != null))
		{
			return;
		}
		if (unityUITimer == null)
		{
			Tools.SetGameObjectActive((Component)(object)timer, value: true);
			unityUITimer = ((Component)(object)timer).GetComponent<UnityUITimer>();
			if (unityUITimer == null)
			{
				unityUITimer = ((Component)(object)timer).gameObject.AddComponent<UnityUITimer>();
			}
			Tools.SetGameObjectActive((Component)(object)timer, value: false);
		}
		if (unityUITimer != null)
		{
			Tools.SetGameObjectActive((Component)(object)timer, value: true);
			unityUITimer.StartCountdown(timeout, OnTimeout);
		}
		else if (DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: No UnityUITimer component found on timer", "Dialogue System"));
		}
	}

	public virtual void OnTimeout()
	{
		if (TimeoutHandler != null)
		{
			TimeoutHandler();
		}
		else
		{
			DefaultTimeoutHandler();
		}
	}

	public void DefaultTimeoutHandler()
	{
		if (selectCurrentOnTimeout || DialogueManager.displaySettings.inputSettings.responseTimeoutAction == ResponseTimeoutAction.ChooseCurrentResponse)
		{
			UnityUIResponseButton unityUIResponseButton = (((UnityEngine.Object)(object)EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null) ? EventSystem.current.currentSelectedGameObject.GetComponent<UnityUIResponseButton>() : null);
			if (unityUIResponseButton != null)
			{
				unityUIResponseButton.OnClick();
				return;
			}
		}
		DialogueManager.instance.SendMessage("OnConversationTimeout");
	}

	public void AutoFocus(GameObject lastSelection = null, bool allowStealFocus = true)
	{
		if ((UnityEngine.Object)(object)EventSystem.current == null)
		{
			return;
		}
		GameObject currentSelection = EventSystem.current.currentSelectedGameObject;
		if (currentSelection == null)
		{
			currentSelection = lastSelection;
			EventSystem.current.SetSelectedGameObject(lastSelection);
		}
		if ((currentSelection != null && !allowStealFocus) || instantiatedButtons.Find((GameObject x) => x.gameObject == currentSelection) != null)
		{
			return;
		}
		for (int num = 0; num < buttons.Length; num++)
		{
			if (buttons[num] != null && buttons[num].gameObject == currentSelection)
			{
				return;
			}
		}
		if (instantiatedButtons.Count > 0)
		{
			UITools.Select((Selectable)(object)instantiatedButtons[0].GetComponent<Button>(), allowStealFocus);
			return;
		}
		for (int num2 = 0; num2 < buttons.Length; num2++)
		{
			if (buttons[num2] != null && buttons[num2].clickable)
			{
				UITools.Select((Selectable)(object)buttons[num2].button, allowStealFocus);
				break;
			}
		}
	}

	private bool CanTriggerAnimation(string triggerName)
	{
		if (CanTriggerAnimations())
		{
			return !string.IsNullOrEmpty(triggerName);
		}
		return false;
	}

	private bool CanTriggerAnimations()
	{
		if (animator == null && !lookedForAnimator)
		{
			lookedForAnimator = true;
			if ((UnityEngine.Object)(object)panel != null)
			{
				animator = ((Component)(object)panel).GetComponentInParent<Animator>();
			}
		}
		if (animator != null)
		{
			return animationTransitions != null;
		}
		return false;
	}
}
