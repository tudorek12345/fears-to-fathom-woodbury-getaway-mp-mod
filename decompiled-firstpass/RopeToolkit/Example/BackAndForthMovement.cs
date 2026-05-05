using UnityEngine;

namespace RopeToolkit.Example;

public class BackAndForthMovement : MonoBehaviour
{
	public Vector3 amount = new Vector3(2f, 0f, 0f);

	protected Vector3 startPos;

	public void Start()
	{
		startPos = base.transform.position;
	}

	public void Update()
	{
		base.transform.position = startPos + amount * Mathf.Sin(Time.time);
	}
}
