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
		public DefaultPageMonitor()
		{
			PageUrl = "https://ciapi.cityindex.com/";
		}

		public string PageUrl { get; set; }

		public override void Execute()
		{
			try
			{
				using (var client = new WebClient())
				{
					var watch = Tracker.StartMeasure();
					var resp = client.DownloadString(PageUrl);
					if (string.IsNullOrEmpty(resp))
						throw new ApplicationException("No response from default page");
					Tracker.EndMeasure(watch, "General.DefaultPage");
				}
			}
			catch (WebException exc)
			{
				if (!Util.IsConnectionFailure(exc))
					throw;
			}
		}
	}
}
