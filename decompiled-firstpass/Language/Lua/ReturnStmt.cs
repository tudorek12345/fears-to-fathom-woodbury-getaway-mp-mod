using System;
using System.Collections.Generic;

namespace Language.Lua;

public class ReturnStmt : Statement
{
	public List<Expr> ExprList = new List<Expr>();

	public override LuaValue Execute(LuaTable enviroment, out bool isBreak)
	{
		throw new NotImplementedException();
	}
}
