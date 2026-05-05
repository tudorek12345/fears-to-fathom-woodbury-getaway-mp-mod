using System;
using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[AddComponentMenu("")]
public class TimerEffect : GUIEffect
{
	public float duration = 5f;

	private GUIProgressBar progressBar;

	public event Action TimeoutHandler;

	public override IEnumerator Play()
	{
		progressBar = GetComponent<GUIProgressBar>();
		if (!(progressBar == null))
		{
			float startTime = DialogueTime.time;
			float endTime = startTime + duration;
			while (DialogueTime.time < endTime)
			{
				float num = DialogueTime.time - startTime;
				progressBar.progress = Mathf.Clamp(1f - num / duration, 0f, 1f);
				yield return null;
			}
			if (this.TimeoutHandler != null)
			{
				this.TimeoutHandler();
			}
		}
	}
}
