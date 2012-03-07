using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LatencyCollectorCore
{
	public static class Util
	{
		public static bool IsNullOrEmpty(this string val)
		{
			return string.IsNullOrEmpty(val);
		}

		public static string GetAppLocation()
		{
			var location = Assembly.GetExecutingAssembly().CodeBase;
			location = (new Uri(location)).LocalPath;
			var res = Path.GetDirectoryName(location) + "\\";
			return res;
		}
	}
}
