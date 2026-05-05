using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class UnityUIQuestLogWindow : QuestLogWindow
{
	[Serializable]
	public class AnimationTransitions
	{
		public string showTrigger = "Show";

		public string hideTrigger = "Hide";

		[Tooltip("Specifies whether Show Trigger and Hide Trigger are animator states or trigger parameters.")]
		public UIShowHideController.TransitionMode transitionMode;

		public bool debug;
	}

	public Graphic mainPanel;

	public Button activeQuestsButton;

	public Button completedQuestsButton;

	public Graphic questTable;

	public UnityUIQuestGroupTemplate questGroupTemplate;

	public UnityUIQuestTemplate questTemplate;

	public Graphic abandonPopup;

	public Text abandonQuestTitle;

	public AnimationTransitions animationTransitions = new AnimationTransitions();

	[Tooltip("Always keep a control focused; useful for gamepads and keyboard.")]
	public bool autoFocus;

	[Tooltip("If auto focusing, check on this frequency in seconds that the control is focused.")]
	public float autoFocusCheckFrequency = 0.5f;

	public UnityEvent onOpen = new UnityEvent();

	public UnityEvent onClose = new UnityEvent();

	public UnityEvent onContentChanged = new UnityEvent();

	[Tooltip("Add an EventSystem if one isn't in the scene.")]
	public bool addEventSystemIfNeeded = true;

	protected Action confirmAbandonQuestHandler;

	private UIShowHideController m_showHideController;

	protected List<string> collapsedGroups = new List<string>();

	protected List<UnityUIQuestGroupTemplate> groupTemplateInstances = new List<UnityUIQuestGroupTemplate>();

	protected List<UnityUIQuestTemplate> questTemplateInstances = new List<UnityUIQuestTemplate>();

	protected List<UnityUIQuestGroupTemplate> unusedGroupTemplateInstances = new List<UnityUIQuestGroupTemplate>();

	protected List<UnityUIQuestTemplate> unusedQuestTemplateInstances = new List<UnityUIQuestTemplate>();

	protected int siblingIndexCounter;

	private float nextAutoFocusCheckTime;

	protected UIShowHideController showHideController
	{
		get
		{
			if (m_showHideController == null)
			{
				m_showHideController = new UIShowHideController(base.gameObject, (Component)(object)mainPanel, animationTransitions.transitionMode, animationTransitions.debug);
			}
			return m_showHideController;
		}
	}

	public override void Awake()
	{
		base.Awake();
		Tools.DeprecationWarning(this);
	}

	protected override void Start()
	{
		base.Start();
		if (addEventSystemIfNeeded)
		{
			UITools.RequireEventSystem();
		}
		Tools.SetGameObjectActive((Component)(object)mainPanel, value: false);
		Tools.SetGameObjectActive((Component)(object)abandonPopup, value: false);
		Tools.SetGameObjectActive(questGroupTemplate, value: false);
		Tools.SetGameObjectActive(questTemplate, value: false);
		showHideController.state = UIShowHideController.State.Hidden;
		SetStateButtonListeners();
		SetStateToggleButtons();
		if (DialogueDebug.logWarnings)
		{
			if ((UnityEngine.Object)(object)mainPanel == null)
			{
				Debug.LogWarning(string.Format("{0}: {1} Main Panel is unassigned", new object[2] { "Dialogue System", base.name }));
			}
			if ((UnityEngine.Object)(object)questTable == null)
			{
				Debug.LogWarning(string.Format("{0}: {1} Quest Table is unassigned", new object[2] { "Dialogue System", base.name }));
			}
			if (useGroups && (questTemplate == null || !questTemplate.ArePropertiesAssigned))
			{
				Debug.LogWarning(string.Format("{0}: {1} Quest Group Template or one of its properties is unassigned", new object[2] { "Dialogue System", base.name }));
			}
			if (questTemplate == null || !questTemplate.ArePropertiesAssigned)
			{
				Debug.LogWarning(string.Format("{0}: {1} Quest Template or one of its properties is unassigned", new object[2] { "Dialogue System", base.name }));
			}
		}
	}

	public virtual void Update()
	{
		if (autoFocus && base.isOpen && (UnityEngine.Object)(object)EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && autoFocusCheckFrequency > 0.001f && Time.realtimeSinceStartup > nextAutoFocusCheckTime)
		{
			nextAutoFocusCheckTime = Time.realtimeSinceStartup + autoFocusCheckFrequency;
			AutoFocus();
		}
	}

	public override void OpenWindow(Action openedWindowHandler)
	{
		showHideController.Show(animationTransitions.showTrigger, pauseWhileOpen, openedWindowHandler, wait: false);
		base.isOpen = true;
		AutoFocus();
		onOpen.Invoke();
	}

	public void AutoFocus()
	{
		UITools.Select((((Component)(object)completedQuestsButton).gameObject.activeSelf ? ((Component)(object)completedQuestsButton).gameObject : ((Component)(object)activeQuestsButton).gameObject).GetComponent<Selectable>());
	}

	public override void CloseWindow(Action closedWindowHandler)
	{
		ResumeGameplay();
		showHideController.Hide(animationTransitions.hideTrigger, closedWindowHandler);
		base.isOpen = false;
		onClose.Invoke();
	}

	public override void OnQuestListUpdated()
	{
		unusedGroupTemplateInstances.AddRange(groupTemplateInstances);
		unusedQuestTemplateInstances.AddRange(questTemplateInstances);
		groupTemplateInstances.Clear();
		questTemplateInstances.Clear();
		siblingIndexCounter = 0;
		if (base.quests.Length == 0)
		{
			AddQuestToTable(new QuestInfo(string.Empty, new FormattedText(base.noQuestsMessage), FormattedText.empty, new FormattedText[0], new QuestState[0], trackable: false, track: false, abandonable: false));
		}
		else
		{
			AddQuestsToTable();
		}
		for (int i = 0; i < unusedGroupTemplateInstances.Count; i++)
		{
			UnityEngine.Object.Destroy(unusedGroupTemplateInstances[i].gameObject);
		}
		unusedGroupTemplateInstances.Clear();
		for (int j = 0; j < unusedQuestTemplateInstances.Count; j++)
		{
			UnityEngine.Object.Destroy(unusedQuestTemplateInstances[j].gameObject);
		}
		unusedQuestTemplateInstances.Clear();
		SetStateToggleButtons();
		if ((UnityEngine.Object)(object)mainPanel != null)
		{
			LayoutRebuilder.MarkLayoutForRebuild(mainPanel.rectTransform);
		}
	}

	protected void SetStateButtonListeners()
	{
		if ((UnityEngine.Object)(object)activeQuestsButton != null)
		{
			((UnityEvent)(object)activeQuestsButton.onClick).RemoveListener((UnityAction)delegate
			{
				ClickShowActiveQuestsButton();
			});
			((UnityEvent)(object)activeQuestsButton.onClick).AddListener((UnityAction)delegate
			{
				ClickShowActiveQuestsButton();
			});
		}
		if ((UnityEngine.Object)(object)completedQuestsButton != null)
		{
			((UnityEvent)(object)completedQuestsButton.onClick).RemoveListener((UnityAction)delegate
			{
				ClickShowCompletedQuestsButton();
			});
			((UnityEvent)(object)completedQuestsButton.onClick).AddListener((UnityAction)delegate
			{
				ClickShowCompletedQuestsButton();
			});
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

	protected virtual void ClearQuestTable()
	{
		if ((UnityEngine.Object)(object)questTable == null)
		{
			return;
		}
		foreach (Transform item in ((Component)(object)questTable).transform)
		{
			if (item.gameObject.activeSelf)
			{
				UnityEngine.Object.Destroy(item.gameObject);
			}
		}
		NotifyContentChanged();
	}

	protected virtual void AddQuestsToTable()
	{
		if ((UnityEngine.Object)(object)questTable == null)
		{
			return;
		}
		string text = null;
		bool flag = false;
		for (int i = 0; i < base.quests.Length; i++)
		{
			if (!string.Equals(base.quests[i].Group, text))
			{
				text = base.quests[i].Group;
				AddQuestGroupToTable(text);
				flag = collapsedGroups.Contains(text);
			}
			if (!flag)
			{
				AddQuestToTable(base.quests[i]);
			}
		}
		NotifyContentChanged();
	}

	protected virtual void AddQuestGroupToTable(string group)
	{
		if (string.IsNullOrEmpty(group) || questGroupTemplate == null || !questGroupTemplate.ArePropertiesAssigned)
		{
			return;
		}
		UnityUIQuestGroupTemplate unityUIQuestGroupTemplate = unusedGroupTemplateInstances.Find((UnityUIQuestGroupTemplate x) => string.Equals(x.heading.text, group));
		if (unityUIQuestGroupTemplate != null)
		{
			unusedGroupTemplateInstances.Remove(unityUIQuestGroupTemplate);
			groupTemplateInstances.Add(unityUIQuestGroupTemplate);
			unityUIQuestGroupTemplate.transform.SetSiblingIndex(siblingIndexCounter++);
			return;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(questGroupTemplate.gameObject);
		if (gameObject == null)
		{
			Debug.LogError(string.Format("{0}: {1} couldn't instantiate quest group template", new object[2] { "Dialogue System", base.name }));
			return;
		}
		gameObject.name = group;
		gameObject.transform.SetParent(((Component)(object)questTable).transform, worldPositionStays: false);
		gameObject.SetActive(value: true);
		UnityUIQuestGroupTemplate component = gameObject.GetComponent<UnityUIQuestGroupTemplate>();
		if (component == null)
		{
			return;
		}
		groupTemplateInstances.Add(component);
		component.transform.SetSiblingIndex(siblingIndexCounter++);
		component.Initialize();
		component.heading.text = group;
		Button componentInChildren = gameObject.GetComponentInChildren<Button>();
		if ((UnityEngine.Object)(object)componentInChildren != null)
		{
			((UnityEvent)(object)componentInChildren.onClick).AddListener((UnityAction)delegate
			{
				ClickQuestGroupFoldout(group);
			});
		}
	}

	protected virtual void AddQuestToTable(QuestInfo questInfo)
	{
		if ((UnityEngine.Object)(object)questTable == null || questTemplate == null || !questTemplate.ArePropertiesAssigned)
		{
			return;
		}
		UnityUIQuestTemplate unityUIQuestTemplate = unusedQuestTemplateInstances.Find((UnityUIQuestTemplate x) => (UnityEngine.Object)(object)((Component)(object)x.heading).GetComponentInChildren<Text>() != null && string.Equals(((Component)(object)x.heading).GetComponentInChildren<Text>().text, questInfo.Heading.text));
		if (unityUIQuestTemplate != null)
		{
			unusedQuestTemplateInstances.Remove(unityUIQuestTemplate);
			questTemplateInstances.Add(unityUIQuestTemplate);
			unityUIQuestTemplate.transform.SetSiblingIndex(siblingIndexCounter++);
			unityUIQuestTemplate.description.text = questInfo.Description.text;
			unityUIQuestTemplate.ClearQuestDetails();
			SetQuestDetails(unityUIQuestTemplate, questInfo);
			return;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(questTemplate.gameObject);
		if (gameObject == null)
		{
			Debug.LogError(string.Format("{0}: {1} couldn't instantiate quest template", new object[2] { "Dialogue System", base.name }));
			return;
		}
		gameObject.name = questInfo.Heading.text;
		gameObject.transform.SetParent(((Component)(object)questTable).transform, worldPositionStays: false);
		gameObject.transform.SetSiblingIndex(siblingIndexCounter++);
		gameObject.SetActive(value: true);
		UnityUIQuestTemplate component = gameObject.GetComponent<UnityUIQuestTemplate>();
		if (!(component == null))
		{
			questTemplateInstances.Add(component);
			component.Initialize();
			Button heading = component.heading;
			Text componentInChildren = ((Component)(object)heading).GetComponentInChildren<Text>();
			if ((UnityEngine.Object)(object)componentInChildren != null)
			{
				componentInChildren.text = questInfo.Heading.text;
			}
			((UnityEvent)(object)heading.onClick).AddListener((UnityAction)delegate
			{
				ClickQuestFoldout(questInfo.Title);
			});
			SetQuestDetails(component, questInfo);
		}
	}

	protected virtual void SetQuestDetails(UnityUIQuestTemplate questControl, QuestInfo questInfo)
	{
		if (IsSelectedQuest(questInfo))
		{
			if (questHeadingSource == QuestHeadingSource.Name)
			{
				questControl.description.text = questInfo.Description.text;
				Tools.SetGameObjectActive((Component)(object)questControl.description, value: true);
			}
			if (questInfo.EntryStates.Length != 0)
			{
				for (int i = 0; i < questInfo.Entries.Length; i++)
				{
					questControl.AddEntryDescription(questInfo.Entries[i].text, questInfo.EntryStates[i]);
				}
			}
			if ((UnityEngine.Object)(object)questControl.trackButton != null)
			{
				((Component)(object)questControl.trackButton).gameObject.AddComponent<UnityUIQuestTitle>().questTitle = questInfo.Title;
				((UnityEventBase)(object)questControl.trackButton.onClick).RemoveAllListeners();
				((UnityEvent)(object)questControl.trackButton.onClick).AddListener((UnityAction)delegate
				{
					ClickTrackQuestButton();
				});
				Tools.SetGameObjectActive((Component)(object)questControl.trackButton, questInfo.Trackable);
			}
			if ((UnityEngine.Object)(object)questControl.abandonButton != null)
			{
				((Component)(object)questControl.abandonButton).gameObject.AddComponent<UnityUIQuestTitle>().questTitle = questInfo.Title;
				((UnityEventBase)(object)questControl.abandonButton.onClick).RemoveAllListeners();
				((UnityEvent)(object)questControl.abandonButton.onClick).AddListener((UnityAction)delegate
				{
					ClickAbandonQuestButton();
				});
				Tools.SetGameObjectActive((Component)(object)questControl.abandonButton, questInfo.Abandonable);
			}
		}
		else
		{
			Tools.SetGameObjectActive((Component)(object)questControl.description, value: false);
			Tools.SetGameObjectActive((Component)(object)questControl.entryDescription, value: false);
			Tools.SetGameObjectActive((Component)(object)questControl.trackButton, value: false);
			Tools.SetGameObjectActive((Component)(object)questControl.abandonButton, value: false);
		}
	}

	public void NotifyContentChanged()
	{
		onContentChanged.Invoke();
	}

	public void ClickQuestFoldout(string questTitle)
	{
		ClickQuest(questTitle);
	}

	public void ClickQuestGroupFoldout(string group)
	{
		if (collapsedGroups.Contains(group))
		{
			collapsedGroups.Remove(group);
		}
		else
		{
			collapsedGroups.Add(group);
		}
		OnQuestListUpdated();
	}

	protected void OnTrackButtonClicked(GameObject button)
	{
		base.selectedQuest = button.GetComponent<UnityUIQuestTitle>().questTitle;
		ClickTrackQuest(base.selectedQuest);
	}

	protected void OnAbandonButtonClicked(GameObject button)
	{
		base.selectedQuest = button.GetComponent<UnityUIQuestTitle>().questTitle;
		ClickAbandonQuest(base.selectedQuest);
	}

	public override void ConfirmAbandonQuest(string title, Action confirmAbandonQuestHandler)
	{
		this.confirmAbandonQuestHandler = confirmAbandonQuestHandler;
		OpenAbandonPopup(title);
	}

	protected void OpenAbandonPopup(string title)
	{
		if (!((UnityEngine.Object)(object)abandonPopup != null))
		{
			return;
		}
		Tools.SetGameObjectActive((Component)(object)abandonPopup, value: true);
		if ((UnityEngine.Object)(object)abandonQuestTitle != null)
		{
			abandonQuestTitle.text = QuestLog.GetQuestTitle(title);
		}
		if (autoFocus && (UnityEngine.Object)(object)EventSystem.current != null)
		{
			Button componentInChildren = ((Component)(object)abandonPopup).GetComponentInChildren<Button>();
			if ((UnityEngine.Object)(object)componentInChildren != null)
			{
				EventSystem.current.SetSelectedGameObject(((Component)(object)componentInChildren).gameObject);
			}
		}
		else
		{
			confirmAbandonQuestHandler();
		}
	}

	protected void CloseAbandonPopup()
	{
		Tools.SetGameObjectActive((Component)(object)abandonPopup, value: false);
	}

	public void ClickConfirmAbandonQuestButton()
	{
		CloseAbandonPopup();
		confirmAbandonQuestHandler();
	}

	public void ClickCancelAbandonQuestButton()
	{
		CloseAbandonPopup();
	}
}
