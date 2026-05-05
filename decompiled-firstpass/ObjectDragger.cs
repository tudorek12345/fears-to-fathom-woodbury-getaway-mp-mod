using UnityEngine;

public class ObjectDragger : MonoBehaviour
{
	private Vector3 screenPoint;

	private Vector3 offset;

	private void OnMouseDown()
	{
		screenPoint = Camera.main.WorldToScreenPoint(base.gameObject.transform.position);
		offset = base.gameObject.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
	}

	private void OnMouseDrag()
	{
		Vector3 position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
		base.transform.position = Camera.main.ScreenToWorldPoint(position) + offset;
	}
}
