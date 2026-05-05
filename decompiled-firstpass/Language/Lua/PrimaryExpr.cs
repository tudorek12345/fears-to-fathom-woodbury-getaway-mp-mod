using System.Collections.Generic;

namespace Language.Lua;

public class PrimaryExpr : Term
{
	public BaseExpr Base;

	public List<Access> Accesses = new List<Access>();

	public override LuaValue Evaluate(LuaTable enviroment)
	{
		LuaValue luaValue = Base.Evaluate(enviroment);
		foreach (Access access in Accesses)
		{
			luaValue = access.Evaluate(luaValue, enviroment);
		}
		return luaValue;
	}

	public override Term Simplify()
	{
		if (Accesses.Count == 0)
		{
			return Base.Simplify();
		}
		return this;
	}
}
