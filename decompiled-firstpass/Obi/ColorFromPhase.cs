using UnityEngine;

namespace Obi;

[RequireComponent(typeof(ObiActor))]
public class ColorFromPhase : MonoBehaviour
{
	private ObiActor actor;

	private void Awake()
	{
		actor = GetComponent<ObiActor>();
	}

	private void LateUpdate()
	{
		if (base.isActiveAndEnabled && !((Object)(object)actor.solver == null))
		{
			for (int i = 0; i < actor.solverIndices.Length; i++)
			{
				int num = actor.solverIndices[i];
				int groupFromPhase = ObiUtils.GetGroupFromPhase(((ObiNativeList<int>)(object)actor.solver.phases)[num]);
				((ObiNativeList<Color>)(object)actor.solver.colors)[num] = ObiUtils.colorAlphabet[groupFromPhase % ObiUtils.colorAlphabet.Length];
			}
		}
	}
}
