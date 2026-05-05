namespace Language.Lua;

public class LuaNil : LuaValue
{
	public static readonly LuaNil Nil = new LuaNil();

	public override object Value => null;

	private LuaNil()
	{
	}

	public override string GetTypeCode()
	{
		return "nil";
	}

	public override bool GetBooleanValue()
	{
		return false;
	}

	public override string ToString()
	{
		return "nil";
	}
}
