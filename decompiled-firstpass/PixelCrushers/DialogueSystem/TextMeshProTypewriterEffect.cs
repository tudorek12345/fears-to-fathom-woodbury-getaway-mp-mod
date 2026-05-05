using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
[DisallowMultipleComponent]
public class TextMeshProTypewriterEffect : AbstractTypewriterEffect
{
	[Serializable]
	public class AutoScrollSettings
	{
		[Tooltip("Automatically scroll to bottom of scroll rect. Useful for long text. Works best with left justification.")]
		public bool autoScrollEnabled;

		public ScrollRect scrollRect;

		[Tooltip("Optional. Add a UIScrollBarEnabler to main dialogue panel, assign UI elements, then assign it here to automatically enable scrollbar if content is taller than viewport.")]
		public UIScrollbarEnabler scrollbarEnabler;
	}

	protected enum RPGMakerTokenType
	{
		None,
		QuarterPause,
		FullPause,
		SkipToEnd,
		InstantOpen,
		InstantClose
	}

	public AutoScrollSettings autoScrollSettings = new AutoScrollSettings();

	public UnityEvent onBegin = new UnityEvent();

	public UnityEvent onCharacter = new UnityEvent();

	public UnityEvent onEnd = new UnityEvent();

	protected const string RPGMakerCodeQuarterPause = "\\,";

	protected const string RPGMakerCodeFullPause = "\\.";

	protected const string RPGMakerCodeSkipToEnd = "\\^";

	protected const string RPGMakerCodeInstantOpen = "\\>";

	protected const string RPGMakerCodeInstantClose = "\\<";

	protected Dictionary<int, List<RPGMakerTokenType>> rpgMakerTokens = new Dictionary<int, List<RPGMakerTokenType>>();

	protected TMP_Text m_textComponent;

	protected LayoutElement m_layoutElement;

	protected bool started;

	protected int charactersTyped;

	protected Coroutine typewriterCoroutine;

	protected MonoBehaviour coroutineController;

	public override bool isPlaying => typewriterCoroutine != null;

	public bool IsPlaying => isPlaying;

	protected TMP_Text textComponent
	{
		get
		{
			if ((UnityEngine.Object)(object)m_textComponent == null)
			{
				m_textComponent = GetComponent<TMP_Text>();
			}
			return m_textComponent;
		}
	}

	protected LayoutElement layoutElement
	{
		get
		{
			if ((UnityEngine.Object)(object)m_layoutElement == null)
			{
				m_layoutElement = GetComponent<LayoutElement>();
				if ((UnityEngine.Object)(object)m_layoutElement == null)
				{
					m_layoutElement = base.gameObject.AddComponent<LayoutElement>();
				}
			}
			return m_layoutElement;
		}
	}

	protected AudioSource runtimeAudioSource
	{
		get
		{
			if (audioSource == null)
			{
				audioSource = GetComponent<AudioSource>();
			}
			if (audioSource == null && audioClip != null)
			{
				audioSource = base.gameObject.AddComponent<AudioSource>();
				audioSource.playOnAwake = false;
				audioSource.panStereo = 0f;
			}
			return audioSource;
		}
	}

	public override void Awake()
	{
		base.Awake();
		if (removeDuplicateTypewriterEffects)
		{
			RemoveIfDuplicate();
		}
	}

	protected void RemoveIfDuplicate()
	{
		TextMeshProTypewriterEffect[] components = GetComponents<TextMeshProTypewriterEffect>();
		if (components.Length <= 1)
		{
			return;
		}
		TextMeshProTypewriterEffect textMeshProTypewriterEffect = components[0];
		for (int i = 1; i < components.Length; i++)
		{
			if (components[i].GetInstanceID() < textMeshProTypewriterEffect.GetInstanceID())
			{
				textMeshProTypewriterEffect = components[i];
			}
		}
		for (int j = 0; j < components.Length; j++)
		{
			if (components[j] != textMeshProTypewriterEffect)
			{
				UnityEngine.Object.Destroy(components[j]);
			}
		}
	}

	public override void Start()
	{
		if (!IsPlaying && playOnEnable)
		{
			StopTypewriterCoroutine();
			StartTypewriterCoroutine(0);
		}
		started = true;
	}

	public override void OnEnable()
	{
		base.OnEnable();
		if (!IsPlaying && playOnEnable && started)
		{
			StopTypewriterCoroutine();
			StartTypewriterCoroutine(0);
		}
	}

