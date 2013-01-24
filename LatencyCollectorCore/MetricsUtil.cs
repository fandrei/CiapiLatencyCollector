using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AppMetrics.Client;

namespace LatencyCollectorCore
{
	static class MetricsUtil
	{
		public static void ReportNodeInfo(Tracker tracker)
		{
			tracker.Log("Info_UserId", PluginSettings.Instance.UserId);
			tracker.Log("Info_NodeName", PluginSettings.Instance.NodeName);
			var curAssembly = typeof(Program).Assembly;
			tracker.Log("Info_ProcessVersion", curAssembly.FullName);
		}
	}
}
