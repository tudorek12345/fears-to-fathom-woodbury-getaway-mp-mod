using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class DialogueActor : MonoBehaviour
{
	[Serializable]
	public class BarkUISettings
	{
		[Tooltip("If a prefab, Dialogue Actor will instantiate it at runtime.")]
		public AbstractBarkUI barkUI;

		[Tooltip("If instantiating bark UI prefab, offset it this far from Dialogue Actor's origin.")]
		public Vector3 barkUIOffset = new Vector3(0f, 2f, 0f);
	}

	public enum UseMenuPanelFor
	{
		OnlyMe,
		MeAndResponsesToMe
	}

	[Serializable]
	public class StandardDialogueUISettings
	{
		[Tooltip("If using Standard Dialogue UI, subtitle panel to use for this actor.")]
		public SubtitlePanelNumber subtitlePanelNumber;

		[Tooltip("The panel to use if Subtitle Panel Number is set to Custom.")]
		public StandardUISubtitlePanel customSubtitlePanel;

		[Tooltip("If instantiating subtitle panel prefab, offset it this far from Dialogue Actor's origin.")]
		public Vector3 customSubtitlePanelOffset = new Vector3(0f, 0f, 0f);

		[Tooltip("If using Standard Dialogue UI, menu panel to use for this actor.")]
		public MenuPanelNumber menuPanelNumber;

		[Tooltip("The panel to use if Menu Panel Number is set to Custom.")]
		public StandardUIMenuPanel customMenuPanel;

		[Tooltip("If instantiating menu panel prefab, offset it this far from Dialogue Actor's origin.")]
		public Vector3 customMenuPanelOffset = new Vector3(0f, 0f, 0f);

		[Tooltip("If Only Me, only use this menu panel when this Dialogue Actor is the respondent.\nIf MeAndResponsesToMe, use this menu panel when this Dialogue Actor is the response or the character being responded to (i.e., the last one to speak).")]
		public UseMenuPanelFor useMenuPanelFor;

		[Tooltip("If assigned, animator controller that runs this actor's animated portrait. It should animate an Image component, not a SpriteRenderer.")]
		public RuntimeAnimatorController portraitAnimatorController;

		[Tooltip("Specify subtitle color for this actor.")]
		public bool setSubtitleColor;

		[Tooltip("Prepend actor name and apply color only to name.")]
		public bool applyColorToPrependedName;

		[Tooltip("If prepending actor name, separate from Dialogue Text with this string.")]
		public string prependActorNameSeparator = ": ";

		[Tooltip("If prepending actor name, format this way, where {0} is name + separator, and {1} is Dialogue Text.")]
		public string prependActorNameFormat = "{0}{1}";

		[Tooltip("Color to use for this actor's subtitles.")]
		public Color subtitleColor = Color.white;
	}

	[Tooltip("Use this actor name in conversations.")]
	[ActorPopup(true)]
	[FormerlySerializedAs("overrideName")]
	public string actor;

	[Tooltip("Name used when saving persistent data. If blank, use actor name.")]
	[FormerlySerializedAs("internalName")]
	public string persistentDataName;

	[Tooltip("Optional portrait. If unassigned, will use portrait of actor in database. This field allows you to assign a Texture.")]
	public Texture2D portrait;

	[Tooltip("Optional portrait. If unassigned, will use portrait of actor in database. This field allows you to assign a Sprite.")]
	public Sprite spritePortrait;

	public BarkUISettings barkUISettings = new BarkUISettings();

	public StandardDialogueUISettings standardDialogueUISettings = new StandardDialogueUISettings();

	protected virtual void Awake()
	{
		SetupBarkUI();
		SetupDialoguePanels();
	}

	public virtual Sprite GetPortraitSprite()
	{
		return UITools.GetSprite(portrait, spritePortrait);
	}

	protected virtual void SetupBarkUI()
	{
		if (barkUISettings.barkUI != null && Tools.IsPrefab(barkUISettings.barkUI.gameObject))
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(barkUISettings.barkUI.gameObject);
			gameObject.transform.SetParent(base.transform);
			gameObject.transform.localPosition = barkUISettings.barkUIOffset;
			gameObject.transform.localRotation = Quaternion.identity;
			barkUISettings.barkUI = gameObject.GetComponent<AbstractBarkUI>();
		}
	}

	protected virtual void SetupDialoguePanels()
	{
		if (standardDialogueUISettings.subtitlePanelNumber == SubtitlePanelNumber.Custom && standardDialogueUISettings.customSubtitlePanel != null && Tools.IsPrefab(standardDialogueUISettings.customSubtitlePanel.gameObject))
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(standardDialogueUISettings.customSubtitlePanel.gameObject, base.transform.position, base.transform.rotation);
			gameObject.transform.SetParent(base.transform);
			gameObject.transform.localPosition = standardDialogueUISettings.customSubtitlePanelOffset;
			gameObject.transform.localRotation = Quaternion.identity;
			standardDialogueUISettings.customSubtitlePanel = gameObject.GetComponent<StandardUISubtitlePanel>();
		}
		if (standardDialogueUISettings.menuPanelNumber == MenuPanelNumber.Custom && standardDialogueUISettings.customMenuPanel != null && Tools.IsPrefab(standardDialogueUISettings.customMenuPanel.gameObject))
		{
			GameObject gameObject2 = UnityEngine.Object.Instantiate(standardDialogueUISettings.customMenuPanel.gameObject, base.transform.position, base.transform.rotation);
			gameObject2.transform.SetParent(base.transform);
			gameObject2.transform.localPosition = standardDialogueUISettings.customMenuPanelOffset;
			gameObject2.transform.localRotation = Quaternion.identity;
			standardDialogueUISettings.customMenuPanel = gameObject2.GetComponent<StandardUIMenuPanel>();
		}
	}

	protected virtual void OnEnable()
	{
		if (!string.IsNullOrEmpty(actor))
		{
			CharacterInfo.RegisterActorTransform(actor, base.transform);
		}
	}

	protected virtual void OnDisable()
	{
		if (!string.IsNullOrEmpty(actor))
		{
			CharacterInfo.UnregisterActorTransform(actor, base.transform);
		}
	}

	public virtual string GetName()
	{
		return GetActorName();
	}

	public virtual string GetActorName()
	{
		string text = (string.IsNullOrEmpty(actor) ? base.name : actor);
		string localizedDisplayNameInDatabase = CharacterInfo.GetLocalizedDisplayNameInDatabase(DialogueLua.GetActorField(text, "Name").asString);
		if (!string.IsNullOrEmpty(localizedDisplayNameInDatabase))
		{
			text = localizedDisplayNameInDatabase;
		}
		if (text.Contains("[lua") || text.Contains("[var") || text.Contains("[em"))
		{
			return FormattedText.Parse(text, DialogueManager.masterDatabase.emphasisSettings).text;
		}
		return text;
	}

	public virtual string GetPersistentDataName()
	{
		if (!string.IsNullOrEmpty(persistentDataName))
		{
			return persistentDataName;
		}
		return GetActorName();
	}

	public virtual SubtitlePanelNumber GetSubtitlePanelNumber()
	{
		return standardDialogueUISettings.subtitlePanelNumber;
	}

	public virtual void SetSubtitlePanelNumber(SubtitlePanelNumber newSubtitlePanelNumber)
	{
		standardDialogueUISettings.subtitlePanelNumber = newSubtitlePanelNumber;
		if (DialogueManager.isConversationActive && DialogueManager.dialogueUI is IStandardDialogueUI)
		{
			(DialogueManager.dialogueUI as IStandardDialogueUI).SetActorSubtitlePanelNumber(this, newSubtitlePanelNumber);
		}
	}

	public virtual MenuPanelNumber GetMenuPanelNumber()
	{
		return standardDialogueUISettings.menuPanelNumber;
	}

	public virtual void SetMenuPanelNumber(MenuPanelNumber newMenuPanelNumber)
	{
		standardDialogueUISettings.menuPanelNumber = newMenuPanelNumber;
		if (DialogueManager.isConversationActive && DialogueManager.dialogueUI is IStandardDialogueUI)
		{
			(DialogueManager.dialogueUI as IStandardDialogueUI).SetActorMenuPanelNumber(this, newMenuPanelNumber);
		}
	}

	public virtual string AdjustSubtitleColor(Subtitle subtitle)
	{
		string text = subtitle.formattedText.text;
		if (!standardDialogueUISettings.setSubtitleColor)
		{
			return text;
		}
		if (standardDialogueUISettings.applyColorToPrependedName)
		{
			if (string.IsNullOrEmpty(subtitle.speakerInfo.Name))
			{
				return text;
			}
			string text2 = UITools.WrapTextInColor(subtitle.speakerInfo.Name + standardDialogueUISettings.prependActorNameSeparator, standardDialogueUISettings.subtitleColor);
			return FormattedText.Parse(string.Format(standardDialogueUISettings.prependActorNameFormat, new object[2] { text2, text })).text;
		}
		return UITools.WrapTextInColor(text, standardDialogueUISettings.subtitleColor);
	}

	public static DialogueActor GetDialogueActorComponent(Transform t)
	{
		if (t == null)
		{
			return null;
		}
		return t.GetComponent<DialogueActor>() ?? t.GetComponentInChildren<DialogueActor>() ?? t.GetComponentInParent<DialogueActor>();
	}

	public static string GetActorName(Transform t)
	{
		if (t == null)
		{
			return string.Empty;
		}
		DialogueActor dialogueActorComponent = GetDialogueActorComponent(t);
		if (!(dialogueActorComponent != null) || !dialogueActorComponent.isActiveAndEnabled)
		{
			return CharacterInfo.GetLocalizedDisplayNameInDatabase(t.name);
		}
		return dialogueActorComponent.GetName();
	}

	public static string GetPersistentDataName(Transform t)
	{
		if (t == null)
		{
			return string.Empty;
		}
		DialogueActor dialogueActorComponent = GetDialogueActorComponent(t);
		if (dialogueActorComponent != null)
		{
			if (!string.IsNullOrEmpty(dialogueActorComponent.persistentDataName))
			{
				return dialogueActorComponent.persistentDataName;
			}
			if (!string.IsNullOrEmpty(dialogueActorComponent.actor))
			{
				return dialogueActorComponent.actor;
			}
		}
		return t.name;
	}

	public static SubtitlePanelNumber GetSubtitlePanelNumber(Transform t)
	{
		DialogueActor dialogueActorComponent = GetDialogueActorComponent(t);
		if (!(dialogueActorComponent != null))
		{
			return SubtitlePanelNumber.Default;
		}
		return dialogueActorComponent.GetSubtitlePanelNumber();
	}

	public static IBarkUI GetBarkUI(Transform t)
	{
		if (t == null)
		{
			return null;
		}
		DialogueActor dialogueActorComponent = GetDialogueActorComponent(t);
		if (!(dialogueActorComponent != null))
		{
			return t.GetComponentInChildren(typeof(IBarkUI)) as IBarkUI;
		}
		return dialogueActorComponent.barkUISettings.barkUI;
	}
}
