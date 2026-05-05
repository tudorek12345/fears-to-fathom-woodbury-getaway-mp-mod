using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class PauseGameOnConversation : MonoBehaviour
{
	private float preConversationTimeScale = 1f;

	public void OnConversationStart(Transform actor)
	{
		if (base.enabled)
		{
			preConversationTimeScale = Time.timeScale;
			Time.timeScale = 0f;
		}
	}

	public void OnConversationEnd(Transform actor)
	{
		if (base.enabled)
		{
			Time.timeScale = preConversationTimeScale;
		}
	}
}
