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
		public string PageUrl { get; set; }

		public override void Execute()
		{
			try
			{
				if (string.IsNullOrEmpty(PageUrl))
					throw new ApplicationException("DefaultPageMonitor: PageUrl is not set");
				if (AppSettings.Instance.MonitorSettings.PollingDisabled)
					return;

				var request = (HttpWebRequest)WebRequest.Create(PageUrl);
				request.Method = "HEAD";

				var watch = Tracker.StartMeasure();
				using (var response = request.GetResponse())
				{
					Tracker.EndMeasure(watch, "General.DefaultPage");
				}
			}
			catch (WebException exc)
			{
				if (!WebUtil.IsConnectionFailure(exc))
					throw;
			}
		}
	}
}
