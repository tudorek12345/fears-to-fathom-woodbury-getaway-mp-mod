namespace Language.Lua;

public class ExprStmt : Statement
{
	public Expr Expr;

	public override LuaValue Execute(LuaTable enviroment, out bool isBreak)
	{
		Expr.Evaluate(enviroment);
		isBreak = false;
		return null;
	}
}
