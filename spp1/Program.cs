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
			var assembly = typeof(Tests.LibraryTests).Assembly;
			var testEntries = CollectTests(assembly);

			Thread monitor = new Thread(() => {
				while (true)
				{
					Console.Title = $"Threads: {pool.CurrentThreadCount} | Queue: {pool.QueueCount} | P:{_passed} F:{_failed} S:{_skipped}";
					Thread.Sleep(200);
				}
			})
			{ IsBackground = true };
			monitor.Start();

			Console.WriteLine("\nPHASE 1: Single submissions (Low load)");
			for (int i = 0; i < 5; i++)
			{
				var entry = testEntries[i % testEntries.Count];
				pool.Enqueue(() => ExecuteTest(entry));
				Thread.Sleep(1000); 
			}

			Console.WriteLine("\nPHASE 2: PEAK LOAD! (Submitting 50 tasks)");
			for (int i = 0; i < 80; i++)
			{
				var entry = testEntries[i % testEntries.Count];
				pool.Enqueue(() => ExecuteTest(entry));
			}

			Console.WriteLine("\nPHASE 3: Inactivity period (Waiting for pool to scale down)");
			while (pool.QueueCount > 0) { Thread.Sleep(500); }
			Thread.Sleep(5000);

			Console.WriteLine("\nPHASE 4: Hanging thread simulation");
			pool.Enqueue(() => {
				Log("[BUSY]", ConsoleColor.Cyan, "Hanging: Infinite loop task started");
				while (true)
				{
					Thread.Sleep(1000);
				}
			});
			Thread.Sleep(6000);

			Console.WriteLine("\nALL PHASES COMPLETED");
			Console.WriteLine($"Stats - Passed: {_passed}, Failed: {_failed}, Skipped: {_skipped}");
			Console.WriteLine("Press any key to exit");
			Console.ReadKey();
		}

		static List<TestEntry> CollectTests(Assembly assembly)
		{
			var list = new List<TestEntry>();
			var types = assembly.GetTypes().Where(t => t.GetCustomAttribute<TestClassAttribute>() != null);
			foreach (var type in types)
			{
				var methods = type.GetMethods();
				var setup = methods.FirstOrDefault(m => m.GetCustomAttribute<BeforeEachAttribute>() != null);
				var cleanup = methods.FirstOrDefault(m => m.GetCustomAttribute<AfterEachAttribute>() != null);
				var tests = methods.Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null);
				foreach (var m in tests)
				{
					var cases = m.GetCustomAttributes<TestCaseAttribute>().ToList();
					if (!cases.Any()) cases.Add(new TestCaseAttribute(null));
					foreach (var tc in cases)
						list.Add(new TestEntry { Type = type, Method = m, Params = tc.Params, Setup = setup, Cleanup = cleanup });
				}
			}
			return list;
		}

		static void ExecuteTest(TestEntry entry)
		{
			var testAttr = entry.Method.GetCustomAttribute<TestMethodAttribute>();
			var timeoutAttr = entry.Method.GetCustomAttribute<TimeoutAttribute>();
			var ignoreAttr = entry.Method.GetCustomAttribute<IgnoreAttribute>();
			string name = $"{entry.Method.Name}: {testAttr.Description}";

			if (ignoreAttr != null)
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
				Thread testWorker = new Thread(() => {
					try
					{
						object result = (entry.Params == null)
							? entry.Method.Invoke(instance, null)
							: entry.Method.Invoke(instance, entry.Params);

						if (result is Task t) t.GetAwaiter().GetResult();
					}
					catch (Exception ex)
					{
						threadException = ex.InnerException ?? ex;
					}
				});

				testWorker.Start();

				if (timeoutAttr != null)
				{
					bool finishedInTime = testWorker.Join(timeoutAttr.Milliseconds);

					if (!finishedInTime)
					{
						throw new Exception($"Timeout exceeded ({timeoutAttr.Milliseconds}ms)");
					}
				}
				else
				{
					testWorker.Join();
				}

				if (threadException != null) throw threadException;

				Log("[PASS]", ConsoleColor.Green, name);
				Interlocked.Increment(ref _passed);
			}
			catch (Exception ex)
			{
				var msg = ex.InnerException?.Message ?? ex.Message;
				Log("[FAIL]", ConsoleColor.Red, $"{name}. Error: {msg}");
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
	}

	class TestEntry
	{
		public Type Type; public MethodInfo Method; public object[] Params;
		public MethodInfo Setup; public MethodInfo Cleanup;
	}
}