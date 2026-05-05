using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands;

[AddComponentMenu("")]
public class SequencerCommandAnimation : SequencerCommand
{
	private Transform subject;

	private int nextAnimationIndex = 2;

	private float stopTime;

	private Animation anim;

	public void Start()
	{
		string parameter = GetParameter(0);
		subject = GetSubject(1);
		nextAnimationIndex = 2;
		anim = ((subject == null) ? null : subject.GetComponent<Animation>());
		if (subject == null || anim == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: Animation({1}, {2},...) command: No Animation component found on {2}.", new object[3]
				{
					"Dialogue System",
					parameter,
					(subject != null) ? subject.name : GetParameter(1)
				}));
			}
			return;
		}
		if (string.IsNullOrEmpty(parameter))
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: Animation({1}, {2},...) command: Animation name is blank.", new object[3] { "Dialogue System", parameter, subject.name }));
			}
			return;
		}
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Sequencer: Animation({1}, {2},...)", new object[3]
			{
				"Dialogue System",
				parameter,
				Tools.GetObjectName(subject)
			}));
		}
		TryAnimationClip(parameter);
	}

	private void TryAnimationClip(string clipName)
	{
		try
		{
			anim.CrossFade(clipName);
			stopTime = DialogueTime.time + Mathf.Max(0.1f, anim[clipName].length - 0.3f);
		}
		catch (Exception)
		{
			stopTime = 0f;
		}
	}

	public void Update()
	{
		if (DialogueTime.time >= stopTime)
		{
			if (nextAnimationIndex < base.parameters.Length)
			{
				TryAnimationClip(GetParameter(nextAnimationIndex));
				nextAnimationIndex++;
			}
			if (nextAnimationIndex >= base.parameters.Length)
			{
				Stop();
			}
		}
	}
}
