using System;
using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class MultiEnabledSaver : Saver
{
	[Serializable]
	public class Data
	{
		public bool[] active;
	}

	[Tooltip("Components to watch.")]
	[SerializeField]
	private Component[] m_componentsToWatch;

	private Data m_data = new Data();

	public Component[] componentsToWatch
	{
		get
		{
			return m_componentsToWatch;
		}
		set
		{
			m_componentsToWatch = value;
		}
	}

	public override string RecordData()
	{
		if (componentsToWatch == null)
		{
			return string.Empty;
		}
		if (m_data.active == null || m_data.active.Length != componentsToWatch.Length)
		{
			m_data.active = new bool[componentsToWatch.Length];
		}
		for (int i = 0; i < componentsToWatch.Length; i++)
		{
			m_data.active[i] = componentsToWatch[i] != null && ComponentUtility.IsComponentEnabled(componentsToWatch[i]);
		}
		return SaveSystem.Serialize(m_data);
	}

	public override void ApplyData(string s)
	{
		if (componentsToWatch == null || string.IsNullOrEmpty(s))
		{
			return;
		}
		Data data = SaveSystem.Deserialize(s, m_data);
		if (data == null || data.active == null)
		{
			return;
		}
		m_data = data;
		for (int i = 0; i < Mathf.Min(data.active.Length, componentsToWatch.Length); i++)
		{
			if (!(componentsToWatch[i] == null))
			{
				ComponentUtility.SetComponentEnabled(componentsToWatch[i], data.active[i]);
			}
		}
	}
}
