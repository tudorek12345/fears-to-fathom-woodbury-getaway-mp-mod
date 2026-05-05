using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public class ConversationController
{
	public delegate void EndConversationDelegate(ConversationController ConversationController);

	private ConversationModel m_model;

	private ConversationView m_view;

	private ConversationState m_state;

	private EndConversationDelegate m_endConversationHandler;

	private int m_currentConversationID;

	private Response m_currentResponse;

	private static int _frameLastConversationEnded = -1;

	public bool isActive { get; private set; }

	public CharacterInfo actorInfo
	{
		get
		{
			if (m_model == null)
			{
				return null;
			}
			return m_model.actorInfo;
		}
	}

	public ConversationModel conversationModel => m_model;

	public ConversationView conversationView => m_view;

	public ConversationState currentState => m_state;

	public ActiveConversationRecord activeConversationRecord { get; set; }

	public IsDialogueEntryValidDelegate isDialogueEntryValid
	{
		get
		{
			return m_model.isDialogueEntryValid;
		}
		set
		{
			m_model.isDialogueEntryValid = value;
		}
	}

	public bool randomizeNextEntry { get; set; }

	public CharacterInfo conversantInfo
	{
		get
		{
			if (m_model == null)
			{
				return null;
			}
			return m_model.conversantInfo;
		}
	}

	public bool IsActive
	{
		get
		{
			return isActive;
		}
		private set
		{
			isActive = value;
		}
	}

	public CharacterInfo ActorInfo => actorInfo;

	public ConversationModel ConversationModel => conversationModel;

	public ConversationView ConversationView => conversationView;

	public IsDialogueEntryValidDelegate IsDialogueEntryValid
	{
		get
		{
			return isDialogueEntryValid;
		}
		set
		{
			isDialogueEntryValid = value;
		}
	}

	public CharacterInfo ConversantInfo => conversantInfo;

	public static int frameLastConversationEnded => _frameLastConversationEnded;

	public ConversationController()
	{
	}

	public ConversationController(ConversationModel model, ConversationView view, bool alwaysForceResponseMenu, EndConversationDelegate endConversationHandler)
	{
		isActive = true;
		m_model = model;
		m_view = view;
		m_endConversationHandler = endConversationHandler;
		randomizeNextEntry = false;
		DialogueManager.instance.currentConversationState = model.firstState;
		model.InformParticipants("OnConversationStart");
		view.FinishedSubtitleHandler += OnFinishedSubtitle;
		view.SelectedResponseHandler += OnSelectedResponse;
		m_currentConversationID = model.GetConversationID(model.firstState);
		SetConversationOverride(model.firstState);
		GotoState(model.firstState);
	}

	public void Initialize(ConversationModel model, ConversationView view, bool alwaysForceResponseMenu, EndConversationDelegate endConversationHandler)
	{
		isActive = true;
		m_model = model;
		m_view = view;
		m_endConversationHandler = endConversationHandler;
		randomizeNextEntry = false;
		DialogueManager.instance.currentConversationState = model.firstState;
		model.InformParticipants("OnConversationStart");
		view.FinishedSubtitleHandler += OnFinishedSubtitle;
		view.SelectedResponseHandler += OnSelectedResponse;
		m_currentConversationID = model.GetConversationID(model.firstState);
		SetConversationOverride(model.firstState);
		GotoState(model.firstState);
	}

	public void Close()
	{
		if (isActive)
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Ending conversation.", new object[1] { "Dialogue System" }));
			}
			isActive = false;
			_frameLastConversationEnded = Time.frameCount;
			m_view.displaySettings.conversationOverrideSettings = null;
			m_view.FinishedSubtitleHandler -= OnFinishedSubtitle;
			m_view.SelectedResponseHandler -= OnSelectedResponse;
			m_view.Close();
			DialogueManager.instance.lastConversationEnded = m_model.conversationTitle;
			m_model.InformParticipants("OnConversationEnd", informDialogueManager: true);
			if (m_endConversationHandler != null)
			{
				m_endConversationHandler(this);
			}
			DialogueManager.instance.currentConversationState = null;
		}
	}

	public void GotoState(ConversationState state)
	{
		m_state = state;
		DialogueManager.instance.currentConversationState = state;
		if (state != null)
		{
			if (state.subtitle != null)
			{
				state.subtitle.activeConversationRecord = activeConversationRecord;
			}
			int conversationID = m_model.GetConversationID(state);
			if (conversationID != m_currentConversationID)
			{
				m_currentConversationID = conversationID;
				m_model.InformParticipants("OnLinkedConversationStart", informDialogueManager: true);
				m_model.UpdateParticipantsOnLinkedConversation(conversationID);
				m_view.SetPCPortrait(m_model.GetPCSprite(), m_model.GetPCName());
				SetConversationOverride(state);
			}
			if (state.isGroup)
			{
				m_view.ShowLastNPCSubtitle();
				return;
			}
			AnalyzePCResponses(state, out var isPCResponseMenuNext, out var isPCAutoResponseNext);
			m_view.StartSubtitle(state.subtitle, isPCResponseMenuNext, isPCAutoResponseNext);
		}
		else
		{
			Close();
		}
	}

	private void AnalyzePCResponses(ConversationState state, out bool isPCResponseMenuNext, out bool isPCAutoResponseNext)
	{
		bool alwaysForceResponseMenu = m_view.displaySettings.GetAlwaysForceResponseMenu();
		bool flag = false;
		bool flag2 = false;
		int num = ((state.pcResponses != null) ? state.pcResponses.Length : 0);
		for (int i = 0; i < num; i++)
		{
			if (state.pcResponses[i].formattedText.forceMenu)
			{
				flag = true;
			}
			if (state.pcResponses[i].formattedText.forceAuto)
			{
				flag2 = true;
				break;
			}
		}
		isPCResponseMenuNext = !state.hasNPCResponse && !flag2 && (num > 1 || flag || (num == 1 && alwaysForceResponseMenu && !string.IsNullOrEmpty(state.pcResponses[0].formattedText.text)));
		isPCAutoResponseNext = (!state.hasNPCResponse && flag2) || (num == 1 && string.IsNullOrEmpty(state.pcResponses[0].formattedText.text)) || (num == 1 && !flag && (!alwaysForceResponseMenu || state.pcResponses[0].destinationEntry.isGroup));
	}

	private void SetConversationOverride(ConversationState state)
	{
		m_view.displaySettings.conversationOverrideSettings = m_model.GetConversationOverrideSettings(state);
		DialogueManager.displaySettings.conversationOverrideSettings = m_view.displaySettings.conversationOverrideSettings;
	}

	public void OnFinishedSubtitle(object sender, EventArgs e)
	{
		DialogueManager.instance.activeConversation = activeConversationRecord;
		bool flag = randomizeNextEntry;
		randomizeNextEntry = false;
		if (m_state.hasNPCResponse)
		{
			GotoState(m_model.GetState(flag ? m_state.GetRandomNPCEntry() : m_state.firstNPCResponse.destinationEntry));
		}
		else if (m_state.hasPCResponses)
		{
			AnalyzePCResponses(m_state, out var _, out var isPCAutoResponseNext);
			if (isPCAutoResponseNext)
			{
				GotoState(m_model.GetState(m_state.pcAutoResponse.destinationEntry));
			}
			else
			{
				m_view.StartResponses(m_state.subtitle, m_state.pcResponses);
			}
		}
		else
		{
			Close();
		}
	}

	public void OnSelectedResponse(object sender, SelectedResponseEventArgs e)
	{
		DialogueManager.instance.activeConversation = activeConversationRecord;
		GotoState(m_model.GetState(e.DestinationEntry));
	}

	public void GotoFirstResponse()
	{
		if (m_state != null && m_state.pcResponses.Length != 0)
		{
			m_view.SelectResponse(new SelectedResponseEventArgs(m_state.pcResponses[0]));
		}
	}

	public void GotoLastResponse()
	{
		if (m_state != null && m_state.pcResponses.Length != 0)
		{
			m_view.SelectResponse(new SelectedResponseEventArgs(m_state.pcResponses[m_state.pcResponses.Length - 1]));
		}
	}

	public void GotoRandomResponse()
	{
		if (m_state != null && m_state.pcResponses.Length != 0)
		{
			m_view.SelectResponse(new SelectedResponseEventArgs(m_state.pcResponses[UnityEngine.Random.Range(0, m_state.pcResponses.Length)]));
		}
	}

	public void GotoCurrentResponse()
	{
		if (m_currentResponse != null)
		{
			m_view.SelectResponse(new SelectedResponseEventArgs(m_currentResponse));
		}
		else
		{
			GotoFirstResponse();
		}
	}

	public void SetCurrentResponse(Response response)
	{
		m_currentResponse = response;
	}

	public void UpdateResponses()
	{
		if (m_state != null)
		{
			m_model.UpdateResponses(m_state);
			OnFinishedSubtitle(this, EventArgs.Empty);
		}
	}

	public void SetActorPortraitSprite(string actorName, Sprite sprite)
	{
		m_model.SetActorPortraitSprite(actorName, sprite);
		m_view.SetActorPortraitSprite(actorName, sprite);
	}
}
