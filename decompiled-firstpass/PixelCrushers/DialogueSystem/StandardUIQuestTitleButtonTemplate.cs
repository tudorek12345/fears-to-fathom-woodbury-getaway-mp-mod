using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class StandardUIQuestTitleButtonTemplate : StandardUIContentTemplate
{
	[Header("Quest Title Button")]
	[Tooltip("Button UI element.")]
	public Button button;

	[Tooltip("Label text to set on button.")]
	public UITextField label;

	[Header("Tracking Toggle")]
	public StandardUIToggleTemplate trackToggleTemplate;

	public virtual void Awake()
	{
		if ((Object)(object)button == null && DialogueDebug.logWarnings)
		{
			Debug.LogWarning("Dialogue System: UI Button is unassigned.", this);
		}
		if (trackToggleTemplate == null && DialogueDebug.logWarnings)
		{
			Debug.LogWarning("Dialogue System: UI Track Toggle Template is unassigned.", this);
		}
	}

	public virtual void Assign(string questName, string displayName, ToggleChangedDelegate trackToggleDelegate)
	{
		if (UITextField.IsNull(label))
		{
			label.uiText = ((Component)(object)button).GetComponentInChildren<Text>();
		}
		base.name = questName;
		label.text = displayName;
		bool isVisible = QuestLog.IsQuestActive(questName) && QuestLog.IsQuestTrackingAvailable(questName);
		trackToggleTemplate.Assign(isVisible, QuestLog.IsQuestTrackingEnabled(questName), questName, trackToggleDelegate);
	}
}
