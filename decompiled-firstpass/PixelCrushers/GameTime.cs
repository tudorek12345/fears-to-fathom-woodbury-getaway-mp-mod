using UnityEngine;

namespace PixelCrushers;

public static class GameTime
{
	private static GameTimeMode s_mode;

	private static float s_manualTime;

	private static float s_manualDeltaTime;

	private static bool s_manualPaused;

	public static GameTimeMode mode
	{
		get
		{
			return s_mode;
		}
		set
		{
			s_mode = value;
		}
	}

	public static float time
	{
		get
		{
			return mode switch
			{
				GameTimeMode.Realtime => Time.realtimeSinceStartup, 
				GameTimeMode.Manual => s_manualTime, 
				_ => Time.time, 
			};
		}
		set
		{
			s_manualTime = value;
		}
	}

	public static float deltaTime
	{
		get
		{
			return mode switch
			{
				GameTimeMode.Realtime => Time.unscaledDeltaTime, 
				GameTimeMode.Manual => s_manualDeltaTime, 
				_ => Time.deltaTime, 
			};
		}
		set
		{
			s_manualDeltaTime = value;
		}
	}

	public static float timeScale => Time.timeScale;

	public static bool isPaused
	{
		get
		{
			return mode switch
			{
				GameTimeMode.Realtime => false, 
				GameTimeMode.Manual => s_manualPaused, 
				_ => Mathf.Approximately(0f, Time.timeScale), 
			};
		}
		set
		{
			switch (mode)
			{
			default:
				Time.timeScale = ((!value) ? 1 : 0);
				break;
			case GameTimeMode.Manual:
				s_manualPaused = value;
				break;
			case GameTimeMode.Realtime:
				break;
			}
		}
	}
}
