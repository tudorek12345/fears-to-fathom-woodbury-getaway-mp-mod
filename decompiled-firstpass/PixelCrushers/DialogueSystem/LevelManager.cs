using System.Collections;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class LevelManager : MonoBehaviour
{
	[Tooltip("Level to use if none is recorded in saved-game data.")]
	public string defaultStartingLevel;

	[Tooltip("Load asynchronously to prevent freeze while loading.")]
	public bool useAsyncLoad = true;

	public bool isLoading { get; private set; }

	public bool IsLoading
	{
		get
		{
			return isLoading;
		}
		set
		{
			isLoading = value;
		}
	}

	protected virtual void Awake()
	{
		isLoading = false;
	}

	protected virtual void OnEnable()
	{
		PersistentDataManager.RegisterPersistentData(base.gameObject);
	}

	protected virtual void OnDisable()
	{
		PersistentDataManager.UnregisterPersistentData(base.gameObject);
	}

	public void LoadGame(string saveData)
	{
		StartCoroutine(LoadLevelFromSaveData(saveData));
	}

	public void RestartGame()
	{
		StartCoroutine(LoadLevelFromSaveData(null));
	}

	private IEnumerator LoadLevelFromSaveData(string saveData)
	{
		if (DialogueDebug.logInfo)
		{
			Debug.Log("Dialogue System: LevelManager: Starting LoadLevelFromSaveData coroutine");
		}
		string levelName = defaultStartingLevel;
		if (string.IsNullOrEmpty(saveData))
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log("Dialogue System: LevelManager: Save data is empty, so just resetting database");
			}
			DialogueManager.ResetDatabase(DatabaseResetOptions.RevertToDefault);
		}
		else
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log("Dialogue System: LevelManager: Applying save data to get value of 'SavedLevelName' variable");
			}
			Lua.Run(saveData, DialogueDebug.logInfo);
			levelName = DialogueLua.GetVariable("SavedLevelName").asString;
			if (string.IsNullOrEmpty(levelName) || string.Equals(levelName, "nil"))
			{
				levelName = defaultStartingLevel;
				if (DialogueDebug.logInfo)
				{
					Debug.Log("Dialogue System: LevelManager: 'SavedLevelName' isn't defined. Using default level " + levelName);
				}
			}
			else if (DialogueDebug.logInfo)
			{
				Debug.Log("Dialogue System: LevelManager: SavedLevelName = " + levelName);
			}
		}
		PersistentDataManager.LevelWillBeUnloaded();
		if (CanLoadAsync())
		{
			AsyncOperation async = Tools.LoadLevelAsync(levelName);
			isLoading = true;
			while (!async.isDone)
			{
				yield return null;
			}
			isLoading = false;
		}
		else
		{
			Tools.LoadLevel(levelName);
		}
		if (DialogueDebug.logInfo)
		{
			Debug.Log("Dialogue System: LevelManager finished loading level " + levelName + ". Waiting 2 frames for scene objects to start.");
		}
		yield return null;
		yield return null;
		if (!string.IsNullOrEmpty(saveData))
		{
			if (DialogueDebug.logInfo)
			{
				Debug.Log("Dialogue System: LevelManager waited 2 frames. Appling save data: " + saveData);
			}
			PersistentDataManager.ApplySaveData(saveData);
		}
		DialogueManager.SendUpdateTracker();
	}

	public void LoadLevel(string levelName)
	{
		StartCoroutine(LoadLevelCoroutine(levelName, -1));
	}

	public void LoadLevel(int levelIndex)
	{
		StartCoroutine(LoadLevelCoroutine(null, levelIndex));
	}

	private IEnumerator LoadLevelCoroutine(string levelName, int levelIndex)
	{
		PersistentDataManager.Record();
		PersistentDataManager.LevelWillBeUnloaded();
		if (CanLoadAsync())
		{
			AsyncOperation async = ((!string.IsNullOrEmpty(levelName)) ? Tools.LoadLevelAsync(levelName) : Tools.LoadLevelAsync(levelIndex));
			isLoading = true;
			while (!async.isDone)
			{
				yield return null;
			}
			isLoading = false;
		}
		else if (!string.IsNullOrEmpty(levelName))
		{
			Tools.LoadLevel(levelName);
		}
		else
		{
			Tools.LoadLevel(levelIndex);
		}
		yield return null;
		yield return null;
		GameObject gameObject = GameObject.FindGameObjectWithTag("Player");
		PersistentPositionData persistentPositionData = ((gameObject != null) ? gameObject.GetComponent<PersistentPositionData>() : null);
		bool restoreCurrentLevelPosition = false;
		if (persistentPositionData != null)
		{
			restoreCurrentLevelPosition = persistentPositionData.restoreCurrentLevelPosition;
			persistentPositionData.restoreCurrentLevelPosition = false;
		}
		PersistentDataManager.Apply();
		if (persistentPositionData != null)
		{
			persistentPositionData.restoreCurrentLevelPosition = restoreCurrentLevelPosition;
		}
		DialogueManager.SendUpdateTracker();
	}

	private bool CanLoadAsync()
	{
		return useAsyncLoad;
	}

	public virtual void OnRecordPersistentData()
	{
		DialogueLua.SetVariable("SavedLevelName", Tools.loadedLevelName);
	}
}
