using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PixelCrushers.DialogueSystem.SequencerCommands;
using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public class SequenceParser
{
	private const int MaxSafeguard = 9999;

	private int column;

	private int row;

	private bool isNextCharEscaped;

	public List<QueuedSequencerCommand> Parse(string sequence, bool throwExceptions = false)
	{
		List<QueuedSequencerCommand> list = new List<QueuedSequencerCommand>();
		try
		{
			StringReader stringReader = new StringReader(sequence);
			row = 1;
			column = 1;
			int num = 0;
			while (stringReader.Peek() != -1 && num < 9999)
			{
				num++;
				QueuedSequencerCommand queuedSequencerCommand = ParseCommand(stringReader);
				if (queuedSequencerCommand != null)
				{
					list.Add(queuedSequencerCommand);
				}
			}
		}
		catch (ParserException ex)
		{
			if (DialogueDebug.logWarnings)
			{
				Debug.LogWarning("Dialogue System: Syntax error '" + ex.Message + "' at column " + column + " row " + row + " parsing: " + sequence);
			}
			list.Clear();
			if (throwExceptions)
			{
				throw ex;
			}
		}
		return list;
	}

	private QueuedSequencerCommand ParseCommand(StringReader reader)
	{
		ParseOptionalWhitespace(reader, includingSemicolons: true);
		CheckParseComment(reader);
		bool required = false;
		string text = ParseWord(reader);
		if (string.Equals(text, "required", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "require", StringComparison.OrdinalIgnoreCase))
		{
			required = true;
			ParseOptionalWhitespace(reader);
			text = ParseWord(reader);
		}
		string command = text;
		ParseOptionalWhitespace(reader);
		if (reader.Peek() == -1)
		{
			return null;
		}
		ParseOpenParen(reader);
		ParseOptionalWhitespace(reader);
		string[] parameters = ParseParameters(reader);
		ParseCloseParen(reader);
		ParseOptionalWhitespace(reader);
		ParsePostParameters(reader, out var atTime, out var atMessage, out var sendMessage);
		ParseOptionalWhitespace(reader);
		if (!CheckParseComment(reader))
		{
			ParseSemicolonOrEnd(reader);
		}
		ParseOptionalWhitespace(reader);
		CheckParseComment(reader);
		return new QueuedSequencerCommand(command, parameters, atTime, atMessage, sendMessage, required);
	}

	private string ParseWord(StringReader reader, bool allowWhiteSpace = false)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		while (HasNextChar(reader) && num < 9999)
		{
			num++;
			char c = (char)reader.Peek();
			if ((char.IsWhiteSpace(c) && !allowWhiteSpace) || c == '(' || c == ')' || c == ';' || c == '-')
			{
				break;
			}
			stringBuilder.Append(ReadNextChar(reader));
		}
		return stringBuilder.ToString();
	}

	private void ParseOptionalWhitespace(StringReader reader, bool includingSemicolons = false)
	{
		int num = 0;
		while ((IsNextCharWhiteSpace(reader) || (includingSemicolons && IsNextChar(reader, ';'))) && num < 9999)
		{
			num++;
			ReadNextChar(reader);
		}
	}

	private bool HasNextChar(StringReader reader)
	{
		if (reader != null)
		{
			return reader.Peek() != -1;
		}
		return false;
	}

	private char PeekNextChar(StringReader reader)
	{
		if (reader == null)
		{
			return '\0';
		}
		if (reader.Peek() == 92)
		{
			isNextCharEscaped = true;
			reader.Read();
			column++;
			return (char)reader.Peek();
		}
		return (char)reader.Peek();
	}

	private bool IsNextCharWhiteSpace(StringReader reader)
	{
		if (HasNextChar(reader))
		{
			return char.IsWhiteSpace(PeekNextChar(reader));
		}
		return false;
	}

	private bool IsNextChar(StringReader reader, char requiredChar)
	{
		if (HasNextChar(reader))
		{
			return PeekNextChar(reader) == requiredChar;
		}
		return false;
	}

	private bool IsNextCharNot(StringReader reader, char requiredChar)
	{
		if (HasNextChar(reader))
		{
			return PeekNextChar(reader) != requiredChar;
		}
		return false;
	}

	private char ReadNextChar(StringReader reader)
	{
		ushort num = (ushort)reader.Read();
		if (num == 10)
		{
			row++;
			column = 1;
		}
		else
		{
			column++;
		}
		isNextCharEscaped = false;
		return (char)num;
	}

	private void ParseChar(StringReader reader, char requiredChar)
	{
		if (IsNextChar(reader, requiredChar))
		{
			ReadNextChar(reader);
			return;
		}
		throw new ParserException("Expected '" + requiredChar + "'");
	}

	private void ParseOpenParen(StringReader reader)
	{
		ParseChar(reader, '(');
	}

	private void ParseCloseParen(StringReader reader)
	{
		ParseChar(reader, ')');
	}

	private string[] ParseParameters(StringReader reader)
	{
		List<string> list = new List<string>();
		int num = 0;
		while (IsNextCharNot(reader, ')') && num < 9999)
		{
			num++;
			ParseOptionalWhitespace(reader);
			list.Add(ParseParameter(reader));
			if (IsNextChar(reader, ','))
			{
				ReadNextChar(reader);
			}
		}
		return list.ToArray();
	}

	private string ParseParameter(StringReader reader)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		int num2 = 0;
		while (HasNextChar(reader) && num2 < 9999)
		{
			num2++;
			char c = PeekNextChar(reader);
			if (num <= 0 && (c == ',' || c == ')') && !isNextCharEscaped)
			{
				break;
			}
			char c2 = ReadNextChar(reader);
			stringBuilder.Append(c2);
			switch (c2)
			{
			case '(':
				num++;
				break;
			case ')':
				num--;
				break;
			}
		}
		return stringBuilder.ToString().Trim();
	}

	private void ParsePostParameters(StringReader reader, out float atTime, out string atMessage, out string sendMessage)
	{
		atTime = 0f;
		atMessage = string.Empty;
		sendMessage = string.Empty;
		ParseOptionalWhitespace(reader);
		if (IsNextChar(reader, '@'))
		{
			ParseAtSignModifier(reader, out atTime, out atMessage);
		}
		ParseOptionalWhitespace(reader);
		if (IsNextChar(reader, '-'))
		{
			ParseArrowModifier(reader, out sendMessage);
		}
	}

	private void ParseAtSignModifier(StringReader reader, out float atTime, out string atMessage)
	{
		atTime = 0f;
		atMessage = string.Empty;
		if (!IsNextChar(reader, '@'))
		{
			return;
		}
		ReadNextChar(reader);
		ParseOptionalWhitespace(reader);
		string text = ParseWord(reader);
		if (string.Equals(text, "message", StringComparison.OrdinalIgnoreCase))
		{
			ParseOptionalWhitespace(reader);
			ParseChar(reader, '(');
			ParseOptionalWhitespace(reader);
			text = ParseWord(reader, allowWhiteSpace: true);
			ParseChar(reader, ')');
			atMessage = text;
			atTime = (string.IsNullOrEmpty(atMessage) ? 0f : 31536000f);
		}
		else
		{
			if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				throw new ParserException("Can't convert " + text + " to a number");
			}
			atTime = result;
		}
	}

	private void ParseArrowModifier(StringReader reader, out string sendMessage)
	{
		sendMessage = string.Empty;
		if (IsNextChar(reader, '-'))
		{
			ReadNextChar(reader);
			if (!IsNextChar(reader, '>'))
			{
				throw new ParserException("Invalid modifier after command; expected @time, @Message(x), ->Message(x) or nothing");
			}
			ReadNextChar(reader);
			ParseOptionalWhitespace(reader);
			string a = ParseWord(reader);
			if (string.Equals(a, "Message", StringComparison.OrdinalIgnoreCase))
			{
				ParseOptionalWhitespace(reader);
				ParseChar(reader, '(');
				ParseOptionalWhitespace(reader);
				a = ParseWord(reader, allowWhiteSpace: true);
				sendMessage = a.Trim();
				ParseChar(reader, ')');
			}
		}
	}

	private void ParseSemicolonOrEnd(StringReader reader)
	{
		if (!HasNextChar(reader) || (ushort)reader.Peek() == 59)
		{
			ReadNextChar(reader);
			return;
		}
		throw new ParserException("Expected semicolon or end of sequence");
	}

	private bool CheckParseComment(StringReader reader)
	{
		if (!HasNextChar(reader) || (ushort)reader.Peek() == 47)
		{
			reader.Read();
			if (!HasNextChar(reader) || (ushort)reader.Peek() == 47)
			{
				reader.ReadLine();
				return true;
			}
		}
		return false;
	}
}
