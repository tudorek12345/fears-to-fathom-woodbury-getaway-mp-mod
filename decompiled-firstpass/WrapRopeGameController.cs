using Obi;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(ObiSolver))]
public class WrapRopeGameController : MonoBehaviour
{
	private ObiSolver solver;

	public Wrappable[] wrappables;

	public UnityEvent onFinish = new UnityEvent();

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

	private void Update()
	{
		bool flag = true;
		Wrappable[] array = wrappables;
		for (int i = 0; i < array.Length; i++)
		{
			if (!array[i].IsWrapped())
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			onFinish.Invoke();
		}
	}

	private void Solver_OnCollision(ObiSolver s, ObiCollisionEventArgs e)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		Wrappable[] array = wrappables;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Reset();
		}
		ObiColliderWorld instance = ObiColliderWorld.GetInstance();
		foreach (Contact contact in e.contacts)
		{
			if (!(contact.distance < 0.025f))
			{
				continue;
			}
			ObiColliderBase owner = ((ObiResourceHandle<ObiColliderBase>)(object)instance.colliderHandles[contact.bodyB]).owner;
			if ((Object)(object)owner != null)
			{
				Wrappable component = ((Component)(object)owner).GetComponent<Wrappable>();
				if (component != null)
				{
					component.SetWrapped();
				}
			}
		}
	}
}
