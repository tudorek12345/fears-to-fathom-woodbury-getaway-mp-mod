using System;
using System.Collections.Generic;

namespace Language.Lua;

public class FunctionBody
{
	public ParamList ParamList;

	public Chunk Chunk;

	public LuaValue Evaluate(LuaTable enviroment)
	{
		return new LuaFunction(delegate(LuaValue[] args)
		{
			LuaTable luaTable = new LuaTable(enviroment);
			List<string> nameList = ParamList.NameList;
			if (nameList.Count > 0)
			{
				int num = Math.Min(nameList.Count, args.Length);
				for (int i = 0; i < num; i++)
				{
					luaTable.SetNameValue(nameList[i], args[i]);
				}
				if (ParamList.HasVarArg && num < args.Length)
				{
					LuaValue[] array = new LuaValue[args.Length - num];
					for (int j = 0; j < array.Length; j++)
					{
						array[j] = args[num + j];
					}
					luaTable.SetNameValue("...", new LuaMultiValue(array));
				}
			}
			else if (ParamList.IsVarArg != null)
			{
				luaTable.SetNameValue("...", new LuaMultiValue(args));
			}
			Chunk.Enviroment = luaTable;
			return Chunk.Execute();
		});
	}
}
