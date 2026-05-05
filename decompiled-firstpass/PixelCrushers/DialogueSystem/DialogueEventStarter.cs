using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public abstract class DialogueEventStarter : MonoBehaviour
{
	[Tooltip("Only trigger once for this instance of the scene, then destroy this component. NOTE: This is not persistent across scene changes or saved games. It only applies to the current instance of this scene. To make something only happen once for the player's playthrough (including scene changes and saved games), use persistent data components.")]
	public bool once;

	protected virtual bool useOnce => true;

	protected void DestroyIfOnce()
	{
		if (once)
		{
			Object.Destroy(this);
		}
	}
}
