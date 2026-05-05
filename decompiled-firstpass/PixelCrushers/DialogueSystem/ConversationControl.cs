using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class ConversationControl : MonoBehaviour
{
	[Tooltip("Skip all subtitles until response menu or end of conversation is reached. Set by SkipAll().")]
	public bool skipAll;

	[Tooltip("Stop SkipAll() when response menu is reached.")]
	public bool stopSkipAllOnResponseMenu = true;

	[Tooltip("Stop SkipAll() when end of conversation is reached.")]
	public bool stopSkipAllOnConversationEnd;

	protected AbstractDialogueUI dialogueUI;

	protected virtual void Awake()
	{
		dialogueUI = GetComponent<AbstractDialogueUI>() ?? DialogueManager.standardDialogueUI ?? GameObjectUtility.FindFirstObjectByType<AbstractDialogueUI>();
	}

	public virtual void ToggleAutoPlay()
	{
		DisplaySettings.SubtitleSettings.ContinueButtonMode continueButtonMode = ((DialogueManager.displaySettings.subtitleSettings.continueButton == DisplaySettings.SubtitleSettings.ContinueButtonMode.Never) ? DisplaySettings.SubtitleSettings.ContinueButtonMode.Always : DisplaySettings.SubtitleSettings.ContinueButtonMode.Never);
		DialogueManager.displaySettings.subtitleSettings.continueButton = continueButtonMode;
		if (continueButtonMode == DisplaySettings.SubtitleSettings.ContinueButtonMode.Never)
		{
			dialogueUI.OnContinueConversation();
		}
	}

	public virtual void SkipAll()
	{
		skipAll = true;
		if (dialogueUI != null)
		{
			dialogueUI.OnContinueConversation();
		}
	}

	public virtual void StopSkipAll()
	{
		skipAll = false;
	}

	public virtual void OnConversationLine(Subtitle subtitle)
	{
		if (skipAll)
		{
			subtitle.sequence = "Continue(); " + subtitle.sequence;
		}
	}

	public virtual void OnConversationResponseMenu(Response[] responses)
	{
		if (skipAll)
		{
			if (stopSkipAllOnResponseMenu)
			{
				skipAll = false;
			}
			if (dialogueUI != null)
			{
				dialogueUI.ShowSubtitle(DialogueManager.currentConversationState.subtitle);
			}
		}
	}

	public virtual void OnConversationEnd(Transform actor)
	{
		if (stopSkipAllOnConversationEnd)
		{
			skipAll = false;
		}
	}
}
