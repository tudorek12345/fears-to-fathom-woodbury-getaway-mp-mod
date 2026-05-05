using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelCrushers;

[AddComponentMenu("")]
public class PositionSaver : Saver
{
	[Serializable]
	public class PositionData
	{
		public int scene = -1;

		public Vector3 position;

		public Quaternion rotation;
	}

	[Serializable]
	public class ScenePositionData
	{
		public int scene;

		public Vector3 position;

		public Quaternion rotation;

		public ScenePositionData(int _scene, Vector3 _position, Quaternion _rotation)
		{
			scene = _scene;
			position = _position;
			rotation = _rotation;
		}
	}

	[Serializable]
	public class MultiscenePositionData
	{
		public List<ScenePositionData> positions = new List<ScenePositionData>();
	}

	[Tooltip("If set, save position of target. Otherwise save this GameObject's position.")]
	[SerializeField]
	private Transform m_target;

	[Tooltip("When changing scenes, if a player spawnpoint is specified, move this GameObject to the spawnpoint.")]
	[SerializeField]
	private bool m_usePlayerSpawnpoint;

	[Tooltip("Record positions in every scene. If unticked, only records position in most recent scene.")]
	[SerializeField]
	private bool m_multiscene;

	protected PositionData m_data;

	protected MultiscenePositionData m_multisceneData;

	public Transform target
	{
		get
		{
			if (!(m_target == null))
			{
				return m_target;
			}
			return base.transform;
		}
		set
		{
			m_target = value;
		}
	}

	public bool usePlayerSpawnpoint
	{
		get
		{
			return m_usePlayerSpawnpoint;
		}
		set
		{
			m_usePlayerSpawnpoint = value;
		}
	}

	protected bool multiscene => m_multiscene;

	public override void Awake()
	{
		base.Awake();
		if (m_multiscene)
		{
			m_multisceneData = new MultiscenePositionData();
		}
		else
		{
			m_data = new PositionData();
		}
	}

	public override string RecordData()
	{
		int buildIndex = SceneManager.GetActiveScene().buildIndex;
		if (multiscene)
		{
			bool flag = false;
			for (int i = 0; i < m_multisceneData.positions.Count; i++)
			{
				if (m_multisceneData.positions[i].scene == buildIndex)
				{
					flag = true;
					m_multisceneData.positions[i].position = target.transform.position;
					m_multisceneData.positions[i].rotation = target.transform.rotation;
					break;
				}
			}
			if (!flag)
			{
				m_multisceneData.positions.Add(new ScenePositionData(buildIndex, target.transform.position, target.transform.rotation));
			}
			return SaveSystem.Serialize(m_multisceneData);
		}
		m_data.scene = buildIndex;
		m_data.position = target.transform.position;
		m_data.rotation = target.transform.rotation;
		return SaveSystem.Serialize(m_data);
	}

	public override void ApplyData(string s)
	{
		if (usePlayerSpawnpoint && SaveSystem.playerSpawnpoint != null)
		{
			SetPosition(SaveSystem.playerSpawnpoint.transform.position, SaveSystem.playerSpawnpoint.transform.rotation);
		}
		else
		{
			if (string.IsNullOrEmpty(s))
			{
				return;
			}
			int buildIndex = SceneManager.GetActiveScene().buildIndex;
			if (multiscene)
			{
				MultiscenePositionData multiscenePositionData = SaveSystem.Deserialize(s, m_multisceneData);
				if (multiscenePositionData == null)
				{
					return;
				}
				m_multisceneData = multiscenePositionData;
				for (int i = 0; i < m_multisceneData.positions.Count; i++)
				{
					if (m_multisceneData.positions[i].scene == buildIndex)
					{
						SetPosition(m_multisceneData.positions[i].position, m_multisceneData.positions[i].rotation);
						break;
					}
				}
				return;
			}
			PositionData positionData = SaveSystem.Deserialize(s, m_data);
			if (positionData != null)
			{
				m_data = positionData;
				if (positionData.scene == buildIndex || positionData.scene == -1)
				{
					SetPosition(positionData.position, positionData.rotation);
				}
			}
		}
	}

	protected virtual void SetPosition(Vector3 position, Quaternion rotation)
	{
		target.transform.position = position;
		target.transform.rotation = rotation;
	}
}
