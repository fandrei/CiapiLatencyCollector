using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LatencyCollectorCore
{
	public abstract class Monitor
	{
		public abstract void Execute();
		public MonitorInfo Info { get; set; }
		public DateTime LastExecution { get; set; }
	}
}
