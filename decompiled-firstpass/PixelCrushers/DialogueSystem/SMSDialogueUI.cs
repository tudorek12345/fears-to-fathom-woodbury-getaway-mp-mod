using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

public class SMSDialogueUI : StandardDialogueUI
{
	[Serializable]
	public class PreDelaySettings
	{
		[Tooltip("Show this when waiting before showing subtitle. Often a '...' image suggesting NPC is typing.")]
		public GameObject preDelayIcon;

		[Tooltip("Before showing subtitle, delay based on the Dialogue Text length and Dialogue Manager > Subtitle Settings > Subtitle Chars Per Second.")]
		public bool basedOnTextLength;

		[Tooltip("Before showing subtitle, also delay for this many seconds.")]
		public float additionalSeconds;

		public float GetDelayDuration(Subtitle subtitle)
		{
			return (basedOnTextLength ? Mathf.Max(DialogueManager.DisplaySettings.subtitleSettings.minSubtitleSeconds, (float)subtitle.formattedText.text.Length / Mathf.Max(1f, DialogueManager.DisplaySettings.subtitleSettings.subtitleCharsPerSecond)) : 0f) + additionalSeconds;
		}

		public void CopyTo(PreDelaySettings dest)
		{
			dest.basedOnTextLength = basedOnTextLength;
			dest.additionalSeconds = additionalSeconds;
		}
	}

	[Serializable]
	public class DialogueEntryRecord
	{
		public int conversationID;

		public int entryID;

		public DialogueEntryRecord(int conversationID, int entryID)
		{
			this.conversationID = conversationID;
			this.entryID = entryID;
		}
	}

	[Header("Heading")]
	[Tooltip("(Optional) If assigned, set to the conversation title.")]
	public Text headingText;

	[Header("Message Panel")]
	[Tooltip("The scroll rect containing the content panel.")]
	public ScrollRect scrollRect;

	[Tooltip("The content panel inside the scroll rect containing the message panel and response panel.")]
	public RectTransform contentPanel;

	[Tooltip("Add messages to this panel.")]
	public RectTransform messagePanel;

	[Tooltip("If non-zero, drop older messages when the number of messages in the history reaches this value.")]
	public int maxMessages;

	[Tooltip("Speed at which to smoothly scroll down.")]
	public float scrollSpeed = 5f;

	[Tooltip("Before showing NPC subtitles, delay for this duration.")]
	public PreDelaySettings npcPreDelaySettings = new PreDelaySettings();

	[Tooltip("Before showing PC subtitles, delay for this duration.")]
	public PreDelaySettings pcPreDelaySettings = new PreDelaySettings();

	[Header("Save/Load")]
	[Tooltip("Load the saved conversation specified in the Conversation variable.")]
	public bool useConversationVariable;

	[Tooltip("When resuming conversation, don't play sequence of last entry.")]
	public bool dontRepeatLastSequence;

	[Tooltip("Disable Audio() and AudioWait() sequencer commands when resuming last entry.")]
	public bool disableAudioOnLastSequence = true;

	[Tooltip("When entering these scene(s), don't resume the conversation. Typically used for the start menu (scene 0).")]
	public int[] dontLoadConversationInScenes = new int[1];

	public static string conversationVariableOverride;

	protected List<DialogueEntryRecord> records = new List<DialogueEntryRecord>();

	protected List<GameObject> instantiatedMessages = new List<GameObject>();

	protected int currentSceneIndex = -1;

	protected PreDelaySettings npcPreDelaySettingsCopy = new PreDelaySettings();

	protected PreDelaySettings pcPreDelaySettingsCopy = new PreDelaySettings();

	protected bool isLoadingGame;

	protected bool skipNextRecord;

	protected bool isInPreDelay;

	protected Coroutine scrollCoroutine;

	protected bool shouldShowContinueButton;

	protected Button continueButton;

	protected Dictionary<Transform, DialogueActor> dialogueActorCache = new Dictionary<Transform, DialogueActor>();

