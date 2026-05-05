using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class DisplaySettings
{
	[Serializable]
	public class LocalizationSettings
	{
		[Tooltip("Current language, or blank to use the default language.")]
		public string language = string.Empty;

		[Tooltip("Use the system language at startup.")]
		public bool useSystemLanguage;

		[Tooltip("Optional localized text for alerts and other general text. Note: Now uses Text Table instead of Localized Text Table.")]
		public TextTable textTable;

		public LocalizationSettings()
		{
		}

		public LocalizationSettings(LocalizationSettings source)
		{
			language = source.language;
			useSystemLanguage = source.useSystemLanguage;
			textTable = source.textTable;
		}
	}

	[Serializable]
	public class SubtitleSettings
	{
		public enum ContinueButtonMode
		{
			Never,
			Always,
			Optional,
			OptionalBeforeResponseMenu,
			NotBeforeResponseMenu,
			OptionalBeforePCAutoresponseOrMenu,
			NotBeforePCAutoresponseOrMenu,
			OptionalForPC,
			NotForPC,
			OptionalForPCOrBeforeResponseMenu,
			NotForPCOrBeforeResponseMenu,
			OptionalForPCOrBeforePCAutoresponseOrMenu,
			NotForPCOrBeforePCAutoresponseOrMenu,
			OnlyForPC
		}

		[Tooltip("Show NPC subtitle text while NPC speaks a line of dialogue.")]
		public bool showNPCSubtitlesDuringLine = true;

		[Tooltip("Show NPC subtitle reminder text while showing the player response menu. If you're using Standard Dialogue UI, the subtitle panel's Visiblity value takes precedenece over this.")]
		public bool showNPCSubtitlesWithResponses = true;

		[Tooltip("Show PC subtitle text while PC speaks a line of dialogue. If Skip PC Subtitle After Response Menu (below) is ticked, PC subtitles from response menu selections will be skipped.")]
		public bool showPCSubtitlesDuringLine;

		[Tooltip("Allow PC subtitles to be used for reminder text while showing the response menu.")]
		public bool allowPCSubtitleReminders;

		[Tooltip("If the PC's subtitle came from a response menu selection, don't show the subtitle even if Show PC Subtitles During Line is ticked.")]
		public bool skipPCSubtitleAfterResponseMenu;

		[Tooltip("Used to compute default duration to display subtitle. Typewriter effects have their own separate setting.")]
		public float subtitleCharsPerSecond = 30f;

		[Tooltip("Minimum default duration to display subtitle.")]
		public float minSubtitleSeconds = 2f;

		[Tooltip("How to handle continue buttons.")]
		public ContinueButtonMode continueButton;

		[Tooltip("If ticked, always require continue button on subtitle that ends conversation. Overrides Continue Button dropdown above.")]
		public bool requireContinueOnLastLine;

		[Tooltip("Use rich text codes for [em#] markup tags. If unticked, [em#] tag will apply color to entire text.")]
		public bool richTextEmphases = true;

		[Tooltip("Send OnSequenceStart and OnSequenceEnd messages with every dialogue entry's sequence.")]
		public bool informSequenceStartAndEnd;

		public SubtitleSettings()
		{
		}

		public SubtitleSettings(SubtitleSettings source)
		{
			showNPCSubtitlesDuringLine = source.showNPCSubtitlesDuringLine;
			showNPCSubtitlesWithResponses = source.showNPCSubtitlesWithResponses;
			showPCSubtitlesDuringLine = source.showPCSubtitlesDuringLine;
			allowPCSubtitleReminders = source.allowPCSubtitleReminders;
			skipPCSubtitleAfterResponseMenu = source.skipPCSubtitleAfterResponseMenu;
			subtitleCharsPerSecond = source.subtitleCharsPerSecond;
			minSubtitleSeconds = source.minSubtitleSeconds;
			continueButton = source.continueButton;
			requireContinueOnLastLine = source.requireContinueOnLastLine;
			richTextEmphases = source.richTextEmphases;
			informSequenceStartAndEnd = source.informSequenceStartAndEnd;
		}
	}

	[Serializable]
	public class CameraSettings
	{
		[Tooltip("Camera or prefab to use for sequences. If unassigned, sequences use the current main camera.")]
		public Camera sequencerCamera;

		[Tooltip("If assigned, use instead of Sequencer Camera -- for example, Oculus VR GameObject. Can't be a prefab.")]
		public GameObject alternateCameraObject;

		[Tooltip("Camera angle object or prefab. If unassigned, use default camera angle definitions.")]
		public GameObject cameraAngles;

		[Tooltip("If conversation's sequences use Main Camera, leave camera in current position at end of conversation instead of restoring pre-conversation position.")]
		public bool keepCameraPositionAtConversationEnd;

		[Tooltip("Used when a dialogue entry doesn't define its own Sequence. Set to Delay({{end}}) to leave the camera untouched.")]
		[TextArea]
		public string defaultSequence = "Delay({{end}})";

		[Tooltip("If defined, overrides Default Sequence for player (PC) lines only.")]
		[TextArea]
		public string defaultPlayerSequence = string.Empty;

		[Tooltip("Used when a dialogue entry doesn't define its own Response Menu Sequence.")]
		[TextArea]
		public string defaultResponseMenuSequence = string.Empty;

		[Tooltip("Format to use for the 'entrytag' keyword.")]
		public EntrytagFormat entrytagFormat;

		[Tooltip("By default, Audio() and AudioWait() sequencer commands don't report missing audio files to reduce Console spam during development.")]
		public bool reportMissingAudioFiles;

		[HideInInspector]
		public bool disableInternalSequencerCommands;

		public CameraSettings()
		{
		}

		public CameraSettings(CameraSettings source)
		{
			sequencerCamera = source.sequencerCamera;
			alternateCameraObject = source.alternateCameraObject;
			cameraAngles = source.cameraAngles;
			keepCameraPositionAtConversationEnd = source.keepCameraPositionAtConversationEnd;
			defaultSequence = source.defaultSequence;
			defaultPlayerSequence = source.defaultPlayerSequence;
			defaultResponseMenuSequence = source.defaultResponseMenuSequence;
			entrytagFormat = source.entrytagFormat;
			reportMissingAudioFiles = source.reportMissingAudioFiles;
			disableInternalSequencerCommands = source.disableInternalSequencerCommands;
		}
	}

	[Serializable]
	public class InputSettings
	{
		[Tooltip("Show the response menu even if there's only one response.")]
		public bool alwaysForceResponseMenu = true;

		[Tooltip("Include responses whose Conditions are false. typically shown in a disabled state.")]
		public bool includeInvalidEntries;

		[Tooltip("If nonzero, the duration in seconds until the response menu times out.")]
		public float responseTimeout;

		[Tooltip("What to do if the response menu times out.")]
		public ResponseTimeoutAction responseTimeoutAction;

		[Tooltip("The [em#] tag to wrap around responses that have been previously chosen.")]
		public EmTag emTagForOldResponses;

		[Tooltip("The [em#] tag to wrap around invalid responses. These responses are only shown if Include Invalid Entries is ticked.")]
		public EmTag emTagForInvalidResponses;

		[Tooltip("Input buttons mapped to QTEs.")]
		public string[] qteButtons = new string[2] { "Fire1", "Fire2" };

		[Tooltip("Key or button that cancels subtitle sequences.")]
		public InputTrigger cancel = new InputTrigger(KeyCode.Escape);

		[Tooltip("Key or button that cancels active conversation while in response menu.")]
		public InputTrigger cancelConversation = new InputTrigger(KeyCode.Escape);

		public InputSettings()
		{
		}

		public InputSettings(InputSettings source)
		{
			alwaysForceResponseMenu = source.alwaysForceResponseMenu;
			includeInvalidEntries = source.includeInvalidEntries;
			responseTimeout = source.responseTimeout;
			responseTimeoutAction = source.responseTimeoutAction;
			emTagForOldResponses = source.emTagForOldResponses;
			emTagForInvalidResponses = source.emTagForInvalidResponses;
			qteButtons = source.qteButtons;
			cancel = source.cancel;
			cancelConversation = source.cancelConversation;
		}
	}

	[Serializable]
	public class BarkSettings
	{
		[Tooltip("Allow barks to play during conversations.")]
		public bool allowBarksDuringConversations = true;

		[Tooltip("Show barks for this many characters per second. If zero, use Subtitle Settings > Subtitle Chars Per Second.")]
		public float barkCharsPerSecond;

		[Tooltip("Show barks  for at least this many seconds. If zero, use Subtitle Settings > Min Subtitle Seconds.")]
		public float minBarkSeconds;

		[Tooltip("If non-blank, play this sequence with barks that don't specify their own Sequence.")]
		public string defaultBarkSequence = string.Empty;

		public BarkSettings()
		{
		}

		public BarkSettings(BarkSettings source)
		{
			allowBarksDuringConversations = source.allowBarksDuringConversations;
			barkCharsPerSecond = source.barkCharsPerSecond;
			minBarkSeconds = source.minBarkSeconds;
			defaultBarkSequence = source.defaultBarkSequence;
		}
	}

	[Serializable]
	public class AlertSettings
	{
		[Tooltip("Allow the dialogue UI to show alerts during conversations.")]
		public bool allowAlertsDuringConversations;

		[Tooltip("If nonzero, check Variable['Alert'] at this frequency to show alert messages.")]
		public float alertCheckFrequency;

		[Tooltip("Show alerts for this many characters per second. If zero, use Subtitle Settings > Subtitle Chars Per Second.")]
		public float alertCharsPerSecond;

		[Tooltip("Show alerts for at least this many seconds. If zero, use Subtitle Settings > Min Subtitle Seconds.")]
		public float minAlertSeconds;

		public AlertSettings()
		{
		}

		public AlertSettings(AlertSettings source)
		{
			allowAlertsDuringConversations = source.allowAlertsDuringConversations;
			alertCheckFrequency = source.alertCheckFrequency;
			alertCharsPerSecond = source.alertCharsPerSecond;
			minAlertSeconds = source.minAlertSeconds;
		}
	}

	[HideInInspector]
	public ConversationOverrideDisplaySettings conversationOverrideSettings;

	[Tooltip("Assign a GameObject that contains an active dialogue UI component. Can be a prefab. If unassigned, Dialogue Manager will search its children for an active dialogue UI component.")]
	public GameObject dialogueUI;

	[Tooltip("Optional. Assign Canvas into which dialogue UI will be instantiated if it's a prefab.")]
	public Canvas defaultCanvas;

	public LocalizationSettings localizationSettings = new LocalizationSettings();

	public SubtitleSettings subtitleSettings = new SubtitleSettings();

	public CameraSettings cameraSettings = new CameraSettings();

	public InputSettings inputSettings = new InputSettings();

	public BarkSettings barkSettings = new BarkSettings();

	public AlertSettings alertSettings = new AlertSettings();

	public DisplaySettings()
	{
	}

	public DisplaySettings(DisplaySettings source)
	{
		conversationOverrideSettings = source.conversationOverrideSettings;
		dialogueUI = source.dialogueUI;
		defaultCanvas = source.defaultCanvas;
		localizationSettings = new LocalizationSettings(source.localizationSettings);
		subtitleSettings = new SubtitleSettings(source.subtitleSettings);
		cameraSettings = new CameraSettings(source.cameraSettings);
		inputSettings = new InputSettings(source.inputSettings);
		barkSettings = new BarkSettings(source.barkSettings);
		alertSettings = new AlertSettings(source.alertSettings);
	}

	public bool ShouldUseOverrides()
	{
		if (conversationOverrideSettings != null)
		{
			return conversationOverrideSettings.useOverrides;
		}
		return false;
	}

	public bool ShouldUseSubtitleOverrides()
	{
		if (ShouldUseOverrides())
		{
			return conversationOverrideSettings.overrideSubtitleSettings;
		}
		return false;
	}

	public bool GetShowNPCSubtitlesDuringLine()
	{
		if (!ShouldUseSubtitleOverrides())
		{
			if (subtitleSettings == null)
			{
				return true;
			}
			return subtitleSettings.showNPCSubtitlesDuringLine;
		}
		return conversationOverrideSettings.showNPCSubtitlesDuringLine;
	}

	public bool GetShowNPCSubtitlesWithResponses()
	{
		if (!ShouldUseSubtitleOverrides())
		{
			if (subtitleSettings == null)
			{
				return true;
			}
			return subtitleSettings.showNPCSubtitlesWithResponses;
		}
		return conversationOverrideSettings.showNPCSubtitlesWithResponses;
	}

	public bool GetShowPCSubtitlesDuringLine()
	{
		if (!ShouldUseSubtitleOverrides())
		{
			if (subtitleSettings == null)
			{
				return true;
			}
			return subtitleSettings.showPCSubtitlesDuringLine;
		}
		return conversationOverrideSettings.showPCSubtitlesDuringLine;
	}

	public bool GetSkipPCSubtitleAfterResponseMenu()
	{
		if (!ShouldUseSubtitleOverrides())
		{
			if (subtitleSettings == null)
			{
				return true;
			}
			return subtitleSettings.skipPCSubtitleAfterResponseMenu;
		}
		return conversationOverrideSettings.skipPCSubtitleAfterResponseMenu;
	}

	public float GetSubtitleCharsPerSecond()
	{
		if (!ShouldUseSubtitleOverrides())
		{
			if (subtitleSettings == null)
			{
				return 30f;
			}
			return subtitleSettings.subtitleCharsPerSecond;
		}
		return conversationOverrideSettings.subtitleCharsPerSecond;
	}

	public float GetMinSubtitleSeconds()
	{
		if (!ShouldUseSubtitleOverrides())
		{
			if (subtitleSettings == null)
			{
				return 2f;
			}
			return subtitleSettings.minSubtitleSeconds;
		}
		return conversationOverrideSettings.minSubtitleSeconds;
	}

	public SubtitleSettings.ContinueButtonMode GetContinueButtonMode()
	{
		if (!ShouldUseSubtitleOverrides())
		{
			if (subtitleSettings == null)
			{
				return SubtitleSettings.ContinueButtonMode.Never;
			}
			return subtitleSettings.continueButton;
		}
		return conversationOverrideSettings.continueButton;
	}

	public bool ShouldUseSequenceOverrides()
	{
		if (ShouldUseOverrides())
		{
			return conversationOverrideSettings.overrideSequenceSettings;
		}
		return false;
	}

	public string GetDefaultSequence()
	{
		if (!ShouldUseSequenceOverrides() || string.IsNullOrEmpty(conversationOverrideSettings.defaultSequence))
		{
			if (cameraSettings == null)
			{
				return string.Empty;
			}
			return cameraSettings.defaultSequence;
		}
		return conversationOverrideSettings.defaultSequence;
	}

	public string GetDefaultPlayerSequence()
	{
		if (!ShouldUseSequenceOverrides() || string.IsNullOrEmpty(conversationOverrideSettings.defaultPlayerSequence))
		{
			if (cameraSettings == null)
			{
				return string.Empty;
			}
			return cameraSettings.defaultPlayerSequence;
		}
		return conversationOverrideSettings.defaultPlayerSequence;
	}

	public string GetDefaultResponseMenuSequence()
	{
		if (!ShouldUseSequenceOverrides() || string.IsNullOrEmpty(conversationOverrideSettings.defaultResponseMenuSequence))
		{
			if (cameraSettings == null)
			{
				return string.Empty;
			}
			return cameraSettings.defaultResponseMenuSequence;
		}
		return conversationOverrideSettings.defaultResponseMenuSequence;
	}

	public bool ShouldUseInputOverrides()
	{
		if (ShouldUseOverrides())
		{
			return conversationOverrideSettings.overrideInputSettings;
		}
		return false;
	}

	public bool GetAlwaysForceResponseMenu()
	{
		if (!ShouldUseInputOverrides())
		{
			if (inputSettings == null)
			{
				return true;
			}
			return inputSettings.alwaysForceResponseMenu;
		}
		return conversationOverrideSettings.alwaysForceResponseMenu;
	}

	public bool GetIncludeInvalidEntries()
	{
		if (!ShouldUseInputOverrides())
		{
			if (inputSettings == null)
			{
				return true;
			}
			return inputSettings.includeInvalidEntries;
		}
		return conversationOverrideSettings.includeInvalidEntries;
	}

	public float GetResponseTimeout()
	{
		if (!ShouldUseInputOverrides())
		{
			if (inputSettings == null)
			{
				return 0f;
			}
			return inputSettings.responseTimeout;
		}
		return conversationOverrideSettings.responseTimeout;
	}

	public EmTag GetEmTagForOldResponses()
	{
		if (!ShouldUseInputOverrides())
		{
			if (inputSettings == null)
			{
				return EmTag.None;
			}
			return inputSettings.emTagForOldResponses;
		}
		return conversationOverrideSettings.emTagForOldResponses;
	}

	public EmTag GetEmTagForInvalidResponses()
	{
		if (!ShouldUseInputOverrides())
		{
			if (inputSettings == null)
			{
				return EmTag.None;
			}
			return inputSettings.emTagForInvalidResponses;
		}
		return conversationOverrideSettings.emTagForInvalidResponses;
	}

	public InputTrigger GetCancelSubtitleInput()
	{
		if (!ShouldUseInputOverrides())
		{
			if (inputSettings == null)
			{
				return null;
			}
			return inputSettings.cancel;
		}
		return conversationOverrideSettings.cancelSubtitle;
	}

	public InputTrigger GetCancelConversationInput()
	{
		if (!ShouldUseInputOverrides())
		{
			if (inputSettings == null)
			{
				return null;
			}
			return inputSettings.cancelConversation;
		}
		return conversationOverrideSettings.cancelConversation;
	}
}
