using System.Collections.Generic;
using Obi;
using UnityEngine;

[RequireComponent(typeof(ObiSolver))]
public class ObiParticleCounter : MonoBehaviour
{
	private ObiSolver solver;

	public int counter;

	public Collider2D targetCollider;

	private ObiCollisionEventArgs frame;

	private HashSet<int> particles = new HashSet<int>();

	private void Awake()
	{
		solver = GetComponent<ObiSolver>();
	}

	private void OnEnable()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		solver.OnCollision += new CollisionCallback(Solver_OnCollision);
	}

	private void OnDisable()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		solver.OnCollision -= new CollisionCallback(Solver_OnCollision);
	}

	private void Solver_OnCollision(object sender, ObiCollisionEventArgs e)
	{
		HashSet<int> other = new HashSet<int>();
		for (int i = 0; i < e.contacts.Count; i++)
		{
			_ = e.contacts.Data[i].distance;
			_ = 0.001f;
		}
		particles.ExceptWith(other);
		counter += particles.Count;
		particles = other;
		Debug.Log(counter);
	}
}
