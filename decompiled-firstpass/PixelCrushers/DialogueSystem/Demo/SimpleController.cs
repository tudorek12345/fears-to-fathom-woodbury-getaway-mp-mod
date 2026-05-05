using UnityEngine;

namespace PixelCrushers.DialogueSystem.Demo;

[AddComponentMenu("")]
[RequireComponent(typeof(CharacterController))]
public class SimpleController : MonoBehaviour
{
	[Header("Animator")]
	[Tooltip("Float parameter defined in animator controller that controls forward/backward speed.")]
	public string forwardSpeedFloatParameter = "Speed";

	[Tooltip("Float parameter defined in animator controller that controls left/right side-step speed.")]
	public string lateralSpeedFloatParameter = "Strafe";

	[Tooltip("Bool parameter defined in animator controller that specifies whether to use two-hand weapon animation or one-hand.")]
	public string twoHandWeaponBoolParameter = "Rifle";

	[Tooltip("Trigger parameter defined in animator controller that makes the animator play an attack animation.")]
	public string attackTriggerParameter = "Fire";

	[Header("Movement & Camera")]
	[Tooltip("Speed at which player moves if animator doesn't use root motion.")]
	public float runSpeed = 5f;

	[Tooltip("Mouse look rotation sensitivity.")]
	public float mouseSensitivityX = 15f;

	public float mouseSensitivityY = 10f;

	[Tooltip("Maximum up/down angles for mouse look.")]
	public float mouseMinimumY = -60f;

	public float mouseMaximumY = 60f;

	[Header("Attack")]
	[Tooltip("Use two-hand weapon animation.")]
	public bool useTwoHandWeapon;

	[Tooltip("Attack animation checks for target hit at this time in animation.")]
	public float hitDelay = 0.3f;

	[Tooltip("Play this sound at Hit Delay time.")]
	public AudioClip attackSound;

	[Tooltip("Distance at which attack can hit target.")]
	public float hitDistance = 100f;

	[Tooltip("Check for targets on these layers.")]
	public LayerMask hitLayerMask = 1;

	[Tooltip("Send this message to targets that are hit.")]
	public string damageMessage = "TakeDamage";

	[Tooltip("Send with parameter with the Damage Message.")]
	public float weaponDamage = 100f;

	[Header("Input")]
	public string horizontalAxis = "Horizontal";

	public string verticalAxis = "Vertical";

	public string mouseXAxis = "Mouse X";

	public string mouseYAxis = "Mouse Y";

	public string attackButton = "Fire1";

	private CharacterController m_controller;

	private SmoothCameraWithBumper m_smoothCamera;

	private Animator m_animator;

	private float m_cameraRotationY;

	private Quaternion m_originalCameraRotation;

	private bool m_firing;

	private void Awake()
	{
		m_controller = GetComponent<CharacterController>();
		m_smoothCamera = GetComponentInChildren<SmoothCameraWithBumper>();
		m_animator = GetComponent<Animator>();
	}

	private void Start()
	{
		Camera main = Camera.main;
		m_originalCameraRotation = ((main != null) ? main.transform.localRotation : Quaternion.identity);
	}

	private void Update()
	{
		if (Time.timeScale <= 0f)
		{
			return;
		}
		float axis = InputDeviceManager.GetAxis(mouseXAxis);
		float axis2 = InputDeviceManager.GetAxis(mouseYAxis);
		base.transform.Rotate(0f, axis * mouseSensitivityX, 0f);
		m_cameraRotationY += axis2 * mouseSensitivityY;
		m_cameraRotationY = ClampAngle(m_cameraRotationY, mouseMinimumY, mouseMaximumY);
		Quaternion quaternion = Quaternion.AngleAxis(m_cameraRotationY, -Vector3.right);
		if (m_smoothCamera != null)
		{
			m_smoothCamera.adjustQuaternion = quaternion;
		}
		else
		{
			Camera.main.transform.localRotation = m_originalCameraRotation * quaternion;
		}
		if (m_animator != null)
		{
			m_animator.SetBool(twoHandWeaponBoolParameter, useTwoHandWeapon);
		}
		if (DialogueManager.GetInputButtonDown(attackButton) && !m_firing)
		{
			if (m_animator != null)
			{
				m_animator.SetTrigger(attackTriggerParameter);
			}
			m_firing = true;
			Invoke("OnFired", hitDelay);
		}
		float axis3 = InputDeviceManager.GetAxis(verticalAxis);
		float axis4 = InputDeviceManager.GetAxis(horizontalAxis);
		if (Mathf.Abs(axis3) > 0.1f || Mathf.Abs(axis4) > 0.1f)
		{
			SetSpeed(axis3, axis4);
		}
		else
		{
			SetSpeed(0f, 0f);
		}
		if (m_animator == null || !m_animator.applyRootMotion)
		{
			m_controller.Move(base.transform.rotation * (Vector3.forward * axis3 * runSpeed * Time.deltaTime + Vector3.right * axis4 * runSpeed * Time.deltaTime) + Vector3.down * 20f * Time.deltaTime);
		}
	}

	private void OnFired()
	{
		m_firing = false;
		if (attackSound != null)
		{
			AudioSource.PlayClipAtPoint(attackSound, base.transform.position);
		}
		if (Physics.Raycast(Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2)), out var hitInfo, hitDistance, hitLayerMask))
		{
			hitInfo.collider.gameObject.BroadcastMessage(damageMessage, weaponDamage, SendMessageOptions.DontRequireReceiver);
		}
	}

	private void SetSpeed(float forwardSpeed, float lateralSpeed)
	{
		if (m_animator != null)
		{
			if (!string.IsNullOrEmpty(forwardSpeedFloatParameter))
			{
				m_animator.SetFloat(forwardSpeedFloatParameter, forwardSpeed);
			}
			if (!string.IsNullOrEmpty(lateralSpeedFloatParameter))
			{
				m_animator.SetFloat(lateralSpeedFloatParameter, lateralSpeed);
			}
		}
	}

	private void OnConversationStart(Transform actor)
	{
		SetSpeed(0f, 0f);
		CancelInvoke("OnFired");
		m_firing = false;
	}

	public static float ClampAngle(float angle, float min, float max)
	{
		if (angle < -360f)
		{
			angle += 360f;
		}
		if (angle > 360f)
		{
			angle -= 360f;
		}
		return Mathf.Clamp(angle, min, max);
	}
}
