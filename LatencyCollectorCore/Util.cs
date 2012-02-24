using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LatencyCollectorCore
{
	public static class Util
	{
		public static bool IsNullOrEmpty(this string val)
		{
			return string.IsNullOrEmpty(val);
		}
	}
}
