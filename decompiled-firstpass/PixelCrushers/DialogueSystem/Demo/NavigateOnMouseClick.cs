using UnityEngine;

namespace PixelCrushers.DialogueSystem.Demo;

[AddComponentMenu("")]
public class NavigateOnMouseClick : MonoBehaviour
{
	public enum MouseButtonType
	{
		Left,
		Right,
		Middle
	}

	public string animatorSpeedParameter = "Speed";

	public float stoppingDistance = 0.5f;

	public MouseButtonType mouseButton;

	public bool ignoreClicksOnUI = true;
}
