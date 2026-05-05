using System.Collections.Generic;
using System.Text;

namespace Language.Lua.Library;

public static class TableLib
{
	public static void RegisterModule(LuaTable enviroment)
	{
		LuaTable luaTable = new LuaTable();
		RegisterFunctions(luaTable);
		enviroment.SetNameValue("table", luaTable);
	}

	public static void RegisterFunctions(LuaTable module)
	{
		module.Register("concat", concat);
		module.Register("insert", insert);
		module.Register("remove", remove);
		module.Register("removeitem", removeitem);
		module.Register("maxn", maxn);
		module.Register("getn", getn);
		module.Register("setn", getn);
		module.Register("sort", sort);
	}

	public static LuaValue concat(LuaValue[] values)
	{
		LuaTable luaTable = values[0] as LuaTable;
		LuaString luaString = ((values.Length > 1) ? (values[1] as LuaString) : LuaString.Empty);
		LuaNumber luaNumber = ((values.Length > 2) ? (values[2] as LuaNumber) : null);
		LuaNumber luaNumber2 = ((values.Length > 3) ? (values[3] as LuaNumber) : null);
		int num = ((luaNumber == null) ? 1 : ((int)luaNumber.Number));
		int num2 = ((luaNumber2 == null) ? luaTable.Length : ((int)luaNumber2.Number));
		if (num > num2)
		{
			return LuaString.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = num; i < num2; i++)
		{
			stringBuilder.Append(luaTable.GetValue(i).ToString());
			stringBuilder.Append(luaString.Text);
		}
		stringBuilder.Append(luaTable.GetValue(num2).ToString());
		return new LuaString(stringBuilder.ToString());
	}

	public static LuaValue insert(LuaValue[] values)
	{
		LuaTable luaTable = values[0] as LuaTable;
		if (values.Length == 2)
		{
			LuaValue value = values[1];
			luaTable.AddValue(value);
		}
		else if (values.Length == 3)
		{
			LuaNumber obj = values[1] as LuaNumber;
			LuaValue value2 = values[2];
			int index = (int)obj.Number;
			luaTable.InsertValue(index, value2);
		}
		return null;
	}

	public static LuaValue remove(LuaValue[] values)
	{
		LuaTable obj = values[0] as LuaTable;
		int index = obj.Length;
		if (values.Length == 2)
		{
			index = (int)(values[1] as LuaNumber).Number;
		}
		LuaValue value = obj.GetValue(index);
		obj.RemoveAt(index);
		return value;
	}

	public static LuaValue removeitem(LuaValue[] values)
	{
		LuaTable obj = values[0] as LuaTable;
		LuaValue item = values[1];
		return LuaBoolean.From(obj.Remove(item));
	}

	public static LuaValue maxn(LuaValue[] values)
	{
		LuaTable obj = values[0] as LuaTable;
		double num = double.MinValue;
		foreach (LuaValue key in obj.Keys)
		{
			if (key is LuaNumber { Number: >0.0 } luaNumber && luaNumber.Number > num)
			{
				num = luaNumber.Number;
			}
		}
		return new LuaNumber(num);
	}

	public static LuaValue sort(LuaValue[] values)
	{
		LuaTable luaTable = values[0] as LuaTable;
		if (values.Length == 2)
		{
			LuaFunction compare = values[1] as LuaFunction;
			luaTable.Sort(compare);
		}
		else
		{
			luaTable.Sort();
		}
		return null;
	}

	public static LuaValue getn(LuaValue[] values)
	{
		int num = 0;
		if (values.Length >= 1 && values[0] is LuaTable luaTable)
		{
			if (luaTable.Count > 0)
			{
				foreach (KeyValuePair<LuaValue, LuaValue> keyValuePair in luaTable.KeyValuePairs)
				{
					LuaString luaString = keyValuePair.Key as LuaString;
					LuaValue value = keyValuePair.Value;
					if (luaString != null && string.Equals(luaString.Text, "n"))
					{
						if (value is LuaNumber)
						{
							return value as LuaNumber;
						}
						continue;
					}
					if (value == LuaNil.Nil)
					{
						return new LuaNumber(num);
					}
					num++;
				}
			}
			else
			{
				foreach (LuaValue listValue in luaTable.ListValues)
				{
					if (listValue == LuaNil.Nil)
					{
						return new LuaNumber(num);
					}
					num++;
				}
			}
		}
		return new LuaNumber(num);
	}

	public static LuaValue setn(LuaValue[] values)
	{
		if (values.Length >= 2)
		{
			LuaTable luaTable = values[0] as LuaTable;
			LuaNumber luaNumber = values[1] as LuaNumber;
			if (luaTable != null && luaNumber != null)
			{
				luaTable.SetNameValue("n", luaNumber);
			}
		}
		return LuaNil.Nil;
	}
}
