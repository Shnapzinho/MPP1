using System;

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
			try { act(); } catch (T) { return; }
			throw new AssertionException($"Expected exception {typeof(T).Name} was not thrown");
		}
	}
}