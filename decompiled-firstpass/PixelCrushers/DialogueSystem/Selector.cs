using System;
using PixelCrushers.DialogueSystem.UnityGUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class Selector : MonoBehaviour, IEventSystemUser
{
	[Serializable]
	public class Reticle
	{
		public Texture2D inRange;

		public Texture2D outOfRange;

		public float width = 64f;

		public float height = 64f;
	}

	public enum SelectAt
	{
		CenterOfScreen,
		MousePosition,
		CustomPosition
	}

	public enum DistanceFrom
	{
		Camera,
		GameObject,
		ActorTransform
	}

	public enum Dimension
	{
		In2D,
		In3D
	}

	private static LayerMask DefaultLayer = 1;

	[Tooltip("How to target. This is where the raycast points to.")]
	public SelectAt selectAt;

	[Tooltip("Layer mask to use when targeting objects; objects on others layers are ignored.")]
	public LayerMask layerMask = DefaultLayer;

	[Tooltip("How to compute range to targeted object.")]
	public DistanceFrom distanceFrom;

	[Tooltip("Don't target objects farther than this; targets may still be unusable if beyond their usable range.")]
	public float maxSelectionDistance = 30f;

	public Dimension runRaycasts = Dimension.In3D;

	[Tooltip("Check all objects within raycast range for usables, even passing through obstacles.")]
	public bool raycastAll;

	[Tooltip("")]
	public bool useDefaultGUI = true;

	[Tooltip("GUI skin to use for the target's information (name and use message).")]
	public GUISkin guiSkin;

	[Tooltip("Name of the GUI style in the skin.")]
	public string guiStyleName = "label";

	public TextAnchor alignment = TextAnchor.UpperCenter;

	public TextStyle textStyle = TextStyle.Shadow;

	public Color textStyleColor = Color.black;

	[Tooltip("Color of the information labels when target is in range.")]
	public Color inRangeColor = Color.yellow;

	[Tooltip("Color of the information labels when target is out of range.")]
	public Color outOfRangeColor = Color.gray;

	public Reticle reticle;

	public KeyCode useKey = KeyCode.Space;

	public string useButton = "Fire2";

	[Tooltip("Default use message; can be overridden in the target's Usable component")]
	public string defaultUseMessage = "(spacebar to interact)";

	[Tooltip("Tick to also broadcast to the usable object's children")]
	public bool broadcastToChildren = true;

	[Tooltip("Actor transform to send with OnUse; defaults to this transform")]
	public Transform actorTransform;

	[Tooltip("If set, show this alert message if attempt to use something beyond its usable range")]
	public string tooFarMessage = string.Empty;

	public UsableUnityEvent onSelectedUsable = new UsableUnityEvent();

	public UsableUnityEvent onDeselectedUsable = new UsableUnityEvent();

	public UnityEvent tooFarEvent = new UnityEvent();

	[Tooltip("Tick to draw gizmos in Scene view")]
	public bool debug;

	private EventSystem m_eventSystem;

	protected GameObject selection;

	protected Usable usable;

	protected GameObject clickedDownOn;

	protected string heading = string.Empty;

	protected string useMessage = string.Empty;

	protected float distance;

	protected GUIStyle guiStyle;

	protected float guiStyleLineHeight = 16f;

	protected Ray lastRay;

	protected RaycastHit lastHit;

	protected RaycastHit[] lastHits;

	protected int numLastHits;

	protected const int MaxHits = 100;

	protected bool hasReportedInvalidCamera;

	protected bool hasCheckedDefaultInputManager;

	protected bool isUsingDefaultInputManager = true;

	public Vector3 CustomPosition { get; set; }

	public Usable CurrentUsable
	{
		get
		{
			return usable;
		}
		set
		{
			SetCurrentUsable(value);
		}
	}

	public float CurrentDistance => distance;

	public GUIStyle GuiStyle
	{
		get
		{
			SetGuiStyle();
			return guiStyle;
		}
	}

	public EventSystem eventSystem
	{
		get
		{
			if ((UnityEngine.Object)(object)m_eventSystem != null)
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
		if (Camera.main == null)
		{
			Debug.LogError("Dialogue System: The scene is missing a camera tagged 'MainCamera'. The Selector may not behave the way you expect.", this);
		}
	}

	protected virtual void Update()
	{
		if (base.enabled && !(Time.timeScale <= 0f) && !(Camera.main == null) && (selectAt != SelectAt.MousePosition || !((UnityEngine.Object)(object)eventSystem != null) || !eventSystem.IsPointerOverGameObject()))
		{
			switch (runRaycasts)
			{
			case Dimension.In2D:
				Run2DRaycast();
				break;
			default:
				Run3DRaycast();
				break;
			}
			if (IsUseButtonDown())
			{
				UseCurrentSelection();
			}
		}
	}

	public virtual void UseCurrentSelection()
	{
		if (!(usable != null) || !usable.enabled || !usable.gameObject.activeInHierarchy)
		{
			return;
		}
		clickedDownOn = null;
		if (distance <= usable.maxUseDistance)
		{
			usable.OnUseUsable();
			if (usable != null)
			{
				Transform transform = ((actorTransform != null) ? actorTransform : base.transform);
				if (broadcastToChildren)
				{
					usable.gameObject.BroadcastMessage("OnUse", transform, SendMessageOptions.DontRequireReceiver);
				}
				else
				{
					usable.gameObject.SendMessage("OnUse", transform, SendMessageOptions.DontRequireReceiver);
				}
			}
		}
		else
		{
			if (!string.IsNullOrEmpty(tooFarMessage))
			{
				DialogueManager.ShowAlert(tooFarMessage);
			}
			tooFarEvent.Invoke();
		}
	}

	protected virtual void Run2DRaycast()
	{
	}

	protected virtual void Run3DRaycast()
	{
		Ray ray = (lastRay = Camera.main.ScreenPointToRay(GetSelectionPoint()));
		float maxDistance = ((distanceFrom == DistanceFrom.GameObject) ? float.PositiveInfinity : maxSelectionDistance);
		if (raycastAll)
		{
			if (lastHits == null)
			{
				lastHits = new RaycastHit[100];
			}
			numLastHits = Physics.RaycastNonAlloc(ray, lastHits, maxDistance, layerMask);
			bool flag = false;
			for (int i = 0; i < numLastHits; i++)
			{
				RaycastHit raycastHit = lastHits[i];
				float num = ((distanceFrom == DistanceFrom.Camera) ? raycastHit.distance : ((distanceFrom == DistanceFrom.GameObject || actorTransform == null) ? Vector3.Distance(base.gameObject.transform.position, raycastHit.collider.transform.position) : Vector3.Distance(actorTransform.position, raycastHit.collider.transform.position)));
				if (selection == raycastHit.collider.gameObject)
				{
					flag = true;
					distance = num;
					break;
				}
				Usable component = raycastHit.collider.GetComponent<Usable>();
				if (component != null && component.enabled && num <= maxSelectionDistance)
				{
					flag = true;
					distance = num;
					SetCurrentUsable(component);
					break;
				}
			}
			if (!flag)
			{
				DeselectTarget();
			}
			return;
		}
		if (Physics.Raycast(ray, out var hitInfo, maxSelectionDistance, layerMask))
		{
			distance = ((distanceFrom == DistanceFrom.Camera) ? hitInfo.distance : ((distanceFrom == DistanceFrom.GameObject || actorTransform == null) ? Vector3.Distance(base.gameObject.transform.position, hitInfo.collider.transform.position) : Vector3.Distance(actorTransform.position, hitInfo.collider.transform.position)));
			Usable component2 = hitInfo.collider.GetComponent<Usable>();
			if (component2 != null && component2.enabled)
			{
				if (selection != hitInfo.collider.gameObject)
				{
					SetCurrentUsable(component2);
				}
			}
			else
			{
				DeselectTarget();
			}
		}
		else
		{
			DeselectTarget();
		}
		lastHit = hitInfo;
	}

	public virtual void SetCurrentUsable(Usable usable)
	{
		if (usable == this.usable)
		{
			return;
		}
		if (usable == null)
		{
			DeselectTarget();
			return;
		}
		if (this.usable != null && this.usable != usable)
		{
			DeselectTarget();
		}
		this.usable = usable;
		usable.disabled -= OnUsableDisabled;
		usable.disabled += OnUsableDisabled;
		selection = usable.gameObject;
		heading = string.Empty;
		useMessage = string.Empty;
		OnSelectedUsableObject(usable);
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

	protected virtual void DeselectTarget()
	{
		if (usable != null)
		{
			usable.disabled -= OnUsableDisabled;
		}
		OnDeselectedUsableObject(usable);
		usable = null;
		selection = null;
		heading = string.Empty;
		useMessage = string.Empty;
	}

	protected virtual void OnUsableDisabled(Usable usable)
	{
		if (usable == this.usable)
		{
			DeselectTarget();
		}
	}

	protected virtual bool IsUseButtonDown()
	{
		if (DialogueManager.IsDialogueSystemInputDisabled())
		{
			return false;
		}
		if (!string.IsNullOrEmpty(useButton) && DialogueManager.getInputButtonDown(useButton))
		{
			clickedDownOn = selection;
		}
		if (useKey != KeyCode.None && InputDeviceManager.IsKeyDown(useKey))
		{
			return true;
		}
		if (!string.IsNullOrEmpty(useButton))
		{
			if (DialogueManager.instance != null && DialogueManager.getInputButtonDown == new GetInputButtonDownDelegate(DialogueManager.instance.StandardGetInputButtonDown) && IsUsingDefaultInputManager())
			{
				if (InputDeviceManager.IsButtonUp(useButton))
				{
					return selection == clickedDownOn;
				}
				return false;
			}
			return DialogueManager.GetInputButtonDown(useButton);
		}
		return false;
	}

	protected virtual bool IsUsingDefaultInputManager()
	{
		if (!hasCheckedDefaultInputManager)
		{
			hasCheckedDefaultInputManager = true;
			Type typeFromName = RuntimeTypeUtility.GetTypeFromName("PixelCrushers.RewiredSupport.InputDeviceManagerRewired");
			bool flag = typeFromName != null && GameObjectUtility.FindFirstObjectByType(typeFromName) != null;
			isUsingDefaultInputManager = !flag;
		}
		return isUsingDefaultInputManager;
	}

	protected virtual Vector3 GetSelectionPoint()
	{
		return selectAt switch
		{
			SelectAt.MousePosition => InputDeviceManager.GetMousePosition(), 
			SelectAt.CustomPosition => CustomPosition, 
			_ => new Vector3(Screen.width / 2, Screen.height / 2), 
		};
	}

	public virtual void OnGUI()
	{
		if (!base.enabled || !useDefaultGUI)
		{
			return;
		}
		if (guiStyle == null && (Event.current.type == EventType.Repaint || usable != null))
		{
			SetGuiStyle();
		}
		if (usable != null)
		{
			bool flag = distance <= usable.maxUseDistance;
			guiStyle.normal.textColor = (flag ? inRangeColor : outOfRangeColor);
			if (string.IsNullOrEmpty(heading))
			{
				heading = usable.GetName();
				useMessage = DialogueManager.GetLocalizedText(string.IsNullOrEmpty(usable.overrideUseMessage) ? defaultUseMessage : usable.overrideUseMessage);
			}
			UnityGUITools.DrawText(new Rect(0f, 0f, Screen.width, Screen.height), heading, guiStyle, textStyle, textStyleColor);
			UnityGUITools.DrawText(new Rect(0f, guiStyleLineHeight, Screen.width, Screen.height), useMessage, guiStyle, textStyle, textStyleColor);
			Texture2D texture2D = (flag ? reticle.inRange : reticle.outOfRange);
			if (texture2D != null)
			{
				GUI.Label(new Rect(0.5f * ((float)Screen.width - reticle.width), 0.5f * ((float)Screen.height - reticle.height), reticle.width, reticle.height), texture2D);
			}
		}
	}

	protected void SetGuiStyle()
	{
		guiSkin = UnityGUITools.GetValidGUISkin(guiSkin);
		GUI.skin = guiSkin;
		guiStyle = new GUIStyle(string.IsNullOrEmpty(guiStyleName) ? GUI.skin.label : (GUI.skin.FindStyle(guiStyleName) ?? GUI.skin.label));
		guiStyle.alignment = alignment;
		guiStyleLineHeight = guiStyle.CalcSize(new GUIContent("Ay")).y;
	}

	public virtual void OnDrawGizmos()
	{
		if (!debug)
		{
			return;
		}
		Gizmos.color = Color.yellow;
		Gizmos.DrawLine(lastRay.origin, lastRay.origin + lastRay.direction * maxSelectionDistance);
		if (raycastAll)
		{
			if (lastHits != null)
			{
				for (int i = 0; i < numLastHits; i++)
				{
					RaycastHit raycastHit = lastHits[i];
					Usable component = raycastHit.collider.GetComponent<Usable>();
					Gizmos.color = ((component != null && component.enabled) ? Color.green : Color.red);
					Gizmos.DrawWireSphere(raycastHit.point, 0.2f);
				}
			}
		}
		else if (lastHit.collider != null)
		{
			Usable component2 = lastHit.collider.GetComponent<Usable>();
			Gizmos.color = ((component2 != null && component2.enabled) ? Color.green : Color.red);
			Gizmos.DrawWireSphere(lastHit.point, 0.2f);
		}
	}
}
