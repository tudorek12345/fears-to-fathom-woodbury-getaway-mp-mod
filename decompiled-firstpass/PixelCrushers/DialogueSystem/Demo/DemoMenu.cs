using PixelCrushers.DialogueSystem.UnityGUI;
using UnityEngine;
using UnityEngine.Events;

namespace PixelCrushers.DialogueSystem.Demo;

[AddComponentMenu("")]
public class DemoMenu : MonoBehaviour
{
	[TextArea]
	public string startMessage = "Press Escape for Menu";

	public KeyCode menuKey = KeyCode.Escape;

	public GUISkin guiSkin;

	public bool closeWhenQuestLogOpen = true;

	public bool lockCursorDuringPlay;

	public UnityEvent onOpen = new UnityEvent();

	public UnityEvent onClose = new UnityEvent();

	private QuestLogWindow questLogWindow;

	private bool isMenuOpen;

	private Rect windowRect = new Rect(0f, 0f, 500f, 500f);

	private ScaledRect scaledRect = ScaledRect.FromOrigin(ScaledRectAlignment.MiddleCenter, ScaledValue.FromPixelValue(300f), ScaledValue.FromPixelValue(320f));

	private void Start()
	{
		if (questLogWindow == null)
		{
			questLogWindow = GameObjectUtility.FindFirstObjectByType<QuestLogWindow>();
		}
		if (!string.IsNullOrEmpty(startMessage))
		{
			DialogueManager.ShowAlert(startMessage);
		}
	}

	private void OnDestroy()
	{
		if (isMenuOpen)
		{
			Time.timeScale = 1f;
		}
	}

	private void Update()
	{
		if (InputDeviceManager.IsKeyDown(menuKey) && !DialogueManager.isConversationActive && !IsQuestLogOpen())
		{
			SetMenuStatus(!isMenuOpen);
		}
		if (lockCursorDuringPlay)
		{
			CursorControl.SetCursorActive(DialogueManager.isConversationActive || isMenuOpen || IsQuestLogOpen());
		}
	}

	private void OnGUI()
	{
		if (isMenuOpen && !IsQuestLogOpen())
		{
			if (guiSkin != null)
			{
				GUI.skin = guiSkin;
			}
			windowRect = GUI.Window(0, windowRect, WindowFunction, "Menu");
		}
	}

	private void WindowFunction(int windowID)
	{
		if (GUI.Button(new Rect(10f, 60f, windowRect.width - 20f, 48f), "Quest Log"))
		{
			if (closeWhenQuestLogOpen)
			{
				SetMenuStatus(open: false);
			}
			OpenQuestLog();
		}
		if (GUI.Button(new Rect(10f, 110f, windowRect.width - 20f, 48f), "Save Game"))
		{
			SetMenuStatus(open: false);
			SaveGame();
		}
		if (GUI.Button(new Rect(10f, 160f, windowRect.width - 20f, 48f), "Load Game"))
		{
			SetMenuStatus(open: false);
			LoadGame();
		}
		if (GUI.Button(new Rect(10f, 210f, windowRect.width - 20f, 48f), "Clear Saved Game"))
		{
			SetMenuStatus(open: false);
			ClearSavedGame();
		}
		if (GUI.Button(new Rect(10f, 260f, windowRect.width - 20f, 48f), "Close Menu"))
		{
			SetMenuStatus(open: false);
		}
	}

	public void Open()
	{
		SetMenuStatus(open: true);
	}

	public void Close()
	{
		SetMenuStatus(open: false);
	}

	private void SetMenuStatus(bool open)
	{
		isMenuOpen = open;
		if (open)
		{
			windowRect = scaledRect.GetPixelRect();
		}
		Time.timeScale = ((!open) ? 1 : 0);
		if (open)
		{
			onOpen.Invoke();
		}
		else
		{
			onClose.Invoke();
		}
	}

	private bool IsQuestLogOpen()
	{
		if (questLogWindow != null)
		{
			return questLogWindow.isOpen;
		}
		return false;
	}

	private void OpenQuestLog()
	{
		if (questLogWindow != null && !IsQuestLogOpen())
		{
			questLogWindow.Open();
		}
	}

	private void SaveGame()
	{
		if (GameObjectUtility.FindFirstObjectByType<SaveSystem>() != null)
		{
			SaveSystem.SaveToSlot(1);
		}
		else
		{
			string saveData = PersistentDataManager.GetSaveData();
			PlayerPrefs.SetString("SavedGame", saveData);
			Debug.Log("Save Game Data: " + saveData);
		}
		DialogueManager.ShowAlert("Game saved.");
	}

	private void LoadGame()
	{
		PersistentDataManager.LevelWillBeUnloaded();
		if (GameObjectUtility.FindFirstObjectByType<SaveSystem>() != null)
		{
			if (SaveSystem.HasSavedGameInSlot(1))
			{
				SaveSystem.LoadFromSlot(1);
				DialogueManager.ShowAlert("Game loaded.");
			}
			else
			{
				DialogueManager.ShowAlert("Save a game first.");
			}
		}
		else if (PlayerPrefs.HasKey("SavedGame"))
		{
			string text = PlayerPrefs.GetString("SavedGame");
			Debug.Log("Load Game Data: " + text);
			LevelManager levelManager = GameObjectUtility.FindFirstObjectByType<LevelManager>();
			if (levelManager != null)
			{
				levelManager.LoadGame(text);
			}
			else
			{
				PersistentDataManager.ApplySaveData(text);
				DialogueManager.SendUpdateTracker();
			}
			DialogueManager.ShowAlert("Game loaded.");
		}
		else
		{
			DialogueManager.ShowAlert("Save a game first.");
		}
	}

	private void ClearSavedGame()
	{
		if (GameObjectUtility.FindFirstObjectByType<SaveSystem>() != null)
		{
			if (SaveSystem.HasSavedGameInSlot(1))
			{
				SaveSystem.DeleteSavedGameInSlot(1);
			}
		}
		else if (PlayerPrefs.HasKey("SavedGame"))
		{
			PlayerPrefs.DeleteKey("SavedGame");
			Debug.Log("Cleared saved game data");
		}
		DialogueManager.ShowAlert("Saved Game Cleared");
	}
}
