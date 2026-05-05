namespace PixelCrushers.DialogueSystem;

public static class CharacterTypeUtility
{
	public static CharacterType OtherType(CharacterType characterType)
	{
		if (characterType != CharacterType.PC)
		{
			return CharacterType.PC;
		}
		return CharacterType.NPC;
	}
}
