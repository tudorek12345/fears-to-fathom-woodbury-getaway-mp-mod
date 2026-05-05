using System;
using PixelCrushers.DialogueSystem.ChatMapper;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class Link
{
	public int originConversationID;

	public int originDialogueID;

	public int destinationConversationID;

	public int destinationDialogueID;

	public bool isConnector;

	public ConditionPriority priority = ConditionPriority.Normal;

	public Link()
	{
	}

	public Link(PixelCrushers.DialogueSystem.ChatMapper.Link chatMapperLink)
	{
		if (chatMapperLink != null)
		{
			originConversationID = ((chatMapperLink.OriginConvoID == 0 && chatMapperLink.ConversationID > 0) ? chatMapperLink.ConversationID : chatMapperLink.OriginConvoID);
			originDialogueID = chatMapperLink.OriginDialogID;
			destinationConversationID = ((chatMapperLink.DestinationConvoID == 0 && chatMapperLink.ConversationID > 0) ? chatMapperLink.ConversationID : chatMapperLink.DestinationConvoID);
			destinationDialogueID = chatMapperLink.DestinationDialogID;
			isConnector = chatMapperLink.IsConnector;
		}
	}

	public Link(Link sourceLink)
	{
		originConversationID = sourceLink.originConversationID;
		originDialogueID = sourceLink.originDialogueID;
		destinationConversationID = sourceLink.destinationConversationID;
		destinationDialogueID = sourceLink.destinationDialogueID;
		isConnector = sourceLink.isConnector;
		priority = sourceLink.priority;
	}

	public Link(int originConversationID, int originDialogueID, int destinationConversationID, int destinationDialogueID)
	{
		this.originConversationID = originConversationID;
		this.originDialogueID = originDialogueID;
		this.destinationConversationID = destinationConversationID;
		this.destinationDialogueID = destinationDialogueID;
	}
}
