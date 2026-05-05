using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class OverrideDialogueUI : OverrideUIBase
{
	[Tooltip("Use this dialogue UI when this GameObject is involved in conversation.")]
	public GameObject ui;

	[Tooltip("If instantiating a prefab, keep it ready in memory instead of destroying it when conversation ends.")]
	public bool dontDestroyPrefabIntance = true;

	protected virtual void OnDestroy()
	{
		if (!Tools.IsPrefab(ui))
		{
			Object.Destroy(ui);
		}
	}
}
