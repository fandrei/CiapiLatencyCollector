using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace LatencyCollectorCore.Monitors
{
	public abstract class AuthenticatedMonitor : LatencyMonitor
	{
		protected AuthenticatedMonitor()
		{
			UserName = "";
		}

		public string UserName { get; set; }
		public string Password { get; set; }
	}
}
