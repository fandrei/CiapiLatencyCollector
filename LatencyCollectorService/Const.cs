using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CiapiLatencyCollector
{
	class Const
	{
		public const string AppName = "CIAPI Latency Collector";

		public const string AutoUpdateBaseUrl = "http://ci.labs.cityindex.com:8080/job/CiapiLatencyCollector/ws/_bin/Release/";

		public static string WorkingAreaBinPath
		{
			get
			{
				var basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
				var res = basePath + @"\City Index\CIAPI Latency Collector\bin\";
				return res;
			}
		}

		public static string WorkerAssemblyPath
		{
			get { return WorkingAreaBinPath + @"\\CiapiLatencyCollectorCore.exe"; }
		}
	}
}
