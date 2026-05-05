using UnityEngine;

public class SimpleMove : MonoBehaviour
{
	public float AngularSpeed = 1f;

	private void Update()
	{
		base.transform.RotateAround(Vector3.zero, Vector3.up, Time.deltaTime * AngularSpeed);
		if (Physics.Raycast(new Ray(base.transform.position + Vector3.up, Vector3.down), out var hitInfo))
		{
			base.transform.position = hitInfo.point;
		}
	}
}
