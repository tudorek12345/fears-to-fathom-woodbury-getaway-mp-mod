namespace Language.Lua;

public abstract class Expr
{
	public abstract LuaValue Evaluate(LuaTable enviroment);

	public abstract Term Simplify();
}
