using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using AppMetrics.Client;

using CIAPI.DTO;
using CIAPI.Rpc;

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
					var client = new Client(new Uri(AppSettings.Instance.ServerUrl), 
						new Uri(AppSettings.Instance.StreamingServerUrl), "{API_KEY}");
					try
					{
						var measure = AppMetrics.StartMeasure();
						client.LogIn(AppSettings.Instance.UserName, AppSettings.Instance.Password);
						AppMetrics.EndMeasure(measure, "LogIn");
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
						streamingClient = client.CreateStreamingClient();
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
					var measure = AppMetrics.StartMeasure();
					_client.LogOut();
					AppMetrics.EndMeasure(measure, "LogOut");

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

				var measure = AppMetrics.StartMeasure();
				var accountInfo = client.AccountInformation.GetClientAndTradingAccount();
				AppMetrics.EndMeasure(measure, "GetClientAndTradingAccount");

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
						var measure = AppMetrics.StartMeasure();
						var resp = client.CFDMarkets.ListCfdMarkets(nameFilter, codeFilter,
							_accountInfo.ClientAccountId, maxCount, false);
						AppMetrics.EndMeasure(measure, "ListCfdMarkets");
						return resp.Markets;
					}
				case MarketType.Spread:
					{
						var measure = AppMetrics.StartMeasure();
						var resp = client.SpreadMarkets.ListSpreadMarkets(nameFilter, codeFilter,
							_accountInfo.ClientAccountId, maxCount, false);
						AppMetrics.EndMeasure(measure, "ListSpreadMarkets");
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

			NtpdInfo.CheckTimeIsStable();

			//if (!SntpClient.TimeSynchronized)
			//    return;
			//var timeOffset = SntpClient.GetTimeOffset();
			//var latency = (now + timeOffset) - price.TickDate;

			//if (!NtpdInfo.IsTimeStable())
			//    return;

			var latency = now - price.TickDate;

			AppMetrics.Tracker.Log("Latency PriceStream", latency.TotalSeconds);
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
	}

	enum MarketType { CFD, Spread, }
}
