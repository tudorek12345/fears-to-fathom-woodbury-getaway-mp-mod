using UnityEngine;

namespace Obi;

[RequireComponent(typeof(Camera))]
public class LookAroundCamera : MonoBehaviour
{
	public struct CameraShot(Vector3 position, Quaternion rotation, Vector3 up, float fieldOfView)
	{
		public Vector3 position = position;

		public Quaternion rotation = rotation;

		public Vector3 up = up;

		public float fieldOfView = fieldOfView;
	}

	private Camera cam;

	private CameraShot currentShot;

	public float movementSpeed = 5f;

	public float rotationSpeed = 8f;

	public float translationResponse = 10f;

	public float rotationResponse = 10f;

	public float fovResponse = 10f;

	private void Awake()
	{
		cam = GetComponent<Camera>();
		currentShot = new CameraShot(base.transform.position, base.transform.rotation, base.transform.up, cam.fieldOfView);
	}

	private void LookAt(Vector3 position, Vector3 up)
	{
		currentShot.up = up;
		currentShot.rotation = Quaternion.LookRotation(position - currentShot.position, currentShot.up);
	}

	private void UpdateShot()
	{
		base.transform.position = Vector3.Lerp(base.transform.position, currentShot.position, translationResponse * Time.deltaTime);
		base.transform.rotation = Quaternion.Slerp(base.transform.rotation, currentShot.rotation, rotationResponse * Time.deltaTime);
		cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, currentShot.fieldOfView, fovResponse * Time.deltaTime);
	}

	private void LateUpdate()
	{
		Vector3 zero = Vector3.zero;
		if (Input.GetKey(KeyCode.W))
		{
			zero += cam.transform.forward;
		}
		if (Input.GetKey(KeyCode.A))
		{
			zero -= cam.transform.right;
		}
		if (Input.GetKey(KeyCode.S))
		{
			zero -= cam.transform.forward;
		}
		if (Input.GetKey(KeyCode.D))
		{
			zero += cam.transform.right;
		}
		currentShot.position += zero * Time.deltaTime * movementSpeed;
		if (Input.GetKey(KeyCode.Mouse0))
		{
			float angle = Input.GetAxis("Mouse X") * rotationSpeed;
			float angle2 = Input.GetAxis("Mouse Y") * rotationSpeed;
			Quaternion quaternion = currentShot.rotation * Quaternion.AngleAxis(angle, Vector3.up) * Quaternion.AngleAxis(angle2, -Vector3.right);
			LookAt(currentShot.position + quaternion * Vector3.forward, Vector3.up);
		}
		UpdateShot();
	}
}
