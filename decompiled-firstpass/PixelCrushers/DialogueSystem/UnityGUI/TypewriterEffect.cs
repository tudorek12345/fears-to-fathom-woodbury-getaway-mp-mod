using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[AddComponentMenu("")]
public class TypewriterEffect : GUIEffect
{
	public float charactersPerSecond = 50f;

	public AudioClip audioClip;

	private const string RichTextBoldOpen = "<b>";

	private const string RichTextBoldClose = "</b>";

	private const string RichTextItalicOpen = "<i>";

	private const string RichTextItalicClose = "</i>";

	private const string RichTextColorOpenPrefix = "<color=";

	private const string RichTextColorClose = "</color>";

	public bool IsPlaying { get; private set; }

	public override IEnumerator Play()
	{
		GUILabel control = GetComponent<GUILabel>();
		if (control == null)
		{
			yield break;
		}
		IsPlaying = true;
		control.currentLength = 0;
		while (control.currentLength + 1 < control.text.Length)
		{
			float seconds = 1f / charactersPerSecond;
			if (!DialogueTime.isPaused)
			{
				if (audioClip != null && control.currentLength > 0)
				{
					control.PlaySound(audioClip);
				}
				AdvanceOneCharacter(control);
			}
			yield return StartCoroutine(DialogueTime.WaitForSeconds(seconds));
		}
		control.currentLength = control.text.Length;
		control.ResetClosureTags();
		IsPlaying = false;
	}

	private void AdvanceOneCharacter(GUILabel control)
	{
		if (control.text[control.currentLength] == '<')
		{
			if (string.Compare(control.text, control.currentLength, "<b>", 0, "<b>".Length) == 0)
			{
				control.currentLength += "<b>".Length;
				control.PushClosureTag("</b>");
			}
			else if (string.Compare(control.text, control.currentLength, "</b>", 0, "</b>".Length) == 0)
			{
				control.currentLength += "</b>".Length;
				control.PopClosureTag();
			}
			else if (string.Compare(control.text, control.currentLength, "<i>", 0, "<i>".Length) == 0)
			{
				control.currentLength += "<i>".Length;
				control.PushClosureTag("</i>");
			}
			else if (string.Compare(control.text, control.currentLength, "</i>", 0, "</i>".Length) == 0)
			{
				control.currentLength += "</i>".Length;
				control.PopClosureTag();
			}
			if (string.Compare(control.text, control.currentLength, "<color=", 0, "<color=".Length) == 0)
			{
				control.currentLength += "<color=".Length + 10;
				control.PushClosureTag("</color>");
			}
			else if (string.Compare(control.text, control.currentLength, "</color>", 0, "</color>".Length) == 0)
			{
				control.currentLength += "</color>".Length;
				control.PopClosureTag();
			}
		}
		else
		{
			control.currentLength++;
		}
	}

	public override void Stop()
	{
		base.Stop();
		IsPlaying = false;
		GUILabel component = GetComponent<GUILabel>();
		if (component != null)
		{
			component.currentLength = component.text.Length;
			component.ResetClosureTags();
		}
	}
}
