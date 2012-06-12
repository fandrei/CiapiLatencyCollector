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
                                  ServerUrl = "https://ciapipreprod.cityindextest9.co.uk/tradingapi",
                                  StreamingServerUrl = "https://pushpreprod.cityindextest9.co.uk",
                                  UserName = "DM366183",
                                  Password = "password",
                                  AllowTrading = true
                              };
            monitor.Execute();
        }
    }
}
