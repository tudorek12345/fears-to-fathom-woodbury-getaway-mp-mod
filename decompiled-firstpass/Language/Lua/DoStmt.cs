namespace Language.Lua;

public class DoStmt : Statement
{
	public Chunk Body;

	public override LuaValue Execute(LuaTable enviroment, out bool isBreak)
	{
		return Body.Execute(enviroment, out isBreak);
	}
}
