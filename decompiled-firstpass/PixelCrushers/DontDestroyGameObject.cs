using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class DontDestroyGameObject : MonoBehaviour
{
	private void Awake()
	{
		base.transform.SetParent(null);
		Object.DontDestroyOnLoad(base.gameObject);
	}
}
