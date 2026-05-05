using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public abstract class BarkStarter : ConversationStarter
{
	[Tooltip("The order in which to bark dialogue entries.")]
	public BarkOrder barkOrder;

	[Tooltip("Allow barks during active conversations.")]
	public bool allowDuringConversations;

	[Tooltip("Cache all lines during first bark. This can reduce stutter when barking on slower mobile devices, but barks' conditions are not reevaluated each time as the state changes, barks use no em formatting codes, and sequences are not played with barks.")]
	public bool cacheBarkLines;

	private BarkHistory barkHistory;

	private bool tryingToBark;

	private ConversationState cachedState;

	private IBarkUI barkUI;

	protected BarkGroupMember barkGroupMember;

	public Sequencer sequencer { get; private set; }

	public int BarkIndex
	{
		get
		{
			return barkHistory.index;
		}
		set
		{
			barkHistory.index = value;
		}
	}

	protected virtual void Awake()
	{
		barkHistory = new BarkHistory(barkOrder);
		sequencer = null;
		barkGroupMember = GetBarker().GetComponent<BarkGroupMember>();
	}

	protected virtual void Start()
	{
		if (cacheBarkLines && cachedState == null)
		{
			PopulateCache(GetBarker(), null);
		}
	}

	protected virtual void OnEnable()
	{
		PersistentDataManager.RegisterPersistentData(base.gameObject);
	}

	protected virtual void OnDisable()
	{
		PersistentDataManager.UnregisterPersistentData(base.gameObject);
	}

	public void TryBark(Transform target)
	{
		TryBark(target, target);
	}

	public void TryBark(Transform target, Transform interactor)
	{
		if (tryingToBark)
		{
			return;
		}
		tryingToBark = true;
		try
		{
			if (condition != null && !condition.IsTrue(interactor))
			{
				return;
			}
			if (string.IsNullOrEmpty(conversation))
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning(string.Format("{0}: Bark triggered on {1}, but conversation name is blank.", new object[2] { "Dialogue System", base.name }), GetBarker());
				}
			}
			else if (DialogueManager.isConversationActive && !allowDuringConversations)
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning(string.Format("{0}: Bark triggered on {1}, but a conversation is already active.", new object[2] { "Dialogue System", base.name }), GetBarker());
				}
			}
			else if (cacheBarkLines)
			{
				BarkCachedLine(GetBarker(), target);
			}
			else
			{
				if (barkGroupMember != null)
				{
					barkGroupMember.GroupBark(conversation, target, barkHistory);
				}
				else
				{
					DialogueManager.Bark(conversation, GetBarker(), target, barkHistory);
				}
				sequencer = BarkController.LastSequencer;
			}
			DestroyIfOnce();
		}
		finally
		{
			tryingToBark = false;
		}
	}

	private Transform GetBarker()
	{
		return Tools.Select(conversant, base.transform);
	}

	private string GetBarkerName()
	{
		return DialogueActor.GetActorName(GetBarker());
	}

	private void BarkCachedLine(Transform speaker, Transform listener)
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

	private void PopulateCache(Transform speaker, Transform listener)
	{
		if (string.IsNullOrEmpty(conversation) && DialogueDebug.logWarnings)
		{
			Debug.Log(string.Format("{0}: Bark (speaker={1}, listener={2}): conversation title is blank", new object[3] { "Dialogue System", speaker, listener }), speaker);
		}
		ConversationModel conversationModel = new ConversationModel(DialogueManager.masterDatabase, conversation, speaker, listener, DialogueManager.allowLuaExceptions, DialogueManager.isDialogueEntryValid);
		cachedState = conversationModel.firstState;
		if (cachedState == null && DialogueDebug.logWarnings)
		{
			Debug.Log(string.Format("{0}: Bark (speaker={1}, listener={2}): '{3}' has no START entry", "Dialogue System", speaker, listener, conversation), speaker);
		}
		if (!cachedState.hasAnyResponses && DialogueDebug.logWarnings)
		{
			Debug.Log(string.Format("{0}: Bark (speaker={1}, listener={2}): '{3}' has no valid bark lines", "Dialogue System", speaker, listener, conversation), speaker);
		}
	}

	private void BarkNextCachedLine(Transform speaker, Transform listener)
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
			Subtitle subtitle = new Subtitle(cachedState.subtitle.listenerInfo, cachedState.subtitle.speakerInfo, FormattedText.Parse(destinationEntry.currentDialogueText), string.Empty, string.Empty, destinationEntry);
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

	public void OnRecordPersistentData()
	{
		if (base.enabled && barkHistory != null)
		{
			DialogueLua.SetActorField(GetBarkerName(), "Bark_Index", barkHistory.index);
		}
	}

	public void OnApplyPersistentData()
	{
		if (base.enabled)
		{
			if (barkHistory == null)
			{
				barkHistory = new BarkHistory(barkOrder);
			}
			barkHistory.index = DialogueLua.GetActorField(GetBarkerName(), "Bark_Index").asInt;
		}
	}
}
