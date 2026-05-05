using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
[DisallowMultipleComponent]
public class UnityUITypewriterEffect : AbstractTypewriterEffect
{
	[Serializable]
	public class AutoScrollSettings
	{
		[Tooltip("Automatically scroll to bottom of scroll rect. Useful for long text. Works best with left justification. Make sure the text has a Content Size Fitter.")]
		public bool autoScrollEnabled;

		public ScrollRect scrollRect;

		[Tooltip("If assigned, the Scrollbar Enabler will be updated with each character to determine if it needs to show the scrollbar.")]
		public UIScrollbarEnabler scrollbarEnabler;

		[Tooltip("If assigned, this should be a copy of the Text component on this typewriter effect. The Sizer Text should have a Content Size Fitter, but the typewriter Text component should not. Make the Sizer Text a parent of the typewriter Text component.")]
		public Text sizerText;
	}

	protected enum TokenType
	{
		Character,
		BoldOpen,
		BoldClose,
		ItalicOpen,
		ItalicClose,
		ColorOpen,
		ColorClose,
		SizeOpen,
		SizeClose,
		Quad,
		Pause,
		InstantOpen,
		InstantClose
	}

	protected class Token
	{
		public TokenType tokenType;

		public char character;

		public string code;

		public float duration;

		public Token(TokenType tokenType, char character, string code, float duration)
		{
			this.tokenType = tokenType;
			this.character = character;
			this.code = code;
			this.duration = duration;
		}
	}

	public AutoScrollSettings autoScrollSettings = new AutoScrollSettings();

	public UnityEvent onBegin = new UnityEvent();

	public UnityEvent onCharacter = new UnityEvent();

	public UnityEvent onEnd = new UnityEvent();

	protected const string RichTextBoldOpen = "<b>";

	protected const string RichTextBoldClose = "</b>";

	protected const string RichTextItalicOpen = "<i>";

	protected const string RichTextItalicClose = "</i>";

	protected const string RichTextColorOpenPrefix = "<color=";

	protected const string RichTextColorClose = "</color>";

	protected const string RichTextSizeOpenPrefix = "<size=";

	protected const string RichTextSizeClose = "</size>";

	protected const string QuadPrefix = "<quad ";

	protected Text control;

	protected bool started;

	protected string original;

	protected string frontSkippedText = string.Empty;

	protected Coroutine typewriterCoroutine;

	protected MonoBehaviour coroutineController;

	protected StringBuilder current;

	protected List<TokenType> openTokenTypes;

	protected List<Token> tokens;

	protected int MaxSafeguard = 16384;

	public override bool isPlaying => typewriterCoroutine != null;

	public bool IsPlaying => isPlaying;

