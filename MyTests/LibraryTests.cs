using Framework;
using Project;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tests
{
	[TestClass]
	public class LibraryTests
	{
		private Database _db;
		private LibraryService _service;

		[BeforeEach] public void Setup() { _db = new Database(); _service = new LibraryService(_db); }
		[AfterEach] public void Cleanup() => _db.ClearAll();


		public static IEnumerable<object[]> BookData()
		{
			yield return new object[] { "C# 12 in a Nutshell", "Joseph Albahari" };
			yield return new object[] { "Design Patterns", "Erich Gamma" };
			yield return new object[] { "Refactoring", "Martin Fowler" };
		}

		public static IEnumerable<object[]> MemberData()
		{
			yield return new object[] { "Alice" };
			yield return new object[] { "Bob" };
			yield return new object[] { "Charlie" };
		}

		public static IEnumerable<object[]> MathData()
		{
			yield return new object[] { 10, 5 };
			yield return new object[] { 100, 99 };
			yield return new object[] { 1, 0 };
		}


		[TestMethod(Description = "Add book (yield return)", Category = "Library", Author = "Shnapz", Priority = 1)]
		[TestDataSource(nameof(BookData))]
		public async Task YieldBook(string title, string author)
		{
			await _service.AddBookAsync(title, author);
			Assert.IsNotNull(_db.GetBooks().Find(b => b.Title == title));
		}

		[TestMethod(Description = "Register member (yield return)", Category = "Members", Author = "Shnapz", Priority = 5)]
		[TestDataSource(nameof(MemberData))]
		public async Task YieldMember(string name)
		{
			await _service.RegisterMemberAsync(name);
			Assert.Contains(name, _db.GetMembers()[0].Name);
		}

		[TestMethod(Description = "Math boundary (yield return)", Category = "Math", Author = "Shnapz")]
		[TestDataSource(nameof(MathData))]
		public void YieldMath(int a, int b) => Assert.GreaterThan(a, b);


		[TestMethod(Description = "Assert.That: pass", Category = "ExprTree", Author = "Shnapz", Priority = 1)]
		public void ExprPass() { int x = 42; Assert.That(() => x == 42); }

		[TestMethod(Description = "Assert.That: fail with detailed info", Category = "ExprTree", Author = "Shnapz", Priority = 1)]
		public void ExprFail() { int a = 10, b = 99; Assert.That(() => b == a); } 

		[TestMethod(Description = "Assert.That: method call", Category = "ExprTree", Author = "Shnapz")]
		public async Task ExprMethodCall()
		{
			await _service.AddBookAsync("Clean Code", "Robert Martin");
			var books = _db.GetBooks();
			Assert.That(() => books.Count > 0);
		}


		[TestMethod(Description = "Add multiple books using parameters", Category = "Library", Author = "Shnapz")]
		[TestCase("C# 12 in a Nutshell", "Joseph Albahari")]
		[TestCase("Design Patterns", "Erich Gamma")]
		[TestCase("Refactoring", "Martin Fowler")]
		public async Task T1(string title, string author)
		{
			await _service.AddBookAsync(title, author);
			Assert.IsNotNull(_db.GetBooks().Find(b => b.Title == title));
		}

		[TestMethod(Description = "Register members with different names", Category = "Members", Author = "Shnapz", Priority = 5)]
		[TestCase("Alice")]
		[TestCase("Bob")]
		[TestCase("Charlie")]
		public async Task T2(string name) { await _service.RegisterMemberAsync(name); Assert.Contains(name, _db.GetMembers()[0].Name); }

		[TestMethod(Description = "Math logic boundary checks", Category = "Math", Author = "Shnapz")]
		[TestCase(10, 5)]
		[TestCase(100, 99)]
		[TestCase(1, 0)]
		public void T3(int a, int b) => Assert.GreaterThan(a, b);

		[TestMethod(Description = "Check service type and inequality", Category = "General", Author = "Shnapz")]
		public void T4() { Assert.IsInstanceOf<LibraryService>(_service); Assert.AreNotEqual("Current", "Old"); }

		[TestMethod(Description = "Search finds correct partial matches", Category = "Library", Author = "Shnapz")]
		[TestCase("Clean")]
		[TestCase("Robert")]
		public async Task T5(string query)
		{
			await _service.AddBookAsync("Clean Code", "Robert Martin");
			Assert.IsTrue(_service.SearchBooks(query).Count == 1);
		}

		[Ignore]
		[TestMethod(Description = "Ignored test", Category = "General", Author = "Shnapz")]
		public void T6() { Assert.IsNull(null); Assert.IsNotNull(_service); }

		[TestMethod(Description = "Boolean logic and string checks", Category = "General", Author = "Shnapz")]
		public void T7() { Assert.IsTrue(5 + 5 == 10); Assert.Contains("Test", "The final Test"); }

		[TestMethod(Description = "Exception on empty title", Category = "Library", Author = "Shnapz")]
		[TestCase("")]
		public void T8(string bad) => Assert.Throws<ArgumentException>(() => _service.AddBookAsync(bad, "Author").GetAwaiter().GetResult());

		[TestMethod(Description = "Verify initial database state", Category = "General", Author = "Shnapz")]
		public void T9() => Assert.IsFalse(_db.GetBooks().Count > 0);

		[Ignore]
		[TestMethod(Description = "This test is ignored", Category = "General", Author = "Shnapz")]
		public void T10() => Assert.IsNotNull(null);

		[TestMethod(Description = "Boundary math failures", Category = "Math", Author = "Shnapz")]
		[TestCase(5, 10)]
		[TestCase(0, 0)]
		public void T11(int a, int b) => Assert.GreaterThan(a, b);

		[TestMethod(Description = "Search for non-existent terms", Category = "Library", Author = "Shnapz")]
		[TestCase("Python")]
		[TestCase("Java")]
		public async Task T12(string q) { await _service.AddBookAsync("C# Guide", "Author"); Assert.AreNotEqual(0, _service.SearchBooks(q).Count); }

		[TestMethod(Description = "Expected book count mismatch", Category = "General", Author = "Shnapz")] 
		public void T13() => Assert.AreEqual(99, _db.GetBooks().Count);

		[TestMethod(Description = "Type mismatch check", Category = "General", Author = "Shnapz")] 
		public void T14() => Assert.IsInstanceOf<string>(_service);

		[TestMethod(Description = "Expected null object check", Category = "General", Author = "Shnapz")] 
		public void T15() => Assert.IsNull(_db);

		[TestMethod(Description = "Logic truth failure", Category = "General", Author = "Shnapz")] 
		public void T16() => Assert.IsTrue(1 > 100);

		[TestMethod(Description = "Logic false failure", Category = "General", Author = "Shnapz")] 
		public void T17() => Assert.IsFalse(true);

		[TestMethod(Description = "String content mismatch", Category = "General", Author = "Shnapz", Priority = 3)]
		public void T18() => Assert.Contains("Orange", "Apple Juice");

		[TestMethod(Description = "Book limit logic error", Category = "Library", Author = "Shnapz")]
		public async Task T19() { await _service.RegisterMemberAsync("Member"); Assert.Throws<Exception>(() => _service.BorrowBook(1, 1)); Assert.AreEqual(1, _db.GetBooks().Count); }

		[TestMethod(Description = "Member name case sensitivity fail", Category = "Members", Author = "Shnapz")]
		public async Task T20() { await _service.RegisterMemberAsync("ALICE"); Assert.AreEqual("alice", _db.GetMembers()[0].Name); }
	}
}