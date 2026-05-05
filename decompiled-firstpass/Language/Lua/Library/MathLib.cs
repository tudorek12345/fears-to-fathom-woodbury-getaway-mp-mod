using System;
using System.Linq;

namespace Language.Lua.Library;

public static class MathLib
{
	private static Random randomGenerator = new Random();

	public static void RegisterModule(LuaTable enviroment)
	{
		LuaTable luaTable = new LuaTable();
		RegisterFunctions(luaTable);
		enviroment.SetNameValue("math", luaTable);
	}

	public static void RegisterFunctions(LuaTable module)
	{
		module.SetNameValue("huge", new LuaNumber(double.MaxValue));
		module.SetNameValue("pi", new LuaNumber(Math.PI));
		module.Register("abs", abs);
		module.Register("acos", acos);
		module.Register("asin", asin);
		module.Register("atan", atan);
		module.Register("atan2", atan2);
		module.Register("ceil", ceil);
		module.Register("cos", cos);
		module.Register("cosh", cosh);
		module.Register("deg", deg);
		module.Register("exp", exp);
		module.Register("floor", floor);
		module.Register("fmod", fmod);
		module.Register("log", log);
		module.Register("log10", log10);
		module.Register("max", max);
		module.Register("min", min);
		module.Register("modf", modf);
		module.Register("pow", pow);
		module.Register("rad", rad);
		module.Register("random", random);
		module.Register("randomseed", randomseed);
		module.Register("sin", sin);
		module.Register("sinh", sinh);
		module.Register("sqrt", sqrt);
		module.Register("tan", tan);
		module.Register("tanh", tanh);
	}

	public static LuaValue abs(LuaValue[] values)
	{
		return new LuaNumber(Math.Abs(CheckArgs(values).Number));
	}

	public static LuaValue acos(LuaValue[] values)
	{
		return new LuaNumber(Math.Acos(CheckArgs(values).Number));
	}

	public static LuaValue asin(LuaValue[] values)
	{
		return new LuaNumber(Math.Asin(CheckArgs(values).Number));
	}

	public static LuaValue atan(LuaValue[] values)
	{
		return new LuaNumber(Math.Atan(CheckArgs(values).Number));
	}

	public static LuaValue atan2(LuaValue[] values)
	{
		Tuple<double, double> tuple = CheckArgs2(values);
		return new LuaNumber(Math.Atan2(tuple.Item1, tuple.Item2));
	}

	public static LuaValue ceil(LuaValue[] values)
	{
		return new LuaNumber(Math.Ceiling(CheckArgs(values).Number));
	}

	public static LuaValue cos(LuaValue[] values)
	{
		return new LuaNumber(Math.Cos(CheckArgs(values).Number));
	}

	public static LuaValue cosh(LuaValue[] values)
	{
		return new LuaNumber(Math.Cosh(CheckArgs(values).Number));
	}

	public static LuaValue deg(LuaValue[] values)
	{
		return new LuaNumber(CheckArgs(values).Number * 180.0 / Math.PI);
	}

	public static LuaValue exp(LuaValue[] values)
	{
		return new LuaNumber(Math.Exp(CheckArgs(values).Number));
	}

	public static LuaValue floor(LuaValue[] values)
	{
		return new LuaNumber(Math.Floor(CheckArgs(values).Number));
	}

	public static LuaValue fmod(LuaValue[] values)
	{
		Tuple<double, double> tuple = CheckArgs2(values);
		return new LuaNumber(Math.IEEERemainder(tuple.Item1, tuple.Item2));
	}

	public static LuaValue log(LuaValue[] values)
	{
		return new LuaNumber(Math.Log(CheckArgs(values).Number));
	}

	public static LuaValue log10(LuaValue[] values)
	{
		return new LuaNumber(Math.Log10(CheckArgs(values).Number));
	}

	public static LuaValue max(LuaValue[] values)
	{
		return new LuaNumber(values.Max((LuaValue v) => (v as LuaNumber).Number));
	}

	public static LuaValue min(LuaValue[] values)
	{
		return new LuaNumber(values.Min((LuaValue v) => (v as LuaNumber).Number));
	}

	public static LuaValue modf(LuaValue[] values)
	{
		LuaNumber luaNumber = CheckArgs(values);
		double num = Math.Floor(luaNumber.Number);
		LuaValue[] values2 = new LuaNumber[2]
		{
			new LuaNumber(num),
			new LuaNumber(luaNumber.Number - num)
		};
		return new LuaMultiValue(values2);
	}

	public static LuaValue pow(LuaValue[] values)
	{
		Tuple<double, double> tuple = CheckArgs2(values);
		return new LuaNumber(Math.Pow(tuple.Item1, tuple.Item2));
	}

	public static LuaValue rad(LuaValue[] values)
	{
		return new LuaNumber(CheckArgs(values).Number * Math.PI / 180.0);
	}

	public static LuaValue random(LuaValue[] values)
	{
		if (values.Length == 0)
		{
			return new LuaNumber(randomGenerator.NextDouble());
		}
		if (values.Length == 1)
		{
			LuaNumber luaNumber = values[0] as LuaNumber;
			return new LuaNumber(randomGenerator.Next((int)luaNumber.Number) + 1);
		}
		Tuple<double, double> tuple = CheckArgs2(values);
		return new LuaNumber(randomGenerator.Next((int)tuple.Item1, (int)tuple.Item2 + 1));
	}

	public static LuaValue randomseed(LuaValue[] values)
	{
		LuaNumber luaNumber = CheckArgs(values);
		randomGenerator = new Random((int)luaNumber.Number);
		return luaNumber;
	}

	public static LuaValue sin(LuaValue[] values)
	{
		return new LuaNumber(Math.Sin(CheckArgs(values).Number));
	}

	public static LuaValue sinh(LuaValue[] values)
	{
		return new LuaNumber(Math.Sinh(CheckArgs(values).Number));
	}

	public static LuaValue sqrt(LuaValue[] values)
	{
		return new LuaNumber(Math.Sqrt(CheckArgs(values).Number));
	}

	public static LuaValue tan(LuaValue[] values)
	{
		return new LuaNumber(Math.Tan(CheckArgs(values).Number));
	}

	public static LuaValue tanh(LuaValue[] values)
	{
		return new LuaNumber(Math.Tanh(CheckArgs(values).Number));
	}

	private static LuaNumber CheckArgs(LuaValue[] values)
	{
		if (values.Length >= 1)
		{
			if (values[0] is LuaNumber result)
			{
				return result;
			}
			throw new LuaError("bad argument #1 to 'abs' (number expected, got {0})", values[0].GetTypeCode());
		}
		throw new LuaError("bad argument #1 to 'abs' (number expected, got no value)");
	}

	private static Tuple<double, double> CheckArgs2(LuaValue[] values)
	{
		if (values.Length >= 2)
		{
			if (!(values[0] is LuaNumber luaNumber))
			{
				throw new LuaError("bad argument #1 to 'abs' (number expected, got {0})", values[0].GetTypeCode());
			}
			if (!(values[1] is LuaNumber luaNumber2))
			{
				throw new LuaError("bad argument #2 to 'abs' (number expected, got {0})", values[1].GetTypeCode());
			}
			return Tuple.Create(luaNumber.Number, luaNumber2.Number);
		}
		throw new LuaError("bad argument #1 to 'abs' (number expected, got no value)");
	}
}
