namespace PixelCrushers.DialogueSystem;

public class Response
{
	public FormattedText formattedText;

	public DialogueEntry destinationEntry;

	public bool enabled;

	public Response(FormattedText formattedText, DialogueEntry destinationEntry, bool enabled = true)
	{
		this.formattedText = formattedText;
		this.destinationEntry = destinationEntry;
		this.enabled = enabled;
	}
}
