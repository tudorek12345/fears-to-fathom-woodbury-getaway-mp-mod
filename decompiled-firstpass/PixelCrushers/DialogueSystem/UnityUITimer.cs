using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class UnityUITimer : MonoBehaviour
{
	private Slider slider;

	private float startTime;

	public virtual void Awake()
	{
		slider = GetComponent<Slider>();
		Tools.DeprecationWarning(this);
	}

	public virtual void StartCountdown(float duration, Action timeoutHandler)
	{
		StartCoroutine(Countdown(duration, timeoutHandler));
	}

	private IEnumerator Countdown(float duration, Action timeoutHandler)
	{
		startTime = DialogueTime.time;
		float endTime = startTime + duration;
		while (DialogueTime.time < endTime)
		{
			float num = DialogueTime.time - startTime;
			UpdateTimeLeft(Mathf.Clamp(1f - num / duration, 0f, 1f));
			yield return null;
		}
		timeoutHandler?.Invoke();
	}

	public void SkipTime(float amountToSkip)
	{
		startTime -= amountToSkip;
	}

	public virtual void UpdateTimeLeft(float normalizedTimeLeft)
	{
		if (!((UnityEngine.Object)(object)slider == null))
		{
			slider.value = normalizedTimeLeft;
		}
	}

	public virtual void OnDisable()
	{
		StopAllCoroutines();
	}
}
