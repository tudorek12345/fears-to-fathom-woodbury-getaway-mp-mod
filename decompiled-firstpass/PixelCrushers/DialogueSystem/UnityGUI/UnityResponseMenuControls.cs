using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[Serializable]
public class UnityResponseMenuControls : AbstractUIResponseMenuControls
{
	public GUIControl panel;

	public GUILabel pcImage;

	public GUILabel pcName;

	public UnitySubtitleControls subtitleReminder;

	public GUIProgressBar timer;

	public GUIButton[] buttons;

	private TimerEffect timerEffect;

	private Texture2D pcPortraitTexture;

	private string pcPortraitName;

	public override AbstractUISubtitleControls subtitleReminderControls => subtitleReminder;

	public override void SetPCPortrait(Sprite portraitSprite, string portraitName)
	{
		pcPortraitTexture = UITools.GetTexture2D(portraitSprite);
		pcPortraitName = portraitName;
	}

	public override void SetActorPortraitSprite(string actorName, Sprite portraitSprite)
	{
		if (string.Equals(actorName, pcPortraitName))
		{
			Texture2D image = (pcPortraitTexture = UITools.GetTexture2D(AbstractDialogueUI.GetValidPortraitSprite(actorName, portraitSprite)));
			if (pcImage != null && DialogueManager.masterDatabase.IsPlayer(actorName))
			{
				pcImage.image = image;
			}
		}
	}

	public override void SetActive(bool value)
	{
		subtitleReminder.SetActive(value && subtitleReminder.hasText);
		GUIButton[] array = buttons;
		foreach (GUIButton gUIButton in array)
		{
			UnityDialogueUIControls.SetControlActive(gUIButton, value && gUIButton.visible);
		}
		UnityDialogueUIControls.SetControlActive(timer, value: false);
		UnityDialogueUIControls.SetControlActive(pcImage, value);
		UnityDialogueUIControls.SetControlActive(pcName, value);
		UnityDialogueUIControls.SetControlActive(panel, value);
		if (value)
		{
			if (pcImage != null && pcPortraitTexture != null)
			{
				pcImage.image = pcPortraitTexture;
			}
			if (pcName != null && pcPortraitName != null)
			{
				pcName.text = pcPortraitName;
			}
		}
	}

	protected override void ClearResponseButtons()
	{
		if (buttons != null)
		{
			for (int i = 0; i < buttons.Length; i++)
			{
				SetResponseButton(buttons[i], null, null);
				buttons[i].visible = showUnusedButtons;
			}
		}
	}

	protected override void SetResponseButtons(Response[] responses, Transform target)
	{
		if (buttons == null || buttons.Length == 0 || responses == null)
		{
			return;
		}
		for (int i = 0; i < responses.Length; i++)
		{
			if (responses[i].formattedText.position != -1)
			{
				int num = Mathf.Clamp(responses[i].formattedText.position, 0, buttons.Length - 1);
				SetResponseButton(buttons[num], responses[i], target);
			}
		}
		if (buttonAlignment == ResponseButtonAlignment.ToFirst)
		{
			for (int j = 0; j < Mathf.Min(buttons.Length, responses.Length); j++)
			{
				if (responses[j].formattedText.position == -1)
				{
					int num2 = Mathf.Clamp(GetNextAvailableResponseButtonPosition(0, 1), 0, buttons.Length - 1);
					SetResponseButton(buttons[num2], responses[j], target);
				}
			}
			return;
		}
		for (int num3 = Mathf.Min(buttons.Length, responses.Length) - 1; num3 >= 0; num3--)
		{
			if (responses[num3].formattedText.position == -1)
			{
				int num4 = Mathf.Clamp(GetNextAvailableResponseButtonPosition(buttons.Length - 1, -1), 0, buttons.Length - 1);
				SetResponseButton(buttons[num4], responses[num3], target);
			}
		}
	}

	private void SetResponseButton(GUIButton button, Response response, Transform target)
	{
		if (button != null)
		{
			button.visible = true;
			button.clickable = response?.enabled ?? false;
			if (response != null)
			{
				button.SetFormattedText(response.formattedText);
			}
			else if (showUnusedButtons)
			{
				button.SetUnformattedText(" ");
			}
			button.target = target;
			button.data = response;
		}
	}

	private int GetNextAvailableResponseButtonPosition(int start, int direction)
	{
		if (buttons != null)
		{
			for (int i = start; 0 <= i && i < buttons.Length; i += direction)
			{
				if (!buttons[i].clickable)
				{
					return i;
				}
			}
		}
		return 0;
	}

	public override void StartTimer(float timeout)
	{
		if (timer != null)
		{
			if (timerEffect == null)
			{
				UnityDialogueUIControls.SetControlActive(timer, value: true);
				timerEffect = timer.GetComponent<TimerEffect>();
				UnityDialogueUIControls.SetControlActive(timer, value: false);
			}
			if (timerEffect != null)
			{
				timer.progress = 1f;
				timerEffect.duration = timeout;
				timerEffect.TimeoutHandler -= OnTimeout;
				timerEffect.TimeoutHandler += OnTimeout;
				UnityDialogueUIControls.SetControlActive(timer, value: true);
			}
		}
	}

	public void OnTimeout()
	{
		DialogueManager.instance.SendMessage("OnConversationTimeout");
	}
}
