namespace PixelCrushers.DialogueSystem;

public class LuaWatchItem
{
	private string m_currentValue;

	public string luaExpression { get; set; }

	public string LuaExpression
	{
		get
		{
			return luaExpression;
		}
		set
		{
			luaExpression = value;
		}
	}

	private event LuaChangedDelegate luaChanged;

	public static string LuaExpressionWithReturn(string luaExpression)
	{
		if (!luaExpression.StartsWith("return "))
		{
			return "return " + luaExpression;
		}
		return luaExpression;
	}

	public LuaWatchItem(string luaExpression, LuaChangedDelegate luaChangedHandler)
	{
		this.luaExpression = LuaExpressionWithReturn(luaExpression);
		m_currentValue = Lua.Run(this.luaExpression).asString;
		this.luaChanged = luaChangedHandler;
	}

	public bool Matches(string luaExpression, LuaChangedDelegate luaChangedHandler)
	{
		if (luaChangedHandler == this.luaChanged)
		{
			return string.Equals(luaExpression, this.luaExpression);
		}
		return false;
	}

	public void Check()
	{
		Lua.Result newValue = Lua.Run(luaExpression);
		string asString = newValue.asString;
		if (!string.Equals(m_currentValue, asString))
		{
			m_currentValue = asString;
			if (this.luaChanged != null)
			{
				this.luaChanged(this, newValue);
			}
		}
	}
}
