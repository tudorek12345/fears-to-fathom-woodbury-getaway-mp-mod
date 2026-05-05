namespace Language.Lua;

public abstract class Statement
{
	public abstract LuaValue Execute(LuaTable enviroment, out bool isBreak);
}
