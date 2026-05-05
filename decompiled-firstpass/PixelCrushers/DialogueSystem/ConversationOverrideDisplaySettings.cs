using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class ConversationOverrideDisplaySettings
{
	public bool useOverrides;

	public bool overrideSubtitleSettings;

	public bool showNPCSubtitlesDuringLine = true;

	public bool showNPCSubtitlesWithResponses = true;

	public bool showPCSubtitlesDuringLine;

	public bool skipPCSubtitleAfterResponseMenu;

	public float subtitleCharsPerSecond = 30f;

	public float minSubtitleSeconds = 2f;

	public DisplaySettings.SubtitleSettings.ContinueButtonMode continueButton;

	public bool overrideSequenceSettings;

	[TextArea]
	public string defaultSequence;

	[TextArea]
	public string defaultPlayerSequence;

	[TextArea]
	public string defaultResponseMenuSequence;

	public bool overrideInputSettings;

	public bool alwaysForceResponseMenu = true;

	public bool includeInvalidEntries;

	public float responseTimeout;

	public EmTag emTagForOldResponses;

	public EmTag emTagForInvalidResponses;

	public InputTrigger cancelSubtitle = new InputTrigger(KeyCode.Escape);

	public InputTrigger cancelConversation = new InputTrigger(KeyCode.Escape);
}
