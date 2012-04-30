using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using AppMetrics.Client;
using CIAPI.DTO;
using CIAPI.Rpc;
using CIAPI.Streaming;
using StreamingClient;
using IStreamingClient = CIAPI.Streaming.IStreamingClient;

namespace LatencyCollectorCore
{
	class Data
	{
		public void Login()
		{
			VerifyIsNotDisposed();

			lock (_sync)
			{
				if (_client == null)
				{
					var client = new Client(new Uri(AppSettings.Instance.ServerUrl), "{API_KEY}");
					try
					{
						var measure = StartMeasure();
						client.LogIn(AppSettings.Instance.UserName, AppSettings.Instance.Password);
						EndMeasure(measure, "LogIn");
					}
					catch (Exception exc)
					{
						try
						{
							client.Dispose();
						}
						catch (Exception exc2)
						{
							Trace.WriteLine(exc2);
						}
						throw new MessageException(exc, "{0}\r\n{1}", AppSettings.Instance.ServerUrl, exc.Message);
					}

					IStreamingClient streamingClient;
					try
					{
						streamingClient = StreamingClientFactory.CreateStreamingClient(
							new Uri(AppSettings.Instance.StreamingServerUrl), AppSettings.Instance.UserName, client.Session);
					}
					catch (Exception exc)
					{
						client.LogOut();
						client.Dispose();
						throw new MessageException(exc, "{0}\r\n{1}", AppSettings.Instance.StreamingServerUrl, exc.Message);
					}

					_client = client;
					_streamingClient = streamingClient;

					Connected = true;
				}

				EnsureAccountInfoIsCached();
			}
		}

		public void Logout()
		{
			lock (_sync)
			{
				Connected = false;

				if (_streamingClient != null)
				{
					_streamingClient.Dispose();
					_streamingClient = null;
				}

				if (_client != null)
				{
					var measure = StartMeasure();
					_client.LogOut();
					EndMeasure(measure, "LogOut");

					_client.Dispose();
					_client = null;
				}

				_accountInfo = null;
			}
		}

		void EnsureAccountInfoIsCached()
		{
			if (_accountInfo == null)
			{
				var client = GetClient();

				var measure = StartMeasure();
				var accountInfo = client.AccountInformation.GetClientAndTradingAccount();
				EndMeasure(measure, "GetClientAndTradingAccount");

				lock (_sync)
				{
					_accountInfo = accountInfo;
				}
			}
		}

		public ApiMarketDTO[] GetMarketsList(MarketType type, int maxCount, string nameFilter, string codeFilter)
		{
			var client = GetClient();
			switch (type)
			{
				case MarketType.CFD:
					{
						var measure = StartMeasure();
						var resp = client.CFDMarkets.ListCfdMarkets(nameFilter, codeFilter,
							_accountInfo.ClientAccountId, maxCount, false);
						EndMeasure(measure, "ListCfdMarkets");
						return resp.Markets;
					}
				case MarketType.Spread:
					{
						var measure = StartMeasure();
						var resp = client.SpreadMarkets.ListSpreadMarkets(nameFilter, codeFilter,
							_accountInfo.ClientAccountId, maxCount, false);
						EndMeasure(measure, "ListSpreadMarkets");
						return resp.Markets;
					}
				default:
					throw new NotSupportedException();
			}
		}

		public void SubscribePrice(int market)
		{
			lock (_sync)
			{
				var newListener = _streamingClient.BuildPricesListener(market);

				_streamingStartTime = DateTime.UtcNow;

				newListener.MessageReceived += OnPriceUpdate;
			}
		}

		private static DateTime _streamingStartTime;

		static void OnPriceUpdate(object sender, MessageEventArgs<PriceDTO> e)
		{
			var price = e.Data;
			if (price.TickDate < _streamingStartTime) // outdated tick
				return;

			var now = DateTime.UtcNow;

			//if (!SntpClient.TimeSynchronized)
			//    return;
			//var timeOffset = SntpClient.GetTimeOffset();
			//var latency = (now + timeOffset) - price.TickDate;

			if (!NtpdInfo.IsTimeStable())
				return;
			var latency = now - price.TickDate;

			Tracker.Log("Latency PriceStream", latency.TotalSeconds);
		}

		readonly object _sync = new object();
		private Client _client;
		private IStreamingClient _streamingClient;

		private volatile AccountInformationResponseDTO _accountInfo;

		Client GetClient()
		{
			lock (_sync)
			{
				VerifyIsConnected();
				return _client;
			}
		}

		private volatile bool _disposed;

		public void Dispose()
		{
			_disposed = true;

			try
			{
				Logout();
			}
			catch (Exception exc)
			{
				Trace.WriteLine("Data.Dispose() error:\r\n{0}", exc.ToString());
			}
		}

		private volatile bool _connected;

		public bool Connected
		{
			get { return _connected; }
			private set { _connected = value; }
		}

		void VerifyIsConnected()
		{
			VerifyIsNotDisposed();

			if (!Connected)
				throw new NotConnectedException();
		}

		void VerifyIsNotDisposed()
		{
			if (_disposed)
				throw new ObjectDisposedException("Data");
		}

		static Stopwatch StartMeasure()
		{
			return Stopwatch.StartNew();
		}

		static void EndMeasure(Stopwatch watch, string label)
		{
			var diff = watch.Elapsed;
			watch.Stop();

			Tracker.Log("Latency " + label, diff.TotalSeconds);
		}

		public static readonly Tracker Tracker = new Tracker("http://metrics.labs.cityindex.com/LogEvent.ashx",
			"CiapiLatencyCollector");
	}

	enum MarketType { CFD, Spread, }
}
