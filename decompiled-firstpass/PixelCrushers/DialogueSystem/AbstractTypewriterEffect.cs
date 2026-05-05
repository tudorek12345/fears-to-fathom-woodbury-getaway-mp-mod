using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public abstract class AbstractTypewriterEffect : MonoBehaviour
{
	[Tooltip("Tick for right-to-left text such as Arabic.")]
	public bool rightToLeft;

	[Tooltip("How fast to type. This is separate from Dialogue Manager > Subtitle Settings > Chars Per Second.")]
	public float charactersPerSecond = 50f;

	[Tooltip("Optional audio clip to play with each character.")]
	public AudioClip audioClip;

	[Tooltip("If specified, randomly use these clips or the main Audio Clip.")]
	public AudioClip[] alternateAudioClips = new AudioClip[0];

	[Tooltip("Optional audio source through which to play the clip.")]
	public AudioSource audioSource;

	[Tooltip("Use AudioSource.PlayOneShot instead of Play. Slightly heavier performance but produces different effect.")]
	public bool usePlayOneShot;

	[Tooltip("If audio clip is still playing from previous character, stop and restart it when typing next character.")]
	public bool interruptAudioClip;

	[Tooltip("Stop audio when typing any of the Silent Characters specified below.")]
	public bool stopAudioOnSilentCharacters;

	[Tooltip("Stop audio when upon reaching a pause code.")]
	public bool stopAudioOnPauseCodes;

	[Tooltip("Don't play audio on these characters.")]
	public string silentCharacters = string.Empty;

	[Tooltip("Play a full pause on these characters.")]
	public string fullPauseCharacters = string.Empty;

	[Tooltip("Play a quarter pause on these characters.")]
	public string quarterPauseCharacters = string.Empty;

	[Tooltip("Duration to pause on when text contains '\\.'")]
	public float fullPauseDuration = 1f;

	[Tooltip("Duration to pause when text contains '\\,'")]
	public float quarterPauseDuration = 0.25f;

	[Tooltip("Ensure this GameObject has only one typewriter effect.")]
	public bool removeDuplicateTypewriterEffects = true;

	[Tooltip("Play using the current text content whenever component is enabled.")]
	public bool playOnEnable = true;

	[Tooltip("Wait one frame to allow layout elements to setup first.")]
	public bool waitOneFrameBeforeStarting;

	[Tooltip("Stop typing when the conversation ends.")]
	public bool stopOnConversationEnd;

	protected bool paused;

	public abstract bool isPlaying { get; }

	public virtual float GetSpeed()
	{
		return charactersPerSecond;
	}

	public virtual void SetSpeed(float charactersPerSecond)
	{
		this.charactersPerSecond = charactersPerSecond;
	}

	public virtual void Awake()
	{
		PreprocessPauseCharacters();
	}

	public abstract void Start();

	public virtual void OnEnable()
	{
		if (stopOnConversationEnd && DialogueManager.hasInstance)
		{
			DialogueManager.instance.conversationEnded -= StopOnConversationEnd;
			DialogueManager.instance.conversationEnded += StopOnConversationEnd;
		}
	}

	public virtual void OnDisable()
	{
		if (stopOnConversationEnd && DialogueManager.hasInstance)
		{
			DialogueManager.instance.conversationEnded -= StopOnConversationEnd;
		}
	}

	public virtual void StopOnConversationEnd(Transform actor)
	{
		if (isPlaying)
		{
			Stop();
		}
	}

	public abstract void Stop();

	public abstract void StartTyping(string text, int fromIndex = 0);

	public abstract void StopTyping();

	public static string StripRPGMakerCodes(string s)
	{
		return UITools.StripRPGMakerCodes(s);
	}

	protected virtual void PreprocessPauseCharacters()
	{
		fullPauseCharacters = fullPauseCharacters.Replace("\\n", "\n");
		quarterPauseCharacters = quarterPauseCharacters.Replace("\\n", "\n");
	}

	protected virtual bool IsFullPauseCharacter(char c)
	{
		return IsCharacterInString(c, fullPauseCharacters);
	}

	protected virtual bool IsQuarterPauseCharacter(char c)
	{
		return IsCharacterInString(c, quarterPauseCharacters);
	}

	protected virtual bool IsSilentCharacter(char c)
	{
		return IsCharacterInString(c, silentCharacters);
	}

	protected bool IsCharacterInString(char c, string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return false;
		}
		for (int i = 0; i < s.Length; i++)
		{
			if (s[i] == c)
			{
				return true;
			}
		}
		return false;
	}

	public virtual void StopCharacterAudio()
	{
		if (audioSource != null)
		{
			audioSource.Stop();
		}
	}

	protected virtual void PlayCharacterAudio(char c)
	{
		PlayCharacterAudio();
	}

	protected virtual void PlayCharacterAudio()
	{
		if (this.audioClip == null || audioSource == null)
		{
			return;
		}
		AudioClip audioClip = null;
		if (alternateAudioClips != null && alternateAudioClips.Length != 0)
		{
			int num = Random.Range(0, alternateAudioClips.Length + 1);
			audioClip = ((num < alternateAudioClips.Length) ? alternateAudioClips[num] : this.audioClip);
		}
		if (interruptAudioClip)
		{
			if (usePlayOneShot)
			{
				if (audioClip != null)
				{
					audioSource.clip = audioClip;
				}
				audioSource.PlayOneShot(audioSource.clip);
				return;
			}
			if (audioSource.isPlaying)
			{
				audioSource.Stop();
			}
			if (audioClip != null)
			{
				audioSource.clip = audioClip;
			}
			audioSource.pitch = Random.Range(0.5f, 1f);
			audioSource.Play();
		}
		else if (!audioSource.isPlaying)
		{
			if (audioClip != null)
			{
				audioSource.clip = audioClip;
			}
			if (usePlayOneShot)
			{
				audioSource.PlayOneShot(audioSource.clip);
			}
			else
			{
				audioSource.Play();
			}
		}
	}

	protected virtual IEnumerator PauseForDuration(float duration)
	{
		paused = true;
		if (stopAudioOnPauseCodes && audioSource != null)
		{
			audioSource.Stop();
		}
		float continueTime = DialogueTime.time + duration;
		int pauseSafeguard = 0;
		while (DialogueTime.time < continueTime && pauseSafeguard < 999)
		{
			pauseSafeguard++;
			yield return null;
		}
		paused = false;
	}
}
