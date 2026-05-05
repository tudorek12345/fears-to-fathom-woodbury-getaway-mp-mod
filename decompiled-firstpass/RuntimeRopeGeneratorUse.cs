using System.Collections;
using Obi;
using UnityEngine;

public class RuntimeRopeGeneratorUse : MonoBehaviour
{
	public ObiCollider pendulum;

	private RuntimeRopeGenerator rg;

	public IEnumerator Start()
	{
		rg = new RuntimeRopeGenerator();
		yield return rg.MakeRope(base.transform, Vector3.zero, 1f);
		rg.AddPendulum(pendulum, Vector3.up * 0.5f);
	}

	public void Update()
	{
		if (Input.GetKey(KeyCode.W))
		{
			rg.ChangeRopeLength(0f - Time.deltaTime);
		}
		if (Input.GetKey(KeyCode.S))
		{
			rg.ChangeRopeLength(Time.deltaTime);
		}
	}
}
