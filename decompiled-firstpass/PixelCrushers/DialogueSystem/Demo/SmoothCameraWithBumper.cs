using UnityEngine;

namespace PixelCrushers.DialogueSystem.Demo;

[AddComponentMenu("")]
public class SmoothCameraWithBumper : MonoBehaviour
{
	public Transform target;

	[SerializeField]
	private float distance = 3f;

	[SerializeField]
	private float height = 1f;

	[SerializeField]
	private float damping = 5f;

	[SerializeField]
	private bool smoothRotation = true;

	[SerializeField]
	private float rotationDamping = 10f;

	[SerializeField]
	private Vector3 targetLookAtOffset = Vector3.zero;

	[SerializeField]
	private float bumperDistanceCheck = 2.5f;

	[SerializeField]
	private float bumperCameraHeight = 1f;

	[SerializeField]
	private Vector3 bumperRayOffset = Vector3.zero;

	private Quaternion originalRotation;

	public Quaternion adjustQuaternion { get; set; }

	private void Awake()
	{
		adjustQuaternion = Quaternion.identity;
	}

	private void Start()
	{
		originalRotation = base.transform.localRotation;
	}

	private void FixedUpdate()
	{
		Vector3 b = target.TransformPoint(0f, height, 0f - distance);
		Vector3 direction = target.transform.TransformDirection(-1f * Vector3.forward);
		if (Physics.Raycast(target.TransformPoint(bumperRayOffset), direction, out var hitInfo, bumperDistanceCheck) && hitInfo.transform != target)
		{
			b.x = hitInfo.point.x;
			b.z = hitInfo.point.z;
			b.y = Mathf.Lerp(hitInfo.point.y + bumperCameraHeight, b.y, Time.deltaTime * damping);
		}
		base.transform.position = Vector3.Lerp(base.transform.position, b, Time.deltaTime * damping);
		Vector3 vector = target.TransformPoint(targetLookAtOffset);
		if (smoothRotation)
		{
			Quaternion b2 = Quaternion.LookRotation(vector - base.transform.position, target.up);
			base.transform.rotation = Quaternion.Slerp(base.transform.rotation, b2, Time.deltaTime * rotationDamping);
		}
		else
		{
			base.transform.rotation = Quaternion.LookRotation(vector - base.transform.position, target.up);
		}
		base.transform.localRotation = originalRotation * adjustQuaternion;
	}
}
