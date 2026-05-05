using UnityEngine;

public class RopeTenser : MonoBehaviour
{
	public float force = 10f;

	private void Update()
	{
		GetComponent<Rigidbody>().AddForce(Vector3.down * force);
	}
}
