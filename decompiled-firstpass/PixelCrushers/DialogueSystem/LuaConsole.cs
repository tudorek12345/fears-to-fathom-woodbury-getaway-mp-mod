using System;
using System.Collections.Generic;
using System.Text;
using PixelCrushers.DialogueSystem.UnityGUI;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

[AddComponentMenu("")]
public class LuaConsole : MonoBehaviour
{
	[Tooltip("Hold down this key and press Second Key to open console.")]
	public KeyCode firstKey = KeyCode.BackQuote;

	[Tooltip("Hold down First Key and press this key to open console.")]
	public KeyCode secondKey = KeyCode.L;

	[Tooltip("Console is visible.")]
	public bool visible;

	[Tooltip("Optional GUI Skin to style console window.")]
	public GUISkin guiSkin;

	[Tooltip("Minimum size of console window.")]
	public Vector2 minSize = new Vector2(384f, 384f);

	[Tooltip("Max number of previous commands to remember.")]
	public int maxHistory = 20;

	[Tooltip("While open, set Time.timeScale to 0.")]
	public bool pauseGameWhileOpen;

	protected List<string> m_history = new List<string>();

	protected int m_historyPosition;

	protected string m_input = string.Empty;

	protected string m_output = string.Empty;

	protected Rect m_windowRect = new Rect(0f, 0f, 0f, 0f);

	protected Rect m_closeButtonRect = new Rect(0f, 0f, 0f, 0f);

	protected Vector2 m_scrollPosition = new Vector2(0f, 0f);

	protected bool m_isFirstKeyDown;

	protected virtual void Start()
	{
		SetVisible(visible);
	}

	protected virtual void SetVisible(bool newValue)
	{
		visible = newValue;
		if (pauseGameWhileOpen)
		{
			Time.timeScale = ((!visible) ? 1 : 0);
		}
	}

	protected virtual void OnGUI()
	{
		if (Event.current.type == EventType.KeyDown && Event.current.keyCode == firstKey)
		{
			m_isFirstKeyDown = true;
		}
		else if (Event.current.type == EventType.KeyUp && Event.current.keyCode == firstKey)
		{
			m_isFirstKeyDown = false;
		}
		if (Event.current.type == EventType.KeyDown && Event.current.keyCode == secondKey && m_isFirstKeyDown)
		{
			Event.current.Use();
			SetVisible(!visible);
		}
		if (visible)
		{
			if (Event.current.type == EventType.Repaint)
			{
				GUI.skin = UnityGUITools.GetValidGUISkin(guiSkin);
			}
			if (pauseGameWhileOpen)
			{
				Time.timeScale = 0f;
			}
			DrawConsole();
		}
	}

	protected virtual void DrawConsole()
	{
		if (m_windowRect.width <= 0f)
		{
			m_windowRect = DefineWindowRect();
			m_closeButtonRect = new Rect(m_windowRect.width - 30f, 2f, 26f, 16f);
		}
		m_windowRect = GUI.Window(0, m_windowRect, DrawConsoleWindow, "Lua Console");
	}

	protected Rect DefineWindowRect()
	{
		float num = Mathf.Max(minSize.x, (float)Screen.width / 4f);
		float height = Mathf.Max(minSize.y, (float)Screen.height / 4f);
		return new Rect((float)Screen.width - num, 0f, num, height);
	}

	protected virtual void DrawConsoleWindow(int id)
	{
		if (IsKeyEvent(KeyCode.Return))
		{
			RunLuaCommand();
		}
		else if (IsKeyEvent(KeyCode.UpArrow))
		{
			UseHistory(-1);
		}
		else if (IsKeyEvent(KeyCode.DownArrow))
		{
			UseHistory(1);
		}
		else if (IsKeyEvent(KeyCode.Escape) || GUI.Button(m_closeButtonRect, "X"))
		{
			SetVisible(newValue: false);
			return;
		}
		GUI.SetNextControlName("Input");
		GUI.FocusControl("Input");
		if (string.Equals(m_input, "\n"))
		{
			m_input = string.Empty;
		}
		m_input = GUILayout.TextArea(m_input);
		m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition);
		GUILayout.Label(m_output);
		GUILayout.EndScrollView();
	}

	protected virtual bool IsKeyEvent(KeyCode keyCode)
	{
		if (Event.current.type == EventType.KeyDown && Event.current.keyCode == keyCode)
		{
			Event.current.Use();
			return true;
		}
		return false;
	}

	protected virtual void RunLuaCommand()
	{
		if (!string.IsNullOrEmpty(m_input))
		{
			try
			{
				Lua.Result result = Lua.Run(m_input, DialogueDebug.logInfo);
				m_output = "Output: " + GetLuaResultString(result);
			}
			catch (Exception ex)
			{
				m_output = "Output: [Exception] " + ex.Message;
			}
			m_history.Add(m_input);
			if (m_history.Count >= maxHistory)
			{
				m_history.RemoveAt(0);
			}
			m_historyPosition = m_history.Count;
			m_input = string.Empty;
		}
	}

	protected virtual string GetLuaResultString(Lua.Result result)
	{
		if (!result.hasReturnValue)
		{
			return "(no return value)";
		}
		if (!result.isTable)
		{
			return result.asString;
		}
		return FormatTableResult(result);
	}

	protected virtual string FormatTableResult(Lua.Result result)
	{
		if (!result.isTable)
		{
			return result.asString;
		}
		LuaTableWrapper asTable = result.asTable;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("Table:\n");
		foreach (string key in asTable.keys)
		{
			stringBuilder.Append(string.Format("[{0}]: {1}\n", new object[2]
			{
				key.ToString(),
				asTable[key.ToString()].ToString()
			}));
		}
		return stringBuilder.ToString();
	}

	protected virtual void UseHistory(int direction)
	{
		m_historyPosition = Mathf.Clamp(m_historyPosition + direction, 0, m_history.Count);
		m_input = ((m_history.Count > 0 && m_historyPosition < m_history.Count) ? m_history[m_historyPosition] : string.Empty);
	}
}
