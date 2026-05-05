namespace PixelCrushers.DialogueSystem;

public static class DatabaseUtility
{
	public static DialogueDatabase CreateDialogueDatabaseInstance(bool createDefaultAssets = false)
	{
		Template template = Template.FromDefault();
		DialogueDatabase dialogueDatabase = ScriptableObjectUtility.CreateScriptableObject(RuntimeTypeUtility.GetWrapperType(typeof(DialogueDatabase)) ?? typeof(DialogueDatabase)) as DialogueDatabase;
		dialogueDatabase.ResetEmphasisSettings();
		if (createDefaultAssets)
		{
			dialogueDatabase.actors.Add(template.CreateActor(1, "Player", isPlayer: true));
			dialogueDatabase.variables.Add(template.CreateVariable(1, "Alert", string.Empty));
		}
		return dialogueDatabase;
	}
}
