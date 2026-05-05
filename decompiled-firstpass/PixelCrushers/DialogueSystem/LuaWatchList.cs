using System;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem;

public class LuaWatchList
{
	private List<LuaWatchItem> m_watchList = new List<LuaWatchItem>();

	public void AddObserver(string luaExpression, LuaChangedDelegate luaChangedHandler)
	{
		m_watchList.Add(new LuaWatchItem(luaExpression, luaChangedHandler));
	}

	public void RemoveObserver(string luaExpression, LuaChangedDelegate luaChangedHandler)
	{
		m_watchList.RemoveAll((LuaWatchItem watchItem) => watchItem.Matches(LuaWatchItem.LuaExpressionWithReturn(luaExpression), luaChangedHandler));
	}

	public void RemoveAllObservers()
	{
		m_watchList.Clear();
	}

	public void NotifyObservers()
	{
		if (m_watchList.Count <= 0)
		{
			return;
		}
		try
		{
			for (int num = m_watchList.Count - 1; num >= 0; num--)
			{
				m_watchList[num].Check();
			}
		}
		catch (InvalidOperationException)
		{
		}
		catch (ArgumentOutOfRangeException)
		{
		}
	}
}
