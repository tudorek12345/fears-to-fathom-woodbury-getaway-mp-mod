using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public class ActiveConversationRecord
{
	public IDialogueUI originalDialogueUI;

	public DisplaySettings originalDisplaySettings;

	public bool isOverrideUIPrefab;

	public bool dontDestroyPrefabInstance;

	public string conversationTitle { get; set; }

	public Transform actor { get; set; }

	public Transform conversant { get; set; }

	public ConversationController conversationController { get; set; }

	public ConversationModel conversationModel
	{
		get
		{
			if (conversationController == null)
			{
				return null;
			}
			return conversationController.conversationModel;
		}
	}

	public ConversationView conversationView
	{
		get
		{
			if (conversationController == null)
			{
				return null;
			}
			return conversationController.conversationView;
		}
	}

	public bool isConversationActive
	{
		get
		{
			if (conversationController != null)
			{
				return conversationController.isActive;
			}
			return false;
		}
	}

	public Transform Actor
	{
		get
		{
			return actor;
		}
		set
		{
			actor = value;
		}
	}

	public Transform Conversant
	{
		get
		{
			return conversant;
		}
		set
		{
			conversant = value;
		}
	}

	public ConversationController ConversationController
	{
		get
		{
			return conversationController;
		}
		set
		{
			conversationController = value;
		}
	}

	public ConversationModel ConversationModel => conversationModel;

	public ConversationView ConversationView => conversationView;

	public bool IsConversationActive => isConversationActive;
}
