using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class StandardUIColorText : MonoBehaviour
{
	public Color color;

	public UITextField text;

	private Color originalColor;

	private void Awake()
	{
		if (text.gameObject == null)
		{
			text.uiText = GetComponentInChildren<Text>();
		}
		if (text.gameObject == null)
		{
			text.textMeshProUGUI = GetComponentInChildren<TextMeshProUGUI>();
		}
		originalColor = text.color;
	}

	public void ApplyColor()
	{
		originalColor = text.color;
		text.color = color;
	}

	public void UndoColor()
	{
		text.color = originalColor;
	}
}
