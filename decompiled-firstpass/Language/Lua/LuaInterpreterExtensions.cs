using System;
using System.Collections.Generic;

namespace Language.Lua;

public static class LuaInterpreterExtensions
{
	public static List<LuaValue> EvaluateAll(List<Expr> exprList, LuaTable environment)
	{
		List<LuaValue> list = new List<LuaValue>();
		foreach (Expr expr in exprList)
		{
			list.Add(expr.Evaluate(environment));
		}
		return list;
	}

	public static LuaValue ObjectToLuaValue(object o)
	{
		if (o == null)
		{
			return LuaNil.Nil;
		}
		Type type = o.GetType();
		if (type == typeof(bool))
		{
			if (!(bool)o)
			{
				return LuaBoolean.False;
			}
			return LuaBoolean.True;
		}
		if (type == typeof(string))
		{
			return new LuaString((string)o);
		}
		if (type == typeof(int))
		{
			return new LuaNumber((int)o);
		}
		if (type == typeof(float))
		{
			return new LuaNumber((float)o);
		}
		if (type == typeof(double))
		{
			return new LuaNumber((double)o);
		}
		if (type == typeof(byte))
		{
			return new LuaNumber((int)(byte)o);
		}
		if (type == typeof(sbyte))
		{
			return new LuaNumber((sbyte)o);
		}
		if (type == typeof(short))
		{
			return new LuaNumber((short)o);
		}
		if (type == typeof(ushort))
		{
			return new LuaNumber((int)(ushort)o);
		}
		if (type == typeof(uint))
		{
			return new LuaNumber((uint)o);
		}
		if (type == typeof(long))
		{
			return new LuaNumber((long)o);
		}
		if (type == typeof(ulong))
		{
			return new LuaNumber((ulong)o);
		}
		if (o is LuaValue)
		{
			return o as LuaValue;
		}
		return new LuaString(o.ToString());
	}

	public static object LuaValueToObject(LuaValue luaValue)
	{
		if (luaValue == null)
		{
			return null;
		}
		if (luaValue is LuaNumber)
		{
			return (float)(luaValue as LuaNumber).Number;
		}
		return luaValue.Value;
	}
}
