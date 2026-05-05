using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands;

[AddComponentMenu("")]
public class SequencerCommandVoice : SequencerCommand
{
	private const float DefaultCrossfadeDuration = 0.3f;

	private float stopTime;

	private Transform subject;

	private string finalClipName = string.Empty;

	private Animation anim;

	private Animator animator;

	private AudioSource audioSource;

	private AudioClip audioClip;

	private int layer = -1;

	private float crossfadeDuration = 0.3f;

	public void Start()
	{
		string audioClipName = GetParameter(0);
		string animationClipName = GetParameter(1);
		finalClipName = GetParameter(2);
		subject = GetSubject(3);
		crossfadeDuration = GetParameterAsFloat(4, 0.3f);
		layer = GetParameterAsInt(5, -1);
		anim = ((subject == null) ? null : subject.GetComponent<Animation>());
		animator = ((subject == null) ? null : subject.GetComponent<Animator>());
		if (anim == null && animator == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: Voice({1}, {2}, {3}, {4}) command: No Animator or Animation component found on {3}.", "Dialogue System", audioClipName, animationClipName, finalClipName, (subject != null) ? subject.name : GetParameter(3)));
			}
			return;
		}
		if (string.IsNullOrEmpty(audioClipName))
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: Voice({1}, {2}, {3}, {4}) command: Audio clip name is blank.", "Dialogue System", audioClipName, animationClipName, finalClipName, subject.name));
			}
			return;
		}
		if (string.IsNullOrEmpty(animationClipName))
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Sequencer: Voice({1}, {2}, {3}, {4}) command: Animation name is blank.", "Dialogue System", audioClipName, animationClipName, finalClipName, subject.name));
			}
			return;
		}
		DialogueManager.LoadAsset(audioClipName, typeof(AudioClip), delegate(UnityEngine.Object asset)
		{
			audioClip = asset as AudioClip;
			if (audioClip == null)
			{
				if (DialogueDebug.logWarnings && Sequencer.reportMissingAudioFiles)
				{
					Debug.LogWarning(string.Format("{0}: Sequencer: Voice({1}, {2}, {3}, {4}) command: Audio clip is null.", "Dialogue System", audioClipName, animationClipName, finalClipName, subject.name));
				}
				stopTime = 0f;
				return;
			}
			if (IsAudioMuted())
			{
				if (DialogueDebug.logInfo)
				{
					Debug.Log(string.Format("{0}: Sequencer: Voice({1}, {2}, {3}, {4}): Audio is muted; not playing it.", "Dialogue System", audioClipName, animationClipName, finalClipName, Tools.GetObjectName(subject)));
				}
			}
			else
			{
				audioSource = SequencerTools.GetAudioSource(subject);
				audioSource.clip = audioClip;
				audioSource.Play();
			}
			try
			{
				if (animator != null)
				{
					if (Mathf.Approximately(0f, crossfadeDuration))
					{
						animator.Play(animationClipName, layer, 0f);
					}
					else
					{
						animator.CrossFadeInFixedTime(animationClipName, crossfadeDuration, layer);
					}
					stopTime = DialogueTime.time + audioClip.length;
				}
				else
				{
					anim.CrossFade(animationClipName, crossfadeDuration);
					stopTime = DialogueTime.time + Mathf.Max(0.1f, anim[animationClipName].length - 0.3f);
					if (audioClip.length > anim[animationClipName].length)
					{
						stopTime = DialogueTime.time + audioClip.length;
					}
				}
			}
			catch (Exception)
			{
				stopTime = 0f;
			}
		});
	}

	public void Update()
	{
		if (DialogueTime.time >= stopTime)
		{
			Stop();
		}
	}

	public void OnDialogueSystemPause()
	{
		if (!(audioSource == null))
		{
			audioSource.Pause();
		}
	}

	public void OnDialogueSystemUnpause()
	{
		if (!(audioSource == null))
		{
			audioSource.Play();
		}
	}

	public void OnDestroy()
	{
		if (animator != null)
		{
			if (!string.IsNullOrEmpty(finalClipName))
			{
				animator.CrossFadeInFixedTime(finalClipName, crossfadeDuration, layer);
			}
		}
		else if (anim != null)
		{
			if (!string.IsNullOrEmpty(finalClipName))
			{
				anim.CrossFade(finalClipName, crossfadeDuration);
			}
			else if (anim.clip != null)
			{
				anim.CrossFade(anim.clip.name, crossfadeDuration);
			}
		}
		if (audioSource != null && DialogueTime.time < stopTime)
		{
			audioSource.Stop();
		}
		DialogueManager.UnloadAsset(audioClip);
	}
}
