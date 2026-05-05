using UnityEngine;

namespace UI.ThreeDimensional.Examples;

public class UIObject3DExampleCamera : MonoBehaviour
{
	public float xRotationSpeed = 5f;

	public float yRotationSpeed = 2.5f;

	public float moveSpeed = 10f;

	private float mouseX;

	private float mouseY;

	private float mouseZ;

	private void Start()
	{
		mouseX = base.transform.rotation.eulerAngles.y;
		mouseY = base.transform.rotation.eulerAngles.x;
		mouseZ = base.transform.rotation.eulerAngles.z;
	}

	private void Update()
	{
		if (Input.GetMouseButton(1) || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
		{
			mouseX += Input.GetAxis("Mouse X") * xRotationSpeed;
			if (mouseX <= -180f)
			{
				mouseX += 360f;
			}
			else if (mouseX > 180f)
			{
				mouseX -= 360f;
			}
			mouseY -= Input.GetAxis("Mouse Y") * yRotationSpeed;
			if (mouseY <= -180f)
			{
				mouseY += 360f;
			}
			else if (mouseY > 180f)
			{
				mouseY -= 360f;
			}
		}
		base.transform.rotation = Quaternion.Euler(mouseY, mouseX, mouseZ);
		float num = moveSpeed;
		if (Input.GetKey(KeyCode.LeftShift))
		{
			num *= 5f;
		}
		if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
		{
			base.transform.position += base.transform.forward * (Time.deltaTime * num);
		}
		else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
		{
			base.transform.position -= base.transform.forward * (Time.deltaTime * num);
		}
		if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
		{
			base.transform.position -= base.transform.right * (Time.deltaTime * num);
		}
		else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
		{
			base.transform.position += base.transform.right * (Time.deltaTime * num);
		}
	}
}
