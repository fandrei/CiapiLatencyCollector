using System;
using System.Diagnostics;

using LatencyCollectorCore.Monitors;
using NUnit.Framework;
using Salient.ReflectiveLoggingAdapter;

namespace LatencyCollectorCore.Tests
{
	[TestFixture]
	public class AllServiceMonitorFixture
	{
		static AllServiceMonitorFixture()
		{
			var listeners = new[] { new TextWriterTraceListener(Console.Out) };
			Debug.Listeners.AddRange(listeners);

			LogManager.CreateInnerLogger =
				(logName, logLevel, showLevel, showDateTime, showLogName, dateTimeFormat) =>
				new SimpleTraceAppender(logName, logLevel, showLevel, showDateTime, showLogName, dateTimeFormat);
		}

		[Test]
		public void ShouldExecuteWithoutExceptions()
		{
			var monitor = new AllServiceMonitor
				{
					ServerUrl = TestSettings.CiapiTestUrl,
					StreamingServerUrl = TestSettings.CiapiTestStreamingUrl,
					UserName = TestSettings.CiapiTestUserName,
					Password = TestSettings.CiapiTestPassword,
					AllowTrading = true
				};
			monitor.Execute();
		}
	}
}
