namespace PixelCrushers.DialogueSystem;

public enum EntrytagFormat
{
	ActorName_ConversationID_EntryID = 0,
	ConversationTitle_EntryID = 1,
	ActorNameLineNumber = 2,
	ConversationID_ActorName_EntryID = 3,
	ActorName_ConversationTitle_EntryDescriptor = 4,
	VoiceOverFile = 5,
	Title = 6,
	Custom = 99
}
