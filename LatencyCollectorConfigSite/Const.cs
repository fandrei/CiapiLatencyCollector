using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Hosting;

namespace LatencyCollectorConfigSite
{
	public class Const
	{
		public static string ConfigBasePath
		{
			get
			{
				const string tmp = "~/CIAPILatencyCollectorConfig/";
				return HostingEnvironment.MapPath(tmp);
			}
		}

		public static string StopFileName
		{
			get
			{
				return ConfigBasePath + "stop.txt";
			}
		}
	}
}