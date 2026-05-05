using Obi;
using UnityEngine;

[RequireComponent(typeof(ObiSolver))]
public class ColliderHighlighter : MonoBehaviour
{
	private ObiSolver solver;

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
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		ObiColliderWorld instance = ObiColliderWorld.GetInstance();
		Contact[] data = e.contacts.Data;
		for (int i = 0; i < e.contacts.Count; i++)
		{
			Contact val = data[i];
			if (!(val.distance < 0.01f))
			{
				continue;
			}
			ObiColliderBase owner = ((ObiResourceHandle<ObiColliderBase>)(object)instance.colliderHandles[val.bodyB]).owner;
			if ((Object)(object)owner != null)
			{
				Blinker component = ((Component)(object)owner).GetComponent<Blinker>();
				if ((bool)component)
				{
					component.Blink();
				}
			}
		}
	}
}
