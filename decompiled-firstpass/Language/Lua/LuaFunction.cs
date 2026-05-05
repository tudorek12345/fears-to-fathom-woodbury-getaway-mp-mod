namespace Language.Lua;

public class LuaFunction : LuaValue
{
	public LuaFunc Function { get; set; }

	public override object Value => Function;

	public LuaFunction(LuaFunc function)
	{
		Function = function;
	}

	public override string GetTypeCode()
	{
		return "function";
	}

	public LuaValue Invoke(LuaValue[] args)
	{
		return Function(args);
	}
}
