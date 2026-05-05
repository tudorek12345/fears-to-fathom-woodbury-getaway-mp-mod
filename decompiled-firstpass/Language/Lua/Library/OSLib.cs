using System;

namespace Language.Lua.Library;

public static class OSLib
{
	public static void RegisterModule(LuaTable enviroment)
	{
		LuaTable luaTable = new LuaTable();
		RegisterFunctions(luaTable);
		enviroment.SetNameValue("os", luaTable);
	}

	public static void RegisterFunctions(LuaTable module)
	{
		module.Register("clock", clock);
		module.Register("date", date);
		module.Register("time", time);
		module.Register("execute", execute);
		module.Register("exit", exit);
		module.Register("getenv", getenv);
		module.Register("remove", remove);
		module.Register("rename", rename);
		module.Register("tmpname", tmpname);
	}

	public static LuaValue clock(LuaValue[] values)
	{
		return new LuaNumber(Environment.TickCount / 1000);
	}

	public static LuaValue date(LuaValue[] values)
	{
		if (values[0] is LuaString luaString)
		{
			if (!(luaString.Text == "*t"))
			{
				return new LuaString(DateTime.Now.ToString(luaString.Text));
			}
			LuaTable luaTable = new LuaTable();
			DateTime now = DateTime.Now;
			luaTable.SetNameValue("year", new LuaNumber(now.Year));
			luaTable.SetNameValue("month", new LuaNumber(now.Month));
			luaTable.SetNameValue("day", new LuaNumber(now.Day));
			luaTable.SetNameValue("hour", new LuaNumber(now.Hour));
			luaTable.SetNameValue("min", new LuaNumber(now.Minute));
			luaTable.SetNameValue("sec", new LuaNumber(now.Second));
			luaTable.SetNameValue("wday", new LuaNumber((double)now.DayOfWeek));
			luaTable.SetNameValue("yday", new LuaNumber(now.DayOfYear));
			luaTable.SetNameValue("isdst", LuaBoolean.From(now.IsDaylightSavingTime()));
		}
		return new LuaString(DateTime.Now.ToString());
	}

	public static LuaValue time(LuaValue[] values)
	{
		return new LuaNumber(new TimeSpan(DateTime.Now.Ticks).TotalSeconds);
	}

	public static LuaValue execute(LuaValue[] values)
	{
		_ = values.LongLength;
		return new LuaNumber(1.0);
	}

	public static LuaValue exit(LuaValue[] values)
	{
		return new LuaNumber(0.0);
	}

	public static LuaValue getenv(LuaValue[] values)
	{
		string text = null;
		if (text == null)
		{
			return LuaNil.Nil;
		}
		return new LuaString(text);
	}

	public static LuaValue remove(LuaValue[] values)
	{
		return LuaNil.Nil;
	}

	public static LuaValue rename(LuaValue[] values)
	{
		return LuaNil.Nil;
	}

	public static LuaValue tmpname(LuaValue[] values)
	{
		return LuaNil.Nil;
	}
}