	public override void Awake()
	{
		base.Awake();
		control = GetComponent<Text>();
		if (removeDuplicateTypewriterEffects)
		{
			RemoveIfDuplicate();
		}
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}
		if (audioSource == null && (audioClip != null || (alternateAudioClips != null && alternateAudioClips.Length != 0)))
		{
			audioSource = base.gameObject.AddComponent<AudioSource>();
			audioSource.playOnAwake = false;
			audioSource.panStereo = 0f;
		}
	}

	protected void RemoveIfDuplicate()
	{
		UnityUITypewriterEffect[] components = GetComponents<UnityUITypewriterEffect>();
		if (components.Length <= 1)
		{
			return;
		}
		UnityUITypewriterEffect unityUITypewriterEffect = components[0];
		for (int i = 1; i < components.Length; i++)
		{
			if (components[i].GetInstanceID() < unityUITypewriterEffect.GetInstanceID())
			{
				unityUITypewriterEffect = components[i];
			}
		}
		for (int j = 0; j < components.Length; j++)
		{
			if (components[j] != unityUITypewriterEffect)
			{
				UnityEngine.Object.Destroy(components[j]);
			}
		}
	}

	public override void Start()
	{
		if ((UnityEngine.Object)(object)control != null)
		{
			control.supportRichText = true;
		}
		if (!IsPlaying && playOnEnable)
		{
			original = null;
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
			original = null;
			StopTypewriterCoroutine();
			StartTypewriterCoroutine(0);
		}
	}

	public override void OnDisable()
	{
		base.OnDisable();
		Stop();
	}

	public virtual void Pause()
	{
		paused = true;
	}

	public virtual void Unpause()
	{
		paused = false;
	}

	public override void StartTyping(string text, int fromIndex = 0)
	{
		StopTypewriterCoroutine();
		original = text;
		StartTypewriterCoroutine(fromIndex);
	}

	public override void StopTyping()
	{
		Stop();
	}

	public virtual void PlayText(string text, int fromIndex = 0)
	{
		StartTyping(text, fromIndex);
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

	public virtual IEnumerator Play(int fromIndex = 0)
	{
		if ((UnityEngine.Object)(object)control != null && charactersPerSecond > 0f)
		{
			InitAutoScroll();
			if (waitOneFrameBeforeStarting)
			{
				yield return null;
			}
			if (audioSource != null)
			{
				audioSource.clip = audioClip;
			}
			onBegin.Invoke();
			paused = false;
			float delay = 1f / charactersPerSecond;
			float lastTime = DialogueTime.time;
			float elapsed = 0f;
			int charactersTyped = 0;
			if (original == null)
			{
				original = control.text;
			}
			tokens = Tokenize(original);
			openTokenTypes = new List<TokenType>();
			current = new StringBuilder();
			frontSkippedText = string.Empty;
			int preTyped = 0;
			if (fromIndex > 0)
			{
				int num = 0;
				while (preTyped < fromIndex && tokens.Count > 0 && num < 65535)
				{
					num++;
					Token nextToken = GetNextToken(tokens);
					switch (nextToken.tokenType)
					{
					case TokenType.Character:
						preTyped++;
						if (rightToLeft)
						{
							current.Insert(0, nextToken.character);
						}
						else
						{
							current.Append(nextToken.character);
						}
						break;
					case TokenType.BoldOpen:
					case TokenType.ItalicOpen:
					case TokenType.ColorOpen:
					case TokenType.SizeOpen:
						OpenRichText(current, nextToken, openTokenTypes);
						break;
					case TokenType.BoldClose:
					case TokenType.ItalicClose:
					case TokenType.ColorClose:
					case TokenType.SizeClose:
						CloseRichText(current, nextToken, openTokenTypes);
						break;
					case TokenType.Quad:
						current.Append(nextToken.code);
						break;
					}
				}
				control.text = GetCurrentText(current, openTokenTypes, tokens);
				charactersTyped = preTyped;
			}
			int safeguard = 0;
			while (tokens.Count > 0 && safeguard < 65535)
			{
				safeguard++;
				if (!paused)
				{
					float num2 = DialogueTime.time - lastTime;
					elapsed += num2;
					float goal = (float)preTyped + elapsed * charactersPerSecond;
					bool flag = false;
					while (((float)charactersTyped < goal || flag) && tokens.Count > 0)
					{
						Token nextToken2 = GetNextToken(tokens);
						switch (nextToken2.tokenType)
						{
						case TokenType.Character:
							if (rightToLeft)
							{
								current.Insert(0, nextToken2.character);
							}
							else
							{
								current.Append(nextToken2.character);
							}
							if (IsSilentCharacter(nextToken2.character))
							{
								if (stopAudioOnSilentCharacters)
								{
									StopCharacterAudio();
								}
							}
							else
							{
								PlayCharacterAudio(nextToken2.character);
							}
							onCharacter.Invoke();
							charactersTyped++;
							if (IsFullPauseCharacter(nextToken2.character))
							{
								if (tokens.Count > 0)
								{
									_ = tokens[0].tokenType != TokenType.Character;
								}
								control.text = frontSkippedText + GetCurrentText(current, openTokenTypes, tokens);
								yield return PauseForDuration(fullPauseDuration);
							}
							else if (IsQuarterPauseCharacter(nextToken2.character))
							{
								if (tokens.Count > 0)
								{
									_ = tokens[0].tokenType != TokenType.Character;
								}
								control.text = frontSkippedText + GetCurrentText(current, openTokenTypes, tokens);
								yield return PauseForDuration(quarterPauseDuration);
							}
							break;
						case TokenType.BoldOpen:
						case TokenType.ItalicOpen:
						case TokenType.ColorOpen:
						case TokenType.SizeOpen:
							OpenRichText(current, nextToken2, openTokenTypes);
							break;
						case TokenType.BoldClose:
						case TokenType.ItalicClose:
						case TokenType.ColorClose:
						case TokenType.SizeClose:
							CloseRichText(current, nextToken2, openTokenTypes);
							break;
						case TokenType.Quad:
							current.Append(nextToken2.code);
							break;
						case TokenType.Pause:
							control.text = GetCurrentText(current, openTokenTypes, tokens);
							yield return PauseForDuration(nextToken2.duration);
							break;
						case TokenType.InstantOpen:
							AddInstantText(current, openTokenTypes, tokens);
							break;
						}
						flag = tokens.Count > 0 && tokens[0].tokenType != TokenType.Character;
					}
				}
				control.text = GetCurrentText(current, openTokenTypes, tokens);
				HandleAutoScroll(jumpToEnd: false);
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

	protected Token GetNextToken(List<Token> tokens)
	{
		if (tokens.Count == 0)
		{
			return null;
		}
		int index = (rightToLeft ? (tokens.Count - 1) : 0);
		Token result = tokens[index];
		tokens.RemoveAt(index);
		return result;
	}

	protected void OpenRichText(StringBuilder current, Token token, List<TokenType> openTokens)
	{
		switch (token.tokenType)
		{
		case TokenType.BoldOpen:
			current.Append("<b>");
			break;
		case TokenType.ItalicOpen:
			current.Append("<i>");
			break;
		case TokenType.ColorOpen:
		case TokenType.SizeOpen:
			current.Append(token.code);
			break;
		}
		openTokens.Insert(0, token.tokenType);
	}

	protected void CloseRichText(StringBuilder current, Token token, List<TokenType> openTokens)
	{
		TokenType tokenType = TokenType.BoldOpen;
		switch (token.tokenType)
		{
		case TokenType.BoldClose:
			current.Append("</b>");
			tokenType = TokenType.BoldOpen;
			break;
		case TokenType.ItalicClose:
			current.Append("</i>");
			tokenType = TokenType.ItalicOpen;
			break;
		case TokenType.ColorClose:
			current.Append("</color>");
			tokenType = TokenType.ColorOpen;
			break;
		case TokenType.SizeClose:
			current.Append("</size>");
			tokenType = TokenType.SizeOpen;
			break;
		}
		int num = -1;
		for (int i = 0; i < openTokens.Count; i++)
		{
			if (openTokens[i] == tokenType)
			{
				num = i;
				break;
			}
		}
		if (num != -1)
		{
			openTokens.RemoveAt(num);
		}
	}

	protected void AddInstantText(StringBuilder current, List<TokenType> openTokenTypes, List<Token> tokens)
	{
		int num = 0;
		while (tokens.Count > 0 && num < MaxSafeguard)
		{
			num++;
			Token nextToken = GetNextToken(tokens);
			switch (nextToken.tokenType)
			{
			case TokenType.Character:
				current.Append(nextToken.character);
				break;
			case TokenType.BoldOpen:
			case TokenType.ItalicOpen:
			case TokenType.ColorOpen:
			case TokenType.SizeOpen:
				OpenRichText(current, nextToken, openTokenTypes);
				break;
			case TokenType.BoldClose:
			case TokenType.ItalicClose:
			case TokenType.ColorClose:
			case TokenType.SizeClose:
				CloseRichText(current, nextToken, openTokenTypes);
				break;
			case TokenType.InstantClose:
				return;
			}
		}
	}

	protected string GetCurrentText(StringBuilder current, List<TokenType> openTokenTypes, List<Token> tokens, bool withoutTransparentText = false)
	{
		if (current == null)
		{
			return string.Empty;
		}
		if (openTokenTypes == null || tokens == null)
		{
			return current.ToString();
		}
		StringBuilder stringBuilder = new StringBuilder(current.ToString());
		for (int i = 0; i < openTokenTypes.Count; i++)
		{
			switch (openTokenTypes[i])
			{
			case TokenType.BoldOpen:
				stringBuilder.Append("</b>");
				break;
			case TokenType.ItalicOpen:
				stringBuilder.Append("</i>");
				break;
			case TokenType.ColorOpen:
				stringBuilder.Append("</color>");
				break;
			case TokenType.SizeOpen:
				stringBuilder.Append("</size>");
				break;
			}
		}
		if (withoutTransparentText)
		{
			return stringBuilder.ToString();
		}
		StringBuilder stringBuilder2 = new StringBuilder();
		if (!autoScrollSettings.autoScrollEnabled || !((UnityEngine.Object)(object)autoScrollSettings.sizerText == null))
		{
			stringBuilder2.Append("<color=#00000000>");
			for (int num = openTokenTypes.Count - 1; num >= 0; num--)
			{
				switch (openTokenTypes[num])
				{
				case TokenType.BoldOpen:
					stringBuilder2.Append("<b>");
					break;
				case TokenType.ItalicOpen:
					stringBuilder2.Append("<i>");
					break;
				}
			}
			for (int j = 0; j < tokens.Count; j++)
			{
				Token token = tokens[j];
				switch (token.tokenType)
				{
				case TokenType.BoldOpen:
					stringBuilder2.Append("<b>");
					break;
				case TokenType.BoldClose:
					stringBuilder2.Append("</b>");
					break;
				case TokenType.ItalicOpen:
					stringBuilder2.Append("<i>");
					break;
				case TokenType.ItalicClose:
					stringBuilder2.Append("</i>");
					break;
				case TokenType.Character:
					stringBuilder2.Append(token.character);
					break;
				}
			}
			stringBuilder2.Append("</color>");
			if (rightToLeft)
			{
				stringBuilder.Insert(0, stringBuilder2);
			}
			else
			{
				stringBuilder.Append(stringBuilder2);
			}
		}
		return stringBuilder.ToString();
	}

	protected List<Token> Tokenize(string text)
	{
		List<Token> list = new List<Token>();
		string remainder = text;
		int num = 0;
		while (!string.IsNullOrEmpty(remainder) && num < MaxSafeguard)
		{
			num++;
			Token token = null;
			if (remainder[0].Equals('<'))
			{
				token = TryTokenize("<b>", TokenType.BoldOpen, 0f, ref remainder);
				if (token == null)
				{
					token = TryTokenize("</b>", TokenType.BoldClose, 0f, ref remainder);
				}
				if (token == null)
				{
					token = TryTokenize("<i>", TokenType.ItalicOpen, 0f, ref remainder);
				}
				if (token == null)
				{
					token = TryTokenize("</i>", TokenType.ItalicClose, 0f, ref remainder);
				}
				if (token == null)
				{
					token = TryTokenize("</color>", TokenType.ColorClose, 0f, ref remainder);
				}
				if (token == null)
				{
					token = TryTokenize("</size>", TokenType.SizeClose, 0f, ref remainder);
				}
				if (token == null)
				{
					token = TryTokenizeColorOpen(ref remainder);
				}
				if (token == null)
				{
					token = TryTokenizeSizeOpen(ref remainder);
				}
				if (token == null)
				{
					token = TryTokenizeQuad(ref remainder);
				}
			}
			else if (remainder[0].Equals('\\'))
			{
				token = TryTokenize("\\.", TokenType.Pause, fullPauseDuration, ref remainder);
				if (token == null)
				{
					token = TryTokenize("\\,", TokenType.Pause, quarterPauseDuration, ref remainder);
				}
				if (token == null)
				{
					token = TryTokenize("\\>", TokenType.InstantOpen, 0f, ref remainder);
				}
				if (token == null)
				{
					token = TryTokenize("\\<", TokenType.InstantClose, 0f, ref remainder);
				}
				if (token == null)
				{
					token = TryTokenize("\\^", TokenType.InstantOpen, 0f, ref remainder);
				}
			}
			if (token == null)
			{
				token = new Token(TokenType.Character, remainder[0], string.Empty, 0f);
				remainder = remainder.Remove(0, 1);
			}
			list.Add(token);
		}
		return list;
	}

	protected Token TryTokenize(string code, TokenType tokenType, float duration, ref string remainder)
	{
		if (remainder.StartsWith(code, StringComparison.OrdinalIgnoreCase))
		{
			remainder = remainder.Remove(0, code.Length);
			return new Token(tokenType, ' ', string.Empty, duration);
		}
		return null;
	}

	protected Token TryTokenizeColorOpen(ref string remainder)
	{
		if (remainder.StartsWith("<color="))
		{
			string text = remainder.Substring(0, remainder.IndexOf('>') + 1);
			remainder = remainder.Remove(0, text.Length);
			return new Token(TokenType.ColorOpen, ' ', text, 0f);
		}
		return null;
	}

	protected Token TryTokenizeSizeOpen(ref string remainder)
	{
		if (remainder.StartsWith("<size="))
		{
			string text = remainder.Substring(0, remainder.IndexOf('>') + 1);
			remainder = remainder.Remove(0, text.Length);
			return new Token(TokenType.SizeOpen, ' ', text, 0f);
		}
		return null;
	}

	protected Token TryTokenizeQuad(ref string remainder)
	{
		if (remainder.StartsWith("<quad "))
		{
			string text = remainder.Substring(0, remainder.IndexOf('>') + 1);
			remainder = remainder.Remove(0, text.Length);
			return new Token(TokenType.Quad, ' ', text, 0f);
		}
		return null;
	}

	protected virtual void StopTypewriterCoroutine()
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
		if ((UnityEngine.Object)(object)control != null && original != null)
		{
			control.text = UITools.StripRPGMakerCodes(frontSkippedText + original);
		}
		original = null;
		if (!autoScrollSettings.autoScrollEnabled)
		{
			return;
		}
		if (current != null && (UnityEngine.Object)(object)autoScrollSettings.sizerText != null)
		{
			current = new StringBuilder(control.text);
			if (base.enabled && base.gameObject.activeInHierarchy)
			{
				StartCoroutine(HandleAutoScrollAfterOneFrame(jumpToEnd: true));
			}
		}
		HandleAutoScroll(jumpToEnd: true);
	}

	protected void InitAutoScroll()
	{
		if (autoScrollSettings.autoScrollEnabled && (UnityEngine.Object)(object)autoScrollSettings.sizerText != null)
		{
			((Graphic)autoScrollSettings.sizerText).color = new Color(0f, 0f, 0f, 0f);
		}
	}

	protected void HandleAutoScroll(bool jumpToEnd)
	{
		if (!autoScrollSettings.autoScrollEnabled)
		{
			return;
		}
		if ((UnityEngine.Object)(object)autoScrollSettings.sizerText != null)
		{
			autoScrollSettings.sizerText.text = GetCurrentText(current, openTokenTypes, tokens, withoutTransparentText: true);
		}
		if ((UnityEngine.Object)(object)autoScrollSettings.scrollRect != null)
		{
			if (!jumpToEnd && autoScrollSettings.scrollbarEnabler != null && autoScrollSettings.scrollbarEnabler.smoothScroll)
			{
				if (autoScrollSettings.scrollRect.verticalNormalizedPosition > 0f)
				{
					autoScrollSettings.scrollRect.verticalNormalizedPosition = Mathf.Max(0f, autoScrollSettings.scrollRect.verticalNormalizedPosition - autoScrollSettings.scrollbarEnabler.smoothScrollSpeed * DialogueTime.deltaTime);
				}
			}
			else
			{
				autoScrollSettings.scrollRect.normalizedPosition = Vector2.zero;
				autoScrollSettings.scrollbarEnabler.CheckScrollbar();
			}
		}
		else if (autoScrollSettings.scrollbarEnabler != null)
		{
			autoScrollSettings.scrollbarEnabler.CheckScrollbar();
		}
	}

	protected IEnumerator HandleAutoScrollAfterOneFrame(bool jumpToEnd)
	{
		yield return null;
		HandleAutoScroll(jumpToEnd);
	}
}
