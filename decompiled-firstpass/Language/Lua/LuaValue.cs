using System;

namespace Language.Lua;

public abstract class LuaValue : IEquatable<LuaValue>
{
	public abstract object Value { get; }

	public abstract string GetTypeCode();

	public virtual bool GetBooleanValue()
	{
		return true;
	}

	public bool Equals(LuaValue other)
	{
		if (other == null)
		{
			return false;
		}
		if (this is LuaNil)
		{
			return other is LuaNil;
		}
		if (this is LuaTable && other is LuaTable)
		{
			return this == other;
		}
		return Value.Equals(other.Value);
	}

	public override bool Equals(object obj)
	{
		if (!(obj is LuaValue))
		{
			return base.Equals(obj);
		}
		return Equals(obj as LuaValue);
	}

	public override int GetHashCode()
	{
		if (this is LuaNumber || this is LuaString)
		{
			return Value.GetHashCode();
		}
		return base.GetHashCode();
	}

	public static LuaValue GetKeyValue(LuaValue baseValue, LuaValue key)
	{
		if (baseValue is LuaTable luaTable)
		{
			return luaTable.GetValue(key);
		}
		if (baseValue is LuaUserdata { MetaTable: not null } luaUserdata)
		{
			LuaValue value = luaUserdata.MetaTable.GetValue("__index");
			if (value != null)
			{
				if (value is LuaFunction luaFunction)
				{
					return luaFunction.Invoke(new LuaValue[2] { baseValue, key });
				}
				return GetKeyValue(value, key);
			}
		}
		throw new Exception($"Lookup of field '{key.Value}' in the table element failed because the table element itself isn't in the table.");
	}
}
