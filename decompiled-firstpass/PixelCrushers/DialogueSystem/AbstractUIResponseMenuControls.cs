using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public abstract class AbstractUIResponseMenuControls : AbstractUIControls
{
	public ResponseButtonAlignment buttonAlignment;

	public bool showUnusedButtons;

	public abstract AbstractUISubtitleControls subtitleReminderControls { get; }

	protected abstract void ClearResponseButtons();

	protected abstract void SetResponseButtons(Response[] responses, Transform target);

	public abstract void StartTimer(float timeout);

	public virtual void ShowResponses(Subtitle subtitle, Response[] responses, Transform target)
	{
		if (responses != null && responses.Length != 0)
		{
			subtitleReminderControls.ShowSubtitle(subtitle);
			ClearResponseButtons();
			SetResponseButtons(responses, target);
			Show();
		}
		else
		{
			Hide();
		}
	}

	public virtual void SetPCPortrait(Sprite sprite, string portraitName)
	{
	}

	[Obsolete("Use SetPCPortrait(Sprite,string) instead.")]
	public virtual void SetPCPortrait(Texture2D texture, string portraitName)
	{
	}

	public virtual void SetActorPortraitSprite(string actorName, Sprite sprite)
	{
	}

	[Obsolete("Use SetActorPortraitSprite instead.")]
	public virtual void SetActorPortraitTexture(string actorName, Texture2D texture)
	{
	}
}
