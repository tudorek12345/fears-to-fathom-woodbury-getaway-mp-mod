using System;

namespace Language.Lua;

public class Function : Statement
{
	public FunctionName Name;

	public FunctionBody Body;

	public override LuaValue Execute(LuaTable enviroment, out bool isBreak)
	{
		LuaTable luaTable = enviroment;
		if (Name.MethodName == null)
		{
			for (int i = 0; i < Name.FullName.Count - 1; i++)
			{
				luaTable = enviroment.GetValue(Name.FullName[i]) as LuaTable;
				if (luaTable == null)
				{
					throw new Exception("Not a table: " + Name.FullName[i]);
				}
			}
			luaTable.SetNameValue(Name.FullName[Name.FullName.Count - 1], Body.Evaluate(enviroment));
		}
		else
		{
			for (int j = 0; j < Name.FullName.Count; j++)
			{
				luaTable = enviroment.GetValue(Name.FullName[j]) as LuaTable;
				if (luaTable == null)
				{
					throw new Exception("Not a table " + Name.FullName[j]);
				}
			}
			Body.ParamList.NameList.Insert(0, "self");
			luaTable.SetNameValue(Name.MethodName, Body.Evaluate(enviroment));
		}
		isBreak = false;
		return null;
	}
}
