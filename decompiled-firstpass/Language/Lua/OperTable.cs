using System.Collections.Generic;

namespace Language.Lua;

public class OperTable
{
	private static Dictionary<string, int> precedence;

	private static Associativity[] associativity;

	static OperTable()
	{
		precedence = new Dictionary<string, int>();
		List<string[]> list = new List<string[]>
		{
			new string[1] { "or" },
			new string[1] { "and" },
			new string[2] { "==", "~=" },
			new string[4] { ">", ">=", "<", "<=" },
			new string[1] { ".." },
			new string[2] { "+", "-" },
			new string[3] { "*", "/", "%" },
			new string[2] { "#", "not" },
			new string[1] { "^" }
		};
		for (int i = 0; i < list.Count; i++)
		{
			string[] array = list[i];
			foreach (string key in array)
			{
				precedence.Add(key, i);
			}
		}
		associativity = new Associativity[list.Count];
		associativity[0] = Associativity.LeftAssociative;
		associativity[1] = Associativity.LeftAssociative;
		associativity[2] = Associativity.NonAssociative;
		associativity[3] = Associativity.LeftAssociative;
		associativity[4] = Associativity.LeftAssociative;
		associativity[5] = Associativity.LeftAssociative;
		associativity[6] = Associativity.LeftAssociative;
		associativity[7] = Associativity.NonAssociative;
		associativity[8] = Associativity.RightAssociative;
	}

	public static bool Contains(string oper)
	{
		return precedence.ContainsKey(oper);
	}

	public static bool IsPrior(string operLeft, string operRight)
	{
		if (operLeft == null)
		{
			return false;
		}
		if (operRight == null)
		{
			return true;
		}
		int num = precedence[operLeft];
		int num2 = precedence[operRight];
		if (num > num2)
		{
			return true;
		}
		if (num < num2)
		{
			return false;
		}
		return associativity[num] switch
		{
			Associativity.LeftAssociative => true, 
			Associativity.RightAssociative => false, 
			_ => true, 
		};
	}
}
