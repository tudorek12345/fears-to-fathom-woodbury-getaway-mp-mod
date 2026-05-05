using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class StandardUIFoldoutTemplate : StandardUIContentTemplate
{
	public Button foldoutButton;

	public UITextField foldoutText;

	public RectTransform interiorPanel;

	protected StandardUIInstancedContentManager contentManager { get; set; }

	public virtual void Awake()
	{
		if ((Object)(object)foldoutButton == null && DialogueDebug.logWarnings)
		{
			Debug.LogWarning("Dialogue System: Foldout Button is unassigned.", this);
		}
		if (UITextField.IsNull(foldoutText) && DialogueDebug.logWarnings)
		{
			Debug.LogWarning("Dialogue System: Foldout Text is unassigned.", this);
		}
		if (interiorPanel == null && DialogueDebug.logWarnings)
		{
			Debug.LogWarning("Dialogue System: Interior Panel is unassigned.", this);
		}
	}

	public void Assign(string text, bool expanded)
	{
		if (contentManager == null)
		{
			contentManager = new StandardUIInstancedContentManager();
		}
		contentManager.Clear();
		base.name = text;
		foldoutText.text = text;
		interiorPanel.gameObject.SetActive(expanded);
	}

	public void ToggleInterior()
	{
		interiorPanel.gameObject.SetActive(!interiorPanel.gameObject.activeSelf);
	}
}
