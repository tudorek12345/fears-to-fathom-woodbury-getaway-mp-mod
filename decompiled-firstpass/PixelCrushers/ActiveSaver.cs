using System;
using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class ActiveSaver : Saver
{
	[Serializable]
	public class Data
	{
		public bool active;
	}

	[Tooltip("GameObject to watch.")]
	[SerializeField]
	private GameObject m_gameObjectToWatch;

	private Data m_data = new Data();

	public GameObject gameObjectToWatch
	{
		get
		{
			return m_gameObjectToWatch;
		}
		set
		{
			m_gameObjectToWatch = value;
		}
	}

	public override string RecordData()
	{
		bool active = gameObjectToWatch != null && gameObjectToWatch.activeSelf;
		m_data.active = active;
		return SaveSystem.Serialize(m_data);
	}

	public override void ApplyData(string s)
	{
		if (gameObjectToWatch == null || string.IsNullOrEmpty(s))
		{
			return;
		}
		Data data = SaveSystem.Deserialize(s, m_data);
		if (data == null)
		{
			return;
		}
		m_data = data;
		bool num = data.active && !gameObjectToWatch.activeSelf;
		if (!data.active)
		{
			gameObjectToWatch.BroadcastMessage("OnBeforeSceneChange", SendMessageOptions.DontRequireReceiver);
			gameObjectToWatch.BroadcastMessage("OnLevelWillBeUnloaded", SendMessageOptions.DontRequireReceiver);
		}
		gameObjectToWatch.SetActive(data.active);
		if (!num)
		{
			return;
		}
		Saver[] componentsInChildren = gameObjectToWatch.GetComponentsInChildren<Saver>();
		foreach (Saver saver in componentsInChildren)
		{
			if (!(saver == this) && saver.enabled)
			{
				saver.ApplyData(SaveSystem.currentSavedGameData.GetData(saver.key));
			}
		}
	}
}
