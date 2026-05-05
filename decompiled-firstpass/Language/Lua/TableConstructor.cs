using System.Collections.Generic;

namespace Language.Lua;

public class TableConstructor : Term
{
	public List<Field> FieldList = new List<Field>();

	public override LuaValue Evaluate(LuaTable enviroment)
	{
		LuaTable luaTable = new LuaTable();
		foreach (Field field in FieldList)
		{
			if (field is NameValue nameValue)
			{
				luaTable.SetNameValue(nameValue.Name, nameValue.Value.Evaluate(enviroment));
			}
			else if (field is KeyValue keyValue)
			{
				luaTable.SetKeyValue(keyValue.Key.Evaluate(enviroment), keyValue.Value.Evaluate(enviroment));
			}
			else if (field is ItemValue itemValue)
			{
				luaTable.AddValue(itemValue.Value.Evaluate(enviroment));
			}
		}
		return luaTable;
	}
}
