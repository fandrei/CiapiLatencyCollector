using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

using CIAPI.DTO;
using CIAPI.Rpc;

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

		private Client _client = null;
		internal Client ApiClient
		{
			get
			{
				if (_client == null)
				{
					_client = new Client(new Uri(ServerUrl), new Uri(StreamingServerUrl), "{API_KEY}", 1);
					_client.AppKey = "CiapiLatencyCollector." + GetType().Name + ".BuiltIn";

					if (Tracker != null)
					{
						_metricsRecorder = new MetricsRecorder(_client, new Uri(Tracker.Url), Guid.NewGuid().ToString(), "{APPMETRICS_ACCESS_KEY}");
						_metricsRecorder.Start();
					}
				}
				return _client;
			}
			set { _client = value; }
		}

		private MetricsRecorder _metricsRecorder;

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

				using (var client = new WebClient())
				{
					// check if internet connection is available
					var resp = client.DownloadString("http://www.msftncsi.com/ncsi.txt");
				}

				Login();

				var accountInfo = GetAccountInfo();

				ListSpreadMarkets(accountInfo);
				ListNews();
				GetMarketInformation();
				GetPriceBars();

				if (AllowTrading)
				{
					var price = GetPrice(ApiClient);
					var canTrade = (price.StatusSummary == 0); // normal status

					if (canTrade)
					{
						CloseAllOpenPositions(accountInfo);

						var orderId = Trade(ApiClient, accountInfo, price, 1M, "buy", new int[0]);
						OpenPositions(accountInfo);
						Trade(ApiClient, accountInfo, price, 1M, "sell", new[] { orderId });
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
			catch (Exception exc)
			{
				Report(exc);
			}
			finally
			{
				try
				{
					if (ApiClient != null && !String.IsNullOrEmpty(ApiClient.Session))
					{
						Logout();
					}
				}
				catch (Exception exc)
				{
					Report(exc);
				}
			}
		}

		private void Login()
		{
			var measure = Tracker.StartMeasure();
			ApiClient.LogIn(UserName, Password);
			Tracker.EndMeasure(measure, "CIAPI.LogIn");
		}

		private void Logout()
		{
			var measure = Tracker.StartMeasure();
			ApiClient.LogOut();
			Tracker.EndMeasure(measure, "CIAPI.LogOut");
		}

		private AccountInformationResponseDTO GetAccountInfo()
		{
			var measure = Tracker.StartMeasure();
			var accountInfo = ApiClient.AccountInformation.GetClientAndTradingAccount();
			Tracker.EndMeasure(measure, "CIAPI.GetClientAndTradingAccount");
			return accountInfo;
		}

		private void ListSpreadMarkets(AccountInformationResponseDTO accountInfo)
		{
			try
			{
				var measure = Tracker.StartMeasure();
				var resp = ApiClient.SpreadMarkets.ListSpreadMarkets("", "",
					accountInfo.ClientAccountId, 100, false);
				Tracker.EndMeasure(measure, "CIAPI.ListSpreadMarkets");
			}
			catch (Exception exc)
			{
				Report(exc);
			}
		}

		private void ListNews()
		{
			try
			{
				var measure = Tracker.StartMeasure();
				var resp = ApiClient.News.ListNewsHeadlinesWithSource("dj", "UK", 10);
				Tracker.EndMeasure(measure, "CIAPI.ListNewsHeadlinesWithSource");
			}
			catch (Exception exc)
			{
				Report(exc);
			}
		}

		private void GetMarketInformation()
		{
			try
			{
				var measure = Tracker.StartMeasure();
				var resp = ApiClient.Market.GetMarketInformation(MarketId.ToString());
				Tracker.EndMeasure(measure, "CIAPI.GetMarketInformation");
			}
			catch (Exception exc)
			{
				Report(exc);
			}
		}

		private void GetPriceBars()
		{
			try
			{
				var measure = Tracker.StartMeasure();
				var resp = ApiClient.PriceHistory.GetPriceBars(MarketId.ToString(), "MINUTE", 1, "20");
				Tracker.EndMeasure(measure, "CIAPI.GetPriceBars");
			}
			catch (Exception exc)
			{
				Report(exc);
			}
		}

		private void OpenPositions(AccountInformationResponseDTO accountInfo)
		{
			try
			{
				var measure = Tracker.StartMeasure();
				ApiClient.TradesAndOrders.ListOpenPositions(accountInfo.SpreadBettingAccount.TradingAccountId);
				Tracker.EndMeasure(measure, "CIAPI.ListOpenPositions");
			}
			catch (Exception exc)
			{
				Report(exc);
			}
		}

		private void CloseAllOpenPositions(AccountInformationResponseDTO accountInfo)
		{
			try
			{
				var positions = ApiClient.TradesAndOrders.ListOpenPositions(accountInfo.SpreadBettingAccount.TradingAccountId);
				Tracker.Log("Info_CloseAllOpenPositions_Count", positions.OpenPositions.Length);

				var positionsGrouped = GroupBy(positions.OpenPositions, x => x.Direction);
				foreach (var posGroup in positionsGrouped)
				{
					try
					{
						var price = GetPrice(ApiClient);
						var direction = (posGroup.Key.ToLower() == "buy") ? "sell" : "buy";
						var quantity = posGroup.Value.Sum(x => x.Quantity);
						var ids = posGroup.Value.Select(x => x.OrderId).ToArray();
						Trade(ApiClient, accountInfo, price, quantity, direction, ids);
					}
					catch (Exception exc)
					{
						Report(exc);
					}
				}
			}
			catch (Exception exc)
			{
				Report(exc);
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
				var tradeHistory = ApiClient.TradesAndOrders.ListTradeHistory(accountInfo.SpreadBettingAccount.TradingAccountId, 20);
				Tracker.EndMeasure(measure, "CIAPI.ListTradeHistory");
			}
			catch (Exception exc)
			{
				Report(exc);
			}
		}

		static void Report(Exception exc)
		{
			var webExc = exc as WebException;
			if (webExc != null && WebUtil.IsConnectionFailure(webExc))
				return;

			Program.Report(exc);
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
			var streamingClient = client.CreateStreamingClient();
			var listener = streamingClient.BuildPricesListener(MarketId);

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
				streamingClient.Dispose();
			}
		}

		protected override void Cleanup()
		{
			if (ApiClient != null)
			{
				ApiClient.Dispose();
				ApiClient = null;
			}

			if (_metricsRecorder != null)
			{
				_metricsRecorder.Stop();
				_metricsRecorder = null;
			}
		}
	}
}