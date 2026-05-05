using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class KeepRectTransformOnscreen : MonoBehaviour
{
	private Vector3 originalLocalPosition;

	private void Awake()
	{
		originalLocalPosition = base.transform.localPosition;
	}

	private void LateUpdate()
	{
		Camera main = Camera.main;
		if (!(main == null))
		{
			base.transform.localPosition = originalLocalPosition;
			Vector3 position = main.WorldToViewportPoint(base.transform.position);
			position.x = Mathf.Clamp01(position.x);
			position.y = Mathf.Clamp01(position.y);
			base.transform.position = main.ViewportToWorldPoint(position);
		}
	}
}
