namespace Language.Lua;

public class LuaString : LuaValue
{
	public static readonly LuaString Empty = new LuaString(string.Empty);

	public string Text { get; set; }

	public override object Value => Text;

	public LuaString(string text)
	{
		Text = text;
	}

	public override string GetTypeCode()
	{
		return "string";
	}

	public override string ToString()
	{
		return Text;
	}
}
