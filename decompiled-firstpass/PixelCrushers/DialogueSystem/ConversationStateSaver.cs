using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class ConversationStateSaver : Saver
{
	[Serializable]
	public class Data
	{
		public int conversationID;

		public int entryID;

		public string actorName;

		public string conversantName;

		public List<string> actorGOs;

		public List<SubtitlePanelNumber> actorGOPanels;

		public List<int> actorIDs;

		public List<SubtitlePanelNumber> actorIDPanels;

		public List<string> panelOpenOnActorName;

		public string accumulatedText;
	}

	public override string key
	{
		get
		{
			if (string.IsNullOrEmpty(m_runtimeKey))
			{
				m_runtimeKey = ((!string.IsNullOrEmpty(base._internalKeyValue)) ? base._internalKeyValue : "ConversationState");
				if (base.appendSaverTypeToKey)
				{
					string text = GetType().Name;
					if (text.EndsWith("Saver"))
					{
						text.Remove(text.Length - "Saver".Length);
					}
					m_runtimeKey += text;
				}
			}
			return m_runtimeKey;
		}
		set
		{
			base._internalKeyValue = value;
			m_runtimeKey = value;
		}
	}

	public override string RecordData()
	{
		if (!DialogueManager.isConversationActive)
		{
			return string.Empty;
		}
		Data data = new Data();
		ConversationState currentConversationState = DialogueManager.currentConversationState;
		DialogueEntry dialogueEntry = currentConversationState.subtitle.dialogueEntry;
		data.conversationID = dialogueEntry.conversationID;
		data.entryID = currentConversationState.subtitle.dialogueEntry.id;
		data.actorName = ((DialogueManager.currentActor != null) ? DialogueManager.currentActor.name : string.Empty);
		data.conversantName = ((DialogueManager.currentConversant != null) ? DialogueManager.currentConversant.name : string.Empty);
		StandardDialogueUI standardDialogueUI = DialogueManager.dialogueUI as StandardDialogueUI;
		if (standardDialogueUI != null)
		{
			standardDialogueUI.conversationUIElements.standardSubtitleControls.RecordActorPanelCache(out data.actorGOs, out data.actorGOPanels, out data.actorIDs, out data.actorIDPanels, out data.panelOpenOnActorName);
			data.accumulatedText = string.Empty;
			for (int i = 0; i < standardDialogueUI.conversationUIElements.subtitlePanels.Length; i++)
			{
				StandardUISubtitlePanel standardUISubtitlePanel = standardDialogueUI.conversationUIElements.subtitlePanels[i];
				if (!standardUISubtitlePanel.isOpen && 0 <= i && i < data.panelOpenOnActorName.Count)
				{
					data.panelOpenOnActorName[i] = null;
				}
				if (standardUISubtitlePanel.isOpen && standardUISubtitlePanel.accumulateText)
				{
					data.accumulatedText = standardUISubtitlePanel.accumulatedText;
					break;
				}
			}
		}
		return SaveSystem.Serialize(data);
	}

	public override void ApplyData(string s)
	{
		if (base.enabled && !string.IsNullOrEmpty(s))
		{
			Data data = SaveSystem.Deserialize<Data>(s);
			if (data != null)
			{
				StartCoroutine(StartSavedConversation(data));
			}
		}
	}

	protected IEnumerator StartSavedConversation(Data data)
	{
		StandardDialogueUI dialogueUI = DialogueManager.dialogueUI as StandardDialogueUI;
		DialogueManager.StopConversation();
		if (dialogueUI != null)
		{
			float safeguardTimeout = Time.realtimeSinceStartup + 5f;
			while (dialogueUI.isOpen && Time.realtimeSinceStartup < safeguardTimeout)
			{
				yield return null;
			}
		}
		int conversationID = data.conversationID;
		int entryID = data.entryID;
		Conversation conversation = DialogueManager.masterDatabase.GetConversation(conversationID);
		string asString = DialogueLua.GetVariable("CurrentConversationActor").AsString;
		string asString2 = DialogueLua.GetVariable("CurrentConversationConversant").AsString;
		if (DialogueDebug.logInfo)
		{
			Debug.Log("Dialogue System: ConversationStateSaver is resuming conversation " + conversation.Title + " with actor=" + asString + " and conversant=" + asString2 + " at entry " + entryID + ".", this);
		}
		GameObject gameObject = (string.IsNullOrEmpty(asString) ? null : GameObject.Find(asString));
		GameObject gameObject2 = (string.IsNullOrEmpty(asString2) ? null : GameObject.Find(asString2));
		Transform actor = ((gameObject != null) ? gameObject.transform : null);
		Transform conversant = ((gameObject2 != null) ? gameObject2.transform : null);
		StandardDialogueUI standardDialogueUI = DialogueManager.dialogueUI as StandardDialogueUI;
		if (standardDialogueUI != null)
		{
			standardDialogueUI.conversationUIElements.standardSubtitleControls.QueueSavedActorPanelCache(data.actorGOs, data.actorGOPanels, data.actorIDs, data.actorIDPanels);
		}
		DialogueManager.StartConversation(conversation.Title, actor, conversant, entryID);
		if (!(standardDialogueUI != null))
		{
			yield break;
		}
		for (int i = 0; i < standardDialogueUI.conversationUIElements.subtitlePanels.Length; i++)
		{
			StandardUISubtitlePanel standardUISubtitlePanel = standardDialogueUI.conversationUIElements.subtitlePanels[i];
			if (0 <= i && i < data.panelOpenOnActorName.Count && !string.IsNullOrEmpty(data.panelOpenOnActorName[i]))
			{
				Transform registeredActorTransform = CharacterInfo.GetRegisteredActorTransform(data.panelOpenOnActorName[i]);
				DialogueActor dialogueActor = ((registeredActorTransform != null) ? registeredActorTransform.GetComponent<DialogueActor>() : null);
				Actor actor2 = DialogueManager.masterDatabase.GetActor(data.panelOpenOnActorName[i]);
				Sprite portraitSprite = actor2.GetPortraitSprite();
				string text = data.panelOpenOnActorName[i];
				if (dialogueActor != null)
				{
					Sprite portraitSprite2 = dialogueActor.GetPortraitSprite();
					if (portraitSprite2 != null)
					{
						portraitSprite = portraitSprite2;
					}
					text = dialogueActor.GetActorName();
				}
				else if (actor2 != null)
				{
					portraitSprite = actor2.GetPortraitSprite();
					text = CharacterInfo.GetLocalizedDisplayNameInDatabase(text);
				}
				standardUISubtitlePanel.OpenOnStartConversation(portraitSprite, text, dialogueActor);
			}
			if (standardUISubtitlePanel.accumulateText)
			{
				standardUISubtitlePanel.accumulatedText = data.accumulatedText;
			}
		}
	}
}
