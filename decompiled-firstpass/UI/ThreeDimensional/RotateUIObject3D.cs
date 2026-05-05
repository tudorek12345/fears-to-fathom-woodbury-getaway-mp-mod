using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace UI.ThreeDimensional;

[RequireComponent(typeof(UIObject3D))]
[AddComponentMenu("UI/UIObject3D/Rotate UIObject3D")]
public class RotateUIObject3D : MonoBehaviour
{
	public enum eRotationMode
	{
		Constant,
		WhenMouseIsOver,
		WhenMouseIsOverThenSnapBack
	}

	public eRotationMode RotationMode;

	public bool RotateX;

	public float RotateXSpeed = 45f;

	public bool RotateY = true;

	public float RotateYSpeed = 45f;

	public bool RotateZ;

	public float RotateZSpeed = 45f;

	public float snapbackTime = 0.25f;

	private UIObject3D UIObject3D;

	private bool mouseIsOver;

	private Vector3 initialRotation = Vector3.zero;

	private EventTrigger _eventTrigger;

	private float timeSinceLastUpdate;

	private EventTrigger eventTrigger
	{
		get
		{
			if ((Object)(object)_eventTrigger == null)
			{
				_eventTrigger = GetComponent<EventTrigger>() ?? base.gameObject.AddComponent<EventTrigger>();
			}
			return _eventTrigger;
		}
	}

	private void Awake()
	{
		UIObject3D = GetComponent<UIObject3D>();
		initialRotation = UIObject3DUtilities.NormalizeRotation(UIObject3D.TargetRotation);
		SetupEvents();
	}

	private void Update()
	{
		timeSinceLastUpdate += Time.deltaTime;
		if (UIObject3D.LimitFrameRate && timeSinceLastUpdate < UIObject3D.timeBetweenFrames)
		{
			return;
		}
		switch (RotationMode)
		{
		case eRotationMode.Constant:
			UpdateRotation();
			break;
		case eRotationMode.WhenMouseIsOver:
		case eRotationMode.WhenMouseIsOverThenSnapBack:
			if (mouseIsOver)
			{
				UpdateRotation();
			}
			break;
		}
	}

	private void UpdateRotation()
	{
		UIObject3D.TargetRotation += new Vector3(RotateX ? (RotateXSpeed * timeSinceLastUpdate) : 0f, RotateY ? (RotateYSpeed * timeSinceLastUpdate) : 0f, RotateZ ? (RotateZSpeed * timeSinceLastUpdate) : 0f);
		timeSinceLastUpdate = 0f;
	}

	private void SetupEvents()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		Entry val = new Entry
		{
			eventID = (EventTriggerType)0
		};
		Entry val2 = new Entry
		{
			eventID = (EventTriggerType)1
		};
		((UnityEvent<BaseEventData>)(object)val.callback).AddListener((UnityAction<BaseEventData>)delegate
		{
			OnPointerEnter();
		});
		((UnityEvent<BaseEventData>)(object)val2.callback).AddListener((UnityAction<BaseEventData>)delegate
		{
			OnPointerExit();
		});
		eventTrigger.triggers.Add(val);
		eventTrigger.triggers.Add(val2);
	}

	private void OnPointerEnter()
	{
		mouseIsOver = true;
	}

	private void OnPointerExit()
	{
		mouseIsOver = false;
		if (RotationMode == eRotationMode.WhenMouseIsOverThenSnapBack)
		{
			StartCoroutine(SnapBack(snapbackTime));
		}
	}

	private IEnumerator SnapBack(float time)
	{
		float timeStarted = Time.time;
		float percentageComplete = 0f;
		Vector3 snapStartRotation = UIObject3DUtilities.NormalizeRotation(UIObject3D.TargetRotation);
		float desiredX = ((Mathf.Abs(snapStartRotation.x - initialRotation.x) >= 180f) ? (initialRotation.x - 180f) : initialRotation.x);
		float desiredY = ((Mathf.Abs(snapStartRotation.y - initialRotation.y) >= 180f) ? (initialRotation.y - 180f) : initialRotation.y);
		float desiredZ = ((Mathf.Abs(snapStartRotation.z - initialRotation.z) >= 180f) ? (initialRotation.z - 180f) : initialRotation.z);
		while (percentageComplete < 1f)
		{
			UIObject3D.TargetRotation = new Vector3(RotateX ? Mathf.Lerp(snapStartRotation.x, desiredX, percentageComplete) : desiredX, RotateY ? Mathf.Lerp(snapStartRotation.y, desiredY, percentageComplete) : desiredY, RotateZ ? Mathf.Lerp(snapStartRotation.z, desiredZ, percentageComplete) : desiredZ);
			percentageComplete = (Time.time - timeStarted) / time;
			yield return null;
		}
		UIObject3D.TargetRotation = initialRotation;
	}

	private void OnValidate()
	{
		((Behaviour)(object)eventTrigger).enabled = RotationMode != eRotationMode.Constant;
	}
}
