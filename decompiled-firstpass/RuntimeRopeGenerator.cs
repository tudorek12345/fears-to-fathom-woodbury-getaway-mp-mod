using System.Collections;
using Obi;
using UnityEngine;

public class RuntimeRopeGenerator
{
	private ObiSolver solver;

	private int pinnedParticle = -1;

	public IEnumerator MakeRope(Transform anchoredTo, Vector3 attachmentOffset, float ropeLength)
	{
		yield return 0;
	}

	public void AddPendulum(ObiCollider pendulum, Vector3 attachmentOffset)
	{
	}

	public void RemovePendulum()
	{
	}

	public void ChangeRopeLength(float changeAmount)
	{
	}

	private void UpdateTethers()
	{
	}
}
