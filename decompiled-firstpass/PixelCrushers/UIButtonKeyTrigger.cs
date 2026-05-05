using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PixelCrushers;

[AddComponentMenu("")]
[RequireComponent(typeof(Selectable))]
public class UIButtonKeyTrigger : MonoBehaviour, IEventSystemUser
{
	[Tooltip("Trigger the selectable when this key is pressed.")]
	public KeyCode key;

	[Tooltip("Trigger the selectable when this input button is pressed.")]
	public string buttonName = string.Empty;

	[Tooltip("Trigger if any key, input button, or mouse button is pressed.")]
	public bool anyKeyOrButton;

	[Tooltip("Ignore trigger key/button if UI button is being clicked Event System's Submit input. Prevents unintentional double clicks. For this checkbox to work, you must set the Input Device Manager component's Submit input to the same inputs as the EventSystem's Submit.")]
	public bool skipIfBeingClickedBySubmit = true;

	[Tooltip("Visually show UI Button in pressed state when triggered.")]
	public bool simulateButtonClick = true;

	[Tooltip("Show pressed state for this duration in seconds.")]
	public float simulateButtonDownDuration = 0.1f;

	private Selectable m_selectable;

	private EventSystem m_eventSystem;

	public static bool monitorInput = true;

	protected Selectable selectable
	{
		get
		{
			return m_selectable;
		}
		set
		{
			m_selectable = value;
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

	protected virtual void Awake()
	{
		m_selectable = GetComponent<Selectable>();
		if ((Object)(object)m_selectable == null)
		{
			base.enabled = false;
		}
	}

	protected void Update()
	{
		if (monitorInput && ((Behaviour)(object)m_selectable).enabled && m_selectable.interactable && ((Component)(object)m_selectable).gameObject.activeInHierarchy && (InputDeviceManager.IsKeyDown(key) || (!string.IsNullOrEmpty(buttonName) && InputDeviceManager.IsButtonDown(buttonName)) || (anyKeyOrButton && InputDeviceManager.IsAnyKeyDown())) && (!skipIfBeingClickedBySubmit || !IsBeingClickedBySubmit()))
		{
			Click();
		}
	}

	protected virtual bool IsBeingClickedBySubmit()
	{
		if ((Object)(object)eventSystem != null && eventSystem.currentSelectedGameObject == ((Component)(object)m_selectable).gameObject && InputDeviceManager.instance != null)
		{
			return InputDeviceManager.IsButtonDown(InputDeviceManager.instance.submitButton);
		}
		return false;
	}

	protected virtual void Click()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		if (simulateButtonClick)
		{
			StartCoroutine(SimulateButtonClick());
		}
		else
		{
			ExecuteEvents.Execute<ISubmitHandler>(((Component)(object)m_selectable).gameObject, (BaseEventData)new PointerEventData(eventSystem), ExecuteEvents.submitHandler);
		}
	}

	protected IEnumerator SimulateButtonClick()
	{
		m_selectable.OnPointerDown(new PointerEventData(eventSystem));
		for (float timeLeft = simulateButtonDownDuration; timeLeft > 0f; timeLeft -= Time.unscaledDeltaTime)
		{
			yield return null;
		}
		m_selectable.OnPointerUp(new PointerEventData(eventSystem));
		m_selectable.OnDeselect((BaseEventData)null);
		ExecuteEvents.Execute<ISubmitHandler>(((Component)(object)m_selectable).gameObject, (BaseEventData)new PointerEventData(eventSystem), ExecuteEvents.submitHandler);
	}
}
