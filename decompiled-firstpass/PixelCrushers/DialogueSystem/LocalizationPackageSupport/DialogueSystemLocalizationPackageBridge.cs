using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace PixelCrushers.DialogueSystem.LocalizationPackageSupport;

[AddComponentMenu("Pixel Crushers/Dialogue System/UI/Misc/Dialogue System Localization Package Bridge")]
public class DialogueSystemLocalizationPackageBridge : MonoBehaviour
{
	[Tooltip("Assign string tables that contain dialogue translations to this list.")]
	public List<LocalizedStringTable> localizedStringTables;

	[Tooltip("Default locale that game starts in.")]
	public Locale defaultLocale;

	[Tooltip("Title of dialogue entry field that corresponds to key in string table.")]
	public string uniqueFieldTitle = "Guid";

	[Tooltip("When Dialogue System attempts to localize non-dialogue text, use localized string tables instead of Dialogue System's default behavior of using Text Table assets.")]
	public bool replaceGetLocalizedText;

	[Tooltip("Update onscreen dialogue UI as soon as locale changes, not on next line. Limitation: Works with standard dialogue UI in single conversations (not simultaneous conversations). Override UpdateDialogueUI add different behavior.")]
	public bool updateDialogueUIImmediately = true;

	protected List<StringTable> tables = new List<StringTable>();

	protected virtual IEnumerator Start()
	{
		yield return LocalizationSettings.InitializationOperation;
		yield return new WaitForEndOfFrame();
		CacheStringTables();
		UpdateActorDisplayNames();
		LocaleIdentifier identifier = LocalizationSettings.SelectedLocale.Identifier;
		Localization.language = ((LocaleIdentifier)(ref identifier)).Code;
		LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
		if (replaceGetLocalizedText && DialogueManager.instance.overrideGetLocalizedText == null)
		{
			DialogueManager.instance.overrideGetLocalizedText = GetLocalizedTextFromStringTables;
		}
	}

	protected virtual void OnDestroy()
	{
		LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
	}

	public virtual void CacheStringTables()
	{
		tables.Clear();
		foreach (LocalizedStringTable localizedStringTable in localizedStringTables)
		{
			if (localizedStringTable != null)
			{
				tables.Add(((LocalizedTable<StringTable, StringTableEntry>)(object)localizedStringTable).GetTable());
			}
		}
	}

	protected virtual void OnSelectedLocaleChanged(Locale locale)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		if (Application.isPlaying)
		{
			CacheStringTables();
			UpdateActorDisplayNames();
			if (updateDialogueUIImmediately)
			{
				UpdateDialogueUI();
			}
			LocaleIdentifier identifier = LocalizationSettings.SelectedLocale.Identifier;
			Localization.language = ((LocaleIdentifier)(ref identifier)).Code;
		}
	}

	public virtual void UpdateActorDisplayNames()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		Locale selectedLocale = LocalizationSettings.SelectedLocale;
		LocaleIdentifier identifier = selectedLocale.Identifier;
		Localization.language = ((LocaleIdentifier)(ref identifier)).Code;
		foreach (Actor actor in DialogueManager.masterDatabase.actors)
		{
			string text = actor.LookupValue(uniqueFieldTitle);
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}
			foreach (StringTable table in tables)
			{
				StringTableEntry val = ((DetailedLocalizationTable<StringTableEntry>)(object)table)[text];
				if (val != null)
				{
					object obj;
					if (!((Object)(object)selectedLocale == (Object)(object)defaultLocale))
					{
						identifier = selectedLocale.Identifier;
						obj = "Display Name " + ((LocaleIdentifier)(ref identifier)).Code;
					}
					else
					{
						obj = "Display Name";
					}
					string field = (string)obj;
					DialogueLua.SetActorField(actor.Name, field, ((TableEntry)val).LocalizedValue);
					break;
				}
			}
		}
	}

	public virtual void OnBarkLine(Subtitle subtitle)
	{
		LocalizeSubtitle(subtitle);
	}

	public virtual void OnConversationLine(Subtitle subtitle)
	{
		LocalizeSubtitle(subtitle);
	}

	public virtual void LocalizeSubtitle(Subtitle subtitle)
	{
		if (string.IsNullOrEmpty(subtitle.formattedText.text))
		{
			return;
		}
		string text = Field.LookupValue(subtitle.dialogueEntry.fields, uniqueFieldTitle);
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		foreach (StringTable table in tables)
		{
			StringTableEntry val = ((DetailedLocalizationTable<StringTableEntry>)(object)table)[text];
			if (val != null)
			{
				string localizedValue = ((TableEntry)val).LocalizedValue;
				subtitle.formattedText = FormattedText.Parse(localizedValue);
				break;
			}
		}
	}

	public virtual void OnConversationResponseMenu(Response[] responses)
	{
		foreach (Response response in responses)
		{
			string text = Field.LookupValue(response.destinationEntry.fields, uniqueFieldTitle);
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}
			foreach (StringTable table in tables)
			{
				StringTableEntry val = ((DetailedLocalizationTable<StringTableEntry>)(object)table)[text + "_MenuText"];
				if (val != null)
				{
					response.formattedText = FormattedText.Parse(((TableEntry)val).LocalizedValue);
					break;
				}
				val = ((DetailedLocalizationTable<StringTableEntry>)(object)table)[text];
				if (val != null)
				{
					response.formattedText = FormattedText.Parse(((TableEntry)val).LocalizedValue);
					break;
				}
			}
		}
	}

	protected virtual void UpdateDialogueUI()
	{
		if (!DialogueManager.IsConversationActive)
		{
			return;
		}
		StandardUIDialogueControls conversationUIElements = DialogueManager.standardDialogueUI.conversationUIElements;
		ConversationState currentConversationState = DialogueManager.currentConversationState;
		LocalizeSubtitle(currentConversationState.subtitle);
		DialogueActor dialogueActor;
		StandardUISubtitlePanel panel = conversationUIElements.standardSubtitleControls.GetPanel(currentConversationState.subtitle, out dialogueActor);
		panel.subtitleText.text = currentConversationState.subtitle.formattedText.text;
		if (panel.portraitName != null)
		{
			Actor actor = DialogueManager.masterDatabase.GetActor(currentConversationState.subtitle.speakerInfo.id);
			if (actor != null)
			{
				panel.portraitName.text = DialogueLua.GetLocalizedActorField(actor.Name, "Display Name").asString;
			}
		}
		if (conversationUIElements.defaultMenuPanel.isOpen)
		{
			OnConversationResponseMenu(currentConversationState.pcResponses);
			Transform target = ((conversationUIElements.defaultMenuPanel.instantiatedButtons.Count > 0) ? conversationUIElements.defaultMenuPanel.instantiatedButtons[0].GetComponent<StandardUIResponseButton>().target : conversationUIElements.defaultMenuPanel.buttons[0].target);
			conversationUIElements.defaultMenuPanel.ShowResponses(currentConversationState.subtitle, currentConversationState.pcResponses, target);
		}
	}

	protected virtual string GetLocalizedTextFromStringTables(string s)
	{
		foreach (StringTable table in tables)
		{
			StringTableEntry val = ((DetailedLocalizationTable<StringTableEntry>)(object)table)[s];
			if (val != null)
			{
				return ((TableEntry)val).LocalizedValue;
			}
		}
		return s;
	}
}
