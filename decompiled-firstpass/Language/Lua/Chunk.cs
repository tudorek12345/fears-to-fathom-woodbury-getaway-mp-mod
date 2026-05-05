using System.Collections.Generic;

namespace Language.Lua;

public class Chunk
{
	public LuaTable Enviroment;

	public List<Statement> Statements = new List<Statement>();

	public LuaValue Execute()
	{
		bool isBreak;
		return Execute(out isBreak);
	}

	public LuaValue Execute(LuaTable enviroment, out bool isBreak)
	{
		Enviroment = new LuaTable(enviroment);
		return Execute(out isBreak);
	}

	public LuaValue Execute(out bool isBreak)
	{
		foreach (Statement statement in Statements)
		{
			if (statement is ReturnStmt returnStmt)
			{
				isBreak = false;
				return LuaMultiValue.WrapLuaValues(LuaInterpreterExtensions.EvaluateAll(returnStmt.ExprList, Enviroment).ToArray());
			}
			if (statement is BreakStmt)
			{
				isBreak = true;
				return null;
			}
			LuaValue luaValue = statement.Execute(Enviroment, out isBreak);
			if (luaValue != null || isBreak)
			{
				return luaValue;
			}
		}
		isBreak = false;
		return null;
	}
}
