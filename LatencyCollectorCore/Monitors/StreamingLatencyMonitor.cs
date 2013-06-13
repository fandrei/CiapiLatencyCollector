using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CIAPI.DTO;
using CIAPI.Rpc;
using CIAPI.Streaming;
using CIAPI.StreamingClient;

namespace LatencyCollectorCore.Monitors
{
	public class StreamingLatencyMonitor : AuthenticatedMonitor
	{
		public string ServerUrl { get; set; }
		public string StreamingServerUrl { get; set; }

		// GBP/USD markets
		private const int MarketId = 400616150;

		private Client _client;
		private readonly object _sync = new object();

		private IStreamingClient _streamingClient;
		private IStreamingListener<PriceDTO> _listener;

		private DateTime _streamingStartTime;

		public override void Execute()
		{
			if (string.IsNullOrEmpty(ServerUrl))
				throw new ApplicationException("StreamingLatencyMonitor: ServerUrl is not set");

			lock (_sync)
			{
				if (_client == null)
				{
					_client = new Client(new Uri(ServerUrl), new Uri(StreamingServerUrl), "{API_KEY}", 1);

					_client.LogIn(UserName, Password);

					_streamingClient = _client.CreateStreamingClient();
					_streamingStartTime = DateTime.UtcNow;

					_listener = _streamingClient.BuildPricesListener(MarketId);

					_listener.MessageReceived += OnPriceUpdate;
				}
			}
		}

		void OnPriceUpdate(object sender, MessageEventArgs<PriceDTO> args)
		{
			var now = DateTime.UtcNow;
			var tickTime = args.Data.TickDate;

			if (tickTime < _streamingStartTime) // outdated tick
				return;

			var diff = now - tickTime;
			Tracker.Log("Latency CIAPI.PriceStream", diff.TotalSeconds);
		}

		protected override bool InterruptInternal()
		{
			lock (_sync)
			{
				if (_client != null)
				{
					_client.LogOut();
				}
			}
			return true;
		}

		protected override void Cleanup()
		{
			if (_listener != null && _streamingClient != null)
			{
				_streamingClient.TearDownListener(_listener);
			}

			if (_listener != null)
			{
				_listener.Stop();
				_listener.Dispose();
				_listener = null;
			}

			if (_streamingClient != null)
			{
				_streamingClient.Dispose();
				_streamingClient = null;
			}

			if (_client != null)
			{
				if (!string.IsNullOrEmpty(_client.Session))
					_client.LogOut();

				_client.Dispose();
				_client = null;
			}
		}
	}
}
