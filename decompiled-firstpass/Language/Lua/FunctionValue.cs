namespace Language.Lua;

public class FunctionValue : Term
{
	public FunctionBody Body;

	public override LuaValue Evaluate(LuaTable enviroment)
	{
		return Body.Evaluate(enviroment);
	}
}
