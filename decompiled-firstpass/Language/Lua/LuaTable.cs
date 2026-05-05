using System;
using System.Collections.Generic;
using System.Reflection;

namespace Language.Lua;

public class LuaTable : LuaValue
{
	private List<LuaValue> list;

	private Dictionary<LuaValue, LuaValue> dict;

	private Dictionary<int, LuaValue> m_intKeyCache = new Dictionary<int, LuaValue>();

	private Dictionary<string, LuaValue> m_stringKeyCache = new Dictionary<string, LuaValue>();

	public LuaTable MetaTable { get; set; }

	public override object Value => this;

	public int Length
	{
		get
		{
			if (list == null)
			{
				return 0;
			}
			return list.Count;
		}
	}

	public int Count
	{
		get
		{
			if (dict == null)
			{
				return 0;
			}
			return dict.Count;
		}
	}

	public List<LuaValue> List
	{
		get
		{
			if (list == null)
			{
				list = new List<LuaValue>();
			}
			return list;
		}
	}

	public Dictionary<LuaValue, LuaValue> Dict
	{
		get
		{
			if (dict == null)
			{
				dict = new Dictionary<LuaValue, LuaValue>();
			}
			return dict;
		}
	}

	public IEnumerable<LuaValue> ListValues => list;

	public IEnumerable<LuaValue> Keys
	{
		get
		{
			if (Length > 0)
			{
				for (int index = 1; index <= list.Count; index++)
				{
					yield return new LuaNumber(index);
				}
			}
			if (Count <= 0)
			{
				yield break;
			}
			foreach (LuaValue key in dict.Keys)
			{
				yield return key;
			}
		}
	}

	public IEnumerable<KeyValuePair<LuaValue, LuaValue>> KeyValuePairs => dict;

	public LuaTable()
	{
	}

	public LuaTable(LuaTable parent)
	{
		MetaTable = new LuaTable();
		MetaTable.SetNameValue("__index", parent);
		MetaTable.SetNameValue("__newindex", parent);
	}

	public override string GetTypeCode()
	{
		return "table";
	}

	public override string ToString()
	{
		if (MetaTable != null && MetaTable.GetValue("__tostring") is LuaFunction luaFunction)
		{
			return luaFunction.Invoke(new LuaValue[1] { this }).ToString();
		}
		return "Table " + GetHashCode();
	}

	public void AddRaw(string key, LuaValue value)
	{
		if (m_stringKeyCache.ContainsKey(key))
		{
			LuaValue key2 = m_stringKeyCache[key];
			Dict[key2] = value;
		}
		else
		{
			LuaString luaString = new LuaString(key);
			Dict[luaString] = value;
			m_stringKeyCache[key] = luaString;
		}
	}

	public void AddRaw(int key, LuaValue value)
	{
		if (m_intKeyCache.ContainsKey(key))
		{
			LuaValue key2 = m_intKeyCache[key];
			Dict[key2] = value;
		}
		else if (key == Length + 1)
		{
			AddValue(value);
		}
		else if (key > 0 && key <= Length)
		{
			list[key - 1] = value;
		}
		else
		{
			LuaNumber luaNumber = new LuaNumber(key);
			Dict[luaNumber] = value;
			m_intKeyCache[key] = luaNumber;
		}
	}

	public bool ContainsKey(LuaValue key)
	{
		if (dict != null && dict.ContainsKey(key))
		{
			return true;
		}
		if (list != null && key is LuaNumber luaNumber && luaNumber.Number == (double)(int)luaNumber.Number)
		{
			if (luaNumber.Number >= 1.0)
			{
				return luaNumber.Number <= (double)list.Count;
			}
			return false;
		}
		return false;
	}

	public void AddValue(LuaValue value)
	{
		if (list == null)
		{
			list = new List<LuaValue>();
		}
		list.Add(value);
	}

	public void InsertValue(int index, LuaValue value)
	{
		if (index > 0 && index <= Length + 1)
		{
			list.Insert(index - 1, value);
			return;
		}
		throw new ArgumentOutOfRangeException("index");
	}

	public bool Remove(LuaValue item)
	{
		return list.Remove(item);
	}

	public void RemoveAt(int index)
	{
		list.RemoveAt(index - 1);
	}

	public void Sort()
	{
		list.Sort(delegate(LuaValue a, LuaValue b)
		{
			LuaNumber luaNumber = a as LuaNumber;
			LuaNumber luaNumber2 = b as LuaNumber;
			if (luaNumber != null && luaNumber2 != null)
			{
				return luaNumber.Number.CompareTo(luaNumber2.Number);
			}
			LuaString luaString = a as LuaString;
			LuaString luaString2 = b as LuaString;
			return (luaString != null && luaString2 != null) ? luaString.Text.CompareTo(luaString2.Text) : 0;
		});
	}

	public void Sort(LuaFunction compare)
	{
		list.Sort((LuaValue a, LuaValue b) => (compare.Invoke(new LuaValue[2] { a, b }) is LuaBoolean { BoolValue: not false }) ? 1 : (-1));
	}

	public LuaValue GetValue(int index)
	{
		if (index > 0 && index <= Length)
		{
			return list[index - 1];
		}
		if (dict != null)
		{
			if (m_intKeyCache.ContainsKey(index) && dict.ContainsKey(m_intKeyCache[index]))
			{
				return dict[m_intKeyCache[index]];
			}
			return GetValue(index.ToString());
		}
		return LuaNil.Nil;
	}

