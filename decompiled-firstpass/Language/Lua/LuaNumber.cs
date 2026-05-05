using System.Globalization;

namespace Language.Lua;

public class LuaNumber : LuaValue
{
	public double Number { get; set; }

	public override object Value => Number;

	public LuaNumber(double number)
	{
		Number = number;
	}

	public override string GetTypeCode()
	{
		return "number";
	}

	public override string ToString()
	{
		return Number.ToString(CultureInfo.InvariantCulture);
	}
}
