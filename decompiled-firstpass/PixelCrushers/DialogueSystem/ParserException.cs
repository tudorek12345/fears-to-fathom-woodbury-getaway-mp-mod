using System;

namespace PixelCrushers.DialogueSystem;

public class ParserException : Exception
{
	public ParserException(string message)
		: base(message)
	{
	}
}
