using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class TextlessBarkUI : MonoBehaviour, IBarkUI
{
	public bool isPlaying => false;

	public void Bark(Subtitle subtitle)
	{
	}

	public void Hide()
	{
	}
}
