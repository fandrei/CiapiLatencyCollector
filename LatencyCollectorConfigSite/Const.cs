using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Hosting;

namespace LatencyCollectorConfigSite
{
	public class Const
	{
		public static string StopFileName
		{
			get
			{
				const string tmp = "~/CIAPILatencyCollectorConfig/stop.txt";
				return HostingEnvironment.MapPath(tmp);
			}
		}
	}
}