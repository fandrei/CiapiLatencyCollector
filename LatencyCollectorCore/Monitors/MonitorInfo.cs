using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LatencyCollectorCore.Monitors
{
	public class MonitorInfo
	{
		public string Name { get; set; }
		public double PeriodSeconds { get; set; }

		public override string ToString()
		{
			return string.Format("{0} {1}\r\n", Name, PeriodSeconds);
		}

		public static string ToString(IEnumerable<MonitorInfo> vals)
		{
			if (vals == null)
				return null;

			var buf = new StringBuilder();
			foreach (var val in vals)
			{
				buf.Append(val.ToString());
			}
			return buf.ToString();
		}

		public static MonitorInfo[] Parse(string text)
		{
			var lines = text.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
			var monitors = new List<MonitorInfo>();
			foreach (var line in lines)
			{
				var columns = line.Split(new[] { '\t', ' ' });
				var monitor = new MonitorInfo
					{
						Name = columns[0],
						PeriodSeconds = double.Parse(columns[1]),
					};
				monitors.Add(monitor);
			}
			return monitors.ToArray();
		}
	}
}
