using System;
using UnityEngine;

namespace UI.ThreeDimensional;

public static class UIObject3DTimer
{
	private static UIObject3DTimerComponent _timerComponent;

	private static UIObject3DTimerComponent timerComponent
	{
		get
		{
			if (_timerComponent == null)
			{
				_timerComponent = UnityEngine.Object.FindObjectOfType<UIObject3DTimerComponent>();
				if (_timerComponent == null && !IsQuitting)
				{
					GameObject gameObject = new GameObject("UIObject3DTimer");
					_timerComponent = gameObject.AddComponent<UIObject3DTimerComponent>();
					UnityEngine.Object.DontDestroyOnLoad(gameObject);
				}
			}
			return _timerComponent;
		}
	}

	public static bool IsFirstFrame => Time.frameCount <= 1;

	private static bool IsQuitting { get; set; }

	[RuntimeInitializeOnLoadMethod]
	public static void OnLoad()
	{
		Application.quitting += delegate
		{
			IsQuitting = true;
		};
	}

	public static WaitForSecondsRealtime GetWaitForSecondsRealtimeInstruction(float seconds)
	{
		return new WaitForSecondsRealtime(seconds);
	}

	public static WaitForSeconds GetWaitForSecondsInstruction(float seconds)
	{
		return new WaitForSeconds(seconds);
	}

	private static void EditorUpdate()
	{
	}

	public static void DelayedCall(float delay, Action action, MonoBehaviour actionTarget, bool forceEvenIfObjectIsInactive = false)
	{
		if (Application.isPlaying && timerComponent != null)
		{
			timerComponent.DelayedCall(delay, action, actionTarget, forceEvenIfObjectIsInactive);
		}
	}

	public static void AtEndOfFrame(Action action, MonoBehaviour actionTarget, bool forceEvenIfObjectIsInactive = false)
	{
		DelayedCall(0f, action, actionTarget, forceEvenIfObjectIsInactive);
	}
}
