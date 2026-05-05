using System.Collections.Generic;

namespace Language.Lua;

public class ParamList
{
	public List<string> NameList = new List<string>();

	public bool HasVarArg;

	public string IsVarArg;
}
