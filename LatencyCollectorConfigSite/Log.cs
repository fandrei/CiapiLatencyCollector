using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LatencyCollectorConfigSite
{
	public static class Log
	{
		public static void Report(object val)
		{
			var msg = val.ToString();
			Trace.WriteLine(msg);
		}
	}
}