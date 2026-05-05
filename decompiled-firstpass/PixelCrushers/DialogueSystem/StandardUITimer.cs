using System;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class StandardUITimer : MonoBehaviour
{
	private Slider slider;

	private bool m_isCountingDown;

	private float m_startTime;

	private float m_duration;

	private Action m_timeoutHandler;

	public virtual void Awake()
	{
		slider = GetComponent<Slider>();
	}

	public virtual void StartCountdown(float duration, Action timeoutHandler)
	{
		m_isCountingDown = true;
		m_startTime = DialogueTime.time;
		m_duration = duration;
		m_timeoutHandler = timeoutHandler;
	}

	protected virtual void Update()
	{
		if (!m_isCountingDown)
		{
			return;
		}
		float num = DialogueTime.time - m_startTime;
		UpdateTimeLeft(Mathf.Clamp01(1f - num / m_duration));
		if (num >= m_duration)
		{
			m_isCountingDown = false;
			if (m_timeoutHandler != null)
			{
				m_timeoutHandler();
			}
		}
	}

	public virtual void StopCountdown()
	{
		m_isCountingDown = false;
		m_timeoutHandler = null;
	}

	public void SkipTime(float amountToSkip)
	{
		m_startTime -= amountToSkip;
	}

	public virtual void UpdateTimeLeft(float normalizedTimeLeft)
	{
		if (!((UnityEngine.Object)(object)slider == null))
		{
			slider.value = normalizedTimeLeft;
		}
	}
}
