using System.Collections.Generic;
using UnityEngine;

namespace EPOOutline;

public class MeshPool
{
	private Queue<Mesh> freeMeshes = new Queue<Mesh>();

	private List<Mesh> allMeshes = new List<Mesh>();

	public Mesh AllocateMesh()
	{
		while (freeMeshes.Count > 0 && freeMeshes.Peek() == null)
		{
			freeMeshes.Dequeue();
		}
		if (freeMeshes.Count == 0)
		{
			Mesh mesh = new Mesh();
			mesh.MarkDynamic();
			allMeshes.Add(mesh);
			freeMeshes.Enqueue(mesh);
		}
		return freeMeshes.Dequeue();
	}

	public void ReleaseAllMeshes()
	{
		freeMeshes.Clear();
		foreach (Mesh allMesh in allMeshes)
		{
			freeMeshes.Enqueue(allMesh);
		}
	}
}
