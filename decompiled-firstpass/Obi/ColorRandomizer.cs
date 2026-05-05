using UnityEngine;

namespace Obi;

[RequireComponent(typeof(ObiActor))]
public class ColorRandomizer : MonoBehaviour
{
	private ObiActor actor;

	public Gradient gradient = new Gradient();

	private void Start()
	{
		actor = GetComponent<ObiActor>();
		for (int i = 0; i < actor.solverIndices.Length; i++)
		{
			((ObiNativeList<Color>)(object)actor.solver.colors)[actor.solverIndices[i]] = gradient.Evaluate(Random.value);
		}
	}
}
