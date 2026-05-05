using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class InputDeviceMethods : MonoBehaviour
{
	public void UseJoystick()
	{
		if (!(InputDeviceManager.instance == null))
		{
			InputDeviceManager.instance.SetInputDevice(InputDevice.Joystick);
		}
	}

	public void UseKeyboard()
	{
		if (!(InputDeviceManager.instance == null))
		{
			InputDeviceManager.instance.SetInputDevice(InputDevice.Keyboard);
		}
	}

	public void UseMouse()
	{
		if (!(InputDeviceManager.instance == null))
		{
			InputDeviceManager.instance.SetInputDevice(InputDevice.Mouse);
		}
	}

	public void UseTouch()
	{
		if (!(InputDeviceManager.instance == null))
		{
			InputDeviceManager.instance.SetInputDevice(InputDevice.Touch);
		}
	}

	public void SetCursor(bool visible)
	{
		if (!(InputDeviceManager.instance == null))
		{
			InputDeviceManager.instance.SetCursor(visible);
		}
	}

	public void ForceCursor(bool visible)
	{
		if (!(InputDeviceManager.instance == null))
		{
			InputDeviceManager.instance.ForceCursor(visible);
		}
	}

	public void BrieflyIgnoreMouseMovement()
	{
		if (!(InputDeviceManager.instance == null))
		{
			InputDeviceManager.instance.BrieflyIgnoreMouseMovement();
		}
	}

	public void AllowInput(bool value)
	{
		InputDeviceManager.isInputAllowed = value;
	}
}
