using UnityEngine;

namespace Obi;

[RequireComponent(typeof(ObiActor))]
public class ColorFromVelocity : MonoBehaviour
{
	private ObiActor actor;

	public float sensibility = 0.2f;

	private void Awake()
	{
		actor = GetComponent<ObiActor>();
	}

	public void OnEnable()
	{
	}

	private void LateUpdate()
	{
		if (base.isActiveAndEnabled && !((Object)(object)actor.solver == null))
		{
			for (int i = 0; i < actor.solverIndices.Length; i++)
			{
				int num = actor.solverIndices[i];
				Vector4 vector = ((ObiNativeList<Vector4>)(object)actor.solver.velocities)[num];
				((ObiNativeList<Color>)(object)actor.solver.colors)[num] = new Color(Mathf.Clamp(vector.x / sensibility, -1f, 1f) * 0.5f + 0.5f, Mathf.Clamp(vector.y / sensibility, -1f, 1f) * 0.5f + 0.5f, Mathf.Clamp(vector.z / sensibility, -1f, 1f) * 0.5f + 0.5f, 1f);
			}
		}
	}
}
