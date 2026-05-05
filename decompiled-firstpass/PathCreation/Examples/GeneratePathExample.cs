using System.Collections.Generic;
using UnityEngine;

namespace PathCreation.Examples;

[RequireComponent(typeof(PathCreator))]
public class GeneratePathExample : MonoBehaviour
{
	public bool closedLoop = true;

	public Transform[] waypoints;

	private void Start()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		if (waypoints.Length != 0)
		{
			BezierPath bezierPath = new BezierPath((IEnumerable<Transform>)waypoints, closedLoop, (PathSpace)0);
			GetComponent<PathCreator>().bezierPath = bezierPath;
		}
	}
}
