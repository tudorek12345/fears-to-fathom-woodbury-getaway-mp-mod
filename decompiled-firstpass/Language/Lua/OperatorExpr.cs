using System.Collections.Generic;

namespace Language.Lua;

public class OperatorExpr : Expr
{
	public LinkedList<object> Terms = new LinkedList<object>();

	public void Add(string oper)
	{
		Terms.AddLast(oper);
	}

	public void Add(Term term)
	{
		Terms.AddLast(term);
	}

	public Term BuildExpressionTree()
	{
		LinkedListNode<object> first = Terms.First;
		Term term = first.Value as Term;
		if (Terms.Count == 1)
		{
			return term;
		}
		if (term != null)
		{
			return BuildExpressionTree(first.Value as Term, first.Next);
		}
		if (first.Value is string)
		{
			return BuildExpressionTree(null, first);
		}
		return null;
	}

	private static Term BuildExpressionTree(Term leftTerm, LinkedListNode<object> node)
	{
		string text = node.Value as string;
		LinkedListNode<object> next = node.Next;
		Term term = next.Value as Term;
		if (next.Next == null)
		{
			return new Operation(text, leftTerm, term);
		}
		string operRight = next.Next.Value as string;
		if (OperTable.IsPrior(text, operRight))
		{
			return BuildExpressionTree(new Operation(text, leftTerm, term), next.Next);
		}
		return new Operation(text, leftTerm, BuildExpressionTree(term, next.Next));
	}

	public override LuaValue Evaluate(LuaTable enviroment)
	{
		return BuildExpressionTree().Evaluate(enviroment);
	}

	public override Term Simplify()
	{
		return BuildExpressionTree().Simplify();
	}
}
