using System;
using System.Reflection;
using UnityEngine;

namespace Language.Lua;

public class LuaMethodFunction : LuaFunction
{
	public object Target { get; set; }

	public MethodInfo Method { get; set; }

	public LuaMethodFunction(object target, MethodInfo method)
		: base(null)
	{
		base.Function = InvokeMethod;
		Target = target;
		Method = method;
	}

	public LuaValue InvokeMethod(LuaValue[] args)
	{
		if (Method == null || args == null)
		{
			return LuaNil.Nil;
		}
		object[] array = new object[args.Length];
		for (int i = 0; i < args.Length; i++)
		{
			array[i] = LuaInterpreterExtensions.LuaValueToObject(args[i]);
		}
		try
		{
			return LuaInterpreterExtensions.ObjectToLuaValue(Method.Invoke(Target, array));
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
			throw ex;
		}
	}
}
