namespace Language.Lua;

public class WhileStmt : Statement
{
	public Expr Condition;

	public Chunk Body;

	public override LuaValue Execute(LuaTable enviroment, out bool isBreak)
	{
		while (Condition.Evaluate(enviroment).GetBooleanValue())
		{
			LuaValue luaValue = Body.Execute(enviroment, out isBreak);
			if (luaValue != null || isBreak)
			{
				isBreak = false;
				return luaValue;
			}
		}
		isBreak = false;
		return null;
	}
}
