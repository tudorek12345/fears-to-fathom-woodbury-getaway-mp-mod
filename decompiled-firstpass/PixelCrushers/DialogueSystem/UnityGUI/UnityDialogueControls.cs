using System;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[Serializable]
public class UnityDialogueControls : AbstractDialogueUIControls
{
	public GUIControl panel;

	public UnitySubtitleControls npcSubtitle;

	public UnitySubtitleControls pcSubtitle;

	public UnityResponseMenuControls responseMenu;

	public override AbstractUISubtitleControls npcSubtitleControls => npcSubtitle;

	public override AbstractUISubtitleControls pcSubtitleControls => pcSubtitle;

	public override AbstractUIResponseMenuControls responseMenuControls => responseMenu;

	public override void ShowPanel()
	{
		UnityDialogueUIControls.SetControlActive(panel, value: true);
	}

	public override void SetActive(bool value)
	{
		base.SetActive(value);
		UnityDialogueUIControls.SetControlActive(panel, value);
	}
}
