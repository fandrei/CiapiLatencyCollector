using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AppMetrics.Client;

namespace LatencyCollectorCore
{
	static class AppMetrics
	{
		public static Stopwatch StartMeasure()
		{
			return Stopwatch.StartNew();
		}

		public static void EndMeasure(Stopwatch watch, string label)
		{
			var diff = watch.Elapsed;
			watch.Stop();

			Tracker.Log("Latency " + label, diff.TotalSeconds);
		}

		public static readonly Tracker Tracker = new Tracker("http://metrics.labs.cityindex.com/LogEvent.ashx",
			"CiapiLatencyCollector");
	}
}
