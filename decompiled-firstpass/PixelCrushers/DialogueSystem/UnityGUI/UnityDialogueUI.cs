using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[AddComponentMenu("")]
public class UnityDialogueUI : AbstractDialogueUI
{
	public GUIRoot guiRoot;

	public UnityDialogueControls dialogue;

	public GUIControl[] qteIndicators;

	public UnityAlertControls alert;

	private UnityUIRoot unityUIRoot;

	private UnityQTEControls unityQTEControls;

	public override AbstractUIRoot uiRootControls => unityUIRoot;

	public override AbstractDialogueUIControls dialogueControls => dialogue;

	public override AbstractUIQTEControls qteControls => unityQTEControls;

	public override AbstractUIAlertControls alertControls => alert;

	public override void Awake()
	{
		base.Awake();
		FindControls();
	}

	public override void Open()
	{
		base.Open();
		dialogue.responseMenu.Hide();
	}

	private void FindControls()
	{
		if (guiRoot == null)
		{
			guiRoot = GetComponentInChildren<GUIRoot>();
		}
		unityUIRoot = new UnityUIRoot(guiRoot);
		unityQTEControls = new UnityQTEControls(qteIndicators);
		SetupContinueButton(dialogue.npcSubtitle.continueButton);
		SetupContinueButton(dialogue.pcSubtitle.continueButton);
		SetupContinueButton(alert.continueButton);
		if (!DialogueDebug.logErrors)
		{
			return;
		}
		if (guiRoot == null)
		{
			Debug.LogError(string.Format("{0}: UnityDialogueUI can't find GUIRoot and won't be able to display dialogue.", new object[1] { "Dialogue System" }));
		}
		if (DialogueDebug.logWarnings)
		{
			if (dialogue.npcSubtitle.line == null)
			{
				Debug.LogWarning(string.Format("{0}: UnityDialogueUI NPC Subtitle Line needs to be assigned.", new object[1] { "Dialogue System" }));
			}
			if (dialogue.pcSubtitle.line == null)
			{
				Debug.LogWarning(string.Format("{0}: UnityDialogueUI PC Subtitle Line needs to be assigned.", new object[1] { "Dialogue System" }));
			}
			if (dialogue.responseMenu.buttons.Length == 0)
			{
				Debug.LogWarning(string.Format("{0}: UnityDialogueUI Response buttons need to be assigned.", new object[1] { "Dialogue System" }));
			}
			if (alert.line == null)
			{
				Debug.LogWarning(string.Format("{0}: UnityDialogueUI Alert Line needs to be assigned.", new object[1] { "Dialogue System" }));
			}
		}
	}

	private void SetupContinueButton(GUIButton continueButton)
	{
		if (continueButton != null)
		{
			if (string.IsNullOrEmpty(continueButton.message) || string.Equals(continueButton.message, "OnClick"))
			{
				continueButton.message = "OnContinue";
			}
			if (continueButton.target == null)
			{
				continueButton.target = base.transform;
			}
		}
	}
}
