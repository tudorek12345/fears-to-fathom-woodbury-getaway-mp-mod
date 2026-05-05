using System;
using System.Collections.Generic;

namespace Language.Lua;

public class FunctionCall : Access
{
	public Args Args;

	public override LuaValue Evaluate(LuaValue baseValue, LuaTable enviroment)
	{
		if (baseValue is LuaFunction luaFunction)
		{
			if (Args.Table != null)
			{
				return luaFunction.Function(new LuaValue[1] { Args.Table.Evaluate(enviroment) });
			}
			if (Args.String != null)
			{
				return luaFunction.Function(new LuaValue[1] { Args.String.Evaluate(enviroment) });
			}
			List<LuaValue> list = LuaInterpreterExtensions.EvaluateAll(Args.ArgList, enviroment);
			return luaFunction.Function(LuaMultiValue.UnWrapLuaValues(list.ToArray()));
		}
		throw new Exception("Tried to invoke a function call on a non-function value. If you're calling a function, is it registered with Lua?");
	}
}
