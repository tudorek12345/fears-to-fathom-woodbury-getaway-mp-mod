using UnityEngine;

namespace PixelCrushers.DialogueSystem.Wrappers;

[AddComponentMenu("")]
public class CinemachineCameraPriorityOnDialogueEvent : PixelCrushers.DialogueSystem.CinemachineCameraPriorityOnDialogueEvent
{
	private void Reset()
	{
		Debug.LogWarning("Support for " + GetType().Name + " must be enabled using Tools > Pixel Crushers > Dialogue System > Welcome Window.", this);
	}
}
