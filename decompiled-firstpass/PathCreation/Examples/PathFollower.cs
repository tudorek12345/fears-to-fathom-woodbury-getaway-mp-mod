using UnityEngine;

namespace PathCreation.Examples;

public class PathFollower : MonoBehaviour
{
	public PathCreator pathCreator;

	public EndOfPathInstruction endOfPathInstruction;

	public float speed = 5f;

	private float distanceTravelled;

	private void Start()
	{
		if ((Object)(object)pathCreator != null)
		{
			pathCreator.pathUpdated += OnPathChanged;
		}
	}

	private void Update()
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)pathCreator != null)
		{
			distanceTravelled += speed * Time.deltaTime;
			base.transform.position = pathCreator.path.GetPointAtDistance(distanceTravelled, endOfPathInstruction);
			base.transform.rotation = pathCreator.path.GetRotationAtDistance(distanceTravelled, endOfPathInstruction);
		}
	}

	private void OnPathChanged()
	{
		distanceTravelled = pathCreator.path.GetClosestDistanceAlongPath(base.transform.position);
	}
}
