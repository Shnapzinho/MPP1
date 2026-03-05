using Framework;
using Project;
using System;
using System.Threading.Tasks;

namespace Tests
{
	[TestClass]
	public class LibraryTests
	{
		private Database _db;
		private LibraryService _service;

		[BeforeEach]
		public void Setup()
		{
			_db = new Database();
			_service = new LibraryService(_db);
		}

		[TestMethod(Description = "Add multiple books using parameters")]
		[TestCase("C# 12 in a Nutshell", "Joseph Albahari")]
		[TestCase("Design Patterns", "Erich Gamma")]
		[TestCase("Refactoring", "Martin Fowler")]
		public async Task T1(string title, string author)
		{
			await _service.AddBookAsync(title, author);
			Assert.IsNotNull(_db.GetBooks().Find(b => b.Title == title));
		}

		[TestMethod(Description = "Register members with different names", Priority = 5)]
		[TestCase("Alice")]
		[TestCase("Bob")]
		[TestCase("Charlie")]
		public async Task T2(string name)
		{
			await _service.RegisterMemberAsync(name);
			Assert.Contains(name, _db.GetMembers()[0].Name);
		}

		[TestMethod(Description = "Math logic boundary checks")]
		[TestCase(10, 5)]
		[TestCase(100, 99)]
		[TestCase(1, 0)]
		public void T3(int a, int b) => Assert.GreaterThan(a, b);

		[TestMethod(Description = "Check service type and inequality")]
		public void T4()
		{
			Assert.IsInstanceOf<LibraryService>(_service);
			Assert.AreNotEqual("Current", "Old");
		}

		[TestMethod(Description = "Search finds correct partial matches")]
		[TestCase("Clean")]
		[TestCase("Robert")]
		public async Task T5(string query)
		{
			await _service.AddBookAsync("Clean Code", "Robert Martin");
			var res = _service.SearchBooks(query);
			Assert.IsTrue(res.Count == 1);
		}
		[Ignore]
		[TestMethod(Description = "Validate null and not null states")]
		public void T6()
		{
			Assert.IsNull(null);
			Assert.IsNotNull(_service);
		}

		[TestMethod(Description = "Boolean logic and string checks")]
		public void T7()
		{
			Assert.IsTrue(5 + 5 == 10);
			Assert.Contains("Test", "The final Test");
		}

		[TestMethod(Description = "Multiple exceptions validation")]
		[TestCase("")] 
		public void T8(string badTitle)
		{
			Assert.Throws<ArgumentException>(() => _service.AddBookAsync(badTitle, "Author").GetAwaiter().GetResult());
		}

		[TestMethod(Description = "Verify initial database state")]
		public void T9() => Assert.IsFalse(_db.GetBooks().Count > 0);

		[Ignore]
		[TestMethod(Description = "This test is ignored")]
		public void T10() => Assert.IsNotNull(null);


		[TestMethod(Description = "Boundary math check failures")]
		[TestCase(5, 10)]
		[TestCase(0, 0)]
		public void T11(int a, int b) => Assert.GreaterThan(a, b);

		[TestMethod(Description = "Searching for non-existent terms")]
		[TestCase("Python")]
		[TestCase("Java")]
		public async Task T12(string query)
		{
			await _service.AddBookAsync("C# Guide", "Author");
			var res = _service.SearchBooks(query);
			Assert.AreNotEqual(0, res.Count);
		}

		[TestMethod(Description = "Expected book count mismatch")]
		public void T13() => Assert.AreEqual(99, _db.GetBooks().Count);

		[TestMethod(Description = "Type mismatch check")]
		public void T14() => Assert.IsInstanceOf<string>(_service);

		[TestMethod(Description = "Expected null object check")]
		public void T15() => Assert.IsNull(_db);

		[TestMethod(Description = "Logic truth failure")]
		public void T16() => Assert.IsTrue(1 > 100);

		[TestMethod(Description = "Logic false failure")]
		public void T17() => Assert.IsFalse(true);

		[TestMethod(Description = "String content mismatch", Priority = 3)]
		public void T18() => Assert.Contains("Orange", "Apple Juice");

		[TestMethod(Description = "Book limit logic error demonstration")]
		public async Task T19()
		{
			await _service.RegisterMemberAsync("Member");
			Assert.Throws<Exception>(() => _service.BorrowBook(1, 1));
			Assert.AreEqual(1, _db.GetBooks().Count);
		}

		[TestMethod(Description = "Member name case sensitivity fail")]
		public async Task T20()
		{
			await _service.RegisterMemberAsync("ALICE");
			Assert.AreEqual("alice", _db.GetMembers()[0].Name);
		}

		[AfterEach]
		public void Cleanup() => _db.ClearAll();
	}
}