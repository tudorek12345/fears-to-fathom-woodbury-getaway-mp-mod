namespace Language.Lua;

public class NameAccess : Access
{
	public string Name;

	public override LuaValue Evaluate(LuaValue baseValue, LuaTable enviroment)
	{
		LuaValue key = new LuaString(Name);
		return LuaValue.GetKeyValue(baseValue, key);
	}
}
