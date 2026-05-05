using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class UnityUIQuestTrackTemplate : MonoBehaviour
{
	[Header("Quest Heading")]
	[Tooltip("The heading - name or description depends on tracker setting")]
	public Text description;

	public UnityUIQuestTemplateAlternateDescriptions alternateDescriptions = new UnityUIQuestTemplateAlternateDescriptions();

	[Header("Quest Entries")]
	[Tooltip("(Optional) If set, holds instantiated quest entries")]
	public Transform entryContainer;

	[Tooltip("Used for quest entries")]
	public Text entryDescription;

	public UnityUIQuestTemplateAlternateDescriptions alternateEntryDescriptions = new UnityUIQuestTemplateAlternateDescriptions();

	private List<Text> instances;

	private int numEntries;

	public bool ArePropertiesAssigned
	{
		get
		{
			if ((Object)(object)description != null)
			{
				return (Object)(object)entryDescription != null;
			}
			return false;
		}
	}

	public void Initialize()
	{
		if ((Object)(object)description != null)
		{
			((Component)(object)description).gameObject.SetActive(value: false);
		}
		alternateDescriptions.SetActive(value: false);
		if ((Object)(object)entryDescription != null)
		{
			((Component)(object)entryDescription).gameObject.SetActive(value: false);
		}
		alternateEntryDescriptions.SetActive(value: false);
		if (entryContainer != null)
		{
			entryContainer.gameObject.SetActive(value: false);
			if (instances != null)
			{
				for (int i = 0; i < instances.Count; i++)
				{
					if ((Object)(object)instances[i] != null)
					{
						Object.Destroy(((Component)(object)instances[i]).gameObject);
					}
				}
			}
			instances = new List<Text>();
		}
		numEntries = 0;
	}

	public void SetDescription(string text, QuestState questState)
	{
		if (text != null)
		{
			switch (questState)
			{
			case QuestState.Active:
				SetFirstValidTextElement(text, description);
				break;
			case QuestState.Success:
				SetFirstValidTextElement(text, alternateDescriptions.successDescription, description);
				break;
			case QuestState.Failure:
				SetFirstValidTextElement(text, alternateDescriptions.failureDescription, description);
				break;
			}
		}
	}

	private void SetFirstValidTextElement(string text, params Text[] textElements)
	{
		for (int i = 0; i < textElements.Length; i++)
		{
			if ((Object)(object)textElements[i] != null)
			{
				((Component)(object)textElements[i]).gameObject.SetActive(value: true);
				textElements[i].text = text;
				break;
			}
		}
	}

	public void AddEntryDescription(string text, QuestState entryState)
	{
		if (entryContainer == null)
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

	private void InstantiateFirstValidTextElement(string text, Transform container, params Text[] textElements)
	{
		for (int i = 0; i < textElements.Length; i++)
		{
			if ((Object)(object)textElements[i] != null)
			{
				GameObject obj = Object.Instantiate(((Component)(object)textElements[i]).gameObject);
				obj.transform.SetParent(container.transform, worldPositionStays: false);
				obj.SetActive(value: true);
				Text component = obj.GetComponent<Text>();
				if ((Object)(object)component != null)
				{
					component.text = text;
				}
				instances.Add(component);
				break;
			}
		}
	}
}
