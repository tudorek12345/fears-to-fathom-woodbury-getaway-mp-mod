using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class SaveSystemMethods : MonoBehaviour
{
	[Tooltip("Scene to load in LoadOrRestart method if no saved game exists yet.")]
	public string defaultStartingSceneName;

	public virtual void SaveSlot(int slotNumber)
	{
		SaveSystem.SaveToSlot(slotNumber);
	}

	public virtual void LoadFromSlot(int slotNumber)
	{
		SaveSystem.LoadFromSlot(slotNumber);
	}

	public virtual void LoadScene(string sceneNameAndSpawnpoint)
	{
		SaveSystem.LoadScene(sceneNameAndSpawnpoint);
	}

	public virtual void ResetGameState()
	{
		SaveSystem.ResetGameState();
	}

	public virtual void RestartGame(string startingSceneName)
	{
		SaveSystem.RestartGame(startingSceneName);
	}

	public virtual void LoadOrRestart(int slotNumber)
	{
		if (SaveSystem.HasSavedGameInSlot(slotNumber))
		{
			SaveSystem.LoadFromSlot(slotNumber);
		}
		else
		{
			SaveSystem.RestartGame(defaultStartingSceneName);
		}
	}

	public virtual void DeleteSavedGameInSlot(int slotNumber)
	{
		SaveSystem.DeleteSavedGameInSlot(slotNumber);
	}

	public virtual void RecordSavedGameData()
	{
		SaveSystem.RecordSavedGameData();
	}

	public virtual void ApplySavedGameData()
	{
		SaveSystem.ApplySavedGameData();
	}

	public virtual void LoadAdditiveScene(string sceneName)
	{
		SaveSystem.LoadAdditiveScene(sceneName);
	}

	public virtual void UnloadAdditiveScene(string sceneName)
	{
		SaveSystem.UnloadAdditiveScene(sceneName);
	}
}
