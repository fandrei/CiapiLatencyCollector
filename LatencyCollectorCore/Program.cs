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
				Report(exc);
			}
		}

		public static void Stop()
		{
			try
			{
				lock (Sync)
				{
					_terminated = true;
					_thread.Interrupt();
					_thread.Join(TimeSpan.FromMinutes(2));
					_thread = null;

					AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
				}
			}
			catch (Exception exc)
			{
				Report(exc);
			}
		}

		static void ThreadProc()
		{
			var period = TimeSpan.FromMinutes(1.0 / AppSettings.Instance.DataPollingRate);

			try
			{
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
						Report(exc);
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
				Tracker.Terminate(true);
			}
			catch (Exception exc)
			{
				Report(exc);
			}
		}

		private static void PerformPolling()
		{
			var data = new Data();
			try
			{
				data.Login();

				data.GetMarketsList(MarketType.CFD, 100, "", "");
				data.GetMarketsList(MarketType.Spread, 100, "", "");
			}
			catch (Exception exc)
			{
				Report(exc);
			}
			finally
			{
				try
				{
					data.Logout();
				}
				catch (Exception exc)
				{
					Report(exc);
				}
				finally
				{
					data.Dispose();
				}
			}
		}

		static void Report(Exception exc)
		{
			if (exc is ThreadInterruptedException)
				return;
			ReportEvent(exc.ToString());
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

		private static volatile bool _terminated;
		private static Thread _thread;
		private static readonly object Sync = new object();
	}
}
