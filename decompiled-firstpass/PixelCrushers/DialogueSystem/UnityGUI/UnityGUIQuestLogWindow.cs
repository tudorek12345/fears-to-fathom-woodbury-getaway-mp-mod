using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[AddComponentMenu("")]
public class UnityGUIQuestLogWindow : QuestLogWindow
{
	[Serializable]
	public class AbandonControls
	{
		public GUIControl panel;

		public GUILabel questTitleLabel;

		public GUIButton ok;

		public GUIButton cancel;
	}

	public GUIRoot guiRoot;

	public GUIScrollView scrollView;

	public GUIButton activeButton;

	public GUIButton completedButton;

	public AbandonControls abandonQuestPopup = new AbandonControls();

	public string groupHeadingGuiStyleName;

	public string questHeadingGuiStyleName;

	public string questHeadingOpenGuiStyleName;

	public string questBodyGuiStyleName;

	public string questEntryActiveGuiStyleName;

	public string questEntrySuccessGuiStyleName;

	public string questEntryFailureGuiStyleName;

	public string questEntryButtonStyleName;

	public string noQuestsGuiStyleName;

	public int padding = 2;

	private GUIStyle groupHeadingStyle;

	private GUIStyle questHeadingStyle;

	private GUIStyle questHeadingOpenStyle;

	private GUIStyle questBodyStyle;

	private GUIStyle questEntryActiveStyle;

	private GUIStyle questEntrySuccessStyle;

	private GUIStyle questEntryFailureStyle;

	private GUIStyle questButtonStyle;

	private Action confirmAbandonQuestHandler;

	private List<string> collapsedGroups = new List<string>();

	public override void Awake()
	{
		base.Awake();
		if (guiRoot == null)
		{
			guiRoot = GetComponentInChildren<GUIRoot>();
		}
		if (scrollView == null)
		{
			scrollView = GetComponentInChildren<GUIScrollView>();
		}
		if (scrollView != null)
		{
			scrollView.MeasureContentHandler += OnMeasureContent;
			scrollView.DrawContentHandler += OnDrawContent;
		}
		if (string.IsNullOrEmpty(groupHeadingGuiStyleName))
		{
			groupHeadingGuiStyleName = questHeadingGuiStyleName;
		}
	}

	protected override void Start()
	{
		base.Start();
		if (guiRoot != null)
		{
			guiRoot.gameObject.SetActive(value: false);
		}
	}

	public override void OpenWindow(Action openedWindowHandler)
	{
		if (guiRoot != null)
		{
			guiRoot.gameObject.SetActive(value: true);
			if (abandonQuestPopup != null && abandonQuestPopup.panel != null)
			{
				abandonQuestPopup.panel.gameObject.SetActive(value: false);
			}
			guiRoot.ManualRefresh();
		}
		openedWindowHandler();
	}

	public override void CloseWindow(Action closedWindowHandler)
	{
		if (guiRoot != null)
		{
			guiRoot.gameObject.SetActive(value: false);
		}
		closedWindowHandler();
	}

	public override void ConfirmAbandonQuest(string title, Action confirmAbandonQuestHandler)
	{
		this.confirmAbandonQuestHandler = confirmAbandonQuestHandler;
		OpenAbandonQuestPopup(title);
	}

	private void OpenAbandonQuestPopup(string title)
	{
		if (abandonQuestPopup.panel == null)
		{
			if (confirmAbandonQuestHandler != null)
			{
				confirmAbandonQuestHandler();
			}
			return;
		}
		if (abandonQuestPopup.questTitleLabel != null)
		{
			abandonQuestPopup.questTitleLabel.text = title;
		}
		abandonQuestPopup.panel.gameObject.SetActive(value: true);
	}

	private void CloseAbandonQuestPopup()
	{
		if (abandonQuestPopup.panel != null)
		{
			abandonQuestPopup.panel.gameObject.SetActive(value: false);
		}
	}

	public void ClickConfirmAbandonQuest(object data)
	{
		CloseAbandonQuestPopup();
		if (confirmAbandonQuestHandler != null)
		{
			confirmAbandonQuestHandler();
		}
	}

	public void ClickCancelAbandonQuest(object data)
	{
		CloseAbandonQuestPopup();
	}

	public void OnMeasureContent()
	{
		MeasureQuestContent();
	}

	public void OnDrawContent()
	{
		DrawQuests();
	}

