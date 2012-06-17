using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LatencyCollectorCore.Monitors
{
	public class MonitorSettings : IDisposable
	{
		public MonitorSettings()
		{
			Monitors = new LatencyMonitor[0];
		}

		public string LogEventUrl { get; set; }
		public LatencyMonitor[] Monitors { get; set; }

		public void Dispose()
		{
			if (Monitors != null)
			{
				foreach (var monitor in Monitors)
				{
					monitor.Dispose();
				}
			}
		}
	}
}
