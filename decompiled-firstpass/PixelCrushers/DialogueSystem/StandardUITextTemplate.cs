using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class StandardUITextTemplate : StandardUIContentTemplate
{
	[Tooltip("Text UI element.")]
	[SerializeField]
	private UITextField m_text;

	public UITextField text
	{
		get
		{
			return m_text;
		}
		set
		{
			m_text = value;
		}
	}

	public virtual void Awake()
	{
		if (UITextField.IsNull(text))
		{
			text.uiText = GetComponentInChildren<Text>();
			if (UITextField.IsNull(text) && Debug.isDebugBuild)
			{
				Debug.LogError("Dialogue System: UI Text is unassigned.", this);
			}
		}
	}

	public void Assign(string text)
	{
		base.name = text;
		this.text.text = text;
	}
}
