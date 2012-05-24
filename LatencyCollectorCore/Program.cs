using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

using AppMetrics.Client;
using LatencyCollectorCore.Monitors;

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
			try
			{
				AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

				var curAssembly = typeof(AppSettings).Assembly;
				AppMetrics.Tracker.Log("Info_LatencyCollectorVersion", curAssembly.FullName);

				//SntpClient.Init();

				while (!_terminated)
				{
					try
					{
						PerformPolling();
					}
					catch (NotConnectedException)
					{ }
					catch (Exception exc)
					{
						Report(exc);
					}

					if (_terminated)
						break;

					var period = TimeSpan.FromSeconds(1.0);
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

		static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			var exception = e.ExceptionObject as Exception;
			if (exception != null)
				Report(exception);
		}

		private static void PerformPolling()
		{
			var now = DateTime.UtcNow;
			var monitors = GetMonitors();

			foreach (var monitor in monitors)
			{
				if ((now - monitor.LastExecution).TotalSeconds > monitor.PeriodSeconds)
				{
					monitor.LastExecution = now;
					var tmp = monitor;
					ThreadPool.QueueUserWorkItem(
						s =>
						{
							try
							{
								tmp.Execute();
							}
							catch (ThreadInterruptedException)
							{
							}
							catch (Exception exc)
							{
								Report(exc);
							}
						});
				}
			}
		}

		public static void Report(Exception exc)
		{
			if (exc is ThreadInterruptedException)
				return;
			ReportEvent("Exception", exc.ToString());
		}

		public static void ReportEvent(string type, string message)
		{
			Trace.WriteLine(message);
			AppMetrics.Tracker.Log(type, message);
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

		static LatencyMonitor[] GetMonitors()
		{
			lock (Sync)
			{
				if (_monitors == null)
				{
					_monitors = AppSettings.Instance.Monitors.ToList();
				}
				return _monitors.ToArray();
			}
		}

		private static volatile bool _terminated;
		private static Thread _thread;
		private static readonly object Sync = new object();

		private static List<LatencyMonitor> _monitors;
	}
}
