using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class StandardUIQuestTrackTemplate : MonoBehaviour
{
	[Header("Quest Heading")]
	[Tooltip("The heading - name or description depends on tracker setting")]
	public UITextField description;

	public StandardUIQuestTemplateAlternateDescriptions alternateDescriptions = new StandardUIQuestTemplateAlternateDescriptions();

	[Header("Quest Entries")]
	[Tooltip("(Optional) If set, holds instantiated quest entries")]
	public Transform entryContainer;

	[Tooltip("Used for quest entries")]
	public UITextField entryDescription;

	public StandardUIQuestTemplateAlternateDescriptions alternateEntryDescriptions = new StandardUIQuestTemplateAlternateDescriptions();

	protected List<GameObject> m_instances;

	protected int numEntries;

	public bool arePropertiesAssigned
	{
		get
		{
			if (description != null)
			{
				return entryDescription != null;
			}
			return false;
		}
	}

	public virtual void Initialize()
	{
		description.SetActive(value: false);
		alternateDescriptions.SetActive(value: false);
		entryDescription.SetActive(value: false);
		alternateEntryDescriptions.SetActive(value: false);
		if (entryContainer != null)
		{
			entryContainer.gameObject.SetActive(value: false);
			if (m_instances != null)
			{
				for (int i = 0; i < m_instances.Count; i++)
				{
					if (m_instances[i] != null)
					{
						Object.Destroy(m_instances[i].gameObject);
					}
				}
			}
			m_instances = new List<GameObject>();
		}
		numEntries = 0;
	}

	public virtual void SetDescription(string text, QuestState questState)
	{
		if (text != null)
		{
			switch (questState)
			{
			case QuestState.Active:
			case QuestState.ReturnToNPC:
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

	public virtual void AddEntryDescription(string text, QuestState entryState)
	{
		if (entryContainer == null)
		{
			alternateEntryDescriptions.SetActive(value: false);
			if (entryDescription != null)
			{
				if (numEntries == 0)
				{
					entryDescription.SetActive(value: true);
					entryDescription.text = text;
				}
				else
				{
					UITextField uITextField = entryDescription;
					uITextField.text = uITextField.text + "\n" + text;
				}
			}
		}
		else
		{
			if (numEntries == 0)
			{
				entryContainer.gameObject.SetActive(value: true);
				if (entryDescription != null)
				{
					entryDescription.gameObject.SetActive(value: false);
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

	protected void SetFirstValidTextElement(string text, params UITextField[] textElements)
	{
		for (int i = 0; i < textElements.Length; i++)
		{
			if (textElements[i] != null && textElements[i].gameObject != null)
			{
				textElements[i].SetActive(value: true);
				textElements[i].text = text;
				break;
			}
		}
	}

	protected void InstantiateFirstValidTextElement(string text, Transform container, params UITextField[] textElements)
	{
		for (int i = 0; i < textElements.Length; i++)
		{
			if (textElements[i] != null && textElements[i].gameObject != null)
			{
				textElements[i].text = text;
				GameObject gameObject = Object.Instantiate(textElements[i].gameObject);
				gameObject.transform.SetParent(container.transform, worldPositionStays: false);
				gameObject.SetActive(value: true);
				m_instances.Add(gameObject);
				break;
			}
		}
	}
}
