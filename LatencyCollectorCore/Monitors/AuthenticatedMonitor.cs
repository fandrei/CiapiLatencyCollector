using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LatencyCollectorCore.Monitors
{
	public abstract class AuthenticatedMonitor : LatencyMonitor
	{
		protected AuthenticatedMonitor()
		{
			UserName = "";
			Password = "";
		}

		public string UserName { get; set; }
		public string Password { get; set; }
	}
}
