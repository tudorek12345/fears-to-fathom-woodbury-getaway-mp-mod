using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[AddComponentMenu("")]
public class FadeEffect : GUIEffect
{
	public float fadeInDuration = 0.5f;

	public float duration = 1f;

	public float fadeOutDuration = 0.5f;

	private GUIVisibleControl control;

	public void SetFadeDurations(float fadeInDuration, float duration, float fadeOutDuration)
	{
		this.fadeInDuration = fadeInDuration;
		this.duration = duration;
		this.fadeOutDuration = fadeOutDuration;
	}

	public override IEnumerator Play()
	{
		control = GetComponent<GUIVisibleControl>();
		if (control == null)
		{
			yield break;
		}
		float startTime = DialogueTime.time;
		float endTime = startTime + fadeInDuration;
		while (DialogueTime.time < endTime)
		{
			float num = DialogueTime.time - startTime;
			control.Alpha = num / fadeInDuration;
			yield return null;
		}
		control.Alpha = 1f;
		if (!Tools.ApproximatelyZero(fadeOutDuration))
		{
			yield return StartCoroutine(DialogueTime.WaitForSeconds(duration));
			startTime = DialogueTime.time;
			endTime = startTime + fadeOutDuration;
			while (DialogueTime.time < endTime)
			{
				float num2 = DialogueTime.time - startTime;
				control.Alpha = 1f - num2 / fadeOutDuration;
				yield return null;
			}
			control.Alpha = 0f;
		}
	}
}
