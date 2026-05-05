using Obi;
using UnityEngine;

[RequireComponent(typeof(ObiSolver))]
public class WorldSpaceGravity : MonoBehaviour
{
	private ObiSolver solver;

	public Vector3 worldGravity = new Vector3(0f, -9.81f, 0f);

	private void Awake()
	{
		solver = GetComponent<ObiSolver>();
	}

	private void Update()
	{
		solver.parameters.gravity = base.transform.InverseTransformDirection(worldGravity);
		solver.PushSolverParameters();
	}
}
