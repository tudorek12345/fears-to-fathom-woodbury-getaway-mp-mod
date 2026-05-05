using System;
using System.Globalization;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public class ConversationView : MonoBehaviour
{
	private delegate bool IsCancelKeyDownDelegate();

	public static GetDefaultSubtitleDurationDelegate overrideGetDefaultSubtitleDuration;

	private IDialogueUI ui;

	private Sequencer m_sequencer;

	private DisplaySettings settings;

	private Subtitle lastNPCSubtitle;

	private Subtitle lastPCSubtitle;

	private Subtitle lastSubtitle;

	private IsCancelKeyDownDelegate IsCancelKeyDown;

	private Action CancelledHandler;

	private DialogueEntrySpokenDelegate dialogueEntrySpokenHandler;

	private bool waitForContinue;

	private bool notifyOnFinishSubtitle;

	private bool isPlayingResponseMenuSequence;

	private int initialFrameCount;

	private Subtitle _subtitle;

	private bool _isPCResponseMenuNext;

	private bool _isPCAutoResponseNext;

	private bool _lastModeWasResponseMenu;

	public DisplaySettings displaySettings => settings;

	public bool isWaitingForContinue => waitForContinue;

	public Sequencer sequencer => m_sequencer;

	public IDialogueUI dialogueUI
	{
		get
		{
			return ui;
		}
		set
		{
			if (ui != value)
			{
				ui.SelectedResponseHandler -= OnSelectedResponse;
				ui.Close();
				ui = value;
				ui.Open();
				ui.SelectedResponseHandler += OnSelectedResponse;
			}
		}
	}

	public event EventHandler FinishedSubtitleHandler;

	public event EventHandler<SelectedResponseEventArgs> SelectedResponseHandler;

	public void Initialize(IDialogueUI ui, Sequencer sequencer, DisplaySettings displaySettings, DialogueEntrySpokenDelegate dialogueEntrySpokenHandler)
	{
		this.ui = ui;
		m_sequencer = sequencer;
		settings = (DialogueManager.allowSimultaneousConversations ? new DisplaySettings(displaySettings) : displaySettings);
		this.dialogueEntrySpokenHandler = dialogueEntrySpokenHandler;
		initialFrameCount = Time.frameCount;
		ui.Open();
		sequencer.Open();
		ui.SelectedResponseHandler += OnSelectedResponse;
		sequencer.FinishedSequenceHandler += OnFinishedSubtitle;
	}

	public void Close()
	{
		ui.SelectedResponseHandler -= OnSelectedResponse;
		m_sequencer.FinishedSequenceHandler -= OnFinishedSubtitle;
		ui.Close();
		m_sequencer.Close();
		UnityEngine.Object.Destroy(this);
	}

	public void Update()
	{
		if (Cancelled() && CancelledHandler != null)
		{
			CancelledHandler();
		}
	}

	private bool Cancelled()
	{
		if (IsCancelKeyDown != null)
		{
			return IsCancelKeyDown();
		}
		return false;
	}

	private bool IsSubtitleCancelKeyDown()
	{
		return settings.GetCancelSubtitleInput().isDown;
	}

	private bool IsConversationCancelKeyDown()
	{
		return settings.GetCancelConversationInput().isDown;
	}

	public void StartSubtitle(Subtitle subtitle, bool isPCResponseMenuNext, bool isPCAutoResponseNext)
	{
		notifyOnFinishSubtitle = true;
		if (subtitle != null)
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: {1} says '{2}'", new object[3]
				{
					"Dialogue System",
					Tools.GetGameObjectName(subtitle.speakerInfo.transform),
					subtitle.formattedText.text
				}));
			}
			if (DialogueManager.instance.allowSimultaneousConversations)
			{
				DialogueManager.instance.displaySettings = settings;
			}
			NotifyParticipantsOnConversationLine(subtitle);
			m_sequencer.SetParticipants(subtitle.speakerInfo.transform, subtitle.listenerInfo.transform);
			m_sequencer.entrytag = subtitle.entrytag;
			m_sequencer.subtitleEndTime = GetDefaultSubtitleDuration(subtitle.formattedText.text);
			if (!string.IsNullOrEmpty(subtitle.sequence) && subtitle.sequence.Contains("{{default}}"))
			{
				subtitle.sequence = subtitle.sequence.Replace("{{default}}", GetDefaultSequence(subtitle));
			}
			subtitle.sequence = (string.IsNullOrEmpty(subtitle.sequence) ? GetDefaultSequence(subtitle) : PreprocessSequence(subtitle));
			if (ShouldShowSubtitle(subtitle))
			{
				ui.ShowSubtitle(subtitle);
				_subtitle = subtitle;
				_isPCResponseMenuNext = isPCResponseMenuNext;
				_isPCAutoResponseNext = isPCAutoResponseNext;
				SetupContinueButton(subtitle, isPCResponseMenuNext, isPCAutoResponseNext);
			}
			else
			{
				waitForContinue = false;
			}
			if (subtitle.speakerInfo.isNPC)
			{
				lastNPCSubtitle = subtitle;
			}
			else
			{
				lastPCSubtitle = subtitle;
			}
			lastSubtitle = subtitle;
			if (dialogueEntrySpokenHandler != null)
			{
				dialogueEntrySpokenHandler(subtitle);
			}
			m_sequencer.PlaySequence(subtitle.sequence, settings.subtitleSettings.informSequenceStartAndEnd, destroyWhenDone: false);
		}
		else
		{
			FinishSubtitle();
		}
		IsCancelKeyDown = IsSubtitleCancelKeyDown;
		CancelledHandler = OnCancelSubtitle;
		if (!string.IsNullOrEmpty(subtitle.formattedText.text))
		{
			_lastModeWasResponseMenu = false;
		}
	}

	public void SetupContinueButton()
	{
		SetupContinueButton(_subtitle, _isPCResponseMenuNext, _isPCAutoResponseNext);
	}

	private void SetupContinueButton(Subtitle subtitle, bool isPCResponseMenuNext, bool isPCAutoResponseNext)
	{
		if (subtitle == null)
		{
			return;
		}
		bool isPCLine = subtitle.speakerInfo.characterType == CharacterType.PC;
		waitForContinue = ShouldWaitForContinueButton(isPCLine, isPCResponseMenuNext, isPCAutoResponseNext);
		bool flag = ShouldShowContinueButton(isPCLine, isPCResponseMenuNext, isPCAutoResponseNext);
		if (waitForContinue && string.IsNullOrEmpty(subtitle.formattedText.text) && subtitle.dialogueEntry.id == 0)
		{
			waitForContinue = false;
		}
		AbstractDialogueUI abstractDialogueUI = ui as AbstractDialogueUI;
		if (abstractDialogueUI != null)
		{
			if (flag)
			{
				abstractDialogueUI.ShowContinueButton(subtitle);
			}
			else
			{
				abstractDialogueUI.HideContinueButton(subtitle);
			}
		}
	}

	private bool ShouldWaitForContinueButton(bool isPCLine, bool isPCResponseMenuNext, bool isPCAutoResponseNext)
	{
		return settings.GetContinueButtonMode() switch
		{
			DisplaySettings.SubtitleSettings.ContinueButtonMode.Always => true, 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.Never => false, 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.Optional => false, 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.OptionalBeforeResponseMenu => !isPCResponseMenuNext, 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.NotBeforeResponseMenu => !isPCResponseMenuNext, 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.OptionalBeforePCAutoresponseOrMenu => !(isPCResponseMenuNext || isPCAutoResponseNext), 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.NotBeforePCAutoresponseOrMenu => !(isPCResponseMenuNext || isPCAutoResponseNext), 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.OptionalForPC => !isPCLine, 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.NotForPC => !isPCLine, 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.OptionalForPCOrBeforeResponseMenu => !(isPCLine || isPCResponseMenuNext), 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.NotForPCOrBeforeResponseMenu => !(isPCLine || isPCResponseMenuNext), 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.OptionalForPCOrBeforePCAutoresponseOrMenu => !(isPCLine || isPCResponseMenuNext || isPCAutoResponseNext), 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.NotForPCOrBeforePCAutoresponseOrMenu => !(isPCLine || isPCResponseMenuNext || isPCAutoResponseNext), 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.OnlyForPC => isPCLine, 
			_ => false, 
		};
	}

	private bool ShouldShowContinueButton(bool isPCLine, bool isPCResponseMenuNext, bool isPCAutoResponseNext)
	{
		if (settings.subtitleSettings.requireContinueOnLastLine && !DialogueManager.instance.currentConversationState.hasAnyResponses)
		{
			waitForContinue = true;
			return true;
		}
		return settings.GetContinueButtonMode() switch
		{
			DisplaySettings.SubtitleSettings.ContinueButtonMode.Always => true, 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.Never => false, 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.Optional => true, 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.OptionalBeforeResponseMenu => true, 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.NotBeforeResponseMenu => !isPCResponseMenuNext, 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.OptionalBeforePCAutoresponseOrMenu => true, 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.NotBeforePCAutoresponseOrMenu => !(isPCResponseMenuNext || isPCAutoResponseNext), 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.OptionalForPC => true, 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.NotForPC => !isPCLine, 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.OptionalForPCOrBeforeResponseMenu => true, 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.NotForPCOrBeforeResponseMenu => !(isPCLine || isPCResponseMenuNext), 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.OptionalForPCOrBeforePCAutoresponseOrMenu => true, 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.NotForPCOrBeforePCAutoresponseOrMenu => !(isPCLine || isPCResponseMenuNext || isPCAutoResponseNext), 
			DisplaySettings.SubtitleSettings.ContinueButtonMode.OnlyForPC => isPCLine, 
			_ => false, 
		};
	}

	public void ShowLastNPCSubtitle()
	{
		if (ShouldShowLastNPCSubtitle())
		{
			ui.ShowSubtitle(lastNPCSubtitle);
		}
		FinishSubtitle();
	}

	private bool ShouldShowLastNPCSubtitle()
	{
		if (settings != null && settings.GetShowNPCSubtitlesWithResponses() && lastNPCSubtitle != null)
		{
			return lastNPCSubtitle.speakerInfo.characterType == CharacterType.NPC;
		}
		return false;
	}

	private bool ShouldShowLastPCSubtitle()
	{
		if (settings != null && settings.GetShowNPCSubtitlesWithResponses() && settings.subtitleSettings.allowPCSubtitleReminders && lastPCSubtitle != null && lastSubtitle == lastPCSubtitle)
		{
			return lastPCSubtitle.speakerInfo.characterType == CharacterType.PC;
		}
		return false;
	}

	private bool ShouldShowSubtitle(Subtitle subtitle)
	{
		if (subtitle != null && settings != null && settings.subtitleSettings != null)
		{
			if (subtitle.formattedText.noSubtitle || string.Equals(subtitle.sequence, "None()") || string.Equals(subtitle.sequence, "None();") || string.Equals(subtitle.sequence, "Continue()") || string.Equals(subtitle.sequence, "Continue();"))
			{
				return false;
			}
			if (subtitle.speakerInfo.characterType == CharacterType.NPC && settings.GetShowNPCSubtitlesDuringLine())
			{
				return true;
			}
			if (subtitle.speakerInfo.characterType == CharacterType.PC && settings.GetShowPCSubtitlesDuringLine())
			{
				if (_lastModeWasResponseMenu)
				{
					return !settings.GetSkipPCSubtitleAfterResponseMenu();
				}
				return true;
			}
		}
		return false;
	}

	public void OnConversationContinue(IDialogueUI dialogueUI)
	{
		if (dialogueUI == ui)
		{
			HandleContinueButtonClick();
		}
	}

	public void OnConversationContinueAll()
	{
		HandleContinueButtonClick();
	}

	public void HandleContinueButtonClick()
	{
		if (Time.frameCount != initialFrameCount || initialFrameCount != ConversationController.frameLastConversationEnded)
		{
			waitForContinue = false;
			FinishSubtitle();
		}
	}

	private void OnCancelSubtitle()
	{
		if (lastSubtitle == null)
		{
			Subtitle parameter = new Subtitle(null, null, null, string.Empty, string.Empty, null);
			BroadcastMessage("OnConversationLineCancelled", parameter, SendMessageOptions.DontRequireReceiver);
		}
		else
		{
			BroadcastMessage("OnConversationLineCancelled", lastSubtitle, SendMessageOptions.DontRequireReceiver);
		}
		waitForContinue = false;
		FinishSubtitle();
	}

	private void FinishSubtitle()
	{
		if (waitForContinue)
		{
			return;
		}
		if (m_sequencer != null)
		{
			m_sequencer.Stop();
		}
		ui.HideSubtitle(lastSubtitle);
		if (notifyOnFinishSubtitle)
		{
			notifyOnFinishSubtitle = false;
			if (_subtitle != null)
			{
				NotifyParticipantsOnConversationLineEnd(lastSubtitle);
			}
			if (this.FinishedSubtitleHandler != null)
			{
				this.FinishedSubtitleHandler(this, EventArgs.Empty);
			}
		}
	}

	private void OnFinishedSubtitle()
	{
		FinishSubtitle();
	}

	public void StartResponses(Subtitle subtitle, Response[] responses)
	{
		PlayResponseMenuSequence(subtitle.responseMenuSequence);
		Subtitle subtitle2 = (ShouldShowLastPCSubtitle() ? lastPCSubtitle : (ShouldShowLastNPCSubtitle() ? lastNPCSubtitle : null));
		NotifyOnResponseMenu(responses);
		ui.ShowResponses(subtitle2, responses, settings.GetResponseTimeout());
		IsCancelKeyDown = IsConversationCancelKeyDown;
		CancelledHandler = OnCancelResponseMenu;
		_lastModeWasResponseMenu = true;
	}

	private void PlayResponseMenuSequence(string responseMenuSequence)
	{
		if (string.IsNullOrEmpty(responseMenuSequence) && !string.IsNullOrEmpty(settings.GetDefaultResponseMenuSequence()))
		{
			responseMenuSequence = settings.GetDefaultResponseMenuSequence();
		}
		if (!string.IsNullOrEmpty(responseMenuSequence))
		{
			m_sequencer.FinishedSequenceHandler -= OnFinishedSubtitle;
			m_sequencer.Stop();
			m_sequencer.PlaySequence(responseMenuSequence);
			isPlayingResponseMenuSequence = true;
		}
	}

	private void StopResponseMenuSequence()
	{
		if (isPlayingResponseMenuSequence)
		{
			isPlayingResponseMenuSequence = false;
			m_sequencer.Stop();
			m_sequencer.StopAllCoroutines();
			m_sequencer.FinishedSequenceHandler += OnFinishedSubtitle;
		}
	}

	private void OnCancelResponseMenu()
	{
		NotifyParticipantsOnConversationCancelled();
		SelectResponse(new SelectedResponseEventArgs(null));
	}

	private void OnSelectedResponse(object sender, SelectedResponseEventArgs e)
	{
		SelectResponse(e);
	}

	public void SelectResponse(SelectedResponseEventArgs e)
	{
		StopResponseMenuSequence();
		ui.HideResponses();
		if (this.SelectedResponseHandler != null)
		{
			this.SelectedResponseHandler(this, e);
		}
	}

	public string GetDefaultSequence(Subtitle subtitle)
	{
		float subtitleEndTime = m_sequencer.subtitleEndTime;
		bool flag = subtitle.speakerInfo.characterType == CharacterType.PC;
		if (flag && (!settings.GetShowPCSubtitlesDuringLine() || (_lastModeWasResponseMenu && settings.GetSkipPCSubtitleAfterResponseMenu())))
		{
			return Sequencer.ReplaceShortcuts(settings.GetDefaultPlayerSequence()).Replace("{{end}}", subtitleEndTime.ToString(CultureInfo.InvariantCulture));
		}
		string text = settings.GetDefaultSequence();
		if (flag && !string.IsNullOrEmpty(settings.GetDefaultPlayerSequence()))
		{
			text = settings.GetDefaultPlayerSequence();
		}
		if (string.IsNullOrEmpty(text))
		{
			return string.Format(CultureInfo.InvariantCulture, "Delay({0})", new object[1] { subtitleEndTime });
		}
		return Sequencer.ReplaceShortcuts(text).Replace("{{end}}", subtitleEndTime.ToString(CultureInfo.InvariantCulture));
	}

	public float GetDefaultSubtitleDuration(string text)
	{
		return GetDefaultSubtitleDurationInSeconds(text, settings);
	}

	public static float GetDefaultSubtitleDurationInSeconds(string text, DisplaySettings displaySettings = null)
	{
		if (overrideGetDefaultSubtitleDuration != null)
		{
			return overrideGetDefaultSubtitleDuration(text);
		}
		DisplaySettings displaySettings2 = displaySettings ?? DialogueManager.displaySettings;
		int num = ((!string.IsNullOrEmpty(text)) ? Tools.StripRichTextCodes(text).Length : 0);
		float num2 = 0f;
		if (text.Contains("\\"))
		{
			int num3 = (text.Length - text.Replace("\\.", string.Empty).Length) / 2;
			int num4 = (text.Length - text.Replace("\\,", string.Empty).Length) / 2;
			num2 = 1f * (float)num3 + 0.25f * (float)num4;
		}
		return Mathf.Max(displaySettings2.GetMinSubtitleSeconds(), num2 + (float)num / Mathf.Max(1f, displaySettings2.GetSubtitleCharsPerSecond()));
	}

	private string PreprocessSequence(Subtitle subtitle)
	{
		if (subtitle == null || string.IsNullOrEmpty(subtitle.sequence))
		{
			return string.Empty;
		}
		subtitle.sequence = Sequencer.ReplaceShortcuts(subtitle.sequence);
		if (!subtitle.sequence.Contains("{{end}}"))
		{
			return subtitle.sequence;
		}
		float subtitleEndTime = m_sequencer.subtitleEndTime;
		return subtitle.sequence.Replace("{{end}}", subtitleEndTime.ToString(CultureInfo.InvariantCulture));
	}

	private void NotifyParticipantsOnConversationLine(Subtitle subtitle)
	{
		NotifyParticipants("OnConversationLine", subtitle);
	}

	private void NotifyParticipantsOnConversationLineEnd(Subtitle subtitle)
	{
		NotifyParticipants("OnConversationLineEnd", subtitle);
	}

	private void NotifyParticipants(string message, Subtitle subtitle)
	{
		if (subtitle != null)
		{
			bool num = CharacterInfoHasValidTransform(subtitle.speakerInfo);
			bool flag = CharacterInfoHasValidTransform(subtitle.listenerInfo);
			bool flag2 = num && flag && subtitle.speakerInfo.transform == subtitle.listenerInfo.transform;
			if (num)
			{
				subtitle.speakerInfo.transform.BroadcastMessage(message, subtitle, SendMessageOptions.DontRequireReceiver);
			}
			if (flag && !flag2)
			{
				subtitle.listenerInfo.transform.BroadcastMessage(message, subtitle, SendMessageOptions.DontRequireReceiver);
			}
			DialogueManager.instance.BroadcastMessage(message, subtitle, SendMessageOptions.DontRequireReceiver);
		}
	}

	private void NotifyOnResponseMenu(Response[] responses)
	{
		if (responses == null)
		{
			return;
		}
		if (lastSubtitle != null)
		{
			bool num = CharacterInfoHasValidTransform(lastSubtitle.speakerInfo);
			bool flag = CharacterInfoHasValidTransform(lastSubtitle.listenerInfo);
			bool flag2 = num && flag && lastSubtitle.speakerInfo.transform == lastSubtitle.listenerInfo.transform;
			if (num)
			{
				lastSubtitle.speakerInfo.transform.BroadcastMessage("OnConversationResponseMenu", responses, SendMessageOptions.DontRequireReceiver);
			}
			if (flag && !flag2)
			{
				lastSubtitle.listenerInfo.transform.BroadcastMessage("OnConversationResponseMenu", responses, SendMessageOptions.DontRequireReceiver);
			}
		}
		DialogueManager.instance.BroadcastMessage("OnConversationResponseMenu", responses, SendMessageOptions.DontRequireReceiver);
	}

	private void NotifyParticipantsOnConversationCancelled()
	{
		if (lastSubtitle != null)
		{
			bool num = CharacterInfoHasValidTransform(lastSubtitle.speakerInfo);
			bool flag = CharacterInfoHasValidTransform(lastSubtitle.listenerInfo);
			bool flag2 = num && flag && lastSubtitle.speakerInfo.transform == lastSubtitle.listenerInfo.transform;
			if (num)
			{
				lastSubtitle.speakerInfo.transform.BroadcastMessage("OnConversationCancelled", m_sequencer.listener ?? base.transform, SendMessageOptions.DontRequireReceiver);
			}
			if (flag && !flag2)
			{
				lastSubtitle.listenerInfo.transform.BroadcastMessage("OnConversationCancelled", m_sequencer.speaker ?? base.transform, SendMessageOptions.DontRequireReceiver);
			}
		}
		DialogueManager.instance.BroadcastMessage("OnConversationCancelled", m_sequencer.speaker ?? base.transform, SendMessageOptions.DontRequireReceiver);
	}

	private bool CharacterInfoHasValidTransform(CharacterInfo characterInfo)
	{
		if (characterInfo != null)
		{
			return characterInfo.transform != null;
		}
		return false;
	}

	public void SetPCPortrait(Sprite pcSprite, string pcName)
	{
		AbstractDialogueUI abstractDialogueUI = DialogueManager.dialogueUI as AbstractDialogueUI;
		if (!(abstractDialogueUI == null))
		{
			abstractDialogueUI.SetPCPortrait(pcSprite, pcName);
		}
	}

	public void SetActorPortraitSprite(string actorName, Sprite sprite)
	{
		AbstractDialogueUI abstractDialogueUI = DialogueManager.dialogueUI as AbstractDialogueUI;
		if (!(abstractDialogueUI == null))
		{
			abstractDialogueUI.SetActorPortraitSprite(actorName, sprite);
		}
	}
}
