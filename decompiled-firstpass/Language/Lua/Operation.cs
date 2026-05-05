using System;

namespace Language.Lua;

public class Operation : Term
{
	public string Operator;

	public Term LeftOperand;

	public Term RightOperand;

	public Operation(string oper)
	{
		Operator = oper;
	}

	public Operation(string oper, Term left, Term right)
	{
		Operator = oper;
		LeftOperand = left?.Simplify();
		RightOperand = right?.Simplify();
	}

	public override LuaValue Evaluate(LuaTable enviroment)
	{
		if (LeftOperand == null)
		{
			return PrefixUnaryOperation(Operator, RightOperand, enviroment);
		}
		if (RightOperand == null)
		{
			return LeftOperand.Evaluate(enviroment);
		}
		return InfixBinaryOperation(LeftOperand, Operator, RightOperand, enviroment);
	}

	private LuaValue PrefixUnaryOperation(string Operator, Term RightOperand, LuaTable enviroment)
	{
		LuaValue luaValue = RightOperand.Evaluate(enviroment);
		switch (Operator)
		{
		case "-":
		{
			if (luaValue is LuaNumber luaNumber)
			{
				return new LuaNumber(0.0 - luaNumber.Number);
			}
			LuaFunction metaFunction = GetMetaFunction("__unm", luaValue, null);
			if (metaFunction != null)
			{
				return metaFunction.Invoke(new LuaValue[1] { luaValue });
			}
			break;
		}
		case "#":
			if (luaValue is LuaTable luaTable)
			{
				return new LuaNumber(luaTable.Length);
			}
			if (luaValue is LuaString luaString)
			{
				return new LuaNumber(luaString.Text.Length);
			}
			break;
		case "not":
			if (luaValue is LuaBoolean luaBoolean)
			{
				return LuaBoolean.From(!luaBoolean.BoolValue);
			}
			break;
		}
		return LuaNil.Nil;
	}

