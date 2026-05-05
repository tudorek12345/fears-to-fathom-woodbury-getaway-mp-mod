using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class UnityUIQuestTemplate : MonoBehaviour
{
	[Header("Quest Heading")]
	[Tooltip("The heading - name or description depends on window setting")]
	public Button heading;

	[Tooltip("Used for Description")]
	public Text description;

	public UnityUIQuestTemplateAlternateDescriptions alternateDescriptions = new UnityUIQuestTemplateAlternateDescriptions();

	[Header("Quest Entries")]
	[Tooltip("(Optional) If set, holds instantiated quest entries")]
	public Transform entryContainer;

	[Tooltip("Used for quest entries")]
	public Text entryDescription;

	public UnityUIQuestTemplateAlternateDescriptions alternateEntryDescriptions = new UnityUIQuestTemplateAlternateDescriptions();

	[Header("Buttons")]
	[Tooltip("Used for Track button if quest is trackable")]
	public Button trackButton;

	[Tooltip("Used for Abandon button if quest is abandonable")]
	public Button abandonButton;

	protected List<GameObject> entryInstances = new List<GameObject>();

	protected int numEntries;

	public bool ArePropertiesAssigned
	{
		get
		{
			if ((Object)(object)heading != null && (Object)(object)description != null && (Object)(object)entryDescription != null && (Object)(object)trackButton != null)
			{
				return (Object)(object)abandonButton != null;
			}
			return false;
		}
	}

	public virtual void Initialize()
	{
		if ((Object)(object)description != null)
		{
			((Component)(object)description).gameObject.SetActive(value: false);
		}
		if ((Object)(object)entryDescription != null)
		{
			((Component)(object)entryDescription).gameObject.SetActive(value: false);
		}
		alternateEntryDescriptions.SetActive(value: false);
		if (entryContainer != null)
		{
			entryContainer.gameObject.SetActive(value: false);
		}
	}

	public virtual void ClearQuestDetails()
	{
		if (entryContainer == null)
		{
			if ((Object)(object)entryDescription != null)
			{
				entryDescription.text = string.Empty;
			}
		}
		else
		{
			for (int i = 0; i < entryInstances.Count; i++)
			{
				Object.Destroy(entryInstances[i]);
			}
			entryInstances.Clear();
		}
		numEntries = 0;
	}

	public virtual void AddEntryDescription(string text, QuestState entryState)
	{
		if (entryContainer == null)
		{
			if (entryState != QuestState.Unassigned)
			{
				alternateEntryDescriptions.SetActive(value: false);
				if ((Object)(object)entryDescription != null)
				{
					if (numEntries == 0)
					{
						((Component)(object)entryDescription).gameObject.SetActive(value: true);
						entryDescription.text = text;
					}
					else
					{
						Text obj = entryDescription;
						obj.text = obj.text + "\n" + text;
					}
				}
			}
		}
		else
		{
			if (numEntries == 0)
			{
				entryContainer.gameObject.SetActive(value: true);
				if ((Object)(object)entryDescription != null)
				{
					((Component)(object)entryDescription).gameObject.SetActive(value: false);
				}
				alternateEntryDescriptions.SetActive(value: false);
			}
			switch (entryState)
			{
			case QuestState.Active:
				InstantiateFirstValidTextElement(text, entryContainer, entryDescription);
				break;
			case QuestState.Success:
				InstantiateFirstValidTextElement(text, entryContainer, alternateEntryDescriptions.successDescription, entryDescription);
				break;
			case QuestState.Failure:
				InstantiateFirstValidTextElement(text, entryContainer, alternateEntryDescriptions.failureDescription, entryDescription);
				break;
			}
		}
		numEntries++;
	}

	protected void InstantiateFirstValidTextElement(string text, Transform container, params Text[] textElements)
	{
		for (int i = 0; i < textElements.Length; i++)
		{
			if ((Object)(object)textElements[i] != null)
			{
				GameObject gameObject = Object.Instantiate(((Component)(object)textElements[i]).gameObject);
				entryInstances.Add(gameObject);
				gameObject.transform.SetParent(container.transform, worldPositionStays: false);
				gameObject.SetActive(value: true);
				Text component = gameObject.GetComponent<Text>();
				if ((Object)(object)component != null)
				{
					component.text = text;
				}
				break;
			}
		}
	}
}
