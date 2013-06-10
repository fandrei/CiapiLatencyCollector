using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

using CIAPI.DTO;
using CIAPI.Rpc;
using Salient.ReliableHttpClient;

namespace LatencyCollectorCore.Monitors
{
	public class AllServiceMonitor : AuthenticatedMonitor
	{
		public AllServiceMonitor()
		{
			PeriodSeconds = 5;
		}

		public string ServerUrl { get; set; }
		public string StreamingServerUrl { get; set; }

		public bool AllowTrading { get; set; }

		private Client _client;
		private readonly object _sync = new object();

		private RecorderBase _metricsRecorder;

		// GBP/USD markets
		private const int MarketId = 400616150;

		public override void Execute()
		{
			try
			{
				if (string.IsNullOrEmpty(ServerUrl))
					throw new ApplicationException("AllServiceMonitor: ServerUrl is not set");
				if (PluginSettings.Instance.MonitorSettings.PollingDisabled)
					return;

				lock (_sync)
				{
					if (_client == null)
					{
						_client = new Client(new Uri(ServerUrl), new Uri(StreamingServerUrl), "{API_KEY}", 1);
					}
				}

				if (_metricsRecorder == null && Tracker != null)
				{
					var appKey = "CiapiLatencyCollector." + GetType().Name + ".BuiltIn";
					var tracker = AppMetrics.Client.Tracker.Create(Tracker.Url, appKey, Tracker.AccessKey);
					MetricsUtil.ReportNodeInfo(tracker);

					_metricsRecorder = new CiapiLatencyRecorder(_client, tracker);
					_metricsRecorder.Start();
				}

				Login();

				var accountInfo = GetAccountInfo();

				ListSpreadMarkets(accountInfo);
				ListNews();
				GetMarketInformation();
				GetPriceBars();

				if (AllowTrading)
				{
					var price = GetPrice(_client);
					var canTrade = (price.StatusSummary == 0); // normal status

					if (canTrade)
					{
						CloseAllOpenPositions(accountInfo);

						var orderId = Trade(_client, accountInfo, price, 1M, "buy", new int[0]);
						OpenPositions(accountInfo);
						Trade(_client, accountInfo, price, 1M, "sell", new[] { orderId });
					}
					else
					{
						Tracker.LogFormat("Event", "Trade is not placed: market status is {0}", price.StatusSummary);
						OpenPositions(accountInfo);
					}
				}
				else
				{
					Tracker.Log("Event", "Trade is not placed: AllowTrading==false");
					OpenPositions(accountInfo);
				}

				ListTradeHistory(accountInfo);
			}
			finally
			{
				try
				{
					if (_client != null && !String.IsNullOrEmpty(_client.Session))
					{
						Logout();
					}
				}
				catch (Exception exc)
				{
					Tracker.Log(exc);
				}
			}
		}

		private void Login()
		{
			var measure = Tracker.StartMeasure();
			_client.LogIn(UserName, Password);
			Tracker.EndMeasure(measure, "CIAPI.LogIn");
		}

		private void Logout()
		{
			var measure = Tracker.StartMeasure();
			_client.LogOut();
			Tracker.EndMeasure(measure, "CIAPI.LogOut");
		}

		private AccountInformationResponseDTO GetAccountInfo()
		{
			var measure = Tracker.StartMeasure();
			var accountInfo = _client.AccountInformation.GetClientAndTradingAccount();
			Tracker.EndMeasure(measure, "CIAPI.GetClientAndTradingAccount");
			return accountInfo;
		}

		private void ListSpreadMarkets(AccountInformationResponseDTO accountInfo)
		{
			try
			{
				var measure = Tracker.StartMeasure();
				var resp = _client.SpreadMarkets.ListSpreadMarkets("", "",
					accountInfo.ClientAccountId, 100, false);
				Tracker.EndMeasure(measure, "CIAPI.ListSpreadMarkets");
			}
			catch (Exception exc)
			{
				Tracker.Log(exc);
			}
		}

		private void ListNews()
		{
			try
			{
				var measure = Tracker.StartMeasure();
				var resp = _client.News.ListNewsHeadlinesWithSource("dj", "UK", 10);
				Tracker.EndMeasure(measure, "CIAPI.ListNewsHeadlinesWithSource");
			}
			catch (Exception exc)
			{
				Tracker.Log(exc);
			}
		}

		private void GetMarketInformation()
		{
			try
			{
				var measure = Tracker.StartMeasure();
				var resp = _client.Market.GetMarketInformation(MarketId.ToString());
				Tracker.EndMeasure(measure, "CIAPI.GetMarketInformation");
			}
			catch (Exception exc)
			{
				Tracker.Log(exc);
			}
		}

