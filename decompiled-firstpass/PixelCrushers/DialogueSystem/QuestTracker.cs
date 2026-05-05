using System.Collections;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem.UnityGUI;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class QuestTracker : MonoBehaviour
{
	public enum QuestDescriptionSource
	{
		Title,
		Description
	}

	private class QuestTrackerLine
	{
		public string guiStyleName;

		public GUIStyle guiStyle;

		public string text;
	}

	[Tooltip("Record the quest tracker display toggle in this PlayerPrefs key.")]
	public string playerPrefsToggleKey = "QuestTracker";

	public ScaledRect rect = new ScaledRect(ScaledRectAlignment.TopRight, ScaledRectAlignment.TopRight, ScaledValue.FromPixelValue(0f), ScaledValue.FromPixelValue(0f), ScaledValue.FromNormalizedValue(0.25f), ScaledValue.FromNormalizedValue(1f), 64f, 32f);

	public GUISkin guiSkin;

	public string TitleStyle;

	public string SuccessTitleStyle;

	public string FailureTitleStyle;

	public string ActiveEntryStyle;

	public string SuccessEntryStyle;

	public string FailureEntryStyle;

	public bool showActiveQuests = true;

	public bool showCompletedQuests;

	public bool showCompletedEntryText;

	public QuestDescriptionSource questDescriptionSource;

	private Rect screenRect;

	private List<QuestTrackerLine> lines = new List<QuestTrackerLine>();

	private bool isVisible = true;

	public void Start()
	{
		isVisible = PlayerPrefs.GetInt(playerPrefsToggleKey, 1) == 1;
		StartCoroutine(UpdateTrackerAfterOneFrame());
	}

	private IEnumerator UpdateTrackerAfterOneFrame()
	{
		yield return null;
		UpdateTracker();
	}

	public void ShowTracker()
	{
		isVisible = true;
		PlayerPrefs.SetInt(playerPrefsToggleKey, 1);
	}

	public void HideTracker()
	{
		isVisible = false;
		PlayerPrefs.SetInt(playerPrefsToggleKey, 0);
	}

	public void ToggleTracker()
	{
		if (isVisible)
		{
			HideTracker();
		}
		else
		{
			ShowTracker();
		}
	}

	public void OnQuestTrackingEnabled(string quest)
	{
		UpdateTracker();
	}

	public void OnQuestTrackingDisabled(string quest)
	{
		UpdateTracker();
	}

	public void OnConversationEnd(Transform actor)
	{
		UpdateTracker();
	}

	public void UpdateTracker()
	{
		screenRect = rect.GetPixelRect();
		lines.Clear();
		string[] allQuests = QuestLog.GetAllQuests((QuestState)((showActiveQuests ? 2 : 0) | (showCompletedQuests ? 12 : 0)));
		foreach (string text in allQuests)
		{
			if (QuestLog.IsQuestTrackingEnabled(text))
			{
				AddQuestTitle(text);
				AddQuestEntries(text);
			}
		}
	}

	private void AddQuestTitle(string quest)
	{
		QuestTrackerLine questTrackerLine = new QuestTrackerLine();
		string rawText = ((questDescriptionSource == QuestDescriptionSource.Title) ? QuestLog.GetQuestTitle(quest) : QuestLog.GetQuestDescription(quest));
		questTrackerLine.text = FormattedText.Parse(rawText, DialogueManager.masterDatabase.emphasisSettings).text;
		questTrackerLine.guiStyleName = GetTitleStyleName(QuestLog.GetQuestState(quest));
		questTrackerLine.guiStyle = null;
		lines.Add(questTrackerLine);
	}

	private void AddQuestEntries(string quest)
	{
		int questEntryCount = QuestLog.GetQuestEntryCount(quest);
		for (int i = 1; i <= questEntryCount; i++)
		{
			QuestState questEntryState = QuestLog.GetQuestEntryState(quest, i);
			if (questEntryState != QuestState.Unassigned && ((questEntryState != QuestState.Success && questEntryState != QuestState.Failure) || showCompletedEntryText))
			{
				QuestTrackerLine questTrackerLine = new QuestTrackerLine();
				string questEntryText = GetQuestEntryText(quest, i, questEntryState);
				questTrackerLine.text = FormattedText.Parse(questEntryText, DialogueManager.masterDatabase.emphasisSettings).text;
				questTrackerLine.guiStyleName = GetEntryStyleName(questEntryState);
				questTrackerLine.guiStyle = null;
				lines.Add(questTrackerLine);
			}
		}
	}

	private string GetQuestEntryText(string quest, int entryNum, QuestState entryState)
	{
		if (entryState == QuestState.Unassigned || entryState == QuestState.Abandoned)
		{
			return string.Empty;
		}
		if (entryState == QuestState.Success && showCompletedEntryText)
		{
			string asString = DialogueLua.GetQuestField(quest, "Entry " + entryNum + " Success").asString;
			if (!string.IsNullOrEmpty(asString))
			{
				return asString;
			}
		}
		else if (entryState == QuestState.Failure && showCompletedEntryText)
		{
			string asString2 = DialogueLua.GetQuestField(quest, "Entry " + entryNum + " Failure").asString;
			if (!string.IsNullOrEmpty(asString2))
			{
				return asString2;
			}
		}
		return QuestLog.GetQuestEntry(quest, entryNum);
	}

	private string GetTitleStyleName(QuestState state)
	{
		return state switch
		{
			QuestState.Active => TitleStyle, 
			QuestState.Success => SuccessTitleStyle, 
			QuestState.Failure => FailureTitleStyle, 
			_ => TitleStyle, 
		};
	}

	private string GetEntryStyleName(QuestState entryState)
	{
		return entryState switch
		{
			QuestState.Active => ActiveEntryStyle, 
			QuestState.Success => SuccessEntryStyle, 
			QuestState.Failure => FailureEntryStyle, 
			_ => ActiveEntryStyle, 
		};
	}

	private void OnGUI()
	{
		if (!isVisible)
		{
			return;
		}
		if (guiSkin != null)
		{
			GUI.skin = guiSkin;
		}
		GUILayout.BeginArea(screenRect);
		foreach (QuestTrackerLine line in lines)
		{
			if (line.guiStyle == null)
			{
				line.guiStyle = UnityGUITools.GetGUIStyle(line.guiStyleName, GUI.skin.label);
			}
			GUILayout.Label(line.text, line.guiStyle);
		}
		GUILayout.EndArea();
	}
}
