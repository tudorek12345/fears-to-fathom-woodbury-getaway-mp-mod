using Obi;
using UnityEngine;

public class CraneController : MonoBehaviour
{
	private ObiRopeCursor cursor;

	private ObiRope rope;

	private void Start()
	{
		cursor = GetComponentInChildren<ObiRopeCursor>();
		rope = ((Component)(object)cursor).GetComponent<ObiRope>();
	}

	private void Update()
	{
		if (Input.GetKey(KeyCode.W) && ((ObiRopeBase)rope).restLength > 6.5f)
		{
			cursor.ChangeLength(((ObiRopeBase)rope).restLength - 1f * Time.deltaTime);
		}
		if (Input.GetKey(KeyCode.S))
		{
			cursor.ChangeLength(((ObiRopeBase)rope).restLength + 1f * Time.deltaTime);
		}
		if (Input.GetKey(KeyCode.A))
		{
			base.transform.Rotate(0f, Time.deltaTime * 15f, 0f);
		}
		if (Input.GetKey(KeyCode.D))
		{
			base.transform.Rotate(0f, (0f - Time.deltaTime) * 15f, 0f);
		}
	}
}
