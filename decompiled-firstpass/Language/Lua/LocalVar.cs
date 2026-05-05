using System;
using System.Collections.Generic;

namespace Language.Lua;

public class LocalVar : Statement
{
	public List<string> NameList = new List<string>();

	public List<Expr> ExprList = new List<Expr>();

	public override LuaValue Execute(LuaTable enviroment, out bool isBreak)
	{
		LuaValue[] array = LuaMultiValue.UnWrapLuaValues(LuaInterpreterExtensions.EvaluateAll(ExprList, enviroment).ToArray());
		for (int i = 0; i < Math.Min(NameList.Count, array.Length); i++)
		{
			enviroment.RawSetValue(NameList[i], array[i]);
		}
		if (array.Length < NameList.Count)
		{
			for (int j = array.Length; j < NameList.Count - array.Length; j++)
			{
				enviroment.RawSetValue(NameList[j], LuaNil.Nil);
			}
		}
		isBreak = false;
		return null;
	}
}
