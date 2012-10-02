using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CiapiLatencyCollector
{
	public class Const
	{
		public const string AppName = "CIAPI Latency Collector";

		public static string WorkingAreaPath
		{
			get
			{
				var basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
				var res = basePath + @"\City Index\CIAPI Latency Collector\";
				return res;
			}
		}

		public static string WorkingAreaBinPath
		{
			get { return WorkingAreaPath + @"\bin\"; }
		}

		public static string WorkingAreaTempPath
		{
			get { return WorkingAreaPath + @"\temp\"; }
		}

		public static string WorkerAssemblyPath
		{
			get { return WorkingAreaBinPath + @"\\CiapiLatencyCollectorCore.exe"; }
		}
	}
}
