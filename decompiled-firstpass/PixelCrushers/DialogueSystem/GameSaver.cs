using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class GameSaver : MonoBehaviour
{
	public enum FunctionOnUse
	{
		None,
		Save,
		Load,
		Restart
	}

	public string playerPrefsKey = "savedgame";

	public int slot;

	public FunctionOnUse functionOnUse;

	public bool includeAllItemData;

	public bool includeLocationData;

	public bool includeSimStatus;

	public string startingLevel = string.Empty;

	public bool dontDestroyOnLoad;

	public void Awake()
	{
		if (dontDestroyOnLoad)
		{
			base.transform.parent = null;
			Object.DontDestroyOnLoad(base.gameObject);
		}
		PersistentDataManager.includeAllItemData = includeAllItemData;
		PersistentDataManager.includeLocationData = includeLocationData;
		PersistentDataManager.includeSimStatus = includeSimStatus;
	}

	public void OnUse()
	{
		switch (functionOnUse)
		{
		case FunctionOnUse.Save:
			SaveGame();
			break;
		case FunctionOnUse.Load:
			LoadGame();
			break;
		}
	}

	public void SaveGame(int slot)
	{
		if (SaveSystem.instance != null)
		{
			SaveSystem.SaveToSlot(slot);
			return;
		}
		if (string.IsNullOrEmpty(playerPrefsKey))
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: PlayerPrefs Key isn't set. Not saving.", new object[1] { "Dialogue System" }));
			}
			return;
		}
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Saving game in slot {1}.", new object[2] { "Dialogue System", slot }));
		}
		PlayerPrefs.SetString(playerPrefsKey + slot, PersistentDataManager.GetSaveData());
	}

	public void SaveGame()
	{
		SaveGame(slot);
	}

	public void LoadGame(int slot)
	{
		if (SaveSystem.instance != null)
		{
			SaveSystem.LoadFromSlot(slot);
			return;
		}
		if (string.IsNullOrEmpty(playerPrefsKey))
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: PlayerPrefs Key isn't set. Not loading.", new object[1] { "Dialogue System" }));
			}
			return;
		}
		string text = playerPrefsKey + slot;
		if (!PlayerPrefs.HasKey(text))
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: No saved game in PlayerPrefs key '{1}'. Not loading.", new object[2] { "Dialogue System", text }));
			}
			return;
		}
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Loading save data from slot {1} and applying it.", new object[2] { "Dialogue System", slot }));
		}
		string saveData = PlayerPrefs.GetString(text);
		LevelManager levelManager = FindLevelManager();
		if (levelManager != null)
		{
			levelManager.LoadGame(saveData);
			return;
		}
		PersistentDataManager.ApplySaveData(saveData);
		DialogueManager.SendUpdateTracker();
	}

	public void LoadGame()
	{
		LoadGame(slot);
	}

	public void SaveGame(string slotString)
	{
		SaveGame(StringToSlot(slotString));
	}

	public void LoadGame(string slotString)
	{
		LoadGame(StringToSlot(slotString));
	}

	public void RestartGame()
	{
		LevelManager levelManager = FindLevelManager();
		if (SaveSystem.instance != null)
		{
			SaveSystem.RestartGame((levelManager != null && !string.IsNullOrEmpty(levelManager.defaultStartingLevel)) ? levelManager.defaultStartingLevel : startingLevel);
			return;
		}
		if (levelManager != null)
		{
			levelManager.RestartGame();
			return;
		}
		DialogueManager.ResetDatabase(DatabaseResetOptions.RevertToDefault);
		if (string.IsNullOrEmpty(startingLevel))
		{
			Tools.LoadLevel(0);
		}
		else
		{
			Tools.LoadLevel(startingLevel);
		}
		DialogueManager.SendUpdateTracker();
	}

	private LevelManager FindLevelManager()
	{
		LevelManager levelManager = GetComponentInChildren<LevelManager>();
		if (levelManager == null)
		{
			levelManager = GameObjectUtility.FindFirstObjectByType<LevelManager>();
		}
		return levelManager;
	}

	private int StringToSlot(string slotString)
	{
		int result = 0;
		int.TryParse(slotString, out result);
		return result;
	}

	public void Record()
	{
		PersistentDataManager.Record();
	}

	public void Apply()
	{
		PersistentDataManager.Apply();
	}
}
