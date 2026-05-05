using UnityEngine;

namespace PixelCrushers.Wrappers;

[AddComponentMenu("Pixel Crushers/Common/Misc/Editor Note")]
public class EditorNote : MonoBehaviour
{
	private void Awake()
	{
		Object.Destroy(this);
	}
}