	public string conversationVariableValue => DialogueLua.GetVariable("Conversation").AsString;

	public string currentConversationActor
	{
		get
		{
			if (!useConversationVariable)
			{
				return "CurrentConversationActor";
			}
			return "CurrentConversationActor_" + conversationVariableValue;
		}
	}

	public string currentConversationConversant
	{
		get
		{
			if (!useConversationVariable)
			{
				return "CurrentConversationConversant";
			}
			return "CurrentConversationConversant_" + conversationVariableValue;
		}
	}

	public string currentDialogueEntryRecords
	{
		get
		{
			if (!useConversationVariable)
			{
				return "DialogueEntryRecords";
			}
			return "DialogueEntryRecords_" + conversationVariableValue;
		}
	}

	protected virtual void CheckAssignments()
	{
		if ((UnityEngine.Object)(object)scrollRect == null)
		{
			Debug.LogWarning("Textline: Assign the dialogue UI's Scroll Rect", this);
		}
		if (contentPanel == null)
		{
			Debug.LogWarning("Textline: Assign the dialogue UI's Content Panel", this);
		}
		if (messagePanel == null)
		{
			Debug.LogWarning("Textline: Assign the dialogue UI's Message Panel", this);
		}
	}

	public override void OnEnable()
	{
		base.OnEnable();
		SceneManager.sceneLoaded -= RecordCurrentScene;
		SceneManager.sceneLoaded += RecordCurrentScene;
		PersistentDataManager.RegisterPersistentData(base.gameObject);
	}

	public override void OnDisable()
	{
		base.OnDisable();
		SceneManager.sceneLoaded -= RecordCurrentScene;
		PersistentDataManager.UnregisterPersistentData(base.gameObject);
	}

	protected void RecordCurrentScene(Scene scene, LoadSceneMode mode)
	{
		currentSceneIndex = scene.buildIndex;
	}

	public override void Open()
	{
		base.Open();
		CheckAssignments();
		DestroyInstantiatedMessages();
		dialogueActorCache.Clear();
		if ((UnityEngine.Object)(object)headingText != null)
		{
			Conversation conversation = DialogueManager.masterDatabase.GetConversation(DialogueManager.lastConversationID);
			if (conversation != null)
			{
				headingText.text = Field.LookupLocalizedValue(conversation.fields, "Title");
			}
		}
		Tools.SetGameObjectActive(npcPreDelaySettings.preDelayIcon, value: false);
		Tools.SetGameObjectActive(pcPreDelaySettings.preDelayIcon, value: false);
	}

	public override void Close()
	{
		StopAllCoroutines();
		base.Close();
		if (!isLoadingGame)
		{
			records.Clear();
		}
	}

	public override void ShowSubtitle(Subtitle subtitle)
	{
		if (subtitle.dialogueEntry.id != 0 && !string.IsNullOrEmpty(subtitle.formattedText.text))
		{
			float num = (subtitle.speakerInfo.IsNPC ? npcPreDelaySettings.GetDelayDuration(subtitle) : pcPreDelaySettings.GetDelayDuration(subtitle));
			if (Mathf.Approximately(0f, num))
			{
				AddMessage(subtitle);
			}
			else
			{
				StartCoroutine(AddMessageWithPreDelay(num, subtitle));
			}
			AddRecord(subtitle);
		}
	}

	public override void HideSubtitle(Subtitle subtitle)
	{
	}

	public override void ShowResponses(Subtitle subtitle, Response[] responses, float timeout)
	{
		if ((UnityEngine.Object)(object)continueButton != null)
		{
			((Component)(object)continueButton).gameObject.SetActive(value: false);
		}
		if (isInPreDelay && !isLoadingGame)
		{
			ShowResponsesNow(subtitle, responses, timeout);
		}
		else
		{
			ShowResponsesNow(subtitle, responses, timeout);
		}
	}

