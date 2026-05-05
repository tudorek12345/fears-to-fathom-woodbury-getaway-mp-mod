using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class StandardUIQuestLogWindow : QuestLogWindow, IEventSystemUser
{
	[Header("Main Panel")]
	public UIPanel mainPanel;

	public UITextField showingActiveQuestsHeading;

	public UITextField showingCompletedQuestHeading;

	[Tooltip("Button to switch display to active quests.")]
	public Button activeQuestsButton;

	[Tooltip("Button to switch display to completed quests.")]
	public Button completedQuestsButton;

	[Header("Selection Panel")]
	public RectTransform questSelectionContentContainer;

	public StandardUIFoldoutTemplate questGroupTemplate;

	[Tooltip("Use this template for active quests.")]
	public StandardUIQuestTitleButtonTemplate activeQuestHeadingTemplate;

	[Tooltip("Use this template for the currently-selected active quest.")]
	public StandardUIQuestTitleButtonTemplate selectedActiveQuestHeadingTemplate;

	[Tooltip("Use this template for completed quests.")]
	public StandardUIQuestTitleButtonTemplate completedQuestHeadingTemplate;

	[Tooltip("Use this template for the currently-selected completed quest.")]
	public StandardUIQuestTitleButtonTemplate selectedCompletedQuestHeadingTemplate;

	[Tooltip("If there are no quests to show, show the No Active/Completed Quests Text above.")]
	public bool showNoQuestsText = true;

	[Tooltip("Select first quest in list when open. If unticked and Always Auto Focus is ticked, selects button assigned to main panel's First Selected field (Close button).")]
	public bool selectFirstQuestOnOpen;

	[Tooltip("Show details when quest button is selected (highlighted/hovered), not when clicked.")]
	public bool showDetailsOnSelect;

	[Tooltip("Keep all groups expanded.")]
	public bool keepGroupsExpanded;

	[Header("Details Panel")]
	public RectTransform questDetailsContentContainer;

	public StandardUITextTemplate questHeadingTextTemplate;

	public StandardUITextTemplate questDescriptionTextTemplate;

	public StandardUITextTemplate questEntryActiveTextTemplate;

	public StandardUITextTemplate questEntrySuccessTextTemplate;

	public StandardUITextTemplate questEntryFailureTextTemplate;

	public StandardUIButtonTemplate abandonButtonTemplate;

	[Header("Abandon Quest Panel")]
	public UIPanel abandonQuestPanel;

	public UITextField abandonQuestTitleText;

	[Header("Events")]
	public UnityEvent onOpen = new UnityEvent();

	public UnityEvent onClose = new UnityEvent();

	[Tooltip("Add an EventSystem if one isn't in the scene.")]
	public bool addEventSystemIfNeeded = true;

	private StandardUIInstancedContentManager m_selectionPanelContentManager = new StandardUIInstancedContentManager();

	private StandardUIInstancedContentManager m_detailsPanelContentManager = new StandardUIInstancedContentManager();

	private EventSystem m_eventSystem;

	protected List<string> expandedGroupNames = new List<string>();

	protected Action confirmAbandonQuestHandler;

	private Coroutine m_refreshCoroutine;

	private bool m_isAwake;

	public string foldoutToSelect;

	public string questTitleToSelect;

	protected StandardUIInstancedContentManager selectionPanelContentManager
	{
		get
		{
			return m_selectionPanelContentManager;
		}
		set
		{
			m_selectionPanelContentManager = value;
		}
	}

	protected StandardUIInstancedContentManager detailsPanelContentManager
	{
		get
		{
			return m_detailsPanelContentManager;
		}
		set
		{
			m_detailsPanelContentManager = value;
		}
	}

	public EventSystem eventSystem
	{
		get
		{
			if ((UnityEngine.Object)(object)m_eventSystem != null)
			{
				return m_eventSystem;
			}
			return EventSystem.current;
		}
		set
		{
			m_eventSystem = value;
		}
	}

	public override void Awake()
	{
		m_isAwake = true;
		base.Awake();
		if (addEventSystemIfNeeded)
		{
			UITools.RequireEventSystem();
		}
		InitializeTemplates();
	}

	protected virtual void InitializeTemplates()
	{
		if (DialogueDebug.logWarnings)
		{
			if (mainPanel == null)
			{
				Debug.LogWarning("Dialogue System: Main Panel is unassigned.", this);
			}
			if (questSelectionContentContainer == null)
			{
				Debug.LogWarning("Dialogue System: Quest Selection Content Container is unassigned.", this);
			}
			if (questGroupTemplate == null)
			{
				Debug.LogWarning("Dialogue System: Quest Group Template is unassigned.", this);
			}
			if (activeQuestHeadingTemplate == null)
			{
				Debug.LogWarning("Dialogue System: Active Quest Title Template is unassigned.", this);
			}
			if (completedQuestHeadingTemplate == null)
			{
				Debug.LogWarning("Dialogue System: Completed Quest Title Template is unassigned.", this);
			}
			if (questDetailsContentContainer == null)
			{
				Debug.LogWarning("Dialogue System: Quest Details Content Container is unassigned.", this);
			}
			if (questHeadingTextTemplate == null)
			{
				Debug.LogWarning("Dialogue System: Quest Heading Text Template is unassigned.", this);
			}
			if (questDescriptionTextTemplate == null)
			{
				Debug.LogWarning("Dialogue System: Quest Body Text Template is unassigned.", this);
			}
			if (abandonQuestPanel == null)
			{
				Debug.LogWarning("Dialogue System: Abandon Quest Panel is unassigned.", this);
			}
			if (abandonQuestTitleText == null)
			{
				Debug.LogWarning("Dialogue System: Abandon Quest Title Text is unassigned.", this);
			}
		}
		Tools.SetGameObjectActive(questGroupTemplate, value: false);
		Tools.SetGameObjectActive(activeQuestHeadingTemplate, value: false);
		Tools.SetGameObjectActive(completedQuestHeadingTemplate, value: false);
		Tools.SetGameObjectActive(selectedActiveQuestHeadingTemplate, value: false);
		Tools.SetGameObjectActive(selectedCompletedQuestHeadingTemplate, value: false);
		Tools.SetGameObjectActive(questHeadingTextTemplate, value: false);
		Tools.SetGameObjectActive(questDescriptionTextTemplate, value: false);
		Tools.SetGameObjectActive(questEntryActiveTextTemplate, value: false);
		Tools.SetGameObjectActive(questEntrySuccessTextTemplate, value: false);
		Tools.SetGameObjectActive(questEntryFailureTextTemplate, value: false);
		Tools.SetGameObjectActive(abandonButtonTemplate, value: false);
	}

	public override void OpenWindow(Action openedWindowHandler)
	{
		mainPanel.Open();
		openedWindowHandler();
		onOpen.Invoke();
		if (selectFirstQuestOnOpen && base.quests.Length != 0)
		{
			RepaintSelectedQuest(base.quests[0]);
		}
	}

	public override void CloseWindow(Action closedWindowHandler)
	{
		closedWindowHandler();
		mainPanel.Close();
		onClose.Invoke();
	}

	public virtual void Toggle()
	{
		if (base.isOpen)
		{
			Close();
		}
		else
		{
			Open();
		}
	}

	public virtual bool IsGroupExpanded(string groupName)
	{
		if (!keepGroupsExpanded)
		{
			return expandedGroupNames.Contains(groupName);
		}
		return true;
	}

	public virtual void ToggleGroup(string groupName)
	{
		if (IsGroupExpanded(groupName))
		{
			expandedGroupNames.Remove(groupName);
		}
		else
		{
			expandedGroupNames.Add(groupName);
		}
	}

	protected void SetStateToggleButtons()
	{
		if ((UnityEngine.Object)(object)activeQuestsButton != null)
		{
			((Selectable)activeQuestsButton).interactable = !isShowingActiveQuests;
		}
		if ((UnityEngine.Object)(object)completedQuestsButton != null)
		{
			((Selectable)completedQuestsButton).interactable = isShowingActiveQuests;
		}
	}

	public virtual void Repaint()
	{
		if (base.isOpen && m_refreshCoroutine == null)
		{
			m_refreshCoroutine = StartCoroutine(RefreshAtEndOfFrame());
		}
	}

	private IEnumerator RefreshAtEndOfFrame()
	{
		yield return CoroutineUtility.endOfFrame;
		m_refreshCoroutine = null;
		OnQuestListUpdated();
	}

	public override void OnQuestListUpdated()
	{
		if (!m_isAwake)
		{
			return;
		}
		Selectable val = null;
		showingActiveQuestsHeading.SetActive(isShowingActiveQuests);
		showingCompletedQuestHeading.SetActive(!isShowingActiveQuests);
		selectionPanelContentManager.Clear();
		List<string> list = new List<string>();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		int num = 0;
		bool flag = false;
		QuestInfo[] array;
		if (base.quests.Length != 0)
		{
			array = base.quests;
			foreach (QuestInfo questInfo in array)
			{
				if (IsSelectedQuest(questInfo))
				{
					RepaintSelectedQuest(questInfo);
					flag = true;
				}
				string text = questInfo.Group;
				string value = (string.IsNullOrEmpty(questInfo.GroupDisplayName) ? questInfo.Group : questInfo.GroupDisplayName);
				if (string.IsNullOrEmpty(text))
				{
					num++;
				}
				if (!string.IsNullOrEmpty(text) && !list.Contains(text))
				{
					list.Add(text);
					dictionary[text] = value;
				}
			}
		}
		if (!flag)
		{
			RepaintSelectedQuest(null);
		}
		foreach (string item in list)
		{
			StandardUIFoldoutTemplate standardUIFoldoutTemplate = selectionPanelContentManager.Instantiate(questGroupTemplate);
			selectionPanelContentManager.Add(standardUIFoldoutTemplate, questSelectionContentContainer);
			standardUIFoldoutTemplate.Assign(dictionary[item], IsGroupExpanded(item));
			string targetGroupName = item;
			StandardUIFoldoutTemplate targetGroupFoldout = standardUIFoldoutTemplate;
			if (!keepGroupsExpanded)
			{
				((UnityEvent)(object)standardUIFoldoutTemplate.foldoutButton.onClick).AddListener((UnityAction)delegate
				{
					OnClickGroup(targetGroupName, targetGroupFoldout);
				});
			}
			if (string.Equals(foldoutToSelect, item))
			{
				val = (Selectable)(object)standardUIFoldoutTemplate.foldoutButton;
				foldoutToSelect = null;
			}
			array = base.quests;
			foreach (QuestInfo questInfo2 in array)
			{
				if (string.Equals(questInfo2.Group, item))
				{
					StandardUIQuestTitleButtonTemplate template = (IsSelectedQuest(questInfo2) ? GetSelectedQuestTitleTemplate(questInfo2) : GetQuestTitleTemplate(questInfo2));
					StandardUIQuestTitleButtonTemplate standardUIQuestTitleButtonTemplate = selectionPanelContentManager.Instantiate(template);
					standardUIQuestTitleButtonTemplate.Assign(questInfo2.Title, questInfo2.Heading.text, OnToggleTracking);
					selectionPanelContentManager.Add(standardUIQuestTitleButtonTemplate, standardUIFoldoutTemplate.interiorPanel);
					string target = questInfo2.Title;
					((UnityEvent)(object)standardUIQuestTitleButtonTemplate.button.onClick).AddListener((UnityAction)delegate
					{
						OnClickQuest(target);
					});
					if (showDetailsOnSelect)
					{
						AddShowDetailsOnSelect(standardUIQuestTitleButtonTemplate.button, target);
					}
					if (string.Equals(questInfo2.Title, questTitleToSelect))
					{
						val = (Selectable)(object)standardUIQuestTitleButtonTemplate.button;
						questTitleToSelect = null;
					}
				}
			}
		}
		array = base.quests;
		foreach (QuestInfo questInfo3 in array)
		{
			if (string.IsNullOrEmpty(questInfo3.Group))
			{
				StandardUIQuestTitleButtonTemplate template2 = (IsSelectedQuest(questInfo3) ? GetSelectedQuestTitleTemplate(questInfo3) : GetQuestTitleTemplate(questInfo3));
				StandardUIQuestTitleButtonTemplate standardUIQuestTitleButtonTemplate2 = selectionPanelContentManager.Instantiate(template2);
				standardUIQuestTitleButtonTemplate2.Assign(questInfo3.Title, questInfo3.Heading.text, OnToggleTracking);
				selectionPanelContentManager.Add(standardUIQuestTitleButtonTemplate2, questSelectionContentContainer);
				string target2 = questInfo3.Title;
				((UnityEvent)(object)standardUIQuestTitleButtonTemplate2.button.onClick).AddListener((UnityAction)delegate
				{
					OnClickQuest(target2);
				});
				if (showDetailsOnSelect)
				{
					AddShowDetailsOnSelect(standardUIQuestTitleButtonTemplate2.button, target2);
				}
				if (string.Equals(questInfo3.Title, questTitleToSelect))
				{
					val = (Selectable)(object)standardUIQuestTitleButtonTemplate2.button;
					questTitleToSelect = null;
				}
			}
		}
		if (base.quests.Length == 0 && showNoQuestsText)
		{
			StandardUIQuestTitleButtonTemplate standardUIQuestTitleButtonTemplate3 = selectionPanelContentManager.Instantiate(completedQuestHeadingTemplate);
			string text2 = base.noQuestsMessage;
			standardUIQuestTitleButtonTemplate3.Assign(text2, text2, null);
			selectionPanelContentManager.Add(standardUIQuestTitleButtonTemplate3, questSelectionContentContainer);
		}
		SetStateToggleButtons();
		mainPanel.RefreshSelectablesList();
		if (mainPanel != null)
		{
			LayoutRebuilder.MarkLayoutForRebuild(mainPanel.GetComponent<RectTransform>());
		}
		if ((UnityEngine.Object)(object)val != null)
		{
			StartCoroutine(SelectElement(val));
		}
		else if (eventSystem.currentSelectedGameObject == null && mainPanel != null && mainPanel.firstSelected != null && InputDeviceManager.autoFocus)
		{
			UITools.Select(mainPanel.firstSelected.GetComponent<Selectable>(), allowStealFocus: true, eventSystem);
		}
	}

	protected virtual StandardUIQuestTitleButtonTemplate GetQuestTitleTemplate(QuestInfo quest)
	{
		if (!isShowingActiveQuests)
		{
			return completedQuestHeadingTemplate;
		}
		return activeQuestHeadingTemplate;
	}

	protected virtual StandardUIQuestTitleButtonTemplate GetSelectedQuestTitleTemplate(QuestInfo quest)
	{
		object obj;
		if (!isShowingActiveQuests)
		{
			obj = selectedCompletedQuestHeadingTemplate;
			if (obj == null)
			{
				return completedQuestHeadingTemplate;
			}
		}
		else
		{
			obj = selectedActiveQuestHeadingTemplate ?? activeQuestHeadingTemplate;
		}
		return (StandardUIQuestTitleButtonTemplate)obj;
	}

	protected IEnumerator SelectElement(Selectable elementToSelect)
	{
		yield return null;
		UITools.Select(elementToSelect, allowStealFocus: true, eventSystem);
	}

	protected virtual void AddShowDetailsOnSelect(Button button, string target)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Expected O, but got Unknown
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		EventTrigger obj = ((Component)(object)button).GetComponent<EventTrigger>() ?? ((Component)(object)button).gameObject.AddComponent<EventTrigger>();
		Entry val = new Entry();
		val.eventID = (EventTriggerType)9;
		((UnityEvent<BaseEventData>)(object)val.callback).AddListener((UnityAction<BaseEventData>)delegate
		{
			ShowDetailsOnSelect(target);
		});
		obj.triggers.Add(val);
		val = new Entry();
		val.eventID = (EventTriggerType)0;
		((UnityEvent<BaseEventData>)(object)val.callback).AddListener((UnityAction<BaseEventData>)delegate
		{
			ShowDetailsOnSelect(target);
		});
		obj.triggers.Add(val);
	}

	protected virtual void OnClickGroup(string groupName, StandardUIFoldoutTemplate groupFoldout)
	{
		ToggleGroup(groupName);
		groupFoldout.ToggleInterior();
	}

	protected virtual void ShowDetailsOnSelect(string questTitle)
	{
		if (!string.Equals(base.selectedQuest, questTitle))
		{
			SelectQuest(questTitle);
		}
	}

	protected virtual void OnClickQuest(string questTitle)
	{
		SelectQuest(questTitle);
	}

	public virtual void SelectQuest(string questTitle)
	{
		questTitleToSelect = questTitle;
		ClickQuest(questTitle);
	}

	protected virtual void RepaintSelectedQuest(QuestInfo quest)
	{
		detailsPanelContentManager.Clear();
		if (quest == null)
		{
			return;
		}
		StandardUITextTemplate standardUITextTemplate = detailsPanelContentManager.Instantiate(questHeadingTextTemplate);
		standardUITextTemplate.Assign(quest.Heading.text);
		detailsPanelContentManager.Add(standardUITextTemplate, questDetailsContentContainer);
		StandardUITextTemplate standardUITextTemplate2 = detailsPanelContentManager.Instantiate(questDescriptionTextTemplate);
		standardUITextTemplate2.Assign(quest.Description.text);
		detailsPanelContentManager.Add(standardUITextTemplate2, questDetailsContentContainer);
		for (int i = 0; i < quest.Entries.Length; i++)
		{
			StandardUITextTemplate entryTemplate = GetEntryTemplate(quest.EntryStates[i]);
			if (entryTemplate != null)
			{
				StandardUITextTemplate standardUITextTemplate3 = detailsPanelContentManager.Instantiate(entryTemplate);
				standardUITextTemplate3.Assign(quest.Entries[i].text);
				detailsPanelContentManager.Add(standardUITextTemplate3, questDetailsContentContainer);
			}
		}
		if (currentQuestStateMask == QuestState.Active && QuestLog.IsQuestAbandonable(quest.Title))
		{
			StandardUIButtonTemplate standardUIButtonTemplate = detailsPanelContentManager.Instantiate(abandonButtonTemplate);
			detailsPanelContentManager.Add(standardUIButtonTemplate, questDetailsContentContainer);
			((UnityEvent)(object)standardUIButtonTemplate.button.onClick).AddListener((UnityAction)base.ClickAbandonQuestButton);
		}
	}

	protected virtual StandardUITextTemplate GetEntryTemplate(QuestState state)
	{
		switch (state)
		{
		case QuestState.Active:
			return questEntryActiveTextTemplate;
		case QuestState.Success:
			if (!(questEntrySuccessTextTemplate != null))
			{
				return questEntryActiveTextTemplate;
			}
			return questEntrySuccessTextTemplate;
		case QuestState.Failure:
			if (!(questEntryFailureTextTemplate != null))
			{
				return questEntryActiveTextTemplate;
			}
			return questEntryFailureTextTemplate;
		default:
			return null;
		}
	}

	public virtual void OnToggleTracking(bool value, object data)
	{
		string text = (string)data;
		if (!string.IsNullOrEmpty(text))
		{
			string text2 = base.selectedQuest;
			base.selectedQuest = text;
			ClickTrackQuest(text);
			base.selectedQuest = text2;
		}
	}

	public override void ConfirmAbandonQuest(string title, Action confirmAbandonQuestHandler)
	{
		if (!(abandonQuestPanel == null) && base.selectedQuest != null)
		{
			this.confirmAbandonQuestHandler = confirmAbandonQuestHandler;
			abandonQuestTitleText.text = QuestLog.GetQuestTitle(base.selectedQuest);
			abandonQuestPanel.Open();
		}
	}

	public virtual void AbandonQuestConfirmed()
	{
		OnConfirmAbandonQuest();
		detailsPanelContentManager.Clear();
	}

	protected override void ShowQuests(QuestState questStateMask)
	{
		if (questStateMask != currentQuestStateMask)
		{
			detailsPanelContentManager.Clear();
		}
		base.ShowQuests(questStateMask);
	}
}