	private void MeasureQuestContent()
	{
		float num = padding;
		string text = null;
		bool flag = false;
		QuestInfo[] array = base.quests;
		foreach (QuestInfo questInfo in array)
		{
			if (!string.Equals(questInfo.Group, text))
			{
				text = questInfo.Group;
				if (!string.IsNullOrEmpty(text))
				{
					num += GroupHeadingHeight(text);
					flag = collapsedGroups.Contains(text);
				}
			}
			if (!flag)
			{
				num += QuestHeadingHeight(questInfo);
				if (IsSelectedQuest(questInfo))
				{
					num += QuestDescriptionHeight(questInfo);
					num += QuestEntriesHeight(questInfo);
					num += QuestButtonsHeight(questInfo);
				}
			}
		}
		num += (float)padding;
		if (scrollView != null)
		{
			scrollView.contentHeight = num;
		}
	}

	public override void OnQuestListUpdated()
	{
		if (activeButton != null)
		{
			activeButton.clickable = !isShowingActiveQuests;
		}
		if (completedButton != null)
		{
			completedButton.clickable = isShowingActiveQuests;
		}
	}

	private GUIStyle UseGUIStyle(GUIStyle guiStyle, string guiStyleName, GUIStyle defaultStyle)
	{
		if (guiStyle == null)
		{
			return UnityGUITools.GetGUIStyle(guiStyleName, defaultStyle);
		}
		return guiStyle;
	}

	private GUIStyle GetGroupHeadingStyle()
	{
		return UseGUIStyle(groupHeadingStyle, groupHeadingGuiStyleName, GUI.skin.button);
	}

	private GUIStyle GetQuestHeadingStyle(bool isSelectedQuest)
	{
		if (isSelectedQuest && !string.IsNullOrEmpty(questHeadingOpenGuiStyleName))
		{
			questHeadingOpenStyle = UseGUIStyle(questHeadingOpenStyle, questHeadingOpenGuiStyleName, GUI.skin.button);
			return questHeadingOpenStyle;
		}
		questHeadingStyle = UseGUIStyle(questHeadingStyle, questHeadingGuiStyleName, GUI.skin.button);
		return questHeadingStyle;
	}

	private GUIStyle GetQuestEntryStyle(QuestState entryState)
	{
		questEntryActiveStyle = UseGUIStyle(questEntryActiveStyle, questEntryActiveGuiStyleName, GUI.skin.label);
		questEntrySuccessStyle = UseGUIStyle(questEntrySuccessStyle, questEntrySuccessGuiStyleName, GUI.skin.label);
		questEntryFailureStyle = UseGUIStyle(questEntryFailureStyle, questEntryFailureGuiStyleName, GUI.skin.label);
		return entryState switch
		{
			QuestState.Success => questEntrySuccessStyle, 
			QuestState.Failure => questEntryFailureStyle, 
			_ => questEntryActiveStyle, 
		};
	}

	private float GroupHeadingHeight(string group)
	{
		return GetGroupHeadingStyle().CalcHeight(new GUIContent(group), scrollView.contentWidth - (float)(2 * padding));
	}

	private float QuestHeadingHeight(QuestInfo questInfo)
	{
		return Mathf.Max(activeButton.rect.height, GetQuestHeadingStyle(IsSelectedQuest(questInfo)).CalcHeight(new GUIContent(questInfo.Heading.text), scrollView.contentWidth - (float)(2 * padding)));
	}

	private float QuestDescriptionHeight(QuestInfo questInfo)
	{
		questBodyStyle = UseGUIStyle(questBodyStyle, questBodyGuiStyleName, GUI.skin.label);
		if (questHeadingSource == QuestHeadingSource.Name)
		{
			return questBodyStyle.CalcHeight(new GUIContent(questInfo.Description.text), scrollView.contentWidth - (float)(2 * padding));
		}
		return 0f;
	}

	private float QuestEntriesHeight(QuestInfo questInfo)
	{
		float num = 0f;
		for (int i = 0; i < questInfo.Entries.Length; i++)
		{
			QuestState questState = questInfo.EntryStates[i];
			GUIStyle questEntryStyle = GetQuestEntryStyle(questState);
			if (questState != QuestState.Unassigned)
			{
				string text = questInfo.Entries[i].text;
				num += questEntryStyle.CalcHeight(new GUIContent(text), scrollView.contentWidth - (float)(2 * padding));
			}
		}
		return num;
	}

	private float QuestButtonsHeight(QuestInfo questInfo)
	{
		if (questInfo.Trackable || questInfo.Abandonable)
		{
			questButtonStyle = UseGUIStyle(questButtonStyle, questEntryButtonStyleName, GUI.skin.button);
			questButtonStyle.wordWrap = false;
			return questButtonStyle.CalcHeight(new GUIContent("Abandon"), scrollView.contentWidth - (float)(2 * padding));
		}
		return 0f;
	}