	public override void OnDisable()
	{
		base.OnDisable();
		Stop();
	}

	public void Pause()
	{
		paused = true;
	}

	public void Unpause()
	{
		paused = false;
	}

	public void Rewind()
	{
		charactersTyped = 0;
	}

	public override void StartTyping(string text, int fromIndex = 0)
	{
		StopTypewriterCoroutine();
		textComponent.text = text;
		StartTypewriterCoroutine(fromIndex);
	}

	public override void StopTyping()
	{
		Stop();
	}

	public virtual void PlayText(string text, int fromIndex = 0)
	{
		StopTypewriterCoroutine();
		textComponent.text = text;
		StartTypewriterCoroutine(fromIndex);
	}

	protected virtual void StartTypewriterCoroutine(int fromIndex)
	{
		if (coroutineController == null || !coroutineController.gameObject.activeInHierarchy)
		{
			MonoBehaviour monoBehaviour = GetComponentInParent<AbstractDialogueUI>();
			if (monoBehaviour == null)
			{
				monoBehaviour = DialogueManager.instance;
			}
			coroutineController = monoBehaviour;
			if (coroutineController == null)
			{
				coroutineController = this;
			}
		}
		typewriterCoroutine = coroutineController.StartCoroutine(Play(fromIndex));
	}

	public virtual IEnumerator Play(int fromIndex)
	{
		if ((UnityEngine.Object)(object)textComponent != null && charactersPerSecond > 0f && !string.IsNullOrEmpty(textComponent.text))
		{
			if (waitOneFrameBeforeStarting)
			{
				yield return null;
			}
			textComponent.text = textComponent.text.Replace("<br>", "\n");
			fromIndex = AbstractTypewriterEffect.StripRPGMakerCodes(Tools.StripTextMeshProTags(textComponent.text)).Substring(0, fromIndex).Length;
			ProcessRPGMakerCodes();
			if (runtimeAudioSource != null)
			{
				runtimeAudioSource.clip = audioClip;
			}
			onBegin.Invoke();
			paused = false;
			float delay = 1f / charactersPerSecond;
			float lastTime = DialogueTime.time;
			float elapsed = (float)fromIndex / charactersPerSecond;
			textComponent.maxVisibleCharacters = fromIndex;
			textComponent.ForceMeshUpdate(false, false);
			yield return null;
			textComponent.maxVisibleCharacters = fromIndex;
			textComponent.ForceMeshUpdate(false, false);
			TMP_TextInfo textInfo = textComponent.textInfo;
			if (textInfo == null)
			{
				yield break;
			}
			string parsedText = textComponent.GetParsedText();
			int totalVisibleCharacters = textInfo.characterCount;
			charactersTyped = fromIndex;
			int skippedCharacters = 0;
			while (charactersTyped < totalVisibleCharacters)
			{
				if (!paused)
				{
					float num = DialogueTime.time - lastTime;
					elapsed += num;
					float goal = elapsed * charactersPerSecond + (float)skippedCharacters;
					while ((float)charactersTyped < goal)
					{
						if (rpgMakerTokens.ContainsKey(charactersTyped))
						{
							List<RPGMakerTokenType> tokens = rpgMakerTokens[charactersTyped];
							for (int i = 0; i < tokens.Count; i++)
							{
								switch (tokens[i])
								{
								case RPGMakerTokenType.QuarterPause:
									yield return PauseForDuration(quarterPauseDuration);
									break;
								case RPGMakerTokenType.FullPause:
									yield return PauseForDuration(fullPauseDuration);
									break;
								case RPGMakerTokenType.SkipToEnd:
									charactersTyped = totalVisibleCharacters - 1;
									break;
								case RPGMakerTokenType.InstantOpen:
								{
									bool flag = false;
									while (!flag && charactersTyped < totalVisibleCharacters)
									{
										charactersTyped++;
										skippedCharacters++;
										if (rpgMakerTokens.ContainsKey(charactersTyped) && rpgMakerTokens[charactersTyped].Contains(RPGMakerTokenType.InstantClose))
										{
											flag = true;
										}
									}
									break;
								}
								}
							}
						}
						char c = ((0 <= charactersTyped && charactersTyped < parsedText.Length) ? parsedText[charactersTyped] : ' ');
						if (charactersTyped < totalVisibleCharacters)
						{
							if (IsSilentCharacter(c))
							{
								if (stopAudioOnSilentCharacters)
								{
									StopCharacterAudio();
								}
							}
							else
							{
								PlayCharacterAudio(c);
							}
						}
						onCharacter.Invoke();
						charactersTyped++;
						textComponent.maxVisibleCharacters = charactersTyped;
						if (IsFullPauseCharacter(c))
						{
							yield return DialogueTime.WaitForSeconds(fullPauseDuration);
						}
						else if (IsQuarterPauseCharacter(c))
						{
							yield return DialogueTime.WaitForSeconds(quarterPauseDuration);
						}
					}
				}
				textComponent.maxVisibleCharacters = charactersTyped;
				HandleAutoScroll();
				textComponent.ForceMeshUpdate(false, false);
				lastTime = DialogueTime.time;
				float delayTime = DialogueTime.time + delay;
				int delaySafeguard = 0;
				while (DialogueTime.time < delayTime && delaySafeguard < 999)
				{
					delaySafeguard++;
					yield return null;
				}
			}
		}
		Stop();
	}

