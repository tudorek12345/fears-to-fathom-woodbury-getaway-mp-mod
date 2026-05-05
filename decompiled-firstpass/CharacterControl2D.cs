using UnityEngine;

public class CharacterControl2D : MonoBehaviour
{
	public float acceleration = 10f;

	public float maxSpeed = 8f;

	public float jumpPower = 2f;

	private Rigidbody unityRigidbody;

	public void Awake()
	{
		unityRigidbody = GetComponent<Rigidbody>();
	}

	private void FixedUpdate()
	{
		unityRigidbody.AddForce(new Vector3(Input.GetAxis("Horizontal") * acceleration, 0f, 0f));
		unityRigidbody.velocity = Vector3.ClampMagnitude(unityRigidbody.velocity, maxSpeed);
		if (Input.GetButtonDown("Jump"))
		{
			unityRigidbody.AddForce(Vector3.up * jumpPower, ForceMode.VelocityChange);
		}
	}
}
