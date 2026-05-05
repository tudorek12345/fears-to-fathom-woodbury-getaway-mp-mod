using System;
using System.Collections.Generic;

namespace Language.Lua;

public class LuaMultiValue : LuaValue
{
	public LuaValue[] Values { get; set; }

	public override object Value => Values;

	public LuaMultiValue(LuaValue[] values)
	{
		Values = values;
	}

	public override string GetTypeCode()
	{
		throw new InvalidOperationException();
	}

	public static LuaValue WrapLuaValues(LuaValue[] values)
	{
		if (values == null || values.Length == 0)
		{
			return LuaNil.Nil;
		}
		if (values.Length == 1)
		{
			return values[0];
		}
		return new LuaMultiValue(UnWrapLuaValues(values));
	}

	public static LuaValue[] UnWrapLuaValues(LuaValue[] values)
	{
		if (values == null || values.Length == 0 || !ContainsMultiValue(values))
		{
			return values;
		}
		if (values.Length == 1 && values[0] is LuaMultiValue)
		{
			return (values[0] as LuaMultiValue).Values;
		}
		List<LuaValue> list = new List<LuaValue>(values.Length);
		for (int i = 0; i < values.Length - 1; i++)
		{
			LuaValue luaValue = values[i];
			if (luaValue is LuaMultiValue luaMultiValue)
			{
				list.Add(luaMultiValue.Values[0]);
			}
			else
			{
				list.Add(luaValue);
			}
		}
		LuaValue luaValue2 = values[^1];
		if (luaValue2 is LuaMultiValue luaMultiValue2)
		{
			list.AddRange(luaMultiValue2.Values);
		}
		else
		{
			list.Add(luaValue2);
		}
		return list.ToArray();
	}

	private static bool ContainsMultiValue(LuaValue[] values)
	{
		for (int i = 0; i < values.Length; i++)
		{
			if (values[i] is LuaMultiValue)
			{
				return true;
			}
		}
		return false;
	}
}
