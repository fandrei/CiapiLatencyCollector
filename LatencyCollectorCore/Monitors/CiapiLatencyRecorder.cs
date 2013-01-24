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
				_tracker.Log(info.Exception);
			}
			else
			{
				_tracker.LogLatency(info.Uri.ToString(), info.Watch.Elapsed.TotalSeconds);
			}
		}

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
