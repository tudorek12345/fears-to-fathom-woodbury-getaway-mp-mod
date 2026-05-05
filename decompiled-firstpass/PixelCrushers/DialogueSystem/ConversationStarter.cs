using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public abstract class ConversationStarter : DialogueEventStarter
{
	[Tooltip("Start this conversation.")]
	[ConversationPopup(true, false)]
	public string conversation;

	public Condition condition;

	[Tooltip("Only trigger if at least one entry's Conditions are currently true.")]
	public bool skipIfNoValidEntries;

	[Tooltip("Only trigger if no other conversation is already active.")]
	public bool exclusive;

	[Tooltip("The other actor (e.g., NPC). If unassigned, this GameObject.")]
	public Transform conversant;

	private bool tryingToStart;

	[HideInInspector]
	public bool useConversationTitlePicker = true;

	[HideInInspector]
	public DialogueDatabase selectedDatabase;

	public void TryStartConversation(Transform actor)
	{
		TryStartConversation(actor, actor);
	}

	public void TryStartConversation(Transform actor, Transform interactor)
	{
		if (tryingToStart)
		{
			return;
		}
		tryingToStart = true;
		try
		{
			if (condition != null && !condition.IsTrue(interactor))
			{
				return;
			}
			if (string.IsNullOrEmpty(conversation))
			{
				if (DialogueDebug.logErrors)
				{
					Debug.LogError(string.Format("{0}: Conversation triggered on {1}, but conversation name is blank.", new object[2] { "Dialogue System", base.name }));
				}
			}
			else if ((DialogueManager.isConversationActive && !DialogueManager.allowSimultaneousConversations) || (exclusive && DialogueManager.isConversationActive))
			{
				if (DialogueDebug.logInfo)
				{
					Debug.Log(string.Format("{0}: Conversation triggered on {1}, but another conversation is already active.", new object[2] { "Dialogue System", base.name }));
				}
			}
			else
			{
				StartConversation(actor);
			}
			DestroyIfOnce();
		}
		finally
		{
			tryingToStart = false;
		}
	}

	private void StartConversation(Transform actor)
	{
		Transform transform = Tools.Select(conversant, base.transform);
		if (skipIfNoValidEntries && !DialogueManager.ConversationHasValidEntry(conversation, actor, transform))
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log(string.Format("{0}: Conversation triggered on {1}, but skipping because no entries are currently valid.", new object[2] { "Dialogue System", base.name }));
			}
		}
		else
		{
			DialogueManager.StartConversation(conversation, actor, transform);
		}
	}
}
