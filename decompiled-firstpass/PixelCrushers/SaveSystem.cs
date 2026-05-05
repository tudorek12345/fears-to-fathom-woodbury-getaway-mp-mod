using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelCrushers;

[AddComponentMenu("")]
public class SaveSystem : MonoBehaviour
{
	public delegate string ValidateSceneNameDelegate(string sceneName, SceneValidationMode sceneValidationMode);

	public delegate void SceneLoadedDelegate(string sceneName, int sceneIndex);

	public const int NoSceneIndex = -1;

	public const string LastSavedGameSlotPlayerPrefsKey = "savedgame_lastSlotNum";

	[Tooltip("Optional saved game version number of your choosing. Version number is included in saved game files.")]
	[SerializeField]
	private int m_version;

	[Tooltip("When loading a game, load the scene that the game was saved in.")]
	[SerializeField]
	private bool m_saveCurrentScene = true;

	[Tooltip("Highest save slot number allowed.")]
	[SerializeField]
	private int m_maxSaveSlot = 99999;

	[Tooltip("When loading a game/scene, wait this many frames before applying saved data to allow other scripts to initialize first.")]
	[SerializeField]
	private int m_framesToWaitBeforeApplyData;

	[Tooltip("Log debug info.")]
	[SerializeField]
	private bool m_debug;

	private bool m_isLoadingAdditiveScene;

	private static SaveSystem m_instance = null;

	private static HashSet<Saver> m_savers = new HashSet<Saver>();

	private static List<Saver> m_tmpSavers = new List<Saver>();

	private static SavedGameData m_savedGameData = new SavedGameData();

	private static DataSerializer m_serializer = null;

	private static SavedGameDataStorer m_storer = null;

	private static SceneTransitionManager m_sceneTransitionManager = null;

	private static bool m_allowNegativeSlotNumbers = false;

	private static GameObject m_playerSpawnpoint = null;

	private static int m_currentSceneIndex = -1;

	private static List<string> m_addedScenes = new List<string>();

	private static bool m_autoUnloadAdditiveScenes = false;

	private static AsyncOperation m_currentAsyncOperation = null;

	private static int m_framesToWaitBeforeSaveDataAppliedEvent = 0;

	private static bool m_isQuitting = false;

	public static ValidateSceneNameDelegate validateNameScene = null;

	public static int version
	{
		get
		{
			if (!(m_instance != null))
			{
				return 0;
			}
			return m_instance.m_version;
		}
		set
		{
			if (m_instance != null)
			{
				m_instance.m_version = value;
			}
		}
	}

	public static bool saveCurrentScene
	{
		get
		{
			if (!(m_instance != null))
			{
				return true;
			}
			return m_instance.m_saveCurrentScene;
		}
		set
		{
			if (m_instance != null)
			{
				m_instance.m_saveCurrentScene = value;
			}
		}
	}

	public static int maxSaveSlot
	{
		get
		{
			if (!(m_instance != null))
			{
				return int.MaxValue;
			}
			return m_instance.m_maxSaveSlot;
		}
		set
		{
			if (m_instance != null)
			{
				m_instance.m_maxSaveSlot = value;
			}
		}
	}

	public static int framesToWaitBeforeApplyData
	{
		get
		{
			if (!(m_instance != null))
			{
				return 1;
			}
			return m_instance.m_framesToWaitBeforeApplyData;
		}
		set
		{
			if (m_instance != null)
			{
				m_instance.m_framesToWaitBeforeApplyData = value;
			}
		}
	}

	public static int framesToWaitBeforeSaveDataAppliedEvent
	{
		get
		{
			return m_framesToWaitBeforeSaveDataAppliedEvent;
		}
		set
		{
			m_framesToWaitBeforeSaveDataAppliedEvent = value;
		}
	}

	public static bool debug
	{
		get
		{
			if (!(m_instance != null))
			{
				return false;
			}
			if (m_instance.m_debug)
			{
				return Debug.isDebugBuild;
			}
			return false;
		}
		set
		{
			if (m_instance != null)
			{
				m_instance.m_debug = value;
			}
		}
	}

	public static bool hasInstance => m_instance != null;

	public static SaveSystem instance
	{
		get
		{
			if (m_instance == null && !m_isQuitting)
			{
				m_instance = GameObjectUtility.FindFirstObjectByType<SaveSystem>();
				if (m_instance == null)
				{
					m_instance = new GameObject("Save System", typeof(SaveSystem)).GetComponent<SaveSystem>();
				}
			}
			return m_instance;
		}
	}

