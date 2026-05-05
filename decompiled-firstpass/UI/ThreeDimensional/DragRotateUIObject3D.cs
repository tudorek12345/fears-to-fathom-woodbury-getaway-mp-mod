using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace UI.ThreeDimensional;

[RequireComponent(typeof(UIObject3D))]
[AddComponentMenu("UI/UIObject3D/Drag Rotate UIObject3D")]
public class DragRotateUIObject3D : MonoBehaviour
{
	[Header("Speed")]
	public float RotationSpeed = 10f;

	[Header("X")]
	public bool RotateX = true;

	public bool InvertX;

	[Header("Y")]
	public bool RotateY = true;

	public bool InvertY;

	[Header("Inertia")]
	public bool UseInertia;

	public float SlowSpeed = 1f;

	private UIObject3D UIObject3D;

	private bool beingDragged;

	private Vector3 speed = Vector3.zero;

	private Vector3 averageSpeed = Vector3.zero;

	private Vector2 lastMousePosition = Vector2.zero;

	private int _xMultiplier
	{
		get
		{
			if (!InvertX)
			{
				return 1;
			}
			return -1;
		}
	}

	private int _yMultiplier
	{
		get
		{
			if (!InvertY)
			{
				return 1;
			}
			return -1;
		}
	}

	private void Awake()
	{
		UIObject3D = GetComponent<UIObject3D>();
		SetupEvents();
	}

	private void Update()
	{
		if (UIObject3D == null || UIObject3D.targetContainer == null)
		{
			return;
		}
		if (lastMousePosition == Vector2.zero)
		{
			lastMousePosition = Input.mousePosition;
		}
		if (Input.GetMouseButton(0) && beingDragged)
		{
			Vector2 vector = ((Vector2)Input.mousePosition - lastMousePosition) * 100f;
			vector.Set(vector.x / (float)Screen.width, vector.y / (float)Screen.height);
			speed = new Vector3((0f - vector.x) * (float)_xMultiplier, vector.y * (float)_yMultiplier, 0f);
			averageSpeed = Vector3.Lerp(averageSpeed, speed, Time.deltaTime * 5f);
		}
		else
		{
			if (beingDragged)
			{
				speed = averageSpeed;
				beingDragged = false;
			}
			if (UseInertia)
			{
				float t = Time.deltaTime * SlowSpeed;
				speed = Vector3.Lerp(speed, Vector3.zero, t);
			}
			else
			{
				speed = Vector3.zero;
			}
		}
		if (speed != Vector3.zero)
		{
			if (RotateX)
			{
				UIObject3D.targetContainer.Rotate(Camera.main.transform.up * speed.x * RotationSpeed, Space.World);
			}
			if (RotateY)
			{
				UIObject3D.targetContainer.Rotate(Camera.main.transform.right * speed.y * RotationSpeed, Space.World);
			}
			UIObject3D.TargetRotation = UIObject3D.targetContainer.localRotation.eulerAngles;
		}
		lastMousePosition = Input.mousePosition;
	}

	private void SetupEvents()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		EventTrigger obj = GetComponent<EventTrigger>() ?? base.gameObject.AddComponent<EventTrigger>();
		Entry val = new Entry
		{
			eventID = (EventTriggerType)2
		};
		((UnityEvent<BaseEventData>)(object)val.callback).AddListener((UnityAction<BaseEventData>)delegate
		{
			beingDragged = true;
		});
		obj.triggers.Add(val);
	}
}
