using System;
using System.Linq.Expressions;
using System.Text;

namespace Framework
{
	public static class Assert
	{
		public static void AreEqual(object exp, object act) { if (!Equals(exp, act)) throw new AssertionException($"Expected: {exp}, but got: {act}"); }
		public static void AreNotEqual(object exp, object act) { if (Equals(exp, act)) throw new AssertionException("Values should be not equal"); }
		public static void IsTrue(bool cond) { if (!cond) throw new AssertionException("Expected: True"); }
		public static void IsFalse(bool cond) { if (cond) throw new AssertionException("Expected: False"); }
		public static void IsNull(object obj) { if (obj != null) throw new AssertionException("Expected: Null"); }
		public static void IsNotNull(object obj) { if (obj == null) throw new AssertionException("Expected: Not Null"); }
		public static void GreaterThan(int val, int min) { if (val <= min) throw new AssertionException($"{val} is not greater than {min}"); }
		public static void Contains(string sub, string full) { if (!full.Contains(sub)) throw new AssertionException($"Substring '{sub}' not found"); }
		public static void IsInstanceOf<T>(object obj) { if (!(obj is T)) throw new AssertionException($"Object is not an instance of {typeof(T).Name}"); }
		public static void Throws<T>(Action act) where T : Exception
		{
			try 
			{ 
				act(); 
			} 
			catch (T) 
			{ 
				return; 
			}
			throw new AssertionException($"Expected exception {typeof(T).Name} was not thrown");
		}

		public static void That(Expression<Func<bool>> expr)
		{
			bool result;
			try { result = expr.Compile()(); }
			catch (Exception ex) { throw new AssertionException($"Expression threw: {ex.Message}\nExpression: {expr.Body}"); }

			if (!result)
				throw new AssertionException(BuildMessage(expr.Body));
		}

		private static string BuildMessage(Expression expr)
		{
			var sb = new StringBuilder();
			sb.AppendLine($"Expression : {expr}");
			sb.AppendLine($"NodeType   : {expr.NodeType}");

			if (expr is BinaryExpression bin)
			{
				sb.AppendLine($"Operator   : {OpSymbol(bin.NodeType)}");
				sb.AppendLine($"Left       : {bin.Left}  =>  {Eval(bin.Left)}");
				sb.AppendLine($"Right      : {bin.Right}  =>  {Eval(bin.Right)}");
			}
			else if (expr is UnaryExpression un)
			{
				sb.AppendLine($"Operator   : {OpSymbol(un.NodeType)}");
				sb.AppendLine($"Operand    : {un.Operand}  =>  {Eval(un.Operand)}");
			}
			else if (expr is MethodCallExpression call)
			{
				sb.AppendLine($"Method     : {call.Method.DeclaringType?.Name}.{call.Method.Name}");
				for (int i = 0; i < call.Arguments.Count; i++)
					sb.AppendLine($"Arg[{i}]     : {call.Arguments[i]}  =>  {Eval(call.Arguments[i])}");
			}

			return sb.ToString().TrimEnd();
		}

		private static string Eval(Expression e)
		{
			try 
			{ 
				return Expression.Lambda(e).Compile().DynamicInvoke()?.ToString() ?? "null"; 
			}
			catch { return "<error>"; }
		}

		private static string OpSymbol(ExpressionType t)
		{
			switch (t)
			{
				case ExpressionType.Equal: return "==";
				case ExpressionType.NotEqual: return "!=";
				case ExpressionType.GreaterThan: return ">";
				case ExpressionType.GreaterThanOrEqual: return ">=";
				case ExpressionType.LessThan: return "<";
				case ExpressionType.LessThanOrEqual: return "<=";
				case ExpressionType.AndAlso: return "&&";
				case ExpressionType.OrElse: return "||";
				case ExpressionType.Not: return "!";
				default: return t.ToString();
			}
		}
	}
}