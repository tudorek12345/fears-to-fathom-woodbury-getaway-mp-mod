using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[AddComponentMenu("")]
public class FlashEffect : GUIEffect
{
	public float interval = 0.5f;

	private GUIControl control;

	public override IEnumerator Play()
	{
		control = GetComponent<GUIControl>();
		if (control == null)
		{
			yield break;
		}
		control.visible = true;
		while (true)
		{
			yield return StartCoroutine(DialogueTime.WaitForSeconds(interval));
			control.visible = !control.visible;
		}
	}
}
