using UnityEngine;
using UnityEngine.Events;

namespace PixelCrushers;

[AddComponentMenu("")]
public class SaveSystemTestMenu : MonoBehaviour
{
	[Tooltip("Unity input button that toggles menu open/closed.")]
	public string menuInputButton = "Cancel";

	[Tooltip("Optional GUI Skin to provide custom Label, Button, and Box styles.")]
	public GUISkin guiSkin;

	[Tooltip("Size of menu buttons.")]
	public Vector2 buttonSize = new Vector2(200f, 30f);

	[Tooltip("Slot that menu saves game in.")]
	public int saveSlot = 1;

	[Tooltip("Optional instructions to show when script starts.")]
	public string instructions = "Press Escape for menu.";

	[Tooltip("How long to show instructions.")]
	public float instructionsDuration = 5f;

	[Tooltip("Pause the game while the menu is open.")]
	public bool pauseWhileOpen;

	[Tooltip("If Input Device Manager mode is mouse, show cursor when opening and hide when closing.")]
	public bool allowCursorWhileOpen;

	public UnityEvent onShow = new UnityEvent();

	public UnityEvent onHide = new UnityEvent();

	private bool m_isVisible;

	private float m_instructionsDoneTime;

	private bool m_prevCursorState;

	private void Awake()
	{
		m_instructionsDoneTime = (string.IsNullOrEmpty(instructions) ? 0f : (Time.time + instructionsDuration));
	}

	private void Update()
	{
		if (InputDeviceManager.IsButtonDown(menuInputButton))
		{
			ToggleMenu();
		}
	}

	private void OnDestroy()
	{
		Time.timeScale = 1f;
	}

	public void ToggleMenu()
	{
		m_isVisible = !m_isVisible;
		if (pauseWhileOpen)
		{
			Time.timeScale = ((!m_isVisible) ? 1 : 0);
		}
		if (m_isVisible)
		{
			HandleCursor(open: true);
			onShow.Invoke();
		}
		else
		{
			HandleCursor(open: false);
			onHide.Invoke();
		}
	}

	private void HandleCursor(bool open)
	{
		if (allowCursorWhileOpen && InputDeviceManager.deviceUsesCursor)
		{
			if (open)
			{
				m_prevCursorState = Cursor.visible;
				Cursor.visible = true;
				Cursor.lockState = CursorLockMode.None;
			}
			else
			{
				Cursor.visible = m_prevCursorState;
				Cursor.lockState = ((!m_prevCursorState) ? CursorLockMode.Locked : CursorLockMode.None);
			}
		}
	}

	private void OnGUI()
	{
		GUISkin skin = GUI.skin;
		if (guiSkin != null)
		{
			GUI.skin = guiSkin;
		}
		if (Time.time < m_instructionsDoneTime)
		{
			GUILayout.Label(instructions);
		}
		if (m_isVisible)
		{
			float x = buttonSize.x;
			float y = buttonSize.y;
			GUILayout.BeginArea(new Rect(((float)Screen.width - x) / 2f, ((float)Screen.height - 4f * y) / 2f, x, 4f * (y + 10f)));
			if (GUILayout.Button("Resume", GUILayout.Height(y)))
			{
				ToggleMenu();
			}
			if (GUILayout.Button("Save", GUILayout.Height(y)))
			{
				ToggleMenu();
				Debug.Log("Saving game to slot " + saveSlot);
				SaveSystem.SaveToSlot(saveSlot);
			}
			if (GUILayout.Button("Load", GUILayout.Height(y)))
			{
				ToggleMenu();
				Debug.Log("Loading game from slot " + saveSlot);
				SaveSystem.LoadFromSlot(saveSlot);
			}
			if (GUILayout.Button("Quit", GUILayout.Height(y)))
			{
				ToggleMenu();
				Debug.Log("Quitting");
				Application.Quit();
			}
			GUILayout.EndArea();
			if (guiSkin != null)
			{
				GUI.skin = skin;
			}
		}
	}
}
