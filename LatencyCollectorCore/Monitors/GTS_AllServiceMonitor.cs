using System;
using System.Net;

using AppMetrics.Client;

namespace LatencyCollectorCore.Monitors
{
	public class GTS_AllServiceMonitor : LatencyMonitor
	{
		public override void Execute()
		{
			var watch = Tracker.StartMeasure();
			var resp = HttpUtil.Request("http://www.google.com");
			if (string.IsNullOrEmpty(resp))
				throw new ApplicationException("No response from default page");
			Tracker.EndMeasure(watch, "GTS.DefaultPage");
		}
	}
}
