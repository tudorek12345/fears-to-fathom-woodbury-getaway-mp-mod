namespace Language.Lua;

public class RepeatStmt : Statement
{
	public Chunk Body;

	public Expr Condition;

	public override LuaValue Execute(LuaTable enviroment, out bool isBreak)
	{
		do
		{
			LuaValue luaValue = Body.Execute(enviroment, out isBreak);
			if (luaValue != null || isBreak)
			{
				isBreak = false;
				return luaValue;
			}
		}
		while (!Condition.Evaluate(enviroment).GetBooleanValue());
		return null;
	}
}
