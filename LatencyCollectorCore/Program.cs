using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace LatencyCollectorCore
{
	class Program
	{
		static void Main(string[] args)
		{
			var listeners = new[] { new TextWriterTraceListener(Console.Out) };
			Debug.Listeners.AddRange(listeners);

			Start();
			Console.ReadKey();
			Stop();
		}

		static void Start()
		{
			try
			{
				_data = new Data();
				_data.Login();
				Trace.WriteLine("Logged in");

				_thread = new Thread(
				() =>
				{
					var period = TimeSpan.FromMinutes(1.0 / AppSettings.Instance.DataPollingRate);
					while (_data.Connected)
					{
						try
						{
							_data.GetMarketsList(MarketType.CFD, 100, "", "");
							_data.GetMarketsList(MarketType.Spread, 100, "", "");
							Trace.WriteLine("Request finished ok");

							Thread.Sleep(period);
						}
						catch (Exception exc)
						{
							Trace.WriteLine(exc);
						}
					}
				});
				_thread.Start();
			}
			catch (Exception exc)
			{
				Trace.WriteLine(exc);
			}
		}

		static void Stop()
		{
			try
			{
				_data.Logout();
				_thread.Interrupt();
			}
			catch (Exception exc)
			{
				Trace.WriteLine(exc);
			}

			Trace.WriteLine("Logged out");
		}

		private static Data _data;
		private static Thread _thread;
	}
}
