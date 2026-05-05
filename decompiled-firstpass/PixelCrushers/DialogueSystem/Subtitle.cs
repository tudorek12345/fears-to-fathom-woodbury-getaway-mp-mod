using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public class Subtitle
{
	public CharacterInfo speakerInfo;

	public CharacterInfo listenerInfo;

	public FormattedText formattedText;

	public string sequence;

	public string responseMenuSequence;

	public DialogueEntry dialogueEntry;

	public string entrytag = string.Empty;

	public ActiveConversationRecord activeConversationRecord;

	public Subtitle(CharacterInfo speakerInfo, CharacterInfo listenerInfo, FormattedText formattedText, string sequence, string responseMenuSequence, DialogueEntry dialogueEntry)
	{
		this.speakerInfo = speakerInfo;
		this.listenerInfo = listenerInfo;
		this.formattedText = formattedText;
		this.sequence = sequence;
		this.responseMenuSequence = responseMenuSequence;
		this.dialogueEntry = dialogueEntry;
		entrytag = null;
		CheckVariableInputPrompt();
	}

	public Subtitle(CharacterInfo speakerInfo, CharacterInfo listenerInfo, FormattedText formattedText, string sequence, string responseMenuSequence, DialogueEntry dialogueEntry, string entrytag)
	{
		this.speakerInfo = speakerInfo;
		this.listenerInfo = listenerInfo;
		this.formattedText = formattedText;
		this.sequence = sequence;
		this.responseMenuSequence = responseMenuSequence;
		this.dialogueEntry = dialogueEntry;
		this.entrytag = entrytag;
		CheckVariableInputPrompt();
	}

	private void CheckVariableInputPrompt()
	{
		if (formattedText != null && formattedText.hasVariableInputPrompt)
		{
			sequence = string.Format("{0}{1}TextInput(Text Field UI,{2},{2})", sequence, string.IsNullOrEmpty(sequence) ? string.Empty : "; ", formattedText.variableInputPrompt);
		}
	}

	public Sprite GetSpeakerPortrait()
	{
		if (speakerInfo == null)
		{
			return null;
		}
		if (formattedText == null)
		{
			return speakerInfo.portrait;
		}
		if (formattedText.pic != 0)
		{
			return speakerInfo.GetPicOverride(formattedText.pic);
		}
		if (formattedText.picActor != 0)
		{
			return speakerInfo.GetPicOverride(formattedText.picActor);
		}
		if (formattedText.picConversant != 0 && listenerInfo != null)
		{
			return listenerInfo.GetPicOverride(formattedText.picConversant);
		}
		return speakerInfo.portrait;
	}
}
