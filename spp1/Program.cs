using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Framework;

namespace Runner
{
	class Program
	{
		private static readonly object _consoleLock = new object();

		static async Task Main()
		{
			var assembly = typeof(Tests.LibraryTests).Assembly;

			Console.WriteLine("RUNNING SEQUENTIAL");
			var sw = Stopwatch.StartNew();
			await RunAll(assembly, 1);
			sw.Stop();
			long seqTime = sw.ElapsedMilliseconds;

			Console.WriteLine($"\nSequential Time: {seqTime} ms\n");

			int maxParallelism = 4;
			Console.WriteLine($"RUNNING PARALLEL (MaxDegree: {maxParallelism})");
			sw.Restart();
			await RunAll(assembly, maxParallelism);
			sw.Stop();
			long parTime = sw.ElapsedMilliseconds;

			Console.WriteLine($"\nParallel Time: {parTime} ms");
			Console.WriteLine($"Efficiency: {((double)seqTime / parTime):F2}x faster");

			Console.ReadKey();
		}

		static async Task RunAll(Assembly assembly, int degree)
		{
			var testClasses = assembly.GetTypes().Where(t => t.GetCustomAttribute<TestClassAttribute>() != null);
			var allTests = new List<TestEntry>();

			foreach (var type in testClasses)
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
						allTests.Add(new TestEntry { Type = type, Method = m, Params = tc.Params, Setup = setup, Cleanup = cleanup });
				}
			}

			using (var semaphore = new SemaphoreSlim(degree))
			{
				var tasks = allTests.Select(async test =>
				{
					await semaphore.WaitAsync();
					try { await ExecuteTest(test); }
					finally { semaphore.Release(); }
				});
				await Task.WhenAll(tasks);
			}
		}

		static async Task ExecuteTest(TestEntry entry)
		{
			var instance = Activator.CreateInstance(entry.Type);
			var testAttr = entry.Method.GetCustomAttribute<TestMethodAttribute>();
			var timeoutAttr = entry.Method.GetCustomAttribute<TimeoutAttribute>();
			string name = $"{entry.Method.Name}: {testAttr.Description}";

			try
			{
				entry.Setup?.Invoke(instance, null);

				var task = Task.Run(() => {
					object res = (entry.Params == null) ? entry.Method.Invoke(instance, null) : entry.Method.Invoke(instance, entry.Params);
					if (res is Task t) t.GetAwaiter().GetResult();
				});

				if (timeoutAttr != null)
				{
					if (await Task.WhenAny(task, Task.Delay(timeoutAttr.Milliseconds)) != task)
						throw new Exception($"Timeout exceeded ({timeoutAttr.Milliseconds}ms)");
				}

				await task;
				Log("[PASS]", ConsoleColor.Green, name);
			}
			catch (Exception ex)
			{
				var msg = ex.InnerException?.Message ?? ex.Message;
				Log("[FAIL]", ConsoleColor.Red, $"{name}. {msg}");
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
		public Type Type; 
		public MethodInfo Method; 
		public object[] Params; 
		public MethodInfo Setup; 
		public MethodInfo Cleanup;
	}
}