		private void GetPriceBars()
		{
			try
			{
				var measure = Tracker.StartMeasure();
				var resp = _client.PriceHistory.GetPriceBars(MarketId.ToString(), "MINUTE", 1, "20");
				Tracker.EndMeasure(measure, "CIAPI.GetPriceBars");
			}
			catch (Exception exc)
			{
				Tracker.Log(exc);
			}
		}

		private void OpenPositions(AccountInformationResponseDTO accountInfo)
		{
			try
			{
				var measure = Tracker.StartMeasure();
				_client.TradesAndOrders.ListOpenPositions(accountInfo.SpreadBettingAccount.TradingAccountId);
				Tracker.EndMeasure(measure, "CIAPI.ListOpenPositions");
			}
			catch (Exception exc)
			{
				Tracker.Log(exc);
			}
		}

		private void CloseAllOpenPositions(AccountInformationResponseDTO accountInfo)
		{
			try
			{
				var positions = _client.TradesAndOrders.ListOpenPositions(accountInfo.SpreadBettingAccount.TradingAccountId);
				Tracker.Log("Info_CloseAllOpenPositions_Count", positions.OpenPositions.Length);

				var positionsGrouped = GroupBy(positions.OpenPositions, x => x.Direction);
				foreach (var posGroup in positionsGrouped)
				{
					try
					{
						var price = GetPrice(_client);
						var direction = (posGroup.Key.ToLower() == "buy") ? "sell" : "buy";
						var quantity = posGroup.Value.Sum(x => x.Quantity);
						var ids = posGroup.Value.Select(x => x.OrderId).ToArray();
						Trade(_client, accountInfo, price, quantity, direction, ids);
					}
					catch (Exception exc)
					{
						Tracker.Log(exc);
					}
				}
			}
			catch (Exception exc)
			{
				Tracker.Log(exc);
			}
		}

		public static Dictionary<TKey, List<TSource>> GroupBy<TSource, TKey>(IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector)
		{
			var res = source.GroupBy(keySelector).ToDictionary(pair => pair.Key, pair => pair.ToList());
			return new Dictionary<TKey, List<TSource>>(res);
		}

		private void ListTradeHistory(AccountInformationResponseDTO accountInfo)
		{
			try
			{
				var measure = Tracker.StartMeasure();
				var tradeHistory = _client.TradesAndOrders.ListTradeHistory(accountInfo.SpreadBettingAccount.TradingAccountId, 20);
				Tracker.EndMeasure(measure, "CIAPI.ListTradeHistory");
			}
			catch (Exception exc)
			{
				Tracker.Log(exc);
			}
		}

		private int Trade(Client client, AccountInformationResponseDTO accountInfo, PriceDTO price,
			decimal quantity, string direction, IEnumerable<int> closeOrderIds)
		{
			var tradeRequest = new NewTradeOrderRequestDTO
			{
				MarketId = MarketId,
				Quantity = quantity,
				Direction = direction,
				TradingAccountId = accountInfo.SpreadBettingAccount.TradingAccountId,
				AuditId = price.AuditId,
				BidPrice = price.Bid,
				OfferPrice = price.Offer,
				Close = closeOrderIds.ToArray(),
			};

			var measure = Tracker.StartMeasure();
			var resp = client.TradesAndOrders.Trade(tradeRequest);
			Tracker.EndMeasure(measure, "CIAPI.Trade");

			if (resp.OrderId == 0)
			{
				client.MagicNumberResolver.ResolveMagicNumbers(resp);
				var message = GetResponseDescription(resp);
				throw new ApplicationException(message);
			}

			return resp.OrderId;
		}

		private static string GetResponseDescription(ApiTradeOrderResponseDTO resp)
		{
			var statusText = string.Format("\r\n{0}: {1}\r\n\r\n",
				resp.Status_Resolved, resp.StatusReason_Resolved);

			foreach (var apiOrderResponseDto in resp.Orders)
			{
				statusText += string.Format("{0}: {1}\r\n",
					apiOrderResponseDto.Status_Resolved, apiOrderResponseDto.StatusReason_Resolved);
			}

			return statusText;
		}

		private static PriceDTO GetPrice(Client client)
		{
			using (var streamingClient = client.CreateStreamingClient())
			using (var listener = streamingClient.BuildPricesListener(MarketId))
			{
				PriceDTO price = null;
				try
				{
					var finished = new ManualResetEvent(false);

					listener.MessageReceived +=
						(s, args) =>
							{
								price = args.Data;
								finished.Set();
							};

					if (!finished.WaitOne(TimeSpan.FromMinutes(1)))
						throw new ApplicationException("Can't obtain price: timed out");

					return price;
				}
				finally
				{
					listener.Stop();
				}
			}
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
			if (_client != null)
			{
				if (!string.IsNullOrEmpty(_client.Session))
					_client.LogOut();

				_client.Dispose();
				_client = null;
			}

			if (_metricsRecorder != null)
			{
				_metricsRecorder.Stop();
				_metricsRecorder.Dispose();
				_metricsRecorder = null;
			}
		}
	}
}