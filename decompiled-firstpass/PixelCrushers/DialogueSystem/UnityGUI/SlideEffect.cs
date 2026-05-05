using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[AddComponentMenu("")]
public class SlideEffect : GUIEffect
{
	public enum SlideDirection
	{
		FromBottom,
		FromTop,
		FromLeft,
		FromRight
	}

	public SlideDirection direction;

	public float duration = 0.3f;

	private GUIControl control;

	public override IEnumerator Play()
	{
		control = GetComponent<GUIControl>();
		if (control == null)
		{
			yield break;
		}
		control.visible = false;
		Rect rect = control.scaledRect.GetPixelRect();
		float startTime = DialogueTime.time;
		float endTime = startTime + duration;
		while (DialogueTime.time < endTime)
		{
			float num = Mathf.Clamp((DialogueTime.time - startTime) / duration, 0f, 1f);
			switch (direction)
			{
			case SlideDirection.FromBottom:
				control.Offset = new Vector2(0f, (1f - num) * ((float)Screen.height - rect.y));
				break;
			case SlideDirection.FromTop:
				control.Offset = new Vector2(0f, (0f - (1f - num)) * (rect.y + rect.height));
				break;
			case SlideDirection.FromLeft:
				control.Offset = new Vector2((0f - (1f - num)) * (rect.x + rect.width), 0f);
				break;
			case SlideDirection.FromRight:
				control.Offset = new Vector2((1f - num) * ((float)Screen.width - rect.x), 0f);
				break;
			}
			control.visible = true;
			control.Refresh();
			yield return null;
		}
		control.Offset = Vector2.zero;
		control.visible = true;
		control.Refresh();
	}
}
