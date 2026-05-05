namespace Language.Lua;

public abstract class Access
{
	public abstract LuaValue Evaluate(LuaValue baseValue, LuaTable enviroment);
}