	protected IEnumerator ShowResponsesAfterPreDelay(Subtitle subtitle, Response[] responses, float timeout)
	{
		float safeguardTime = Time.time + 10f;
		while (isInPreDelay && Time.time < safeguardTime)
		{
			yield return null;
		}
		ShowResponsesNow(subtitle, responses, timeout);
	}

	protected virtual void ShowResponsesNow(Subtitle subtitle, Response[] responses, float timeout)
	{
		Tools.SetGameObjectActive(npcPreDelaySettings.preDelayIcon, value: false);
		Tools.SetGameObjectActive(pcPreDelaySettings.preDelayIcon, value: false);
		base.ShowResponses(subtitle, responses, timeout);
		ScrollToBottom();
	}

	protected virtual DialogueActor GetDialogueActor(Subtitle subtitle)
	{
		if (subtitle.speakerInfo.transform == null)
		{
			return null;
		}
		if (dialogueActorCache.TryGetValue(subtitle.speakerInfo.transform, out var value))
		{
			return value;
		}
		value = DialogueActor.GetDialogueActorComponent(subtitle.speakerInfo.transform);
		dialogueActorCache[subtitle.speakerInfo.transform] = value;
		return value;
	}

	protected virtual StandardUISubtitlePanel GetTemplate(Subtitle subtitle, DialogueActor dialogueActor)
	{
		SubtitlePanelNumber subtitlePanelNumber = ((dialogueActor != null) ? dialogueActor.GetSubtitlePanelNumber() : SubtitlePanelNumber.Default);
		if (subtitlePanelNumber != SubtitlePanelNumber.Default)
		{
			return conversationUIElements.subtitlePanels[PanelNumberUtility.GetSubtitlePanelIndex(subtitlePanelNumber)];
		}
		if (!subtitle.speakerInfo.IsNPC)
		{
			return conversationUIElements.defaultPCSubtitlePanel;
		}
		return conversationUIElements.defaultNPCSubtitlePanel;
	}

	protected virtual StandardUISubtitlePanel GetTemplate(Subtitle subtitle)
	{
		return GetTemplate(subtitle, null);
	}

	protected virtual IEnumerator AddMessageWithPreDelay(float preDelay, Subtitle subtitle)
	{
		isInPreDelay = true;
		GameObject preDelayIcon = (subtitle.speakerInfo.isNPC ? npcPreDelaySettings.preDelayIcon : pcPreDelaySettings.preDelayIcon);
		Tools.SetGameObjectActive(preDelayIcon, value: true);
		if (preDelayIcon != null)
		{
			preDelayIcon.transform.SetAsLastSibling();
		}
		ScrollToBottom();
		yield return new WaitForSeconds(preDelay);
		Sequencer.Message(subtitle.speakerInfo.isNPC ? "Received" : "Sent");
		Tools.SetGameObjectActive(preDelayIcon, value: false);
		AddMessage(subtitle);
		isInPreDelay = false;
		yield return null;
	}

	protected virtual void AddMessage(Subtitle subtitle)
	{
		DialogueActor dialogueActor = GetDialogueActor(subtitle);
		GameObject gameObject = UnityEngine.Object.Instantiate(GetTemplate(subtitle, dialogueActor).panel.gameObject);
		string text = subtitle.formattedText.text;
		gameObject.name = ((text.Length <= 20) ? text : (text.Substring(0, Mathf.Min(20, text.Length)) + "..."));
		instantiatedMessages.Add(gameObject);
		gameObject.transform.SetParent(messagePanel.transform, worldPositionStays: false);
		StandardUISubtitlePanel component = gameObject.GetComponent<StandardUISubtitlePanel>();
		if (component.addSpeakerName)
		{
			subtitle.formattedText.text = FormattedText.Parse(string.Format(component.addSpeakerNameFormat, new object[2]
			{
				subtitle.speakerInfo.Name,
				subtitle.formattedText.text
			})).text;
		}
		if (dialogueActor != null && dialogueActor.standardDialogueUISettings.setSubtitleColor)
		{
			subtitle.formattedText.text = dialogueActor.AdjustSubtitleColor(subtitle);
		}
		component.ShowSubtitle(subtitle);
		continueButton = component.continueButton;
		if (shouldShowContinueButton && !isLoadingGame)
		{
			component.ShowContinueButton();
		}
		else
		{
			component.HideContinueButton();
		}
		if (isLoadingGame)
		{
			AbstractTypewriterEffect typewriter = component.GetTypewriter();
			if (typewriter != null)
			{
				typewriter.Stop();
			}
		}
		if (maxMessages > 0 && instantiatedMessages.Count > maxMessages)
		{
			UnityEngine.Object.Destroy(instantiatedMessages[0]);
			instantiatedMessages.RemoveAt(0);
		}
		ScrollToBottom();
	}

