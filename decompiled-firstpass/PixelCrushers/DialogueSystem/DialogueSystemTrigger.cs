using System;
using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class DialogueSystemTrigger : MonoBehaviour
{
	[Serializable]
	public class SendMessageAction
	{
		[Tooltip("Target GameObject.")]
		public GameObject gameObject;

		[Tooltip("Name of method to call on target. One or more scripts on target should have a method with this name.")]
		public string message = "OnUse";

		[Tooltip("Optional method parameter. Specify if method accepts a string parameter.")]
		public string parameter = string.Empty;
	}

	public enum BarkSource
	{
		None,
		Conversation,
		Text
	}

	[Serializable]
	public class SetGameObjectActiveAction
	{
		public Condition condition = new Condition();

		public Transform target;

		public Toggle state;
	}

	[Serializable]
	public class SetComponentEnabledAction
	{
		public Condition condition = new Condition();

		public Component target;

		public Toggle state;
	}

	[Serializable]
	public class SetAnimatorStateAction
	{
		public Condition condition = new Condition();

		[Tooltip("Set the state of the animator on this GameObject. Animator can be on a child GameObject.")]
		public Transform target;

		[Tooltip("State to crossfade to.")]
		public string stateName;

		public float crossFadeDuration = 0.3f;
	}

	[Tooltip("The trigger that this component listens for.")]
	[DialogueSystemTriggerEvent]
	public DialogueSystemTriggerEvent trigger = DialogueSystemTriggerEvent.OnUse;

	public Condition condition;

	[Tooltip("Set a quest state when triggered.")]
	public bool setQuestState = true;

	[QuestPopup(false)]
	public string questName;

	[Tooltip("Set quest's main state.")]
	[QuestState]
	public QuestState questState;

	[Tooltip("Set state of a quest entry.")]
	public bool setQuestEntryState;

	[QuestEntryPopup]
	public int questEntryNumber = 1;

	[QuestState]
	public QuestState questEntryState;

	public bool setAnotherQuestEntryState;

	[QuestEntryPopup]
	public int anotherQuestEntryNumber = 1;

	[QuestState]
	public QuestState anotherQuestEntryState;

	[Tooltip("Lua code to run. Leave blank for no message.")]
	public string luaCode = string.Empty;

	[TextArea(1, 10)]
	public string sequence = string.Empty;

	[Tooltip("Optional GameObject to use if sequence uses 'speaker' keyword.")]
	public Transform sequenceSpeaker;

	[Tooltip("Optional GameObject to use if sequence uses 'listener' keyword.")]
	public Transform sequenceListener;

	public bool waitOneFrameOnStartOrEnable = true;

	[Tooltip("Alert message. Leave blank for no message.")]
	public string alertMessage;

	[Tooltip("Optional text table to use to localize messages.")]
	public TextTable textTable;

	public float alertDuration;

	public SendMessageAction[] sendMessages = new SendMessageAction[0];

	[Tooltip("Where to get content to bark.")]
	public BarkSource barkSource;

	[Tooltip("Conversation to get bark content from.")]
	[ConversationPopup(false, true)]
	public string barkConversation = string.Empty;

	[Tooltip("Dialogue entry to bark. Otherwise will bark from a valid entry in bark conversation.")]
	public int barkEntryID = -1;

	[Tooltip("Bark entry with this Title. If set, this takes precedence over Bark Entry ID.")]
	public string barkEntryTitle;

	[Tooltip("Bark this text. Will be localized through Dialogue Manager's Text Table if assigned.")]
	public string barkText = string.Empty;

	[Tooltip("Optional sequence to play when barking text.")]
	public string barkTextSequence = string.Empty;

	[Tooltip("Character that bark comes from. Should have a bark UI or a Dialogue Actor component with a bark UI prefab assigned.")]
	public Transform barker;

	[Tooltip("Optional target of the bark. Receives OnBark events.")]
	public Transform barkTarget;

	public BarkOrder barkOrder;

	public bool allowBarksDuringConversations;

	[Tooltip("Only trigger if at least one entry's Conditions are currently true.")]
	public bool skipBarkIfNoValidEntries;

	[Tooltip("Cache all lines during first bark. This can reduce stutter when barking on slower mobile devices, but barks' conditions are not reevaluated each time as the state changes, barks use no em formatting codes, and sequences are not played with barks.")]
	public bool cacheBarkLines;

	[Tooltip("Conversation to start. Leave blank for no conversation.")]
	[ConversationPopup(false, true)]
	public string conversation = string.Empty;

	[Tooltip("Other actor (e.g., NPC). If unassigned, this GameObject.")]
	public Transform conversationConversant;

	[Tooltip("Primary actor (e.g., player). If unassigned, GameObject that triggered conversation.")]
	public Transform conversationActor;

	[Tooltip("Start at this entry ID.")]
	public int startConversationEntryID = -1;

	[Tooltip("Start at entry with this Title. If set, this takes precedence over Start Conversation Entry ID.")]
	public string startConversationEntryTitle;

	[Tooltip("Only trigger if no other conversation is already active.")]
	public bool exclusive;

	[Tooltip("Stop other conversation if one is active.")]
	public bool replace;

	[Tooltip("If another conversation is active and simultaneous conversations aren't allowed, queue this conversation to start as soon as active one ends.")]
	public bool queue;

	[Tooltip("Only trigger if at least one entry's Conditions are currently true.")]
	public bool skipIfNoValidEntries;

	[Tooltip("Disallow conversation if same conversation just ended on this frame.")]
	public bool preventRestartOnSameFrameEnded;

	[Tooltip("Stop conversation if actor leaves trigger area.")]
	public bool stopConversationOnTriggerExit;

	[Tooltip("Stop conversation if Conversation Actor exceeds Max Conversation Distance from this trigger's GameObject.")]
	public bool stopConversationIfTooFar;

	[Tooltip("If Stop Conversation If Too Far is ticked, this is too far.")]
	public float maxConversationDistance = 5f;

	[Tooltip("Check distance on this frequency.")]
	public float monitorConversationDistanceFrequency = 1f;

	[Tooltip("Make the cursor visible when the conversation starts. Return to previous visibility state when conversation ends.")]
	public bool showCursorDuringConversation;

	[Tooltip("Set Time.timeScale to 0 during conversation, back to previous timeScale when conversation ends.")]
	public bool pauseGameDuringConversation;

	public SetGameObjectActiveAction[] setActiveActions = new SetGameObjectActiveAction[0];

	public SetComponentEnabledAction[] setEnabledActions = new SetComponentEnabledAction[0];

	public SetAnimatorStateAction[] setAnimatorStateActions = new SetAnimatorStateAction[0];

	public GameObjectUnityEvent onExecute = new GameObjectUnityEvent();

	[HideInInspector]
	public bool useConversationTitlePicker = true;

	[HideInInspector]
	public bool useBarkTitlePicker = true;

	[HideInInspector]
	public bool useQuestNamePicker = true;

	[HideInInspector]
	public DialogueDatabase selectedDatabase;

	protected BarkHistory barkHistory;

	protected ConversationState cachedState;

	protected BarkGroupMember barkGroupMember;

	protected IBarkUI barkUI;

	protected bool isConversationQueued;

	protected Transform queuedActor;

	protected float earliestTimeToAllowTriggerExit;

	protected const float MarginToAllowTriggerExit = 0.2f;

	protected Coroutine monitorDistanceCoroutine;

	protected bool wasCursorVisible;

	protected CursorLockMode savedLockState;

	protected bool didIPause;

	protected float preConversationTimeScale = 1f;

	protected int frameConversationEnded = -1;

	protected bool tryingToStart;

	protected bool hasSaveSystem;

	protected Coroutine fireIfNoSaveDataAppliedCoroutine;

	protected ActiveConversationRecord activeConversation;

	protected bool listenForOnDestroy;

	public Sequencer sequencer { get; protected set; }

	public virtual void Awake()
	{
		sequencer = null;
		hasSaveSystem = SaveSystem.hasInstance;
		if (hasSaveSystem && (trigger == DialogueSystemTriggerEvent.OnSaveDataApplied || (trigger == DialogueSystemTriggerEvent.OnStart && DialogueManager.instance.onStartTriggerWaitForSaveDataApplied)))
		{
			SaveSystem.saveDataApplied += OnSaveDataApplied;
		}
	}

	public virtual void Start()
	{
		if (trigger == DialogueSystemTriggerEvent.OnCollisionEnter || trigger == DialogueSystemTriggerEvent.OnCollisionExit || trigger == DialogueSystemTriggerEvent.OnTriggerEnter || trigger == DialogueSystemTriggerEvent.OnTriggerExit)
		{
			bool flag = false;
			if (GetComponent<Collider>() != null)
			{
				flag = true;
			}
			if (!flag && DialogueDebug.logWarnings)
			{
				Debug.LogWarning("Dialogue System: Dialogue System Trigger is set to a mode that requires a collider, but it has no collider component. If your project is 2D, did you enable 2D support? (Tools > Pixel Crushers > Dialogue System > Welcome Window)", this);
			}
		}
		else if (trigger == DialogueSystemTriggerEvent.OnStart)
		{
			if (hasSaveSystem && DialogueManager.instance.onStartTriggerWaitForSaveDataApplied)
			{
				fireIfNoSaveDataAppliedCoroutine = StartCoroutine(FireIfNoSaveDataApplied());
			}
			else
			{
				StartCoroutine(StartAtEndOfFrame());
			}
		}
		else if (trigger == DialogueSystemTriggerEvent.OnSaveDataApplied)
		{
			if (hasSaveSystem)
			{
				fireIfNoSaveDataAppliedCoroutine = StartCoroutine(FireIfNoSaveDataApplied());
			}
			else
			{
				StartCoroutine(StartAtEndOfFrame());
			}
		}
		if (barkSource != BarkSource.None)
		{
			barkGroupMember = GetBarker(barkConversation).GetComponent<BarkGroupMember>();
		}
		if (cacheBarkLines && barkSource == BarkSource.Conversation && !string.IsNullOrEmpty(barkConversation))
		{
			PopulateCache(GetBarker(barkConversation), barkTarget);
		}
	}

	public void OnBarkStart(Transform actor)
	{
		if (base.enabled && trigger == DialogueSystemTriggerEvent.OnBarkStart)
		{
			TryStart(actor);
		}
	}

	public void OnBarkEnd(Transform actor)
	{
		if (base.enabled && trigger == DialogueSystemTriggerEvent.OnBarkEnd)
		{
			TryStart(actor);
		}
	}

	public void OnConversationStart(Transform actor)
	{
		if (base.enabled && trigger == DialogueSystemTriggerEvent.OnConversationStart)
		{
			TryStart(actor);
		}
	}

	public void OnConversationEnd(Transform actor)
	{
		if (base.enabled && trigger == DialogueSystemTriggerEvent.OnConversationEnd)
		{
			TryStart(actor);
		}
	}

	private void OnConversationStartAnywhere(Transform actor)
	{
		DialogueManager.instance.conversationStarted -= OnConversationStartAnywhere;
		if (showCursorDuringConversation)
		{
			wasCursorVisible = Cursor.visible;
			savedLockState = Cursor.lockState;
			StartCoroutine(ShowCursorAfterOneFrame());
		}
		if (pauseGameDuringConversation && string.Equals(DialogueManager.lastConversationStarted, conversation))
		{
			didIPause = true;
			preConversationTimeScale = Time.timeScale;
			Time.timeScale = 0f;
		}
	}

	protected IEnumerator ShowCursorAfterOneFrame()
	{
		yield return null;
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
	}

	private void OnConversationEndAnywhere(Transform actor)
	{
		if (!DialogueManager.allowSimultaneousConversations || activeConversation == null || !activeConversation.conversationController.isActive)
		{
			DialogueManager.instance.conversationEnded -= OnConversationEndAnywhere;
			StopMonitoringConversationDistance();
			if (showCursorDuringConversation)
			{
				Cursor.visible = wasCursorVisible;
				Cursor.lockState = savedLockState;
			}
			if (pauseGameDuringConversation && didIPause)
			{
				didIPause = false;
				Time.timeScale = preConversationTimeScale;
			}
			frameConversationEnded = Time.frameCount;
		}
	}

	private void OnConversationEndCheckQueue(Transform actor)
	{
		if (isConversationQueued && !DialogueManager.isConversationActive)
		{
			DialogueManager.instance.conversationEnded -= OnConversationEndAnywhere;
			isConversationQueued = false;
			DoConversationAction(queuedActor);
		}
	}

	public void OnSequenceStart(Transform actor)
	{
		if (base.enabled && trigger == DialogueSystemTriggerEvent.OnSequenceStart)
		{
			TryStart(actor);
		}
	}

	public void OnSequenceEnd(Transform actor)
	{
		if (base.enabled && trigger == DialogueSystemTriggerEvent.OnSequenceEnd)
		{
			TryStart(actor);
		}
	}

	public void OnSequenceEnd()
	{
		OnSequenceEnd(null);
	}

	public void OnUse(Transform actor)
	{
		if (base.enabled && trigger == DialogueSystemTriggerEvent.OnUse)
		{
			TryStart(actor);
		}
	}

	public void OnUse(string message)
	{
		if (base.enabled && trigger == DialogueSystemTriggerEvent.OnUse)
		{
			TryStart(null);
		}
	}

	public void OnUse()
	{
		if (base.enabled && trigger == DialogueSystemTriggerEvent.OnUse)
		{
			TryStart(null);
		}
	}

	public void OnTriggerEnter(Collider other)
	{
		if (base.enabled && trigger == DialogueSystemTriggerEvent.OnTriggerEnter)
		{
			TryStart(other.transform);
		}
	}

	public void OnTriggerExit(Collider other)
	{
		CheckOnTriggerExit(other.transform);
	}

	protected void CheckOnTriggerExit(Transform otherTransform)
	{
		if (!base.enabled)
		{
			return;
		}
		if (stopConversationOnTriggerExit && DialogueManager.isConversationActive && GetCurrentDialogueTime() > earliestTimeToAllowTriggerExit && (DialogueManager.currentActor == otherTransform || DialogueManager.currentConversant == otherTransform))
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log("Dialogue System: Stopping conversation because " + otherTransform?.ToString() + " exited trigger area.", this);
			}
			StopActiveConversation();
		}
		else if (trigger == DialogueSystemTriggerEvent.OnTriggerExit)
		{
			TryStart(otherTransform.transform);
		}
	}

	public void OnCollisionEnter(Collision collision)
	{
		if (base.enabled && trigger == DialogueSystemTriggerEvent.OnCollisionEnter)
		{
			TryStart(collision.collider.transform);
		}
	}

	public void OnCollisionExit(Collision collision)
	{
		if (base.enabled && trigger == DialogueSystemTriggerEvent.OnTriggerExit)
		{
			TryStart(collision.collider.transform);
		}
	}

	public void OnEnable()
	{
		PersistentDataManager.RegisterPersistentData(base.gameObject);
		listenForOnDestroy = true;
		if (trigger == DialogueSystemTriggerEvent.OnEnable)
		{
			StartCoroutine(StartAtEndOfFrame());
		}
	}

	public void OnDisable()
	{
		StopMonitoringConversationDistance();
		StopAllCoroutines();
		PersistentDataManager.UnregisterPersistentData(base.gameObject);
		if (listenForOnDestroy && trigger == DialogueSystemTriggerEvent.OnDisable)
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
		if (hasSaveSystem)
		{
			SaveSystem.saveDataApplied -= OnSaveDataApplied;
			if (fireIfNoSaveDataAppliedCoroutine != null)
			{
				StopCoroutine(fireIfNoSaveDataAppliedCoroutine);
				fireIfNoSaveDataAppliedCoroutine = null;
			}
		}
		if (listenForOnDestroy && trigger == DialogueSystemTriggerEvent.OnDestroy)
		{
			TryStart(null);
		}
	}

	protected IEnumerator StartAtEndOfFrame()
	{
		if (Time.frameCount > 1)
		{
			yield return CoroutineUtility.endOfFrame;
		}
		TryStart(null);
	}

	protected virtual void OnSaveDataApplied()
	{
		if (fireIfNoSaveDataAppliedCoroutine != null)
		{
			StopCoroutine(fireIfNoSaveDataAppliedCoroutine);
			fireIfNoSaveDataAppliedCoroutine = null;
		}
		if (base.enabled)
		{
			TryStart(null);
		}
	}

	protected virtual IEnumerator FireIfNoSaveDataApplied()
	{
		if (hasSaveSystem)
		{
			for (int i = 0; i < SaveSystem.framesToWaitBeforeApplyData + 1; i++)
			{
				yield return null;
			}
			fireIfNoSaveDataAppliedCoroutine = null;
			TryStart(null);
		}
	}

	public void TryStart(Transform actor)
	{
		TryStart(actor, actor);
	}

	public virtual void TryStart(Transform actor, Transform interactor)
	{
		if (tryingToStart)
		{
			return;
		}
		tryingToStart = true;
		try
		{
			if (condition == null || condition.IsTrue(interactor))
			{
				Fire(actor);
			}
		}
		finally
		{
			tryingToStart = false;
		}
	}

	public virtual void Fire(Transform actor)
	{
		if (!DialogueManager.hasInstance)
		{
			Debug.LogError("Dialogue System: Dialogue System Trigger '" + base.name + "' can't fire. There is no Dialogue Manager GameObject.", this);
			return;
		}
		if (DialogueDebug.logInfo)
		{
			Debug.Log("Dialogue System: Dialogue System Trigger is firing " + trigger.ToString() + ".", this);
		}
		DoQuestAction();
		DoLuaAction(actor);
		DoSequenceAction(actor);
		DoAlertAction();
		DoSendMessageActions();
		DoBarkAction(actor);
		DoConversationAction(actor);
		DoSetActiveActions(actor);
		DoSetEnabledActions(actor);
		DoSetAnimatorStateActions(actor);
		if (onExecute != null)
		{
			onExecute.Invoke((actor != null) ? actor.gameObject : null);
		}
		DialogueManager.SendUpdateTracker();
	}

	protected virtual void DoQuestAction()
	{
		if (string.IsNullOrEmpty(questName))
		{
			return;
		}
		if (setQuestState)
		{
			QuestLog.SetQuestState(questName, questState);
		}
		if (setQuestEntryState)
		{
			QuestLog.SetQuestEntryState(questName, questEntryNumber, questEntryState);
			if (setAnotherQuestEntryState)
			{
				QuestLog.SetQuestEntryState(questName, anotherQuestEntryNumber, anotherQuestEntryState);
			}
		}
	}

	protected virtual void DoLuaAction(Transform actor)
	{
		if (!string.IsNullOrEmpty(luaCode))
		{
			if (actor != null)
			{
				DialogueActor dialogueActorComponent = DialogueActor.GetDialogueActorComponent(actor);
				string value = ((dialogueActorComponent != null) ? dialogueActorComponent.actor : actor.name);
				DialogueLua.SetVariable("ActorIndex", value);
				DialogueLua.SetVariable("Actor", DialogueActor.GetActorName(actor));
			}
			DoLuaAction();
		}
	}

	protected virtual void DoLuaAction()
	{
		if (!string.IsNullOrEmpty(luaCode))
		{
			Lua.Run(luaCode, DialogueDebug.logInfo);
		}
	}

	protected virtual void DoSequenceAction(Transform actor)
	{
		if (!string.IsNullOrEmpty(sequence))
		{
			DialogueManager.PlaySequence(sequence, Tools.Select(sequenceSpeaker, base.transform), Tools.Select(sequenceListener, actor));
		}
	}

	protected virtual void DoAlertAction()
	{
		if (!string.IsNullOrEmpty(alertMessage))
		{
			string message = ((!(textTable != null) || !textTable.HasFieldTextForLanguage(alertMessage, Localization.GetCurrentLanguageID(textTable))) ? DialogueManager.GetLocalizedText(alertMessage) : textTable.GetFieldTextForLanguage(alertMessage, Localization.GetCurrentLanguageID(textTable)));
			if (Mathf.Approximately(0f, alertDuration))
			{
				DialogueManager.ShowAlert(message);
			}
			else
			{
				DialogueManager.ShowAlert(message, alertDuration);
			}
		}
	}

	protected virtual void DoSendMessageActions()
	{
		for (int i = 0; i < sendMessages.Length; i++)
		{
			SendMessageAction sendMessageAction = sendMessages[i];
			if (sendMessageAction != null && sendMessageAction.gameObject != null && !string.IsNullOrEmpty(sendMessageAction.message))
			{
				sendMessageAction.gameObject.SendMessage(sendMessageAction.message, sendMessageAction.parameter, SendMessageOptions.DontRequireReceiver);
			}
		}
	}

	protected virtual void DoBarkAction(Transform actor)
	{
		switch (barkSource)
		{
		case BarkSource.Conversation:
		{
			if (string.IsNullOrEmpty(barkConversation))
			{
				break;
			}
			if (barkHistory == null)
			{
				barkHistory = new BarkHistory(barkOrder);
			}
			if (DialogueManager.isConversationActive && !allowBarksDuringConversations)
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning("Dialogue System: Bark triggered on " + base.name + ", but a conversation is already active.", GetBarker(barkConversation));
				}
				break;
			}
			if (cacheBarkLines)
			{
				BarkCachedLine(GetBarker(barkConversation), Tools.Select(barkTarget, actor));
				break;
			}
			int num = ((!string.IsNullOrEmpty(barkEntryTitle)) ? GetEntryIDFromTitle(barkConversation, barkEntryTitle) : barkEntryID);
			if (barkGroupMember != null)
			{
				if (num == -1)
				{
					barkGroupMember.GroupBark(barkConversation, Tools.Select(barkTarget, actor), barkHistory);
				}
				else
				{
					barkGroupMember.GroupBark(barkConversation, Tools.Select(barkTarget, actor), num);
				}
			}
			else if (num == -1)
			{
				DialogueManager.Bark(barkConversation, GetBarker(barkConversation), Tools.Select(barkTarget, actor), barkHistory);
			}
			else
			{
				DialogueManager.Bark(barkConversation, GetBarker(barkConversation), Tools.Select(barkTarget, actor), num);
			}
			sequencer = BarkController.LastSequencer;
			break;
		}
		case BarkSource.Text:
			if (string.IsNullOrEmpty(barkText))
			{
				break;
			}
			if (DialogueManager.isConversationActive && !allowBarksDuringConversations)
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning("Dialogue System: Bark triggered on " + base.name + ", but a conversation is already active.", GetBarker(null));
				}
				break;
			}
			if (barkGroupMember != null)
			{
				barkGroupMember.GroupBarkString(barkText, Tools.Select(barkTarget, actor), barkTextSequence);
			}
			else
			{
				DialogueManager.BarkString(barkText, GetBarker(null), Tools.Select(barkTarget, actor), barkTextSequence);
			}
			sequencer = BarkController.LastSequencer;
			break;
		}
	}

	protected virtual Transform GetBarker(string conversation)
	{
		if (barker != null)
		{
			return barker;
		}
		if (!string.IsNullOrEmpty(conversation))
		{
			Conversation conversation2 = DialogueManager.MasterDatabase.GetConversation(conversation);
			if (conversation2 != null)
			{
				Actor actor = DialogueManager.MasterDatabase.GetActor(conversation2.ConversantID);
				Transform transform = ((actor != null) ? CharacterInfo.GetRegisteredActorTransform(actor.Name) : null);
				if (transform != null)
				{
					return transform;
				}
			}
		}
		return base.transform;
	}

	protected virtual string GetBarkerName()
	{
		return DialogueActor.GetActorName(GetBarker((barkSource == BarkSource.Conversation) ? barkConversation : null));
	}

	protected virtual void BarkCachedLine(Transform speaker, Transform listener)
	{
		if (barkUI == null)
		{
			barkUI = speaker.GetComponentInChildren(typeof(IBarkUI)) as IBarkUI;
		}
		if (cachedState == null)
		{
			PopulateCache(speaker, listener);
		}
		BarkNextCachedLine(speaker, listener);
	}

	protected void PopulateCache(Transform speaker, Transform listener)
	{
		if (string.IsNullOrEmpty(barkConversation) && DialogueDebug.logWarnings)
		{
			Debug.Log(string.Format("{0}: Bark (speaker={1}, listener={2}): conversation title is blank", new object[3] { "Dialogue System", speaker, listener }), speaker);
		}
		ConversationModel conversationModel = new ConversationModel(DialogueManager.masterDatabase, barkConversation, speaker, listener, DialogueManager.allowLuaExceptions, DialogueManager.isDialogueEntryValid, barkEntryID);
		cachedState = conversationModel.firstState;
		if (cachedState == null && DialogueDebug.logWarnings)
		{
			Debug.Log(string.Format("{0}: Bark (speaker={1}, listener={2}): '{3}' has no START entry", "Dialogue System", speaker, listener, barkConversation), speaker);
		}
		if (!cachedState.hasAnyResponses && DialogueDebug.logWarnings)
		{
			Debug.Log(string.Format("{0}: Bark (speaker={1}, listener={2}): '{3}' has no valid bark lines", "Dialogue System", speaker, listener, barkConversation), speaker);
		}
	}

	protected void BarkNextCachedLine(Transform speaker, Transform listener)
	{
		if (barkUI == null || cachedState == null || !cachedState.hasAnyResponses)
		{
			return;
		}
		Response[] array = (cachedState.hasNPCResponse ? cachedState.npcResponses : cachedState.pcResponses);
		int nextIndex = (barkHistory ?? new BarkHistory(BarkOrder.Random)).GetNextIndex(array.Length);
		DialogueEntry destinationEntry = array[nextIndex].destinationEntry;
		if (destinationEntry == null && DialogueDebug.logWarnings)
		{
			Debug.Log(string.Format("{0}: Bark (speaker={1}, listener={2}): '{3}' bark entry is null", "Dialogue System", speaker, listener, conversation), speaker);
		}
		if (destinationEntry != null)
		{
			Subtitle subtitle = new Subtitle(cachedState.subtitle.listenerInfo, cachedState.subtitle.speakerInfo, new FormattedText(destinationEntry.currentDialogueText), destinationEntry.currentSequence, string.Empty, destinationEntry);
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Bark (speaker={1}, listener={2}): '{3}'", "Dialogue System", speaker, listener, subtitle.formattedText.text), speaker);
			}
			if (barkGroupMember != null)
			{
				barkGroupMember.GroupBarkString(subtitle.formattedText.text, listener, subtitle.sequence);
			}
			else
			{
				StartCoroutine(BarkController.Bark(subtitle, speaker, listener, barkUI));
			}
		}
	}

	public void ResetBarkHistory()
	{
		barkHistory.Reset();
	}

	protected virtual void DoConversationAction(Transform actor)
	{
		if (string.IsNullOrEmpty(this.conversation))
		{
			return;
		}
		if (replace && DialogueManager.isConversationActive)
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log("Dialogue System: Stopping current active conversation " + DialogueManager.lastConversationStarted + " and starting " + this.conversation + ".", this);
			}
			DialogueManager.StopAllConversations();
		}
		if (queue && !DialogueManager.allowSimultaneousConversations && DialogueManager.isConversationActive)
		{
			isConversationQueued = true;
			queuedActor = actor;
			DialogueManager.instance.conversationEnded += OnConversationEndCheckQueue;
			if (DialogueDebug.logInfo)
			{
				Debug.Log("Dialogue System: Conversation triggered on " + base.name + " is queued to play as soon as the current conversation ends.", this);
			}
			return;
		}
		if (exclusive && DialogueManager.isConversationActive)
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log("Dialogue System: Conversation triggered on " + base.name + " but skipping because another conversation is active.", this);
			}
			return;
		}
		Transform actor2 = Tools.Select(conversationActor, actor);
		Transform transform = conversationConversant;
		if (transform == null)
		{
			Conversation conversation = DialogueManager.MasterDatabase.GetConversation(this.conversation);
			Actor actor3 = ((conversation != null) ? DialogueManager.MasterDatabase.GetActor(conversation.ConversantID) : null);
			Transform transform2 = ((actor3 != null) ? CharacterInfo.GetRegisteredActorTransform(actor3.Name) : null);
			transform = ((transform2 != null) ? transform2 : base.transform);
		}
		int initialDialogueEntryID = ((!string.IsNullOrEmpty(startConversationEntryTitle)) ? GetEntryIDFromTitle(this.conversation, startConversationEntryTitle) : startConversationEntryID);
		if (skipIfNoValidEntries && !DialogueManager.ConversationHasValidEntry(this.conversation, actor2, transform, initialDialogueEntryID))
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log("Dialogue System: Conversation triggered on " + base.name + " but skipping because no entries are currently valid.", this);
			}
			return;
		}
		if (preventRestartOnSameFrameEnded && frameConversationEnded == Time.frameCount && DialogueManager.lastConversationStarted == this.conversation)
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log("Dialogue System: Conversation triggered on " + base.name + " but skipping because same conversation just ended on this frame.", this);
			}
			return;
		}
		if (stopConversationIfTooFar || showCursorDuringConversation || pauseGameDuringConversation || preventRestartOnSameFrameEnded)
		{
			DialogueManager.instance.conversationStarted += OnConversationStartAnywhere;
			DialogueManager.instance.conversationEnded += OnConversationEndAnywhere;
		}
		DialogueManager.StartConversation(this.conversation, actor2, transform, initialDialogueEntryID);
		activeConversation = DialogueManager.instance.activeConversation;
		earliestTimeToAllowTriggerExit = GetCurrentDialogueTime() + 0.2f;
		if (stopConversationIfTooFar)
		{
			monitorDistanceCoroutine = StartCoroutine(MonitorDistance(DialogueManager.currentActor));
		}
	}

	private float GetCurrentDialogueTime()
	{
		if (DialogueTime.mode != DialogueTime.TimeMode.Gameplay)
		{
			return Time.realtimeSinceStartup;
		}
		return Time.time;
	}

	private int GetEntryIDFromTitle(string conversation, string entryTitle)
	{
		if (string.IsNullOrEmpty(conversation) || string.IsNullOrEmpty(entryTitle))
		{
			return -1;
		}
		Conversation conversation2 = DialogueManager.MasterDatabase.GetConversation(conversation);
		if (conversation2 == null)
		{
			return -1;
		}
		return conversation2.dialogueEntries.Find((DialogueEntry x) => string.Equals(x.Title, entryTitle))?.id ?? (-1);
	}

	protected virtual void StopActiveConversation()
	{
		if (activeConversation != null && activeConversation.conversationController != null)
		{
			activeConversation.conversationController.Close();
			activeConversation = null;
		}
	}

	protected void StopMonitoringConversationDistance()
	{
		if (monitorDistanceCoroutine != null)
		{
			StopCoroutine(monitorDistanceCoroutine);
		}
		monitorDistanceCoroutine = null;
	}

	protected IEnumerator MonitorDistance(Transform actor)
	{
		if (!(actor == null))
		{
			Transform myTransform = base.transform;
			do
			{
				yield return StartCoroutine(DialogueTime.WaitForSeconds(monitorConversationDistanceFrequency));
			}
			while (!(Vector3.Distance(myTransform.position, actor.position) > maxConversationDistance));
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Stopping conversation. Exceeded max distance {1} between {2} and {3}", "Dialogue System", maxConversationDistance, base.name, actor.name));
			}
			StopActiveConversation();
		}
	}

	protected virtual void DoSetActiveActions(Transform actor)
	{
		for (int i = 0; i < setActiveActions.Length; i++)
		{
			SetGameObjectActiveAction setGameObjectActiveAction = setActiveActions[i];
			if (setGameObjectActiveAction != null && setGameObjectActiveAction.condition != null && setGameObjectActiveAction.condition.IsTrue(actor))
			{
				Transform transform = Tools.Select(setGameObjectActiveAction.target, base.transform);
				bool newValue = ToggleUtility.GetNewValue(transform.gameObject.activeSelf, setGameObjectActiveAction.state);
				if (DialogueDebug.logInfo)
				{
					Debug.Log(string.Format("{0}: Trigger: {1}.SetActive({2})", new object[3] { "Dialogue System", transform.name, newValue }));
				}
				transform.gameObject.SetActive(newValue);
			}
		}
	}

	protected virtual void DoSetEnabledActions(Transform actor)
	{
		for (int i = 0; i < setEnabledActions.Length; i++)
		{
			SetComponentEnabledAction setComponentEnabledAction = setEnabledActions[i];
			if (setComponentEnabledAction != null && setComponentEnabledAction.condition != null && setComponentEnabledAction.condition.IsTrue(actor))
			{
				Tools.SetComponentEnabled(setComponentEnabledAction.target, setComponentEnabledAction.state);
			}
		}
	}

	protected virtual void DoSetAnimatorStateActions(Transform actor)
	{
		for (int i = 0; i < setAnimatorStateActions.Length; i++)
		{
			SetAnimatorStateAction setAnimatorStateAction = setAnimatorStateActions[i];
			if (setAnimatorStateAction == null || setAnimatorStateAction.condition == null || !setAnimatorStateAction.condition.IsTrue(actor))
			{
				continue;
			}
			Transform transform = Tools.Select(setAnimatorStateAction.target, base.transform);
			Animator componentInChildren = transform.GetComponentInChildren<Animator>();
			if (componentInChildren == null)
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.Log(string.Format("{0}: Trigger: {1}.SetAnimatorState() can't find Animator", new object[2] { "Dialogue System", transform.name }));
				}
				continue;
			}
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Trigger: {1}.SetAnimatorState({2})", new object[3] { "Dialogue System", transform.name, setAnimatorStateAction.stateName }));
			}
			componentInChildren.CrossFade(setAnimatorStateAction.stateName, setAnimatorStateAction.crossFadeDuration);
		}
	}

	public void OnRecordPersistentData()
	{
		if (base.enabled && !string.IsNullOrEmpty(barkConversation))
		{
			DialogueLua.SetActorField(GetBarkerName(), "Bark_Index", barkHistory.index);
		}
	}

	public void OnApplyPersistentData()
	{
		if (base.enabled && !string.IsNullOrEmpty(barkConversation))
		{
			if (barkHistory == null)
			{
				barkHistory = new BarkHistory(barkOrder);
			}
			barkHistory.index = DialogueLua.GetActorField(GetBarkerName(), "Bark_Index").asInt;
		}
	}
}
