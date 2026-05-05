namespace Language.Lua;

public class NilLiteral : Term
{
	public override LuaValue Evaluate(LuaTable enviroment)
	{
		return LuaNil.Nil;
	}
}
