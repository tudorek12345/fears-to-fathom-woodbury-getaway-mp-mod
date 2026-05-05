using UnityEngine;

namespace EPOOutline.Demo;

public class CameraController : MonoBehaviour
{
	[SerializeField]
	private Vector3 shift;

	[SerializeField]
	private float moveSpeed = 2f;

	[SerializeField]
	private Transform target;

	private void Update()
	{
		base.transform.position = Vector3.Lerp(base.transform.position, target.position + shift, Time.deltaTime * moveSpeed);
	}
}
