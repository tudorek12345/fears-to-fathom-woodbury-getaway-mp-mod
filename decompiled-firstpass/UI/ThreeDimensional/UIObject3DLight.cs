using System;
using UnityEngine;

namespace UI.ThreeDimensional;

[RequireComponent(typeof(UIObject3D))]
[ExecuteInEditMode]
[AddComponentMenu("UI/UIObject3D/UIObject3D Light")]
public class UIObject3DLight : MonoBehaviour
{
	[SerializeField]
	private Vector3 _LightPosition = new Vector3(0f, 0f, -2.5f);

	[SerializeField]
	private Color _LightColor = Color.white;

	[SerializeField]
	[Range(0f, 8f)]
	private float _LightIntensity = 1f;

	[NonSerialized]
	private UIObject3D UIObject3D;

	[NonSerialized]
	private Light _lightObject;

	public Vector3 LightPosition
	{
		get
		{
			return _LightPosition;
		}
		set
		{
			_LightPosition = value;
			SetLightPosition();
		}
	}

	public Color LightColor
	{
		get
		{
			return _LightColor;
		}
		set
		{
			_LightColor = value;
			SetLightProperties();
		}
	}

	public float LightIntensity
	{
		get
		{
			return _LightIntensity;
		}
		set
		{
			_LightIntensity = value;
			SetLightProperties();
		}
	}

	private Light lightObject
	{
		get
		{
			if (_lightObject == null)
			{
				SpawnLight();
			}
			return _lightObject;
		}
		set
		{
			_lightObject = value;
		}
	}

	private void OnEnable()
	{
		if (UIObject3D == null)
		{
			UIObject3D = GetComponent<UIObject3D>();
		}
		UIObject3D.OnUpdateTarget.AddListener(UpdateLightEvent);
		lightObject.enabled = true;
		UpdateLight(scheduleRender: true);
	}

	private void OnDisable()
	{
		UIObject3D.OnUpdateTarget.RemoveListener(UpdateLightEvent);
		if (_lightObject != null)
		{
			lightObject.enabled = false;
			ScheduleRender();
		}
	}

	private void UpdateLightEvent()
	{
		UpdateLight(scheduleRender: true);
	}

	public void UpdateLight(bool scheduleRender = false)
	{
		if (base.enabled)
		{
			if (lightObject == null)
			{
				SpawnLight();
			}
			SetLightPosition(scheduleRender: false);
			SetLightProperties(scheduleRender: false);
			if (scheduleRender)
			{
				ScheduleRender();
			}
		}
	}

	private void SpawnLight()
	{
		GameObject gameObject = new GameObject("UIObject3DLight", typeof(Light));
		_lightObject = gameObject.GetComponent<Light>();
		_lightObject.transform.localScale = Vector3.one;
		_lightObject.transform.SetParent(UIObject3D.container.gameObject.transform);
		_lightObject.range = 200f;
		_lightObject.cullingMask = LayerMask.GetMask(LayerMask.LayerToName(UIObject3D.objectLayer));
		_lightObject.type = LightType.Point;
		_lightObject.bounceIntensity = 0f;
	}

	private void SetLightPosition(bool scheduleRender = true)
	{
		lightObject.transform.localPosition = LightPosition;
		if (scheduleRender)
		{
			ScheduleRender();
		}
	}

	private void SetLightProperties(bool scheduleRender = true)
	{
		lightObject.intensity = LightIntensity;
		lightObject.color = LightColor;
		if (scheduleRender)
		{
			ScheduleRender();
		}
	}

	private void ScheduleRender()
	{
		if (UIObject3D == null || !base.enabled)
		{
			return;
		}
		if (!Application.isPlaying)
		{
			UIObject3DTimer.AtEndOfFrame(delegate
			{
				UIObject3D.OnUpdateTarget.RemoveListener(UpdateLightEvent);
				UIObject3D.UpdateDisplay();
				UIObject3D.OnUpdateTarget.AddListener(UpdateLightEvent);
			}, this);
		}
		else
		{
			UIObject3D.Render();
		}
	}
}
