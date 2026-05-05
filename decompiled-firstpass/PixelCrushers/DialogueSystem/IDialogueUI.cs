using System;

namespace PixelCrushers.DialogueSystem;

public interface IDialogueUI
{
	event EventHandler<SelectedResponseEventArgs> SelectedResponseHandler;

	void Open();

	void Close();

	void ShowSubtitle(Subtitle subtitle);

	void HideSubtitle(Subtitle subtitle);

	void ShowResponses(Subtitle subtitle, Response[] responses, float timeout);

	void HideResponses();

	void ShowQTEIndicator(int index);

	void HideQTEIndicator(int index);

	void ShowAlert(string message, float duration);

	void HideAlert();
}
