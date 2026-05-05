using System;
using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class EnabledSaver : Saver
{
	[Serializable]
	public class Data
	{
		public bool enabled;
	}

	[Tooltip("Component to watch.")]
	[SerializeField]
	private Component m_componentToWatch;

	private Data m_data = new Data();

	public Component componentToWatch
	{
		get
		{
			return m_componentToWatch;
		}
		set
		{
			m_componentToWatch = value;
		}
	}

	public override string RecordData()
	{
		bool flag = componentToWatch != null && ComponentUtility.IsComponentEnabled(componentToWatch);
		m_data.enabled = flag;
		return SaveSystem.Serialize(m_data);
	}

	public override void ApplyData(string s)
	{
		if (!(componentToWatch == null) && !string.IsNullOrEmpty(s))
		{
			Data data = SaveSystem.Deserialize(s, m_data);
			if (data != null)
			{
				m_data = data;
				ComponentUtility.SetComponentEnabled(componentToWatch, data.enabled);
			}
		}
	}
}
