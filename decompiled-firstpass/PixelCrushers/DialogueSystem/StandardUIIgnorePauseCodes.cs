using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
[DisallowMultipleComponent]
public class StandardUIIgnorePauseCodes : MonoBehaviour
{
	private UITextField text = new UITextField();

	public void Start()
	{
		text.uiText = GetComponentInChildren<Text>();
		text.textMeshProUGUI = GetComponentInChildren<TextMeshProUGUI>();
		CheckText();
	}

	public void OnEnable()
	{
		CheckText();
	}

	public void CheckText()
	{
		if (!string.IsNullOrEmpty(text.text) && text.text.Contains("\\"))
		{
			StartCoroutine(Clean());
		}
	}

	private IEnumerator Clean()
	{
		text.text = UITools.StripRPGMakerCodes(text.text);
		yield return null;
		text.text = UITools.StripRPGMakerCodes(text.text);
	}
}
