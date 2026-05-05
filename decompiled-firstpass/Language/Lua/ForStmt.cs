namespace Language.Lua;

public class ForStmt : Statement
{
	public string VarName;

	public Expr Start;

	public Expr End;

	public Expr Step;

	public Chunk Body;

	public override LuaValue Execute(LuaTable enviroment, out bool isBreak)
	{
		LuaNumber luaNumber = Start.Evaluate(enviroment) as LuaNumber;
		LuaNumber luaNumber2 = End.Evaluate(enviroment) as LuaNumber;
		double num = 1.0;
		if (Step != null)
		{
			num = (Step.Evaluate(enviroment) as LuaNumber).Number;
		}
		LuaTable luaTable = new LuaTable(enviroment);
		luaTable.SetNameValue(VarName, luaNumber);
		Body.Enviroment = luaTable;
		while ((num > 0.0 && luaNumber.Number <= luaNumber2.Number) || (num <= 0.0 && luaNumber.Number >= luaNumber2.Number))
		{
			LuaValue luaValue = Body.Execute(out isBreak);
			if (luaValue != null || isBreak)
			{
				isBreak = false;
				return luaValue;
			}
			luaNumber.Number += num;
		}
		isBreak = false;
		return null;
	}
}
