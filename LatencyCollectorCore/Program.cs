using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

using AppMetrics.Client;

namespace LatencyCollectorCore
{
	public class Program
	{
		static void Main()
		{
			var listeners = new[] { new TextWriterTraceListener(Console.Out) };
			Debug.Listeners.AddRange(listeners);

			Start();
			Console.ReadKey();
			Stop();
		}

		public static void Start()
		{
			try
			{
				lock (Sync)
				{
					AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

					if (_thread != null)
						throw new InvalidOperationException();

					_terminated = false;
					_thread = new Thread(ThreadProc);
					_thread.Start();
				}
			}
			catch (Exception exc)
			{
				ReportEvent(exc.ToString());
			}
		}

		public static void Stop()
		{
			try
			{
				lock (Sync)
				{
					_terminated = true;
					_data.Logout();
					_thread.Join(TimeSpan.FromMinutes(2));
					_thread = null;

					AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
				}
			}
			catch (Exception exc)
			{
				ReportEvent(exc.ToString());
			}
		}

		static void ThreadProc()
		{
			var period = TimeSpan.FromMinutes(1.0 / AppSettings.Instance.DataPollingRate);

			try
			{
				_data = new Data();

				while (!_terminated)
				{
					try
					{
						PerformPolling();
					}
					catch (ThreadInterruptedException)
					{
						break;
					}
					catch (NotConnectedException)
					{ }
					catch (Exception exc)
					{
						ReportEvent(exc.ToString());
					}

					if (_terminated)
						break;
					Thread.Sleep(period);
				}
			}
			catch (ThreadInterruptedException)
			{ }

			try
			{
				if (_data != null)
					_data.Dispose();
				_data = null;
			}
			catch (Exception exc)
			{
				ReportEvent(exc.ToString());
			}

			try
			{
				Tracker.Terminate(true);
			}
			catch (Exception exc)
			{
				ReportEvent(exc.ToString());
			}
		}

		private static void PerformPolling()
		{
			_data.Login();

			_data.GetMarketsList(MarketType.CFD, 100, "", "");
			_data.GetMarketsList(MarketType.Spread, 100, "", "");

			_data.Logout();
		}

		static void ReportEvent(string message)
		{
			Trace.WriteLine(message);
			Data.Tracker.Log("Exception", message);
		}

		// load assembly from the working folder, if impossible to resolve automatically
		// (workaround for Newtonsoft.Json load problem)
		static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			var name = new AssemblyName(args.Name);
			var fileName = Util.GetAppLocation() + name.Name + ".dll";
			if (File.Exists(fileName))
			{
				var res = Assembly.LoadFrom(fileName);
				return res;
			}
			return null;
		}

		private static Data _data;
		private static volatile bool _terminated;
		private static Thread _thread;
		private static readonly object Sync = new object();
	}
}
