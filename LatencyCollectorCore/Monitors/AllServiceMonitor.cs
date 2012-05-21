using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using CIAPI.DTO;
using CIAPI.Rpc;

namespace LatencyCollectorCore.Monitors
{
	class AllServiceMonitor : Monitor
	{
		// GBP/USD market
		private const int MarketId = 400481142;

		public override void Execute()
		{
			Client client = null;
			try
			{
				client = new Client(new Uri(AppSettings.Instance.ServerUrl),
					new Uri(AppSettings.Instance.StreamingServerUrl), "{API_KEY}");
				client.StartMetrics();

				{
					var measure = AppMetrics.StartMeasure();
					client.LogIn(AppSettings.Instance.UserName, AppSettings.Instance.Password);
					AppMetrics.EndMeasure(measure, "LogIn");
				}

				AccountInformationResponseDTO accountInfo;
				{
					var measure = AppMetrics.StartMeasure();
					accountInfo = client.AccountInformation.GetClientAndTradingAccount();
					AppMetrics.EndMeasure(measure, "GetClientAndTradingAccount");
				}

				{
					var measure = AppMetrics.StartMeasure();
					var resp = client.CFDMarkets.ListCfdMarkets("", "",
						accountInfo.ClientAccountId, 100, false);
					AppMetrics.EndMeasure(measure, "ListCfdMarkets");
				}

				{
					var measure = AppMetrics.StartMeasure();
					var resp = client.News.ListNewsHeadlinesWithSource("dj", "UK", 10);
					AppMetrics.EndMeasure(measure, "ListNewsHeadlinesWithSource");
				}

				{
					var measure = AppMetrics.StartMeasure();
					var resp = client.Market.GetMarketInformation(MarketId.ToString());
					AppMetrics.EndMeasure(measure, "GetMarketInformation");
				}

				{
					var measure = AppMetrics.StartMeasure();
					var resp = client.PriceHistory.GetPriceBars(MarketId.ToString(), "MINUTE", 1, "20");
					AppMetrics.EndMeasure(measure, "GetPriceBars");
				}

				var price = GetPrice(client);

				var measure1 = AppMetrics.StartMeasure();
				var orderId = Trade(client, accountInfo, price, 1M, "buy", new int[0]);
				AppMetrics.EndMeasure(measure1, "Trade");

				{
					var measure = AppMetrics.StartMeasure();
					client.TradesAndOrders.ListOpenPositions(accountInfo.CFDAccount.TradingAccountId);
					AppMetrics.EndMeasure(measure, "ListOpenPositions");
				}

				{
					var measure = AppMetrics.StartMeasure();
					Trade(client, accountInfo, price, 1M, "sell", new[] { orderId });
					AppMetrics.EndMeasure(measure, "Trade");
				}

				{
					var measure = AppMetrics.StartMeasure();
					var tradeHistory = client.TradesAndOrders.ListTradeHistory(accountInfo.CFDAccount.TradingAccountId, 20);
					AppMetrics.EndMeasure(measure, "ListTradeHistory");
				}
			}
			finally
			{
				try
				{
					if (client != null)
						client.LogOut();
				}
				catch (Exception exc)
				{
					Program.Report(exc);
				}
				finally
				{
					if (client != null)
						client.Dispose();
				}
			}
		}

		private static int Trade(Client client, AccountInformationResponseDTO accountInfo, PriceDTO price,
			decimal quantity, string direction, IEnumerable<int> closeOrderIds)
		{
			var measure = AppMetrics.StartMeasure();
			var tradeRequest = new NewTradeOrderRequestDTO
				{
					MarketId = MarketId,
					Quantity = quantity,
					Direction = direction,
					TradingAccountId = accountInfo.CFDAccount.TradingAccountId,
					AuditId = price.AuditId,
					BidPrice = price.Bid,
					OfferPrice = price.Offer,
					Close = closeOrderIds.ToArray(),
				};
			var resp = client.TradesAndOrders.Trade(tradeRequest);
			AppMetrics.EndMeasure(measure, "Trade");

			if (resp.OrderId == 0)
			{
				client.MagicNumberResolver.ResolveMagicNumbers(resp);
				var message = resp.Status_Resolved + " " + resp.StatusReason_Resolved;
				throw new ApplicationException(message);
			}

			return resp.OrderId;
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
	}
}
