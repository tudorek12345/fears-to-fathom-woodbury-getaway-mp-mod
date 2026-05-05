using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public class LuaWatchers
{
	private LuaWatchList m_everyUpdateList = new LuaWatchList();

	private LuaWatchList m_everyDialogueEntryList = new LuaWatchList();

	private LuaWatchList m_endOfConversationList = new LuaWatchList();

	public void AddObserver(string luaExpression, LuaWatchFrequency frequency, LuaChangedDelegate luaChangedHandler)
	{
		switch (frequency)
		{
		case LuaWatchFrequency.EveryUpdate:
			m_everyUpdateList.AddObserver(luaExpression, luaChangedHandler);
			break;
		case LuaWatchFrequency.EveryDialogueEntry:
			m_everyDialogueEntryList.AddObserver(luaExpression, luaChangedHandler);
			break;
		case LuaWatchFrequency.EndOfConversation:
			m_endOfConversationList.AddObserver(luaExpression, luaChangedHandler);
			break;
		default:
			Debug.LogError("Dialogue System: Internal error - unexpected Lua watch frequency " + frequency);
			break;
		}
	}

	public void RemoveObserver(string luaExpression, LuaWatchFrequency frequency, LuaChangedDelegate luaChangedHandler)
	{
		switch (frequency)
		{
		case LuaWatchFrequency.EveryUpdate:
			m_everyUpdateList.RemoveObserver(luaExpression, luaChangedHandler);
			break;
		case LuaWatchFrequency.EveryDialogueEntry:
			m_everyDialogueEntryList.RemoveObserver(luaExpression, luaChangedHandler);
			break;
		case LuaWatchFrequency.EndOfConversation:
			m_endOfConversationList.RemoveObserver(luaExpression, luaChangedHandler);
			break;
		default:
			Debug.LogError("Dialogue System: Internal error - unexpected Lua watch frequency " + frequency);
			break;
		}
	}

	public void RemoveAllObservers(LuaWatchFrequency frequency)
	{
		switch (frequency)
		{
		case LuaWatchFrequency.EveryUpdate:
			m_everyUpdateList.RemoveAllObservers();
			break;
		case LuaWatchFrequency.EveryDialogueEntry:
			m_everyDialogueEntryList.RemoveAllObservers();
			break;
		case LuaWatchFrequency.EndOfConversation:
			m_endOfConversationList.RemoveAllObservers();
			break;
		default:
			Debug.LogError("Dialogue System: Internal error - unexpected Lua watch frequency " + frequency);
			break;
		}
	}

	public void RemoveAllObservers()
	{
		m_everyUpdateList.RemoveAllObservers();
		m_everyDialogueEntryList.RemoveAllObservers();
		m_endOfConversationList.RemoveAllObservers();
	}

	public void NotifyObservers(LuaWatchFrequency frequency)
	{
		switch (frequency)
		{
		case LuaWatchFrequency.EveryUpdate:
			m_everyUpdateList.NotifyObservers();
			break;
		case LuaWatchFrequency.EveryDialogueEntry:
			m_everyDialogueEntryList.NotifyObservers();
			break;
		case LuaWatchFrequency.EndOfConversation:
			m_endOfConversationList.NotifyObservers();
			break;
		default:
			Debug.LogError("Dialogue System: Internal error - unexpected Lua watch frequency " + frequency);
			break;
		}
	}
}
