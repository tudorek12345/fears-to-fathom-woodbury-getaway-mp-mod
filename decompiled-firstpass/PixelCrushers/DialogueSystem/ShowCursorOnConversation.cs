using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class ShowCursorOnConversation : MonoBehaviour
{
	private bool wasCursorVisible;

	private CursorLockMode savedLockState;

	public void OnConversationStart(Transform actor)
	{
		wasCursorVisible = Cursor.visible;
		savedLockState = Cursor.lockState;
		StartCoroutine(ShowCursorAfterOneFrame());
	}

	private IEnumerator ShowCursorAfterOneFrame()
	{
		yield return null;
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
	}

	public void OnConversationEnd(Transform actor)
	{
		Cursor.visible = wasCursorVisible;
		Cursor.lockState = savedLockState;
	}
}
