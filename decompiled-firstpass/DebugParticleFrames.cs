using Obi;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(ObiActor))]
public class DebugParticleFrames : MonoBehaviour
{
	private ObiActor actor;

	public float size = 1f;

	public void Awake()
	{
		actor = GetComponent<ObiActor>();
	}

	private void OnDrawGizmos()
	{
		Vector4 vector = new Vector4(1f, 0f, 0f, 0f);
		Vector4 vector2 = new Vector4(0f, 1f, 0f, 0f);
		Vector4 vector3 = new Vector4(0f, 0f, 1f, 0f);
		for (int i = 0; i < actor.activeParticleCount; i++)
		{
			Vector3 particlePosition = actor.GetParticlePosition(actor.solverIndices[i]);
			Quaternion particleOrientation = actor.GetParticleOrientation(actor.solverIndices[i]);
			Gizmos.color = Color.red;
			Gizmos.DrawRay(particlePosition, particleOrientation * vector * size);
			Gizmos.color = Color.green;
			Gizmos.DrawRay(particlePosition, particleOrientation * vector2 * size);
			Gizmos.color = Color.blue;
			Gizmos.DrawRay(particlePosition, particleOrientation * vector3 * size);
		}
	}
}
