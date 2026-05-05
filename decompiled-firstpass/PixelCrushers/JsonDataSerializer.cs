using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class JsonDataSerializer : DataSerializer
{
	[Tooltip("Use larger but more human-readable format.")]
	[SerializeField]
	private bool m_prettyPrint;

	public bool prettyPrint
	{
		get
		{
			return m_prettyPrint;
		}
		set
		{
			m_prettyPrint = value;
		}
	}

	public override string Serialize(object data)
	{
		return JsonUtility.ToJson(data, m_prettyPrint);
	}

	public override T Deserialize<T>(string s, T data = default(T))
	{
		if (object.Equals(data, default(T)))
		{
			return JsonUtility.FromJson<T>(s);
		}
		JsonUtility.FromJsonOverwrite(s, data);
		return data;
	}
}
