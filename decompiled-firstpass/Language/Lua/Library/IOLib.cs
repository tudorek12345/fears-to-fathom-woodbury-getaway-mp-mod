using System;
using System.Collections.Generic;
using System.IO;

namespace Language.Lua.Library;

public static class IOLib
{
	private static TextReader DefaultInput;

	private static TextWriter DefaultOutput;

	public static void RegisterModule(LuaTable enviroment)
	{
		LuaTable luaTable = new LuaTable();
		RegisterFunctions(luaTable);
		enviroment.SetNameValue("io", luaTable);
	}

	public static void RegisterFunctions(LuaTable module)
	{
		module.Register("input", input);
		module.Register("output", output);
		module.Register("open", open);
		module.Register("read", read);
		module.Register("write", write);
		module.Register("flush", flush);
		module.Register("tmpfile", tmpfile);
	}

	public static LuaValue input(LuaValue[] values)
	{
		if (values == null || values.Length == 0)
		{
			return new LuaUserdata(DefaultInput);
		}
		if (values[0] is LuaString)
		{
			return null;
		}
		if (values[0] is LuaUserdata luaUserdata && luaUserdata.Value is TextReader)
		{
			DefaultInput = luaUserdata.Value as TextReader;
		}
		return null;
	}

	public static LuaValue output(LuaValue[] values)
	{
		if (values == null || values.Length == 0)
		{
			return new LuaUserdata(DefaultOutput);
		}
		if (values[0] is LuaString)
		{
			return null;
		}
		if (values[0] is LuaUserdata luaUserdata && luaUserdata.Value is TextWriter)
		{
			DefaultOutput = luaUserdata.Value as TextWriter;
		}
		return null;
	}

	public static LuaValue open(LuaValue[] values)
	{
		LuaString luaString = ((values.Length > 1) ? (values[1] as LuaString) : null);
		string text = ((luaString == null) ? "r" : luaString.Text);
		switch (text)
		{
		case "r":
		case "r+":
			return null;
		case "w":
		case "w+":
			return null;
		case "a":
		case "a+":
			return null;
		default:
			throw new ArgumentException("Invalid file open mode " + text);
		}
	}

	public static LuaValue read(LuaValue[] values)
	{
		List<LuaValue> list = new List<LuaValue>(values.Length + 1);
		list.Add(input(null));
		list.AddRange(values);
		return FileLib.read(list.ToArray());
	}

	public static LuaValue write(LuaValue[] values)
	{
		List<LuaValue> list = new List<LuaValue>(values.Length + 1);
		list.Add(output(null));
		list.AddRange(values);
		return FileLib.write(list.ToArray());
	}

	public static LuaValue flush(LuaValue[] values)
	{
		return FileLib.flush(new LuaValue[1] { output(null) });
	}

	public static LuaValue tmpfile(LuaValue[] values)
	{
		return null;
	}
}
