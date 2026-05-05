using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class BarkDialogueUI : MonoBehaviour, IDialogueUI
{
	[Tooltip("Play sequence associated with bark. ConversationView already plays it, but tick this if bark UI needs to wait for sequence to end.")]
	public bool playSequence;

	public event EventHandler<SelectedResponseEventArgs> SelectedResponseHandler;

	public void Open()
	{
	}

	public void Close()
	{
	}

	public void ShowSubtitle(Subtitle subtitle)
	{
		StartCoroutine(BarkController.Bark(subtitle, !playSequence));
	}

	public void HideSubtitle(Subtitle subtitle)
	{
	}

	public void ShowResponses(Subtitle subtitle, Response[] responses, float timeout)
	{
		if (responses.Length != 0)
		{
			this.SelectedResponseHandler(this, new SelectedResponseEventArgs(responses[0]));
		}
	}

	public void HideResponses()
	{
	}

	public void ShowQTEIndicator(int index)
	{
	}

	public void HideQTEIndicator(int index)
	{
	}

	public void ShowAlert(string message, float duration)
	{
	}

	public void HideAlert()
	{
	}
}
