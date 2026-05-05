using System;

namespace Language.Lua;

public class Term : Expr
{
	public override LuaValue Evaluate(LuaTable enviroment)
	{
		throw new NotImplementedException();
	}

	public override Term Simplify()
	{
		return this;
	}
}
