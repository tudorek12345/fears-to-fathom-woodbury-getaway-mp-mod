using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public abstract class OverrideUIBase : MonoBehaviour
{
	[Tooltip("When both participants have overrides, higher priority takes precedence.")]
	public int priority;
}
