namespace PixelCrushers.DialogueSystem;

public interface ITextFieldUI
{
	void StartTextInput(string labelText, string text, int maxLength, AcceptedTextDelegate acceptedText);

	void CancelTextInput();
}
