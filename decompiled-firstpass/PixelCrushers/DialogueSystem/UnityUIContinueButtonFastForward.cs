using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class UnityUIContinueButtonFastForward : MonoBehaviour
{
	[Tooltip("Dialogue UI that the continue button affects.")]
	public UnityUIDialogueUI dialogueUI;

	[Tooltip("Typewriter effect to fast forward if it's not done playing.")]
	public UnityUITypewriterEffect typewriterEffect;

	[Tooltip("Hide the continue button when continuing.")]
	public bool hideContinueButtonOnContinue;

	private Button continueButton;

	public virtual void Awake()
	{
		if (dialogueUI == null)
		{
			dialogueUI = Tools.GetComponentAnywhere<UnityUIDialogueUI>(base.gameObject);
		}
		if (typewriterEffect == null)
		{
			typewriterEffect = GetComponentInChildren<UnityUITypewriterEffect>();
		}
		continueButton = GetComponent<Button>();
		Tools.DeprecationWarning(this);
	}

	public virtual void OnFastForward()
	{
		if (typewriterEffect != null && typewriterEffect.IsPlaying)
		{
			typewriterEffect.Stop();
			return;
		}
		if (hideContinueButtonOnContinue && (Object)(object)continueButton != null)
		{
			((Component)(object)continueButton).gameObject.SetActive(value: false);
		}
		if (dialogueUI != null)
		{
			dialogueUI.OnContinue();
		}
	}
}
