using UnityEngine;

namespace RopeToolkit.Example;

public class ApplyTorqueOnKey : MonoBehaviour
{
	public Vector3 relativeTorque;

	public float maxAngularSpeed;

	public KeyCode key;

	protected Rigidbody rb;

	public void Start()
	{
		rb = GetComponent<Rigidbody>();
	}

	public void FixedUpdate()
	{
		if (!(rb == null) && Input.GetKey(key))
		{
			Vector3 normalized = relativeTorque.normalized;
			float num = Mathf.SmoothStep(relativeTorque.magnitude, 0f, Vector3.Dot(normalized, rb.angularVelocity) / maxAngularSpeed);
			rb.AddRelativeTorque(normalized * num, ForceMode.Force);
		}
	}
}
