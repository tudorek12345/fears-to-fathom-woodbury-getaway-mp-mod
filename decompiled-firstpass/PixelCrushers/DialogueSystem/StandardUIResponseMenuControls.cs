using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class StandardUIResponseMenuControls : AbstractUIResponseMenuControls
{
	public Action timeoutHandler;

	protected List<StandardUIMenuPanel> m_builtinPanels = new List<StandardUIMenuPanel>();

	protected StandardUIMenuPanel m_defaultPanel;

	protected Dictionary<Transform, StandardUIMenuPanel> m_actorPanelCache = new Dictionary<Transform, StandardUIMenuPanel>();

	protected Dictionary<int, StandardUIMenuPanel> m_actorIdPanelCache = new Dictionary<int, StandardUIMenuPanel>();

	protected StandardUIMenuPanel m_currentPanel;

	protected StandardUIMenuPanel m_forcedOverridePanel;

	protected Sprite m_pcPortraitSprite;

	protected string m_pcPortraitName;

	protected bool useFirstResponseForPortrait;

	public override AbstractUISubtitleControls subtitleReminderControls => null;

	public StandardUIMenuPanel defaultPanel
	{
		get
		{
			return m_defaultPanel;
		}
		set
		{
			m_defaultPanel = value;
		}
	}

	public void Initialize(StandardUIMenuPanel[] menuPanels, StandardUIMenuPanel defaultMenuPanel, bool useFirstResponseForMenuPortrait)
	{
		m_builtinPanels.Clear();
		m_builtinPanels.AddRange(menuPanels);
		m_defaultPanel = ((defaultMenuPanel != null) ? defaultMenuPanel : ((m_builtinPanels.Count > 0) ? m_builtinPanels[0] : null));
		ClearCache();
		if (timeoutHandler == null)
		{
			timeoutHandler = DefaultTimeoutHandler;
		}
		useFirstResponseForPortrait = useFirstResponseForMenuPortrait;
	}

	public void ClearCache()
	{
		m_actorPanelCache.Clear();
		m_actorIdPanelCache.Clear();
	}

	public virtual void SetActorMenuPanelNumber(DialogueActor dialogueActor, MenuPanelNumber menuPanelNumber)
	{
		if (!(dialogueActor == null))
		{
			OverrideActorMenuPanel(dialogueActor.transform, menuPanelNumber, dialogueActor.standardDialogueUISettings.customMenuPanel);
		}
	}

	public void ForceOverrideMenuPanel(StandardUIMenuPanel panel)
	{
		m_forcedOverridePanel = panel;
	}

	public void OverrideActorMenuPanel(Transform actorTransform, MenuPanelNumber menuPanelNumber, StandardUIMenuPanel customPanel)
	{
		if (!(actorTransform == null))
		{
			m_actorPanelCache[actorTransform] = GetPanelFromNumber(menuPanelNumber, customPanel);
		}
	}

	public void OverrideActorMenuPanel(Actor actor, MenuPanelNumber menuPanelNumber, StandardUIMenuPanel customPanel)
	{
		if (actor != null)
		{
			m_actorIdPanelCache[actor.id] = GetPanelFromNumber(menuPanelNumber, customPanel);
		}
	}

	protected Transform GetActorTransformFromID(int actorID)
	{
		Actor actor = DialogueManager.masterDatabase.GetActor(actorID);
		if (actor != null)
		{
			Transform transform = CharacterInfo.GetRegisteredActorTransform(actor.Name);
			if (transform == null)
			{
				GameObject gameObject = GameObject.Find(actor.Name);
				if (gameObject != null)
				{
					transform = gameObject.transform;
				}
			}
			if (transform != null)
			{
				return transform;
			}
		}
		return DialogueManager.currentActor;
	}

	public virtual StandardUIMenuPanel GetPanel(Subtitle lastSubtitle, Response[] responses)
	{
		if (m_forcedOverridePanel != null)
		{
			return m_forcedOverridePanel;
		}
		Transform transform = ((lastSubtitle != null && lastSubtitle.speakerInfo.isPlayer) ? lastSubtitle.speakerInfo.transform : null);
		if (transform == null)
		{
			transform = ((!useFirstResponseForPortrait) ? ((lastSubtitle != null && lastSubtitle.listenerInfo.isPlayer) ? lastSubtitle.listenerInfo.transform : ((responses != null && responses.Length != 0) ? GetActorTransformFromID(responses[0].destinationEntry.ActorID) : DialogueManager.currentActor)) : ((responses != null && responses.Length != 0) ? GetActorTransformFromID(responses[0].destinationEntry.ActorID) : ((lastSubtitle != null && lastSubtitle.listenerInfo.isPlayer) ? lastSubtitle.listenerInfo.transform : DialogueManager.currentActor)));
		}
		if (transform == null)
		{
			transform = DialogueManager.currentActor;
		}
		DialogueActor dialogueActorComponent = DialogueActor.GetDialogueActorComponent(transform);
		bool flag = dialogueActorComponent != null && dialogueActorComponent.standardDialogueUISettings.menuPanelNumber == MenuPanelNumber.Default;
		Transform transform2 = ((lastSubtitle != null && lastSubtitle.speakerInfo.isNPC) ? lastSubtitle.speakerInfo.transform : ((lastSubtitle != null) ? lastSubtitle.listenerInfo.transform : DialogueManager.currentConversant));
		if (transform2 == null)
		{
			transform2 = DialogueManager.currentConversant;
		}
		if (flag && transform2 != null && m_actorPanelCache.ContainsKey(transform2))
		{
			return m_actorPanelCache[transform2];
		}
		DialogueActor dialogueActorComponent2 = DialogueActor.GetDialogueActorComponent(transform2);
		if (dialogueActorComponent2 != null && (dialogueActorComponent2.standardDialogueUISettings.useMenuPanelFor == DialogueActor.UseMenuPanelFor.MeAndResponsesToMe || (dialogueActorComponent2.standardDialogueUISettings.menuPanelNumber != MenuPanelNumber.Default && flag)))
		{
			StandardUIMenuPanel dialogueActorPanel = GetDialogueActorPanel(dialogueActorComponent2);
			if (dialogueActorPanel != null)
			{
				m_actorPanelCache[transform2] = dialogueActorPanel;
				return dialogueActorPanel;
			}
		}
		if (transform != null && m_actorPanelCache.ContainsKey(transform))
		{
			StandardUIMenuPanel standardUIMenuPanel = m_actorPanelCache[transform];
			if (standardUIMenuPanel != m_defaultPanel)
			{
				return standardUIMenuPanel;
			}
		}
		int key = ((lastSubtitle != null && lastSubtitle.speakerInfo.isPlayer) ? lastSubtitle.speakerInfo.id : ((responses != null && responses.Length != 0) ? responses[0].destinationEntry.ActorID : (-1)));
		if (m_actorIdPanelCache.ContainsKey(key))
		{
			return m_actorIdPanelCache[key];
		}
		StandardUIMenuPanel dialogueActorPanel2 = GetDialogueActorPanel(dialogueActorComponent);
		if (dialogueActorPanel2 == null)
		{
			dialogueActorPanel2 = m_defaultPanel;
		}
		if (transform != null)
		{
			m_actorPanelCache[transform] = dialogueActorPanel2;
		}
		return dialogueActorPanel2;
	}

	protected StandardUIMenuPanel GetDialogueActorPanel(DialogueActor dialogueActor)
	{
		if (dialogueActor == null)
		{
			return null;
		}
		return GetPanelFromNumber(dialogueActor.standardDialogueUISettings.menuPanelNumber, dialogueActor.standardDialogueUISettings.customMenuPanel);
	}

	protected StandardUIMenuPanel GetPanelFromNumber(MenuPanelNumber menuPanelNumber, StandardUIMenuPanel customMenuPanel)
	{
		switch (menuPanelNumber)
		{
		case MenuPanelNumber.Default:
			return m_defaultPanel;
		case MenuPanelNumber.Custom:
			return customMenuPanel;
		default:
		{
			int menuPanelIndex = PanelNumberUtility.GetMenuPanelIndex(menuPanelNumber);
			if (0 > menuPanelIndex || menuPanelIndex >= m_builtinPanels.Count)
			{
				return null;
			}
			return m_builtinPanels[menuPanelIndex];
		}
		}
	}

	public override void SetPCPortrait(Sprite portraitSprite, string portraitName)
	{
		m_pcPortraitSprite = portraitSprite;
		m_pcPortraitName = portraitName;
	}

	public override void SetActorPortraitSprite(string actorName, Sprite portraitSprite)
	{
		if (string.Equals(actorName, m_pcPortraitName))
		{
			Sprite validPortraitSprite = AbstractDialogueUI.GetValidPortraitSprite(actorName, portraitSprite);
			m_pcPortraitSprite = portraitSprite;
			if (m_currentPanel != null && (UnityEngine.Object)(object)m_currentPanel.pcImage != null && DialogueManager.masterDatabase.IsPlayer(actorName))
			{
				m_currentPanel.pcImage.sprite = validPortraitSprite;
			}
		}
	}

	protected override void ClearResponseButtons()
	{
	}

	protected override void SetResponseButtons(Response[] responses, Transform target)
	{
	}

	public override void SetActive(bool value)
	{
		if (!value && m_currentPanel != null)
		{
			m_currentPanel.HideResponses();
		}
	}

	public override void ShowResponses(Subtitle lastSubtitle, Response[] responses, Transform target)
	{
		StandardUIMenuPanel panel = GetPanel(lastSubtitle, responses);
		if (panel == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning("Dialogue System: Can't find menu panel.");
			}
			return;
		}
		m_currentPanel = panel;
		if (useFirstResponseForPortrait && responses.Length != 0)
		{
			CharacterInfo characterInfo = DialogueManager.conversationModel.GetCharacterInfo(responses[0].destinationEntry.ActorID);
			if (characterInfo != null)
			{
				m_pcPortraitName = characterInfo.Name;
				m_pcPortraitSprite = characterInfo.portrait;
			}
		}
		panel.SetPCPortrait(m_pcPortraitSprite, m_pcPortraitName);
		panel.ShowResponses(lastSubtitle, responses, target);
	}

	public virtual void MakeButtonsNonclickable()
	{
		if (m_currentPanel != null)
		{
			m_currentPanel.MakeButtonsNonclickable();
		}
	}

	public void Close()
	{
		for (int i = 0; i < m_builtinPanels.Count; i++)
		{
			if (m_builtinPanels[i] != null)
			{
				m_builtinPanels[i].Close();
			}
		}
		if (m_defaultPanel != null && !m_builtinPanels.Contains(m_defaultPanel))
		{
			m_defaultPanel.Close();
		}
		foreach (KeyValuePair<Transform, StandardUIMenuPanel> item in m_actorPanelCache)
		{
			StandardUIMenuPanel value = item.Value;
			if (value != null && !m_builtinPanels.Contains(value))
			{
				value.Close();
			}
		}
		if (m_actorIdPanelCache.Count <= 0)
		{
			return;
		}
		List<StandardUIMenuPanel> list = new List<StandardUIMenuPanel>(m_actorIdPanelCache.Values);
		foreach (KeyValuePair<int, StandardUIMenuPanel> item2 in m_actorIdPanelCache)
		{
			StandardUIMenuPanel value2 = item2.Value;
			if (value2 != null && !m_builtinPanels.Contains(value2) && !list.Contains(value2))
			{
				value2.Close();
			}
		}
	}

	public bool AreAnyPanelsClosing()
	{
		for (int i = 0; i < m_builtinPanels.Count; i++)
		{
			if (m_builtinPanels[i] != null && m_builtinPanels[i].panelState == UIPanel.PanelState.Closing)
			{
				return true;
			}
		}
		if (m_defaultPanel != null && !m_builtinPanels.Contains(m_defaultPanel) && m_defaultPanel.panelState == UIPanel.PanelState.Closing)
		{
			return true;
		}
		foreach (KeyValuePair<Transform, StandardUIMenuPanel> item in m_actorPanelCache)
		{
			StandardUIMenuPanel value = item.Value;
			if (value != null && !m_builtinPanels.Contains(value) && value.panelState == UIPanel.PanelState.Closing)
			{
				return true;
			}
		}
		if (m_actorIdPanelCache.Count > 0)
		{
			List<StandardUIMenuPanel> list = new List<StandardUIMenuPanel>(m_actorIdPanelCache.Values);
			foreach (KeyValuePair<int, StandardUIMenuPanel> item2 in m_actorIdPanelCache)
			{
				StandardUIMenuPanel value2 = item2.Value;
				if (value2 != null && !m_builtinPanels.Contains(value2) && !list.Contains(value2) && value2.panelState == UIPanel.PanelState.Closing)
				{
					return true;
				}
			}
		}
		return false;
	}

	public override void StartTimer(float timeout)
	{
		if (m_currentPanel != null)
		{
			m_currentPanel.StartTimer(timeout, timeoutHandler);
		}
	}

	public void DefaultTimeoutHandler()
	{
		DialogueManager.instance.SendMessage("OnConversationTimeout");
	}
}
