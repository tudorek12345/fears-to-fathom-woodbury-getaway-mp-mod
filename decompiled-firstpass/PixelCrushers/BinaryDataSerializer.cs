using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class BinaryDataSerializer : DataSerializer
{
	protected virtual void AddSurrogateSelectors(SurrogateSelector surrogateSelector)
	{
		surrogateSelector.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), new Vector3SerializationSurrogate());
		surrogateSelector.AddSurrogate(typeof(Quaternion), new StreamingContext(StreamingContextStates.All), new QuaternionSerializationSurrogate());
	}

	protected virtual BinaryFormatter CreateBinaryFormatter()
	{
		BinaryFormatter binaryFormatter = new BinaryFormatter();
		SurrogateSelector surrogateSelector = new SurrogateSelector();
		AddSurrogateSelectors(surrogateSelector);
		binaryFormatter.SurrogateSelector = surrogateSelector;
		return binaryFormatter;
	}

	public override string Serialize(object data)
	{
		if (data == null || !data.GetType().IsSerializable)
		{
			return string.Empty;
		}
		using MemoryStream memoryStream = new MemoryStream();
		CreateBinaryFormatter().Serialize(memoryStream, data);
		return Convert.ToBase64String(memoryStream.ToArray());
	}

	public override T Deserialize<T>(string s, T data = default(T))
	{
		if (string.IsNullOrEmpty(s))
		{
			return default(T);
		}
		using MemoryStream serializationStream = new MemoryStream(Convert.FromBase64String(s));
		data = (T)CreateBinaryFormatter().Deserialize(serializationStream);
		return data;
	}
}
