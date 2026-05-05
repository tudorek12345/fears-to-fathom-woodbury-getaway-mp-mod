using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class ConversationLogger : MonoBehaviour
{
	[Tooltip("Log player lines in this color.")]
	public Color playerColor = Color.blue;

	[Tooltip("Log NPC lines in this color.")]
	public Color npcColor = Color.red;

	public void OnConversationStart(Transform actor)
	{
		Debug.Log(string.Format("{0}: Starting conversation with {1}", new object[2]
		{
			base.name,
			GetActorName(actor)
		}));
	}

	public void OnConversationLine(Subtitle subtitle)
	{
		if (!((subtitle == null) | (subtitle.formattedText == null) | string.IsNullOrEmpty(subtitle.formattedText.text)))
		{
			string text = ((subtitle.speakerInfo != null && subtitle.speakerInfo.transform != null) ? subtitle.speakerInfo.transform.name : "(null speaker)");
			Debug.Log(string.Format("<color={0}>{1}: {2}</color>", new object[3]
			{
				GetActorColor(subtitle),
				text,
				subtitle.formattedText.text
			}));
		}
	}

	public void OnConversationEnd(Transform actor)
	{
		Debug.Log($"{base.name}: Ending conversation with {GetActorName(actor)}");
	}

	private string GetActorName(Transform actor)
	{
		if (!(actor != null))
		{
			return "(null transform)";
		}
		return actor.name;
	}

	private string GetActorColor(Subtitle subtitle)
	{
		if ((subtitle == null) | (subtitle.speakerInfo == null))
		{
			return "white";
		}
		return Tools.ToWebColor(subtitle.speakerInfo.isPlayer ? playerColor : npcColor);
	}

	public void OnPrepareConversationLine(DialogueEntry entry)
	{
		if (entry != null)
		{
			Debug.Log($"Preparing line {entry.currentDialogueText}");
		}
	}

	public void OnConversationLineCancelled(Subtitle subtitle)
	{
		if (!((subtitle == null) | (subtitle.formattedText == null) | string.IsNullOrEmpty(subtitle.formattedText.text)))
		{
			string text = ((subtitle.speakerInfo != null && subtitle.speakerInfo.transform != null) ? subtitle.speakerInfo.transform.name : "(null speaker)");
			Debug.Log(string.Format("<color={0}>Line cancelled - {1}: {2}</color>", new object[3]
			{
				GetActorColor(subtitle),
				text,
				subtitle.formattedText.text
			}));
		}
	}

	public void OnConversationLineEnd(Subtitle subtitle)
	{
		if (!((subtitle == null) | (subtitle.formattedText == null) | string.IsNullOrEmpty(subtitle.formattedText.text)))
		{
			string text = ((subtitle.speakerInfo != null && subtitle.speakerInfo.transform != null) ? subtitle.speakerInfo.transform.name : "(null speaker)");
			Debug.Log(string.Format("<color={0}>Line ended - {1}: {2}</color>", new object[3]
			{
				GetActorColor(subtitle),
				text,
				subtitle.formattedText.text
			}));
		}
	}

	public void OnConversationResponseMenu(Response[] responses)
	{
		Debug.Log("Showing conversation response menu.");
	}

	public void OnConversationTimeout()
	{
		Debug.Log("Conversation timed out.");
	}

	public void OnLinkedConversationStart(Transform actor)
	{
		Debug.Log("Starting linked conversation.");
	}
}
