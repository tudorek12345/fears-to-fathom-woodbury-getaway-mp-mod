using UnityEngine;

namespace PixelCrushers;

public abstract class DataSerializer : MonoBehaviour
{
	public abstract string Serialize(object data);

	public abstract T Deserialize<T>(string s, T data = default(T));
}
