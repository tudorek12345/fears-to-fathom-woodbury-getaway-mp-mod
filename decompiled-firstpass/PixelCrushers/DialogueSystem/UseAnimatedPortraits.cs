using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class UseAnimatedPortraits : MonoBehaviour
{
	private UnityUIDialogueUI dialogueUI;

	private Animator npcPortraitAnimator;

	private Animator npcReminderPortraitAnimator;

	private Animator pcPortraitAnimator;

	private Animator otherAnimator;

	private Dictionary<Transform, AnimatedPortrait> animatedPortraits = new Dictionary<Transform, AnimatedPortrait>();

	private Transform lastSpeaker;

	public void OnConversationLine(Subtitle subtitle)
	{
		if (FindDialogueUI())
		{
			StartCoroutine(SetAnimatorAtEndOfFrame(subtitle));
		}
	}

	private IEnumerator SetAnimatorAtEndOfFrame(Subtitle subtitle)
	{
		yield return CoroutineUtility.endOfFrame;
		OverrideUnityUIDialogueControls overrideUnityUIDialogueControls = dialogueUI.FindActorOverride(subtitle.speakerInfo.transform);
		if (overrideUnityUIDialogueControls != null)
		{
			otherAnimator = null;
			SetAnimatorController(overrideUnityUIDialogueControls.subtitle.portraitImage, subtitle.speakerInfo.transform, ref otherAnimator);
		}
		else if (subtitle.speakerInfo.characterType == CharacterType.NPC)
		{
			SetAnimatorController(dialogueUI.dialogue.npcSubtitle.portraitImage, subtitle.speakerInfo.transform, ref npcPortraitAnimator);
		}
		else
		{
			SetAnimatorController(dialogueUI.dialogue.pcSubtitle.portraitImage, subtitle.speakerInfo.transform, ref pcPortraitAnimator);
		}
		lastSpeaker = subtitle.speakerInfo.transform;
	}

	public void OnConversationResponseMenu(Response[] responses)
	{
		if (FindDialogueUI())
		{
			SetAnimatorController(dialogueUI.dialogue.responseMenu.subtitleReminder.portraitImage, lastSpeaker, ref npcReminderPortraitAnimator);
		}
	}

	public void OnConversationEnd(Transform actor)
	{
		animatedPortraits.Clear();
	}

	private bool FindDialogueUI()
	{
		if (dialogueUI == null && (bool)DialogueManager.displaySettings.dialogueUI)
		{
			dialogueUI = DialogueManager.displaySettings.dialogueUI.GetComponent<UnityUIDialogueUI>();
		}
		return dialogueUI != null;
	}

	private void SetAnimatorController(Image image, Transform speaker, ref Animator animator)
	{
		if (speaker == null || (Object)(object)image == null)
		{
			return;
		}
		if (animator == null)
		{
			animator = ((Component)(object)image).GetComponent<Animator>();
		}
		if (animator == null)
		{
			animator = ((Component)(object)image).gameObject.AddComponent<Animator>();
		}
		if (!animatedPortraits.ContainsKey(speaker))
		{
			AnimatedPortrait value = ((speaker != null) ? speaker.GetComponentInChildren<AnimatedPortrait>() : null);
			animatedPortraits.Add(speaker, value);
		}
		if (animatedPortraits[speaker] != null)
		{
			RuntimeAnimatorController animatorController = animatedPortraits[speaker].animatorController;
			if (animator.runtimeAnimatorController != animatorController)
			{
				animator.runtimeAnimatorController = animatorController;
			}
		}
	}
}
