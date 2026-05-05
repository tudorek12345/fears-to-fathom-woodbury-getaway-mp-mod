using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class ShowCursorWhileEnabled : MonoBehaviour
{
	private void OnEnable()
	{
		if (InputDeviceManager.instance == null || InputDeviceManager.deviceUsesCursor)
		{
			CursorControl.SetCursorActive(value: true);
		}
	}

	private void OnDisable()
	{
		if (InputDeviceManager.instance == null || InputDeviceManager.deviceUsesCursor)
		{
			CursorControl.SetCursorActive(value: false);
		}
	}
}
