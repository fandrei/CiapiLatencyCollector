using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LatencyCollectorCore
{
	static class NtpdInfo
	{
		public static void CheckTimeIsStable()
		{
			try
			{
				var now = DateTime.UtcNow;

				lock (Sync)
				{
					if (now - _lastCheckTime < CheckPeriod)
						return;
				}

				CheckMaxOffset();

				lock (Sync)
				{
					_lastCheckTime = now;
				}
			}
			catch (Exception exc)
			{
				Data.Tracker.Log("Exception", exc);
			}
		}

		private static void CheckMaxOffset()
		{
			var statFiles = new List<string>();
			FindStatFiles(StatsPath1, statFiles);
			FindStatFiles(StatsPath2, statFiles);

			var now = DateTime.UtcNow;

			if ((now - File.GetCreationTimeUtc(statFiles.Last())).TotalDays > 1)
				return;

			statFiles.RemoveAll(file => (now - File.GetCreationTimeUtc(file)).TotalDays > 2);
			statFiles.Sort();

			var text = new StringBuilder();
			foreach (var file in statFiles)
			{
				text.Append(File.ReadAllText(file));
			}

			if (text.Length == 0)
				return;

			CheckMaxOffset(text.ToString());
		}

		static void FindStatFiles(string dirPath, List<string> res)
		{
			if (!Directory.Exists(dirPath))
				return;

			res.AddRange(Directory.GetFiles(dirPath, "loopstats.*", SearchOption.TopDirectoryOnly));
		}

		static void CheckMaxOffset(string text)
		{
			var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			if (lines.Length == 0)
				throw new ApplicationException();

			var minOffset = double.MaxValue;
			var maxOffset = double.MinValue;

			foreach (var line in lines)
			{
				var offset = GetOffset(line);
				if (offset > maxOffset)
					maxOffset = offset;
				else if (offset < minOffset)
					minOffset = offset;
			}

			var lastOffset = GetOffset(lines.Last());
			var message = string.Format(CultureInfo.InvariantCulture, "ntpd: last {0}, min {1}, max {2}",
				lastOffset, minOffset, maxOffset);
			Data.Tracker.Log("Info", message);
		}

		private static double GetOffset(string line)
		{
			var columns = line.Split(' ');
			return double.Parse(columns[2]);
		}

		private const string StatsPath1 = @"C:\Program Files\NTP\etc\";
		private const string StatsPath2 = @"C:\Program Files (x86)\NTP\etc\";

		private static DateTime _lastCheckTime = DateTime.MinValue;
		private static readonly TimeSpan CheckPeriod = TimeSpan.FromMinutes(10);
		private static readonly object Sync = new object();
	}
}
