﻿using System;
using System.Collections.Generic;
using System.Linq;
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
			ServerUrl = "https://ciapi.cityindex.com/TradingApi";
			StreamingServerUrl = "https://push.cityindex.com";
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
					_client = new Client(new Uri(ServerUrl), new Uri(StreamingServerUrl), "{API_KEY}");
					_client.AppKey = "CiapiLatencyCollector." + GetType().Name;
				}
				return _client;
			}
			set { _client = value; }
		}

		// GBP/USD markets
		private const int MarketId = 400616150;

		public override void Execute()
		{
			try
			{
				{
					var measure = Tracker.StartMeasure();
					ApiClient.LogIn(UserName, Password);
					Tracker.EndMeasure(measure, "LogIn");
				}

				AccountInformationResponseDTO accountInfo;
				{
					var measure = Tracker.StartMeasure();
					accountInfo = ApiClient.AccountInformation.GetClientAndTradingAccount();
					Tracker.EndMeasure(measure, "GetClientAndTradingAccount");
				}

				{
					var measure = Tracker.StartMeasure();
					var resp = ApiClient.SpreadMarkets.ListSpreadMarkets("", "",
						accountInfo.ClientAccountId, 100, false);
					Tracker.EndMeasure(measure, "ListSpreadMarkets");
				}

				{
					var measure = Tracker.StartMeasure();
					var resp = ApiClient.News.ListNewsHeadlinesWithSource("dj", "UK", 10);
					Tracker.EndMeasure(measure, "ListNewsHeadlinesWithSource");
				}

				{
					var measure = Tracker.StartMeasure();
					var resp = ApiClient.Market.GetMarketInformation(MarketId.ToString());
					Tracker.EndMeasure(measure, "GetMarketInformation");
				}

				{
					var measure = Tracker.StartMeasure();
					var resp = ApiClient.PriceHistory.GetPriceBars(MarketId.ToString(), "MINUTE", 1, "20");
					Tracker.EndMeasure(measure, "GetPriceBars");
				}

				if (AllowTrading)
				{
					var price = GetPrice(ApiClient);

					var orderId = Trade(ApiClient, accountInfo, price, 1M, "buy", new int[0]);

					{
						var measure = Tracker.StartMeasure();
						ApiClient.TradesAndOrders.ListOpenPositions(accountInfo.SpreadBettingAccount.TradingAccountId);
						Tracker.EndMeasure(measure, "ListOpenPositions");
					}

					Trade(ApiClient, accountInfo, price, 1M, "sell", new[] { orderId });
				}
				else
				{
					var measure = Tracker.StartMeasure();
					ApiClient.TradesAndOrders.ListOpenPositions(accountInfo.SpreadBettingAccount.TradingAccountId);
					Tracker.EndMeasure(measure, "ListOpenPositions");
				}

				{
					var measure = Tracker.StartMeasure();
					var tradeHistory = ApiClient.TradesAndOrders.ListTradeHistory(accountInfo.SpreadBettingAccount.TradingAccountId, 20);
					Tracker.EndMeasure(measure, "ListTradeHistory");
				}
			}
			catch (NotConnectedException)
			{ }
			finally
			{
				try
				{
					if (ApiClient != null && !String.IsNullOrEmpty(ApiClient.Session))
						ApiClient.LogOut();
				}
				catch (Exception exc)
				{
					Program.Report(exc);
				}
			}
		}

		private int Trade(Client client, AccountInformationResponseDTO accountInfo, PriceDTO price,
			decimal quantity, string direction, IEnumerable<int> closeOrderIds)
		{
			var measure = Tracker.StartMeasure();
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
			var resp = client.TradesAndOrders.Trade(tradeRequest);
			Tracker.EndMeasure(measure, "Trade");

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
		}
	}
}
