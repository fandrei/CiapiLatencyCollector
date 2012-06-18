using System;
using System.Net;

namespace LatencyCollectorCore.Monitors
{
    public class GTS_AllServiceMonitor : LatencyMonitor
    {
        public override void Execute()
        {
            using (var client = new WebClient())
            {
                var watch = Tracker.StartMeasure();
                var resp = client.DownloadString(new Uri("http://www.google.com"));
                if (string.IsNullOrEmpty(resp))
                    throw new ApplicationException("No response from default page");
                Tracker.EndMeasure(watch, "GTS_DefaultPage");
            }
        }
    }
}
