using System.Collections.Generic;
using UnityEngine;
using ch.sycoforge.Decal;
using ch.sycoforge.Decal.Projectors;

[RequireComponent(typeof(EasyDecal))]
public class CandidateFilter : MonoBehaviour
{
	public GameObject ExclusiveReceiver;

	private EasyDecal decal;

	private void Start()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		decal = GetComponent<EasyDecal>();
		Projector projector = ((DecalBase)decal).Projector;
		if (projector != null && projector is BoxProjector)
		{
			((BoxProjector)((projector is BoxProjector) ? projector : null)).OnCandidatesProcessed += new CandidateProcessHandler(bp_OnCandidatesProcessed);
		}
	}

	private void bp_OnCandidatesProcessed(List<Collider> colliders)
	{
		List<Collider> list = new List<Collider>();
		foreach (Collider collider in colliders)
		{
			if (!collider.gameObject.Equals(ExclusiveReceiver))
			{
				list.Add(collider);
			}
		}
		foreach (Collider item in list)
		{
			colliders.Remove(item);
		}
	}
}
