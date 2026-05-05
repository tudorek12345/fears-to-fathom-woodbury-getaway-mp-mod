using UnityEngine;

public class ExtrapolationCamera : MonoBehaviour
{
	public Transform target;

	public float extrapolation = 10f;

	[Range(0f, 1f)]
	public float smoothness = 0.8f;

	[Range(0f, 1f)]
	public float linearSpeed = 1f;

	[Range(0f, 1f)]
	public float rotationalSpeed = 1f;

	[Min(0f)]
	public float distanceFromTarget = 4f;

	private Vector3 lastPosition;

	private Vector3 extrapolatedPos;

	private void Start()
	{
		if (target != null)
		{
			lastPosition = target.position;
		}
	}

	private void FixedUpdate()
	{
		if (target != null)
		{
			Vector3 vector = target.position - lastPosition;
			vector.y = 0f;
			extrapolatedPos = Vector3.Lerp(target.position + vector * extrapolation, extrapolatedPos, smoothness);
			lastPosition = target.position;
		}
	}

	private void LateUpdate()
	{
		if (target != null)
		{
			Vector3 forward = extrapolatedPos - base.transform.position;
			base.transform.rotation = Quaternion.Lerp(base.transform.rotation, Quaternion.LookRotation(forward), rotationalSpeed);
			forward.y = 0f;
			base.transform.position += forward.normalized * (forward.magnitude - distanceFromTarget) * linearSpeed;
		}
	}

	public void Teleport(Vector3 position, Quaternion rotation)
	{
		base.transform.position = position;
		base.transform.rotation = rotation;
		if (target != null)
		{
			extrapolatedPos = (lastPosition = target.position);
		}
	}
}
