namespace Language.Lua;

public class StringLiteral : Term
{
	public string Text;

	public override LuaValue Evaluate(LuaTable enviroment)
	{
		return new LuaString(Text);
	}
}
