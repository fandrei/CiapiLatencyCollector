using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace LatencyCollectorCore.Monitors
{
	public class MonitorSettings : IDisposable
	{
		public MonitorSettings()
		{
			Monitors = new LatencyMonitor[0];
		}

		public string LogEventUrl { get; set; }
		public string ApplicationKey { get; set; }

		public LatencyMonitor[] Monitors { get; set; }

		[XmlIgnore]
		public bool PollingDisabled { get; set; }

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
