using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class UnityUIDialogueUI : CanvasDialogueUI
{
	private class QueuedAlert
	{
		public string message;

		public float duration;

		public QueuedAlert(string message, float duration)
		{
			this.message = message;
			this.duration = duration;
		}
	}

	[HideInInspector]
	public UnityUIRoot unityUIRoot;

	public UnityUIDialogueControls dialogue;

	public Graphic[] qteIndicators;

	public UnityUIAlertControls alert;

	[Tooltip("Always keep a control focused; useful for gamepads and keyboard.")]
	public bool autoFocus;

	[Tooltip("Allow the dialogue UI to steal focus if a non-dialogue UI panel has it.")]
	public bool allowStealFocus;

	[Tooltip("If auto focusing, check on this frequency in seconds that the control is focused.")]
	public float autoFocusCheckFrequency = 0.5f;

	[Tooltip("Look for OverrideUnityUIDialogueControls on actors.")]
	public bool findActorOverrides = true;

	[Tooltip("Add an EventSystem if one isn't in the scene.")]
	public bool addEventSystemIfNeeded = true;

	private UnityUIQTEControls m_qteControls;

	private float m_nextAutoFocusCheckTime;

	private GameObject m_lastSelection;

	private Queue<QueuedAlert> alertQueue = new Queue<QueuedAlert>();

	protected UnityUISubtitleControls originalNPCSubtitle;

	protected UnityUISubtitleControls originalPCSubtitle;

	protected UnityUIResponseMenuControls originalResponseMenu;

	private Dictionary<Transform, OverrideUnityUIDialogueControls> overrideCache = new Dictionary<Transform, OverrideUnityUIDialogueControls>();

	private bool isShowingNpcSubtitle;

	private bool isShowingPcSubtitle;

	private bool isShowingResponses;

	protected int alertQueueCount;

	protected bool alertIsVisible;

	protected bool alertIsHiding;

	public override AbstractUIRoot uiRootControls => unityUIRoot;

	public override AbstractDialogueUIControls dialogueControls => dialogue;

	public override AbstractUIQTEControls qteControls => m_qteControls;

	public override AbstractUIAlertControls alertControls => alert;

	public override void Awake()
	{
		base.Awake();
		FindControls();
		alert.DeactivateUIElements();
		dialogue.DeactivateUIElements();
		Tools.DeprecationWarning(this, "Use StandardDialogueUI instead.");
	}

	public virtual void OnEnable()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	public virtual void OnDisable()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	private void FindControls()
	{
		if (addEventSystemIfNeeded)
		{
			UITools.RequireEventSystem();
		}
		m_qteControls = new UnityUIQTEControls(qteIndicators);
		if (DialogueDebug.logErrors && DialogueDebug.logWarnings)
		{
			if ((Object)(object)dialogue.npcSubtitle.line == null)
			{
				Debug.LogWarning(string.Format("{0}: UnityUIDialogueUI NPC Subtitle Line needs to be assigned.", "Dialogue System"));
			}
			if ((Object)(object)dialogue.pcSubtitle.line == null)
			{
				Debug.LogWarning(string.Format("{0}: UnityUIDialogueUI PC Subtitle Line needs to be assigned.", "Dialogue System"));
			}
			if (dialogue.responseMenu.buttons.Length == 0 && dialogue.responseMenu.buttonTemplate == null)
			{
				Debug.LogWarning(string.Format("{0}: UnityUIDialogueUI Response buttons need to be assigned.", "Dialogue System"));
			}
			if ((Object)(object)alert.line == null)
			{
				Debug.LogWarning(string.Format("{0}: UnityUIDialogueUI Alert Line needs to be assigned.", "Dialogue System"));
			}
		}
		originalNPCSubtitle = dialogue.npcSubtitle;
		originalPCSubtitle = dialogue.pcSubtitle;
		originalResponseMenu = dialogue.responseMenu;
	}

	public OverrideUnityUIDialogueControls FindActorOverride(Transform actor)
	{
		if (actor == null)
		{
			return null;
		}
		if (!overrideCache.ContainsKey(actor))
		{
			overrideCache.Add(actor, (actor != null) ? actor.GetComponentInChildren<OverrideUnityUIDialogueControls>() : null);
		}
		return overrideCache[actor];
	}

	public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (addEventSystemIfNeeded)
		{
			UITools.RequireEventSystem();
		}
	}

	public override void Open()
	{
		overrideCache.Clear();
		base.Open();
		dialogue.npcSubtitle.CheckSubtitlePortrait(CharacterType.NPC);
		dialogue.pcSubtitle.CheckSubtitlePortrait(CharacterType.PC);
	}

	public override void ShowAlert(string message, float duration)
	{
		if (alert.queueAlerts)
		{
			alertQueue.Enqueue(new QueuedAlert(message, duration));
		}
		else
		{
			StartShowingAlert(message, duration);
		}
	}

	private void ShowNextQueuedAlert()
	{
		if (alertQueue.Count > 0)
		{
			QueuedAlert queuedAlert = alertQueue.Dequeue();
			StartShowingAlert(queuedAlert.message, queuedAlert.duration);
		}
	}

	private void StartShowingAlert(string message, float duration)
	{
		base.ShowAlert(message, duration);
		if (autoFocus)
		{
			alert.AutoFocus();
		}
	}

	public override void ShowSubtitle(Subtitle subtitle)
	{
		SetIsShowingSubtitle(subtitle, value: true);
		if (findActorOverrides && subtitle != null)
		{
			OverrideUnityUIDialogueControls overrideUnityUIDialogueControls = ((subtitle.speakerInfo != null) ? FindActorOverride(subtitle.speakerInfo.transform) : null);
			if (overrideUnityUIDialogueControls != null)
			{
				overrideUnityUIDialogueControls.ApplyToDialogueUI(this);
			}
			if (subtitle.speakerInfo == null || subtitle.speakerInfo.characterType == CharacterType.NPC)
			{
				dialogue.npcSubtitle = ((overrideUnityUIDialogueControls != null) ? overrideUnityUIDialogueControls.subtitle : originalNPCSubtitle);
			}
			else
			{
				dialogue.pcSubtitle = ((overrideUnityUIDialogueControls != null) ? overrideUnityUIDialogueControls.subtitle : originalPCSubtitle);
			}
		}
		HideResponses();
		CheckForSupercededSubtitle(subtitle.speakerInfo.characterType);
		base.ShowSubtitle(subtitle);
		ClearSelection();
		CheckSubtitleAutoFocus(subtitle);
	}

	protected void CheckForSupercededSubtitle(CharacterType characterType)
	{
		UnityUISubtitleControls unityUISubtitleControls = ((characterType == CharacterType.NPC) ? dialogue.pcSubtitle : dialogue.npcSubtitle);
		if (unityUISubtitleControls.uiVisibility == UIVisibility.UntilSuperceded && unityUISubtitleControls.isVisible)
		{
			unityUISubtitleControls.ForceHide();
		}
	}

	public void CheckSubtitleAutoFocus(Subtitle subtitle)
	{
		if (autoFocus)
		{
			if (subtitle.speakerInfo.isPlayer)
			{
				dialogue.pcSubtitle.AutoFocus(allowStealFocus);
			}
			else
			{
				dialogue.npcSubtitle.AutoFocus(allowStealFocus);
			}
		}
	}

	protected void SetIsShowingSubtitle(Subtitle subtitle, bool value)
	{
		if (subtitle != null)
		{
			if (subtitle.speakerInfo.isNPC)
			{
				isShowingNpcSubtitle = value;
			}
			else
			{
				isShowingPcSubtitle = value;
			}
		}
	}

	public override void HideSubtitle(Subtitle subtitle)
	{
		SetIsShowingSubtitle(subtitle, value: false);
		base.HideSubtitle(subtitle);
	}

	public override void ShowResponses(Subtitle subtitle, Response[] responses, float timeout)
	{
		isShowingResponses = true;
		if (findActorOverrides)
		{
			OverrideUnityUIDialogueControls overrideUnityUIDialogueControls = ((subtitle != null && subtitle.speakerInfo != null) ? FindActorOverride(subtitle.speakerInfo.transform) : null);
			UnityUISubtitleControls subtitleReminder = ((overrideUnityUIDialogueControls != null) ? overrideUnityUIDialogueControls.subtitleReminder : originalResponseMenu.subtitleReminder);
			if (overrideUnityUIDialogueControls != null && (Object)(object)overrideUnityUIDialogueControls.responseMenu.panel != null)
			{
				dialogue.responseMenu = ((overrideUnityUIDialogueControls != null && (Object)(object)overrideUnityUIDialogueControls.responseMenu.panel != null) ? overrideUnityUIDialogueControls.responseMenu : originalResponseMenu);
			}
			else
			{
				overrideUnityUIDialogueControls = ((subtitle != null && subtitle.listenerInfo != null) ? FindActorOverride(subtitle.listenerInfo.transform) : null);
				dialogue.responseMenu = ((overrideUnityUIDialogueControls != null && (Object)(object)overrideUnityUIDialogueControls.responseMenu.panel != null) ? overrideUnityUIDialogueControls.responseMenu : originalResponseMenu);
			}
			dialogue.responseMenu.subtitleReminder = subtitleReminder;
		}
		if (dialogue.responseMenu.showHideController.state == UIShowHideController.State.Hiding)
		{
			StartCoroutine(ShowResponsesAfterHidden(subtitle, responses, timeout));
			return;
		}
		base.ShowResponses(subtitle, responses, timeout);
		ClearSelection();
		CheckResponseMenuAutoFocus();
	}

	private IEnumerator ShowResponsesAfterHidden(Subtitle subtitle, Response[] responses, float timeout)
	{
		float safeguardTime = Time.realtimeSinceStartup + 5f;
		while (dialogue.responseMenu.showHideController.state == UIShowHideController.State.Hiding && Time.realtimeSinceStartup < safeguardTime)
		{
			yield return null;
		}
		base.ShowResponses(subtitle, responses, timeout);
		ClearSelection();
		CheckResponseMenuAutoFocus();
	}

	public void CheckResponseMenuAutoFocus()
	{
		if (autoFocus)
		{
			dialogue.responseMenu.AutoFocus(m_lastSelection, allowStealFocus);
		}
	}

	public override void HideResponses()
	{
		isShowingResponses = false;
		dialogue.responseMenu.DestroyInstantiatedButtons();
		base.HideResponses();
		if (isShowingNpcSubtitle && (Object)(object)dialogue.responseMenu.subtitleReminder.panel == (Object)(object)dialogue.npcSubtitle.panel)
		{
			dialogue.npcSubtitle.ForceShow();
		}
	}

	public void ClearSelection()
	{
		if (autoFocus)
		{
			EventSystem.current.SetSelectedGameObject((GameObject)null);
			m_lastSelection = null;
		}
	}

	public override void Update()
	{
		base.Update();
		alertQueueCount = alertQueue.Count;
		alertIsVisible = alert.IsVisible;
		alertIsHiding = alert.IsHiding;
		if (alertQueue.Count > 0 && alert.queueAlerts && !alert.IsVisible && (!alert.waitForHideAnimation || !alert.IsHiding))
		{
			ShowNextQueuedAlert();
		}
		if (!autoFocus || !base.isOpen)
		{
			return;
		}
		if (EventSystem.current.currentSelectedGameObject != null)
		{
			m_lastSelection = EventSystem.current.currentSelectedGameObject;
		}
		if (autoFocusCheckFrequency > 0.001f && Time.realtimeSinceStartup > m_nextAutoFocusCheckTime)
		{
			m_nextAutoFocusCheckTime = Time.realtimeSinceStartup + autoFocusCheckFrequency;
			if (isShowingResponses)
			{
				dialogue.responseMenu.AutoFocus(m_lastSelection, allowStealFocus);
			}
			else if (isShowingPcSubtitle)
			{
				dialogue.pcSubtitle.AutoFocus(allowStealFocus);
			}
			else if (isShowingNpcSubtitle)
			{
				dialogue.npcSubtitle.AutoFocus(allowStealFocus);
			}
		}
	}
}
