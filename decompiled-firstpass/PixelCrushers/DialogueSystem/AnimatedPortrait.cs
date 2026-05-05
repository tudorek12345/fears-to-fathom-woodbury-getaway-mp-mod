using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class AnimatedPortrait : MonoBehaviour
{
	[Tooltip("Animator controller that runs this actor's animated portrait. It should animate an Image component, not a SpriteRenderer.")]
	public RuntimeAnimatorController animatorController;
}
