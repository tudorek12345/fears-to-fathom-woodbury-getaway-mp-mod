using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class LocalizeUIText : LocalizeUI
{
	private void Awake()
	{
		Tools.DeprecationWarning(this, "Use LocalizeUI instead.");
	}

	public virtual void LocalizeText()
	{
		UpdateText();
	}
}
