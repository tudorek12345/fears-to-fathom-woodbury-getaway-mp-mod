using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace PixelCrushers;

[AddComponentMenu("")]
public class SpawnedObject : Saver
{
	public enum Mode
	{
		OnDisable,
		OnDestroy
	}

	[Tooltip("Event to watch for to record that object was despawned.")]
	[SerializeField]
	[FormerlySerializedAs("m_mode")]
	private Mode m_despawnMode = Mode.OnDestroy;

	[Tooltip("Save unique data on this spawned object's Saver components.")]
	[SerializeField]
	private bool m_saveUniqueSaverData = true;

	private bool m_ignoreOnDestroy;

	private string m_guid = string.Empty;

	public Mode despawnMode
	{
		get
		{
			return m_despawnMode;
		}
		set
		{
			m_despawnMode = value;
		}
	}

	public Mode mode
	{
		get
		{
			return despawnMode;
		}
		set
		{
			despawnMode = value;
		}
	}

	public bool saveUniqueSaverData
	{
		get
		{
			return m_saveUniqueSaverData;
		}
		set
		{
			m_saveUniqueSaverData = value;
		}
	}

	public string guid
	{
		get
		{
			return m_guid;
		}
		set
		{
			m_guid = value;
		}
	}

	public override void Awake()
	{
		base.Awake();
		m_guid = Guid.NewGuid().ToString();
	}

	public override void Start()
	{
		base.Start();
		AddGuidToSaverKeys();
		SpawnedObjectManager.AddSpawnedObjectData(this);
	}

	protected virtual void AddGuidToSaverKeys()
	{
		if (string.IsNullOrEmpty(guid))
		{
			return;
		}
		Saver[] componentsInChildren = GetComponentsInChildren<Saver>();
		foreach (Saver saver in componentsInChildren)
		{
			if (!saver.key.EndsWith(guid))
			{
				saver.key += guid;
			}
		}
	}

	public override void OnBeforeSceneChange()
	{
		base.OnBeforeSceneChange();
		m_ignoreOnDestroy = true;
	}

	public override void OnDisable()
	{
		base.OnDisable();
		if (m_despawnMode == Mode.OnDisable)
		{
			RecordDestruction();
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (m_despawnMode == Mode.OnDestroy)
		{
			RecordDestruction();
		}
	}

	protected virtual void RecordDestruction()
	{
		if (!m_ignoreOnDestroy)
		{
			SpawnedObjectManager.RemoveSpawnedObjectData(this);
		}
		m_ignoreOnDestroy = false;
	}

	public override string RecordData()
	{
		return string.Empty;
	}

	public override void ApplyData(string data)
	{
	}
}
