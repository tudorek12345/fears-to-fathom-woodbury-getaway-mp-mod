using System;
using System.Linq.Expressions;
using System.Reflection;

namespace PixelCrushers.DialogueSystem;

public static class SymbolExtensions
{
	public static MethodInfo GetMethodInfo(Expression<Action> expression)
	{
		return GetMethodInfo((LambdaExpression)expression);
	}

	public static MethodInfo GetMethodInfo<T>(Expression<Action<T>> expression)
	{
		return GetMethodInfo((LambdaExpression)expression);
	}

	public static MethodInfo GetMethodInfo<T, TResult>(Expression<Func<T, TResult>> expression)
	{
		return GetMethodInfo((LambdaExpression)expression);
	}

	public static MethodInfo GetMethodInfo(LambdaExpression expression)
	{
		return ((expression.Body as MethodCallExpression) ?? throw new ArgumentException("Invalid Expression. Expression should consist of a Method call only.")).Method;
	}
}
