using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public static class BarkController
{
	private static Dictionary<Transform, int> currentBarkPriority;

	public static Sequencer LastSequencer { get; private set; }

	static BarkController()
	{
		currentBarkPriority = new Dictionary<Transform, int>();
		LastSequencer = null;
	}

	private static int GetSpeakerCurrentBarkPriority(Transform speaker)
	{
		if (!currentBarkPriority.ContainsKey(speaker))
		{
			return 0;
		}
		return currentBarkPriority[speaker];
	}

	private static void SetSpeakerCurrentBarkPriority(Transform speaker, int priority)
	{
		if (currentBarkPriority.ContainsKey(speaker))
		{
			currentBarkPriority[speaker] = priority;
		}
		else
		{
			currentBarkPriority.Add(speaker, priority);
		}
	}

	private static int GetEntryBarkPriority(DialogueEntry entry)
	{
		if (entry != null)
		{
			return Field.LookupInt(entry.fields, "Priority");
		}
		return 0;
	}

	public static IEnumerator Bark(string conversationTitle, Transform speaker, Transform listener, BarkHistory barkHistory, DialogueDatabase database = null, bool stopAtFirstValid = false)
	{
		if (CheckDontBarkDuringConversation())
		{
			yield break;
		}
		bool barked = false;
		if (string.IsNullOrEmpty(conversationTitle) && DialogueDebug.logWarnings)
		{
			Debug.Log(string.Format("{0}: Bark (speaker={1}, listener={2}): conversation title is blank", new object[3] { "Dialogue System", speaker, listener }), speaker);
		}
		if (speaker == null)
		{
			speaker = DialogueManager.instance.FindActorTransformFromConversation(conversationTitle, "Actor");
		}
		if (speaker == null && DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Bark (speaker={1}, listener={2}): '{3}' speaker is null", "Dialogue System", speaker, listener, conversationTitle));
		}
		if (string.IsNullOrEmpty(conversationTitle) || speaker == null)
		{
			yield break;
		}
		IBarkUI barkUI = DialogueActor.GetBarkUI(speaker);
		if (barkUI == null && DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Bark (speaker={1}, listener={2}): '{3}' speaker has no bark UI", "Dialogue System", speaker, listener, conversationTitle), speaker);
		}
		bool stopAtFirstValid2 = stopAtFirstValid || (barkHistory != null && barkHistory.order == BarkOrder.FirstValid);
		ConversationModel conversationModel = new ConversationModel(database ?? DialogueManager.masterDatabase, conversationTitle, speaker, listener, DialogueManager.allowLuaExceptions, DialogueManager.isDialogueEntryValid, -1, stopAtFirstValid2);
		ConversationState firstState = conversationModel.firstState;
		if (firstState == null && DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Bark (speaker={1}, listener={2}): '{3}' has no START entry", "Dialogue System", speaker, listener, conversationTitle), speaker);
		}
		if (firstState != null && !firstState.hasAnyResponses && DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Bark (speaker={1}, listener={2}): '{3}' has no valid bark at this time", "Dialogue System", speaker, listener, conversationTitle), speaker);
		}
		if (firstState == null || !firstState.hasAnyResponses)
		{
			yield break;
		}
		try
		{
			Response[] array = (firstState.hasNPCResponse ? firstState.npcResponses : firstState.pcResponses);
			int nextIndex = (barkHistory ?? new BarkHistory(BarkOrder.Random)).GetNextIndex(array.Length);
			DialogueEntry destinationEntry = array[nextIndex].destinationEntry;
			if (destinationEntry == null && DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Bark (speaker={1}, listener={2}): '{3}' bark entry is null", "Dialogue System", speaker, listener, conversationTitle), speaker);
			}
			if (destinationEntry == null)
			{
				yield break;
			}
			int entryBarkPriority = GetEntryBarkPriority(destinationEntry);
			if (entryBarkPriority < GetSpeakerCurrentBarkPriority(speaker))
			{
				if (DialogueDebug.logInfo)
				{
					Debug.Log(string.Format("{0}: Bark (speaker={1}, listener={2}): '{3}' currently barking a higher priority bark", "Dialogue System", speaker, listener, conversationTitle), speaker);
				}
				yield break;
			}
			SetSpeakerCurrentBarkPriority(speaker, entryBarkPriority);
			barked = true;
			InformParticipants("OnBarkStart", speaker, listener);
			ConversationState state = conversationModel.GetState(destinationEntry, includeLinks: false);
			if (state == null)
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning(string.Format("{0}: Bark (speaker={1}, listener={2}): '{3}' can't find a valid dialogue entry", "Dialogue System", speaker, listener, conversationTitle), speaker);
				}
				yield break;
			}
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Bark (speaker={1}, listener={2}): '{3}'", "Dialogue System", speaker, listener, state.subtitle.formattedText.text), speaker);
			}
			InformParticipantsLine("OnBarkLine", speaker, state.subtitle);
			if ((barkUI == null || !(barkUI as MonoBehaviour).enabled) && DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Bark (speaker={1}, listener={2}): '{3}' bark UI is null or disabled", "Dialogue System", speaker, listener, state.subtitle.formattedText.text), speaker);
			}
			if (barkUI != null && (barkUI as MonoBehaviour).enabled)
			{
				CheckCancelPreviousBarkSequence(speaker, barkUI);
				barkUI.Bark(state.subtitle);
			}
			Sequencer sequencer = (LastSequencer = PlayBarkSequence(state.subtitle, speaker, listener));
			while ((sequencer != null && sequencer.isPlaying) || (barkUI != null && barkUI.isPlaying))
			{
				yield return null;
			}
			if (sequencer != null)
			{
				Object.Destroy(sequencer);
			}
		}
		finally
		{
			if (barked)
			{
				InformParticipants("OnBarkEnd", speaker, listener);
				SetSpeakerCurrentBarkPriority(speaker, 0);
			}
		}
	}

	private static void CheckCancelPreviousBarkSequence(Transform speaker, IBarkUI barkUI)
	{
		if (barkUI.isPlaying && barkUI is StandardBarkUI)
		{
			StandardBarkUI standardBarkUI = barkUI as StandardBarkUI;
			if (standardBarkUI.waitUntilSequenceEnds && standardBarkUI.cancelWaitUntilSequenceEndsIfReplacingBark)
			{
				standardBarkUI.Hide();
			}
		}
	}

	private static Sequencer PlayBarkSequence(Subtitle subtitle, Transform speaker, Transform listener)
	{
		return PlayBarkSequence(subtitle.formattedText.text, subtitle.sequence, subtitle.entrytag, speaker, listener);
	}

	private static Sequencer PlayBarkSequence(string barkText, string sequence, string entrytag, Transform speaker, Transform listener)
	{
		if (string.IsNullOrEmpty(sequence))
		{
			sequence = DialogueManager.displaySettings.barkSettings.defaultBarkSequence;
		}
		if (!string.IsNullOrEmpty(sequence))
		{
			sequence = Sequencer.ReplaceShortcuts(sequence);
			if (sequence.Contains("{{end}}"))
			{
				int num = ((!string.IsNullOrEmpty(barkText)) ? Tools.StripRichTextCodes(barkText).Length : 0);
				sequence = sequence.Replace("{{end}}", Mathf.Max(DialogueManager.displaySettings.GetMinSubtitleSeconds(), (float)num / Mathf.Max(1f, DialogueManager.displaySettings.GetSubtitleCharsPerSecond())).ToString(CultureInfo.InvariantCulture));
			}
			return DialogueManager.PlaySequence(sequence, speaker, listener, informParticipants: false, destroyWhenDone: false, entrytag);
		}
		return null;
	}

	public static IEnumerator Bark(Subtitle subtitle, Transform speaker, Transform listener, IBarkUI barkUI)
	{
		if (CheckDontBarkDuringConversation() || subtitle == null || subtitle.speakerInfo == null)
		{
			yield break;
		}
		int entryBarkPriority = GetEntryBarkPriority(subtitle.dialogueEntry);
		if (entryBarkPriority < GetSpeakerCurrentBarkPriority(speaker))
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Bark (speaker={1}, listener={2}): '{3}' currently barking a higher priority bark", "Dialogue System", speaker, listener, subtitle.formattedText.text), speaker);
			}
			yield break;
		}
		SetSpeakerCurrentBarkPriority(speaker, entryBarkPriority);
		InformParticipants("OnBarkStart", speaker, listener);
		InformParticipantsLine("OnBarkLine", speaker, subtitle);
		if (barkUI == null && DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Bark (speaker={1}, listener={2}): '{3}' speaker has no bark UI", "Dialogue System", speaker, listener, subtitle.formattedText.text), speaker);
		}
		if ((barkUI == null || !(barkUI as MonoBehaviour).enabled) && DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Bark (speaker={1}, listener={2}): '{3}' bark UI is null or disabled", "Dialogue System", speaker, listener, subtitle.formattedText.text), speaker);
		}
		CheckCancelPreviousBarkSequence(speaker, barkUI);
		if (barkUI != null && (barkUI as MonoBehaviour).enabled)
		{
			barkUI.Bark(subtitle);
		}
		Sequencer sequencer = (LastSequencer = PlayBarkSequence(subtitle, speaker, listener));
		while ((sequencer != null && sequencer.isPlaying) || (barkUI != null && barkUI.isPlaying))
		{
			yield return null;
		}
		if (sequencer != null)
		{
			Object.Destroy(sequencer);
		}
		InformParticipants("OnBarkEnd", speaker, listener);
		SetSpeakerCurrentBarkPriority(speaker, 0);
	}

	public static IEnumerator Bark(Subtitle subtitle, bool skipSequence = false)
	{
		if (CheckDontBarkDuringConversation() || subtitle == null || subtitle.speakerInfo == null)
		{
			yield break;
		}
		Transform speaker = subtitle.speakerInfo.transform;
		Transform listener = ((subtitle.listenerInfo != null) ? subtitle.listenerInfo.transform : null);
		int entryBarkPriority = GetEntryBarkPriority(subtitle.dialogueEntry);
		if (entryBarkPriority < GetSpeakerCurrentBarkPriority(speaker))
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Bark (speaker={1}, listener={2}): '{3}' currently barking a higher priority bark", "Dialogue System", speaker, listener, subtitle.formattedText.text), speaker);
			}
			yield break;
		}
		SetSpeakerCurrentBarkPriority(speaker, entryBarkPriority);
		InformParticipants("OnBarkStart", speaker, listener);
		InformParticipantsLine("OnBarkLine", speaker, subtitle);
		IBarkUI barkUI = DialogueActor.GetBarkUI(speaker);
		if (barkUI == null && DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Bark (speaker={1}, listener={2}): '{3}' speaker has no bark UI", "Dialogue System", speaker, listener, subtitle.formattedText.text), speaker);
		}
		if ((barkUI == null || !(barkUI as MonoBehaviour).enabled) && DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Bark (speaker={1}, listener={2}): '{3}' bark UI is null or disabled", "Dialogue System", speaker, listener, subtitle.formattedText.text), speaker);
		}
		CheckCancelPreviousBarkSequence(speaker, barkUI);
		if (barkUI != null && (barkUI as MonoBehaviour).enabled)
		{
			barkUI.Bark(subtitle);
		}
		Sequencer sequencer = null;
		if (!skipSequence)
		{
			sequencer = PlayBarkSequence(subtitle, speaker, listener);
		}
		LastSequencer = sequencer;
		while ((sequencer != null && sequencer.isPlaying) || (barkUI != null && barkUI.isPlaying))
		{
			yield return null;
		}
		if (sequencer != null)
		{
			Object.Destroy(sequencer);
		}
		InformParticipants("OnBarkEnd", speaker, listener);
		SetSpeakerCurrentBarkPriority(speaker, 0);
	}

	private static bool CheckDontBarkDuringConversation()
	{
		if (DialogueManager.isConversationActive && DialogueManager.displaySettings != null && DialogueManager.displaySettings.barkSettings != null)
		{
			return !DialogueManager.displaySettings.barkSettings.allowBarksDuringConversations;
		}
		return false;
	}

	private static void InformParticipants(string message, Transform speaker, Transform listener)
	{
		if (speaker != null)
		{
			speaker.BroadcastMessage(message, speaker, SendMessageOptions.DontRequireReceiver);
			if (listener != null && listener != speaker)
			{
				listener.BroadcastMessage(message, speaker, SendMessageOptions.DontRequireReceiver);
			}
		}
		Transform transform = DialogueManager.instance.transform;
		if (transform != speaker && transform != listener)
		{
			Transform parameter = ((speaker != null) ? speaker : ((listener != null) ? listener : transform));
			DialogueManager.instance.BroadcastMessage(message, parameter, SendMessageOptions.DontRequireReceiver);
		}
	}

	private static void InformParticipantsLine(string message, Transform speaker, Subtitle subtitle)
	{
		if (speaker != null)
		{
			speaker.BroadcastMessage(message, subtitle, SendMessageOptions.DontRequireReceiver);
		}
		if (DialogueManager.instance.transform != speaker)
		{
			DialogueManager.instance.BroadcastMessage(message, subtitle, SendMessageOptions.DontRequireReceiver);
		}
	}
}
