using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace LatencyCollectorCore.Monitors
{
	[XmlInclude(typeof(DefaultPageMonitor))]
	[XmlInclude(typeof(AuthenticatedMonitor))]
	[XmlInclude(typeof(AllServiceMonitor))]
	public abstract class LatencyMonitor
	{
		public LatencyMonitor()
		{
			PeriodSeconds = 10;
		}

		public abstract void Execute();

		public int PeriodSeconds { get; set; }

		[XmlIgnore]
		public DateTime LastExecution { get; set; }
	}
}
