using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;

namespace LatencyCollectorCore
{
	class DefaultPageMonitor : Monitor
	{
		public override void Execute()
		{
			using (var client = new WebClient())
			{
				var watch = AppMetrics.StartMeasure();
				var resp = client.DownloadString("https://ciapi.cityindex.com/");
				AppMetrics.EndMeasure(watch, "DefaultPage");
			}
		}
	}
}
