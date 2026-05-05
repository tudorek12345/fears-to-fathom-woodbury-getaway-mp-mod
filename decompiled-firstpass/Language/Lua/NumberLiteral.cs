using System.Globalization;

namespace Language.Lua;

public class NumberLiteral : Term
{
	public string HexicalText;

	public string Text;

	public override LuaValue Evaluate(LuaTable enviroment)
	{
		double number = ((!string.IsNullOrEmpty(HexicalText)) ? ((double)int.Parse(HexicalText, NumberStyles.HexNumber)) : double.Parse(Text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture));
		return new LuaNumber(number);
	}
}
