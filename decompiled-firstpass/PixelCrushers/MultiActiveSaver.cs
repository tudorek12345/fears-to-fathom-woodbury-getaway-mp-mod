using System;
using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class MultiActiveSaver : Saver
{
	[Serializable]
	public class Data
	{
		public bool[] active;
	}

	[Tooltip("GameObjects to watch.")]
	[SerializeField]
	private GameObject[] m_gameObjectsToWatch;

	private Data m_data = new Data();

	public GameObject[] gameObjectsToWatch
	{
		get
		{
			return m_gameObjectsToWatch;
		}
		set
		{
			m_gameObjectsToWatch = value;
		}
	}

	public override string RecordData()
	{
		if (gameObjectsToWatch == null)
		{
			return string.Empty;
		}
		if (m_data.active == null || m_data.active.Length != gameObjectsToWatch.Length)
		{
			m_data.active = new bool[gameObjectsToWatch.Length];
		}
		for (int i = 0; i < gameObjectsToWatch.Length; i++)
		{
			m_data.active[i] = gameObjectsToWatch[i] != null && gameObjectsToWatch[i].activeSelf;
		}
		return SaveSystem.Serialize(m_data);
	}

	public override void ApplyData(string s)
	{
		if (gameObjectsToWatch == null || string.IsNullOrEmpty(s))
		{
			return;
		}
		Data data = SaveSystem.Deserialize(s, m_data);
		if (data == null || data.active == null)
		{
			return;
		}
		m_data = data;
		for (int i = 0; i < Mathf.Min(data.active.Length, gameObjectsToWatch.Length); i++)
		{
			if (!(gameObjectsToWatch[i] == null) && !data.active[i])
			{
				gameObjectsToWatch[i].BroadcastMessage("OnBeforeSceneChange", SendMessageOptions.DontRequireReceiver);
				gameObjectsToWatch[i].BroadcastMessage("OnLevelWillBeUnloaded", SendMessageOptions.DontRequireReceiver);
			}
		}
		for (int j = 0; j < Mathf.Min(data.active.Length, gameObjectsToWatch.Length); j++)
		{
			if (gameObjectsToWatch[j] == null)
			{
				continue;
			}
			bool num = data.active[j] && !gameObjectsToWatch[j].activeSelf;
			gameObjectsToWatch[j].SetActive(data.active[j]);
			if (!num)
			{
				continue;
			}
			Saver[] componentsInChildren = gameObjectsToWatch[j].GetComponentsInChildren<Saver>();
			foreach (Saver saver in componentsInChildren)
			{
				if (!(saver == this) && saver.enabled)
				{
					saver.ApplyData(SaveSystem.currentSavedGameData.GetData(saver.key));
				}
			}
		}
	}
}
