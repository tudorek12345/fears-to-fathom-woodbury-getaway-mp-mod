using System;
using UnityEngine;

namespace PathCreation.Examples;

[ExecuteInEditMode]
public abstract class PathSceneTool : MonoBehaviour
{
	public PathCreator pathCreator;

	public bool autoUpdate = true;

	protected VertexPath path => pathCreator.path;

	public event Action onDestroyed;

	public void TriggerUpdate()
	{
		PathUpdated();
	}

	protected virtual void OnDestroy()
	{
		if (this.onDestroyed != null)
		{
			this.onDestroyed();
		}
	}

	protected abstract void PathUpdated();
}
