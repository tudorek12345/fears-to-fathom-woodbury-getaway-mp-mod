using System;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem.UnityGUI;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class ProximitySelector : MonoBehaviour
{
	[Serializable]
	public class Reticle
	{
		public Texture2D inRange;

		public Texture2D outOfRange;

		public float width = 64f;

		public float height = 64f;
	}

	[Tooltip("Use a default OnGUI to display selection message and targeting reticle.")]
	public bool useDefaultGUI = true;

	[Tooltip("GUI skin to use for the target's information (name and use message).")]
	public GUISkin guiSkin;

	[Tooltip("Name of the GUI style in the skin.")]
	public string guiStyleName = "label";

	public TextAnchor alignment = TextAnchor.UpperCenter;

	[Tooltip("Color of the information labels when the target is in range.")]
	public Color color = Color.yellow;

	public TextStyle textStyle = TextStyle.Shadow;

	[Tooltip("Color of the text style's outline or shadow.")]
	public Color textStyleColor = Color.black;

	[Tooltip("Default use message. This can be overridden in the target's Usable component.")]
	public string defaultUseMessage = "(spacebar to interact)";

	[Tooltip("Key that sends an OnUse message.")]
	public KeyCode useKey = KeyCode.Space;

	[Tooltip("Input button that sends an OnUse message.")]
	public string useButton = "Fire2";

	[Tooltip("Enable touch triggering.")]
	public bool enableTouch;

	public ScaledRect touchArea = new ScaledRect(ScaledRect.empty);

	[Tooltip("Broadcast OnUse message to Usable object's children.")]
	public bool broadcastToChildren = true;

	[Tooltip("Actor transform to send with OnUse. Defaults to this transform.")]
	public Transform actorTransform;

	public UsableUnityEvent onSelectedUsable = new UsableUnityEvent();

	public UsableUnityEvent onDeselectedUsable = new UsableUnityEvent();

	public List<Usable> usablesInRange = new List<Usable>();

	protected Usable currentUsable;

	protected string currentHeading = string.Empty;

	protected string currentUseMessage = string.Empty;

	protected bool toldListenersHaveUsable;

	protected GUIStyle guiStyle;

	protected float guiStyleLineHeight = 16f;

	protected const float MinTimeBetweenUseButton = 0.5f;

	protected float timeToEnableUseButton;

	public Usable CurrentUsable
	{
		get
		{
			return currentUsable;
		}
		set
		{
			SetCurrentUsable(value);
		}
	}

	public GUIStyle GuiStyle
	{
		get
		{
			SetGuiStyle();
			return guiStyle;
		}
	}

	public event SelectedUsableObjectDelegate SelectedUsableObject;

	public event DeselectedUsableObjectDelegate DeselectedUsableObject;

	public event Action Enabled;

	public event Action Disabled;

	protected virtual void Reset()
	{
	}

	protected virtual void OnEnable()
	{
		this.Enabled?.Invoke();
	}

	protected virtual void OnDisable()
	{
		this.Disabled?.Invoke();
	}

	public virtual void Start()
	{
		bool flag = false;
		if (GetComponent<Collider>() != null)
		{
			flag = true;
		}
		if (!flag && DialogueDebug.logWarnings)
		{
			Debug.LogWarning("Dialogue System: Proximity Selector requires a collider, but it has no collider component. If your project is 2D, did you enable 2D support? (Tools > Pixel Crushers > Dialogue System > Welcome Window)", this);
		}
	}

	public virtual void OnConversationStart(Transform actor)
	{
		timeToEnableUseButton = Time.time + 0.5f;
	}

	public virtual void OnConversationEnd(Transform actor)
	{
		timeToEnableUseButton = Time.time + 0.5f;
	}

	protected virtual void Update()
	{
		if (base.enabled && !(Time.timeScale <= 0f))
		{
			if (toldListenersHaveUsable && (currentUsable == null || !currentUsable.enabled || !currentUsable.gameObject.activeInHierarchy))
			{
				SetCurrentUsable(null);
				OnDeselectedUsableObject(null);
				toldListenersHaveUsable = false;
			}
			if (IsUseButtonDown())
			{
				UseCurrentSelection();
			}
		}
	}

	protected void OnSelectedUsableObject(Usable usable)
	{
		if (this.SelectedUsableObject != null)
		{
			this.SelectedUsableObject(usable);
		}
		onSelectedUsable.Invoke(usable);
		if (usable != null)
		{
			usable.OnSelectUsable();
		}
	}

	protected void OnDeselectedUsableObject(Usable usable)
	{
		if (this.DeselectedUsableObject != null)
		{
			this.DeselectedUsableObject(usable);
		}
		onDeselectedUsable.Invoke(usable);
		if (usable != null)
		{
			usable.OnDeselectUsable();
		}
	}

	public virtual void UseCurrentSelection()
	{
		if (!(currentUsable != null) || !currentUsable.enabled || !(currentUsable.gameObject != null) || !(Time.time >= timeToEnableUseButton))
		{
			return;
		}
		currentUsable.OnUseUsable();
		if (currentUsable != null)
		{
			Transform transform = ((actorTransform != null) ? actorTransform : base.transform);
			if (broadcastToChildren)
			{
				currentUsable.gameObject.BroadcastMessage("OnUse", transform, SendMessageOptions.DontRequireReceiver);
			}
			else
			{
				currentUsable.gameObject.SendMessage("OnUse", transform, SendMessageOptions.DontRequireReceiver);
			}
		}
	}

	protected virtual bool IsUseButtonDown()
	{
		if (DialogueManager.IsDialogueSystemInputDisabled())
		{
			return false;
		}
		if (enableTouch && IsTouchDown())
		{
			return true;
		}
		if (useKey == KeyCode.None || !InputDeviceManager.IsKeyDown(useKey))
		{
			if (!string.IsNullOrEmpty(useButton))
			{
				return DialogueManager.GetInputButtonDown(useButton);
			}
			return false;
		}
		return true;
	}

	protected bool IsTouchDown()
	{
		if (Input.touchCount >= 1)
		{
			Touch[] touches = Input.touches;
			for (int i = 0; i < touches.Length; i++)
			{
				Touch touch = touches[i];
				Vector2 point = new Vector2(touch.position.x, (float)Screen.height - touch.position.y);
				if (touchArea.GetPixelRect().Contains(point))
				{
					return true;
				}
			}
		}
		return false;
	}

	protected void OnTriggerEnter(Collider other)
	{
		CheckTriggerEnter(other.gameObject);
	}

	protected void OnTriggerExit(Collider other)
	{
		CheckTriggerExit(other.gameObject);
	}

	protected virtual void CheckTriggerEnter(GameObject other)
	{
		if (!base.enabled)
		{
			return;
		}
		Usable component = other.GetComponent<Usable>();
		if (component != null && component.enabled)
		{
			SetCurrentUsable(component);
			if (!usablesInRange.Contains(component))
			{
				usablesInRange.Add(component);
			}
			OnSelectedUsableObject(component);
			toldListenersHaveUsable = true;
		}
	}

	protected virtual void CheckTriggerExit(GameObject other)
	{
		Usable component = other.GetComponent<Usable>();
		if (component != null)
		{
			RemoveUsableFromDetectedList(component);
		}
	}

	public virtual void RemoveGameObjectFromDetectedList(GameObject other)
	{
		if (other != null)
		{
			RemoveUsableFromDetectedList(other.GetComponent<Usable>());
		}
	}

	public virtual void RemoveUsableFromDetectedList(Usable usable)
	{
		if (!(usable != null))
		{
			return;
		}
		if (usablesInRange.Contains(usable))
		{
			usablesInRange.Remove(usable);
		}
		if (currentUsable == usable)
		{
			OnDeselectedUsableObject(usable);
			toldListenersHaveUsable = false;
			usablesInRange.RemoveAll((Usable x) => x == null || !x.gameObject.activeInHierarchy);
			if (usablesInRange.Count > 0)
			{
				Usable usable2 = usablesInRange[0];
				SetCurrentUsable(usable2);
				OnSelectedUsableObject(usable2);
				toldListenersHaveUsable = true;
			}
			else
			{
				SetCurrentUsable(null);
			}
		}
	}

	public virtual void SetCurrentUsable(Usable usable)
	{
		if (usable == currentUsable)
		{
			return;
		}
		if (currentUsable != null)
		{
			currentUsable.disabled -= OnUsableDisabled;
			if (currentUsable != usable)
			{
				OnDeselectedUsableObject(currentUsable);
			}
		}
		currentUsable = usable;
		if (usable != null)
		{
			usable.disabled -= OnUsableDisabled;
			usable.disabled += OnUsableDisabled;
			currentHeading = currentUsable.GetName();
			currentUseMessage = DialogueManager.GetLocalizedText(string.IsNullOrEmpty(currentUsable.overrideUseMessage) ? defaultUseMessage : currentUsable.overrideUseMessage);
		}
		else
		{
			currentHeading = string.Empty;
			currentUseMessage = string.Empty;
		}
	}

	protected virtual void OnUsableDisabled(Usable usable)
	{
		if (usable != null)
		{
			RemoveUsableFromDetectedList(usable);
		}
	}

	public virtual void OnGUI()
	{
		if (base.enabled && useDefaultGUI)
		{
			if (guiStyle == null && (Event.current.type == EventType.Repaint || currentUsable != null))
			{
				SetGuiStyle();
			}
			if (currentUsable != null)
			{
				GUI.skin = guiSkin;
				UnityGUITools.DrawText(new Rect(0f, 0f, Screen.width, Screen.height), currentHeading, guiStyle, textStyle, textStyleColor);
				UnityGUITools.DrawText(new Rect(0f, guiStyleLineHeight, Screen.width, Screen.height), currentUseMessage, guiStyle, textStyle, textStyleColor);
			}
		}
	}

	protected void SetGuiStyle()
	{
		guiSkin = UnityGUITools.GetValidGUISkin(guiSkin);
		GUI.skin = guiSkin;
		guiStyle = new GUIStyle(string.IsNullOrEmpty(guiStyleName) ? GUI.skin.label : (GUI.skin.FindStyle(guiStyleName) ?? GUI.skin.label));
		guiStyle.alignment = alignment;
		guiStyle.normal.textColor = color;
		guiStyleLineHeight = guiStyle.CalcSize(new GUIContent("Ay")).y;
	}
}
