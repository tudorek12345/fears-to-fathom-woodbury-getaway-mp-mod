using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class ActorSubtitleColor : MonoBehaviour
{
	public enum ApplyTo
	{
		DialogueText,
		PrependedActorName
	}

	[Tooltip("Color to use for subtitle lines spoken by this actor.")]
	public Color color = Color.white;

	[Tooltip("Apply color to entire Dialogue Text or prepend actor name and apply color only to name.")]
	public ApplyTo applyTo;

	[Tooltip("If prepending actor name, separate from Dialogue Text with this string.")]
	public string prependActorNameSeparator = ": ";

	public void OnConversationLine(Subtitle subtitle)
	{
		CheckSubtitle(subtitle);
	}

	public void OnBarkLine(Subtitle subtitle)
	{
		CheckSubtitle(subtitle);
	}

	private void CheckSubtitle(Subtitle subtitle)
	{
		if (subtitle != null && subtitle.speakerInfo != null && subtitle.speakerInfo.transform == base.transform)
		{
			subtitle.formattedText.text = ProcessText(subtitle);
		}
	}

	private string ProcessText(Subtitle subtitle)
	{
		ApplyTo applyTo = this.applyTo;
		if (applyTo == ApplyTo.DialogueText || applyTo != ApplyTo.PrependedActorName)
		{
			return UITools.WrapTextInColor(subtitle.formattedText.text, color);
		}
		return UITools.WrapTextInColor(subtitle.speakerInfo.Name + prependActorNameSeparator, color) + subtitle.formattedText.text;
	}
}
