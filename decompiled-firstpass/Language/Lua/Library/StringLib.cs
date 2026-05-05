using System;
using System.Text;

namespace Language.Lua.Library;

public static class StringLib
{
	public static void RegisterModule(LuaTable enviroment)
	{
		LuaTable luaTable = new LuaTable();
		RegisterFunctions(luaTable);
		enviroment.SetNameValue("string", luaTable);
	}

	public static void RegisterFunctions(LuaTable module)
	{
		module.Register("byte", @byte);
		module.Register("char", @char);
		module.Register("format", format);
		module.Register("len", len);
		module.Register("sub", sub);
		module.Register("lower", lower);
		module.Register("upper", upper);
		module.Register("rep", rep);
		module.Register("reverse", reverse);
		module.Register("find", find);
	}

	public static LuaValue @byte(LuaValue[] values)
	{
		LuaString luaString = values[0] as LuaString;
		LuaNumber luaNumber = ((values.Length > 1) ? (values[1] as LuaNumber) : null);
		LuaNumber luaNumber2 = ((values.Length > 2) ? (values[2] as LuaNumber) : null);
		int num = ((luaNumber == null) ? 1 : ((int)luaNumber.Number));
		LuaValue[] array = new LuaValue[((luaNumber2 == null) ? num : ((int)luaNumber2.Number)) - num + 1];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new LuaNumber(char.ConvertToUtf32(luaString.Text, num - 1 + i));
		}
		return new LuaMultiValue(array);
	}

	public static LuaValue @char(LuaValue[] values)
	{
		char[] array = new char[values.Length];
		for (int i = 0; i < array.Length; i++)
		{
			int num = (int)(values[i] as LuaNumber).Number;
			array[i] = (char)num;
		}
		return new LuaString(new string(array));
	}

	public static LuaValue format(LuaValue[] values)
	{
		LuaString luaString = values[0] as LuaString;
		object[] array = new object[values.Length - 1];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = values[i + 1].Value;
		}
		return new LuaString(string.Format(luaString.Text, array));
	}

	public static LuaValue sub(LuaValue[] values)
	{
		LuaString luaString = values[0] as LuaString;
		LuaNumber obj = values[1] as LuaNumber;
		LuaNumber luaNumber = ((values.Length > 2) ? (values[2] as LuaNumber) : null);
		int num = (int)obj.Number;
		int num2 = ((luaNumber == null) ? (-1) : ((int)luaNumber.Number));
		if (num < 0)
		{
			num = luaString.Text.Length + num + 1;
		}
		if (num2 < 0)
		{
			num2 = luaString.Text.Length + num2 + 1;
		}
		return new LuaString(luaString.Text.Substring(num - 1, num2 - num + 1));
	}

	public static LuaValue rep(LuaValue[] values)
	{
		LuaString luaString = values[0] as LuaString;
		LuaNumber luaNumber = values[1] as LuaNumber;
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; (double)i < luaNumber.Number; i++)
		{
			stringBuilder.Append(luaString.Text);
		}
		return new LuaString(stringBuilder.ToString());
	}

	public static LuaValue reverse(LuaValue[] values)
	{
		char[] array = (values[0] as LuaString).Text.ToCharArray();
		Array.Reverse(array);
		return new LuaString(new string(array));
	}

	public static LuaValue len(LuaValue[] values)
	{
		return new LuaNumber((values[0] as LuaString).Text.Length);
	}

	public static LuaValue lower(LuaValue[] values)
	{
		return new LuaString((values[0] as LuaString).Text.ToLower());
	}

	public static LuaValue upper(LuaValue[] values)
	{
		return new LuaString((values[0] as LuaString).Text.ToUpper());
	}

	public static LuaValue find(LuaValue[] values)
	{
		LuaString obj = values[0] as LuaString;
		LuaString luaString = values[1] as LuaString;
		LuaNumber luaNumber = ((values.Length > 2) ? (values[2] as LuaNumber) : null);
		string text = obj.ToString();
		string text2 = luaString.ToString();
		int num = ((luaNumber == null) ? 1 : ((int)luaNumber.Number));
		int num2 = text.IndexOf(text2, num - 1) + 1;
		if (num2 == 0)
		{
			return LuaNil.Nil;
		}
		int num3 = num2 + text2.Length - 1;
		LuaTable luaTable = new LuaTable();
		luaTable.AddValue(new LuaNumber(num2));
		luaTable.AddValue(new LuaNumber(num3));
		return luaTable;
	}
}
