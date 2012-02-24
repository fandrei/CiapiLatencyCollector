using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

using AppMetrics.Client;

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

			Tracker.Terminate(true);
		}

		static void Start()
		{
			try
			{
				_terminated = false;
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

			while (!_terminated)
			{
				try
				{
					PerformPolling();
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
					if (_data != null)
					{
						try
						{
							_data.Dispose();
						}
						catch (Exception exc)
						{
							Trace.WriteLine(exc);
						}
						_data = null;
					}
				}
			}
		}

		private static void PerformPolling()
		{
			_data = new Data();
			_data.Login();
			Trace.WriteLine("Logged in");

			_data.GetMarketsList(MarketType.CFD, 100, "", "");
			_data.GetMarketsList(MarketType.Spread, 100, "", "");
			Trace.WriteLine("Request finished ok");

			_data.Logout();
			_data.Dispose();
			Trace.WriteLine("Logged out");
		}

		static void Stop()
		{
			try
			{
				_terminated = true;
				_data.Logout();
				_thread = null;
			}
			catch (Exception exc)
			{
				Trace.WriteLine(exc);
			}
		}

		private static Data _data;
		private static volatile bool _terminated;
		private static Thread _thread;
	}
}
