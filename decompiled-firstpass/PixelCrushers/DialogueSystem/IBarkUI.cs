namespace PixelCrushers.DialogueSystem;

public interface IBarkUI
{
	bool isPlaying { get; }

	void Bark(Subtitle subtitle);

	void Hide();
}
