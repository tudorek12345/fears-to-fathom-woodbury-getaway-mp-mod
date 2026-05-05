using System.Collections.Generic;
using System.IO;

namespace Language.Lua.Library;

public static class FileLib
{
	public static void RegisterModule(LuaTable enviroment)
	{
		LuaTable luaTable = new LuaTable();
		RegisterFunctions(luaTable);
		enviroment.SetNameValue("file", luaTable);
	}

	public static void RegisterFunctions(LuaTable module)
	{
		module.Register("close", close);
		module.Register("read", read);
		module.Register("write", write);
		module.Register("lines", lines);
		module.Register("flush", flush);
		module.Register("seek", seek);
	}

	public static LuaTable CreateMetaTable()
	{
		LuaTable luaTable = new LuaTable();
		RegisterFunctions(luaTable);
		luaTable.SetNameValue("__index", luaTable);
		return luaTable;
	}

	public static LuaValue close(LuaValue[] values)
	{
		LuaUserdata luaUserdata = values[0] as LuaUserdata;
		if (luaUserdata.Value is TextReader)
		{
			return null;
		}
		_ = luaUserdata.Value is TextWriter;
		return null;
	}

	public static LuaValue read(LuaValue[] values)
	{
		TextReader textReader = (values[0] as LuaUserdata).Value as TextReader;
		LuaNumber luaNumber = ((values.Length > 1) ? (values[1] as LuaNumber) : null);
		if (luaNumber != null)
		{
			if (luaNumber.Number == 0.0)
			{
				return LuaString.Empty;
			}
			if (textReader.Peek() == -1)
			{
				return LuaNil.Nil;
			}
			char[] array = new char[(int)luaNumber.Number];
			int length = textReader.ReadBlock(array, 0, array.Length);
			return new LuaString(new string(array, 0, length));
		}
		LuaString luaString = ((values.Length > 1) ? (values[1] as LuaString) : null);
		switch ((luaString == null) ? "*l" : luaString.Text)
		{
		case "*l":
			if (textReader.Peek() == -1)
			{
				return LuaNil.Nil;
			}
			return new LuaString(textReader.ReadLine());
		case "*a":
			return new LuaString(textReader.ReadToEnd());
		case "*n":
		{
			List<char> list = new List<char>();
			int num = textReader.Peek();
			while (num >= 48 && num <= 57)
			{
				list.Add((char)textReader.Read());
				num = textReader.Peek();
			}
			return new LuaNumber(int.Parse(new string(list.ToArray())));
		}
		default:
			return null;
		}
	}

	public static LuaValue lines(LuaValue[] values)
	{
		LuaUserdata data = values[0] as LuaUserdata;
		LuaFunction luaFunction = new LuaFunction(delegate
		{
			string text = (data.Value as TextReader).ReadLine();
			return (text != null) ? ((LuaValue)new LuaString(text)) : ((LuaValue)LuaNil.Nil);
		});
		return new LuaMultiValue(new LuaValue[3]
		{
			luaFunction,
			data,
			LuaNil.Nil
		});
	}

	public static LuaValue seek(LuaValue[] values)
	{
		LuaUserdata luaUserdata = values[0] as LuaUserdata;
		Stream stream = null;
		if (luaUserdata.Value is StreamWriter streamWriter)
		{
			stream = streamWriter.BaseStream;
		}
		else if (luaUserdata.Value is StreamReader streamReader)
		{
			stream = streamReader.BaseStream;
		}
		if (stream != null)
		{
			LuaString luaString = ((values.Length > 1) ? (values[1] as LuaString) : null);
			string whence = ((luaString == null) ? "cur" : luaString.Text);
			LuaNumber luaNumber = ((values.Length > 1 && luaString == null) ? (values[1] as LuaNumber) : null);
			luaNumber = ((values.Length > 2 && luaNumber == null) ? (values[2] as LuaNumber) : null);
			long offset = ((luaNumber == null) ? 0 : ((long)luaNumber.Number));
			stream.Seek(offset, GetSeekOrigin(whence));
		}
		return null;
	}

	private static SeekOrigin GetSeekOrigin(string whence)
	{
		return whence switch
		{
			"set" => SeekOrigin.Begin, 
			"end" => SeekOrigin.End, 
			_ => SeekOrigin.Current, 
		};
	}

	public static LuaValue write(LuaValue[] values)
	{
		TextWriter textWriter = (values[0] as LuaUserdata).Value as TextWriter;
		for (int i = 1; i < values.Length; i++)
		{
			textWriter.Write(values[i].Value);
		}
		return null;
	}

	public static LuaValue flush(LuaValue[] values)
	{
		((values[0] as LuaUserdata).Value as TextWriter).Flush();
		return null;
	}
}
