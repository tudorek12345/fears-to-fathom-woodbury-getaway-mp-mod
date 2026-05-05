using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public abstract class AbstractUsableUI : MonoBehaviour
{
	public abstract void Show(string useMessage);

	public abstract void Hide();

	public abstract void UpdateDisplay(bool inRange);
}
