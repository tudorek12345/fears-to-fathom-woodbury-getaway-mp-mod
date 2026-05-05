using System.Globalization;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands;

[AddComponentMenu("")]
public class SequencerCommandAnimatorFloat : SequencerCommand
{
	private const float SmoothMoveCutoff = 0.05f;

	private int animatorParameterHash = -1;

	private float targetValue;

	private Transform subject;

	private float duration;

	private Animator animator;

	private float startTime;

	private float endTime;

	private float originalValue;

	public void Start()
	{
		string parameter = GetParameter(0);
		animatorParameterHash = Animator.StringToHash(parameter);
		targetValue = GetParameterAsFloat(1, 1f);
		subject = GetSubject(2);
		duration = GetParameterAsFloat(3);
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format(CultureInfo.InvariantCulture, "{0}: Sequencer: AnimatorFloat({1}, {2}, {3}, {4})", "Dialogue System", parameter, targetValue, subject, duration));
		}
		if (subject == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: AnimatorFloat(): subject '{1}' wasn't found.", new object[2]
				{
					"Dialogue System",
					GetParameter(2)
				}));
			}
			Stop();
			return;
		}
		animator = subject.GetComponentInChildren<Animator>();
		if (animator == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: AnimatorFloat(): no Animator found on '{1}'.", new object[2] { "Dialogue System", subject.name }));
			}
			Stop();
		}
		else if (duration < 0.05f)
		{
			Stop();
		}
		else
		{
			startTime = DialogueTime.time;
			endTime = startTime + duration;
			originalValue = animator.GetFloat(animatorParameterHash);
		}
	}

	public void Update()
	{
		if (DialogueTime.time < endTime)
		{
			float num = (DialogueTime.time - startTime) / duration;
			float value = Mathf.Lerp(originalValue, targetValue, num / duration);
			if (animator != null)
			{
				animator.SetFloat(animatorParameterHash, value);
			}
		}
		else
		{
			Stop();
		}
	}

	public void OnDestroy()
	{
		if (animator != null)
		{
			animator.SetFloat(animatorParameterHash, targetValue);
		}
	}
}
