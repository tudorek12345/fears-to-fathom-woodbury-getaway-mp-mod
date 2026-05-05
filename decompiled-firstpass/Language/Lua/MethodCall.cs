using System;
using System.Collections.Generic;

namespace Language.Lua;

public class MethodCall : Access
{
	public string Method;

	public Args Args;

	public override LuaValue Evaluate(LuaValue baseValue, LuaTable enviroment)
	{
		if (LuaValue.GetKeyValue(baseValue, new LuaString(Method)) is LuaFunction luaFunction)
		{
			if (Args.Table != null)
			{
				return luaFunction.Function(new LuaValue[2]
				{
					baseValue,
					Args.Table.Evaluate(enviroment)
				});
			}
			if (Args.String != null)
			{
				return luaFunction.Function(new LuaValue[2]
				{
					baseValue,
					Args.String.Evaluate(enviroment)
				});
			}
			List<LuaValue> list = LuaInterpreterExtensions.EvaluateAll(Args.ArgList, enviroment);
			list.Insert(0, baseValue);
			return luaFunction.Function(list.ToArray());
		}
		throw new Exception("Invoke method call on non function value.");
	}
}