	private LuaValue InfixBinaryOperation(Term LeftOperand, string Operator, Term RightOperand, LuaTable enviroment)
	{
		bool num = string.Equals(Operator, "and") || string.Equals(Operator, "or");
		LuaValue luaValue = LeftOperand.Evaluate(enviroment);
		LuaValue luaValue2 = (num ? null : RightOperand.Evaluate(enviroment));
		switch (Operator)
		{
		case "+":
		{
			LuaNumber luaNumber = luaValue as LuaNumber;
			LuaNumber luaNumber2 = luaValue2 as LuaNumber;
			if (luaNumber != null && luaNumber2 != null)
			{
				return new LuaNumber(luaNumber.Number + luaNumber2.Number);
			}
			LuaFunction metaFunction7 = GetMetaFunction("__add", luaValue, luaValue2);
			if (metaFunction7 != null)
			{
				return metaFunction7.Invoke(new LuaValue[2] { luaValue, luaValue2 });
			}
			break;
		}
		case "-":
		{
			LuaNumber luaNumber = luaValue as LuaNumber;
			LuaNumber luaNumber2 = luaValue2 as LuaNumber;
			if (luaNumber != null && luaNumber2 != null)
			{
				return new LuaNumber(luaNumber.Number - luaNumber2.Number);
			}
			LuaFunction metaFunction2 = GetMetaFunction("__sub", luaValue, luaValue2);
			if (metaFunction2 != null)
			{
				return metaFunction2.Invoke(new LuaValue[2] { luaValue, luaValue2 });
			}
			break;
		}
		case "*":
		{
			LuaNumber luaNumber = luaValue as LuaNumber;
			LuaNumber luaNumber2 = luaValue2 as LuaNumber;
			if (luaNumber != null && luaNumber2 != null)
			{
				return new LuaNumber(luaNumber.Number * luaNumber2.Number);
			}
			LuaFunction metaFunction = GetMetaFunction("__mul", luaValue, luaValue2);
			if (metaFunction != null)
			{
				return metaFunction.Invoke(new LuaValue[2] { luaValue, luaValue2 });
			}
			break;
		}
		case "/":
		{
			LuaNumber luaNumber = luaValue as LuaNumber;
			LuaNumber luaNumber2 = luaValue2 as LuaNumber;
			if (luaNumber != null && luaNumber2 != null)
			{
				return new LuaNumber(luaNumber.Number / luaNumber2.Number);
			}
			LuaFunction metaFunction8 = GetMetaFunction("__div", luaValue, luaValue2);
			if (metaFunction8 != null)
			{
				return metaFunction8.Invoke(new LuaValue[2] { luaValue, luaValue2 });
			}
			break;
		}
		case "%":
		{
			LuaNumber luaNumber = luaValue as LuaNumber;
			LuaNumber luaNumber2 = luaValue2 as LuaNumber;
			if (luaNumber != null && luaNumber2 != null)
			{
				return new LuaNumber(luaNumber.Number % luaNumber2.Number);
			}
			LuaFunction metaFunction4 = GetMetaFunction("__mod", luaValue, luaValue2);
			if (metaFunction4 != null)
			{
				return metaFunction4.Invoke(new LuaValue[2] { luaValue, luaValue2 });
			}
			break;
		}
		case "^":
		{
			LuaNumber luaNumber = luaValue as LuaNumber;
			LuaNumber luaNumber2 = luaValue2 as LuaNumber;
			if (luaNumber != null && luaNumber2 != null)
			{
				return new LuaNumber(Math.Pow(luaNumber.Number, luaNumber2.Number));
			}
			LuaFunction metaFunction9 = GetMetaFunction("__pow", luaValue, luaValue2);
			if (metaFunction9 != null)
			{
				return metaFunction9.Invoke(new LuaValue[2] { luaValue, luaValue2 });
			}
			break;
		}
		case "==":
			return LuaBoolean.From(luaValue.Equals(luaValue2));
		case "~=":
			return LuaBoolean.From(!luaValue.Equals(luaValue2));
		case "<":
		{
			int? num2 = Compare(luaValue, luaValue2);
			if (num2.HasValue)
			{
				return LuaBoolean.From(num2 < 0);
			}
			LuaFunction metaFunction11 = GetMetaFunction("__lt", luaValue, luaValue2);
			if (metaFunction11 != null)
			{
				return metaFunction11.Invoke(new LuaValue[2] { luaValue, luaValue2 });
			}
			break;
		}
		case ">":
		{
			int? num2 = Compare(luaValue, luaValue2);
			if (num2.HasValue)
			{
				return LuaBoolean.From(num2 > 0);
			}
			LuaFunction metaFunction6 = GetMetaFunction("__gt", luaValue, luaValue2);
			if (metaFunction6 != null)
			{
				return metaFunction6.Invoke(new LuaValue[2] { luaValue, luaValue2 });
			}
			break;
		}
		case "<=":
		{
			int? num2 = Compare(luaValue, luaValue2);
			if (num2.HasValue)
			{
				return LuaBoolean.From(num2 <= 0);
			}
			LuaFunction metaFunction3 = GetMetaFunction("__le", luaValue, luaValue2);
			if (metaFunction3 != null)
			{
				return metaFunction3.Invoke(new LuaValue[2] { luaValue, luaValue2 });
			}
			break;
		}
		case ">=":
		{
			int? num2 = Compare(luaValue, luaValue2);
			if (num2.HasValue)
			{
				return LuaBoolean.From(num2 >= 0);
			}
			LuaFunction metaFunction10 = GetMetaFunction("__ge", luaValue, luaValue2);
			if (metaFunction10 != null)
			{
				return metaFunction10.Invoke(new LuaValue[2] { luaValue, luaValue2 });
			}
			break;
		}
		case "..":
		{
			if ((luaValue is LuaString || luaValue is LuaNumber) && (luaValue2 is LuaString || luaValue2 is LuaNumber))
			{
				return new LuaString(string.Concat(luaValue, luaValue2));
			}
			LuaFunction metaFunction5 = GetMetaFunction("__concat", luaValue, luaValue2);
			if (metaFunction5 != null)
			{
				return metaFunction5.Invoke(new LuaValue[2] { luaValue, luaValue2 });
			}
			break;
		}
		case "and":
			if (!luaValue.GetBooleanValue())
			{
				return luaValue;
			}
			return RightOperand.Evaluate(enviroment);
		case "or":
			if (luaValue.GetBooleanValue())
			{
				return luaValue;
			}
			return RightOperand.Evaluate(enviroment);
		}
		return null;
	}

	private static int? Compare(LuaValue leftValue, LuaValue rightValue)
	{
		LuaNumber luaNumber = leftValue as LuaNumber;
		LuaNumber luaNumber2 = rightValue as LuaNumber;
		if (luaNumber != null && luaNumber2 != null)
		{
			return luaNumber.Number.CompareTo(luaNumber2.Number);
		}
		LuaString luaString = leftValue as LuaString;
		LuaString luaString2 = rightValue as LuaString;
		if (luaString != null && luaString2 != null)
		{
			return StringComparer.Ordinal.Compare(luaString.Text, luaString2.Text);
		}
		return null;
	}

	private static LuaFunction GetMetaFunction(string name, LuaValue leftValue, LuaValue rightValue)
	{
		if (leftValue is LuaTable luaTable && luaTable.GetValue(name) is LuaFunction result)
		{
			return result;
		}
		if (rightValue is LuaTable luaTable2)
		{
			return luaTable2.GetValue(name) as LuaFunction;
		}
		return null;
	}
}
