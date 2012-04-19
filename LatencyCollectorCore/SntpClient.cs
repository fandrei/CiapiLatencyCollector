using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Timer = System.Timers.Timer;

namespace LatencyCollectorCore
{
	static class SntpClient
	{
		public static void Init()
		{
			_timer = new Timer { Interval = 1, AutoReset = false };

			_timer.Elapsed +=
				(state, args) => UpdateTime();

			while (!UpdateTime())
			{
				Thread.Sleep(TimeSpan.FromSeconds(1));
			}

			_timer.Start();
		}

		private static bool UpdateTime()
		{
			try
			{
				var ntpResults = GetNtpResults();
				var bestNtpResults = FilterNtpResults(ntpResults);

				var offsets = bestNtpResults.Select(val => val.Offset.TotalSeconds).ToList();
				offsets.Sort();

				var middle = (offsets.Count - 1) / 2.0;
				var middle1 = (int)Math.Floor(middle);
				var middle2 = (int)Math.Ceiling(middle);
				var newOffset = (offsets[middle1] + offsets[middle2]) / 2.0;

				lock (Sync)
				{
					_timeOffset = TimeSpan.FromSeconds(newOffset);
				}

				return true;
			}
			catch (SocketException exc)
			{
				Trace.WriteLine(exc);
			}
			finally
			{
				_timer.Interval = 1 * 1000;
				_timer.Start();
			}
			return false;
		}

		private static List<NtpResult> GetNtpResults(int measureCount = 8)
		{
			var ntpResults = new List<NtpResult>();

			for (int i = 0; i < measureCount; i++)
			{
				var cur = GetTimeOffset(Server);
				ntpResults.Add(cur);
			}

			return ntpResults;
		}

		private static List<NtpResult> FilterNtpResults(List<NtpResult> ntpResults)
		{
			ntpResults.Sort((left, right) => left.Latency.CompareTo(right.Latency));
			var index = (int)Math.Ceiling(ntpResults.Count / 2.0);
			var res = ntpResults.Take(index).ToList();
			return res;
		}

		private static Timer _timer;

		static readonly object Sync = new object();
		private static TimeSpan _timeOffset;

		public static DateTime GetUtcTime()
		{
			lock (Sync)
			{
				var res = DateTime.UtcNow + _timeOffset;
				return res;
			}
		}

		public static TimeSpan GetTimeOffset()
		{
			lock (Sync)
			{
				return _timeOffset;
			}
		}

		static NtpResult GetTimeOffset(string server)
		{
			NtpData ntpData;
			var watch = Stopwatch.StartNew();
			DateTime now;

			using (var udpClient = new UdpClient(server, NtpPort))
			{
				udpClient.Client.ReceiveTimeout = 1000;
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

		private static readonly string Server = "pool.ntp.org";
		private const int NtpPort = 123;
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
