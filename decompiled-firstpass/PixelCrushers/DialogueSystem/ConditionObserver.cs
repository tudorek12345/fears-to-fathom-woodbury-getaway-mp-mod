using System;
using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class ConditionObserver : MonoBehaviour
{
	[Serializable]
	public class SendMessageAction
	{
		public GameObject gameObject;

		public string message = "OnUse";

		public string parameter = string.Empty;
	}

	[Tooltip("Frequency in seconds between checks.")]
	public float frequency = 1f;

	[Tooltip("When observed condition becomes true, run actions and then deactivate this component.")]
	public bool once;

	[Tooltip("Refer to this GameObject when evaluating the Condition.")]
	public GameObject observeGameObject;

	public Condition condition = new Condition();

	[Tooltip("Set this quest's state when the condition is true.")]
	public string questName = string.Empty;

	[Tooltip("Set the quest to this state when the condition is true.")]
	[QuestState]
	public QuestState questState;

	[Tooltip("Run this Lua code when the condition is true. Leave blank to skip.")]
	public string luaCode = string.Empty;

	[Tooltip("Play this sequence when the condition is true. Leave blank to skip.")]
	[TextArea(1, 20)]
	public string sequence = string.Empty;

	[Tooltip("Show this alert message when the condition is true. Leave blank to skip.")]
	public string alertMessage = string.Empty;

	[Tooltip("Text table to use to localize alert message.")]
	public TextTable textTable;

	public SendMessageAction[] sendMessages = new SendMessageAction[0];

	[HideInInspector]
	public bool useQuestNamePicker = true;

	private bool started;

	private void Start()
	{
		started = true;
		StartObserving();
	}

	private void OnEnable()
	{
		if (started)
		{
			StartObserving();
		}
	}

	private void OnDisable()
	{
		StopObserving();
	}

	private void StartObserving()
	{
		StopObserving();
		StartCoroutine(Observe());
	}

	private void StopObserving()
	{
		StopAllCoroutines();
	}

	private IEnumerator Observe()
	{
		yield return new WaitForSeconds(UnityEngine.Random.value);
		while (true)
		{
			Check();
			yield return new WaitForSeconds(frequency);
		}
	}

	public void Check()
	{
		Transform interactor = ((observeGameObject == null) ? null : observeGameObject.transform);
		if (condition.IsTrue(interactor))
		{
			Fire();
		}
	}

	public void Check(GameObject gameObject)
	{
		observeGameObject = gameObject;
		Check();
	}

	public void Check(string gameObjectName)
	{
		GameObject gameObject = Tools.GameObjectHardFind(gameObjectName);
		if (gameObject != null)
		{
			observeGameObject = gameObject;
		}
		Check();
	}

	public void Fire()
	{
		if (!string.IsNullOrEmpty(questName))
		{
			QuestLog.SetQuestState(questName, questState);
		}
		if (!string.IsNullOrEmpty(luaCode))
		{
			Lua.Run(luaCode, DialogueDebug.logInfo);
			DialogueManager.CheckAlerts();
		}
		if (!string.IsNullOrEmpty(sequence))
		{
			DialogueManager.PlaySequence(sequence);
		}
		if (!string.IsNullOrEmpty(alertMessage))
		{
			string message = ((!(textTable != null) || !textTable.HasFieldTextForLanguage(alertMessage, Localization.GetCurrentLanguageID(textTable))) ? DialogueManager.GetLocalizedText(alertMessage) : textTable.GetFieldTextForLanguage(alertMessage, Localization.GetCurrentLanguageID(textTable)));
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
		if (once)
		{
			StopObserving();
			base.enabled = false;
		}
	}
}
