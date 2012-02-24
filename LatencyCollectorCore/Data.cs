﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

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
					var client = new Client(new Uri(AppSettings.Instance.ServerUrl));
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
						var measure = StartMeasure();
						streamingClient = StreamingClientFactory.CreateStreamingClient(
							new Uri(AppSettings.Instance.StreamingServerUrl), AppSettings.Instance.UserName, client.Session);
						EndMeasure(measure, "CreateStreamingClient");
					}
					catch (Exception exc)
					{
						client.LogOut();
						client.Dispose();
						throw new MessageException(exc, "{0}\r\n{1}", AppSettings.Instance.StreamingServerUrl, exc.Message);
					}

					_client = client;
					_streamingClient = streamingClient;

					_connected = true;
				}

				EnsureAccountInfoIsCached();
			}
		}

		public void Logout()
		{
			lock (_sync)
			{
				_connected = false;

				if (_streamingClient != null)
				{
					var measure = StartMeasure();
					_streamingClient.Dispose();
					EndMeasure(measure, "StreamingClient.Dispose");

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
			Trace.WriteLine("Data.Dispose()\r\n");
			_disposed = true;

			try
			{
				Logout();
				Trace.WriteLine("Data.Dispose() finished successfully\r\n");
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

		static void EndMeasure(Stopwatch watch, string label = null)
		{
			var diff = watch.Elapsed;
			watch.Stop();

			if (label == null)
			{
				var stackTrace = new StackTrace();
				label = stackTrace.GetFrame(1).GetMethod().Name;
			}
			//Tracker.LogEvent("Latency " + label, diff.TotalSeconds, MessagePriority.Low);
		}
	}

	enum MarketType { CFD, Spread, }
}
