namespace PixelCrushers.DialogueSystem;

public struct QuestEntryArgs(string questName, int entryNumber)
{
	public string questName = questName;

	public int entryNumber = entryNumber;
}
