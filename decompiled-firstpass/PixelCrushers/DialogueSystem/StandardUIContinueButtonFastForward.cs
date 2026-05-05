using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class StandardUIContinueButtonFastForward : MonoBehaviour
{
	[Tooltip("Dialogue UI that the continue button affects.")]
	public StandardDialogueUI dialogueUI;

	[Tooltip("Typewriter effect to fast forward if it's not done playing.")]
	public AbstractTypewriterEffect typewriterEffect;

	[Tooltip("Hide the continue button when continuing.")]
	public bool hideContinueButtonOnContinue;

	[Tooltip("If subtitle is displaying, continue past it.")]
	public bool continueSubtitlePanel = true;

	[Tooltip("If alert is displaying, continue past it.")]
	public bool continueAlertPanel = true;

	protected Button continueButton;

	protected AbstractDialogueUI m_runtimeDialogueUI;

	protected virtual AbstractDialogueUI runtimeDialogueUI
	{
		get
		{
			if (m_runtimeDialogueUI == null)
			{
				m_runtimeDialogueUI = dialogueUI;
				if (m_runtimeDialogueUI == null)
				{
					m_runtimeDialogueUI = GetComponentInParent<AbstractDialogueUI>();
					if (m_runtimeDialogueUI == null)
					{
						m_runtimeDialogueUI = DialogueManager.dialogueUI as AbstractDialogueUI;
					}
				}
			}
			return m_runtimeDialogueUI;
		}
	}

	public virtual void Awake()
	{
		if (typewriterEffect == null)
		{
			typewriterEffect = GetComponentInChildren<UnityUITypewriterEffect>();
		}
		continueButton = GetComponent<Button>();
	}

	public virtual void OnFastForward()
	{
		if (typewriterEffect != null && typewriterEffect.isPlaying)
		{
			typewriterEffect.Stop();
			return;
		}
		if (hideContinueButtonOnContinue && (Object)(object)continueButton != null)
		{
			((Component)(object)continueButton).gameObject.SetActive(value: false);
		}
		if (runtimeDialogueUI != null)
		{
			if (continueSubtitlePanel && continueAlertPanel)
			{
				runtimeDialogueUI.OnContinue();
			}
			else if (continueSubtitlePanel)
			{
				runtimeDialogueUI.OnContinueConversation();
			}
			else if (continueAlertPanel)
			{
				runtimeDialogueUI.OnContinueAlert();
			}
		}
	}
}
