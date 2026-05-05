namespace Language.Lua;

public class GroupExpr : BaseExpr
{
	public Expr Expr;

	public override LuaValue Evaluate(LuaTable enviroment)
	{
		return Expr.Evaluate(enviroment);
	}

	public override Term Simplify()
	{
		return Expr.Simplify();
	}
}
