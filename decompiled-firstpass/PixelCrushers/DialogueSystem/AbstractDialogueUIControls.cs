using System;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public abstract class AbstractDialogueUIControls : AbstractUIControls
{
	public abstract AbstractUISubtitleControls npcSubtitleControls { get; }

	public abstract AbstractUISubtitleControls pcSubtitleControls { get; }

	public abstract AbstractUIResponseMenuControls responseMenuControls { get; }

	public abstract void ShowPanel();

	public override void SetActive(bool value)
	{
		npcSubtitleControls.SetActive(value);
		pcSubtitleControls.SetActive(value);
		responseMenuControls.SetActive(value);
	}
}
