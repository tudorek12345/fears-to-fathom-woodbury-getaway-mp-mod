using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class StandardUISubtitleControls : AbstractUISubtitleControls
{
	private List<StandardUISubtitlePanel> m_builtinPanels = new List<StandardUISubtitlePanel>();

	private List<StandardUISubtitlePanel> m_customPanels = new List<StandardUISubtitlePanel>();

	private StandardUISubtitlePanel m_defaultNPCPanel;

	private StandardUISubtitlePanel m_defaultPCPanel;

	private StandardUISubtitlePanel m_forcedOverridePanel;

	private StandardUISubtitlePanel m_focusedPanel;

	private Dictionary<Transform, StandardUISubtitlePanel> m_actorPanelCache = new Dictionary<Transform, StandardUISubtitlePanel>();

	private Dictionary<int, StandardUISubtitlePanel> m_actorIdOverridePanel = new Dictionary<int, StandardUISubtitlePanel>();

	private Dictionary<int, StandardUISubtitlePanel> m_lastPanelUsedByActor = new Dictionary<int, StandardUISubtitlePanel>();

	private Dictionary<StandardUISubtitlePanel, int> m_lastActorToUsePanel = new Dictionary<StandardUISubtitlePanel, int>();

	private Dictionary<Transform, DialogueActor> m_dialogueActorCache = new Dictionary<Transform, DialogueActor>();

	private List<Transform> m_useBarkUIs = new List<Transform>();

	private List<string> m_queuedActorGOs;

	private List<SubtitlePanelNumber> m_queuedActorGOPanels;

	private List<int> m_queuedActorIDs;

	private List<SubtitlePanelNumber> m_queuedActorIDPanels;

	public StandardUISubtitlePanel defaultNPCPanel
	{
		get
		{
			return m_defaultNPCPanel;
		}
		set
		{
			m_defaultNPCPanel = value;
		}
	}

	public StandardUISubtitlePanel defaultPCPanel
	{
		get
		{
			return m_defaultPCPanel;
		}
		set
		{
			m_defaultPCPanel = value;
		}
	}

	public override bool hasText
	{
		get
		{
			if (m_focusedPanel != null)
			{
				return !string.IsNullOrEmpty(m_focusedPanel.subtitleText.text);
			}
			return false;
		}
	}

	public void Initialize(StandardUISubtitlePanel[] subtitlePanels, StandardUISubtitlePanel defaultNPCSubtitlePanel, StandardUISubtitlePanel defaultPCSubtitlePanel)
	{
		m_builtinPanels.Clear();
		m_builtinPanels.AddRange(subtitlePanels);
		m_defaultNPCPanel = ((defaultNPCSubtitlePanel != null) ? defaultNPCSubtitlePanel : ((m_builtinPanels.Count > 0) ? m_builtinPanels[0] : null));
		m_defaultPCPanel = ((defaultPCSubtitlePanel != null) ? defaultPCSubtitlePanel : ((m_builtinPanels.Count > 0) ? m_builtinPanels[0] : null));
		if (m_defaultNPCPanel != null)
		{
			m_defaultNPCPanel.isDefaultNPCPanel = true;
		}
		if (m_defaultPCPanel != null)
		{
			m_defaultPCPanel.isDefaultPCPanel = true;
		}
		for (int i = 0; i < m_builtinPanels.Count; i++)
		{
			if (m_builtinPanels[i] != null)
			{
				m_builtinPanels[i].panelNumber = i;
			}
		}
		ClearCache();
	}

	public void ClearCache()
	{
		m_actorPanelCache.Clear();
		m_customPanels.Clear();
		m_actorIdOverridePanel.Clear();
		m_lastPanelUsedByActor.Clear();
		m_lastActorToUsePanel.Clear();
		m_dialogueActorCache.Clear();
		m_useBarkUIs.Clear();
	}

	public void ClearOverrideCache()
	{
		m_actorPanelCache.Clear();
		m_customPanels.Clear();
	}

	public void ForceOverrideSubtitlePanel(StandardUISubtitlePanel customPanel)
	{
		m_forcedOverridePanel = customPanel;
	}

	public void OverrideActorPanel(Actor actor, SubtitlePanelNumber subtitlePanelNumber, StandardUISubtitlePanel customPanel = null, bool immediate = false)
	{
		if (actor == null)
		{
			return;
		}
		if (customPanel == null)
		{
			customPanel = (actor.IsPlayer ? m_defaultPCPanel : m_defaultNPCPanel);
		}
		StandardUISubtitlePanel panelFromNumber = GetPanelFromNumber(subtitlePanelNumber, customPanel);
		if (panelFromNumber == null)
		{
			m_actorIdOverridePanel.Remove(actor.id);
			return;
		}
		m_actorIdOverridePanel[actor.id] = panelFromNumber;
		if (!immediate)
		{
			return;
		}
		StandardUISubtitlePanel standardUISubtitlePanel = m_builtinPanels.Find((StandardUISubtitlePanel x) => x.isOpen && x.portraitActorName == actor.Name) ?? m_customPanels.Find((StandardUISubtitlePanel x) => x.isOpen && x.portraitActorName == actor.Name);
		if (!(standardUISubtitlePanel != panelFromNumber) || !(standardUISubtitlePanel != null))
		{
			return;
		}
		bool flag = (UnityEngine.Object)(object)standardUISubtitlePanel.continueButton != null && ((Component)(object)standardUISubtitlePanel.continueButton).gameObject.activeInHierarchy;
		string portraitActorName = standardUISubtitlePanel.portraitActorName;
		Sprite portraitImage = (((UnityEngine.Object)(object)standardUISubtitlePanel.portraitImage != null) ? standardUISubtitlePanel.portraitImage.sprite : null);
		bool hasFocus = standardUISubtitlePanel.hasFocus;
		if (standardUISubtitlePanel.subtitleText.gameObject != panelFromNumber.subtitleText.gameObject)
		{
			standardUISubtitlePanel.Close();
		}
		else
		{
			standardUISubtitlePanel.Unfocus();
			standardUISubtitlePanel.SetPortraitName(string.Empty);
			standardUISubtitlePanel.SetPortraitImage(null);
		}
		if (panelFromNumber.panelState != UIPanel.PanelState.Open)
		{
			panelFromNumber.Open();
			if (hasFocus)
			{
				panelFromNumber.Focus();
			}
			if (flag)
			{
				panelFromNumber.ShowContinueButton();
			}
		}
		panelFromNumber.SetPortraitName(portraitActorName);
		if ((UnityEngine.Object)(object)standardUISubtitlePanel.portraitImage != null)
		{
			panelFromNumber.SetPortraitImage(portraitImage);
		}
	}

	public void OverrideActorPanel(DialogueActor dialogueActor, SubtitlePanelNumber subtitlePanelNumber, StandardUISubtitlePanel customPanel = null)
	{
		if (!(dialogueActor == null))
		{
			Actor actor = DialogueManager.masterDatabase.GetActor(dialogueActor.actor);
			StandardUISubtitlePanel standardUISubtitlePanel = null;
			switch (subtitlePanelNumber)
			{
			case SubtitlePanelNumber.Default:
				standardUISubtitlePanel = ((actor != null && actor.IsPlayer) ? m_defaultPCPanel : m_defaultNPCPanel);
				break;
			default:
				standardUISubtitlePanel = GetPanelFromNumber(subtitlePanelNumber, customPanel);
				break;
			case SubtitlePanelNumber.UseBarkUI:
				break;
			}
			if (standardUISubtitlePanel == null)
			{
				m_actorPanelCache.Remove(dialogueActor.transform);
			}
			else
			{
				m_actorPanelCache[dialogueActor.transform] = standardUISubtitlePanel;
			}
			if (actor != null && m_actorIdOverridePanel.ContainsKey(actor.id))
			{
				m_actorIdOverridePanel.Remove(actor.id);
			}
		}
	}

	public virtual StandardUISubtitlePanel GetPanel(Subtitle subtitle, out DialogueActor dialogueActor)
	{
		dialogueActor = null;
		if (subtitle == null)
		{
			return m_defaultNPCPanel;
		}
		if (subtitle.speakerInfo.transform != null)
		{
			m_dialogueActorCache.TryGetValue(subtitle.speakerInfo.transform, out dialogueActor);
		}
		if (m_forcedOverridePanel != null)
		{
			return m_forcedOverridePanel;
		}
		int subtitlePanelNumber = subtitle.formattedText.subtitlePanelNumber;
		if (0 <= subtitlePanelNumber && subtitlePanelNumber < m_builtinPanels.Count)
		{
			StandardUISubtitlePanel standardUISubtitlePanel = m_builtinPanels[subtitlePanelNumber];
			standardUISubtitlePanel.actorOverridingPanel = subtitle.speakerInfo.transform;
			return standardUISubtitlePanel;
		}
		if (m_actorIdOverridePanel.ContainsKey(subtitle.speakerInfo.id))
		{
			StandardUISubtitlePanel standardUISubtitlePanel2 = m_actorIdOverridePanel[subtitle.speakerInfo.id];
			standardUISubtitlePanel2.actorOverridingPanel = subtitle.speakerInfo.transform;
			return standardUISubtitlePanel2;
		}
		Transform transform = subtitle.speakerInfo.transform;
		StandardUISubtitlePanel actorTransformPanel = GetActorTransformPanel(transform, subtitle.speakerInfo.isNPC ? m_defaultNPCPanel : m_defaultPCPanel, out dialogueActor);
		if (subtitle.speakerInfo.transform != null && dialogueActor != null)
		{
			m_dialogueActorCache[subtitle.speakerInfo.transform] = dialogueActor;
		}
		return actorTransformPanel;
	}

	public StandardUISubtitlePanel GetActorTransformPanel(Transform speakerTransform, StandardUISubtitlePanel defaultPanel, out DialogueActor dialogueActor)
	{
		dialogueActor = null;
		if (speakerTransform == null)
		{
			return defaultPanel;
		}
		if (m_dialogueActorCache.ContainsKey(speakerTransform))
		{
			dialogueActor = m_dialogueActorCache[speakerTransform];
		}
		else
		{
			dialogueActor = DialogueActor.GetDialogueActorComponent(speakerTransform);
			m_dialogueActorCache.Add(speakerTransform, dialogueActor);
		}
		if (m_actorPanelCache.ContainsKey(speakerTransform) && m_actorPanelCache[speakerTransform] != null)
		{
			return m_actorPanelCache[speakerTransform];
		}
		if (m_useBarkUIs.Contains(speakerTransform))
		{
			return null;
		}
		if (DialogueActorUsesBarkUI(dialogueActor))
		{
			m_useBarkUIs.Add(speakerTransform);
			return null;
		}
		StandardUISubtitlePanel standardUISubtitlePanel = GetDialogueActorPanel(dialogueActor);
		if (standardUISubtitlePanel == null)
		{
			standardUISubtitlePanel = defaultPanel;
		}
		m_actorPanelCache[speakerTransform] = standardUISubtitlePanel;
		m_useBarkUIs.Remove(speakerTransform);
		return standardUISubtitlePanel;
	}

	private bool DialogueActorUsesBarkUI(DialogueActor dialogueActor)
	{
		if (dialogueActor != null)
		{
			return dialogueActor.GetSubtitlePanelNumber() == SubtitlePanelNumber.UseBarkUI;
		}
		return false;
	}

	public StandardUISubtitlePanel GetDialogueActorPanel(DialogueActor dialogueActor)
	{
		if (dialogueActor == null)
		{
			return null;
		}
		return GetPanelFromNumber(dialogueActor.standardDialogueUISettings.subtitlePanelNumber, dialogueActor.standardDialogueUISettings.customSubtitlePanel);
	}

	public StandardUISubtitlePanel GetPanelFromNumber(SubtitlePanelNumber subtitlePanelNumber, StandardUISubtitlePanel customPanel)
	{
		switch (subtitlePanelNumber)
		{
		case SubtitlePanelNumber.Default:
			return null;
		case SubtitlePanelNumber.Custom:
			if (!m_customPanels.Contains(customPanel))
			{
				m_customPanels.Add(customPanel);
			}
			return customPanel;
		case SubtitlePanelNumber.UseBarkUI:
			return null;
		default:
		{
			int subtitlePanelIndex = PanelNumberUtility.GetSubtitlePanelIndex(subtitlePanelNumber);
			if (0 > subtitlePanelIndex || subtitlePanelIndex >= m_builtinPanels.Count)
			{
				return null;
			}
			return m_builtinPanels[subtitlePanelIndex];
		}
		}
	}

	private bool SubtitleUsesBarkUI(Subtitle subtitle)
	{
		if (subtitle == null)
		{
			return false;
		}
		return m_useBarkUIs.Contains(subtitle.speakerInfo.transform);
	}

	private string GetSubtitleTextSummary(Subtitle subtitle)
	{
		if (subtitle != null)
		{
			return "[" + subtitle.speakerInfo.Name + "] '" + subtitle.formattedText.text + "'";
		}
		return "(empty subtitle)";
	}

	public virtual void SetActorSubtitlePanelNumber(DialogueActor dialogueActor, SubtitlePanelNumber subtitlePanelNumber)
	{
		if (!(dialogueActor == null))
		{
			if (m_actorPanelCache.ContainsKey(dialogueActor.transform))
			{
				m_actorPanelCache.Remove(dialogueActor.transform);
			}
			if (!m_dialogueActorCache.ContainsKey(dialogueActor.transform))
			{
				m_dialogueActorCache.Add(dialogueActor.transform, dialogueActor);
			}
			if (m_useBarkUIs.Contains(dialogueActor.transform) && subtitlePanelNumber != SubtitlePanelNumber.UseBarkUI)
			{
				m_useBarkUIs.Remove(dialogueActor.transform);
			}
			m_actorPanelCache[dialogueActor.transform] = GetPanelFromNumber(subtitlePanelNumber, dialogueActor.standardDialogueUISettings.customSubtitlePanel);
		}
	}

	public virtual void RecordActorPanelCache(out List<string> actorGOs, out List<SubtitlePanelNumber> actorGOPanels, out List<int> actorIDs, out List<SubtitlePanelNumber> actorIDPanels, out List<string> actorNames)
	{
		actorGOs = new List<string>();
		actorGOPanels = new List<SubtitlePanelNumber>();
		actorIDs = new List<int>();
		actorIDPanels = new List<SubtitlePanelNumber>();
		actorNames = new List<string>();
		for (int i = 0; i < m_builtinPanels.Count; i++)
		{
			actorNames.Add(string.Empty);
		}
		foreach (KeyValuePair<Transform, StandardUISubtitlePanel> item in m_actorPanelCache)
		{
			if (item.Key == null)
			{
				continue;
			}
			SubtitlePanelNumber subtitlePanelNumberFromPanel = GetSubtitlePanelNumberFromPanel(item.Value);
			if (subtitlePanelNumberFromPanel != SubtitlePanelNumber.Custom)
			{
				actorGOs.Add(item.Key.name);
				actorGOPanels.Add(subtitlePanelNumberFromPanel);
				if (subtitlePanelNumberFromPanel >= SubtitlePanelNumber.Panel0)
				{
					actorNames[(int)(subtitlePanelNumberFromPanel - 3)] = item.Key.name;
				}
			}
		}
		foreach (KeyValuePair<int, StandardUISubtitlePanel> item2 in m_actorIdOverridePanel)
		{
			actorIDs.Add(item2.Key);
			SubtitlePanelNumber subtitlePanelNumberFromPanel2 = GetSubtitlePanelNumberFromPanel(item2.Value);
			actorIDPanels.Add(subtitlePanelNumberFromPanel2);
			if (subtitlePanelNumberFromPanel2 >= SubtitlePanelNumber.Panel0)
			{
				Actor actor = DialogueManager.masterDatabase.GetActor(item2.Key);
				if (actor != null)
				{
					actorNames[(int)(subtitlePanelNumberFromPanel2 - 3)] = actor.Name;
				}
			}
		}
	}

	public virtual void QueueSavedActorPanelCache(List<string> actorGOs, List<SubtitlePanelNumber> actorGOPanels, List<int> actorIDs, List<SubtitlePanelNumber> actorIDPanels)
	{
		m_queuedActorGOs = actorGOs;
		m_queuedActorGOPanels = actorGOPanels;
		m_queuedActorIDs = actorIDs;
		m_queuedActorIDPanels = actorIDPanels;
	}

	public virtual void ApplyQueuedActorPanelCache()
	{
		try
		{
			if (m_queuedActorGOs == null)
			{
				return;
			}
			for (int i = 0; i < m_queuedActorGOs.Count; i++)
			{
				GameObject gameObject = GameObject.Find(m_queuedActorGOs[i]);
				if (!(gameObject == null))
				{
					StandardUISubtitlePanel panelFromNumber = GetPanelFromNumber(m_queuedActorGOPanels[i], null);
					if (!(panelFromNumber == null))
					{
						m_actorPanelCache[gameObject.transform] = panelFromNumber;
					}
				}
			}
			for (int j = 0; j < m_queuedActorIDs.Count; j++)
			{
				StandardUISubtitlePanel panelFromNumber2 = GetPanelFromNumber(m_queuedActorIDPanels[j], null);
				if (!(panelFromNumber2 == null))
				{
					m_actorIdOverridePanel[m_queuedActorIDs[j]] = panelFromNumber2;
				}
			}
		}
		finally
		{
			m_queuedActorGOs = null;
			m_queuedActorGOPanels = null;
			m_queuedActorIDs = null;
			m_queuedActorIDPanels = null;
		}
	}

	protected virtual SubtitlePanelNumber GetSubtitlePanelNumberFromPanel(StandardUISubtitlePanel panel)
	{
		if (panel == m_defaultNPCPanel || panel == m_defaultPCPanel)
		{
			return SubtitlePanelNumber.Default;
		}
		for (int i = 0; i < m_builtinPanels.Count; i++)
		{
			if (panel == m_builtinPanels[i])
			{
				return PanelNumberUtility.IntToSubtitlePanelNumber(i);
			}
		}
		return SubtitlePanelNumber.Custom;
	}

	public StandardUISubtitlePanel StageFocusedPanel(Subtitle subtitle)
	{
		m_focusedPanel = GetPanel(subtitle, out var _);
		return m_focusedPanel;
	}

	public override void ShowSubtitle(Subtitle subtitle)
	{
		if (subtitle == null)
		{
			return;
		}
		DialogueActor dialogueActor;
		StandardUISubtitlePanel panel = GetPanel(subtitle, out dialogueActor);
		if (SubtitleUsesBarkUI(subtitle))
		{
			DialogueManager.instance.StartCoroutine(BarkController.Bark(subtitle));
			return;
		}
		if (panel == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning("Dialogue System: Can't find subtitle panel for " + GetSubtitleTextSummary(subtitle) + ".");
			}
			return;
		}
		if (string.IsNullOrEmpty(subtitle.formattedText.text))
		{
			HideSubtitle(subtitle);
			return;
		}
		int id = subtitle.speakerInfo.id;
		if (m_lastPanelUsedByActor.ContainsKey(id) && m_lastPanelUsedByActor[id] != panel)
		{
			StandardUISubtitlePanel standardUISubtitlePanel = m_lastPanelUsedByActor[id];
			if (m_lastActorToUsePanel.ContainsKey(standardUISubtitlePanel) && m_lastActorToUsePanel[standardUISubtitlePanel] == id)
			{
				if (standardUISubtitlePanel.hasFocus || standardUISubtitlePanel.isFocusing)
				{
					standardUISubtitlePanel.Unfocus();
				}
				if (standardUISubtitlePanel.isOpen)
				{
					standardUISubtitlePanel.Close();
				}
			}
		}
		SetLastActorToUsePanel(panel, id);
		m_focusedPanel = panel;
		if (panel.addSpeakerName && !string.IsNullOrEmpty(subtitle.speakerInfo.Name))
		{
			subtitle.formattedText.text = FormattedText.Parse(string.Format(panel.addSpeakerNameFormat, new object[2]
			{
				subtitle.speakerInfo.Name,
				subtitle.formattedText.text
			})).text;
		}
		if (dialogueActor != null && dialogueActor.standardDialogueUISettings.setSubtitleColor)
		{
			subtitle.formattedText.text = dialogueActor.AdjustSubtitleColor(subtitle);
		}
		SupercedeOtherPanels(panel);
		panel.ShowSubtitle(subtitle);
	}

	public void HideSubtitle(Subtitle subtitle)
	{
		if (subtitle == null)
		{
			return;
		}
		DialogueActor dialogueActor;
		StandardUISubtitlePanel panel = GetPanel(subtitle, out dialogueActor);
		if (SubtitleUsesBarkUI(subtitle))
		{
			return;
		}
		if (panel == null)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning("Dialogue System: Can't find subtitle panel for " + GetSubtitleTextSummary(subtitle) + ".");
			}
		}
		else if (panel.visibility == UIVisibility.OnlyDuringContent)
		{
			panel.HideSubtitle(subtitle);
		}
		else
		{
			panel.FinishSubtitle();
		}
	}

	public void Close()
	{
		if (m_defaultNPCPanel != null)
		{
			m_defaultNPCPanel.Close();
		}
		if (m_defaultPCPanel != null)
		{
			m_defaultPCPanel.Close();
		}
		for (int i = 0; i < m_builtinPanels.Count; i++)
		{
			if (m_builtinPanels[i] != null)
			{
				m_builtinPanels[i].Close();
			}
		}
		for (int j = 0; j < m_customPanels.Count; j++)
		{
			if (m_customPanels[j] != null)
			{
				m_customPanels[j].Close();
			}
		}
		foreach (KeyValuePair<Transform, StandardUISubtitlePanel> item in m_actorPanelCache)
		{
			if (item.Value != null)
			{
				item.Value.Close();
			}
		}
		ClearOverrideCache();
	}

	public bool AreAnyPanelsClosing()
	{
		if (m_defaultNPCPanel != null && m_defaultNPCPanel.panelState == UIPanel.PanelState.Closing)
		{
			return true;
		}
		if (m_defaultPCPanel != null && m_defaultPCPanel.panelState == UIPanel.PanelState.Closing)
		{
			return true;
		}
		for (int i = 0; i < m_builtinPanels.Count; i++)
		{
			if (m_builtinPanels[i] != null && m_builtinPanels[i].panelState == UIPanel.PanelState.Closing)
			{
				return true;
			}
		}
		for (int j = 0; j < m_customPanels.Count; j++)
		{
			if (m_customPanels[j] != null && m_customPanels[j].panelState == UIPanel.PanelState.Closing)
			{
				return true;
			}
		}
		foreach (KeyValuePair<Transform, StandardUISubtitlePanel> item in m_actorPanelCache)
		{
			if (item.Value != null && item.Value.panelState == UIPanel.PanelState.Closing)
			{
				return true;
			}
		}
		return false;
	}

	protected virtual void SupercedeOtherPanels(StandardUISubtitlePanel newPanel)
	{
		SupercedeOtherPanelsInList(m_builtinPanels, newPanel);
		SupercedeOtherPanelsInList(m_customPanels, newPanel);
	}

	protected virtual void SupercedeOtherPanelsInList(List<StandardUISubtitlePanel> list, StandardUISubtitlePanel newPanel)
	{
		for (int i = 0; i < list.Count; i++)
		{
			StandardUISubtitlePanel standardUISubtitlePanel = list[i];
			if (!(standardUISubtitlePanel == null) && !(standardUISubtitlePanel == newPanel) && standardUISubtitlePanel.isOpen)
			{
				if (standardUISubtitlePanel.visibility == UIVisibility.UntilSuperceded)
				{
					standardUISubtitlePanel.Close();
				}
				else
				{
					standardUISubtitlePanel.Unfocus();
				}
			}
		}
	}

	public virtual void UnfocusAll()
	{
		for (int i = 0; i < m_builtinPanels.Count; i++)
		{
			StandardUISubtitlePanel standardUISubtitlePanel = m_builtinPanels[i];
			if (standardUISubtitlePanel != null && standardUISubtitlePanel.isOpen && (standardUISubtitlePanel.hasFocus || standardUISubtitlePanel.isFocusing))
			{
				standardUISubtitlePanel.Unfocus();
			}
		}
	}

	public virtual void HideOnResponseMenu()
	{
		for (int i = 0; i < m_builtinPanels.Count; i++)
		{
			StandardUISubtitlePanel standardUISubtitlePanel = m_builtinPanels[i];
			if (standardUISubtitlePanel != null && standardUISubtitlePanel.isOpen && standardUISubtitlePanel.visibility == UIVisibility.UntilSupercededOrActorChangeOrMenu)
			{
				standardUISubtitlePanel.Close();
			}
		}
	}

	public override void ShowContinueButton()
	{
		if (m_focusedPanel != null)
		{
			m_focusedPanel.ShowContinueButton();
		}
	}

	public override void HideContinueButton()
	{
		if (m_focusedPanel != null)
		{
			m_focusedPanel.HideContinueButton();
		}
	}

	public override void SetActive(bool value)
	{
	}

	public override void SetSubtitle(Subtitle subtitle)
	{
	}

	public override void ClearSubtitle()
	{
	}

	public virtual void ClearSubtitlesOnCustomPanels()
	{
		foreach (StandardUISubtitlePanel customPanel in m_customPanels)
		{
			customPanel.ClearText();
		}
	}

	public override void SetActorPortraitSprite(string actorName, Sprite portraitSprite)
	{
		if (string.IsNullOrEmpty(actorName))
		{
			return;
		}
		for (int i = 0; i < m_builtinPanels.Count; i++)
		{
			StandardUISubtitlePanel standardUISubtitlePanel = m_builtinPanels[i];
			if (standardUISubtitlePanel != null && ((standardUISubtitlePanel.currentSubtitle != null && string.Equals(standardUISubtitlePanel.currentSubtitle.speakerInfo.nameInDatabase, actorName)) || standardUISubtitlePanel.portraitActorName == actorName))
			{
				standardUISubtitlePanel.SetActorPortraitSprite(actorName, portraitSprite);
			}
		}
		foreach (StandardUISubtitlePanel value in m_actorPanelCache.Values)
		{
			if (value != null && ((value.currentSubtitle != null && string.Equals(value.currentSubtitle.speakerInfo.nameInDatabase, actorName)) || value.portraitActorName == actorName))
			{
				value.SetActorPortraitSprite(actorName, portraitSprite);
			}
		}
	}

	public void OpenSubtitlePanelsOnStartConversation(StandardDialogueUI ui)
	{
		ApplyQueuedActorPanelCache();
		Conversation conversation = DialogueManager.MasterDatabase.GetConversation(DialogueManager.lastConversationStarted);
		if (conversation != null)
		{
			HashSet<StandardUISubtitlePanel> checkedPanels = new HashSet<StandardUISubtitlePanel>();
			HashSet<int> checkedActorIDs = new HashSet<int>();
			int actorID = conversation.ActorID;
			Actor actor = DialogueManager.masterDatabase.GetActor(DialogueActor.GetActorName(DialogueManager.currentActor));
			if (actor != null)
			{
				actorID = actor.id;
			}
			CheckActorIDOnStartConversation(actorID, checkedActorIDs, checkedPanels, ui);
			CheckActorIDOnStartConversation(conversation.ConversantID, checkedActorIDs, checkedPanels, ui);
			for (int i = 0; i < conversation.dialogueEntries.Count; i++)
			{
				int actorID2 = conversation.dialogueEntries[i].ActorID;
				CheckActorIDOnStartConversation(actorID2, checkedActorIDs, checkedPanels, ui);
			}
		}
	}

	private void CheckActorIDOnStartConversation(int actorID, HashSet<int> checkedActorIDs, HashSet<StandardUISubtitlePanel> checkedPanels, StandardDialogueUI ui)
	{
		if (checkedActorIDs.Contains(actorID))
		{
			return;
		}
		checkedActorIDs.Add(actorID);
		Actor actor = DialogueManager.MasterDatabase.GetActor(actorID);
		if (actor == null)
		{
			return;
		}
		Transform actorTransform = GetActorTransform(actor.Name);
		DialogueActor dialogueActor;
		StandardUISubtitlePanel standardUISubtitlePanel = GetActorTransformPanel(actorTransform, actor.IsPlayer ? m_defaultPCPanel : m_defaultNPCPanel, out dialogueActor);
		if (m_actorIdOverridePanel.ContainsKey(actor.id))
		{
			standardUISubtitlePanel = m_actorIdOverridePanel[actor.id];
		}
		if (standardUISubtitlePanel == null && actorTransform == null && Debug.isDebugBuild)
		{
			Debug.LogWarning("Dialogue System: Can't determine what subtitle panel to use for " + actor.Name, actorTransform);
		}
		if (!(standardUISubtitlePanel == null) && !checkedPanels.Contains(standardUISubtitlePanel))
		{
			standardUISubtitlePanel.dialogueUI = ui;
			checkedPanels.Add(standardUISubtitlePanel);
			if (standardUISubtitlePanel.visibility == UIVisibility.AlwaysFromStart)
			{
				Sprite portraitSprite = ((dialogueActor != null && dialogueActor.GetPortraitSprite() != null) ? dialogueActor.GetPortraitSprite() : actor.GetPortraitSprite());
				string localizedDisplayNameInDatabase = CharacterInfo.GetLocalizedDisplayNameInDatabase(actor.Name);
				standardUISubtitlePanel.OpenOnStartConversation(portraitSprite, localizedDisplayNameInDatabase, dialogueActor);
				SetLastActorToUsePanel(standardUISubtitlePanel, actorID);
			}
		}
	}

	public void SetLastActorToUsePanel(StandardUISubtitlePanel panel, int actorID)
	{
		m_lastActorToUsePanel[panel] = actorID;
		m_lastPanelUsedByActor[actorID] = panel;
	}

	protected Transform GetActorTransform(string actorName)
	{
		Transform transform = CharacterInfo.GetRegisteredActorTransform(actorName);
		if (transform == null)
		{
			GameObject gameObject = GameObject.Find(actorName);
			if (gameObject != null)
			{
				transform = gameObject.transform;
			}
		}
		return transform;
	}

	public void OpenSubtitlePanelLikeStart(SubtitlePanelNumber subtitlePanelNumber)
	{
		StandardUISubtitlePanel panelFromNumber = GetPanelFromNumber(subtitlePanelNumber, null);
		if (panelFromNumber == null || panelFromNumber.isOpen)
		{
			return;
		}
		Conversation conversation = DialogueManager.MasterDatabase.GetConversation(DialogueManager.lastConversationStarted);
		if (conversation == null)
		{
			return;
		}
		for (int i = 0; i < conversation.dialogueEntries.Count; i++)
		{
			int actorID = conversation.dialogueEntries[i].ActorID;
			Actor actor = DialogueManager.MasterDatabase.GetActor(actorID);
			Transform actorTransform = GetActorTransform(actor.Name);
			DialogueActor dialogueActor;
			StandardUISubtitlePanel standardUISubtitlePanel = GetActorTransformPanel(actorTransform, actor.IsPlayer ? m_defaultPCPanel : m_defaultNPCPanel, out dialogueActor);
			if (m_actorIdOverridePanel.ContainsKey(actor.id))
			{
				standardUISubtitlePanel = m_actorIdOverridePanel[actor.id];
			}
			if (standardUISubtitlePanel == panelFromNumber)
			{
				Sprite portraitSprite = ((dialogueActor != null && dialogueActor.GetPortraitSprite() != null) ? dialogueActor.GetPortraitSprite() : actor.GetPortraitSprite());
				string localizedDisplayNameInDatabase = CharacterInfo.GetLocalizedDisplayNameInDatabase(actor.Name);
				panelFromNumber.OpenOnStartConversation(portraitSprite, localizedDisplayNameInDatabase, dialogueActor);
				break;
			}
		}
	}

	public virtual float GetTypewriterSpeed()
	{
		AbstractTypewriterEffect typewriter;
		for (int i = 0; i < m_builtinPanels.Count; i++)
		{
			typewriter = GetTypewriter(m_builtinPanels[i]);
			if (typewriter != null)
			{
				return TypewriterUtility.GetTypewriterSpeed(typewriter);
			}
		}
		typewriter = GetTypewriter(m_defaultNPCPanel);
		if (typewriter != null)
		{
			return TypewriterUtility.GetTypewriterSpeed(typewriter);
		}
		typewriter = GetTypewriter(m_defaultNPCPanel);
		return TypewriterUtility.GetTypewriterSpeed(typewriter);
	}

	public virtual void SetTypewriterSpeed(float charactersPerSecond)
	{
		for (int i = 0; i < m_builtinPanels.Count; i++)
		{
			if (m_builtinPanels[i] != null)
			{
				TypewriterUtility.GetTypewriterSpeed(m_builtinPanels[i].subtitleText);
			}
		}
		if (m_defaultNPCPanel != null && !m_builtinPanels.Contains(m_defaultNPCPanel))
		{
			TypewriterUtility.GetTypewriterSpeed(m_defaultNPCPanel.subtitleText);
		}
		if (m_defaultPCPanel != null && !m_builtinPanels.Contains(m_defaultPCPanel))
		{
			TypewriterUtility.GetTypewriterSpeed(m_defaultPCPanel.subtitleText);
		}
	}

	private AbstractTypewriterEffect GetTypewriter(StandardUISubtitlePanel panel)
	{
		if (!(panel != null))
		{
			return null;
		}
		return TypewriterUtility.GetTypewriter(panel.subtitleText);
	}
}
