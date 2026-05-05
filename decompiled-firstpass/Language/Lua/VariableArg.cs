namespace Language.Lua;

public class VariableArg : Term
{
	public string Name;

	public override LuaValue Evaluate(LuaTable enviroment)
	{
		return enviroment.GetValue(Name);
	}
}
