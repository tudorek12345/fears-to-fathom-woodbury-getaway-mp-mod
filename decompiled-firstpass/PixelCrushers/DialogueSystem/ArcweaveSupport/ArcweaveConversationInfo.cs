using System;

namespace PixelCrushers.DialogueSystem.ArcweaveSupport;

[Serializable]
public class ArcweaveConversationInfo
{
	public string boardGuid;

	public int startIndex;

	public int actorIndex;

	public int conversantIndex;

	public ArcweaveConversationInfo()
	{
	}

	public ArcweaveConversationInfo(string boardGuid)
	{
		this.boardGuid = boardGuid;
	}
}
