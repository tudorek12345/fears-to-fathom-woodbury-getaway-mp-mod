using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
	[Serializable]
	public class GroundType
	{
		public string name;

		public PhysicMaterial phyMat;

		public AudioClip[] footstepSounds;

		public bool changeWalkSpped;

		public bool changeHeadBobSpeed;

		public bool overrideVolume;

		[Range(0f, 1f)]
		public float volume;

		public float walkSpeed = 1.5f;

		public float sprintSpeed = 2f;

		public float sprintBobSpeed;

		public float sprintBobAmount;
	}

	private AudioSource m_AudioSource;

	[Header("Functional Options")]
	[SerializeField]
	public bool canSprint = true;

	[SerializeField]
	private bool canJump = true;

	[SerializeField]
	private bool canCrouch = true;

	[SerializeField]
	private bool canUseHeadBop = true;

	[SerializeField]
	private bool willSlideOnSlopes = true;

	[SerializeField]
	public bool canZoomIn = true;

	[SerializeField]
	public bool resetZoomIn;

	[SerializeField]
	private bool canCameraSway = true;

	[Header("Controls")]
	[SerializeField]
	private KeyCode sprintKey = KeyCode.LeftShift;

	[SerializeField]
	private KeyCode jumpKey = KeyCode.Space;

	[SerializeField]
	private KeyCode crouchKey = KeyCode.LeftControl;

	[SerializeField]
	private KeyCode zoomKey = KeyCode.Mouse1;

	[Header("Movement Parameters")]
	[SerializeField]
	public float walkSpeed = 3f;

	public float sprintSpeed = 6f;

	[SerializeField]
	public float crouchSpeed = 1.5f;

	[SerializeField]
	private float slopeSpeed = 8f;

	[Header("Look Parameters")]
	[SerializeField]
	[Range(1f, 10f)]
	public float lookSpeedX = 2f;

	[SerializeField]
	[Range(1f, 10f)]
	public float lookSpeedY = 2f;

	[SerializeField]
	[Range(1f, 180f)]
	public float upperLookLimit = 90f;

	[SerializeField]
	[Range(1f, 180f)]
	public float lowerLookLimit = 90f;

	[Header("Camera Leaning Parameters")]
	[SerializeField]
	private bool isInversed;

	[SerializeField]
	private float leanAmount;

	[SerializeField]
	private float leanSpeed;

	private float rotationZ;

	[Header("Jumping Parameters")]
	[SerializeField]
	private float jumpForce = 8f;

	[SerializeField]
	private float gravity = 30f;

	[Header("Crouch Parameters")]
	[SerializeField]
	public float crouchHeight = 0.5f;

	[SerializeField]
	private float standingHeight = 2f;

	[SerializeField]
	private float timeToCrouch = 0.25f;

	[SerializeField]
	public Vector3 crouchingCenter = new Vector3(0f, 0.5f, 0f);

	[SerializeField]
	private Vector3 standingCenter = new Vector3(0f, 0f, 0f);

	public bool isCrouching;

	private bool duringCrouchAnimation;

	[SerializeField]
	private float crouchStandCheckRadius = 1f;

	[SerializeField]
	private float crouchStandCheckHeight = 1f;

	[Header("Headbob Parameters")]
	[SerializeField]
	private float walkBobSpeed = 14f;

	[SerializeField]
	private float walkBobAmount = 0.05f;

	[SerializeField]
	public float sprintBobSpeed = 18f;

	[SerializeField]
	public float sprintBobAmount = 0.1f;

	[SerializeField]
	public float crouchBobSpeed = 8f;

	[SerializeField]
	public float crouchBobAmount = 0.025f;

	private float defaultYPos;

	private float timer;

	[SerializeField]
	public float iWalkspeed;

	[SerializeField]
	public float iSprintSpeed;

	[SerializeField]
	public float iVolume;

	private float m_StepCycle;

	private float m_NextStep;

	private Vector3 hitPointNormal;

	private AudioClip[] m_FootstepSounds;

	[Header("Footstep Settings")]
	[Range(0f, 1f)]
	public float volume;

	[SerializeField]
	private float m_StepInterval;

	[SerializeField]
	private string currentGround;

	[SerializeField]
	private PlayerController playerController;

	public float iSprintBobSpeed;

	public float iSprintBobAmount;

	[Header("Dynamic Footsteps")]
	public List<GroundType> GroundTypes = new List<GroundType>();

	[Header("Zoom in Parameters")]
	[SerializeField]
	private float zoomFovAmount;

	[SerializeField]
	private float timeToZoom;

	private float startFov;

	private float currentFov;

	public Camera playerCamera;

	public CharacterController characterController;

	private Vector3 moveDirection;

	private Vector2 currentInput;

	[HideInInspector]
	public float rotationX;

	private int moveAbs = 1;

	public bool skipEsc;

	public bool CanMove { get; private set; } = true;

	private bool IsSprinting
	{
		get
		{
			if (canSprint && Input.GetKey(sprintKey))
			{
				return Input.GetAxisRaw("Vertical") > 0f;
			}
			return false;
		}
	}

	private bool ShouldJump
	{
		get
		{
			if (Input.GetKeyDown(jumpKey))
			{
				return characterController.isGrounded;
			}
			return false;
		}
	}

	private bool ShouldCrouch
	{
		get
		{
			if (Input.GetKeyDown(crouchKey) && !duringCrouchAnimation)
			{
				return characterController.isGrounded;
			}
			return false;
		}
	}

	private bool isSliding
	{
		get
		{
			if (characterController.isGrounded && Physics.Raycast(base.transform.position, Vector3.down, out var hitInfo, 2f))
			{
				hitPointNormal = hitInfo.normal;
				return Vector3.Angle(hitPointNormal, Vector3.up) > characterController.slopeLimit;
			}
			return false;
		}
	}

	public event Action<bool> OnCrouch;

	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		bool flag = false;
		flag = false;
		foreach (GroundType groundType in GroundTypes)
		{
			if (hit.collider.sharedMaterial == groundType.phyMat)
			{
				SetGroundType(groundType);
				flag = true;
			}
			else if (!flag)
			{
				SetGroundType(GroundTypes[0]);
			}
		}
	}

	private void SetGroundType(GroundType ground)
	{
		if (currentGround != ground.name)
		{
			m_FootstepSounds = ground.footstepSounds;
			if (ground.changeWalkSpped)
			{
				walkSpeed = ground.walkSpeed;
				sprintSpeed = ground.sprintSpeed;
			}
			else
			{
				walkSpeed = iWalkspeed;
				sprintSpeed = iSprintSpeed;
			}
			if (ground.overrideVolume)
			{
				volume = ground.volume;
			}
			else
			{
				volume = iVolume;
			}
			if (ground.changeHeadBobSpeed)
			{
				sprintBobSpeed = ground.sprintBobSpeed;
				sprintBobAmount = ground.sprintBobAmount;
			}
			else
			{
				sprintBobSpeed = iSprintBobSpeed;
				sprintBobAmount = iSprintBobAmount;
			}
			currentGround = ground.name;
		}
	}

	private void Awake()
	{
		playerCamera = GetComponentInChildren<Camera>();
		characterController = GetComponent<CharacterController>();
		defaultYPos = playerCamera.transform.localPosition.y;
		startFov = playerCamera.fieldOfView;
		currentFov = startFov;
		iWalkspeed = walkSpeed;
		iSprintSpeed = sprintSpeed;
		iVolume = volume;
		m_AudioSource = GetComponent<AudioSource>();
		iSprintBobAmount = sprintBobAmount;
		iSprintBobSpeed = sprintBobSpeed;
	}

	private void Start()
	{
	}

	private void Update()
	{
		if ((!(playerController != null) || !playerController.IsPlayerPaused()) && CanMove)
		{
			HandleMovementInput();
			HandleMouseLook();
			if (canJump)
			{
				HandleJump();
			}
			if (canCrouch)
			{
				HandleCrouch();
			}
			if (canUseHeadBop)
			{
				HandleHeadbob();
			}
			if (canZoomIn)
			{
				HandleZoomIn();
			}
			if (canCameraSway)
			{
				HandleSway();
			}
			ApplyFinalMovement();
		}
	}

	private void FixedUpdate()
	{
		if (!(playerController != null) || !playerController.IsPlayerPaused())
		{
			ProgressStepCycle();
		}
	}

	private void ProgressStepCycle()
	{
		if (characterController.velocity.sqrMagnitude > 0f && (currentInput.x != 0f || currentInput.y != 0f))
		{
			m_StepCycle += (characterController.velocity.magnitude + currentInput.magnitude) * Time.fixedDeltaTime;
		}
		if (m_StepCycle > m_NextStep)
		{
			m_NextStep = m_StepCycle + m_StepInterval;
			PlayFootStepAudio();
		}
	}

	private void PlayFootStepAudio()
	{
		if (characterController.isGrounded && m_FootstepSounds != null && m_FootstepSounds.Length != 0 && characterController.velocity.magnitude > 0.5f)
		{
			int num = UnityEngine.Random.Range(1, m_FootstepSounds.Length);
			m_AudioSource.clip = m_FootstepSounds[num];
			m_AudioSource.PlayOneShot(m_AudioSource.clip, volume);
			m_FootstepSounds[num] = m_FootstepSounds[0];
			m_FootstepSounds[0] = m_AudioSource.clip;
		}
	}

	private void HandleMovementInput()
	{
		currentInput = new Vector2((isCrouching ? crouchSpeed : (IsSprinting ? sprintSpeed : walkSpeed)) * Input.GetAxisRaw("Vertical"), (isCrouching ? crouchSpeed : (IsSprinting ? sprintSpeed : walkSpeed)) * Input.GetAxisRaw("Horizontal"));
		float y = moveDirection.y;
		moveDirection = base.transform.TransformDirection(Vector3.forward) * currentInput.x * moveAbs + base.transform.TransformDirection(Vector3.right) * currentInput.y * moveAbs;
		moveDirection.y = y;
	}

	private void HandleMouseLook()
	{
		rotationX -= Input.GetAxis("Mouse Y") * lookSpeedY;
		rotationX = Mathf.Clamp(rotationX, 0f - lowerLookLimit, upperLookLimit);
		playerCamera.transform.localRotation = Quaternion.Euler(rotationX * (float)moveAbs, 0f, rotationZ);
		base.transform.rotation *= Quaternion.Euler(0f, Input.GetAxis("Mouse X") * lookSpeedX * (float)moveAbs, 0f);
	}

	private void HandleSway()
	{
		rotationZ = Mathf.Lerp(rotationZ, isInversed ? (0f - Input.GetAxis("Horizontal")) : (Input.GetAxis("Horizontal") * leanAmount), leanSpeed * Time.deltaTime);
	}

	private void HandleJump()
	{
		if (ShouldJump)
		{
			moveDirection.y = jumpForce;
		}
	}

	private void HandleCrouch()
	{
		if (ShouldCrouch)
		{
			StartCoroutine(CrouchStand());
		}
	}

	private void HandleHeadbob()
	{
		if (characterController.isGrounded && characterController.velocity.magnitude > 0.5f && (Mathf.Abs(moveDirection.x) > 0.1f || Mathf.Abs(moveDirection.z) > 0.1f))
		{
			timer += Time.deltaTime * (isCrouching ? crouchBobSpeed : (IsSprinting ? sprintBobSpeed : walkBobSpeed));
			playerCamera.transform.localPosition = new Vector3(playerCamera.transform.localPosition.x, defaultYPos + Mathf.Sin(timer) * (isCrouching ? crouchBobAmount : (IsSprinting ? sprintBobAmount : walkBobAmount)), playerCamera.transform.localPosition.z);
		}
	}

	private void HandleZoomIn()
	{
		if (resetZoomIn)
		{
			currentFov = Mathf.Lerp(currentFov, startFov, timeToZoom * Time.deltaTime);
			playerCamera.fieldOfView = currentFov;
		}
		else
		{
			currentFov = Mathf.Lerp(currentFov, Input.GetKey(zoomKey) ? zoomFovAmount : startFov, timeToZoom * Time.deltaTime);
			playerCamera.fieldOfView = currentFov;
		}
	}

	private void ApplyFinalMovement()
	{
		if (!characterController.isGrounded)
		{
			moveDirection.y -= gravity * Time.deltaTime;
		}
		if (willSlideOnSlopes && isSliding)
		{
			moveDirection += new Vector3(hitPointNormal.x, 0f - hitPointNormal.y, hitPointNormal.z) * slopeSpeed;
		}
		characterController.Move(moveDirection * Time.deltaTime);
	}

	private IEnumerator CrouchStand()
	{
		if (isCrouching)
		{
			Physics.CheckSphere(new Vector3(playerCamera.transform.position.x, playerCamera.transform.position.y + crouchStandCheckHeight, playerCamera.transform.position.z), crouchStandCheckRadius);
		}
		duringCrouchAnimation = true;
		float timeElapsed = 0f;
		float targetHeight = (isCrouching ? standingHeight : crouchHeight);
		float currentHeight = characterController.height;
		Vector3 targetCenter = (isCrouching ? standingCenter : crouchingCenter);
		Vector3 currentCenter = characterController.center;
		while (timeElapsed < timeToCrouch)
		{
			characterController.height = Mathf.Lerp(currentHeight, targetHeight, timeElapsed / timeToCrouch);
			characterController.center = Vector3.Lerp(currentCenter, targetCenter, timeElapsed / timeToCrouch);
			timeElapsed += Time.deltaTime;
			yield return null;
		}
		characterController.height = targetHeight;
		characterController.center = targetCenter;
		isCrouching = !isCrouching;
		this.OnCrouch?.Invoke(isCrouching);
		duringCrouchAnimation = false;
	}

	public IEnumerator CrouchStandFromBlinds()
	{
		Physics.CheckSphere(new Vector3(playerCamera.transform.position.x, playerCamera.transform.position.y + crouchStandCheckHeight, playerCamera.transform.position.z), crouchStandCheckRadius);
		duringCrouchAnimation = true;
		float timeElapsed = 0f;
		float targetHeight = crouchHeight;
		float currentHeight = characterController.height;
		Vector3 targetCenter = crouchingCenter;
		Vector3 currentCenter = characterController.center;
		while (timeElapsed < timeToCrouch)
		{
			characterController.height = Mathf.Lerp(currentHeight, targetHeight, timeElapsed / timeToCrouch);
			characterController.center = Vector3.Lerp(currentCenter, targetCenter, timeElapsed / timeToCrouch);
			timeElapsed += Time.deltaTime;
			yield return null;
		}
		characterController.height = targetHeight;
		characterController.center = targetCenter;
		this.OnCrouch?.Invoke(isCrouching);
		duringCrouchAnimation = false;
	}

	public void PlayerStatus(bool status)
	{
		CanMove = status;
	}
}
