using UnityEngine;
using UnityEngine.Events;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class OverrideUnityUIDialogueControls : MonoBehaviour
{
	[Tooltip("Use these controls when playing subtitles through this actor")]
	public UnityUISubtitleControls subtitle;

	[Tooltip("Use these controls when showing subtitle reminders for actor")]
	public UnityUISubtitleControls subtitleReminder;

	[Tooltip("Use these controls when showing a response menu involving this actor")]
	public UnityUIResponseMenuControls responseMenu;

	private bool checkedContinueButton;

	private void Awake()
	{
		Tools.DeprecationWarning(this, "Use StandardDialogueUI and DialogueActor, which make this script unnecessary.");
	}

	public virtual void Start()
	{
		if (subtitle != null)
		{
			subtitle.SetActive(value: false);
		}
		if (subtitleReminder != null)
		{
			subtitleReminder.SetActive(value: false);
		}
		if (responseMenu != null)
		{
			responseMenu.SetActive(value: false);
		}
	}

	public virtual void ApplyToDialogueUI(UnityUIDialogueUI ui)
	{
		if (checkedContinueButton)
		{
			return;
		}
		if (subtitle != null && (Object)(object)subtitle.continueButton != null)
		{
			if (((UnityEventBase)(object)subtitle.continueButton.onClick).GetPersistentEventCount() == 0 || ((UnityEventBase)(object)subtitle.continueButton.onClick).GetPersistentTarget(0) == null)
			{
				((UnityEvent)(object)subtitle.continueButton.onClick).AddListener((UnityAction)ui.OnContinue);
			}
			UnityUIContinueButtonFastForward component = ((Component)(object)subtitle.continueButton).GetComponent<UnityUIContinueButtonFastForward>();
			if (component != null && component.dialogueUI == null)
			{
				component.dialogueUI = ui;
			}
		}
		checkedContinueButton = true;
	}
}
