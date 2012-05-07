using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Timer = System.Timers.Timer;

namespace LatencyCollectorCore
{
	static class SntpClient
	{
		public static void Init()
		{
			_timer = new Timer { Interval = 1, AutoReset = false };

			_timer.Elapsed +=
				(state, args) => OnTimer();

			_timer.Start();
		}

		private static void OnTimer()
		{
			try
			{
				lock (Sync)
				{
					var now = DateTime.UtcNow;
					if (now - _lastRequestedOffset > OffsetUpdatePeriod)
					{
						RequestTime();
						_lastRequestedOffset = now;
					}
					else
					{
						if (_offsetInitialized)
						{
							var driftSeconds = (now - _lastUpdatedOffset).TotalSeconds * _timeDrift;
							_currentTimeOffset = _requestedTimeOffset + TimeSpan.FromSeconds(driftSeconds);
						}
					}
				}
			}
			catch (Exception exc)
			{
				ReportException(exc);
			}
			_timer.Start();
		}

		private static void RequestTime()
		{
			try
			{
				var cur = RequestTimeOffset(Server);
				lock (Sync)
				{
					_requestResults.Add(cur);
					UpdateTime();
				}
			}
			catch (SocketException exc)
			{
				if (exc.SocketErrorCode != SocketError.TimedOut)
					ReportException(exc);
			}
		}

		static void UpdateTime()
		{
			try
			{
				lock (Sync)
				{
					if (_requestResults.Count < MinResultsCount)
						return;

					var bestNtpResults = FilterNtpResults(_requestResults);
					if (bestNtpResults.Count < MinResultsCount)
						return;

					SetTime(bestNtpResults);

					_requestResults.Clear();
				}
			}
			catch (Exception exc)
			{
				ReportException(exc);
			}
		}

		private static void SetTime(List<NtpResult> vals)
		{
			var offsets = vals.Select(val => val.Offset.TotalSeconds).ToList();
			var newOffset = GetTimeOffset(offsets);

			Data.Tracker.LogFormat("Event", "Time sync: offset {0}", newOffset);

			if (_offsetInitialized)
			{
				Data.Tracker.LogFormat("Event", "Time sync: offset changed {0}",
					newOffset - _currentTimeOffset.TotalSeconds);
			}

			Data.Tracker.LogFormat("Event", "Time sync diagnostics: count {0}, min offset {1}, max offset {2}, max latency {3}",
				vals.Count, offsets.First(), offsets.Last(), vals.Last().Latency);

			var now = DateTime.UtcNow;
			if (_offsetInitialized)
			{
				_timeDrift = (newOffset - _requestedTimeOffset.TotalSeconds) / (now - _lastUpdatedOffset).TotalSeconds;
				_timeSynchronized = true;

				Trace.WriteLine("new time drift: " + _timeDrift);
			}

			_requestedTimeOffset = TimeSpan.FromSeconds(newOffset);
			_currentTimeOffset = _requestedTimeOffset;
			_lastUpdatedOffset = now;
			_offsetInitialized = true;
		}

		private static double GetTimeOffset(List<double> offsets)
		{
			offsets.Sort();

			var middle = (offsets.Count - 1) / 2.0;
			var middle1 = (int)Math.Floor(middle);
			var middle2 = (int)Math.Ceiling(middle);
			var newOffset = (offsets[middle1] + offsets[middle2]) / 2.0;

			return newOffset;
		}

		private static List<NtpResult> FilterNtpResults(List<NtpResult> values)
		{
			values.Sort((left, right) => left.Latency.CompareTo(right.Latency));
			var minLatency = values.First().Latency;
			var res = new List<NtpResult>(values.Count);
			res.AddRange(values.Where(cur => cur.Latency < minLatency * 1.5));
			return res;
		}

		static NtpResult RequestTimeOffset(string server)
		{
			NtpData ntpData;
			var watch = Stopwatch.StartNew();
			DateTime now;

			using (var udpClient = new UdpClient(server, NtpPort))
			{
				udpClient.Client.ReceiveTimeout = ReceiveTimeoutMsecs;
				var request = BuildNtpRequest();
				udpClient.Send(request, request.Length);

				var remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
				var response = udpClient.Receive(ref remoteIpEndPoint);

				now = DateTime.UtcNow;
				ntpData = new NtpData(response);
			}

			watch.Stop();
			var roundTripTime = watch.Elapsed;

			var ntpTime = ntpData.TransmitTimestamp + GetHalf(roundTripTime);
			var res = new NtpResult { Offset = ntpTime - now, Latency = roundTripTime.TotalSeconds };

			return res;
		}

		static byte[] BuildNtpRequest()
		{
			var res = new byte[48];
			res[0] = 0x1B;
			return res;
		}

		static TimeSpan GetHalf(TimeSpan val)
		{
			var res = TimeSpan.FromTicks(val.Ticks / 2);
			return res;
		}

		public static DateTime GetUtcTime()
		{
			lock (Sync)
			{
				if (!_timeSynchronized)
					throw new InvalidOperationException();
				var res = DateTime.UtcNow + _currentTimeOffset;
				return res;
			}
		}

		public static TimeSpan GetTimeOffset()
		{
			lock (Sync)
			{
				if (!_timeSynchronized)
					throw new InvalidOperationException();
				return _currentTimeOffset;
			}
		}

		static void ReportException(Exception exc)
		{
			Data.Tracker.Log("Exception", exc);
		}

		private static readonly string Server = "pool.ntp.org";
		private const int NtpPort = 123;

		private static TimeSpan _currentTimeOffset;
		private static TimeSpan _requestedTimeOffset;
		private static double _timeDrift;

		private static volatile bool _offsetInitialized;

		private static volatile bool _timeSynchronized;

		public static bool TimeSynchronized
		{
			get { return _timeSynchronized; }
		}

		private static Timer _timer;

		private static readonly TimeSpan OffsetUpdatePeriod = TimeSpan.FromSeconds(5);
		private static DateTime _lastRequestedOffset = DateTime.MinValue;
		private static DateTime _lastUpdatedOffset = DateTime.MinValue;

		static readonly object Sync = new object();
		private static readonly List<NtpResult> _requestResults = new List<NtpResult>();
		private const int MinResultsCount = 64;
		private const int ReceiveTimeoutMsecs = 2000;
	}

	// see RFC 2030 for reference
	class NtpData
	{
		public NtpData(byte[] data)
		{
			_data = data;
		}

		public DateTime ReceiveTimestamp
		{
			get { return GetTimestamp(32); }
		}

		public DateTime TransmitTimestamp
		{
			get { return GetTimestamp(40); }
		}

		DateTime GetTimestamp(int offset)
		{
			var msecs = GetTimestampInMilliseconds(offset);
			var startTime = new DateTime(1900, 1, 1);
			var res = startTime + TimeSpan.FromMilliseconds(msecs);
			return res;
		}

		ulong GetTimestampInMilliseconds(int offset)
		{
			ulong intpart = 0, fractpart = 0;

			for (int i = 0; i <= 3; i++)
			{
				intpart = 256 * intpart + _data[offset + i];
			}
			for (int i = 4; i <= 7; i++)
			{
				fractpart = 256 * fractpart + _data[offset + i];
			}

			ulong milliseconds = intpart * 1000 + (fractpart * 1000) / 0x100000000L;
			return milliseconds;
		}

		private readonly byte[] _data;
	}

	class NtpResult
	{
		public TimeSpan Offset;
		public double Latency;

		public override string ToString()
		{
			var res = string.Format("{0} {1}", Offset.TotalSeconds, Latency);
			return res;
		}
	}
}
