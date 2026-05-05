using System;
using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands;

[AddComponentMenu("")]
public class SequencerCommandAudioWaitOnce : SequencerCommand
{
	private static string _VarPrefix = "once_";

	private float _stopTime;

	private AudioSource _audioSource;

	private int _nextClipIndex = 2;

	private AudioClip _currentClip;

	private AudioClip _originalClip;

	private bool _restoreOriginalClip;

	protected bool isLoadingAudio;

	public IEnumerator Start()
	{
		string audioClipName = GetParameter(0);
		Transform subject = GetSubject(1);
		_nextClipIndex = 2;
		if (audioClipName == null || audioClipName.Length < 1)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarningFormat("{0}: Sequencer: AudioWaitOnce(): no audio clip name given", "Dialogue System");
			}
			if (!hasNextClip())
			{
				Stop();
			}
		}
		if (DialogueDebug.logInfo)
		{
			Debug.LogFormat("{0}: Sequencer: AudioWaitOnce({1})", "Dialogue System", GetParameters());
		}
		if (hasPlayedAlready(audioClipName) && DialogueDebug.logInfo)
		{
			Debug.LogFormat("{0}: Sequencer: AudioWaitOnce(): clip {1} already played, skipping", "Dialogue System", audioClipName);
			if (!hasNextClip())
			{
				Stop();
			}
		}
		_audioSource = SequencerTools.GetAudioSource(subject);
		if (_audioSource == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarningFormat("{0}: Sequencer: AudioWaitOnce(): can't find or add AudioSource to {1}.", "Dialogue System", subject.name);
			}
			Stop();
		}
		else
		{
			_originalClip = _audioSource.clip;
			_stopTime = DialogueTime.time + 1f;
			yield return null;
			_originalClip = _audioSource.clip;
			TryAudioClip(audioClipName);
		}
	}

	private void TryAudioClip(string audioClipName)
	{
		try
		{
			if (string.IsNullOrEmpty(audioClipName))
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning(string.Format("{0}: Sequencer: AudioWait() command: Audio clip name is blank.", new object[1] { "Dialogue System" }));
				}
				_stopTime = 0f;
				return;
			}
			if (hasPlayedAlready(audioClipName))
			{
				Debug.LogFormat("{0}: Sequencer: AudioWaitOnce(): clip {1} already played, skipping", "Dialogue System", audioClipName);
				_stopTime = DialogueTime.time;
				return;
			}
			isLoadingAudio = true;
			DialogueManager.LoadAsset(audioClipName, typeof(AudioClip), delegate(UnityEngine.Object asset)
			{
				isLoadingAudio = false;
				AudioClip audioClip = asset as AudioClip;
				if (audioClip == null)
				{
					if (DialogueDebug.logWarnings && Sequencer.reportMissingAudioFiles)
					{
						Debug.LogWarning(string.Format("{0}: Sequencer: AudioWait() command: Clip '{1}' wasn't found.", new object[2] { "Dialogue System", audioClipName }));
					}
					_stopTime = 0f;
				}
				else
				{
					if (IsAudioMuted())
					{
						if (DialogueDebug.logInfo)
						{
							Debug.Log(string.Format("{0}: Sequencer: AudioWait(): waiting but not playing '{1}'; audio is muted.", new object[2] { "Dialogue System", audioClipName }));
						}
					}
					else if (_audioSource != null)
					{
						if (DialogueDebug.logInfo)
						{
							Debug.Log(string.Format("{0}: Sequencer: AudioWait(): playing '{1}'.", new object[2] { "Dialogue System", audioClipName }));
						}
						_currentClip = audioClip;
						_audioSource.clip = audioClip;
						_audioSource.Play();
					}
					_stopTime = DialogueTime.time + audioClip.length;
				}
			});
		}
		catch (Exception)
		{
			_stopTime = 0f;
		}
	}

	private string buildOnceVarName(string audioClipName)
	{
		return _VarPrefix + audioClipName;
	}

	private bool hasPlayedAlready(string audioClipName)
	{
		return DialogueLua.GetVariable(buildOnceVarName(audioClipName)).asBool;
	}

	private void markAsPlayedAlready(string audioClipName)
	{
		DialogueLua.SetVariable(buildOnceVarName(audioClipName), true);
	}

	private bool hasNextClip()
	{
		return _nextClipIndex < base.parameters.Length;
	}

	public void Update()
	{
		if (!(DialogueTime.time >= _stopTime))
		{
			return;
		}
		DialogueManager.UnloadAsset(_currentClip);
		_currentClip = null;
		if (!isLoadingAudio)
		{
			if (hasNextClip())
			{
				TryAudioClip(GetParameter(_nextClipIndex));
				_nextClipIndex++;
			}
			else
			{
				_currentClip = null;
				Stop();
			}
		}
	}

	public void OnDialogueSystemPause()
	{
		if (!(_audioSource == null))
		{
			_audioSource.Pause();
		}
	}

	public void OnDialogueSystemUnpause()
	{
		if (!(_audioSource == null))
		{
			_audioSource.Play();
		}
	}

	public void OnDestroy()
	{
		if (_audioSource != null)
		{
			if (_audioSource.isPlaying && _audioSource.clip == _currentClip)
			{
				_audioSource.Stop();
			}
			if (_restoreOriginalClip)
			{
				_audioSource.clip = _originalClip;
			}
			DialogueManager.UnloadAsset(_currentClip);
		}
	}
}