	public static DataSerializer serializer
	{
		get
		{
			if (m_serializer == null)
			{
				m_serializer = instance.GetComponent<DataSerializer>();
				if (m_serializer == null && !m_isQuitting)
				{
					Debug.Log("Save System: No DataSerializer found on " + instance.name + ". Adding JsonDataSerializer.", instance);
					m_serializer = instance.gameObject.AddComponent<JsonDataSerializer>();
				}
			}
			return m_serializer;
		}
	}

	public static SavedGameDataStorer storer
	{
		get
		{
			if (m_storer == null)
			{
				m_storer = instance.GetComponent<SavedGameDataStorer>();
				if (m_storer == null && !m_isQuitting)
				{
					Debug.Log("Save System: No SavedGameDataStorer found on " + instance.name + ". Adding PlayerPrefsSavedGameDataStorer.", instance);
					m_storer = instance.gameObject.AddComponent<PlayerPrefsSavedGameDataStorer>();
				}
			}
			return m_storer;
		}
	}

	public static SceneTransitionManager sceneTransitionManager
	{
		get
		{
			if (m_sceneTransitionManager == null)
			{
				m_sceneTransitionManager = instance.GetComponentInChildren<SceneTransitionManager>();
			}
			return m_sceneTransitionManager;
		}
	}

	public bool allowNegativeSlotNumbers
	{
		get
		{
			return m_allowNegativeSlotNumbers;
		}
		set
		{
			m_allowNegativeSlotNumbers = value;
		}
	}

	public static List<string> addedScenes => m_addedScenes;

	public static bool autoUnloadAdditiveScenes
	{
		get
		{
			return m_autoUnloadAdditiveScenes;
		}
		set
		{
			m_autoUnloadAdditiveScenes = value;
		}
	}

	public static AsyncOperation currentAsyncOperation
	{
		get
		{
			return m_currentAsyncOperation;
		}
		set
		{
			m_currentAsyncOperation = value;
		}
	}

	public static SavedGameData currentSavedGameData
	{
		get
		{
			return m_savedGameData;
		}
		set
		{
			m_savedGameData = value;
		}
	}

	public static GameObject playerSpawnpoint
	{
		get
		{
			return m_playerSpawnpoint;
		}
		set
		{
			m_playerSpawnpoint = value;
		}
	}

	public static int currentSceneIndex
	{
		get
		{
			if (m_currentSceneIndex == -1)
			{
				m_currentSceneIndex = GetCurrentSceneIndex();
			}
			return m_currentSceneIndex;
		}
	}

	public static event SceneLoadedDelegate sceneLoaded;

	public static event Action saveStarted;

	public static event Action saveEnded;

	public static event Action loadStarted;

	public static event Action loadEnded;

	public static event Action saveDataApplied;

