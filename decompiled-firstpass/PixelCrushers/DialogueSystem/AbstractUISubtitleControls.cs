using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public abstract class AbstractUISubtitleControls : AbstractUIControls
{
	protected Subtitle currentSubtitle;

	public abstract bool hasText { get; }

	public bool HasText => hasText;

	public abstract void SetSubtitle(Subtitle subtitle);

	public abstract void ClearSubtitle();

	public virtual void ShowContinueButton()
	{
	}

	public virtual void HideContinueButton()
	{
	}

	public virtual void ShowSubtitle(Subtitle subtitle)
	{
		if (subtitle != null && !string.IsNullOrEmpty(subtitle.formattedText.text))
		{
			currentSubtitle = subtitle;
			SetSubtitle(subtitle);
			Show();
		}
		else
		{
			currentSubtitle = null;
			ClearSubtitle();
			Hide();
		}
	}

	public virtual void SetActorPortraitSprite(string actorName, Sprite sprite)
	{
	}

	[Obsolete("Use SetActorPortraitSprite instead.")]
	public virtual void SetActorPortraitTexture(string actorName, Texture2D texture)
	{
	}
}