	protected void ProcessRPGMakerCodes()
	{
		rpgMakerTokens.Clear();
		string text = textComponent.text;
		string text2 = string.Empty;
		if (!text.Contains("\\"))
		{
			return;
		}
		text = Tools.StripTextMeshProTags(text);
		int num = 0;
		while (!string.IsNullOrEmpty(text) && num < 9999)
		{
			num++;
			if (PeelRPGMakerTokenFromFront(ref text, out var token))
			{
				int length = text2.Length;
				if (!rpgMakerTokens.ContainsKey(length))
				{
					rpgMakerTokens.Add(length, new List<RPGMakerTokenType>());
				}
				rpgMakerTokens[length].Add(token);
			}
			else
			{
				text2 += text[0];
				text = text.Remove(0, 1);
			}
		}
		textComponent.text = Regex.Replace(textComponent.text, "\\\\[\\.\\,\\^\\<\\>]", string.Empty);
	}

	protected bool PeelRPGMakerTokenFromFront(ref string source, out RPGMakerTokenType token)
	{
		token = RPGMakerTokenType.None;
		if (string.IsNullOrEmpty(source) || source.Length < 2 || source[0] != '\\')
		{
			return false;
		}
		string a = source.Substring(0, 2);
		if (string.Equals(a, "\\,"))
		{
			token = RPGMakerTokenType.QuarterPause;
		}
		else if (string.Equals(a, "\\."))
		{
			token = RPGMakerTokenType.FullPause;
		}
		else if (string.Equals(a, "\\^"))
		{
			token = RPGMakerTokenType.SkipToEnd;
		}
		else if (string.Equals(a, "\\>"))
		{
			token = RPGMakerTokenType.InstantOpen;
		}
		else
		{
			if (!string.Equals(a, "\\<"))
			{
				return false;
			}
			token = RPGMakerTokenType.InstantClose;
		}
		source = source.Remove(0, 2);
		return true;
	}

	protected void StopTypewriterCoroutine()
	{
		if (typewriterCoroutine != null)
		{
			if (coroutineController == null)
			{
				StopCoroutine(typewriterCoroutine);
			}
			else
			{
				coroutineController.StopCoroutine(typewriterCoroutine);
			}
			typewriterCoroutine = null;
			coroutineController = null;
		}
	}

	public override void Stop()
	{
		bool num = isPlaying;
		StopTypewriterCoroutine();
		if (num)
		{
			onEnd.Invoke();
			Sequencer.Message("Typed");
		}
		if ((UnityEngine.Object)(object)textComponent != null && textComponent.textInfo != null)
		{
			textComponent.maxVisibleCharacters = textComponent.textInfo.characterCount;
			textComponent.ForceMeshUpdate(false, false);
		}
		HandleAutoScroll();
	}

	protected virtual void HandleAutoScroll()
	{
		if (autoScrollSettings.autoScrollEnabled)
		{
			layoutElement.preferredHeight = Mathf.Max(0f, textComponent.textBounds.size.y);
			if ((UnityEngine.Object)(object)autoScrollSettings.scrollRect != null)
			{
				autoScrollSettings.scrollRect.normalizedPosition = new Vector2(0f, 0f);
			}
			if (autoScrollSettings.scrollbarEnabler != null)
			{
				autoScrollSettings.scrollbarEnabler.CheckScrollbarWithResetValue(0f);
			}
		}
	}
}
