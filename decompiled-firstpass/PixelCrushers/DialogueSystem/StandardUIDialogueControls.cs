using System;
using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class StandardUIDialogueControls : AbstractDialogueUIControls
{
	[Tooltip("Main panel for conversation UI (optional).")]
	public UIPanel mainPanel;

	[Tooltip("Never deactivate Main Panel. Will still play show & hide animations if specified.")]
	public bool dontDeactivateMainPanel;

	[Tooltip("When starting conversation, wait until main panel is open before showing subtitle or menu.")]
	public bool waitForMainPanelOpen;

	public StandardUISubtitlePanel[] subtitlePanels;

	[Tooltip("Default panel for NPC subtitles.")]
	public StandardUISubtitlePanel defaultNPCSubtitlePanel;

	[Tooltip("Default panel for PC subtitles.")]
	public StandardUISubtitlePanel defaultPCSubtitlePanel;

	[Tooltip("Check for subtitle panels that are configured to immediately open when conversation starts. Untick to bypass check.")]
	public bool allowOpenSubtitlePanelsOnStartConversation = true;

	public StandardUIMenuPanel[] menuPanels;

	[Tooltip("Default panel for response menus.")]
	public StandardUIMenuPanel defaultMenuPanel;

	[Tooltip("When showing response menu, use portrait info of player actor assigned to first response. Also use that actor's menu panel if using multiple menu panels.")]
	public bool useFirstResponseForMenuPortrait;

	[Tooltip("When closing, wait for all subtitle panels and menu panels to close.")]
	public bool waitForClose = true;

	private StandardUISubtitleControls m_standardSubtitleControls = new StandardUISubtitleControls();

	private StandardUIResponseMenuControls m_standardMenuControls = new StandardUIResponseMenuControls();

	private bool m_initializedAnimator;

	private Coroutine closeCoroutine;

	public StandardUISubtitleControls standardSubtitleControls => m_standardSubtitleControls;

	public override AbstractUISubtitleControls npcSubtitleControls => m_standardSubtitleControls;

	public override AbstractUISubtitleControls pcSubtitleControls => m_standardSubtitleControls;

	public StandardUIResponseMenuControls standardMenuControls => m_standardMenuControls;

	public override AbstractUIResponseMenuControls responseMenuControls => m_standardMenuControls;

	public void Initialize()
	{
		m_standardSubtitleControls.Initialize(subtitlePanels, defaultNPCSubtitlePanel, defaultPCSubtitlePanel);
		m_standardMenuControls.Initialize(menuPanels, defaultMenuPanel, useFirstResponseForMenuPortrait);
	}

	public override void SetActive(bool value)
	{
		if (value)
		{
			ShowPanel();
		}
		else
		{
			HidePanel();
		}
	}

	public override void ShowPanel()
	{
		if (closeCoroutine != null)
		{
			if (mainPanel != null)
			{
				mainPanel.StopCoroutine(closeCoroutine);
			}
			closeCoroutine = null;
		}
		m_initializedAnimator = true;
		if (mainPanel != null)
		{
			mainPanel.Open();
		}
		standardSubtitleControls.ApplyQueuedActorPanelCache();
	}

	private void HidePanel()
	{
		if (!m_initializedAnimator || (mainPanel != null && !mainPanel.gameObject.activeSelf))
		{
			HideImmediate();
			m_initializedAnimator = true;
			return;
		}
		standardSubtitleControls.Close();
		standardMenuControls.Close();
		if (mainPanel != null && !dontDeactivateMainPanel)
		{
			if (waitForClose)
			{
				closeCoroutine = mainPanel.StartCoroutine(CloseAfterPanelsAreClosed());
			}
			else
			{
				mainPanel.Close();
			}
		}
	}

	public void ClosePanels()
	{
		standardSubtitleControls.Close();
		standardMenuControls.Close();
	}

	private IEnumerator CloseAfterPanelsAreClosed()
	{
		while (AreAnyPanelsClosing())
		{
			yield return null;
		}
		mainPanel.Close();
	}

	public bool AreAnyPanelsClosing(StandardUISubtitlePanel extraSubtitlePanel = null)
	{
		if (extraSubtitlePanel != null && extraSubtitlePanel.panelState == UIPanel.PanelState.Closing)
		{
			return true;
		}
		if (standardSubtitleControls.AreAnyPanelsClosing())
		{
			return true;
		}
		if (standardMenuControls.AreAnyPanelsClosing())
		{
			return true;
		}
		if (mainPanel != null && mainPanel.panelState == UIPanel.PanelState.Closing)
		{
			return true;
		}
		return false;
	}

	public void HideImmediate()
	{
		HideSubtitlePanelsImmediate();
		HideMenuPanelsImmediate();
		if (mainPanel != null && !dontDeactivateMainPanel)
		{
			mainPanel.gameObject.SetActive(value: false);
			mainPanel.panelState = UIPanel.PanelState.Closed;
		}
	}

	private void HideSubtitlePanelsImmediate()
	{
		for (int i = 0; i < subtitlePanels.Length; i++)
		{
			StandardUISubtitlePanel standardUISubtitlePanel = subtitlePanels[i];
			if (standardUISubtitlePanel != null)
			{
				standardUISubtitlePanel.HideImmediate();
			}
		}
	}

	private void HideMenuPanelsImmediate()
	{
		for (int i = 0; i < menuPanels.Length; i++)
		{
			StandardUIMenuPanel standardUIMenuPanel = menuPanels[i];
			if (standardUIMenuPanel != null)
			{
				standardUIMenuPanel.HideImmediate();
			}
		}
	}

	public void OpenSubtitlePanelsOnStart(StandardDialogueUI ui)
	{
		if (allowOpenSubtitlePanelsOnStartConversation)
		{
			standardSubtitleControls.OpenSubtitlePanelsOnStartConversation(ui);
		}
	}

	public void ClearCaches()
	{
		standardSubtitleControls.ClearCache();
		standardMenuControls.ClearCache();
	}

	public virtual void ClearAllSubtitleText()
	{
		for (int i = 0; i < subtitlePanels.Length; i++)
		{
			if (!(subtitlePanels[i] == null))
			{
				subtitlePanels[i].ClearText();
			}
		}
		standardSubtitleControls.ClearSubtitlesOnCustomPanels();
	}

	public virtual void ClearSubtitleTextOnConversationStart()
	{
		for (int i = 0; i < subtitlePanels.Length; i++)
		{
			if (!(subtitlePanels[i] == null) && subtitlePanels[i].clearTextOnConversationStart)
			{
				subtitlePanels[i].ClearText();
			}
		}
	}
}
