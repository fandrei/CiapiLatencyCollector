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
		public static bool IsTimeStable()
		{
			var now = DateTime.UtcNow;

			lock (Sync)
			{
				if (now - _lastCheckTime < CheckPeriod)
					return _cachedResult;
			}

			var res = CheckTimeIsStable();
			lock (Sync)
			{
				_cachedResult = res;
				_lastCheckTime = now;
			}
			return res;
		}

		private static bool CheckTimeIsStable()
		{
			var statFiles = new List<string>();
			FindStatFiles(StatsPath1, statFiles);
			FindStatFiles(StatsPath2, statFiles);

			var now = DateTime.UtcNow;
			statFiles.RemoveAll(file => (now - File.GetCreationTimeUtc(file)).TotalDays > 2);

			statFiles.Sort((x, y) => File.GetCreationTimeUtc(x).CompareTo(File.GetCreationTimeUtc(y)));

			if (statFiles.Count == 0)
				return false;

			try
			{
				return statFiles.All(MaxOffsetIsValid);
			}
			catch (Exception exc)
			{
				Data.Tracker.Log("Exception", exc);
				return false;
			}
		}

		static void FindStatFiles(string dirPath, List<string> res)
		{
			if (!Directory.Exists(dirPath))
				return;

			res.AddRange(Directory.GetFiles(dirPath, "loopstats.*", SearchOption.TopDirectoryOnly));
		}

		static bool MaxOffsetIsValid(string filePath)
		{
			var text = File.ReadAllText(filePath);
			var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var line in lines)
			{
				var columns = line.Split(' ');
				var offset = double.Parse(columns[2]);
				if (Math.Abs(offset) >= MaxOffset)
				{
					var fileName = Path.GetFileName(filePath);
					var message = string.Format(CultureInfo.InvariantCulture,
						"ntpd: max offset exceeded, {0} in \"{1}\"", offset, fileName);
					Data.Tracker.Log("Event", message);
					return false;
				}
			}

			return true;
		}

		private const string StatsPath1 = @"C:\Program Files\NTP\etc\";
		private const string StatsPath2 = @"C:\Program Files (x86)\NTP\etc\";

		private const double MaxOffset = 0.01;

		private static DateTime _lastCheckTime = DateTime.MinValue;
		private static readonly TimeSpan CheckPeriod = TimeSpan.FromMinutes(5);
		private static bool _cachedResult;
		private static readonly object Sync = new object();
	}
}
