using Obi;
using UnityEngine;

public class ActorCOMTransform : MonoBehaviour
{
	public Vector3 offset;

	public ObiActor actor;

	public void Update()
	{
		if ((Object)(object)actor != null && actor.isLoaded)
		{
			Vector3 position = default(Vector3);
			actor.GetMass(ref position);
			base.transform.position = ((Component)(object)actor.solver).transform.TransformPoint(position) + offset;
		}
	}
}
