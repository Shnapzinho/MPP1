using Framework;
using System.Threading;

namespace Tests
{
	[TestClass]
	public class PerformanceTests
	{
		[TestMethod(Description = "Long task 1 (1 second)")]
		public void LongTest1() => Thread.Sleep(1000);

		[TestMethod(Description = "Long task 2 (1 second)")]
		public void LongTest2() => Thread.Sleep(1000);

		[TestMethod(Description = "Long task 3 (1 second)")]
		public void LongTest3() => Thread.Sleep(1000);

		[Timeout(700)]
		[TestMethod(Description = "Should fail by timeout")]
		public void TimeoutTest1() => Thread.Sleep(1000);

		[TestMethod(Description = "Long task 4 (1 second)")]
		public void LongTest4() => Thread.Sleep(1000);

		[Timeout(500)]
		[TestMethod(Description = "Should fail by timeout")]
		public void TimeoutTest2() => Thread.Sleep(1000);
	}
}