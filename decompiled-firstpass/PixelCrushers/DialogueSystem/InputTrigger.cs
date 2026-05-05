using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class InputTrigger
{
	[Tooltip("This key fires the trigger.")]
	public KeyCode key;

	[Tooltip("This button fires the trigger. The button name must be defined in your project's Input Settings.")]
	public string buttonName = string.Empty;

	public bool isDown
	{
		get
		{
			if (DialogueManager.IsDialogueSystemInputDisabled())
			{
				return false;
			}
			if (!InputDeviceManager.IsKeyDown(key))
			{
				if (!string.IsNullOrEmpty(buttonName))
				{
					return DialogueManager.getInputButtonDown(buttonName);
				}
				return false;
			}
			return true;
		}
	}

	public bool IsDown => isDown;

	public InputTrigger()
	{
	}

	public InputTrigger(KeyCode key)
	{
		this.key = key;
	}

	public InputTrigger(string buttonName)
	{
		this.buttonName = buttonName;
	}

	public InputTrigger(KeyCode key, string buttonName)
	{
		this.key = key;
		this.buttonName = buttonName;
	}
}
