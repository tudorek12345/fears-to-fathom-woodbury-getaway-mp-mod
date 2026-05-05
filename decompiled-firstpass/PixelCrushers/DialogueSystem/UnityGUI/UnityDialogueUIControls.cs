using System;

namespace PixelCrushers.DialogueSystem.UnityGUI;

[Serializable]
public static class UnityDialogueUIControls
{
	public static void SetControlActive(GUIControl control, bool value)
	{
		if (control != null)
		{
			if (value && !control.gameObject.activeSelf)
			{
				control.gameObject.SetActive(value: true);
				CheckSlideEffect(control);
			}
			else if (!value && control.gameObject.activeSelf)
			{
				control.gameObject.SetActive(value: false);
			}
		}
	}

	private static void CheckSlideEffect(GUIControl control)
	{
		SlideEffect component = control.GetComponent<SlideEffect>();
		if (component != null && component.trigger == GUIEffectTrigger.OnEnable)
		{
			control.visible = false;
		}
	}
}
