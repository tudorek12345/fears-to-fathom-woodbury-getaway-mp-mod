using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class UnityUISelectorElements : MonoBehaviour
{
	public Graphic mainGraphic;

	public Text nameText;

	public Text useMessageText;

	public Color inRangeColor = Color.yellow;

	public Color outOfRangeColor = Color.gray;

	public Graphic reticleInRange;

	public Graphic reticleOutOfRange;

	public UnityUISelectorDisplay.AnimationTransitions animationTransitions = new UnityUISelectorDisplay.AnimationTransitions();

	public static UnityUISelectorElements instance;

	private void Awake()
	{
		instance = this;
		Tools.DeprecationWarning(this);
	}
}
