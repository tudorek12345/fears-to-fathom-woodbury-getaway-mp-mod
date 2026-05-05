using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[AddComponentMenu("")]
public class ContinueButtonFastForward : MonoBehaviour
{
	public UnityDialogueUI dialogueUI;

	public TypewriterEffect typewriterEffect;

	public void OnFastForward()
	{
		if (typewriterEffect != null && typewriterEffect.IsPlaying)
		{
			typewriterEffect.Stop();
		}
		else if (dialogueUI != null)
		{
			dialogueUI.OnContinue();
		}
	}
}
