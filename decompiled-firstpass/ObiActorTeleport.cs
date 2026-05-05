using Obi;
using UnityEngine;

public class ObiActorTeleport : MonoBehaviour
{
	public ObiActor actor;

	public Transform target;

	public void Teleport()
	{
		actor.Teleport(target.position, target.rotation);
	}
}
