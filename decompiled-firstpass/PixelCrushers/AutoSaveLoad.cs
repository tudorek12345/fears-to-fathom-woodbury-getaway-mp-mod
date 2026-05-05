using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class AutoSaveLoad : MonoBehaviour
{
	[Tooltip("Save to this slot.")]
	public int saveSlotNumber = 1;

	[Tooltip("Don't auto-save in these scene indices.")]
	public int[] dontSaveInScenes = new int[0];

	[Tooltip("Load the saved game when this component starts.")]
	public bool loadOnStart = true;

	[Tooltip("Save when the player quits the app.")]
	public bool saveOnQuit = true;

	[Tooltip("Save when the player pauses or minimizes the app; tick this for mobile builds.")]
	public bool saveOnPause = true;

	[Tooltip("Save when the app loses focus.")]
	public bool saveOnLoseFocus;

	private void Start()
	{
		if (loadOnStart && SaveSystem.HasSavedGameInSlot(saveSlotNumber))
		{
			SaveSystem.LoadFromSlot(saveSlotNumber);
		}
	}

	private void OnEnable()
	{
		Application.wantsToQuit -= OnWantsToQuit;
		Application.wantsToQuit += OnWantsToQuit;
	}

	private void OnDisable()
	{
		Application.wantsToQuit -= OnWantsToQuit;
	}

	private bool OnWantsToQuit()
	{
		CheckSaveOnQuit();
		return true;
	}

	private void CheckSaveOnQuit()
	{
		if (base.enabled && saveOnQuit && CanSaveInThisScene())
		{
			SaveSystem.SaveToSlotImmediate(saveSlotNumber);
		}
	}

	private void OnApplicationPause(bool paused)
	{
		if (base.enabled && paused && saveOnPause && CanSaveInThisScene())
		{
			SaveSystem.SaveToSlotImmediate(saveSlotNumber);
		}
	}

	private void OnApplicationFocus(bool focusStatus)
	{
		if (base.enabled && saveOnLoseFocus && !focusStatus && CanSaveInThisScene())
		{
			SaveSystem.SaveToSlotImmediate(saveSlotNumber);
		}
	}

	private bool CanSaveInThisScene()
	{
		int currentSceneIndex = SaveSystem.GetCurrentSceneIndex();
		for (int i = 0; i < dontSaveInScenes.Length; i++)
		{
			if (currentSceneIndex == dontSaveInScenes[i])
			{
				return false;
			}
		}
		return true;
	}

	public void Restart(string startingSceneName)
	{
		SaveSystem.DeleteSavedGameInSlot(saveSlotNumber);
		SaveSystem.RestartGame(startingSceneName);
	}
}
