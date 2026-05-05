using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public abstract class QuestLogWindow : MonoBehaviour
{
	public enum QuestHeadingSource
	{
		Name,
		Description
	}

	[Serializable]
	public class QuestInfo
	{
		public string Group { get; set; }

		public string GroupDisplayName { get; set; }

		public string Title { get; set; }

		public FormattedText Heading { get; set; }

		public FormattedText Description { get; set; }

		public FormattedText[] Entries { get; set; }

		public QuestState[] EntryStates { get; set; }

		public bool Trackable { get; set; }

		public bool Track { get; set; }

		public bool Abandonable { get; set; }

		public QuestInfo(string group, string groupDisplayName, string title, FormattedText heading, FormattedText description, FormattedText[] entries, QuestState[] entryStates, bool trackable, bool track, bool abandonable)
		{
			Group = group;
			GroupDisplayName = groupDisplayName;
			Title = title;
			Heading = heading;
			Description = description;
			Entries = entries;
			EntryStates = entryStates;
			Trackable = trackable;
			Track = track;
			Abandonable = abandonable;
		}

		public QuestInfo(string group, string title, FormattedText heading, FormattedText description, FormattedText[] entries, QuestState[] entryStates, bool trackable, bool track, bool abandonable)
		{
			Group = group;
			GroupDisplayName = string.Empty;
			Title = title;
			Heading = heading;
			Description = description;
			Entries = entries;
			EntryStates = entryStates;
			Trackable = trackable;
			Track = track;
			Abandonable = abandonable;
		}

		public QuestInfo(string title, FormattedText heading, FormattedText description, FormattedText[] entries, QuestState[] entryStates, bool trackable, bool track, bool abandonable)
		{
			Group = string.Empty;
			GroupDisplayName = string.Empty;
			Title = title;
			Heading = heading;
			Description = description;
			Entries = entries;
			EntryStates = entryStates;
			Trackable = trackable;
			Track = track;
			Abandonable = abandonable;
		}
	}

	[Tooltip("Optional localized text table to use to localize no active/completed quests.")]
	public TextTable textTable;

	[Tooltip("Text to show (or localize) when there are no active quests.")]
	public string noActiveQuestsText = "No Active Quests";

	[Tooltip("Text to show (or localize) when there are no completed quests.")]
	public string noCompletedQuestsText = "No Completed Quests";

	[Tooltip("Check if quest has a field named 'Visible'. If field is false, don't show quest.")]
	public bool checkVisibleField;

	public QuestHeadingSource questHeadingSource;

	[Tooltip("State to assign to quests when player abandons then.")]
	[QuestState]
	public QuestState abandonQuestState = QuestState.Unassigned;

	public bool pauseWhileOpen = true;

	public bool unlockCursorWhileOpen = true;

	[Tooltip("Organize quests by the values of their Group fields.")]
	public bool useGroups;

	[Tooltip("If not blank, show this text next to quest titles that haven't been viewed yet. Will be localized if text has entry in Dialogue Manager's Text Table.")]
	public string newQuestText = string.Empty;

	[Tooltip("Allow only one quest to be tracked at a time.")]
	public bool trackOneQuestAtATime;

	[Tooltip("Clicking again on selected quest title deselects quest.")]
	public bool deselectQuestOnSecondClick = true;

	protected const QuestState ActiveQuestStateMask = QuestState.Active | QuestState.ReturnToNPC;

	protected QuestState currentQuestStateMask = QuestState.Active | QuestState.ReturnToNPC;

	protected float previousTimeScale = 1f;

	protected Coroutine refreshCoroutine;

	protected bool started;

	private bool wasCursorActive;

	public bool isOpen { get; protected set; }

	public QuestInfo[] quests { get; protected set; }

	public string[] groups { get; protected set; }

	public string selectedQuest { get; protected set; }

	public string noQuestsMessage { get; protected set; }

	public virtual bool isShowingActiveQuests => currentQuestStateMask == (QuestState.Active | QuestState.ReturnToNPC);

	public bool IsOpen
	{
		get
		{
			return isOpen;
		}
		protected set
		{
			isOpen = value;
		}
	}

	public QuestInfo[] Quests
	{
		get
		{
			return quests;
		}
		protected set
		{
			quests = value;
		}
	}

	public string[] Groups
	{
		get
		{
			return groups;
		}
		protected set
		{
			groups = value;
		}
	}

	public string SelectedQuest
	{
		get
		{
			return selectedQuest;
		}
		protected set
		{
			selectedQuest = value;
		}
	}

	public string NoQuestsMessage
	{
		get
		{
			return noQuestsMessage;
		}
		protected set
		{
			noQuestsMessage = value;
		}
	}

	public bool IsShowingActiveQuests => isShowingActiveQuests;

	public virtual void Awake()
	{
		isOpen = false;
		quests = new QuestInfo[0];
		groups = new string[0];
		selectedQuest = string.Empty;
		noQuestsMessage = string.Empty;
	}

	protected virtual void Start()
	{
		started = true;
		RegisterForUpdateTrackerEvents();
	}

	protected virtual void OnEnable()
	{
		if (started)
		{
			RegisterForUpdateTrackerEvents();
		}
	}

	protected virtual void OnDisable()
	{
		refreshCoroutine = null;
		UnregisterFromUpdateTrackerEvents();
	}

	protected void RegisterForUpdateTrackerEvents()
	{
		if (started && !(DialogueManager.instance == null) && !(GetComponentInParent<DialogueSystemController>() != null))
		{
			DialogueManager.instance.receivedUpdateTracker -= UpdateTracker;
			DialogueManager.instance.receivedUpdateTracker += UpdateTracker;
		}
	}

	protected void UnregisterFromUpdateTrackerEvents()
	{
		if (started && !(DialogueManager.instance == null))
		{
			DialogueManager.instance.receivedUpdateTracker -= UpdateTracker;
		}
	}

	public virtual void OpenWindow(Action openedWindowHandler)
	{
		openedWindowHandler();
	}

	public virtual void CloseWindow(Action closedWindowHandler)
	{
		closedWindowHandler();
	}

	public virtual void OnQuestListUpdated()
	{
	}

	public virtual void ConfirmAbandonQuest(string title, Action confirmedAbandonQuestHandler)
	{
	}

	public virtual void Open()
	{
		QuestLog.trackOneQuestAtATime = trackOneQuestAtATime;
		PauseGameplay();
		OpenWindow(OnOpenedWindow);
	}

	protected virtual void OnOpenedWindow()
	{
		isOpen = true;
		ShowQuests(currentQuestStateMask);
	}

	public virtual void Close()
	{
		CloseWindow(OnClosedWindow);
	}

	protected virtual void OnClosedWindow()
	{
		isOpen = false;
		ResumeGameplay();
	}

	protected virtual void PauseGameplay()
	{
		if (pauseWhileOpen)
		{
			previousTimeScale = Time.timeScale;
			Time.timeScale = 0f;
		}
		if (unlockCursorWhileOpen)
		{
			wasCursorActive = Tools.IsCursorActive();
			Tools.SetCursorActive(value: true);
		}
	}

	protected virtual void ResumeGameplay()
	{
		if (pauseWhileOpen)
		{
			Time.timeScale = previousTimeScale;
		}
		if (unlockCursorWhileOpen && !wasCursorActive)
		{
			Tools.SetCursorActive(value: false);
		}
	}

	public virtual bool IsQuestVisible(string questTitle)
	{
		if (checkVisibleField)
		{
			return Lua.IsTrue("Quest[\"" + DialogueLua.StringToTableIndex(questTitle) + "\"].Visible ~= false");
		}
		return true;
	}

	protected virtual void ShowQuests(QuestState questStateMask)
	{
		currentQuestStateMask = questStateMask;
		noQuestsMessage = GetNoQuestsMessage(questStateMask);
		List<QuestInfo> list = new List<QuestInfo>();
		if (useGroups)
		{
			QuestGroupRecord[] allGroupsAndQuests = QuestLog.GetAllGroupsAndQuests(questStateMask);
			foreach (QuestGroupRecord questGroupRecord in allGroupsAndQuests)
			{
				if (IsQuestVisible(questGroupRecord.questTitle))
				{
					list.Add(GetQuestInfo(questGroupRecord.groupName, questGroupRecord.questTitle));
				}
			}
		}
		else
		{
			string[] allQuests = QuestLog.GetAllQuests(questStateMask, sortByName: true, null);
			foreach (string text in allQuests)
			{
				if (IsQuestVisible(text))
				{
					list.Add(GetQuestInfo(string.Empty, text));
				}
			}
		}
		quests = list.ToArray();
		OnQuestListUpdated();
	}

	protected virtual QuestInfo GetQuestInfo(string group, string title)
	{
		FormattedText formattedText = FormattedText.Parse(QuestLog.GetQuestDescription(title), DialogueManager.masterDatabase.emphasisSettings);
		FormattedText formattedText2 = FormattedText.Parse(QuestLog.GetQuestTitle(title), DialogueManager.masterDatabase.emphasisSettings);
		FormattedText formattedText3 = ((questHeadingSource == QuestHeadingSource.Description) ? formattedText : formattedText2);
		string text = (string.IsNullOrEmpty(group) ? string.Empty : QuestLog.GetQuestGroup(title));
		string groupDisplayName = (string.IsNullOrEmpty(group) ? string.Empty : QuestLog.GetQuestGroupDisplayName(title));
		bool abandonable = QuestLog.IsQuestAbandonable(title) && isShowingActiveQuests;
		bool trackable = QuestLog.IsQuestTrackingAvailable(title) && isShowingActiveQuests;
		bool track = QuestLog.IsQuestTrackingEnabled(title);
		int questEntryCount = QuestLog.GetQuestEntryCount(title);
		FormattedText[] array = new FormattedText[questEntryCount];
		QuestState[] array2 = new QuestState[questEntryCount];
		for (int i = 0; i < questEntryCount; i++)
		{
			array[i] = FormattedText.Parse(QuestLog.GetQuestEntry(title, i + 1), DialogueManager.masterDatabase.emphasisSettings);
			array2[i] = QuestLog.GetQuestEntryState(title, i + 1);
		}
		if (!string.IsNullOrEmpty(newQuestText) && !QuestLog.WasQuestViewed(title))
		{
			formattedText3.text = formattedText3.text + " " + FormattedText.Parse(DialogueManager.GetLocalizedText(newQuestText)).text;
		}
		return new QuestInfo(text, groupDisplayName, title, formattedText3, formattedText, array, array2, trackable, track, abandonable);
	}

	protected virtual string GetNoQuestsMessage(QuestState questStateMask)
	{
		if (questStateMask != (QuestState.Active | QuestState.ReturnToNPC))
		{
			return GetLocalizedText(noCompletedQuestsText);
		}
		return GetLocalizedText(noActiveQuestsText);
	}

	public virtual string GetLocalizedText(string fieldName)
	{
		if (textTable != null && textTable.HasFieldTextForLanguage(fieldName, Localization.GetCurrentLanguageID(textTable)))
		{
			return textTable.GetFieldTextForLanguage(fieldName, Localization.GetCurrentLanguageID(textTable));
		}
		return DialogueManager.GetLocalizedText(fieldName);
	}

	public virtual bool IsSelectedQuest(QuestInfo questInfo)
	{
		return string.Equals(questInfo.Title, selectedQuest);
	}

	public void ClickClose(object data)
	{
		Close();
	}

	public virtual void ClickShowActiveQuests(object data)
	{
		ShowQuests(QuestState.Active | QuestState.ReturnToNPC);
	}

	public virtual void ClickShowCompletedQuests(object data)
	{
		ShowQuests(QuestState.Success | QuestState.Failure);
	}

	public virtual void ClickQuest(object data)
	{
		if (!IsString(data))
		{
			return;
		}
		string text = (string)data;
		selectedQuest = ((deselectQuestOnSecondClick && string.Equals(selectedQuest, text)) ? string.Empty : text);
		if (!string.IsNullOrEmpty(newQuestText) && !string.IsNullOrEmpty(selectedQuest))
		{
			QuestLog.MarkQuestViewed(selectedQuest);
			QuestInfo[] array = quests;
			foreach (QuestInfo questInfo in array)
			{
				if (IsSelectedQuest(questInfo))
				{
					QuestInfo questInfo2 = GetQuestInfo(questInfo.Group, questInfo.Title);
					questInfo.Heading = questInfo2.Heading;
					break;
				}
			}
		}
		OnQuestListUpdated();
	}

	public virtual void ClickAbandonQuest(object data)
	{
		if (!string.IsNullOrEmpty(selectedQuest))
		{
			ConfirmAbandonQuest(selectedQuest, OnConfirmAbandonQuest);
		}
	}

	protected virtual void OnConfirmAbandonQuest()
	{
		QuestLog.SetQuestState(selectedQuest, abandonQuestState);
		selectedQuest = string.Empty;
		ShowQuests(currentQuestStateMask);
		DialogueManager.instance.BroadcastMessage("OnQuestTrackingDisabled", selectedQuest, SendMessageOptions.DontRequireReceiver);
		string questAbandonSequence = QuestLog.GetQuestAbandonSequence(selectedQuest);
		if (!string.IsNullOrEmpty(questAbandonSequence))
		{
			DialogueManager.PlaySequence(questAbandonSequence);
		}
	}

	public virtual void ClickTrackQuest(object data)
	{
		if (!string.IsNullOrEmpty(selectedQuest))
		{
			bool value = !QuestLog.IsQuestTrackingEnabled(selectedQuest);
			QuestLog.SetQuestTracking(selectedQuest, value);
		}
	}

	private bool IsString(object data)
	{
		if (data != null)
		{
			return data.GetType() == typeof(string);
		}
		return false;
	}

	public virtual void ClickShowActiveQuestsButton()
	{
		ClickShowActiveQuests(null);
	}

	public void ClickShowCompletedQuestsButton()
	{
		ClickShowCompletedQuests(null);
	}

	public void ClickCloseButton()
	{
		ClickClose(null);
	}

	public void ClickAbandonQuestButton()
	{
		ClickAbandonQuest(null);
	}

	public void ClickTrackQuestButton()
	{
		ClickTrackQuest(null);
	}

	public void UpdateTracker()
	{
		if (isOpen && refreshCoroutine == null)
		{
			refreshCoroutine = StartCoroutine(UpdateQuestDisplayAtEndOfFrame());
		}
	}

	protected IEnumerator UpdateQuestDisplayAtEndOfFrame()
	{
		yield return CoroutineUtility.endOfFrame;
		refreshCoroutine = null;
		ShowQuests(currentQuestStateMask);
	}
}
