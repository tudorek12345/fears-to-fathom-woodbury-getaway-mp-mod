using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class UnityUIQuestTracker : MonoBehaviour
{
	public enum QuestDescriptionSource
	{
		Title,
		Description
	}

	[Tooltip("Record the quest tracker display toggle in this PlayerPrefs key.")]
	public string playerPrefsToggleKey = "QuestTracker";

	[Tooltip("UI container that will hold instances of quest track template.")]
	public Transform container;

	[Tooltip("Show Container even if there's nothing to track.")]
	public bool showContainerIfEmpty = true;

	[Tooltip("Template to instantiate for each tracked quest.")]
	public UnityUIQuestTrackTemplate questTrackTemplate;

	[Tooltip("Show active quests.")]
	public bool showActiveQuests = true;

	[Tooltip("Show successful and failed quests.")]
	public bool showCompletedQuests;

	[Tooltip("Show Entry n Success or Entry n Failure text if quest entry is in success/failure state.")]
	public bool showCompletedEntryText;

	[Tooltip("Source for the quest tracker text.")]
	public QuestDescriptionSource questDescriptionSource;

	public bool visibleOnStart = true;

	protected List<UnityUIQuestTrackTemplate> instantiatedItems = new List<UnityUIQuestTrackTemplate>();

	protected List<UnityUIQuestTrackTemplate> unusedInstances = new List<UnityUIQuestTrackTemplate>();

	protected int siblingIndexCounter;

	protected bool isVisible = true;

	protected Coroutine refreshCoroutine;

	public virtual void Start()
	{
		isVisible = PlayerPrefs.GetInt(playerPrefsToggleKey, visibleOnStart ? 1 : 0) == 1;
		if (container == null)
		{
			Debug.LogWarning(string.Format("{0}: {1} Container is unassigned", new object[2] { "Dialogue System", base.name }));
		}
		if (questTrackTemplate == null)
		{
			Debug.LogWarning(string.Format("{0}: {1} Quest Track Template is unassigned", new object[2] { "Dialogue System", base.name }));
		}
		else
		{
			questTrackTemplate.gameObject.SetActive(value: false);
		}
		if (isVisible)
		{
			Invoke("UpdateTracker", 0.5f);
		}
		else
		{
			HideTracker();
		}
	}

	public virtual void ShowTracker()
	{
		isVisible = true;
		PlayerPrefs.SetInt(playerPrefsToggleKey, 1);
		if (container != null)
		{
			container.gameObject.SetActive(value: true);
		}
		UpdateTracker();
	}

	public virtual void HideTracker()
	{
		isVisible = false;
		PlayerPrefs.SetInt(playerPrefsToggleKey, 0);
		if (container != null)
		{
			container.gameObject.SetActive(value: false);
		}
	}

	public virtual void ToggleTracker()
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

	public virtual void OnQuestTrackingEnabled(string quest)
	{
		UpdateTracker();
	}

	public virtual void OnQuestTrackingDisabled(string quest)
	{
		UpdateTracker();
	}

	public void OnConversationEnd(Transform actor)
	{
		UpdateTracker();
	}

	public virtual void UpdateTracker()
	{
		if (isVisible && refreshCoroutine == null)
		{
			refreshCoroutine = StartCoroutine(RefreshAtEndOfFrame());
		}
	}

	protected virtual IEnumerator RefreshAtEndOfFrame()
	{
		yield return CoroutineUtility.endOfFrame;
		unusedInstances.AddRange(instantiatedItems);
		instantiatedItems.Clear();
		siblingIndexCounter = 0;
		int num = 0;
		string[] allQuests = QuestLog.GetAllQuests((QuestState)((showActiveQuests ? 2 : 0) | (showCompletedQuests ? 12 : 0)));
		foreach (string text in allQuests)
		{
			if (QuestLog.IsQuestTrackingEnabled(text))
			{
				AddQuestTrack(text);
				num++;
			}
		}
		if (container != null)
		{
			container.gameObject.SetActive(showContainerIfEmpty || num > 0);
		}
		for (int j = 0; j < unusedInstances.Count; j++)
		{
			Object.Destroy(unusedInstances[j].gameObject);
		}
		unusedInstances.Clear();
		refreshCoroutine = null;
	}

	protected virtual void AddQuestTrack(string quest)
	{
		if (container == null || questTrackTemplate == null)
		{
			return;
		}
		string text = FormattedText.Parse((questDescriptionSource == QuestDescriptionSource.Title) ? QuestLog.GetQuestTitle(quest) : QuestLog.GetQuestDescription(quest), DialogueManager.masterDatabase.emphasisSettings).text;
		GameObject gameObject;
		if (unusedInstances.Count > 0)
		{
			gameObject = unusedInstances[0].gameObject;
			unusedInstances.RemoveAt(0);
		}
		else
		{
			gameObject = Object.Instantiate(questTrackTemplate.gameObject);
			if (gameObject == null)
			{
				Debug.LogError(string.Format("{0}: {1} couldn't instantiate quest track template", new object[2] { "Dialogue System", base.name }));
				return;
			}
		}
		gameObject.name = text;
		gameObject.transform.SetParent(container.transform, worldPositionStays: false);
		gameObject.SetActive(value: true);
		UnityUIQuestTrackTemplate component = gameObject.GetComponent<UnityUIQuestTrackTemplate>();
		instantiatedItems.Add(component);
		if (!(component != null))
		{
			return;
		}
		component.Initialize();
		QuestState questState = QuestLog.GetQuestState(quest);
		component.SetDescription(text, questState);
		int questEntryCount = QuestLog.GetQuestEntryCount(quest);
		for (int i = 1; i <= questEntryCount; i++)
		{
			QuestState questEntryState = QuestLog.GetQuestEntryState(quest, i);
			string text2 = FormattedText.Parse(GetQuestEntryText(quest, i, questEntryState), DialogueManager.masterDatabase.emphasisSettings).text;
			if (!string.IsNullOrEmpty(text2))
			{
				component.AddEntryDescription(text2, questEntryState);
			}
		}
		component.transform.SetSiblingIndex(siblingIndexCounter++);
	}

	protected virtual string GetQuestEntryText(string quest, int entryNum, QuestState entryState)
	{
		switch (entryState)
		{
		case QuestState.Unassigned:
		case QuestState.Abandoned:
			return string.Empty;
		case QuestState.Success:
		case QuestState.Failure:
			if (!showCompletedEntryText)
			{
				return string.Empty;
			}
			break;
		}
		switch (entryState)
		{
		case QuestState.Success:
		{
			string asString2 = DialogueLua.GetQuestField(quest, "Entry " + entryNum + " Success").asString;
			if (!string.IsNullOrEmpty(asString2))
			{
				return asString2;
			}
			break;
		}
		case QuestState.Failure:
		{
			string asString = DialogueLua.GetQuestField(quest, "Entry " + entryNum + " Failure").asString;
			if (!string.IsNullOrEmpty(asString))
			{
				return asString;
			}
			break;
		}
		}
		return QuestLog.GetQuestEntry(quest, entryNum);
	}
}