	public override void ShowContinueButton(Subtitle subtitle)
	{
		shouldShowContinueButton = true;
		if ((UnityEngine.Object)(object)continueButton != null)
		{
			((Component)(object)continueButton).gameObject.SetActive(value: true);
		}
	}

	public override void OnContinueConversation()
	{
		if ((UnityEngine.Object)(object)continueButton != null)
		{
			((Component)(object)continueButton).gameObject.SetActive(value: false);
		}
		base.OnContinueConversation();
	}

	protected virtual T FindUIElement<T>(Transform t, string name1, string name2) where T : MonoBehaviour
	{
		if (t == null)
		{
			return null;
		}
		if (string.Equals(t.name, name1) || string.Equals(t.name, name2))
		{
			return t.GetComponent<T>();
		}
		foreach (Transform item in t)
		{
			T val = FindUIElement<T>(item, name1, name2);
			if (val != null)
			{
				return val;
			}
		}
		return null;
	}

	protected virtual void ScrollToBottom()
	{
		if (scrollCoroutine != null)
		{
			StopCoroutine(scrollCoroutine);
		}
		scrollCoroutine = StartCoroutine(ScrollToBottomCoroutine());
	}

	protected virtual IEnumerator ScrollToBottomCoroutine()
	{
		if ((UnityEngine.Object)(object)scrollRect == null)
		{
			yield break;
		}
		yield return null;
		float height = contentPanel.rect.height;
		float height2 = ((Component)(object)scrollRect).GetComponent<RectTransform>().rect.height;
		if (height > height2)
		{
			float ratio = height2 / height;
			float timeout = Time.time + 10f;
			while (scrollRect.verticalNormalizedPosition > 0.01f && Time.time < timeout)
			{
				float b = scrollRect.verticalNormalizedPosition - scrollSpeed * Time.deltaTime * ratio;
				scrollRect.verticalNormalizedPosition = Mathf.Max(0f, b);
				yield return null;
			}
		}
		scrollRect.verticalNormalizedPosition = 0f;
		scrollCoroutine = null;
	}

	protected virtual void AddRecord(Subtitle subtitle)
	{
		if (skipNextRecord)
		{
			skipNextRecord = false;
			return;
		}
		records.Add(new DialogueEntryRecord(subtitle.dialogueEntry.conversationID, subtitle.dialogueEntry.id));
		if (maxMessages > 0 && records.Count > maxMessages)
		{
			records.RemoveAt(0);
		}
	}

	public virtual void ClearContent()
	{
		records.Clear();
		DestroyInstantiatedMessages();
	}

	protected virtual void DestroyInstantiatedMessages()
	{
		for (int i = 0; i < instantiatedMessages.Count; i++)
		{
			UnityEngine.Object.Destroy(instantiatedMessages[i]);
		}
		instantiatedMessages.Clear();
	}

	protected virtual bool DontLoadInThisScene()
	{
		if (dontLoadConversationInScenes == null)
		{
			return false;
		}
		for (int i = 0; i < dontLoadConversationInScenes.Length; i++)
		{
			if (dontLoadConversationInScenes[i] == currentSceneIndex)
			{
				return true;
			}
		}
		return false;
	}

