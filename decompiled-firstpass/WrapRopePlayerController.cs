using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WrapRopePlayerController : MonoBehaviour
{
	public float acceleration = 50f;

	private Rigidbody rb;

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
	}

	private void Update()
	{
		Vector3 zero = Vector3.zero;
		if (Input.GetKey(KeyCode.W))
		{
			zero += Vector3.up * acceleration;
		}
		if (Input.GetKey(KeyCode.A))
		{
			zero += Vector3.left * acceleration;
		}
		if (Input.GetKey(KeyCode.S))
		{
			zero += Vector3.down * acceleration;
		}
		if (Input.GetKey(KeyCode.D))
		{
			zero += Vector3.right * acceleration;
		}
		rb.AddForce(zero.normalized * acceleration, ForceMode.Acceleration);
	}
}
