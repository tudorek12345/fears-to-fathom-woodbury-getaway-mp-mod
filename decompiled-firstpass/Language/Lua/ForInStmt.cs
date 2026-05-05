using System;
using System.Collections.Generic;

namespace Language.Lua;

public class ForInStmt : Statement
{
	public List<string> NameList = new List<string>();

	public List<Expr> ExprList = new List<Expr>();

	public Chunk Body;

	public override LuaValue Execute(LuaTable enviroment, out bool isBreak)
	{
		LuaValue[] array = LuaMultiValue.UnWrapLuaValues(LuaInterpreterExtensions.EvaluateAll(ExprList, enviroment).ToArray());
		LuaFunction luaFunction = array[0] as LuaFunction;
		LuaValue luaValue = array[1];
		LuaValue luaValue2 = array[2];
		LuaTable luaTable = new LuaTable(enviroment);
		Body.Enviroment = luaTable;
		while (true)
		{
			LuaValue luaValue3 = luaFunction.Invoke(new LuaValue[2] { luaValue, luaValue2 });
			if (luaValue3 is LuaMultiValue luaMultiValue)
			{
				array = LuaMultiValue.UnWrapLuaValues(luaMultiValue.Values);
				luaValue2 = array[0];
				for (int i = 0; i < Math.Min(NameList.Count, array.Length); i++)
				{
					luaTable.SetNameValue(NameList[i], array[i]);
				}
			}
			else
			{
				luaValue2 = luaValue3;
				luaTable.SetNameValue(NameList[0], luaValue3);
			}
			if (luaValue2 == LuaNil.Nil)
			{
				break;
			}
			LuaValue luaValue4 = Body.Execute(out isBreak);
			if (luaValue4 != null || isBreak)
			{
				isBreak = false;
				return luaValue4;
			}
		}
		isBreak = false;
		return null;
	}
}