	public virtual void OnRecordPersistentData()
	{
		if (DontLoadInThisScene() || !DialogueManager.IsConversationActive)
		{
			return;
		}
		if (Debug.isDebugBuild)
		{
			Debug.Log("TextlineDialogueUI.OnRecordPersistentData: Saving current conversation to " + currentDialogueEntryRecords);
		}
		string value = ((DialogueManager.CurrentActor != null) ? DialogueManager.CurrentActor.name : string.Empty);
		string value2 = ((DialogueManager.CurrentConversant != null) ? DialogueManager.CurrentConversant.name : string.Empty);
		DialogueLua.SetVariable(currentConversationActor, value);
		DialogueLua.SetVariable(currentConversationConversant, value2);
		string text = records.Count + ";";
		foreach (DialogueEntryRecord record in records)
		{
			text = text + record.conversationID + ";" + record.entryID + ";";
		}
		DialogueLua.SetVariable(currentDialogueEntryRecords, text);
	}

	public virtual void OnApplyPersistentData()
	{
		if (!string.IsNullOrEmpty(conversationVariableOverride))
		{
			DialogueLua.SetVariable("Conversation", conversationVariableOverride);
		}
		if (DontLoadInThisScene())
		{
			Debug.Log("OnApplyPersistentData Dont Load in this scene: " + SceneManager.GetActiveScene().buildIndex);
		}
		if (DontLoadInThisScene())
		{
			return;
		}
		records.Clear();
		if (!DialogueLua.DoesVariableExist(currentDialogueEntryRecords))
		{
			return;
		}
		StopAllCoroutines();
		string asString = DialogueLua.GetVariable(currentDialogueEntryRecords).AsString;
		if (Debug.isDebugBuild)
		{
			Debug.Log("TextlineDialogueUI.OnApplyPersistentData: Restoring current conversation from " + currentDialogueEntryRecords + ": " + asString);
		}
		string[] array = asString.Split(';');
		int num = Tools.StringToInt(array[0]);
		for (int i = 0; i < num; i++)
		{
			int conversationID = Tools.StringToInt(array[1 + i * 2]);
			int entryID = Tools.StringToInt(array[2 + i * 2]);
			records.Add(new DialogueEntryRecord(conversationID, entryID));
		}
		if (records.Count == 0)
		{
			return;
		}
		DialogueEntryRecord dialogueEntryRecord = records[records.Count - 1];
		if (dialogueEntryRecord.conversationID >= 0 && dialogueEntryRecord.entryID > 0)
		{
			Button val = null;
			try
			{
				isLoadingGame = true;
				Conversation conversation = DialogueManager.MasterDatabase.GetConversation(dialogueEntryRecord.conversationID);
				string asString2 = DialogueLua.GetVariable(currentConversationActor).AsString;
				string asString3 = DialogueLua.GetVariable(currentConversationConversant).AsString;
				GameObject gameObject = GameObject.Find(asString2);
				GameObject gameObject2 = GameObject.Find(asString3);
				Transform actor = ((gameObject != null) ? gameObject.transform : null);
				Transform conversant = ((gameObject2 != null) ? gameObject2.transform : null);
				if (Debug.isDebugBuild)
				{
					Debug.Log("Resuming '" + conversation.Title + "' at entry " + dialogueEntryRecord.entryID);
				}
				DialogueManager.StopConversation();
				DialogueEntry dialogueEntry = DialogueManager.MasterDatabase.GetDialogueEntry(dialogueEntryRecord.conversationID, dialogueEntryRecord.entryID);
				string sequence = dialogueEntry.Sequence;
				npcPreDelaySettings.CopyTo(npcPreDelaySettingsCopy);
				pcPreDelaySettings.CopyTo(pcPreDelaySettingsCopy);
				npcPreDelaySettings.basedOnTextLength = false;
				npcPreDelaySettings.additionalSeconds = 0f;
				pcPreDelaySettings.basedOnTextLength = false;
				pcPreDelaySettings.additionalSeconds = 0f;
				if (dialogueEntry.Sequence.Contains("WaitForMessage(Forever)") || dialogueEntry.outgoingLinks.Count == 0)
				{
					if (!dialogueEntry.Sequence.Contains("WaitForMessage(Forever)"))
					{
						dialogueEntry.Sequence = "WaitForMessage(Forever); " + dialogueEntry.Sequence;
					}
				}
				else if (dontRepeatLastSequence)
				{
					dialogueEntry.Sequence = "None()";
				}
				else if (disableAudioOnLastSequence)
				{
					if (string.IsNullOrEmpty(dialogueEntry.Sequence))
					{
						dialogueEntry.Sequence = DialogueManager.displaySettings.cameraSettings.defaultSequence;
					}
					dialogueEntry.Sequence = dialogueEntry.Sequence.Replace("AudioWait(", "None(").Replace("Audio(", "None(");
				}
				skipNextRecord = true;
				isInPreDelay = false;
				DialogueManager.StartConversation(conversation.Title, actor, conversant, dialogueEntryRecord.entryID);
				val = continueButton;
				dialogueEntry.Sequence = sequence;
				npcPreDelaySettingsCopy.CopyTo(npcPreDelaySettings);
				pcPreDelaySettingsCopy.CopyTo(pcPreDelaySettings);
				GameObject gameObject3 = ((instantiatedMessages.Count > 0) ? instantiatedMessages[instantiatedMessages.Count - 1] : null);
				instantiatedMessages.Remove(gameObject3);
				DestroyInstantiatedMessages();
				for (int j = 0; j < records.Count - 1; j++)
				{
					DialogueEntry dialogueEntry2 = DialogueManager.MasterDatabase.GetDialogueEntry(records[j].conversationID, records[j].entryID);
					CharacterInfo characterInfo = DialogueManager.ConversationModel.GetCharacterInfo(dialogueEntry2.ActorID);
					CharacterInfo characterInfo2 = DialogueManager.ConversationModel.GetCharacterInfo(dialogueEntry2.ConversantID);
					FormattedText formattedText = FormattedText.Parse(dialogueEntry2.currentDialogueText, DialogueManager.MasterDatabase.emphasisSettings);
					Subtitle subtitle = new Subtitle(characterInfo, characterInfo2, formattedText, "None()", dialogueEntry2.ResponseMenuSequence, dialogueEntry2);
					AddMessage(subtitle);
				}
				if (gameObject3 != null)
				{
					instantiatedMessages.Add(gameObject3);
					gameObject3.transform.SetAsLastSibling();
				}
				if (!dontRepeatLastSequence)
				{
					Sequencer.Message("Sent");
					Sequencer.Message("Received");
				}
			}
			finally
			{
				isLoadingGame = false;
				scrollRect.verticalNormalizedPosition = 0f;
				continueButton = val;
				if (shouldShowContinueButton && (UnityEngine.Object)(object)val != null)
				{
					((Component)(object)val).gameObject.SetActive(value: true);
				}
			}
		}
		ScrollToBottom();
	}

	public void ClearRecords()
	{
		records.Clear();
	}

	public virtual void SaveConversation()
	{
		if (base.isOpen)
		{
			OnRecordPersistentData();
		}
	}

	public virtual void ResumeConversation(string conversation = null)
	{
		if (base.isOpen)
		{
			return;
		}
		if (!string.IsNullOrEmpty(conversation))
		{
			DialogueLua.SetVariable("Conversation", conversation);
		}
		if (DialogueLua.DoesVariableExist(currentDialogueEntryRecords))
		{
			OnApplyPersistentData();
			return;
		}
		if (string.IsNullOrEmpty(conversation))
		{
			conversation = DialogueLua.GetVariable("Conversation").asString;
		}
		DialogueManager.StartConversation(conversation);
	}
}