	private void DrawQuests()
	{
		if (!(scrollView != null))
		{
			return;
		}
		float num = padding;
		string text = null;
		bool flag = false;
		QuestInfo[] array = base.quests;
		foreach (QuestInfo questInfo in array)
		{
			if (!string.Equals(questInfo.Group, text))
			{
				text = questInfo.Group;
				if (!string.IsNullOrEmpty(text))
				{
					flag = collapsedGroups.Contains(text);
					float num2 = GroupHeadingHeight(text);
					if (GUI.Button(new Rect(padding, num, scrollView.contentWidth - (float)(2 * padding), num2), text, GetGroupHeadingStyle()))
					{
						ClickQuestGroup(text);
					}
					num += num2;
				}
			}
			if (!flag)
			{
				bool flag2 = IsSelectedQuest(questInfo);
				float num3 = QuestHeadingHeight(questInfo);
				if (GUI.Button(new Rect(padding, num, scrollView.contentWidth - (float)(2 * padding), num3), questInfo.Heading.text, GetQuestHeadingStyle(flag2)))
				{
					ClickQuest(questInfo.Title);
				}
				num += num3;
				if (flag2)
				{
					num = DrawQuestDescription(questInfo, num);
					num = DrawQuestEntries(questInfo, num);
					num = DrawQuestButtons(questInfo, num);
				}
			}
		}
		if (base.quests.Length == 0)
		{
			GUIStyle gUIStyle = UnityGUITools.GetGUIStyle(noQuestsGuiStyleName, GUI.skin.label);
			float num4 = gUIStyle.CalcHeight(new GUIContent(base.noQuestsMessage), scrollView.contentWidth - 4f);
			GUI.Label(new Rect(2f, num, scrollView.contentWidth, num4), base.noQuestsMessage, gUIStyle);
			num += num4;
		}
	}

	private float DrawQuestDescription(QuestInfo questInfo, float contentY)
	{
		if (questHeadingSource == QuestHeadingSource.Name)
		{
			questBodyStyle = UseGUIStyle(questBodyStyle, questBodyGuiStyleName, GUI.skin.label);
			float num = questBodyStyle.CalcHeight(new GUIContent(questInfo.Description.text), scrollView.contentWidth - (float)(2 * padding));
			GUI.Label(new Rect(padding, contentY, scrollView.contentWidth, num), questInfo.Description.text, questBodyStyle);
			return contentY + num;
		}
		return contentY;
	}

	private float DrawQuestEntries(QuestInfo questInfo, float contentY)
	{
		float num = contentY;
		for (int i = 0; i < questInfo.Entries.Length; i++)
		{
			QuestState questState = questInfo.EntryStates[i];
			if (questState != QuestState.Unassigned)
			{
				string text = questInfo.Entries[i].text;
				GUIStyle questEntryStyle = GetQuestEntryStyle(questState);
				float num2 = questEntryStyle.CalcHeight(new GUIContent(text), scrollView.contentWidth - (float)(2 * padding));
				GUI.Label(new Rect(padding, num, scrollView.contentWidth, num2), text, questEntryStyle);
				num += num2;
			}
		}
		return num;
	}

	private float DrawQuestButtons(QuestInfo questInfo, float contentY)
	{
		float num = contentY;
		if (currentQuestStateMask == QuestState.Active && (questInfo.Trackable || questInfo.Abandonable))
		{
			questButtonStyle = UseGUIStyle(questButtonStyle, questEntryButtonStyleName, GUI.skin.button);
			questButtonStyle.wordWrap = false;
			string localizedText = GetLocalizedText("Track");
			Vector2 vector = questButtonStyle.CalcSize(new GUIContent(localizedText));
			float y = vector.y;
			float x = vector.x;
			string localizedText2 = GetLocalizedText("Abandon");
			float x2 = questButtonStyle.CalcSize(new GUIContent(localizedText2)).x;
			float num2 = scrollView.contentWidth - (float)(2 * padding);
			float num3 = num2 - x2;
			float num4 = (questInfo.Abandonable ? (num3 - (float)padding) : num2);
			num4 -= x;
			if (questInfo.Trackable && GUI.Button(new Rect(num4, num, x, y), localizedText))
			{
				ClickTrackQuest(questInfo.Title);
			}
			if (questInfo.Abandonable && GUI.Button(new Rect(num3, num, x2, y), localizedText2))
			{
				ClickAbandonQuest(questInfo.Title);
			}
			num += questButtonStyle.CalcHeight(new GUIContent("Abandon"), scrollView.contentWidth - (float)(2 * padding));
		}
		return num;
	}

	private void ClickQuestGroup(string group)
	{
		if (collapsedGroups.Contains(group))
		{
			collapsedGroups.Remove(group);
		}
		else
		{
			collapsedGroups.Add(group);
		}
	}
}
