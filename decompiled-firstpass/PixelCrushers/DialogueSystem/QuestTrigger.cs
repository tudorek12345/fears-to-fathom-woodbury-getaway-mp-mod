using System;
using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class QuestTrigger : DialogueEventStarter
{
	[Serializable]
	public class SendMessageAction
	{
		public GameObject gameObject;

		public string message = "OnUse";

		public string parameter = string.Empty;
	}

	[DialogueTriggerEvent]
	public DialogueTriggerEvent trigger = DialogueTriggerEvent.OnUse;

	public Condition condition;

	public string questName;

	[Tooltip("Set the main state of the quest.")]
	public bool setQuestState = true;

	[QuestState]
	public QuestState questState;

	[Tooltip("Set the state of a quest entry (subtask) in the quest.")]
	public bool setQuestEntryState;

	[Tooltip("Quest entry number whose state to change.")]
	public int questEntryNumber = 1;

	[QuestState]
	public QuestState questEntryState;

	[Tooltip("(Optional) Run this Lua code after setting the quest state.")]
	public string luaCode = string.Empty;

	[Tooltip("(Optional) Show this alert message after setting the quest state.")]
	public string alertMessage;

	[Tooltip("Localized text table to use for the alert message.")]
	public LocalizedTextTable localizedTextTable;

	public SendMessageAction[] sendMessages = new SendMessageAction[0];

	[HideInInspector]
	public bool useQuestNamePicker = true;

	[HideInInspector]
	public DialogueDatabase selectedDatabase;

	private bool tryingToStart;

	private bool listenForOnDestroy;

	public void OnBarkEnd(Transform actor)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnBarkEnd)
		{
			TryStart(actor);
		}
	}

	public void OnConversationEnd(Transform actor)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnConversationEnd)
		{
			TryStart(actor);
		}
	}

	public void OnSequenceEnd(Transform actor)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnSequenceEnd)
		{
			TryStart(actor);
		}
	}

	public void OnUse(Transform actor)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnUse)
		{
			TryStart(actor);
		}
	}

	public void OnUse(string message)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnUse)
		{
			TryStart(null);
		}
	}

	public void OnUse()
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnUse)
		{
			TryStart(null);
		}
	}

	public void OnTriggerEnter(Collider other)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnTriggerEnter)
		{
			TryStart(other.transform);
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnTriggerExit)
		{
			TryStart(other.transform);
		}
	}

	public void OnCollisionEnter(Collision collision)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnCollisionEnter)
		{
			TryStart(collision.collider.transform);
		}
	}

	public void OnCollisionExit(Collision collision)
	{
		if (base.enabled && trigger == DialogueTriggerEvent.OnTriggerExit)
		{
			TryStart(collision.collider.transform);
		}
	}

	public void Start()
	{
		if (trigger == DialogueTriggerEvent.OnStart)
		{
			StartCoroutine(StartAfterOneFrame());
		}
	}

	public void OnEnable()
	{
		listenForOnDestroy = true;
		if (trigger == DialogueTriggerEvent.OnEnable)
		{
			StartCoroutine(StartAfterOneFrame());
		}
	}

	public void OnDisable()
	{
		if (listenForOnDestroy && trigger == DialogueTriggerEvent.OnDisable)
		{
			TryStart(null);
		}
	}

	public void OnLevelWillBeUnloaded()
	{
		listenForOnDestroy = false;
	}

	public void OnApplicationQuit()
	{
		listenForOnDestroy = false;
	}

	public void OnDestroy()
	{
		if (listenForOnDestroy && trigger == DialogueTriggerEvent.OnDestroy)
		{
			TryStart(null);
		}
	}

	private IEnumerator StartAfterOneFrame()
	{
		yield return null;
		TryStart(null);
	}

	public void TryStart(Transform actor)
	{
		if (tryingToStart)
		{
			return;
		}
		tryingToStart = true;
		try
		{
			if ((condition == null || condition.IsTrue(actor)) && !string.IsNullOrEmpty(questName))
			{
				Fire();
			}
		}
		finally
		{
			tryingToStart = false;
		}
	}

	public void Fire()
	{
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Setting quest '{1}' state to '{2}'", new object[3]
			{
				"Dialogue System",
				questName,
				QuestLog.StateToString(questState)
			}));
		}
		if (!string.IsNullOrEmpty(questName))
		{
			if (setQuestState)
			{
				QuestLog.SetQuestState(questName, questState);
			}
			if (setQuestEntryState)
			{
				QuestLog.SetQuestEntryState(questName, questEntryNumber, questEntryState);
			}
		}
		if (!string.IsNullOrEmpty(luaCode))
		{
			Lua.Run(luaCode, DialogueDebug.logInfo);
		}
		if (!string.IsNullOrEmpty(alertMessage))
		{
			string message = alertMessage;
			if (localizedTextTable != null && localizedTextTable.ContainsField(alertMessage))
			{
				message = localizedTextTable[alertMessage];
			}
			DialogueManager.ShowAlert(message);
		}
		SendMessageAction[] array = sendMessages;
		foreach (SendMessageAction sendMessageAction in array)
		{
			if (sendMessageAction.gameObject != null && !string.IsNullOrEmpty(sendMessageAction.message))
			{
				sendMessageAction.gameObject.SendMessage(sendMessageAction.message, sendMessageAction.parameter, SendMessageOptions.DontRequireReceiver);
			}
		}
		DialogueManager.SendUpdateTracker();
		DestroyIfOnce();
	}
}
