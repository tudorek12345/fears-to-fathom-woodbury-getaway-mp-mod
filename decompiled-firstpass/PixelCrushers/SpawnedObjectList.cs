using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers;

public class SpawnedObjectList : ScriptableObject
{
	[Tooltip("Save unique data on this spawned object's Saver components.")]
	[SerializeField]
	private List<SpawnedObject> m_spawnedObjectPrefabs;

	public List<SpawnedObject> spawnedObjectPrefabs
	{
		get
		{
			return m_spawnedObjectPrefabs;
		}
		set
		{
			m_spawnedObjectPrefabs = value;
		}
	}
}