	private void Awake()
	{
		if (m_instance == null)
		{
			m_instance = this;
			if (base.transform.parent != null)
			{
				base.transform.SetParent(null);
			}
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void OnApplicationQuit()
	{
		m_isQuitting = true;
		BeforeSceneChange();
	}

	private void OnEnable()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		FinishedLoadingScene(scene.name, scene.buildIndex);
	}

	public static string GetCurrentSceneName()
	{
		return SceneManager.GetActiveScene().name;
	}

	public static int GetCurrentSceneIndex()
	{
		return SceneManager.GetActiveScene().buildIndex;
	}

	public static bool IsSceneInBuildSettings(string sceneName)
	{
		for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
		{
			string scenePathByBuildIndex = SceneUtility.GetScenePathByBuildIndex(i);
			if (!string.IsNullOrEmpty(scenePathByBuildIndex) && string.Equals(Path.GetFileNameWithoutExtension(scenePathByBuildIndex), sceneName, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static void SceneManagerOrAddressablesLoadScene(string sceneName)
	{
		if (IsSceneInBuildSettings(sceneName))
		{
			SceneManager.LoadScene(sceneName);
		}
		else
		{
			Debug.LogError("Can't load scene. Scene is not in build settings: " + sceneName);
		}
	}

	private static void SceneManagerOrAddressablesLoadSceneAsync(string sceneName)
	{
		m_currentAsyncOperation = null;
		if (IsSceneInBuildSettings(sceneName))
		{
			m_currentAsyncOperation = SceneManager.LoadSceneAsync(sceneName);
		}
		else
		{
			Debug.LogError("Can't load scene. Scene is not in build settings: " + sceneName);
		}
	}

	private static IEnumerator SceneManagerOrAddressablesLoadSceneAdditiveAsync(string sceneName)
	{
		if (IsSceneInBuildSettings(sceneName))
		{
			yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
		}
		else
		{
			Debug.LogError("Can't load additive scene. Scene is not in build settings: " + sceneName);
		}
	}

	private static IEnumerator LoadSceneInternal(string sceneName, SceneValidationMode sceneValidationMode)
	{
		m_addedScenes.Clear();
		if (sceneTransitionManager == null)
		{
			if (sceneName.StartsWith("index:"))
			{
				SceneManager.LoadScene(SafeConvert.ToInt(sceneName.Substring("index:".Length)));
				yield break;
			}
			if (validateNameScene != null)
			{
				sceneName = validateNameScene(sceneName, sceneValidationMode);
			}
			if (string.IsNullOrEmpty(sceneName))
			{
				if (debug)
				{
					Debug.LogWarning("Scene '" + sceneName + "' is not a valid scene to load.");
				}
			}
			else
			{
				SceneManagerOrAddressablesLoadScene(sceneName);
			}
		}
		else
		{
			yield return instance.StartCoroutine(LoadSceneInternalTransitionCoroutine(sceneName, sceneValidationMode));
		}
	}

	private static IEnumerator LoadSceneInternalTransitionCoroutine(string sceneName, SceneValidationMode sceneValidationMode)
	{
		m_addedScenes.Clear();
		yield return instance.StartCoroutine(sceneTransitionManager.LeaveScene());
		if (sceneName.StartsWith("index:"))
		{
			m_currentAsyncOperation = SceneManager.LoadSceneAsync(SafeConvert.ToInt(sceneName.Substring("index:".Length)));
		}
		else
		{
			if (validateNameScene != null)
			{
				sceneName = validateNameScene(sceneName, sceneValidationMode);
			}
			if (string.IsNullOrEmpty(sceneName))
			{
				if (debug)
				{
					Debug.LogWarning("Scene '" + sceneName + "' is not a valid scene to load.");
				}
				yield break;
			}
			SceneManagerOrAddressablesLoadSceneAsync(sceneName);
		}
		if (m_currentAsyncOperation != null)
		{
			while (m_currentAsyncOperation != null && !m_currentAsyncOperation.isDone)
			{
				sceneTransitionManager.OnLoading(m_currentAsyncOperation.progress);
				yield return null;
			}
		}
		sceneTransitionManager.OnLoading(1f);
		m_currentAsyncOperation = null;
		instance.StartCoroutine(sceneTransitionManager.EnterScene());
	}

	public static IEnumerator LoadAdditiveSceneInternal(string sceneName, SceneValidationMode sceneValidationMode)
	{
		if (validateNameScene != null)
		{
			sceneName = validateNameScene(sceneName, sceneValidationMode);
		}
		if (string.IsNullOrEmpty(sceneName))
		{
			yield break;
		}
		yield return SceneManagerOrAddressablesLoadSceneAdditiveAsync(sceneName);
		Scene sceneByName = SceneManager.GetSceneByName(sceneName);
		if (sceneByName.IsValid())
		{
			GameObject[] rootGameObjects = sceneByName.GetRootGameObjects();
			for (int i = 0; i < rootGameObjects.Length; i++)
			{
				RecursivelyApplySavers(rootGameObjects[i].transform);
			}
		}
	}

	public static void UnloadAdditiveSceneInternal(string sceneName)
	{
		Scene sceneByName = SceneManager.GetSceneByName(sceneName);
		if (sceneByName.IsValid())
		{
			GameObject[] rootGameObjects = sceneByName.GetRootGameObjects();
			for (int i = 0; i < rootGameObjects.Length; i++)
			{
				Transform t = rootGameObjects[i].transform;
				RecursivelyRecordSavers(t, sceneByName.buildIndex);
				RecursivelyInformBeforeSceneChange(t);
			}
		}
		SceneManager.UnloadSceneAsync(sceneName);
	}

	public static void RecursivelyRecordSavers(Transform t, int sceneIndex)
	{
		if (t == null)
		{
			return;
		}
		Saver component = t.GetComponent<Saver>();
		if (component != null)
		{
			currentSavedGameData.SetData(component.key, component.saveAcrossSceneChanges ? (-1) : sceneIndex, component.RecordData());
		}
		foreach (Transform item in t)
		{
			RecursivelyRecordSavers(item, sceneIndex);
		}
	}

	public static void RecursivelyApplySavers(Transform t)
	{
		if (t == null)
		{
			return;
		}
		Saver component = t.GetComponent<Saver>();
		if (component != null)
		{
			component.ApplyData(currentSavedGameData.GetData(component.key));
		}
		foreach (Transform item in t)
		{
			RecursivelyApplySavers(item);
		}
	}

	public static void RecursivelyInformBeforeSceneChange(Transform t)
	{
		if (t == null)
		{
			return;
		}
		Saver component = t.GetComponent<Saver>();
		if (component != null)
		{
			component.OnBeforeSceneChange();
		}
		foreach (Transform item in t)
		{
			RecursivelyInformBeforeSceneChange(item);
		}
	}

	private static bool SanitizeSlotNumberForSave(int slotNumber, out int sanitizedSlotNumber)
	{
		if (slotNumber >= 0 || m_instance == null || m_instance.allowNegativeSlotNumbers)
		{
			sanitizedSlotNumber = slotNumber;
			return true;
		}
		for (int i = 0; i <= maxSaveSlot; i++)
		{
			if (!HasSavedGameInSlot(i))
			{
				sanitizedSlotNumber = i;
				return true;
			}
		}
		sanitizedSlotNumber = 0;
		return false;
	}

	public void SaveGameToSlot(int slotNumber)
	{
		SaveToSlot(slotNumber);
	}

	public void LoadGameFromSlot(int slotNumber)
	{
		LoadFromSlot(slotNumber);
	}

	public void LoadSceneAtSpawnpoint(string sceneNameAndSpawnpoint)
	{
		LoadScene(sceneNameAndSpawnpoint);
	}

	public static bool HasSavedGameInSlot(int slotNumber)
	{
		return storer.HasDataInSlot(slotNumber);
	}

	public static void DeleteSavedGameInSlot(int slotNumber)
	{
		storer.DeleteSavedGameData(slotNumber);
	}

	public static void SaveToSlot(int slotNumber)
	{
		instance.StartCoroutine(SaveToSlotCoroutine(slotNumber));
	}

	private static IEnumerator SaveToSlotCoroutine(int slotNumber)
	{
		if (!SanitizeSlotNumberForSave(slotNumber, out slotNumber))
		{
			Debug.LogError("Can't save game. Invalid save slot: " + slotNumber);
			yield break;
		}
		SaveSystem.saveStarted();
		yield return null;
		PlayerPrefs.SetInt("savedgame_lastSlotNum", slotNumber);
		yield return storer.StoreSavedGameDataAsync(slotNumber, RecordSavedGameData());
		SaveSystem.saveEnded();
	}

	public static void SaveToSlotImmediate(int slotNumber)
	{
		if (!SanitizeSlotNumberForSave(slotNumber, out slotNumber))
		{
			Debug.LogError("Can't save game. Invalid save slot: " + slotNumber);
			return;
		}
		SaveSystem.saveStarted();
		PlayerPrefs.SetInt("savedgame_lastSlotNum", slotNumber);
		storer.StoreSavedGameData(slotNumber, RecordSavedGameData());
		SaveSystem.saveEnded();
	}

	public static void LoadFromSlot(int slotNumber)
	{
		if (!HasSavedGameInSlot(slotNumber))
		{
			if (Debug.isDebugBuild)
			{
				Debug.LogWarning("Save System: LoadFromSlot(" + slotNumber + ") but there is no saved game in this slot.");
			}
		}
		else if (SaveSystem.loadStarted.GetInvocationList().Length > 1)
		{
			instance.StartCoroutine(LoadFromSlotCoroutine(slotNumber));
		}
		else
		{
			LoadFromSlotNow(slotNumber);
		}
	}

	private static IEnumerator LoadFromSlotCoroutine(int slotNumber)
	{
		SaveSystem.loadStarted();
		yield return null;
		LoadFromSlotNow(slotNumber);
	}

	private static void NotifyLoadEndedWhenSceneLoaded(string sceneName, int sceneIndex)
	{
		sceneLoaded -= NotifyLoadEndedWhenSceneLoaded;
		SaveSystem.loadEnded();
	}

	private static void LoadFromSlotNow(int slotNumber)
	{
		sceneLoaded += NotifyLoadEndedWhenSceneLoaded;
		LoadGame(storer.RetrieveSavedGameData(slotNumber));
	}

	public static void RegisterSaver(Saver saver)
	{
		if (!(saver == null) && !m_savers.Contains(saver))
		{
			m_savers.Add(saver);
		}
	}

	public static void UnregisterSaver(Saver saver)
	{
		m_savers.Remove(saver);
	}

	public static void ClearSavedGameData()
	{
		m_savedGameData = new SavedGameData();
	}

	public static SavedGameData RecordSavedGameData()
	{
		m_savedGameData.version = version;
		m_savedGameData.sceneName = GetCurrentSceneName();
		foreach (Saver saver in m_savers)
		{
			try
			{
				m_savedGameData.SetData(saver.key, GetSaverSceneIndex(saver), saver.RecordData());
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}
		return m_savedGameData;
	}

	private static int GetSaverSceneIndex(Saver saver)
	{
		if (!(saver == null) && saver.saveAcrossSceneChanges)
		{
			return -1;
		}
		return currentSceneIndex;
	}

	public static void UpdateSaveData(Saver saver, string data)
	{
		m_savedGameData.SetData(saver.key, GetSaverSceneIndex(saver), data);
	}

	public static void ApplySavedGameData(SavedGameData savedGameData)
	{
		if (savedGameData == null)
		{
			return;
		}
		m_savedGameData = savedGameData;
		if (m_savers.Count <= 0)
		{
			return;
		}
		m_tmpSavers.Clear();
		m_tmpSavers.AddRange(m_savers);
		for (int num = m_tmpSavers.Count - 1; num >= 0; num--)
		{
			try
			{
				if (0 <= num && num < m_tmpSavers.Count)
				{
					Saver saver = m_tmpSavers[num];
					if (saver != null)
					{
						saver.ApplyData(savedGameData.GetData(saver.key));
					}
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}
		if (framesToWaitBeforeSaveDataAppliedEvent == 0 || instance == null)
		{
			SaveSystem.saveDataApplied();
			return;
		}
		instance.StartCoroutine(DelayedSaveDataAppliedCoroutine(framesToWaitBeforeSaveDataAppliedEvent));
		framesToWaitBeforeSaveDataAppliedEvent = 0;
	}

	protected static IEnumerator DelayedSaveDataAppliedCoroutine(int frames)
	{
		for (int i = 0; i < frames; i++)
		{
			yield return null;
		}
		yield return CoroutineUtility.endOfFrame;
		SaveSystem.saveDataApplied();
	}

	public static void ApplySavedGameData()
	{
		ApplySavedGameData(m_savedGameData);
	}

	public static void BeforeSceneChange()
	{
		foreach (Saver saver in m_savers)
		{
			try
			{
				saver.OnBeforeSceneChange();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}
		try
		{
			SceneNotifier.NotifyWillUnloadScene(m_currentSceneIndex);
		}
		catch (Exception exception2)
		{
			Debug.LogException(exception2);
		}
	}

	public static void LoadGame(SavedGameData savedGameData)
	{
		if (savedGameData == null)
		{
			if (Debug.isDebugBuild)
			{
				Debug.LogWarning("SaveSystem.LoadGame received null saved game data. Not loading.");
			}
		}
		else if (saveCurrentScene)
		{
			instance.StartCoroutine(LoadSceneCoroutine(savedGameData, null, SceneValidationMode.LoadingSavedGame));
		}
		else
		{
			ApplySavedGameData(savedGameData);
		}
	}

	public static void LoadScene(string sceneNameAndSpawnpoint)
	{
		if (!string.IsNullOrEmpty(sceneNameAndSpawnpoint))
		{
			string sceneName = sceneNameAndSpawnpoint;
			string spawnpointName = string.Empty;
			if (sceneNameAndSpawnpoint.Contains("@"))
			{
				string[] array = sceneNameAndSpawnpoint.Split('@');
				sceneName = array[0];
				spawnpointName = ((array.Length > 1) ? array[1] : null);
			}
			SavedGameData savedGameData = RecordSavedGameData();
			savedGameData.sceneName = sceneName;
			instance.StartCoroutine(LoadSceneCoroutine(savedGameData, spawnpointName, SceneValidationMode.LoadingScene));
		}
	}

	private static IEnumerator LoadSceneCoroutine(SavedGameData savedGameData, string spawnpointName, SceneValidationMode sceneValidationMode)
	{
		if (savedGameData != null)
		{
			if (debug)
			{
				Debug.Log("Save System: Loading scene " + savedGameData.sceneName + (string.IsNullOrEmpty(spawnpointName) ? string.Empty : (" [spawn at " + spawnpointName + "]")));
			}
			m_savedGameData = savedGameData;
			BeforeSceneChange();
			if (autoUnloadAdditiveScenes)
			{
				UnloadAllAdditiveScenes();
			}
			yield return LoadSceneInternal(savedGameData.sceneName, sceneValidationMode);
			ApplyDataImmediate();
			for (int i = 0; i < framesToWaitBeforeApplyData; i++)
			{
				yield return null;
			}
			yield return CoroutineUtility.endOfFrame;
			m_playerSpawnpoint = ((!string.IsNullOrEmpty(spawnpointName)) ? GameObject.Find(spawnpointName) : null);
			if (!string.IsNullOrEmpty(spawnpointName) && m_playerSpawnpoint == null)
			{
				Debug.LogWarning("Save System: Can't find spawnpoint '" + spawnpointName + "'. Is spelling and capitalization correct?");
			}
			ApplySavedGameData(savedGameData);
		}
	}

	private static void ApplyDataImmediate()
	{
		if (m_savers.Count <= 0)
		{
			return;
		}
		m_tmpSavers.Clear();
		m_tmpSavers.AddRange(m_savers);
		for (int num = m_tmpSavers.Count - 1; num >= 0; num--)
		{
			try
			{
				if (0 <= num && num < m_tmpSavers.Count)
				{
					Saver saver = m_tmpSavers[num];
					if (saver != null)
					{
						saver.ApplyDataImmediate();
					}
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}
	}

	private void FinishedLoadingScene(string sceneName, int sceneIndex)
	{
		m_currentSceneIndex = sceneIndex;
		if (!m_isLoadingAdditiveScene)
		{
			m_savedGameData.DeleteObsoleteSaveData(sceneIndex);
		}
		m_isLoadingAdditiveScene = false;
		SaveSystem.sceneLoaded(sceneName, sceneIndex);
	}

	public static void LoadAdditiveScene(string sceneName)
	{
		if (!string.IsNullOrEmpty(sceneName) && !m_addedScenes.Contains(sceneName))
		{
			m_addedScenes.Add(sceneName);
			instance.m_isLoadingAdditiveScene = true;
			instance.StartCoroutine(LoadAdditiveSceneInternal(sceneName, SceneValidationMode.LoadingScene));
		}
	}

	public static void UnloadAdditiveScene(string sceneName)
	{
		if (m_addedScenes.Contains(sceneName))
		{
			m_addedScenes.Remove(sceneName);
			UnloadAdditiveSceneInternal(sceneName);
		}
	}

	public static void UnloadAllAdditiveScenes()
	{
		for (int num = m_addedScenes.Count - 1; num >= 0; num--)
		{
			UnloadAdditiveScene(m_addedScenes[num]);
		}
	}

	public static void RestartGame(string startingSceneName)
	{
		ResetGameState();
		instance.StartCoroutine(LoadSceneInternal(startingSceneName, SceneValidationMode.RestartingGame));
	}

	public static void ResetGameState()
	{
		ClearSavedGameData();
		BeforeSceneChange();
		SaversRestartGame();
	}

	public static void SaversRestartGame()
	{
		if (m_savers.Count <= 0)
		{
			return;
		}
		foreach (Saver item in m_savers.ToList())
		{
			try
			{
				if (item != null)
				{
					item.OnRestartGame();
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}
	}

	public static string Serialize(object data)
	{
		return serializer.Serialize(data);
	}

	public static T Deserialize<T>(string s, T data = default(T))
	{
		return serializer.Deserialize(s, data);
	}

	static SaveSystem()
	{
		SaveSystem.sceneLoaded = delegate
		{
		};
		SaveSystem.saveStarted = delegate
		{
		};
		SaveSystem.saveEnded = delegate
		{
		};
		SaveSystem.loadStarted = delegate
		{
		};
		SaveSystem.loadEnded = delegate
		{
		};
		SaveSystem.saveDataApplied = delegate
		{
		};
	}
}
