using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Characters.FirstPerson;

public class MouseLook : MonoBehaviour
{
	public float XSensitivity = 2f;

	public float YSensitivity = 2f;

	public bool clampVerticalRotation = true;

	public float MinimumX = -90f;

	public float MaximumX = 90f;

	public bool smooth;

	public float smoothTime = 5f;

	private Quaternion m_CharacterTargetRot;

	private Quaternion m_CameraTargetRot;

	[Header("Custom Flags")]
	[Tooltip("Toggle mouse look on and off")]
	public bool enableMouseLook = true;

	[Tooltip("Flip Y Axis")]
	public bool flipGamepadY = true;

	[HideInInspector]
	public void Init(Transform character, Transform camera)
	{
		m_CharacterTargetRot = character.localRotation;
		m_CameraTargetRot = camera.localRotation;
	}

	public void LookRotation(Transform character, Transform camera)
	{
		if (!enableMouseLook)
		{
			return;
		}
		float num = CrossPlatformInputManager.GetAxis("Mouse X") * XSensitivity;
		float num2 = CrossPlatformInputManager.GetAxis("Mouse Y") * YSensitivity;
		if (num2 == 0f && num == 0f)
		{
			num = CrossPlatformInputManager.GetAxis("Gamepad Look X") * XSensitivity;
			num2 = CrossPlatformInputManager.GetAxis("Gamepad Look Y") * YSensitivity;
			if (flipGamepadY)
			{
				num2 *= -1f;
			}
		}
		m_CharacterTargetRot *= Quaternion.Euler(0f, num, 0f);
		m_CameraTargetRot *= Quaternion.Euler(0f - num2, 0f, 0f);
		if (clampVerticalRotation)
		{
			m_CameraTargetRot = ClampRotationAroundXAxis(m_CameraTargetRot);
		}
		if (smooth)
		{
			character.localRotation = Quaternion.Slerp(character.localRotation, m_CharacterTargetRot, smoothTime * Time.deltaTime);
			camera.localRotation = Quaternion.Slerp(camera.localRotation, m_CameraTargetRot, smoothTime * Time.deltaTime);
		}
		else
		{
			character.localRotation = m_CharacterTargetRot;
			camera.localRotation = m_CameraTargetRot;
		}
	}

	private Quaternion ClampRotationAroundXAxis(Quaternion q)
	{
		q.x /= q.w;
		q.y /= q.w;
		q.z /= q.w;
		q.w = 1f;
		float value = 114.59156f * Mathf.Atan(q.x);
		value = Mathf.Clamp(value, MinimumX, MaximumX);
		q.x = Mathf.Tan(MathF.PI / 360f * value);
		return q;
	}
}
