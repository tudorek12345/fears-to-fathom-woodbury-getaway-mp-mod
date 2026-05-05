using System.Runtime.Serialization;
using UnityEngine;

namespace PixelCrushers;

public class QuaternionSerializationSurrogate : ISerializationSurrogate
{
	public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
	{
		Quaternion quaternion = (Quaternion)obj;
		info.AddValue("w", quaternion.w);
		info.AddValue("x", quaternion.x);
		info.AddValue("y", quaternion.y);
		info.AddValue("z", quaternion.z);
	}

	public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
	{
		Quaternion quaternion = (Quaternion)obj;
		quaternion.w = (float)info.GetValue("w", typeof(float));
		quaternion.x = (float)info.GetValue("x", typeof(float));
		quaternion.y = (float)info.GetValue("y", typeof(float));
		quaternion.z = (float)info.GetValue("z", typeof(float));
		obj = quaternion;
		return obj;
	}
}
