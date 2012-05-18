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
				AppMetrics.Tracker.Log("Exception", exc);
			}
		}

		private static void CheckMaxOffset()
		{
			var statFiles = new List<string>();
			FindStatFiles(StatsPath1, statFiles);
			FindStatFiles(StatsPath2, statFiles);

			if (statFiles.Count == 0)
				return;

			var now = DateTime.UtcNow;

			statFiles.Sort(); // file name reflects date of that file

			statFiles.RemoveAll(file => (now - File.GetCreationTimeUtc(file)).TotalDays > 2);

			if (statFiles.Count > 2)
				statFiles.RemoveRange(0, statFiles.Count - 2);

			var text = new StringBuilder();
			foreach (var file in statFiles)
			{
				using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					using (var reader = new StreamReader(stream))
					{
						var curText = reader.ReadToEnd();
						text.Append(curText);
					}
				}
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
			var lines = ReadLines(text, TimeSpan.FromHours(4));
			if (lines.Count() == 0)
				return;
			if ((DateTime.UtcNow - GetLineTime(lines.Last())).TotalHours > 1)
				return;

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
			AppMetrics.Tracker.LogFormat("Info", "ntpd: last {0}, min {1}, max {2}", lastOffset, minOffset, maxOffset);
		}

		private static string[] ReadLines(string text, TimeSpan period)
		{
			var now = DateTime.UtcNow;
			var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			var res = new List<string>();

			foreach (var line in lines)
			{
				var lineTime = GetLineTime(line);
				if (now - lineTime < period)
					res.Add(line);
			}

			return res.ToArray();
		}

		private static DateTime GetLineTime(string line)
		{
			// see http://en.wikipedia.org/wiki/Julian_day
			var columns = line.Split(' ');
			var modifiedJulianDate = int.Parse(columns[0]);
			var date = new DateTime(1858, 11, 17).AddDays(modifiedJulianDate);
			var timeNumber = double.Parse(columns[1]);
			var res = date.AddSeconds(timeNumber);
			return res;
		}

		private static double GetOffset(string line)
		{
			var columns = line.Split(' ');
			return double.Parse(columns[2]);
		}

		private const string StatsPath1 = @"C:\Program Files\NTP\etc\";
		private const string StatsPath2 = @"C:\Program Files (x86)\NTP\etc\";

		private static DateTime _lastCheckTime = DateTime.MinValue;
		private static readonly TimeSpan CheckPeriod = TimeSpan.FromSeconds(10);
		private static readonly object Sync = new object();
	}
}
