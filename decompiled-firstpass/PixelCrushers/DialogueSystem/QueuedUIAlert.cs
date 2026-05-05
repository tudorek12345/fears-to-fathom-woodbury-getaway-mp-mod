namespace PixelCrushers.DialogueSystem;

public class QueuedUIAlert
{
	public string message;

	public float duration;

	public QueuedUIAlert(string message, float duration)
	{
		this.message = message;
		this.duration = duration;
	}
}
