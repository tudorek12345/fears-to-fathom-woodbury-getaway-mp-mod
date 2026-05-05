using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public class ConversationModel
{
	private DialogueDatabase m_database;

	private CharacterInfo m_actorInfo;

	private CharacterInfo m_conversantInfo;

	private bool m_allowLuaExceptions;

	private Dictionary<int, CharacterInfo> m_characterInfoCache = new Dictionary<int, CharacterInfo>();

	private EntrytagFormat m_entrytagFormat;

	private EmTag m_emTagForOldResponses;

	private EmTag m_emTagForInvalidResponses;

	private bool m_includeInvalidEntries;

	private string pcPortraitName;

	private Sprite pcPortraitSprite;

	private DialogueEntry forceLinkEntry;

	private int m_currentDialogTableConversationID = -1;

	public string conversationTitle { get; private set; }

	public ConversationState firstState { get; private set; }

	public CharacterInfo actorInfo => m_actorInfo;

	public CharacterInfo conversantInfo => m_conversantInfo;

	public bool hasValidEntry
	{
		get
		{
			if (firstState != null)
			{
				if (!firstState.hasAnyResponses)
				{
					return !IsStartEntryState(firstState);
				}
				return true;
			}
			return false;
		}
	}

	public IsDialogueEntryValidDelegate isDialogueEntryValid { get; set; }

	public ConversationState FirstState
	{
		get
		{
			return firstState;
		}
		private set
		{
			firstState = value;
		}
	}

	public CharacterInfo ActorInfo => actorInfo;

	public CharacterInfo ConversantInfo => conversantInfo;

	public bool HasValidEntry => hasValidEntry;

	public IsDialogueEntryValidDelegate IsDialogueEntryValid
	{
		get
		{
			return isDialogueEntryValid;
		}
		private set
		{
			isDialogueEntryValid = value;
		}
	}

	public bool allowLuaExceptions
	{
		get
		{
			return m_allowLuaExceptions;
		}
		set
		{
			m_allowLuaExceptions = value;
		}
	}

	public EntrytagFormat entrytagFormat
	{
		get
		{
			return m_entrytagFormat;
		}
		set
		{
			m_entrytagFormat = value;
		}
	}

	public EmTag emTagForOldResponses
	{
		get
		{
			return m_emTagForOldResponses;
		}
		set
		{
			m_emTagForOldResponses = value;
		}
	}

	public EmTag emTagForInvalidResponses
	{
		get
		{
			return m_emTagForInvalidResponses;
		}
		set
		{
			m_emTagForInvalidResponses = value;
		}
	}

	public bool includeInvalidEntries
	{
		get
		{
			return m_includeInvalidEntries;
		}
		set
		{
			m_includeInvalidEntries = value;
		}
	}

	public ConversationModel(DialogueDatabase database, string title, Transform actor, Transform conversant, bool allowLuaExceptions, IsDialogueEntryValidDelegate isDialogueEntryValid, int initialDialogueEntryID = -1, bool stopAtFirstValid = false, bool skipExecution = false)
	{
		m_allowLuaExceptions = allowLuaExceptions;
		m_database = database;
		conversationTitle = title;
		this.isDialogueEntryValid = isDialogueEntryValid;
		Conversation conversation = database.GetConversation(title);
		if (conversation != null)
		{
			DisplaySettings displaySettings = DialogueManager.displaySettings;
			if (displaySettings != null)
			{
				if (displaySettings.cameraSettings != null)
				{
					m_entrytagFormat = displaySettings.cameraSettings.entrytagFormat;
				}
				if (displaySettings.inputSettings != null)
				{
					m_emTagForOldResponses = displaySettings.inputSettings.emTagForOldResponses;
					m_emTagForInvalidResponses = displaySettings.inputSettings.emTagForInvalidResponses;
					m_includeInvalidEntries = displaySettings.inputSettings.includeInvalidEntries;
				}
			}
			if (conversation.overrideSettings != null && conversation.overrideSettings.overrideInputSettings)
			{
				m_emTagForOldResponses = conversation.overrideSettings.emTagForOldResponses;
				m_emTagForInvalidResponses = conversation.overrideSettings.emTagForInvalidResponses;
				m_includeInvalidEntries = conversation.overrideSettings.includeInvalidEntries;
			}
			SetParticipants(conversation, actor, conversant);
			if (initialDialogueEntryID == -1)
			{
				firstState = GetState(conversation.GetFirstDialogueEntry(), includeLinks: true, stopAtFirstValid, skipExecution);
				FixFirstStateSequence();
			}
			else
			{
				firstState = GetState(conversation.GetDialogueEntry(initialDialogueEntryID), includeLinks: true, stopAtFirstValid, skipExecution);
			}
		}
		else
		{
			firstState = null;
			if (DialogueDebug.logErrors)
			{
				Debug.LogWarning(string.Format("{0}: Conversation '{1}' not found in database.", new object[2] { "Dialogue System", title }));
			}
		}
	}

	public int GetConversationID(ConversationState state)
	{
		if (state == null || state.subtitle == null || state.subtitle.dialogueEntry == null)
		{
			return -1;
		}
		return state.subtitle.dialogueEntry.conversationID;
	}

	public ConversationOverrideDisplaySettings GetConversationOverrideSettings(ConversationState state)
	{
		return m_database.GetConversation(GetConversationID(state))?.overrideSettings;
	}

	private void FixFirstStateSequence()
	{
		if (firstState != null && firstState.subtitle != null && string.IsNullOrEmpty(firstState.subtitle.sequence) && string.IsNullOrEmpty(firstState.subtitle.formattedText.text))
		{
			firstState.subtitle.sequence = "None()";
		}
	}

	private bool IsStartEntryState(ConversationState state)
	{
		if (state != null && state.subtitle != null && state.subtitle.dialogueEntry != null)
		{
			return state.subtitle.dialogueEntry.id == 0;
		}
		return false;
	}

	public void InformParticipants(string message, bool informDialogueManager = false)
	{
		if (DialogueSystemController.isWarmingUp)
		{
			return;
		}
		Transform transform = ((m_actorInfo == null) ? null : m_actorInfo.transform);
		Transform transform2 = ((m_conversantInfo == null) ? null : m_conversantInfo.transform);
		Transform transform3 = null;
		if (transform != null)
		{
			transform3 = ((transform2 != null) ? transform2 : transform);
			if (transform3 != null)
			{
				transform.BroadcastMessage(message, transform3, SendMessageOptions.DontRequireReceiver);
			}
		}
		if (transform2 != null && transform2 != transform)
		{
			transform3 = ((transform != null) ? transform : transform2);
			if (transform3 != null)
			{
				transform2.BroadcastMessage(message, transform3, SendMessageOptions.DontRequireReceiver);
			}
		}
		if (!informDialogueManager)
		{
			return;
		}
		Transform transform4 = DialogueManager.instance.transform;
		if (transform4 != transform && transform4 != transform2)
		{
			transform3 = ((transform != null) ? transform : transform2);
			if (transform3 == null)
			{
				transform3 = DialogueManager.instance.transform;
			}
			DialogueManager.instance.BroadcastMessage(message, transform3, SendMessageOptions.DontRequireReceiver);
		}
	}

	public ConversationState GetState(DialogueEntry entry, bool includeLinks, bool stopAtFirstValid = false, bool skipExecution = false)
	{
		if (entry != null)
		{
			if (DialogueManager.instance.activeConversations.Count > 1)
			{
				DialogueLua.SetParticipants(m_actorInfo.Name, m_conversantInfo.Name, m_actorInfo.nameInDatabase, m_conversantInfo.nameInDatabase);
			}
			DialogueManager.instance.SendMessage("OnPrepareConversationLine", entry, SendMessageOptions.DontRequireReceiver);
			DialogueLua.MarkDialogueEntryDisplayed(entry);
			Lua.Run("thisID = " + entry.id);
			SetDialogTable(entry.conversationID);
			if (!skipExecution)
			{
				Lua.Run(entry.userScript, DialogueDebug.logInfo, m_allowLuaExceptions);
				try
				{
					entry.onExecute.Invoke();
				}
				catch (Exception ex)
				{
					if (DialogueDebug.logWarnings)
					{
						Debug.LogWarning("Non-scene OnExecute() event failed on dialogue entry " + entry.conversationID + ":" + entry.id + ": " + ex.Message);
					}
				}
			}
			CharacterInfo characterInfo = GetCharacterInfo(entry.ActorID);
			CharacterInfo characterInfo2 = GetCharacterInfo(entry.ConversantID);
			if (!skipExecution)
			{
				DialogueEntrySceneEvent dialogueEntrySceneEvent = DialogueSystemSceneEvents.GetDialogueEntrySceneEvent(entry.sceneEventGuid);
				GameObject arg = ((characterInfo.transform != null) ? characterInfo.transform.gameObject : DialogueManager.instance.gameObject);
				if (dialogueEntrySceneEvent != null)
				{
					try
					{
						dialogueEntrySceneEvent.onExecute.Invoke(arg);
					}
					catch (Exception ex2)
					{
						if (DialogueDebug.logWarnings)
						{
							Debug.LogWarning("Scene OnExecute() event failed on dialogue entry " + entry.conversationID + ":" + entry.id + ": " + ex2.Message);
						}
					}
				}
			}
			FormattedText formattedText = FormattedText.Parse(entry.subtitleText, m_database.emphasisSettings);
			CheckSequenceField(entry);
			string entrytag = m_database.GetEntrytag(entry.conversationID, entry.id, m_entrytagFormat);
			Subtitle subtitle = new Subtitle(characterInfo, characterInfo2, formattedText, entry.currentSequence, entry.currentResponseMenuSequence, entry, entrytag);
			List<Response> list = new List<Response>();
			List<Response> list2 = new List<Response>();
			if (includeLinks)
			{
				if (forceLinkEntry != null)
				{
					AddForcedLink(list, list2);
				}
				else
				{
					EvaluateLinks(entry, list, list2, new List<DialogueEntry>(), stopAtFirstValid, skipExecution);
				}
			}
			return new ConversationState(subtitle, list.ToArray(), list2.ToArray(), entry.isGroup);
		}
		return null;
	}

	public void ForceNextStateToLinkToEntry(DialogueEntry entry)
	{
		forceLinkEntry = entry;
	}

	private void AddForcedLink(List<Response> npcResponses, List<Response> pcResponses)
	{
		if (m_database.GetCharacterType(forceLinkEntry.ActorID) == CharacterType.NPC)
		{
			npcResponses.Add(new Response(FormattedText.Parse(forceLinkEntry.subtitleText, m_database.emphasisSettings), forceLinkEntry));
		}
		else
		{
			pcResponses.Add(new Response(FormattedText.Parse(forceLinkEntry.subtitleText, m_database.emphasisSettings), forceLinkEntry));
		}
		forceLinkEntry = null;
	}

	public ConversationState GetState(DialogueEntry entry)
	{
		return GetState(entry, includeLinks: true);
	}

	public void UpdateResponses(ConversationState state)
	{
		List<Response> list = new List<Response>();
		List<Response> list2 = new List<Response>();
		EvaluateLinks(state.subtitle.dialogueEntry, list, list2, new List<DialogueEntry>());
		state.npcResponses = list.ToArray();
		state.pcResponses = list2.ToArray();
	}

	private void SetDialogTable(int newConversationID)
	{
		if (m_currentDialogTableConversationID != newConversationID)
		{
			m_currentDialogTableConversationID = newConversationID;
			Lua.Run($"Dialog = Conversation[{newConversationID}].Dialog");
		}
	}

	private void CheckSequenceField(DialogueEntry entry)
	{
		if (string.IsNullOrEmpty(entry.currentSequence) && !string.IsNullOrEmpty(entry.VideoFile) && DialogueDebug.logWarnings)
		{
			Debug.LogWarning(string.Format("{0}: Dialogue entry '{1}' Video File field is assigned but Sequence is blank. Cutscenes now use Sequence field.", new object[2] { "Dialogue System", entry.currentDialogueText }));
		}
	}

	private void EvaluateLinks(DialogueEntry entry, List<Response> npcResponses, List<Response> pcResponses, List<DialogueEntry> visited, bool stopAtFirstValid = false, bool skipExecution = false)
	{
		if (entry == null || visited.Contains(entry))
		{
			return;
		}
		visited.Add(entry);
		for (int num = 4; num >= 0; num--)
		{
			EvaluateLinksAtPriority((ConditionPriority)num, entry, npcResponses, pcResponses, visited, stopAtFirstValid, skipExecution);
			if (npcResponses.Count > 0 || pcResponses.Count > 0)
			{
				break;
			}
		}
	}

	private void EvaluateLinksAtPriority(ConditionPriority priority, DialogueEntry entry, List<Response> npcResponses, List<Response> pcResponses, List<DialogueEntry> visited, bool stopAtFirstValid = false, bool skipExecution = false)
	{
		if (entry == null)
		{
			return;
		}
		for (int i = 0; i < entry.outgoingLinks.Count; i++)
		{
			Link link = entry.outgoingLinks[i];
			DialogueEntry dialogueEntry = m_database.GetDialogueEntry(link);
			if (dialogueEntry == null || link.priority != priority)
			{
				continue;
			}
			CharacterType characterType = m_database.GetCharacterType(dialogueEntry.ActorID);
			Lua.Run("thisID = " + dialogueEntry.id);
			bool flag = Lua.IsTrue(dialogueEntry.conditionsString, DialogueDebug.logInfo, m_allowLuaExceptions) && (isDialogueEntryValid == null || isDialogueEntryValid(dialogueEntry));
			if (flag || (m_includeInvalidEntries && characterType == CharacterType.PC))
			{
				if (dialogueEntry.isGroup)
				{
					if (DialogueDebug.logInfo)
					{
						Debug.Log(string.Format("{0}: Evaluate Group ({1}): ID={2}:{3} '{4}' ({5})", "Dialogue System", GetActorName(m_database.GetActor(dialogueEntry.ActorID)), link.destinationConversationID, link.destinationDialogueID, dialogueEntry.Title, flag));
					}
					if (!skipExecution)
					{
						Lua.Run(dialogueEntry.userScript, DialogueDebug.logInfo, m_allowLuaExceptions);
						dialogueEntry.onExecute.Invoke();
					}
					flag = false;
					for (int num = 4; num >= 0; num--)
					{
						int num2 = npcResponses.Count + pcResponses.Count;
						EvaluateLinksAtPriority((ConditionPriority)num, dialogueEntry, npcResponses, pcResponses, visited, stopAtFirstValid, skipExecution);
						if (npcResponses.Count + pcResponses.Count > num2)
						{
							flag = true;
							break;
						}
					}
				}
				else
				{
					if (DialogueDebug.logInfo)
					{
						Debug.Log(string.Format("{0}: Add Link ({1}): ID={2}:{3} '{4}' ({5})", "Dialogue System", GetActorName(m_database.GetActor(dialogueEntry.ActorID)), link.destinationConversationID, link.destinationDialogueID, GetLinkText(characterType, dialogueEntry), flag));
					}
					if (characterType == CharacterType.NPC)
					{
						npcResponses.Add(new Response(FormattedText.Parse(dialogueEntry.subtitleText, m_database.emphasisSettings), dialogueEntry, flag));
					}
					else
					{
						string text = dialogueEntry.responseButtonText;
						if (m_emTagForOldResponses != EmTag.None && string.Equals(Lua.Run(string.Format("return Conversation[{0}].Dialog[{1}].SimStatus", new object[2] { dialogueEntry.conversationID, dialogueEntry.id })).asString, "WasDisplayed"))
						{
							text = UITools.StripEmTags(text);
							text = string.Format("[em{0}]{1}[/em{0}]", (int)m_emTagForOldResponses, text);
						}
						if (m_emTagForInvalidResponses != EmTag.None && !flag)
						{
							text = UITools.StripEmTags(text);
							text = string.Format("[em{0}]{1}[/em{0}]", (int)m_emTagForInvalidResponses, text);
						}
						FormattedText formattedText = FormattedText.Parse(text, m_database.emphasisSettings);
						if (!flag)
						{
							formattedText.forceAuto = false;
							formattedText.forceMenu = false;
						}
						pcResponses.Add(new Response(formattedText, dialogueEntry, flag));
						DialogueLua.MarkDialogueEntryOffered(dialogueEntry);
					}
				}
				if (flag && stopAtFirstValid)
				{
					break;
				}
			}
			else if (LinkUtility.IsPassthroughOnFalse(dialogueEntry))
			{
				if (DialogueDebug.logInfo)
				{
					Debug.Log(string.Format("{0}: Passthrough on False Link ({1}): ID={2}:{3} '{4}' Condition='{5}'", "Dialogue System", GetActorName(m_database.GetActor(dialogueEntry.ActorID)), link.destinationConversationID, link.destinationDialogueID, GetLinkText(characterType, dialogueEntry), dialogueEntry.conditionsString));
				}
				List<Response> list = new List<Response>();
				List<Response> list2 = new List<Response>();
				EvaluateLinks(dialogueEntry, list, list2, visited);
				npcResponses.AddRange(list);
				pcResponses.AddRange(list2);
			}
			else if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Block on False Link ({1}): ID={2}:{3} '{4}' Condition='{5}'", "Dialogue System", GetActorName(m_database.GetActor(dialogueEntry.ActorID)), link.destinationConversationID, link.destinationDialogueID, GetLinkText(characterType, dialogueEntry), dialogueEntry.conditionsString));
			}
		}
	}

	private string GetActorName(Actor actor)
	{
		if (actor == null)
		{
			return "null";
		}
		return actor.Name;
	}

	private string GetLinkText(CharacterType characterType, DialogueEntry entry)
	{
		if (characterType != CharacterType.NPC)
		{
			return entry.responseButtonText;
		}
		return entry.subtitleText;
	}

	public void SetParticipants(Conversation conversation, Transform actor, Transform conversant)
	{
		m_characterInfoCache.Clear();
		m_actorInfo = GetCharacterInfo(conversation.ActorID, actor);
		m_conversantInfo = GetCharacterInfo(conversation.ConversantID, conversant);
		if (m_actorInfo != null)
		{
			m_characterInfoCache[m_actorInfo.id] = m_actorInfo;
		}
		if (m_conversantInfo != null)
		{
			m_characterInfoCache[m_conversantInfo.id] = m_conversantInfo;
		}
		DialogueLua.SetParticipants(m_actorInfo.Name, m_conversantInfo.Name, m_actorInfo.nameInDatabase, m_conversantInfo.nameInDatabase);
		IdentifyPCPortrait(conversation);
	}

	public void UpdateParticipantsOnLinkedConversation(int newConversationID)
	{
		Conversation conversation = m_database.GetConversation(newConversationID);
		if (conversation == null)
		{
			return;
		}
		int actorID = conversation.ActorID;
		if (actorID != m_actorInfo.id)
		{
			Actor actor = m_database.GetActor(actorID);
			if (actor != null && actor.IsPlayer)
			{
				m_actorInfo = GetCharacterInfo(actorID);
				IdentifyPCPortrait(conversation);
				return;
			}
		}
		int conversantID = conversation.ConversantID;
		if (conversantID != m_conversantInfo.id)
		{
			Actor actor2 = m_database.GetActor(conversantID);
			if (actor2 != null && actor2.IsPlayer)
			{
				m_conversantInfo = GetCharacterInfo(conversantID);
				IdentifyPCPortrait(conversation);
			}
		}
	}

	private void IdentifyPCPortrait(Conversation conversation)
	{
		if (conversation == null)
		{
			return;
		}
		if (m_actorInfo.isPlayer)
		{
			pcPortraitName = m_actorInfo.Name;
			pcPortraitSprite = m_actorInfo.portrait;
			return;
		}
		if (m_conversantInfo.isPlayer)
		{
			pcPortraitName = m_conversantInfo.Name;
			pcPortraitSprite = m_conversantInfo.portrait;
			return;
		}
		for (int i = 0; i < conversation.dialogueEntries.Count; i++)
		{
			DialogueEntry dialogueEntry = conversation.dialogueEntries[i];
			Actor actor = m_database.GetActor(dialogueEntry.ActorID);
			if (actor != null && actor.IsPlayer)
			{
				pcPortraitName = actor.Name;
				actor.AssignPortraitSprite(delegate(Sprite sprite)
				{
					pcPortraitSprite = sprite;
				});
				break;
			}
		}
	}

	public void OverrideCharacterInfo(int id, Transform character)
	{
		m_characterInfoCache.Remove(id);
		GetCharacterInfo(id, character);
	}

	public CharacterInfo GetCharacterInfo(int id, Transform character)
	{
		if (!m_characterInfoCache.ContainsKey(id))
		{
			Actor actor = null;
			DialogueActor dialogueActorComponent = DialogueActor.GetDialogueActorComponent(character);
			if (dialogueActorComponent != null)
			{
				actor = m_database.GetActor(dialogueActorComponent.actor);
			}
			if (actor == null)
			{
				actor = m_database.GetActor(id);
			}
			string text = ((dialogueActorComponent != null) ? dialogueActorComponent.actor : string.Empty);
			if (string.IsNullOrEmpty(text) && actor != null)
			{
				text = actor.Name;
			}
			if (character == null && !string.IsNullOrEmpty(text))
			{
				character = CharacterInfo.GetRegisteredActorTransform(text);
			}
			int id2 = actor?.id ?? id;
			Sprite sprite = ((dialogueActorComponent != null) ? dialogueActorComponent.GetPortraitSprite() : null);
			if (sprite == null)
			{
				sprite = GetPortrait(character, actor);
			}
			CharacterInfo characterInfo = new CharacterInfo(id2, text, character, m_database.GetCharacterType(id), sprite);
			if (characterInfo.portrait == null)
			{
				actor?.AssignPortraitSprite(delegate(Sprite portrait)
				{
					characterInfo.portrait = portrait;
				});
			}
			if (id == -1)
			{
				return characterInfo;
			}
			m_characterInfoCache.Add(id, characterInfo);
		}
		return m_characterInfoCache[id];
	}

	public CharacterInfo GetCharacterInfo(int id)
	{
		return GetCharacterInfo(id, GetCharacterTransform(id));
	}

	private Transform GetCharacterTransform(int id)
	{
		if (id == m_actorInfo.id)
		{
			return m_actorInfo.transform;
		}
		if (id == m_conversantInfo.id)
		{
			return m_conversantInfo.transform;
		}
		return null;
	}

	private Sprite GetPortrait(Transform character, Actor actor)
	{
		Sprite sprite = null;
		if (character != null)
		{
			sprite = GetPortraitByActorName(DialogueActor.GetActorName(character), actor);
		}
		if (sprite == null && actor != null)
		{
			sprite = GetPortraitByActorName(actor.Name, actor);
			if (sprite == null)
			{
				sprite = actor.GetPortraitSprite();
			}
		}
		return sprite;
	}

	private Sprite GetPortraitByActorName(string actorName, Actor actor)
	{
		DialogueDebug.DebugLevel level = DialogueDebug.level;
		DialogueDebug.level = DialogueDebug.DebugLevel.Warning;
		string asString = DialogueLua.GetActorField(actorName, "Current Portrait").asString;
		DialogueDebug.level = level;
		if (string.IsNullOrEmpty(asString))
		{
			return actor?.GetPortraitSprite();
		}
		if (asString.StartsWith("pic="))
		{
			return actor?.GetPortraitSprite(Tools.StringToInt(asString.Substring("pic=".Length)));
		}
		Sprite portraitSprite = actor.GetPortraitSprite(asString);
		if (!(portraitSprite != null))
		{
			return UITools.CreateSprite(DialogueManager.LoadAsset(asString) as Texture2D);
		}
		return portraitSprite;
	}

	public void SetActorPortraitSprite(string actorName, Sprite sprite)
	{
		foreach (CharacterInfo value in m_characterInfoCache.Values)
		{
			if (string.Equals(value.Name, actorName) || string.Equals(value.nameInDatabase, actorName))
			{
				value.portrait = sprite;
			}
		}
	}

	public string GetPCName()
	{
		if (m_database.IsPlayerID(m_actorInfo.id))
		{
			return m_actorInfo.Name;
		}
		if (m_database.IsPlayerID(m_conversantInfo.id))
		{
			return m_conversantInfo.Name;
		}
		return pcPortraitName;
	}

	public Sprite GetPCSprite()
	{
		if (m_database.IsPlayerID(m_actorInfo.id))
		{
			return m_actorInfo.portrait;
		}
		if (m_database.IsPlayerID(m_conversantInfo.id))
		{
			return m_conversantInfo.portrait;
		}
		return pcPortraitSprite;
	}
}
