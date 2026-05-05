using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class ConversationPositionStack : MonoBehaviour
{
	[Tooltip("Clear stack when new conversation starts. Only applies if component is on Dialogue Manager.")]
	public bool clearOnConversationStart = true;

	[Tooltip("Typically leave unticked so temporary Dialogue Manager's don't unregister your functions.")]
	public bool unregisterOnDisable;

	[Tooltip("Push current dialogue entry instead of its follow-up entry. Use care if ticked; can cause to loop back on itself infinitely.")]
	public bool pushCurrentEntry;

	private static Stack<DialogueEntry> s_stack = new Stack<DialogueEntry>();

	public static bool s_pushCurrentEntry = false;

	private void OnEnable()
	{
		Lua.RegisterFunction("PushConversationPosition", null, SymbolExtensions.GetMethodInfo(() => PushConversationPosition()));
		Lua.RegisterFunction("PopConversationPosition", null, SymbolExtensions.GetMethodInfo(() => PopConversationPosition()));
		Lua.RegisterFunction("ClearConversationPositionStack", null, SymbolExtensions.GetMethodInfo(() => ClearConversationPositionStack()));
		s_pushCurrentEntry = pushCurrentEntry;
	}

	private void OnDisable()
	{
		if (unregisterOnDisable)
		{
			Lua.UnregisterFunction("PushConversationPosition");
			Lua.UnregisterFunction("PopConversationPosition");
			Lua.UnregisterFunction("ClearConversationPositionStack");
		}
	}

	private void OnConversationStart(Transform actor)
	{
		if (clearOnConversationStart)
		{
			ClearConversationPositionStack();
		}
	}

	public static void PushConversationPosition()
	{
		try
		{
			if (!DialogueManager.isConversationActive || DialogueManager.currentConversationState == null)
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning("Dialogue System: PushConversationPosition() Lua function can't save the current conversation position because no conversation is active.");
				}
				return;
			}
			ConversationState currentConversationState = DialogueManager.currentConversationState;
			DialogueEntry dialogueEntry = (s_pushCurrentEntry ? currentConversationState.subtitle.dialogueEntry : (currentConversationState.hasNPCResponse ? currentConversationState.firstNPCResponse.destinationEntry : (currentConversationState.hasPCResponses ? currentConversationState.pcResponses[0].destinationEntry : null)));
			if (dialogueEntry == null)
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning("Dialogue System: PushConversationPosition() Lua function can't save the current conversation position because it's invalid.");
				}
				return;
			}
			s_stack.Push(dialogueEntry);
			if (DialogueDebug.logInfo)
			{
				Debug.Log("Dialogue System: PushConversationPosition() Lua function saved entry " + dialogueEntry.conversationID + ":" + dialogueEntry.id + ": '" + dialogueEntry.currentDialogueText + "'.");
			}
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
			throw ex;
		}
	}

	public static void PopConversationPosition()
	{
		try
		{
			if (!DialogueManager.isConversationActive || DialogueManager.currentConversationState == null)
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning("Dialogue System: PopConversationPosition() Lua function can't restore a saved conversation position because no conversation is active.");
				}
				return;
			}
			if (s_stack.Count == 0)
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning("Dialogue System: PopConversationPosition() Lua function can't restore a saved conversation position no positions are saved on the stack.");
				}
				return;
			}
			DialogueEntry dialogueEntry = s_stack.Pop();
			if (DialogueDebug.logInfo)
			{
				Debug.Log("Dialogue System: PopConversationPosition() Lua function is returning to entry " + dialogueEntry.conversationID + ":" + dialogueEntry.id + ": '" + dialogueEntry.currentDialogueText + "'.");
			}
			DialogueManager.conversationModel.ForceNextStateToLinkToEntry(dialogueEntry);
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
			throw ex;
		}
	}

	public static void ClearConversationPositionStack()
	{
		if (DialogueDebug.logInfo)
		{
			Debug.Log("Dialogue System: ClearConversationPosition() Lua function is clearing the conversation position stack.");
		}
		s_stack.Clear();
	}
}
