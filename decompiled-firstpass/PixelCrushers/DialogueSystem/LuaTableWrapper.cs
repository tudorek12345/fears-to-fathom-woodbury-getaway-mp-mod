using System.Collections.Generic;
using Language.Lua;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public class LuaTableWrapper
{
	public LuaTable luaTable;

	public bool isValid => luaTable != null;

	public int count
	{
		get
		{
			if (luaTable == null)
			{
				return 0;
			}
			return Mathf.Max(luaTable.Length, luaTable.Count);
		}
	}

	public IEnumerable<string> keys
	{
		get
		{
			if (luaTable == null || count <= 0)
			{
				yield break;
			}
			foreach (LuaValue key in luaTable.Keys)
			{
				yield return key.ToString();
			}
		}
	}

	public IEnumerable<object> values
	{
		get
		{
			if (luaTable == null)
			{
				yield break;
			}
			if (luaTable.Length > 0)
			{
				foreach (LuaValue listValue in luaTable.ListValues)
				{
					yield return (listValue is LuaTable) ? new LuaTableWrapper(listValue as LuaTable) : LuaInterpreterExtensions.LuaValueToObject(listValue);
				}
			}
			else
			{
				if (luaTable.Count <= 0)
				{
					yield break;
				}
				foreach (KeyValuePair<LuaValue, LuaValue> keyValuePair in luaTable.KeyValuePairs)
				{
					yield return (keyValuePair.Value is LuaTable) ? new LuaTableWrapper(keyValuePair.Value as LuaTable) : LuaInterpreterExtensions.LuaValueToObject(keyValuePair.Value);
				}
			}
		}
	}

	public bool IsValid => isValid;

	public int Count => count;

	public IEnumerable<string> Keys
	{
		get
		{
			if (luaTable == null || count <= 0)
			{
				yield break;
			}
			foreach (LuaValue key in luaTable.Keys)
			{
				yield return key.ToString();
			}
		}
	}

	public IEnumerable<object> Values
	{
		get
		{
			if (luaTable == null)
			{
				yield break;
			}
			if (luaTable.Length > 0)
			{
				foreach (LuaValue listValue in luaTable.ListValues)
				{
					yield return (listValue is LuaTable) ? new LuaTableWrapper(listValue as LuaTable) : LuaInterpreterExtensions.LuaValueToObject(listValue);
				}
			}
			else
			{
				if (luaTable.Count <= 0)
				{
					yield break;
				}
				foreach (KeyValuePair<LuaValue, LuaValue> keyValuePair in luaTable.KeyValuePairs)
				{
					yield return (keyValuePair.Value is LuaTable) ? new LuaTableWrapper(keyValuePair.Value as LuaTable) : LuaInterpreterExtensions.LuaValueToObject(keyValuePair.Value);
				}
			}
		}
	}

	public object this[string key]
	{
		get
		{
			if (luaTable == null)
			{
				if (DialogueDebug.logErrors)
				{
					Debug.LogError(string.Format("{0}: Lua table is null; lookup[{1}] failed", new object[2] { "Dialogue System", key }));
				}
				return null;
			}
			LuaValue nil = LuaNil.Nil;
			if (luaTable.Length > 0)
			{
				nil = luaTable.GetValue(Tools.StringToInt(key));
			}
			else
			{
				if (luaTable.GetKey(key) == LuaNil.Nil)
				{
					return null;
				}
				nil = luaTable.GetValue(key);
			}
			if (nil is LuaTable)
			{
				return new LuaTableWrapper(nil as LuaTable);
			}
			return LuaInterpreterExtensions.LuaValueToObject(nil);
		}
	}

	public object this[int key]
	{
		get
		{
			if (luaTable == null)
			{
				if (DialogueDebug.logErrors)
				{
					Debug.LogError(string.Format("{0}: Lua table is null; lookup[{1}] failed", new object[2] { "Dialogue System", key }));
				}
				return null;
			}
			LuaValue nil = LuaNil.Nil;
			if (luaTable.Length > 0)
			{
				nil = luaTable.GetValue(key);
			}
			else
			{
				if (luaTable.GetKey(key.ToString()) == LuaNil.Nil)
				{
					return null;
				}
				nil = luaTable.GetValue(key);
			}
			if (nil is LuaTable)
			{
				return new LuaTableWrapper(nil as LuaTable);
			}
			return LuaInterpreterExtensions.LuaValueToObject(nil);
		}
	}

	public LuaTableWrapper(LuaTable luaTable)
	{
		this.luaTable = luaTable;
	}
}
