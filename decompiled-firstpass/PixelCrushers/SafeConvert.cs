using System.Globalization;

namespace PixelCrushers;

public static class SafeConvert
{
	private const string CommaTag = "%COMMA%";

	private const string DoubleQuoteTag = "%QUOTE%";

	private const string NewlineTag = "%NEWLINE%";

	public static int ToInt(string s)
	{
		if (!int.TryParse(s, out var result))
		{
			return 0;
		}
		return result;
	}

	public static float ToFloat(string s)
	{
		if (!float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return 0f;
		}
		return result;
	}

	public static string ToSerializedElement(string s)
	{
		if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
		{
			return s.Replace(",", "%COMMA%").Replace("\"", "%QUOTE%").Replace("\n", "%NEWLINE%");
		}
		return s;
	}

	public static string FromSerializedElement(string s)
	{
		if (s.Contains("%COMMA%") || s.Contains("%QUOTE%") || s.Contains("%NEWLINE%"))
		{
			return s.Replace("%COMMA%", ",").Replace("%QUOTE%", "\"").Replace("%NEWLINE%", "\n");
		}
		return s;
	}
}
