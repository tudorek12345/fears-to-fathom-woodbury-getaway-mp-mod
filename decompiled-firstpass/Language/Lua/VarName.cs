namespace Language.Lua;

public class VarName : BaseExpr
{
	public string Name;

	public override LuaValue Evaluate(LuaTable enviroment)
	{
		return enviroment.GetValue(Name);
	}

	public override Term Simplify()
	{
		return this;
	}
}
