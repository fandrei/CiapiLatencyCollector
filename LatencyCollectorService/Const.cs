﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CiapiLatencyCollector
{
	class Const
	{
		public const string AutoUpdateUrl = 
			"http://ci.labs.cityindex.com:8080/job/CiapiLatencyCollector/ws/_bin/Release/LatencyCollectorCore.exe";

		public static string WorkingAreaPath
		{
			get
			{
				var basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
				var res = basePath + @"\City Index\CIAPI Latency Collector\";
				return res;
			}
		}

		public static string WorkerAssemblyPath
		{
			get { return WorkingAreaPath + @"\\LatencyCollectorCore.exe"; }
		}
	}
}