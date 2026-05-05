using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace PixelCrushers;

[AddComponentMenu("")]
public class TimedEvent : MonoBehaviour
{
	public enum TimingMode
	{
		Seconds,
		Frames
	}

	[Tooltip("Count duration in seconds or number of frames.")]
	[SerializeField]
	private TimingMode m_mode;

	[Tooltip("After starting timer, wait this many seconds before firing event. Mode must be set to Seconds.")]
	[SerializeField]
	private float m_duration;

	[Tooltip("After starting timer, wait this many frames before firing event. Mode must be set to Frames.")]
	[SerializeField]
	private int m_frames;

	[Tooltip("Start timer when this component starts.")]
	[SerializeField]
	private bool m_activateOnStart = true;

	[SerializeField]
	private UnityEvent m_onTimeReached = new UnityEvent();

	public TimingMode mode
	{
		get
		{
			return m_mode;
		}
		set
		{
			m_mode = value;
		}
	}

	public float duration
	{
		get
		{
			return m_duration;
		}
		set
		{
			m_duration = value;
		}
	}

	public int frames
	{
		get
		{
			return m_frames;
		}
		set
		{
			m_frames = value;
		}
	}

	public bool activateOnStart
	{
		get
		{
			return m_activateOnStart;
		}
		set
		{
			m_activateOnStart = value;
		}
	}

	private UnityEvent onTimeReached
	{
		get
		{
			return m_onTimeReached;
		}
		set
		{
			m_onTimeReached = value;
		}
	}

	protected virtual void Start()
	{
		if (activateOnStart)
		{
			StartTimer(duration);
		}
	}

	protected virtual void OnDisable()
	{
		CancelTimer();
	}

	public virtual void StartTimer()
	{
		StartTimer(duration);
	}

	public virtual void StartTimer(float duration)
	{
		if (mode == TimingMode.Frames)
		{
			StartCoroutine(CountFrames());
		}
		else
		{
			Invoke("TimeReached", duration);
		}
	}

	private IEnumerator CountFrames()
	{
		for (int i = 0; i < frames; i++)
		{
			yield return null;
		}
		TimeReached();
	}

	protected virtual void CancelTimer()
	{
		CancelInvoke("TimeReached");
		StopAllCoroutines();
	}

	protected virtual void TimeReached()
	{
		onTimeReached.Invoke();
	}

	public void DestroyUnityObject(Object o)
	{
		Object.Destroy(o);
	}
}
