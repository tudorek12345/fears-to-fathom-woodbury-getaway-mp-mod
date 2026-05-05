using System;
using System.Reflection;
using Language.Lua;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public sealed class Lua
{
	public struct Result(LuaValue luaValue)
	{
		public LuaValue luaValue = luaValue;

		public LuaTableWrapper luaTableWrapper = null;

		public bool hasReturnValue => luaValue != null;

		public string asString
		{
			get
			{
				if (!hasReturnValue)
				{
					return string.Empty;
				}
				return luaValue.ToString();
			}
		}

		public bool asBool
		{
			get
			{
				if (!hasReturnValue || !(luaValue is LuaBoolean))
				{
					return string.Compare(asString, "True", StringComparison.OrdinalIgnoreCase) == 0;
				}
				return (luaValue as LuaBoolean).BoolValue;
			}
		}

		public float asFloat
		{
			get
			{
				if (!hasReturnValue)
				{
					return 0f;
				}
				return Tools.StringToFloat(luaValue.ToString());
			}
		}

		public int asInt
		{
			get
			{
				if (!hasReturnValue)
				{
					return 0;
				}
				return Tools.StringToInt(luaValue.ToString());
			}
		}

		public LuaTableWrapper asTable
		{
			get
			{
				if (luaTableWrapper == null)
				{
					luaTableWrapper = new LuaTableWrapper(luaValue as LuaTable);
				}
				return luaTableWrapper;
			}
		}

		public bool isString
		{
			get
			{
				if (hasReturnValue)
				{
					return luaValue is LuaString;
				}
				return false;
			}
		}

		public bool isBool
		{
			get
			{
				if (hasReturnValue)
				{
					return luaValue is LuaBoolean;
				}
				return false;
			}
		}

		public bool isNumber
		{
			get
			{
				if (hasReturnValue)
				{
					return luaValue is LuaNumber;
				}
				return false;
			}
		}

		public bool isTable => hasReturnValue & (luaValue is LuaTable);

		public bool HasReturnValue => hasReturnValue;

		public string AsString => asString;

		public bool AsBool => asBool;

		public float AsFloat => asFloat;

		public int AsInt => asInt;

		public LuaTableWrapper AsTable => asTable;

		public bool IsString => isString;

		public bool IsBool => isBool;

		public bool IsNumber => isNumber;

		public bool IsTable => isTable;
	}

	private static Result m_noResult = new Result(null);

	private static LuaTable m_environment = LuaInterpreter.CreateGlobalEnviroment();

	public static Result noResult => m_noResult;

	public static Result NoResult => m_noResult;

	public static bool wasInvoked { get; set; }

	public static bool muteExceptions { get; set; }

	public static bool warnRegisteringExistingFunction { get; set; }

	public static LuaTable environment => m_environment;

	public static LuaTable Environment => m_environment;

	public static bool WasInvoked
	{
		get
		{
			return wasInvoked;
		}
		set
		{
			wasInvoked = value;
		}
	}

	public static bool MuteExceptions
	{
		get
		{
			return muteExceptions;
		}
		set
		{
			muteExceptions = value;
		}
	}

	public static bool WarnRegisteringExistingFunction
	{
		get
		{
			return warnRegisteringExistingFunction;
		}
		set
		{
			warnRegisteringExistingFunction = value;
		}
	}

	public static Result Run(string luaCode, bool debug, bool allowExceptions)
	{
		return new Result(RunRaw(luaCode, debug, allowExceptions));
	}

	public static Result Run(string luaCode, bool debug)
	{
		return Run(luaCode, debug, allowExceptions: false);
	}

	public static Result Run(string luaCode)
	{
		return Run(luaCode, debug: false, allowExceptions: false);
	}

	public static bool IsTrue(string luaCondition, bool debug, bool allowExceptions)
	{
		if (!Tools.IsStringNullOrEmptyOrWhitespace(luaCondition) && !IsOnlyComment(luaCondition))
		{
			return Run("return " + luaCondition, debug, allowExceptions).asBool;
		}
		return true;
	}

	public static bool IsTrue(string luaCondition, bool debug)
	{
		return IsTrue(luaCondition, debug, allowExceptions: false);
	}

	public static bool IsTrue(string luaCondition)
	{
		return IsTrue(luaCondition, debug: false, allowExceptions: false);
	}

	public static bool IsOnlyComment(string luaCode)
	{
		if (luaCode.StartsWith("--"))
		{
			if (!luaCode.Contains("\n"))
			{
				return true;
			}
			string[] array = luaCode.Split('\n');
			for (int i = 0; i < array.Length; i++)
			{
				if (!array[i].StartsWith("--"))
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}

	public static LuaValue RunRaw(string luaCode, bool debug, bool allowExceptions)
	{
		try
		{
			if (string.IsNullOrEmpty(luaCode))
			{
				return null;
			}
			if (Debug.isDebugBuild && debug)
			{
				Debug.Log(string.Format("{0}: Lua({1})", new object[2] { "Dialogue System", luaCode }));
			}
			wasInvoked = true;
			return LuaInterpreter.Interpreter(luaCode, environment);
		}
		catch (Exception ex)
		{
			if (Debug.isDebugBuild && !muteExceptions)
			{
				Debug.LogError(string.Format("{0}: Lua code '{1}' threw exception '{2}'", new object[3] { "Dialogue System", luaCode, ex.Message }));
			}
			if (allowExceptions)
			{
				throw ex;
			}
			return null;
		}
	}

	public static LuaValue RunRaw(string luaCode, bool debug)
	{
		return RunRaw(luaCode, debug, allowExceptions: false);
	}

	public static LuaValue RunRaw(string luaCode)
	{
		return RunRaw(luaCode, debug: false, allowExceptions: false);
	}

	public static void RegisterFunction(string functionName, object target, MethodInfo method)
	{
		if (environment.ContainsKey(new LuaString(functionName)))
		{
			if (warnRegisteringExistingFunction && DialogueDebug.logWarnings)
			{
				Debug.LogWarning(string.Format("{0}: Can't register Lua function {1}. A function with that name is already registered.", new object[2] { "Dialogue System", functionName }));
			}
			return;
		}
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Registering Lua function {1}", new object[2] { "Dialogue System", functionName }));
		}
		environment.RegisterMethodFunction(functionName, target, method);
	}

	public static void UnregisterFunction(string functionName)
	{
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: Unregistering Lua function {1}", new object[2] { "Dialogue System", functionName }));
		}
		environment.SetNameValue(functionName, LuaNil.Nil);
	}
}
