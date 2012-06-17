using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;

namespace LatencyCollectorCore.Monitors
{
	public class DefaultPageMonitor : LatencyMonitor
	{
	    public override void Execute()
		{
			using (var client = new WebClient())
			{
				var watch = Program.Tracker.StartMeasure();
				var resp = client.DownloadString("https://ciapi.cityindex.com/");
				if (string.IsNullOrEmpty(resp))
					throw new ApplicationException("No response from default page");
				Program.Tracker.EndMeasure(watch, "DefaultPage");
			}
		}
	}
}
