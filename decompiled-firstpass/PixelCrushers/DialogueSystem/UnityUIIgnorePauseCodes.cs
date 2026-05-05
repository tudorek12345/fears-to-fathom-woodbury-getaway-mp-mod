using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
[DisallowMultipleComponent]
public class UnityUIIgnorePauseCodes : MonoBehaviour
{
	private Text control;

	public void Awake()
	{
		control = GetComponent<Text>();
		Tools.DeprecationWarning(this);
	}

	public void Start()
	{
		CheckText();
	}

	public void OnEnable()
	{
		CheckText();
	}

	public void CheckText()
	{
		if ((Object)(object)control != null && control.text.Contains("\\"))
		{
			StartCoroutine(Clean());
		}
	}

	private IEnumerator Clean()
	{
		control.text = UITools.StripRPGMakerCodes(control.text);
		yield return null;
		control.text = UITools.StripRPGMakerCodes(control.text);
	}
}
