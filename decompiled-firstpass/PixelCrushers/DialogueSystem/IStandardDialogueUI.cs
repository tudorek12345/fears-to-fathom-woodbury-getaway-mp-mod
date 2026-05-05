using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public interface IStandardDialogueUI
{
	void SetActorSubtitlePanelNumber(DialogueActor dialogueActor, SubtitlePanelNumber subtitlePanelNumber);

	void SetActorMenuPanelNumber(DialogueActor dialogueActor, MenuPanelNumber menuPanelNumber);

	void OverrideActorPanel(Actor actor, SubtitlePanelNumber subtitlePanelNumber, bool immediate = false);

	void OverrideActorMenuPanel(Transform actorTransform, MenuPanelNumber menuPanelNumber, StandardUIMenuPanel customPanel);
}
