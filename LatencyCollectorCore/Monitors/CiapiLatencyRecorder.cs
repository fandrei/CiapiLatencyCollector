using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AppMetrics.Client;
using Salient.ReliableHttpClient;

namespace LatencyCollectorCore.Monitors
{
	class CiapiLatencyRecorder : RecorderBase
	{
		public CiapiLatencyRecorder(ClientBase client, Tracker tracker)
			: base(client)
		{
			_tracker = tracker;
		}

		protected override void AddRequest(RequestInfoBase info)
		{
			if (info.Exception != null)
			{
				var now = DateTime.UtcNow;

				_tracker.Log(info.Exception);

				if (_lastExceptionTime > DateTime.MinValue)
				{
					var timeDiff = now - _lastExceptionTime;
					_tracker.Log("TimeFromLastException CIAPI", timeDiff.TotalSeconds);
				}

				_lastExceptionTime = now;
			}
			else
			{
				var now = DateTime.UtcNow;

				_tracker.LogLatency(info.Uri.ToString(), info.Watch.Elapsed.TotalSeconds);

				if (_lastSuccessTime > DateTime.MinValue)
				{
					var timeDiff = now - _lastSuccessTime;
					_tracker.Log("TimeFromLastSuccess CIAPI", timeDiff.TotalSeconds);
				}

				_lastSuccessTime = now;
			}
		}

		private DateTime _lastSuccessTime = DateTime.MinValue;
		private DateTime _lastExceptionTime = DateTime.MinValue;

		public override void Start()
		{
			base.Paused = false;
		}

		public override void Stop()
		{
			base.Paused = true;
		}

		private readonly Tracker _tracker;
	}
}
