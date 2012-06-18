using LatencyCollectorCore.Monitors;
using NUnit.Framework;

namespace LatencyCollectorCore.Tests
{
    [TestFixture]
    public class GTS_AllServiceMonitorFixture
    {
        [Test, Ignore("WIP: Should use playback so this can run without hitting the actual API")]
        public void ShouldExecuteWithoutExceptions()
        {
            var monitor = new GTS_AllServiceMonitor();

            monitor.Start();
            monitor.Execute();

            monitor.WaitForFinish();
        }
    }
}
