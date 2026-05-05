namespace Language.Lua;

public class BoolLiteral : Term
{
	public string Text;

	public override LuaValue Evaluate(LuaTable enviroment)
	{
		return LuaBoolean.From(bool.Parse(Text));
	}
}
