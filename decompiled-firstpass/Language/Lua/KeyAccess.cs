namespace Language.Lua;

public class KeyAccess : Access
{
	public Expr Key;

	public override LuaValue Evaluate(LuaValue baseValue, LuaTable enviroment)
	{
		LuaValue key = Key.Evaluate(enviroment);
		return LuaValue.GetKeyValue(baseValue, key);
	}
}
