using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class DialogueSystemSceneEvents : MonoBehaviour
{
	[HelpBox("Do not remove this GameObject. It contains UnityEvents referenced by a dialogue database. This GameObject should not be a child of the Dialogue Manager or marked as Don't Destroy On Load.", HelpBoxMessageType.Info)]
	public List<DialogueEntrySceneEvent> dialogueEntrySceneEvents = new List<DialogueEntrySceneEvent>();

	private static List<DialogueSystemSceneEvents> m_sceneInstances = new List<DialogueSystemSceneEvents>();

	private void Awake()
	{
		RegisterInstance();
	}

	private void Start()
	{
		RegisterInstance();
	}

	private void OnDestroy()
	{
		m_sceneInstances.Remove(this);
	}

	private void RegisterInstance()
	{
		if (!m_sceneInstances.Contains(this))
		{
			m_sceneInstances.Add(this);
		}
	}

	public static int AddNewDialogueEntrySceneEvent(out string guid, DialogueSystemSceneEvents sceneInstanceToUse = null)
	{
		guid = string.Empty;
		if (sceneInstanceToUse == null)
		{
			sceneInstanceToUse = GameObjectUtility.FindFirstObjectByType<DialogueSystemSceneEvents>();
		}
		if (sceneInstanceToUse == null)
		{
			return -1;
		}
		guid = Guid.NewGuid().ToString();
		DialogueEntrySceneEvent dialogueEntrySceneEvent = new DialogueEntrySceneEvent();
		dialogueEntrySceneEvent.guid = guid;
		sceneInstanceToUse.dialogueEntrySceneEvents.Add(dialogueEntrySceneEvent);
		return sceneInstanceToUse.dialogueEntrySceneEvents.Count - 1;
	}

	public static void RemoveDialogueEntrySceneEvent(string guid, DialogueSystemSceneEvents sceneInstanceToUse = null)
	{
		if (sceneInstanceToUse != null)
		{
			sceneInstanceToUse.dialogueEntrySceneEvents.RemoveAll((DialogueEntrySceneEvent x) => x.guid == guid);
			return;
		}
		DialogueSystemSceneEvents[] array = GameObjectUtility.FindObjectsByType<DialogueSystemSceneEvents>();
		for (int num = 0; num < array.Length; num++)
		{
			array[num].dialogueEntrySceneEvents.RemoveAll((DialogueEntrySceneEvent x) => x.guid == guid);
		}
	}

	public static DialogueEntrySceneEvent GetDialogueEntrySceneEvent(string guid)
	{
		if (!Application.isPlaying)
		{
			return null;
		}
		foreach (DialogueSystemSceneEvents sceneInstance in m_sceneInstances)
		{
			if (!(sceneInstance == null) && sceneInstance.dialogueEntrySceneEvents != null)
			{
				DialogueEntrySceneEvent dialogueEntrySceneEvent = sceneInstance.dialogueEntrySceneEvents.Find((DialogueEntrySceneEvent x) => x.guid == guid);
				if (dialogueEntrySceneEvent != null)
				{
					return dialogueEntrySceneEvent;
				}
			}
		}
		return null;
	}

	public static int GetDialogueEntrySceneEventIndex(string guid)
	{
		if (!Application.isPlaying)
		{
			return -1;
		}
		foreach (DialogueSystemSceneEvents sceneInstance in m_sceneInstances)
		{
			if (!(sceneInstance == null) && sceneInstance.dialogueEntrySceneEvents != null)
			{
				int num = sceneInstance.dialogueEntrySceneEvents.FindIndex((DialogueEntrySceneEvent x) => x.guid == guid);
				if (num != -1)
				{
					return num;
				}
			}
		}
		return -1;
	}

	public static int GetDialogueEntrySceneEventIndex(string guid, DialogueSystemSceneEvents instance)
	{
		if (instance == null)
		{
			return -1;
		}
		return instance.dialogueEntrySceneEvents.FindIndex((DialogueEntrySceneEvent x) => x.guid == guid);
	}
}
