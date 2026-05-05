using Obi;
using UnityEngine;

[RequireComponent(typeof(ObiRope))]
public class CursorController : MonoBehaviour
{
	private ObiRopeCursor cursor;

	private ObiRope rope;

	public float minLength = 0.1f;

	public float speed = 1f;

	private void Start()
	{
		rope = GetComponent<ObiRope>();
		cursor = GetComponent<ObiRopeCursor>();
	}

	private void Update()
	{
		if (Input.GetKey(KeyCode.W) && (Object)(object)cursor != null && ((ObiRopeBase)rope).restLength > minLength)
		{
			cursor.ChangeLength(((ObiRopeBase)rope).restLength - speed * Time.deltaTime);
		}
		if (Input.GetKey(KeyCode.S) && (Object)(object)cursor != null)
		{
			cursor.ChangeLength(((ObiRopeBase)rope).restLength + speed * Time.deltaTime);
		}
		if (Input.GetKey(KeyCode.A))
		{
			((Component)(object)rope).transform.Translate(Vector3.left * Time.deltaTime, Space.World);
		}
		if (Input.GetKey(KeyCode.D))
		{
			((Component)(object)rope).transform.Translate(Vector3.right * Time.deltaTime, Space.World);
		}
	}
}
