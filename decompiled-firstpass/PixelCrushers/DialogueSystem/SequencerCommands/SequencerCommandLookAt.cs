using System;
using System.Globalization;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands;

[AddComponentMenu("")]
public class SequencerCommandLookAt : SequencerCommand
{
	private const float SmoothMoveCutoff = 0.05f;

	private Transform target;

	private Transform subject;

	private float duration;

	private float startTime;

	private float endTime;

	private Quaternion originalRotation;

	private Quaternion targetRotation;

	public void Start()
	{
		target = GetSubject(0, base.sequencer.listener);
		subject = GetSubject(1);
		duration = GetParameterAsFloat(2);
		bool flag = !string.Equals(GetParameter(3), "allAxes", StringComparison.OrdinalIgnoreCase);
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format(CultureInfo.InvariantCulture, "{0}: Sequencer: LookAt({1}, {2}, {3})", "Dialogue System", target, subject, duration));
		}
		if (target == null && DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Sequencer: LookAt Target '{1}' wasn't found.", new object[2]
			{
				"Dialogue System",
				GetParameter(0)
			}));
		}
		if (subject == null && DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Sequencer: LookAt Subject '{1}' wasn't found.", new object[2]
			{
				"Dialogue System",
				GetParameter(1)
			}));
		}
		if (subject != null && target != null && subject != target)
		{
			targetRotation = Quaternion.LookRotation(target.position - subject.position, Vector3.up);
			if (flag)
			{
				targetRotation = Quaternion.Euler(subject.rotation.eulerAngles.x, targetRotation.eulerAngles.y, subject.rotation.eulerAngles.z);
			}
			if (duration > 0.05f)
			{
				startTime = DialogueTime.time;
				endTime = startTime + duration;
				originalRotation = subject.rotation;
			}
			else
			{
				Stop();
			}
		}
		else
		{
			Stop();
		}
	}

	public void Update()
	{
		if (DialogueTime.time < endTime)
		{
			float t = (DialogueTime.time - startTime) / duration;
			subject.rotation = Quaternion.Lerp(originalRotation, targetRotation, t);
		}
		else
		{
			Stop();
		}
	}

	public void OnDestroy()
	{
		if (subject != null && target != null)
		{
			subject.rotation = targetRotation;
		}
	}
}
