using System;
using UnityEngine;

namespace Language.Lua.Library;

public class BaseLib
{
	public static void RegisterFunctions(LuaTable module)
	{
		module.Register("print", print);
		module.Register("type", type);
		module.Register("getmetatable", getmetatable);
		module.Register("setmetatable", setmetatable);
		module.Register("tostring", tostring);
		module.Register("tonumber", tonumber);
		module.Register("ipairs", ipairs);
		module.Register("pairs", pairs);
		module.Register("next", next);
		module.Register("assert", assert);
		module.Register("error", error);
		module.Register("rawget", rawget);
		module.Register("rawset", rawset);
		module.Register("select", select);
		module.Register("dofile", dofile);
		module.Register("loadstring", loadstring);
		module.Register("unpack", unpack);
		module.Register("pcall", pcall);
	}

	public static LuaValue print(LuaValue[] values)
	{
		for (int i = 0; i < values.Length; i++)
		{
			Debug.Log(values[i].ToString());
		}
		return null;
	}

	public static LuaValue type(LuaValue[] values)
	{
		if (values.Length != 0)
		{
			return new LuaString(values[0].GetTypeCode());
		}
		throw new Exception("bad argument #1 to 'type' (value expected)");
	}

	public static LuaValue tostring(LuaValue[] values)
	{
		return new LuaString(values[0].ToString());
	}

	public static LuaValue tonumber(LuaValue[] values)
	{
		if (values[0] is LuaString luaString)
		{
			return new LuaNumber(double.Parse(luaString.Text));
		}
		if (values[0] is LuaString result)
		{
			return result;
		}
		return LuaNil.Nil;
	}

	public static LuaValue setmetatable(LuaValue[] values)
	{
		LuaTable obj = values[0] as LuaTable;
		LuaTable metaTable = values[1] as LuaTable;
		obj.MetaTable = metaTable;
		return null;
	}

	public static LuaValue getmetatable(LuaValue[] values)
	{
		return (values[0] as LuaTable).MetaTable;
	}

	public static LuaValue rawget(LuaValue[] values)
	{
		LuaTable obj = values[0] as LuaTable;
		LuaValue key = values[1];
		return obj.RawGetValue(key);
	}

	public static LuaValue rawset(LuaValue[] values)
	{
		LuaTable obj = values[0] as LuaTable;
		LuaValue key = values[1];
		LuaValue value = values[2];
		obj.SetKeyValue(key, value);
		return null;
	}

	public static LuaValue ipairs(LuaValue[] values)
	{
		LuaTable luaTable = values[0] as LuaTable;
		LuaFunction luaFunction = new LuaFunction(delegate(LuaValue[] args)
		{
			LuaTable luaTable2 = args[0] as LuaTable;
			int num = (int)(args[1] as LuaNumber).Number + 1;
			return (num <= luaTable2.Length) ? ((LuaValue)new LuaMultiValue(new LuaValue[2]
			{
				new LuaNumber(num),
				luaTable2.GetValue(num)
			})) : ((LuaValue)LuaNil.Nil);
		});
		return new LuaMultiValue(new LuaValue[3]
		{
			luaFunction,
			luaTable,
			new LuaNumber(0.0)
		});
	}

	public static LuaValue pairs(LuaValue[] values)
	{
		LuaTable luaTable = values[0] as LuaTable;
		LuaFunction luaFunction = new LuaFunction(next);
		return new LuaMultiValue(new LuaValue[3]
		{
			luaFunction,
			luaTable,
			LuaNil.Nil
		});
	}

	public static LuaValue next(LuaValue[] values)
	{
		LuaTable luaTable = values[0] as LuaTable;
		LuaValue other = ((values.Length > 1) ? values[1] : LuaNil.Nil);
		LuaValue luaValue = LuaNil.Nil;
		LuaValue luaValue2 = LuaNil.Nil;
		foreach (LuaValue key in luaTable.Keys)
		{
			if (luaValue.Equals(other))
			{
				luaValue2 = key;
				break;
			}
			luaValue = key;
		}
		return new LuaMultiValue(new LuaValue[2]
		{
			luaValue2,
			luaTable.GetValue(luaValue2)
		});
	}

	public static LuaValue assert(LuaValue[] values)
	{
		LuaString luaString = ((values.Length > 1) ? (values[1] as LuaString) : null);
		if (luaString != null)
		{
			throw new LuaError(luaString.Text);
		}
		throw new LuaError("assertion failed!");
	}

	public static LuaValue error(LuaValue[] values)
	{
		if (values[0] is LuaString luaString)
		{
			throw new LuaError(luaString.Text);
		}
		throw new LuaError("error raised!");
	}

	public static LuaValue select(LuaValue[] values)
	{
		if (values[0] is LuaNumber luaNumber)
		{
			int num = (int)luaNumber.Number;
			LuaValue[] array = new LuaValue[values.Length - num];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = values[num + i];
			}
			return new LuaMultiValue(array);
		}
		if ((values[0] as LuaString).Text == "#")
		{
			return new LuaNumber(values.Length - 1);
		}
		return LuaNil.Nil;
	}

	public static LuaValue dofile(LuaValue[] values)
	{
		LuaString obj = values[0] as LuaString;
		return LuaInterpreter.RunFile(enviroment: values[1] as LuaTable, luaFile: obj.Text);
	}

	public static LuaValue loadstring(LuaValue[] values)
	{
		LuaString luaString = values[0] as LuaString;
		LuaTable enviroment = values[1] as LuaTable;
		Chunk chunk = LuaInterpreter.Parse(luaString.Text);
		return new LuaFunction(delegate
		{
			chunk.Enviroment = enviroment;
			return chunk.Execute();
		});
	}

	public static LuaValue unpack(LuaValue[] values)
	{
		LuaTable luaTable = values[0] as LuaTable;
		LuaNumber luaNumber = ((values.Length > 1) ? (values[1] as LuaNumber) : null);
		LuaNumber luaNumber2 = ((values.Length > 2) ? (values[2] as LuaNumber) : null);
		int num = ((luaNumber == null) ? 1 : ((int)luaNumber.Number));
		int num2 = ((luaNumber2 == null) ? values.Length : ((int)luaNumber2.Number));
		LuaValue[] array = new LuaValue[num2];
		for (int i = 0; i < num2; i++)
		{
			array[i] = luaTable.GetValue(num + i);
		}
		return new LuaMultiValue(array);
	}

	public static LuaValue pcall(LuaValue[] values)
	{
		LuaFunction luaFunction = values[0] as LuaFunction;
		try
		{
			LuaValue[] array = new LuaValue[values.Length - 1];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = values[i + 1];
			}
			LuaValue luaValue = luaFunction.Invoke(array);
			return new LuaMultiValue(LuaMultiValue.UnWrapLuaValues(new LuaValue[2]
			{
				LuaBoolean.True,
				luaValue
			}));
		}
		catch (Exception ex)
		{
			return new LuaMultiValue(new LuaValue[2]
			{
				LuaBoolean.False,
				new LuaString(ex.Message)
			});
		}
	}
}
