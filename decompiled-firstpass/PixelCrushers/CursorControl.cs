using UnityEngine;

namespace PixelCrushers;

public static class CursorControl
{
	private static CursorLockMode previousLockMode = CursorLockMode.Locked;

	public static bool isCursorActive
	{
		get
		{
			if (isCursorVisible)
			{
				return !isCursorLocked;
			}
			return false;
		}
	}

	public static bool isCursorVisible => Cursor.visible;

	public static bool isCursorLocked => Cursor.lockState != CursorLockMode.None;

	public static void SetCursorActive(bool value)
	{
		ShowCursor(value);
		LockCursor(!value);
	}

	public static void ShowCursor(bool value)
	{
		Cursor.visible = value;
	}

	public static void LockCursor(bool value)
	{
		if (!value && isCursorLocked)
		{
			previousLockMode = Cursor.lockState;
		}
		Cursor.lockState = (value ? previousLockMode : CursorLockMode.None);
	}
}
