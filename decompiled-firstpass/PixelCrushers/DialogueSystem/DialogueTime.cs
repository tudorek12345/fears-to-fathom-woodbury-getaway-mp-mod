using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public static class DialogueTime
{
	public enum TimeMode
	{
		Realtime,
		Gameplay,
		Custom
	}

	private static TimeMode m_mode;

	private static float m_customTime;

	private static float m_customDeltaTime;

	private static bool m_isPaused;

	private static float s_realtimeWhenPaused;

	private static float s_totalRealtimePaused;

	public static TimeMode mode
	{
		get
		{
			return m_mode;
		}
		set
		{
			m_mode = value;
			switch (value)
			{
			case TimeMode.Realtime:
				GameTime.mode = GameTimeMode.Realtime;
				break;
			case TimeMode.Gameplay:
				GameTime.mode = GameTimeMode.UnityStandard;
				break;
			case TimeMode.Custom:
				GameTime.mode = GameTimeMode.Manual;
				break;
			}
		}
	}

	public static float time
	{
		get
		{
			return mode switch
			{
				TimeMode.Gameplay => Time.time, 
				TimeMode.Custom => m_customTime, 
				_ => (m_isPaused ? s_realtimeWhenPaused : Time.realtimeSinceStartup) - s_totalRealtimePaused, 
			};
		}
		set
		{
			m_customTime = value;
			GameTime.time = value;
		}
	}

	public static float deltaTime
	{
		get
		{
			switch (mode)
			{
			default:
				if (!m_isPaused)
				{
					return Time.unscaledDeltaTime;
				}
				return 0f;
			case TimeMode.Gameplay:
				return Time.deltaTime;
			case TimeMode.Custom:
				return m_customDeltaTime;
			}
		}
		set
		{
			m_customDeltaTime = value;
		}
	}

	public static bool isPaused
	{
		get
		{
			return mode switch
			{
				TimeMode.Gameplay => Tools.ApproximatelyZero(Time.timeScale), 
				_ => m_isPaused, 
			};
		}
		set
		{
			switch (mode)
			{
			case TimeMode.Realtime:
				if (!m_isPaused && value)
				{
					s_realtimeWhenPaused = Time.realtimeSinceStartup;
				}
				else if (m_isPaused && !value)
				{
					s_totalRealtimePaused += Time.realtimeSinceStartup - s_realtimeWhenPaused;
				}
				break;
			case TimeMode.Gameplay:
				Time.timeScale = ((!m_isPaused) ? 1 : 0);
				break;
			}
			m_isPaused = value;
			GameTime.isPaused = value;
		}
	}

	public static TimeMode Mode
	{
		get
		{
			return mode;
		}
		set
		{
			mode = value;
		}
	}

	public static bool IsPaused
	{
		get
		{
			return isPaused;
		}
		set
		{
			isPaused = value;
		}
	}

	static DialogueTime()
	{
		mode = TimeMode.Realtime;
	}

	public static IEnumerator WaitForSeconds(float seconds)
	{
		float start = time;
		while (time < start + seconds)
		{
			yield return null;
		}
	}
}
