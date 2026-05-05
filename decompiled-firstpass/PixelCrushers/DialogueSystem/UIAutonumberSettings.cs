using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[Serializable]
public class UIAutonumberSettings
{
	[Tooltip("Enable autonumbering of responses.")]
	public bool enabled;

	[Tooltip("Bind regular number keys as hotkeys.")]
	public bool regularNumberHotkeys = true;

	[Tooltip("Bind numpad keys as hotkeys.")]
	public bool numpadHotkeys;

	[Tooltip("Format for response button text, where {0} is hotkey number and {1} is menu text.")]
	public string format = "{0}. {1}";
}
