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
				_thread = new Thread(ThreadProc);
				_thread.Start();
			}
			catch (Exception exc)
			{
				Trace.WriteLine(exc);
			}
		}

		static void ThreadProc()
		{
			var period = TimeSpan.FromMinutes(1.0 / AppSettings.Instance.DataPollingRate);
			Data data = null;
			while (true)
			{
				try
				{
					data = new Data();
					data.Login();
					Trace.WriteLine("Logged in");

					data.GetMarketsList(MarketType.CFD, 100, "", "");
					data.GetMarketsList(MarketType.Spread, 100, "", "");
					Trace.WriteLine("Request finished ok");

					data.Logout();
					data.Dispose();
					Trace.WriteLine("Logged out");

					Thread.Sleep(period);
				}
				catch (ThreadInterruptedException)
				{
					break;
				}
				catch (Exception exc)
				{
					Trace.WriteLine(exc);
				}
				finally
				{
					if (data != null)
					{
						try
						{
							data.Dispose();
						}
						catch (Exception exc)
						{
							Trace.WriteLine(exc);
						}
						data = null;
					}
				}
			}
		}

		static void Stop()
		{
			try
			{
				_thread.Interrupt();
				_thread = null;
			}
			catch (Exception exc)
			{
				Trace.WriteLine(exc);
			}
		}

		private static Thread _thread;
	}
}