	public LuaValue GetValue(string name)
	{
		LuaValue key = GetKey(name);
		if (key == LuaNil.Nil)
		{
			if (MetaTable != null)
			{
				return GetValueFromMetaTable(name);
			}
			return LuaNil.Nil;
		}
		return dict[key];
	}

	public LuaValue GetKey(string key)
	{
		if (dict == null)
		{
			return LuaNil.Nil;
		}
		if (m_stringKeyCache.ContainsKey(key))
		{
			return m_stringKeyCache[key];
		}
		foreach (LuaValue key2 in dict.Keys)
		{
			if (key2 != null && string.Equals(key2.ToString(), key, StringComparison.Ordinal))
			{
				return key2;
			}
		}
		return LuaNil.Nil;
	}

	public void SetNameValue(string name, LuaValue value)
	{
		if (value == LuaNil.Nil)
		{
			RemoveKey(name);
		}
		else
		{
			RawSetValue(name, value);
		}
	}

	private void RemoveKey(string name)
	{
		LuaValue key = GetKey(name);
		if (key != LuaNil.Nil)
		{
			dict.Remove(key);
		}
		m_stringKeyCache.Remove(name);
	}

	public void SetKeyValue(LuaValue key, LuaValue value)
	{
		if (key is LuaNumber luaNumber && luaNumber.Number == (double)(int)luaNumber.Number)
		{
			int num = (int)luaNumber.Number;
			if (m_intKeyCache.ContainsKey(num) && Dict.ContainsKey(m_intKeyCache[num]))
			{
				Dict[m_intKeyCache[num]] = value;
				return;
			}
			if (num == Length + 1)
			{
				AddValue(value);
				return;
			}
			if (num > 0 && num <= Length)
			{
				list[num - 1] = value;
				return;
			}
		}
		if (value == LuaNil.Nil)
		{
			RemoveKey(key);
			return;
		}
		if (dict == null)
		{
			dict = new Dictionary<LuaValue, LuaValue>();
		}
		dict[key] = value;
		if (GetIntValue(key, out var intValue))
		{
			m_intKeyCache[intValue] = key;
		}
		else if (key is LuaString)
		{
			m_stringKeyCache[(key as LuaString).Text] = key;
		}
	}

	private bool GetIntValue(LuaValue value, out int intValue)
	{
		if (value is LuaNumber luaNumber && luaNumber.Number == (double)(int)luaNumber.Number)
		{
			intValue = (int)luaNumber.Number;
			return true;
		}
		intValue = 0;
		return false;
	}

	private void RemoveKey(LuaValue key)
	{
		if (key != LuaNil.Nil && dict != null && dict.ContainsKey(key))
		{
			dict.Remove(key);
		}
		if (GetIntValue(key, out var intValue))
		{
			m_intKeyCache.Remove(intValue);
		}
		else if (key is LuaString)
		{
			m_stringKeyCache.Remove((key as LuaString).Text);
		}
	}

	public LuaValue GetValue(LuaValue key)
	{
		if (key == LuaNil.Nil)
		{
			return LuaNil.Nil;
		}
		if (key is LuaNumber luaNumber && luaNumber.Number == (double)(int)luaNumber.Number)
		{
			int num = (int)luaNumber.Number;
			if (num > 0 && num <= Length)
			{
				return list[num - 1];
			}
		}
		if (dict != null && dict.ContainsKey(key))
		{
			return dict[key];
		}
		if (MetaTable != null)
		{
			return GetValueFromMetaTable(key);
		}
		return LuaNil.Nil;
	}

	private LuaValue GetValueFromMetaTable(string name)
	{
		LuaValue value = MetaTable.GetValue("__index");
		if (value is LuaTable luaTable)
		{
			return luaTable.GetValue(name);
		}
		if (value is LuaFunction luaFunction)
		{
			return luaFunction.Function(new LuaValue[1]
			{
				new LuaString(name)
			});
		}
		return LuaNil.Nil;
	}

	private LuaValue GetValueFromMetaTable(LuaValue key)
	{
		LuaValue value = MetaTable.GetValue("__index");
		if (value is LuaTable luaTable)
		{
			return luaTable.GetValue(key);
		}
		if (value is LuaFunction luaFunction)
		{
			return luaFunction.Function(new LuaValue[1] { key });
		}
		return LuaNil.Nil;
	}

	public LuaFunction Register(string name, LuaFunc function)
	{
		LuaFunction luaFunction = new LuaFunction(function);
		SetNameValue(name, luaFunction);
		return luaFunction;
	}

	public LuaFunction RegisterMethodFunction(string name, object target, MethodInfo methodInfo)
	{
		LuaMethodFunction luaMethodFunction = new LuaMethodFunction(target, methodInfo);
		SetNameValue(name, luaMethodFunction);
		return luaMethodFunction;
	}

	public LuaValue RawGetValue(LuaValue key)
	{
		if (dict != null && dict.ContainsKey(key))
		{
			return dict[key];
		}
		return LuaNil.Nil;
	}

	public void RawSetValue(string name, LuaValue value)
	{
		LuaValue luaValue = GetKey(name);
		if (luaValue == LuaNil.Nil)
		{
			luaValue = new LuaString(name);
			m_stringKeyCache[name] = luaValue;
		}
		if (dict == null)
		{
			dict = new Dictionary<LuaValue, LuaValue>();
		}
		dict[luaValue] = value;
	}
}
