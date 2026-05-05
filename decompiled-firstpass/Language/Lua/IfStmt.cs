using System.Collections.Generic;

namespace Language.Lua;

public class IfStmt : Statement
{
	public Expr Condition;

	public Chunk ThenBlock;

	public List<ElseifBlock> ElseifBlocks = new List<ElseifBlock>();

	public Chunk ElseBlock;

	public override LuaValue Execute(LuaTable enviroment, out bool isBreak)
	{
		if (Condition.Evaluate(enviroment).GetBooleanValue())
		{
			return ThenBlock.Execute(enviroment, out isBreak);
		}
		foreach (ElseifBlock elseifBlock in ElseifBlocks)
		{
			if (elseifBlock.Condition.Evaluate(enviroment).GetBooleanValue())
			{
				return elseifBlock.ThenBlock.Execute(enviroment, out isBreak);
			}
		}
		if (ElseBlock != null)
		{
			return ElseBlock.Execute(enviroment, out isBreak);
		}
		isBreak = false;
		return null;
	}
}
