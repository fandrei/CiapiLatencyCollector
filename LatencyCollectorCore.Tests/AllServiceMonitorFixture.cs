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
            LogManager.CreateInnerLogger =
                (logName, logLevel, showLevel, showDateTime, showLogName, dateTimeFormat) =>
                new SimpleTraceAppender(logName, logLevel, showLevel, showDateTime, showLogName, dateTimeFormat);
        }

        [Test, Ignore("WIP: Should use playback so this can run without hitting the actual API")]
        public void ShouldExecuteWithoutExceptions()
        {
            var monitor = new AllServiceMonitor
                              {
                                  ServerUrl = "https://ciapi.cityindex.com/tradingapi",
                                  StreamingServerUrl = "https://push.cityindex.com",
                                  UserName = "DM603751",
                                  Password = "password",
                                  AllowTrading = true
                              };

            // sky: apparently you have to start the monitor before executing it
            monitor.Start();
            monitor.Execute();
        }
    }
}
