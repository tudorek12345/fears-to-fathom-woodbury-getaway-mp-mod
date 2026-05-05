using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[Serializable]
public class UnitySubtitleControls : AbstractUISubtitleControls
{
	public GUIControl panel;

	public GUILabel line;

	public GUILabel portraitImage;

	public GUILabel portraitName;

	public GUIButton continueButton;

	public override bool hasText
	{
		get
		{
			if (line != null)
			{
				return !string.IsNullOrEmpty(line.text);
			}
			return false;
		}
	}

	public override void SetActive(bool value)
	{
		UnityDialogueUIControls.SetControlActive(line, value);
		UnityDialogueUIControls.SetControlActive(portraitImage, value);
		UnityDialogueUIControls.SetControlActive(portraitName, value);
		UnityDialogueUIControls.SetControlActive(continueButton, value);
		UnityDialogueUIControls.SetControlActive(panel, value);
	}

	public override void SetSubtitle(Subtitle subtitle)
	{
		if (portraitImage != null)
		{
			Sprite speakerPortrait = subtitle.GetSpeakerPortrait();
			portraitImage.image = ((speakerPortrait != null) ? speakerPortrait.texture : null);
		}
		if (portraitName != null)
		{
			portraitName.text = subtitle.speakerInfo.Name;
		}
		if (line != null)
		{
			line.SetFormattedText(subtitle.formattedText);
		}
	}

	public override void ClearSubtitle()
	{
		if (portraitImage != null)
		{
			portraitImage.image = null;
		}
		if (portraitName != null)
		{
			portraitName.text = null;
		}
		if (line != null)
		{
			line.SetUnformattedText(string.Empty);
		}
	}

	public override void ShowContinueButton()
	{
		UnityDialogueUIControls.SetControlActive(continueButton, value: true);
	}

	public override void HideContinueButton()
	{
		UnityDialogueUIControls.SetControlActive(continueButton, value: false);
	}

	public override void SetActorPortraitSprite(string actorName, Sprite portraitSprite)
	{
		if (currentSubtitle != null && string.Equals(currentSubtitle.speakerInfo.nameInDatabase, actorName) && portraitImage != null)
		{
			portraitImage.image = UITools.GetTexture2D(AbstractDialogueUI.GetValidPortraitSprite(actorName, portraitSprite));
		}
	}
}
