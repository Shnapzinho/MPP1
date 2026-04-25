using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using MyThreadPool;
using Framework;

namespace Runner
{
	class Program
	{
		private static readonly object _consoleLock = new object();
		private static int _passed = 0;
		private static int _failed = 0;
		private static int _skipped = 0;

		static void Main()
		{
			using var pool = new CustomThreadPool(2, 10, 2000);

			pool.ScaledUp += (_, e) => LogPool(ConsoleColor.Cyan, $"Scale UP -> {e.Threads} threads");
			pool.ScaledDown += (_, e) => LogPool(ConsoleColor.DarkCyan, $"Scale DOWN -> {e.Threads} threads");
			pool.ThreadReplaced += (_, e) => LogPool(ConsoleColor.Magenta, e.Message);
			pool.PoolStopped += (_, e) => LogPool(ConsoleColor.Yellow, "Pool stopped");

			var assembly = typeof(Tests.LibraryTests).Assembly;
			var allTests = CollectTests(assembly);

			new Thread(() => {
				while (true)
				{
					Console.Title = $"Threads: {pool.CurrentThreadCount} | Queue: {pool.QueueCount} | P:{_passed} F:{_failed} S:{_skipped}";
					Thread.Sleep(500);
				}
			})
			{ IsBackground = true }.Start();

			Console.WriteLine("\nPHASE 1: Single submissions (Low load)");
			for (int i = 0; i < 5; i++)
			{
				var entry = allTests[i % allTests.Count];
				pool.Enqueue(() => RunTest(entry));
				Thread.Sleep(1000);
			}

			Console.WriteLine("\nPHASE 2: Filter by Category = \"Library\"");
			Func<TestEntry, bool> catFilter = entry => entry.Method.GetCustomAttribute<TestMethodAttribute>()?.Category == "Library";
			foreach (var entry in allTests.Where(catFilter)) 
			{ 
				var x = entry; 
				pool.Enqueue(() => RunTest(x)); 
			}
			WaitEmpty(pool);

			Console.WriteLine("\nPHASE 3: Filter by Priority >= 3");
			Func<TestEntry, bool> priFilter = entry => (entry.Method.GetCustomAttribute<TestMethodAttribute>()?.Priority ?? 0) >= 3;
			foreach (var entry in allTests.Where(priFilter)) 
			{ 
				var x = entry; 
				pool.Enqueue(() => RunTest(x)); 
			}
			WaitEmpty(pool);

			Console.WriteLine("\nPHASE 4: PEAK LOAD! (Submitting 60 tasks)");
			for (int i = 0; i < 60; i++) 
			{
				var entry = allTests[i % allTests.Count];
				pool.Enqueue(() => RunTest(entry));
			}
			WaitEmpty(pool);

			Console.WriteLine("\nPHASE 5: Inactivity period (Waiting for pool to scale down)");
			Thread.Sleep(6000);

			Console.WriteLine("\nPHASE 6: Hanging thread simulation");
			pool.Enqueue(() => 
			{
				Log("[BUSY]", ConsoleColor.Cyan, "Hanging: Infinite loop task started"); 
				while (true) Thread.Sleep(1000); 
			});
			Thread.Sleep(7000);

			Console.WriteLine("\nALL PHASES COMPLETED");
			Console.WriteLine($"Stats - Passed: {_passed}, Failed: {_failed}, Skipped: {_skipped}");
			Console.WriteLine("Press any key to exit");
			Console.ReadKey();
		}

		static void WaitEmpty(CustomThreadPool pool) { while (pool.QueueCount > 0) Thread.Sleep(300); Thread.Sleep(500); }

		static List<TestEntry> CollectTests(Assembly assembly)
		{
			var list = new List<TestEntry>();
			foreach (var type in assembly.GetTypes().Where(t => t.GetCustomAttribute<TestClassAttribute>() != null))
			{
				var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
				var setup = methods.FirstOrDefault(m => m.GetCustomAttribute<BeforeEachAttribute>() != null);
				var cleanup = methods.FirstOrDefault(m => m.GetCustomAttribute<AfterEachAttribute>() != null);

				foreach (var method in methods.Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null))
				{
					var dataSource = method.GetCustomAttribute<TestDataSourceAttribute>();
					if (dataSource != null)
					{
						var srcMethod = type.GetMethod(dataSource.MethodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
						if (srcMethod?.Invoke(null, null) is IEnumerable<object[]> rows)
						{
							foreach (var row in rows)
								list.Add(new TestEntry { Type = type, Method = method, Params = row, Setup = setup, Cleanup = cleanup });
							continue;
						}
					}

					var cases = method.GetCustomAttributes<TestCaseAttribute>().ToList();
					if (!cases.Any()) cases.Add(new TestCaseAttribute((object[])null));
					foreach (var tc in cases)
						list.Add(new TestEntry { Type = type, Method = method, Params = tc.Params, Setup = setup, Cleanup = cleanup });
				}
			}
			return list;
		}

		static void RunTest(TestEntry entry)
		{
			var attr = entry.Method.GetCustomAttribute<TestMethodAttribute>();
			var timeout = entry.Method.GetCustomAttribute<TimeoutAttribute>();
			string name = $"{entry.Method.Name}: {attr?.Description}";

			if (entry.Method.GetCustomAttribute<IgnoreAttribute>() != null)
			{ 
				Log("[SKIP]", ConsoleColor.Yellow, name); 
				Interlocked.Increment(ref _skipped); 
				return; 
			}

			var instance = Activator.CreateInstance(entry.Type);
			try
			{
				entry.Setup?.Invoke(instance, null);
				Exception threadException = null;
				var worker = new Thread(() =>
				{
					try
					{
						object result = (entry.Params == null)
							? entry.Method.Invoke(instance, null)
							: entry.Method.Invoke(instance, entry.Params);
						if (result is System.Threading.Tasks.Task t) 
							t.GetAwaiter().GetResult();
					}
					catch (Exception ex) { threadException = ex.InnerException ?? ex; }
				});
				worker.Start();
				if (timeout != null)
				{
					if (!worker.Join(timeout.Milliseconds))
						throw new Exception($"Timeout exceeded ({timeout.Milliseconds}ms)");
				}
				else worker.Join();
				if (threadException != null) 
					throw threadException;

				Log("[PASS]", ConsoleColor.Green, name);
				Interlocked.Increment(ref _passed);
			}
			catch (Exception ex)
			{
				Log("[FAIL]", ConsoleColor.Red, $"{name}. Error: {ex.InnerException?.Message ?? ex.Message}");
				Interlocked.Increment(ref _failed);
			}
			finally { entry.Cleanup?.Invoke(instance, null); }
		}

		static void Log(string status, ConsoleColor color, string msg)
		{
			lock (_consoleLock)
			{
				Console.ForegroundColor = color;
				Console.Write(status + " ");
				Console.ResetColor();
				Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] {msg}");
			}
		}

		static void LogPool(ConsoleColor color, string msg)
		{
			lock (_consoleLock)
			{
				Console.ForegroundColor = color;
				Console.Write("[POOL] ");
				Console.ResetColor();
				Console.WriteLine(msg);
			}
		}
	}

	class TestEntry
	{
		public Type Type;
		public MethodInfo Method;
		public object[] Params;
		public MethodInfo Setup;
		public MethodInfo Cleanup;
	}
}