using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public static class DialogueDebug
{
	public enum DebugLevel
	{
		None,
		Error,
		Warning,
		Info
	}

	public const string Prefix = "Dialogue System";

	public static DebugLevel level { get; set; }

	public static bool logInfo
	{
		get
		{
			if (level >= DebugLevel.Info)
			{
				return Debug.isDebugBuild;
			}
			return false;
		}
	}

	public static bool logWarnings
	{
		get
		{
			if (level >= DebugLevel.Warning)
			{
				return Debug.isDebugBuild;
			}
			return false;
		}
	}

	public static bool logErrors
	{
		get
		{
			if (level >= DebugLevel.Error)
			{
				return Debug.isDebugBuild;
			}
			return false;
		}
	}

	public static DebugLevel Level
	{
		get
		{
			return level;
		}
		set
		{
			level = value;
		}
	}

	public static bool LogInfo => logInfo;

	public static bool LogWarnings => logWarnings;

	public static bool LogErrors => logErrors;

	static DialogueDebug()
	{
		level = DebugLevel.Warning;
	}
}
