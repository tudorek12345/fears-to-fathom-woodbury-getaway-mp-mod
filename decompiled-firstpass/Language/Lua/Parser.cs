using System.Collections.Generic;
using System.Text;

namespace Language.Lua;

public class Parser
{
	private int position;

	private ParserInput<char> Input;

	public List<Tuple<int, string>> Errors = new List<Tuple<int, string>>();

	private Stack<int> ErrorStack = new Stack<int>();

	private Dictionary<Tuple<int, string>, Tuple<object, bool, int>> ParsingResults = new Dictionary<Tuple<int, string>, Tuple<object, bool, int>>();

	public int Position
	{
		get
		{
			return position;
		}
		set
		{
			position = value;
		}
	}

	public Chunk ParseChunk(ParserInput<char> input, out bool success)
	{
		SetInput(input);
		Chunk result = ParseChunk(out success);
		if (Position < input.Length)
		{
			success = false;
			Error("Failed to parse remaining input.");
		}
		return result;
	}

	private Chunk ParseChunk(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "Chunk");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as Chunk;
		}
		Chunk chunk = new Chunk();
		ParseSpOpt(out success);
		do
		{
			if (PeekTerminalString("end") || PeekTerminalString("else") || PeekTerminalString("elseif"))
			{
				return chunk;
			}
			bool success2 = true;
			int num = 0;
			while (success2 && num < 10)
			{
				MatchTerminal(';', out success2);
				if (success2)
				{
					bool success3 = true;
					int num2 = 0;
					while (success3 && num2 < 10)
					{
						MatchTerminal(' ', out success3);
					}
				}
				num++;
			}
			Statement item = ParseStatement(out success);
			if (success)
			{
				chunk.Statements.Add(item);
				MatchTerminal(';', out success);
				if (success)
				{
					ParseSpOpt(out success);
				}
				success = true;
			}
		}
		while (success);
		success = true;
		ParsingResults[key] = new Tuple<object, bool, int>(chunk, success, position);
		return chunk;
	}

	private Statement ParseStatement(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "Statement");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as Statement;
		}
		int count = Errors.Count;
		Statement statement = null;
		statement = ParseAssignment(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(statement, success, position);
			return statement;
		}
		statement = ParseFunction(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(statement, success, position);
			return statement;
		}
		statement = ParseLocalVar(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(statement, success, position);
			return statement;
		}
		statement = ParseLocalFunc(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(statement, success, position);
			return statement;
		}
		statement = ParseReturnStmt(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(statement, success, position);
			return statement;
		}
		statement = ParseBreakStmt(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(statement, success, position);
			return statement;
		}
		statement = ParseDoStmt(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(statement, success, position);
			return statement;
		}
		statement = ParseIfStmt(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(statement, success, position);
			return statement;
		}
		statement = ParseForStmt(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(statement, success, position);
			return statement;
		}
		statement = ParseForInStmt(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(statement, success, position);
			return statement;
		}
		statement = ParseWhileStmt(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(statement, success, position);
			return statement;
		}
		statement = ParseRepeatStmt(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(statement, success, position);
			return statement;
		}
		statement = ParseExprStmt(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(statement, success, position);
			return statement;
		}
		return statement;
	}

	private Assignment ParseAssignment(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "Assignment");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as Assignment;
		}
		int count = Errors.Count;
		Assignment assignment = new Assignment();
		int num = position;
		assignment.VarList = ParseVarList(out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(assignment, success, position);
			return assignment;
		}
		ParseSpOpt(out success);
		MatchTerminal('=', out success);
		if (!success)
		{
			Error("Failed to parse '=' of Assignment.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(assignment, success, position);
			return assignment;
		}
		ParseSpOpt(out success);
		assignment.ExprList = ParseExprList(out success);
		if (!success)
		{
			Error("Failed to parse ExprList of Assignment.");
			position = num;
		}
		if (success)
		{
			ClearError(count);
		}
		ParsingResults[key] = new Tuple<object, bool, int>(assignment, success, position);
		return assignment;
	}

	private Function ParseFunction(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "Function");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as Function;
		}
		int count = Errors.Count;
		Function function = new Function();
		int num = position;
		MatchTerminalString("function", out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(function, success, position);
			return function;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of Function.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(function, success, position);
			return function;
		}
		function.Name = ParseFunctionName(out success);
		if (!success)
		{
			Error("Failed to parse Name of Function.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(function, success, position);
			return function;
		}
		ParseSpOpt(out success);
		function.Body = ParseFunctionBody(out success);
		if (!success)
		{
			Error("Failed to parse Body of Function.");
			position = num;
		}
		if (success)
		{
			ClearError(count);
		}
		ParsingResults[key] = new Tuple<object, bool, int>(function, success, position);
		return function;
	}

	private LocalVar ParseLocalVar(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "LocalVar");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as LocalVar;
		}
		LocalVar localVar = new LocalVar();
		int num = position;
		MatchTerminalString("local", out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(localVar, success, position);
			return localVar;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of LocalVar.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(localVar, success, position);
			return localVar;
		}
		localVar.NameList = ParseNameList(out success);
		if (!success)
		{
			Error("Failed to parse NameList of LocalVar.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(localVar, success, position);
			return localVar;
		}
		ParseSpOpt(out success);
		int num2 = position;
		MatchTerminal('=', out success);
		if (success)
		{
			ParseSpOpt(out success);
			localVar.ExprList = ParseExprList(out success);
			if (!success)
			{
				Error("Failed to parse ExprList of LocalVar.");
				position = num2;
			}
		}
		success = true;
		ParsingResults[key] = new Tuple<object, bool, int>(localVar, success, position);
		return localVar;
	}

	private LocalFunc ParseLocalFunc(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "LocalFunc");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as LocalFunc;
		}
		int count = Errors.Count;
		LocalFunc localFunc = new LocalFunc();
		int num = position;
		MatchTerminalString("local", out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(localFunc, success, position);
			return localFunc;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of LocalFunc.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(localFunc, success, position);
			return localFunc;
		}
		MatchTerminalString("function", out success);
		if (!success)
		{
			Error("Failed to parse 'function' of LocalFunc.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(localFunc, success, position);
			return localFunc;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of LocalFunc.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(localFunc, success, position);
			return localFunc;
		}
		localFunc.Name = ParseName(out success);
		if (!success)
		{
			Error("Failed to parse Name of LocalFunc.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(localFunc, success, position);
			return localFunc;
		}
		ParseSpOpt(out success);
		localFunc.Body = ParseFunctionBody(out success);
		if (!success)
		{
			Error("Failed to parse Body of LocalFunc.");
			position = num;
		}
		if (success)
		{
			ClearError(count);
		}
		ParsingResults[key] = new Tuple<object, bool, int>(localFunc, success, position);
		return localFunc;
	}

	private ExprStmt ParseExprStmt(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "ExprStmt");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as ExprStmt;
		}
		int count = Errors.Count;
		ExprStmt exprStmt = new ExprStmt();
		exprStmt.Expr = ParseExpr(out success);
		if (success)
		{
			ClearError(count);
		}
		else
		{
			Error("Failed to parse Expr of ExprStmt.");
		}
		ParsingResults[key] = new Tuple<object, bool, int>(exprStmt, success, position);
		return exprStmt;
	}

	private ReturnStmt ParseReturnStmt(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "ReturnStmt");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as ReturnStmt;
		}
		ReturnStmt returnStmt = new ReturnStmt();
		int num = position;
		MatchTerminalString("return", out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(returnStmt, success, position);
			return returnStmt;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of ReturnStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(returnStmt, success, position);
			return returnStmt;
		}
		returnStmt.ExprList = ParseExprList(out success);
		success = true;
		ParsingResults[key] = new Tuple<object, bool, int>(returnStmt, success, position);
		return returnStmt;
	}

	private BreakStmt ParseBreakStmt(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "BreakStmt");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as BreakStmt;
		}
		int count = Errors.Count;
		BreakStmt breakStmt = new BreakStmt();
		int num = position;
		MatchTerminalString("break", out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(breakStmt, success, position);
			return breakStmt;
		}
		ParseSpOpt(out success);
		if (success)
		{
			ClearError(count);
		}
		ParsingResults[key] = new Tuple<object, bool, int>(breakStmt, success, position);
		return breakStmt;
	}

	private DoStmt ParseDoStmt(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "DoStmt");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as DoStmt;
		}
		int count = Errors.Count;
		DoStmt doStmt = new DoStmt();
		int num = position;
		MatchTerminalString("do", out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(doStmt, success, position);
			return doStmt;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of DoStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(doStmt, success, position);
			return doStmt;
		}
		doStmt.Body = ParseChunk(out success);
		if (!success)
		{
			Error("Failed to parse Body of DoStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(doStmt, success, position);
			return doStmt;
		}
		MatchTerminalString("end", out success);
		if (!success)
		{
			Error("Failed to parse 'end' of DoStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(doStmt, success, position);
			return doStmt;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of DoStmt.");
			position = num;
		}
		if (success)
		{
			ClearError(count);
		}
		ParsingResults[key] = new Tuple<object, bool, int>(doStmt, success, position);
		return doStmt;
	}

	private IfStmt ParseIfStmt(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "IfStmt");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as IfStmt;
		}
		int count = Errors.Count;
		IfStmt ifStmt = new IfStmt();
		int num = position;
		MatchTerminalString("if", out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(ifStmt, success, position);
			return ifStmt;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of IfStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(ifStmt, success, position);
			return ifStmt;
		}
		ifStmt.Condition = ParseExpr(out success);
		if (!success)
		{
			Error("Failed to parse Condition of IfStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(ifStmt, success, position);
			return ifStmt;
		}
		MatchTerminalString("then", out success);
		if (!success)
		{
			Error("Failed to parse 'then' of IfStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(ifStmt, success, position);
			return ifStmt;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of IfStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(ifStmt, success, position);
			return ifStmt;
		}
		ifStmt.ThenBlock = ParseChunk(out success);
		if (!success)
		{
			Error("Failed to parse ThenBlock of IfStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(ifStmt, success, position);
			return ifStmt;
		}
		while (true)
		{
			ElseifBlock item = ParseElseifBlock(out success);
			if (!success)
			{
				break;
			}
			ifStmt.ElseifBlocks.Add(item);
		}
		success = true;
		int num2 = position;
		MatchTerminalString("else", out success);
		if (success)
		{
			ParseSpReq(out success);
			if (!success)
			{
				Error("Failed to parse SpReq of IfStmt.");
				position = num2;
			}
			else
			{
				ifStmt.ElseBlock = ParseChunk(out success);
				if (!success)
				{
					Error("Failed to parse ElseBlock of IfStmt.");
					position = num2;
				}
			}
		}
		success = true;
		MatchTerminalString("end", out success);
		if (!success)
		{
			Error("Failed to parse 'end' of IfStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(ifStmt, success, position);
			return ifStmt;
		}
		bool success2 = false;
		MatchTerminalString(";", out success2);
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of IfStmt.");
			position = num;
		}
		if (success)
		{
			ClearError(count);
		}
		ParsingResults[key] = new Tuple<object, bool, int>(ifStmt, success, position);
		return ifStmt;
	}

	private ElseifBlock ParseElseifBlock(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "ElseifBlock");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as ElseifBlock;
		}
		int count = Errors.Count;
		ElseifBlock elseifBlock = new ElseifBlock();
		int num = position;
		MatchTerminalString("elseif", out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(elseifBlock, success, position);
			return elseifBlock;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of ElseifBlock.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(elseifBlock, success, position);
			return elseifBlock;
		}
		elseifBlock.Condition = ParseExpr(out success);
		if (!success)
		{
			Error("Failed to parse Condition of ElseifBlock.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(elseifBlock, success, position);
			return elseifBlock;
		}
		MatchTerminalString("then", out success);
		if (!success)
		{
			Error("Failed to parse 'then' of ElseifBlock.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(elseifBlock, success, position);
			return elseifBlock;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of ElseifBlock.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(elseifBlock, success, position);
			return elseifBlock;
		}
		elseifBlock.ThenBlock = ParseChunk(out success);
		if (!success)
		{
			Error("Failed to parse ThenBlock of ElseifBlock.");
			position = num;
		}
		if (success)
		{
			ClearError(count);
		}
		ParsingResults[key] = new Tuple<object, bool, int>(elseifBlock, success, position);
		return elseifBlock;
	}

	private ForStmt ParseForStmt(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "ForStmt");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as ForStmt;
		}
		int count = Errors.Count;
		ForStmt forStmt = new ForStmt();
		int num = position;
		MatchTerminalString("for", out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(forStmt, success, position);
			return forStmt;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of ForStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(forStmt, success, position);
			return forStmt;
		}
		forStmt.VarName = ParseName(out success);
		if (!success)
		{
			Error("Failed to parse VarName of ForStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(forStmt, success, position);
			return forStmt;
		}
		ParseSpOpt(out success);
		MatchTerminal('=', out success);
		if (!success)
		{
			Error("Failed to parse '=' of ForStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(forStmt, success, position);
			return forStmt;
		}
		ParseSpOpt(out success);
		forStmt.Start = ParseExpr(out success);
		if (!success)
		{
			Error("Failed to parse Start of ForStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(forStmt, success, position);
			return forStmt;
		}
		MatchTerminal(',', out success);
		if (!success)
		{
			Error("Failed to parse ',' of ForStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(forStmt, success, position);
			return forStmt;
		}
		ParseSpOpt(out success);
		forStmt.End = ParseExpr(out success);
		if (!success)
		{
			Error("Failed to parse End of ForStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(forStmt, success, position);
			return forStmt;
		}
		int num2 = position;
		MatchTerminal(',', out success);
		if (success)
		{
			ParseSpOpt(out success);
			forStmt.Step = ParseExpr(out success);
			if (!success)
			{
				Error("Failed to parse Step of ForStmt.");
				position = num2;
			}
		}
		success = true;
		MatchTerminalString("do", out success);
		if (!success)
		{
			Error("Failed to parse 'do' of ForStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(forStmt, success, position);
			return forStmt;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of ForStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(forStmt, success, position);
			return forStmt;
		}
		forStmt.Body = ParseChunk(out success);
		if (!success)
		{
			Error("Failed to parse Body of ForStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(forStmt, success, position);
			return forStmt;
		}
		MatchTerminalString("end", out success);
		if (!success)
		{
			Error("Failed to parse 'end' of ForStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(forStmt, success, position);
			return forStmt;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of ForStmt.");
			position = num;
		}
		if (success)
		{
			ClearError(count);
		}
		ParsingResults[key] = new Tuple<object, bool, int>(forStmt, success, position);
		return forStmt;
	}

	private ForInStmt ParseForInStmt(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "ForInStmt");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as ForInStmt;
		}
		int count = Errors.Count;
		ForInStmt forInStmt = new ForInStmt();
		int num = position;
		MatchTerminalString("for", out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(forInStmt, success, position);
			return forInStmt;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of ForInStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(forInStmt, success, position);
			return forInStmt;
		}
		forInStmt.NameList = ParseNameList(out success);
		if (!success)
		{
			Error("Failed to parse NameList of ForInStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(forInStmt, success, position);
			return forInStmt;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of ForInStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(forInStmt, success, position);
			return forInStmt;
		}
		MatchTerminalString("in", out success);
		if (!success)
		{
			Error("Failed to parse 'in' of ForInStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(forInStmt, success, position);
			return forInStmt;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of ForInStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(forInStmt, success, position);
			return forInStmt;
		}
		forInStmt.ExprList = ParseExprList(out success);
		if (!success)
		{
			Error("Failed to parse ExprList of ForInStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(forInStmt, success, position);
			return forInStmt;
		}
		MatchTerminalString("do", out success);
		if (!success)
		{
			Error("Failed to parse 'do' of ForInStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(forInStmt, success, position);
			return forInStmt;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of ForInStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(forInStmt, success, position);
			return forInStmt;
		}
		forInStmt.Body = ParseChunk(out success);
		if (!success)
		{
			Error("Failed to parse Body of ForInStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(forInStmt, success, position);
			return forInStmt;
		}
		MatchTerminalString("end", out success);
		if (!success)
		{
			Error("Failed to parse 'end' of ForInStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(forInStmt, success, position);
			return forInStmt;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of ForInStmt.");
			position = num;
		}
		if (success)
		{
			ClearError(count);
		}
		ParsingResults[key] = new Tuple<object, bool, int>(forInStmt, success, position);
		return forInStmt;
	}

	private WhileStmt ParseWhileStmt(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "WhileStmt");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as WhileStmt;
		}
		int count = Errors.Count;
		WhileStmt whileStmt = new WhileStmt();
		int num = position;
		MatchTerminalString("while", out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(whileStmt, success, position);
			return whileStmt;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of WhileStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(whileStmt, success, position);
			return whileStmt;
		}
		whileStmt.Condition = ParseExpr(out success);
		if (!success)
		{
			Error("Failed to parse Condition of WhileStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(whileStmt, success, position);
			return whileStmt;
		}
		MatchTerminalString("do", out success);
		if (!success)
		{
			Error("Failed to parse 'do' of WhileStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(whileStmt, success, position);
			return whileStmt;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of WhileStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(whileStmt, success, position);
			return whileStmt;
		}
		whileStmt.Body = ParseChunk(out success);
		if (!success)
		{
			Error("Failed to parse Body of WhileStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(whileStmt, success, position);
			return whileStmt;
		}
		MatchTerminalString("end", out success);
		if (!success)
		{
			Error("Failed to parse 'end' of WhileStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(whileStmt, success, position);
			return whileStmt;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of WhileStmt.");
			position = num;
		}
		if (success)
		{
			ClearError(count);
		}
		ParsingResults[key] = new Tuple<object, bool, int>(whileStmt, success, position);
		return whileStmt;
	}

	private RepeatStmt ParseRepeatStmt(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "RepeatStmt");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as RepeatStmt;
		}
		int count = Errors.Count;
		RepeatStmt repeatStmt = new RepeatStmt();
		int num = position;
		MatchTerminalString("repeat", out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(repeatStmt, success, position);
			return repeatStmt;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of RepeatStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(repeatStmt, success, position);
			return repeatStmt;
		}
		repeatStmt.Body = ParseChunk(out success);
		if (!success)
		{
			Error("Failed to parse Body of RepeatStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(repeatStmt, success, position);
			return repeatStmt;
		}
		MatchTerminalString("until", out success);
		if (!success)
		{
			Error("Failed to parse 'until' of RepeatStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(repeatStmt, success, position);
			return repeatStmt;
		}
		ParseSpReq(out success);
		if (!success)
		{
			Error("Failed to parse SpReq of RepeatStmt.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(repeatStmt, success, position);
			return repeatStmt;
		}
		repeatStmt.Condition = ParseExpr(out success);
		if (!success)
		{
			Error("Failed to parse Condition of RepeatStmt.");
			position = num;
		}
		if (success)
		{
			ClearError(count);
		}
		ParsingResults[key] = new Tuple<object, bool, int>(repeatStmt, success, position);
		return repeatStmt;
	}

	private List<Var> ParseVarList(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "VarList");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as List<Var>;
		}
		List<Var> list = new List<Var>();
		int num = position;
		Var item = ParseVar(out success);
		if (success)
		{
			list.Add(item);
			do
			{
				int num2 = position;
				ParseSpOpt(out success);
				MatchTerminal(',', out success);
				if (!success)
				{
					Error("Failed to parse ',' of VarList.");
					position = num2;
					continue;
				}
				ParseSpOpt(out success);
				item = ParseVar(out success);
				if (success)
				{
					list.Add(item);
					continue;
				}
				Error("Failed to parse Var of VarList.");
				position = num2;
			}
			while (success);
			success = true;
			ParsingResults[key] = new Tuple<object, bool, int>(list, success, position);
			return list;
		}
		position = num;
		ParsingResults[key] = new Tuple<object, bool, int>(list, success, position);
		return list;
	}

	private List<Expr> ParseExprList(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "ExprList");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as List<Expr>;
		}
		List<Expr> list = new List<Expr>();
		int num = position;
		Expr item = ParseExpr(out success);
		if (success)
		{
			list.Add(item);
			do
			{
				int num2 = position;
				ParseSpOpt(out success);
				MatchTerminal(',', out success);
				if (!success)
				{
					Error("Failed to parse ',' of ExprList.");
					position = num2;
					continue;
				}
				ParseSpOpt(out success);
				item = ParseExpr(out success);
				if (success)
				{
					list.Add(item);
					continue;
				}
				Error("Failed to parse Expr of ExprList.");
				position = num2;
			}
			while (success);
			success = true;
			ParsingResults[key] = new Tuple<object, bool, int>(list, success, position);
			return list;
		}
		position = num;
		ParsingResults[key] = new Tuple<object, bool, int>(list, success, position);
		return list;
	}

	private Expr ParseExpr(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "Expr");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as Expr;
		}
		int count = Errors.Count;
		Expr expr = null;
		expr = ParseOperatorExpr(out success);
		if (success)
		{
			return expr.Simplify();
		}
		expr = ParseTerm(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(expr, success, position);
			return expr;
		}
		return expr;
	}

	private Term ParseTerm(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "Term");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as Term;
		}
		int count = Errors.Count;
		Term term = null;
		term = ParseNilLiteral(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(term, success, position);
			return term;
		}
		term = ParseBoolLiteral(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(term, success, position);
			return term;
		}
		term = ParseNumberLiteral(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(term, success, position);
			return term;
		}
		term = ParseStringLiteral(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(term, success, position);
			return term;
		}
		term = ParseFunctionValue(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(term, success, position);
			return term;
		}
		term = ParseTableConstructor(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(term, success, position);
			return term;
		}
		term = ParseVariableArg(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(term, success, position);
			return term;
		}
		term = ParsePrimaryExpr(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(term, success, position);
			return term;
		}
		return term;
	}

	private NilLiteral ParseNilLiteral(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "NilLiteral");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as NilLiteral;
		}
		int count = Errors.Count;
		NilLiteral nilLiteral = new NilLiteral();
		MatchTerminalString("nil", out success);
		if (success)
		{
			ClearError(count);
		}
		else
		{
			Error("Failed to parse 'nil' of NilLiteral.");
		}
		ParsingResults[key] = new Tuple<object, bool, int>(nilLiteral, success, position);
		return nilLiteral;
	}

	private BoolLiteral ParseBoolLiteral(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "BoolLiteral");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as BoolLiteral;
		}
		int count = Errors.Count;
		ErrorStack.Push(count);
		count = Errors.Count;
		BoolLiteral boolLiteral = new BoolLiteral();
		boolLiteral.Text = MatchTerminalString("true", out success);
		if (success)
		{
			ClearError(count);
		}
		else
		{
			boolLiteral.Text = MatchTerminalString("false", out success);
			if (success)
			{
				ClearError(count);
			}
		}
		count = ErrorStack.Pop();
		if (success)
		{
			ClearError(count);
		}
		else
		{
			Error("Failed to parse Text of BoolLiteral.");
		}
		ParsingResults[key] = new Tuple<object, bool, int>(boolLiteral, success, position);
		return boolLiteral;
	}

	private NumberLiteral ParseNumberLiteral(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "NumberLiteral");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as NumberLiteral;
		}
		int count = Errors.Count;
		NumberLiteral numberLiteral = new NumberLiteral();
		numberLiteral.HexicalText = ParseHexicalNumber(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(numberLiteral, success, position);
			return numberLiteral;
		}
		numberLiteral.Text = ParseFloatNumber(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(numberLiteral, success, position);
			return numberLiteral;
		}
		return numberLiteral;
	}

	private StringLiteral ParseStringLiteral(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "StringLiteral");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as StringLiteral;
		}
		int count = Errors.Count;
		StringLiteral stringLiteral = new StringLiteral();
		int num = position;
		MatchTerminal('"', out success);
		if (success)
		{
			stringLiteral.Text = ParseDoubleQuotedText(out success);
			MatchTerminal('"', out success);
			if (!success)
			{
				Error("Failed to parse '\\\"' of StringLiteral.");
				position = num;
			}
		}
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(stringLiteral, success, position);
			return stringLiteral;
		}
		int num2 = position;
		MatchTerminal('\'', out success);
		if (success)
		{
			stringLiteral.Text = ParseSingleQuotedText(out success);
			MatchTerminal('\'', out success);
			if (!success)
			{
				Error("Failed to parse ''' of StringLiteral.");
				position = num2;
			}
		}
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(stringLiteral, success, position);
			return stringLiteral;
		}
		stringLiteral.Text = ParseLongString(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(stringLiteral, success, position);
			return stringLiteral;
		}
		return stringLiteral;
	}

	private VariableArg ParseVariableArg(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "VariableArg");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as VariableArg;
		}
		int count = Errors.Count;
		VariableArg variableArg = new VariableArg();
		variableArg.Name = MatchTerminalString("...", out success);
		if (success)
		{
			ClearError(count);
		}
		else
		{
			Error("Failed to parse Name of VariableArg.");
		}
		ParsingResults[key] = new Tuple<object, bool, int>(variableArg, success, position);
		return variableArg;
	}

	private FunctionValue ParseFunctionValue(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "FunctionValue");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as FunctionValue;
		}
		int count = Errors.Count;
		FunctionValue functionValue = new FunctionValue();
		int num = position;
		MatchTerminalString("function", out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(functionValue, success, position);
			return functionValue;
		}
		ParseSpOpt(out success);
		functionValue.Body = ParseFunctionBody(out success);
		if (!success)
		{
			Error("Failed to parse Body of FunctionValue.");
			position = num;
		}
		if (success)
		{
			ClearError(count);
		}
		ParsingResults[key] = new Tuple<object, bool, int>(functionValue, success, position);
		return functionValue;
	}

	private FunctionBody ParseFunctionBody(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "FunctionBody");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as FunctionBody;
		}
		int count = Errors.Count;
		FunctionBody functionBody = new FunctionBody();
		int num = position;
		MatchTerminal('(', out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(functionBody, success, position);
			return functionBody;
		}
		ParseSpOpt(out success);
		functionBody.ParamList = ParseParamList(out success);
		if (success)
		{
			ParseSpOpt(out success);
		}
		success = true;
		MatchTerminal(')', out success);
		if (!success)
		{
			Error("Failed to parse ')' of FunctionBody.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(functionBody, success, position);
			return functionBody;
		}
		functionBody.Chunk = ParseChunk(out success);
		if (!success)
		{
			Error("Failed to parse Chunk of FunctionBody.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(functionBody, success, position);
			return functionBody;
		}
		MatchTerminalString("end", out success);
		if (!success)
		{
			Error("Failed to parse 'end' of FunctionBody.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(functionBody, success, position);
			return functionBody;
		}
		ParseSpOpt(out success);
		if (success)
		{
			ClearError(count);
		}
		ParsingResults[key] = new Tuple<object, bool, int>(functionBody, success, position);
		return functionBody;
	}

	private Access ParseAccess(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "Access");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as Access;
		}
		int count = Errors.Count;
		Access access = null;
		access = ParseNameAccess(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(access, success, position);
			return access;
		}
		access = ParseKeyAccess(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(access, success, position);
			return access;
		}
		access = ParseMethodCall(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(access, success, position);
			return access;
		}
		access = ParseFunctionCall(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(access, success, position);
			return access;
		}
		return access;
	}

	private BaseExpr ParseBaseExpr(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "BaseExpr");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as BaseExpr;
		}
		int count = Errors.Count;
		BaseExpr baseExpr = null;
		baseExpr = ParseGroupExpr(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(baseExpr, success, position);
			return baseExpr;
		}
		baseExpr = ParseVarName(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(baseExpr, success, position);
			return baseExpr;
		}
		return baseExpr;
	}

	private KeyAccess ParseKeyAccess(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "KeyAccess");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as KeyAccess;
		}
		int count = Errors.Count;
		KeyAccess keyAccess = new KeyAccess();
		int num = position;
		MatchTerminal('[', out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(keyAccess, success, position);
			return keyAccess;
		}
		ParseSpOpt(out success);
		keyAccess.Key = ParseExpr(out success);
		if (!success)
		{
			Error("Failed to parse Key of KeyAccess.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(keyAccess, success, position);
			return keyAccess;
		}
		MatchTerminal(']', out success);
		if (!success)
		{
			Error("Failed to parse ']' of KeyAccess.");
			position = num;
		}
		if (success)
		{
			ClearError(count);
		}
		ParsingResults[key] = new Tuple<object, bool, int>(keyAccess, success, position);
		return keyAccess;
	}

	private NameAccess ParseNameAccess(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "NameAccess");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as NameAccess;
		}
		int count = Errors.Count;
		NameAccess nameAccess = new NameAccess();
		int num = position;
		MatchTerminal('.', out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(nameAccess, success, position);
			return nameAccess;
		}
		ParseSpOpt(out success);
		nameAccess.Name = ParseName(out success);
		if (!success)
		{
			Error("Failed to parse Name of NameAccess.");
			position = num;
		}
		if (success)
		{
			ClearError(count);
		}
		ParsingResults[key] = new Tuple<object, bool, int>(nameAccess, success, position);
		return nameAccess;
	}

	private MethodCall ParseMethodCall(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "MethodCall");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as MethodCall;
		}
		int count = Errors.Count;
		MethodCall methodCall = new MethodCall();
		int num = position;
		MatchTerminal(':', out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(methodCall, success, position);
			return methodCall;
		}
		ParseSpOpt(out success);
		methodCall.Method = ParseName(out success);
		if (!success)
		{
			Error("Failed to parse Method of MethodCall.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(methodCall, success, position);
			return methodCall;
		}
		ParseSpOpt(out success);
		methodCall.Args = ParseArgs(out success);
		if (!success)
		{
			Error("Failed to parse Args of MethodCall.");
			position = num;
		}
		if (success)
		{
			ClearError(count);
		}
		ParsingResults[key] = new Tuple<object, bool, int>(methodCall, success, position);
		return methodCall;
	}

	private FunctionCall ParseFunctionCall(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "FunctionCall");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as FunctionCall;
		}
		int count = Errors.Count;
		FunctionCall functionCall = new FunctionCall();
		functionCall.Args = ParseArgs(out success);
		if (success)
		{
			ClearError(count);
		}
		else
		{
			Error("Failed to parse Args of FunctionCall.");
		}
		ParsingResults[key] = new Tuple<object, bool, int>(functionCall, success, position);
		return functionCall;
	}

	private Var ParseVar(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "Var");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as Var;
		}
		int item = Errors.Count;
		Var var = new Var();
		int num = position;
		var.Base = ParseBaseExpr(out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(var, success, position);
			return var;
		}
		do
		{
			ErrorStack.Push(item);
			item = Errors.Count;
			int num2 = position;
			ParseSpOpt(out success);
			NameAccess item2 = ParseNameAccess(out success);
			if (success)
			{
				var.Accesses.Add(item2);
			}
			else
			{
				Error("Failed to parse NameAccess of Var.");
				position = num2;
			}
			if (success)
			{
				ClearError(item);
			}
			else
			{
				int num3 = position;
				ParseSpOpt(out success);
				KeyAccess item3 = ParseKeyAccess(out success);
				if (success)
				{
					var.Accesses.Add(item3);
				}
				else
				{
					Error("Failed to parse KeyAccess of Var.");
					position = num3;
				}
				if (success)
				{
					ClearError(item);
				}
			}
			item = ErrorStack.Pop();
		}
		while (success);
		success = true;
		ParsingResults[key] = new Tuple<object, bool, int>(var, success, position);
		return var;
	}

	private PrimaryExpr ParsePrimaryExpr(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "PrimaryExpr");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as PrimaryExpr;
		}
		PrimaryExpr primaryExpr = new PrimaryExpr();
		int num = position;
		primaryExpr.Base = ParseBaseExpr(out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(primaryExpr, success, position);
			return primaryExpr;
		}
		do
		{
			int num2 = position;
			ParseSpOpt(out success);
			Access item = ParseAccess(out success);
			if (success)
			{
				primaryExpr.Accesses.Add(item);
				continue;
			}
			Error("Failed to parse Access of PrimaryExpr.");
			position = num2;
		}
		while (success);
		success = true;
		ParsingResults[key] = new Tuple<object, bool, int>(primaryExpr, success, position);
		return primaryExpr;
	}

	private VarName ParseVarName(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "VarName");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as VarName;
		}
		int count = Errors.Count;
		VarName varName = new VarName();
		varName.Name = ParseName(out success);
		if (success)
		{
			ClearError(count);
		}
		else
		{
			Error("Failed to parse Name of VarName.");
		}
		ParsingResults[key] = new Tuple<object, bool, int>(varName, success, position);
		return varName;
	}

	private FunctionName ParseFunctionName(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "FunctionName");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as FunctionName;
		}
		FunctionName functionName = new FunctionName();
		int num = position;
		functionName.FullName = ParseFullName(out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(functionName, success, position);
			return functionName;
		}
		int num2 = position;
		ParseSpOpt(out success);
		MatchTerminal(':', out success);
		if (!success)
		{
			Error("Failed to parse ':' of FunctionName.");
			position = num2;
		}
		else
		{
			ParseSpOpt(out success);
			functionName.MethodName = ParseName(out success);
			if (!success)
			{
				Error("Failed to parse MethodName of FunctionName.");
				position = num2;
			}
		}
		success = true;
		ParsingResults[key] = new Tuple<object, bool, int>(functionName, success, position);
		return functionName;
	}

	private GroupExpr ParseGroupExpr(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "GroupExpr");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as GroupExpr;
		}
		int count = Errors.Count;
		GroupExpr groupExpr = new GroupExpr();
		int num = position;
		MatchTerminal('(', out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(groupExpr, success, position);
			return groupExpr;
		}
		ParseSpOpt(out success);
		groupExpr.Expr = ParseExpr(out success);
		if (!success)
		{
			Error("Failed to parse Expr of GroupExpr.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(groupExpr, success, position);
			return groupExpr;
		}
		MatchTerminal(')', out success);
		if (!success)
		{
			Error("Failed to parse ')' of GroupExpr.");
			position = num;
		}
		if (success)
		{
			ClearError(count);
		}
		ParsingResults[key] = new Tuple<object, bool, int>(groupExpr, success, position);
		return groupExpr;
	}

	private TableConstructor ParseTableConstructor(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "TableConstructor");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as TableConstructor;
		}
		int count = Errors.Count;
		TableConstructor tableConstructor = new TableConstructor();
		int num = position;
		MatchTerminal('{', out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(tableConstructor, success, position);
			return tableConstructor;
		}
		ParseSpOpt(out success);
		tableConstructor.FieldList = ParseFieldList(out success);
		success = true;
		MatchTerminal('}', out success);
		if (!success)
		{
			Error("Failed to parse '}' of TableConstructor.");
			position = num;
		}
		if (success)
		{
			ClearError(count);
		}
		ParsingResults[key] = new Tuple<object, bool, int>(tableConstructor, success, position);
		return tableConstructor;
	}

	private List<Field> ParseFieldList(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "FieldList");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as List<Field>;
		}
		List<Field> list = new List<Field>();
		int num = position;
		Field item = ParseField(out success);
		if (success)
		{
			list.Add(item);
			do
			{
				int num2 = position;
				ParseFieldSep(out success);
				if (success)
				{
					ParseSpOpt(out success);
					item = ParseField(out success);
					if (success)
					{
						list.Add(item);
						continue;
					}
					Error("Failed to parse Field of FieldList.");
					position = num2;
				}
			}
			while (success);
			success = true;
			ParseFieldSep(out success);
			if (success)
			{
				ParseSpOpt(out success);
			}
			success = true;
			ParsingResults[key] = new Tuple<object, bool, int>(list, success, position);
			return list;
		}
		position = num;
		ParsingResults[key] = new Tuple<object, bool, int>(list, success, position);
		return list;
	}

	private Field ParseField(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "Field");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as Field;
		}
		int count = Errors.Count;
		Field field = null;
		field = ParseKeyValue(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(field, success, position);
			return field;
		}
		field = ParseNameValue(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(field, success, position);
			return field;
		}
		field = ParseItemValue(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(field, success, position);
			return field;
		}
		return field;
	}

	private KeyValue ParseKeyValue(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "KeyValue");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as KeyValue;
		}
		int count = Errors.Count;
		KeyValue keyValue = new KeyValue();
		int num = position;
		MatchTerminal('[', out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(keyValue, success, position);
			return keyValue;
		}
		ParseSpOpt(out success);
		keyValue.Key = ParseExpr(out success);
		if (!success)
		{
			Error("Failed to parse Key of KeyValue.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(keyValue, success, position);
			return keyValue;
		}
		MatchTerminal(']', out success);
		if (!success)
		{
			Error("Failed to parse ']' of KeyValue.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(keyValue, success, position);
			return keyValue;
		}
		ParseSpOpt(out success);
		MatchTerminal('=', out success);
		if (!success)
		{
			Error("Failed to parse '=' of KeyValue.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(keyValue, success, position);
			return keyValue;
		}
		ParseSpOpt(out success);
		keyValue.Value = ParseExpr(out success);
		if (!success)
		{
			Error("Failed to parse Value of KeyValue.");
			position = num;
		}
		if (success)
		{
			ClearError(count);
		}
		ParsingResults[key] = new Tuple<object, bool, int>(keyValue, success, position);
		return keyValue;
	}

	private NameValue ParseNameValue(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "NameValue");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as NameValue;
		}
		int count = Errors.Count;
		NameValue nameValue = new NameValue();
		int num = position;
		nameValue.Name = ParseName(out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(nameValue, success, position);
			return nameValue;
		}
		ParseSpOpt(out success);
		MatchTerminal('=', out success);
		if (!success)
		{
			Error("Failed to parse '=' of NameValue.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(nameValue, success, position);
			return nameValue;
		}
		ParseSpOpt(out success);
		nameValue.Value = ParseExpr(out success);
		if (!success)
		{
			Error("Failed to parse Value of NameValue.");
			position = num;
		}
		if (success)
		{
			ClearError(count);
		}
		ParsingResults[key] = new Tuple<object, bool, int>(nameValue, success, position);
		return nameValue;
	}

	private ItemValue ParseItemValue(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "ItemValue");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as ItemValue;
		}
		int count = Errors.Count;
		ItemValue itemValue = new ItemValue();
		itemValue.Value = ParseExpr(out success);
		if (success)
		{
			ClearError(count);
		}
		else
		{
			Error("Failed to parse Value of ItemValue.");
		}
		ParsingResults[key] = new Tuple<object, bool, int>(itemValue, success, position);
		return itemValue;
	}

	private OperatorExpr ParseOperatorExpr(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "OperatorExpr");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as OperatorExpr;
		}
		OperatorExpr operatorExpr = new OperatorExpr();
		int num = position;
		string oper = ParseUnaryOperator(out success);
		if (success)
		{
			operatorExpr.Add(oper);
			ParseSpOpt(out success);
		}
		success = true;
		Term term = ParseTerm(out success);
		if (success)
		{
			operatorExpr.Add(term);
			ParseSpOpt(out success);
			do
			{
				int num2 = position;
				string oper2 = ParseBinaryOperator(out success);
				if (!success)
				{
					continue;
				}
				operatorExpr.Add(oper2);
				ParseSpOpt(out success);
				string text = ParseUnaryOperator(out success);
				if (success)
				{
					num2 = position;
					ParseSpOpt(out success);
				}
				else
				{
					text = null;
				}
				Term term2 = ParseTerm(out success);
				if (success)
				{
					if (text != null)
					{
						Operation term3 = new Operation(text, null, term2);
						operatorExpr.Add(term3);
					}
					else
					{
						operatorExpr.Add(term2);
					}
					ParseSpOpt(out success);
				}
				else
				{
					Error("Failed to parse nextTerm of OperatorExpr.");
					position = num2;
				}
			}
			while (success);
			success = true;
			ParsingResults[key] = new Tuple<object, bool, int>(operatorExpr, success, position);
			return operatorExpr;
		}
		Error("Failed to parse firstTerm of OperatorExpr.");
		position = num;
		ParsingResults[key] = new Tuple<object, bool, int>(operatorExpr, success, position);
		return operatorExpr;
	}

	private Args ParseArgs(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "Args");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as Args;
		}
		int count = Errors.Count;
		Args args = new Args();
		args.ArgList = ParseArgList(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(args, success, position);
			return args;
		}
		args.String = ParseStringLiteral(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(args, success, position);
			return args;
		}
		args.Table = ParseTableConstructor(out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(args, success, position);
			return args;
		}
		return args;
	}

	private List<Expr> ParseArgList(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "ArgList");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as List<Expr>;
		}
		int count = Errors.Count;
		List<Expr> list = new List<Expr>();
		int num = position;
		MatchTerminal('(', out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(list, success, position);
			return list;
		}
		ParseSpOpt(out success);
		list = ParseExprList(out success);
		if (success)
		{
			ParseSpOpt(out success);
		}
		success = true;
		MatchTerminal(')', out success);
		if (!success)
		{
			Error("Failed to parse ')' of ArgList.");
			position = num;
		}
		if (success)
		{
			ClearError(count);
		}
		ParsingResults[key] = new Tuple<object, bool, int>(list, success, position);
		return list;
	}

	private ParamList ParseParamList(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "ParamList");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as ParamList;
		}
		int count = Errors.Count;
		ParamList paramList = new ParamList();
		paramList.NameList = ParseNameList(out success);
		if (success)
		{
			int num = position;
			MatchTerminal(',', out success);
			if (success)
			{
				ParseSpOpt(out success);
				MatchTerminalString("...", out success);
				if (!success)
				{
					Error("Failed to parse '...' of ParamList.");
					position = num;
				}
			}
			paramList.HasVarArg = success;
			success = true;
		}
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(paramList, success, position);
			return paramList;
		}
		paramList.IsVarArg = MatchTerminalString("...", out success);
		if (success)
		{
			ClearError(count);
			ParsingResults[key] = new Tuple<object, bool, int>(paramList, success, position);
			return paramList;
		}
		return paramList;
	}

	private List<string> ParseFullName(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "FullName");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as List<string>;
		}
		List<string> list = new List<string>();
		int num = position;
		string item = ParseName(out success);
		if (success)
		{
			list.Add(item);
			do
			{
				int num2 = position;
				ParseSpOpt(out success);
				MatchTerminal('.', out success);
				if (!success)
				{
					Error("Failed to parse '.' of FullName.");
					position = num2;
					continue;
				}
				ParseSpOpt(out success);
				item = ParseName(out success);
				if (success)
				{
					list.Add(item);
					continue;
				}
				Error("Failed to parse Name of FullName.");
				position = num2;
			}
			while (success);
			success = true;
			ParsingResults[key] = new Tuple<object, bool, int>(list, success, position);
			return list;
		}
		position = num;
		ParsingResults[key] = new Tuple<object, bool, int>(list, success, position);
		return list;
	}

	private List<string> ParseNameList(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "NameList");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as List<string>;
		}
		List<string> list = new List<string>();
		int num = position;
		string item = ParseName(out success);
		if (success)
		{
			list.Add(item);
			do
			{
				int num2 = position;
				ParseSpOpt(out success);
				MatchTerminal(',', out success);
				if (!success)
				{
					Error("Failed to parse ',' of NameList.");
					position = num2;
					continue;
				}
				ParseSpOpt(out success);
				item = ParseName(out success);
				if (success)
				{
					list.Add(item);
					continue;
				}
				Error("Failed to parse Name of NameList.");
				position = num2;
			}
			while (success);
			success = true;
			ParsingResults[key] = new Tuple<object, bool, int>(list, success, position);
			return list;
		}
		position = num;
		ParsingResults[key] = new Tuple<object, bool, int>(list, success, position);
		return list;
	}

	private string ParseName(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "Name");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as string;
		}
		int item = Errors.Count;
		StringBuilder stringBuilder = new StringBuilder();
		int num = position;
		int num2 = position;
		ParseKeyword(out success);
		if (success)
		{
			ParseWordSep(out success);
		}
		position = num2;
		success = !success;
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
			return stringBuilder.ToString();
		}
		char value = ParseLetter(out success);
		if (success)
		{
			stringBuilder.Append(value);
			do
			{
				ErrorStack.Push(item);
				item = Errors.Count;
				value = ParseLetter(out success);
				if (success)
				{
					ClearError(item);
					stringBuilder.Append(value);
				}
				else
				{
					value = ParseDigit(out success);
					if (success)
					{
						ClearError(item);
						stringBuilder.Append(value);
					}
				}
				item = ErrorStack.Pop();
			}
			while (success);
			success = true;
			ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
			return stringBuilder.ToString();
		}
		Error("Failed to parse Letter of Name.");
		position = num;
		ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
		return stringBuilder.ToString();
	}

	private string ParseFloatNumber(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "FloatNumber");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as string;
		}
		int count = Errors.Count;
		StringBuilder stringBuilder = new StringBuilder();
		int num = position;
		char value = MatchTerminal('-', out success);
		if (success)
		{
			stringBuilder.Append(value);
		}
		success = true;
		int num2 = 0;
		while (true)
		{
			value = ParseDigit(out success);
			if (!success)
			{
				break;
			}
			stringBuilder.Append(value);
			num2++;
		}
		if (num2 > 0)
		{
			success = true;
		}
		if (!success)
		{
			Error("Failed to parse (Digit)+ of FloatNumber.");
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
			return stringBuilder.ToString();
		}
		int num3 = position;
		value = MatchTerminal('.', out success);
		if (success)
		{
			stringBuilder.Append(value);
			num2 = 0;
			while (true)
			{
				value = ParseDigit(out success);
				if (!success)
				{
					break;
				}
				stringBuilder.Append(value);
				num2++;
			}
			if (num2 > 0)
			{
				success = true;
			}
			if (!success)
			{
				Error("Failed to parse (Digit)+ of FloatNumber.");
				position = num3;
			}
		}
		success = true;
		ErrorStack.Push(count);
		count = Errors.Count;
		int num4 = position;
		value = MatchTerminal('e', out success);
		if (success)
		{
			ClearError(count);
			stringBuilder.Append(value);
		}
		else
		{
			value = MatchTerminal('E', out success);
			if (success)
			{
				ClearError(count);
				stringBuilder.Append(value);
			}
		}
		count = ErrorStack.Pop();
		if (success)
		{
			value = MatchTerminal('-', out success);
			if (!success)
			{
				value = MatchTerminal('+', out success);
			}
			if (success)
			{
				stringBuilder.Append(value);
			}
			success = true;
			num2 = 0;
			while (true)
			{
				value = ParseDigit(out success);
				if (!success)
				{
					break;
				}
				stringBuilder.Append(value);
				num2++;
			}
			if (num2 > 0)
			{
				success = true;
			}
			if (!success)
			{
				Error("Failed to parse (Digit)+ of FloatNumber.");
				position = num4;
			}
		}
		success = true;
		ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
		return stringBuilder.ToString();
	}

	private string ParseHexicalNumber(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "HexicalNumber");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as string;
		}
		int count = Errors.Count;
		StringBuilder stringBuilder = new StringBuilder();
		int num = position;
		MatchTerminalString("0x", out success);
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
			return stringBuilder.ToString();
		}
		int num2 = 0;
		while (true)
		{
			char value = ParseHexDigit(out success);
			if (!success)
			{
				break;
			}
			stringBuilder.Append(value);
			num2++;
		}
		if (num2 > 0)
		{
			success = true;
		}
		if (!success)
		{
			Error("Failed to parse (HexDigit)+ of HexicalNumber.");
			position = num;
		}
		if (success)
		{
			ClearError(count);
		}
		ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
		return stringBuilder.ToString();
	}

	private string ParseSingleQuotedText(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "SingleQuotedText");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as string;
		}
		int item = Errors.Count;
		StringBuilder stringBuilder = new StringBuilder();
		do
		{
			ErrorStack.Push(item);
			item = Errors.Count;
			char value = MatchTerminalSet("'\\", isComplement: true, out success);
			if (success)
			{
				ClearError(item);
				stringBuilder.Append(value);
			}
			else
			{
				value = ParseEscapeChar(out success);
				if (success)
				{
					ClearError(item);
					stringBuilder.Append(value);
				}
			}
			item = ErrorStack.Pop();
		}
		while (success);
		success = true;
		ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
		return stringBuilder.ToString();
	}

	private string ParseDoubleQuotedText(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "DoubleQuotedText");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as string;
		}
		int item = Errors.Count;
		StringBuilder stringBuilder = new StringBuilder();
		do
		{
			ErrorStack.Push(item);
			item = Errors.Count;
			char value = MatchTerminalSet("\"\\", isComplement: true, out success);
			if (success)
			{
				ClearError(item);
				stringBuilder.Append(value);
			}
			else
			{
				value = ParseEscapeChar(out success);
				if (success)
				{
					ClearError(item);
					stringBuilder.Append(value);
				}
			}
			item = ErrorStack.Pop();
		}
		while (success);
		success = true;
		ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
		return stringBuilder.ToString();
	}

	private string ParseLongString(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "LongString");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as string;
		}
		StringBuilder stringBuilder = new StringBuilder();
		int num = position;
		List<char> list = new List<char>();
		int num2 = position;
		char item = MatchTerminal('[', out success);
		if (success)
		{
			list.Add(item);
			while (true)
			{
				item = MatchTerminal('=', out success);
				if (!success)
				{
					break;
				}
				list.Add(item);
			}
			success = true;
			item = MatchTerminal('[', out success);
			if (success)
			{
				list.Add(item);
			}
			else
			{
				Error("Failed to parse '[' of LongString.");
				position = num2;
			}
		}
		if (!success)
		{
			position = num;
			ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
			return stringBuilder.ToString();
		}
		ParseEol(out success);
		string terminalString = new string(list.ToArray()).Replace('[', ']');
		success = true;
		while (true)
		{
			MatchTerminalString(terminalString, out success);
			if (success)
			{
				break;
			}
			char value = MatchTerminalSet("", isComplement: true, out success);
			if (!success)
			{
				break;
			}
			stringBuilder.Append(value);
		}
		success = true;
		ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
		return stringBuilder.ToString();
	}

	private void ParseKeyword(out bool success)
	{
		int count = Errors.Count;
		MatchTerminalString("and", out success);
		if (success)
		{
			ClearError(count);
			return;
		}
		MatchTerminalString("break", out success);
		if (success)
		{
			ClearError(count);
			return;
		}
		MatchTerminalString("do", out success);
		if (success)
		{
			ClearError(count);
			return;
		}
		MatchTerminalString("elseif", out success);
		if (success)
		{
			ClearError(count);
			return;
		}
		MatchTerminalString("else", out success);
		if (success)
		{
			ClearError(count);
			return;
		}
		MatchTerminalString("end", out success);
		if (success)
		{
			ClearError(count);
			return;
		}
		MatchTerminalString("false", out success);
		if (success)
		{
			ClearError(count);
			return;
		}
		MatchTerminalString("for", out success);
		if (success)
		{
			ClearError(count);
			return;
		}
		MatchTerminalString("function", out success);
		if (success)
		{
			ClearError(count);
			return;
		}
		MatchTerminalString("if", out success);
		if (success)
		{
			ClearError(count);
			return;
		}
		MatchTerminalString("in", out success);
		if (success)
		{
			ClearError(count);
			return;
		}
		MatchTerminalString("local", out success);
		if (success)
		{
			ClearError(count);
			return;
		}
		MatchTerminalString("nil", out success);
		if (success)
		{
			ClearError(count);
			return;
		}
		MatchTerminalString("not", out success);
		if (success)
		{
			ClearError(count);
			return;
		}
		MatchTerminalString("or", out success);
		if (success)
		{
			ClearError(count);
			return;
		}
		MatchTerminalString("repeat", out success);
		if (success)
		{
			ClearError(count);
			return;
		}
		MatchTerminalString("return", out success);
		if (success)
		{
			ClearError(count);
			return;
		}
		MatchTerminalString("then", out success);
		if (success)
		{
			ClearError(count);
			return;
		}
		MatchTerminalString("true", out success);
		if (success)
		{
			ClearError(count);
			return;
		}
		MatchTerminalString("until", out success);
		if (success)
		{
			ClearError(count);
			return;
		}
		MatchTerminalString("while", out success);
		if (success)
		{
			ClearError(count);
		}
	}

	private char ParseDigit(out bool success)
	{
		int count = Errors.Count;
		char result = MatchTerminalRange('0', '9', out success);
		if (success)
		{
			ClearError(count);
			return result;
		}
		Error("Failed to parse '0'...'9' of Digit.");
		return result;
	}

	private char ParseHexDigit(out bool success)
	{
		int count = Errors.Count;
		char result = MatchTerminalSet("0123456789ABCDEFabcdef", isComplement: false, out success);
		if (success)
		{
			ClearError(count);
			return result;
		}
		Error("Failed to parse \"0123456789ABCDEFabcdef\" of HexDigit.");
		return result;
	}

	private char ParseLetter(out bool success)
	{
		int count = Errors.Count;
		char c = '\0';
		c = MatchTerminalRange('A', 'Z', out success);
		if (success)
		{
			ClearError(count);
			return c;
		}
		c = MatchTerminalRange('a', 'z', out success);
		if (success)
		{
			ClearError(count);
			return c;
		}
		c = MatchTerminal('_', out success);
		if (success)
		{
			ClearError(count);
			return c;
		}
		return c;
	}

	private string ParseUnaryOperator(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "UnaryOperator");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as string;
		}
		int count = Errors.Count;
		StringBuilder stringBuilder = new StringBuilder();
		char value = MatchTerminal('#', out success);
		if (success)
		{
			ClearError(count);
			stringBuilder.Append(value);
			ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
			return stringBuilder.ToString();
		}
		value = MatchTerminal('-', out success);
		if (success)
		{
			ClearError(count);
			stringBuilder.Append(value);
			ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
			return stringBuilder.ToString();
		}
		string value2 = MatchTerminalString("not", out success);
		if (success)
		{
			ClearError(count);
			stringBuilder.Append(value2);
			ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
			return stringBuilder.ToString();
		}
		return stringBuilder.ToString();
	}

	private string ParseBinaryOperator(out bool success)
	{
		Tuple<int, string> key = new Tuple<int, string>(position, "BinaryOperator");
		if (ParsingResults.ContainsKey(key))
		{
			Tuple<object, bool, int> tuple = ParsingResults[key];
			success = tuple.Item2;
			position = tuple.Item3;
			return tuple.Item1 as string;
		}
		int count = Errors.Count;
		StringBuilder stringBuilder = new StringBuilder();
		char value = MatchTerminal('+', out success);
		if (success)
		{
			ClearError(count);
			stringBuilder.Append(value);
			ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
			return stringBuilder.ToString();
		}
		value = MatchTerminal('-', out success);
		if (success)
		{
			ClearError(count);
			stringBuilder.Append(value);
			ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
			return stringBuilder.ToString();
		}
		value = MatchTerminal('*', out success);
		if (success)
		{
			ClearError(count);
			stringBuilder.Append(value);
			ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
			return stringBuilder.ToString();
		}
		value = MatchTerminal('/', out success);
		if (success)
		{
			ClearError(count);
			stringBuilder.Append(value);
			ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
			return stringBuilder.ToString();
		}
		value = MatchTerminal('%', out success);
		if (success)
		{
			ClearError(count);
			stringBuilder.Append(value);
			ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
			return stringBuilder.ToString();
		}
		value = MatchTerminal('^', out success);
		if (success)
		{
			ClearError(count);
			stringBuilder.Append(value);
			ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
			return stringBuilder.ToString();
		}
		string value2 = MatchTerminalString("..", out success);
		if (success)
		{
			ClearError(count);
			stringBuilder.Append(value2);
			ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
			return stringBuilder.ToString();
		}
		value2 = MatchTerminalString("==", out success);
		if (success)
		{
			ClearError(count);
			stringBuilder.Append(value2);
			ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
			return stringBuilder.ToString();
		}
		value2 = MatchTerminalString("~=", out success);
		if (success)
		{
			ClearError(count);
			stringBuilder.Append(value2);
			ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
			return stringBuilder.ToString();
		}
		value2 = MatchTerminalString("<=", out success);
		if (success)
		{
			ClearError(count);
			stringBuilder.Append(value2);
			ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
			return stringBuilder.ToString();
		}
		value2 = MatchTerminalString(">=", out success);
		if (success)
		{
			ClearError(count);
			stringBuilder.Append(value2);
			ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
			return stringBuilder.ToString();
		}
		value = MatchTerminal('<', out success);
		if (success)
		{
			ClearError(count);
			stringBuilder.Append(value);
			ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
			return stringBuilder.ToString();
		}
		value = MatchTerminal('>', out success);
		if (success)
		{
			ClearError(count);
			stringBuilder.Append(value);
			ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
			return stringBuilder.ToString();
		}
		value2 = MatchTerminalString("and", out success);
		if (success)
		{
			ClearError(count);
			stringBuilder.Append(value2);
			ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
			return stringBuilder.ToString();
		}
		value2 = MatchTerminalString("or", out success);
		if (success)
		{
			ClearError(count);
			stringBuilder.Append(value2);
			ParsingResults[key] = new Tuple<object, bool, int>(stringBuilder.ToString(), success, position);
			return stringBuilder.ToString();
		}
		return stringBuilder.ToString();
	}

	private void ParseWordSep(out bool success)
	{
		int count = Errors.Count;
		MatchTerminalSet(" \t\r\n'\",;:={}[]()+-*/%^.~<>#", isComplement: false, out success);
		if (success)
		{
			ClearError(count);
		}
		else
		{
			Error("Failed to parse \" \t\r\n'\",;:={}[]()+-*/%^.~<>#\" of WordSep.");
		}
	}

	private void ParseFieldSep(out bool success)
	{
		int count = Errors.Count;
		MatchTerminal(',', out success);
		if (success)
		{
			ClearError(count);
			return;
		}
		MatchTerminal(';', out success);
		if (success)
		{
			ClearError(count);
		}
	}

	private void ParseSpReq(out bool success)
	{
		int num = Errors.Count;
		int num2 = 0;
		while (true)
		{
			ErrorStack.Push(num);
			num = Errors.Count;
			MatchTerminalSet(" \t\r\n", isComplement: false, out success);
			if (success)
			{
				ClearError(num);
			}
			else
			{
				ParseComment(out success);
				if (success)
				{
					ClearError(num);
				}
			}
			num = ErrorStack.Pop();
			if (!success)
			{
				break;
			}
			num2++;
		}
		if (num2 > 0)
		{
			success = true;
		}
		if (success)
		{
			ClearError(num);
			return;
		}
		int num3 = position;
		ParseSpOpt(out success);
		ParseEof(out success);
		if (!success)
		{
			Error("Failed to parse Eof of SpReq.");
			position = num3;
		}
		if (success)
		{
			ClearError(num);
		}
	}

	private void ParseSpOpt(out bool success)
	{
		int item = Errors.Count;
		do
		{
			ErrorStack.Push(item);
			item = Errors.Count;
			MatchTerminalSet(" \t\r\n", isComplement: false, out success);
			if (success)
			{
				ClearError(item);
			}
			else
			{
				ParseComment(out success);
				if (success)
				{
					ClearError(item);
				}
			}
			item = ErrorStack.Pop();
		}
		while (success);
		success = true;
	}

	private void ParseComment(out bool success)
	{
		int count = Errors.Count;
		int num = position;
		MatchTerminalString("--", out success);
		if (!success)
		{
			position = num;
			return;
		}
		ErrorStack.Push(count);
		count = Errors.Count;
		ParseLongString(out success);
		if (success)
		{
			ClearError(count);
		}
		else
		{
			int num2 = position;
			do
			{
				MatchTerminalSet("\r\n", isComplement: true, out success);
			}
			while (success);
			success = true;
			ErrorStack.Push(count);
			count = Errors.Count;
			ParseEol(out success);
			if (success)
			{
				ClearError(count);
			}
			else
			{
				ParseEof(out success);
				if (success)
				{
					ClearError(count);
				}
			}
			count = ErrorStack.Pop();
			if (!success)
			{
				Error("Failed to parse (Eol / Eof) of Comment.");
				position = num2;
			}
			if (success)
			{
				ClearError(count);
			}
		}
		count = ErrorStack.Pop();
		if (!success)
		{
			Error("Failed to parse (LongString / (-\"\r\n\")* (Eol / Eof)) of Comment.");
			position = num;
		}
		if (success)
		{
			ClearError(count);
		}
	}

	private void ParseEol(out bool success)
	{
		int count = Errors.Count;
		MatchTerminalString("\r\n", out success);
		if (success)
		{
			ClearError(count);
			return;
		}
		MatchTerminal('\n', out success);
		if (success)
		{
			ClearError(count);
			return;
		}
		MatchTerminal('\r', out success);
		if (success)
		{
			ClearError(count);
		}
	}

	private void ParseEof(out bool success)
	{
		int count = Errors.Count;
		success = !Input.HasInput(position);
		if (success)
		{
			ClearError(count);
		}
		else
		{
			Error("Failed to parse end of Eof.");
		}
	}

	private char ParseEscapeChar(out bool success)
	{
		char result = '\0';
		MatchTerminalString("\\\\", out success);
		if (success)
		{
			return '\\';
		}
		MatchTerminalString("\\'", out success);
		if (success)
		{
			return '\'';
		}
		MatchTerminalString("\\\"", out success);
		if (success)
		{
			return '"';
		}
		MatchTerminalString("\\r", out success);
		if (success)
		{
			return '\r';
		}
		MatchTerminalString("\\n", out success);
		if (success)
		{
			return '\n';
		}
		MatchTerminalString("\\t", out success);
		if (success)
		{
			return '\t';
		}
		MatchTerminalString("\\v", out success);
		if (success)
		{
			return '\v';
		}
		MatchTerminalString("\\a", out success);
		if (success)
		{
			return '\a';
		}
		MatchTerminalString("\\b", out success);
		if (success)
		{
			return '\b';
		}
		MatchTerminalString("\\f", out success);
		if (success)
		{
			return '\f';
		}
		MatchTerminalString("\\0", out success);
		if (success)
		{
			return '\0';
		}
		return result;
	}

	public void SetInput(ParserInput<char> input)
	{
		Input = input;
		position = 0;
		ParsingResults.Clear();
	}

	private bool TerminalMatch(char terminal)
	{
		if (Input.HasInput(position))
		{
			char inputSymbol = Input.GetInputSymbol(position);
			return terminal == inputSymbol;
		}
		return false;
	}

	private bool TerminalMatch(char terminal, int pos)
	{
		if (Input.HasInput(pos))
		{
			char inputSymbol = Input.GetInputSymbol(pos);
			return terminal == inputSymbol;
		}
		return false;
	}

	private char MatchTerminal(char terminal, out bool success)
	{
		success = false;
		if (Input.HasInput(position))
		{
			char inputSymbol = Input.GetInputSymbol(position);
			if (terminal == inputSymbol)
			{
				position++;
				success = true;
			}
			return inputSymbol;
		}
		return '\0';
	}

	private char MatchTerminalRange(char start, char end, out bool success)
	{
		success = false;
		if (Input.HasInput(position))
		{
			char inputSymbol = Input.GetInputSymbol(position);
			if (start <= inputSymbol && inputSymbol <= end)
			{
				position++;
				success = true;
			}
			return inputSymbol;
		}
		return '\0';
	}

	private char MatchTerminalSet(string terminalSet, bool isComplement, out bool success)
	{
		success = false;
		if (Input.HasInput(position))
		{
			char inputSymbol = Input.GetInputSymbol(position);
			if (isComplement ? (terminalSet.IndexOf(inputSymbol) == -1) : (terminalSet.IndexOf(inputSymbol) > -1))
			{
				position++;
				success = true;
			}
			return inputSymbol;
		}
		return '\0';
	}

	private string MatchTerminalString(string terminalString, out bool success)
	{
		int num = position;
		foreach (char terminal in terminalString)
		{
			MatchTerminal(terminal, out success);
			if (!success)
			{
				position = num;
				return null;
			}
		}
		success = true;
		return terminalString;
	}

	public bool PeekTerminalString(string terminalString)
	{
		if (string.IsNullOrEmpty(terminalString))
		{
			return true;
		}
		for (int i = 0; i < terminalString.Length; i++)
		{
			int pos = position + i;
			if (!Input.HasInput(pos) || Input.GetInputSymbol(pos) != terminalString[i])
			{
				return false;
			}
		}
		return true;
	}

	private int Error(string message)
	{
		Errors.Add(new Tuple<int, string>(position, message));
		return Errors.Count;
	}

	private void ClearError(int count)
	{
		Errors.RemoveRange(count, Errors.Count - count);
	}

	public void ClearErrors()
	{
		Errors.Clear();
		ErrorStack.Clear();
	}

	public string GetErrorMessages()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Tuple<int, string> error in Errors)
		{
			stringBuilder.Append(Input.FormErrorMessage(error.Item1, error.Item2));
			stringBuilder.AppendLine();
		}
		return stringBuilder.ToString();
	}
}
