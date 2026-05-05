namespace Language.Lua;

public class LuaUserdata : LuaValue
{
	private object Object;

	public override object Value => Object;

	public LuaTable MetaTable { get; set; }

	public LuaUserdata(object obj)
	{
		Object = obj;
	}

	public LuaUserdata(object obj, LuaTable metatable)
	{
		Object = obj;
		MetaTable = metatable;
	}

	public override string GetTypeCode()
	{
		return "userdata";
	}

	public override string ToString()
	{
		return "userdata";
	}
}
