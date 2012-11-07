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
			try
			{
				var listeners = new TraceListener[] { new TextWriterTraceListener(Console.Out) };
				Debug.Listeners.AddRange(listeners);

				Start();

				var curProcess = Process.GetCurrentProcess();
				_stopEvent = new EventWaitHandle(false, EventResetMode.ManualReset, curProcess.ProcessName + curProcess.Id);

				var thread = new Thread(ConsoleCheckingThread);
				thread.Start();

				_stopEvent.WaitOne();
				thread.Interrupt();

				Stop();
			}
			catch (Exception exc)
			{
				Report(exc);
			}
		}

		static void ConsoleCheckingThread()
		{
			try
			{
				Console.ReadKey();
				_stopEvent.Set();
			}
			catch (ThreadInterruptedException)
			{
			}
			catch (Exception exc)
			{
				Console.WriteLine(exc);
			}
		}

		public static void Start()
		{
			try
			{
				AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

				InitTracker("http://metrics.labs.cityindex.com/LogEvent.ashx", "CiapiLatencyCollector");

				//SntpClient.Init();

				lock (Sync)
				{
					SettingsUpdater.OnExecute += UpdateSettings;
					SettingsUpdater.Start();

					StartPolling();
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
					SettingsUpdater.Interrupt();
					StopPolling();
					SettingsUpdater.WaitForFinish();

					AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
				}
			}
			catch (Exception exc)
			{
				Report(exc);
			}

			try
			{
				Tracker.Terminate(true);
			}
			catch (Exception exc)
			{
				Report(exc);
			}
		}

		private static void StartPolling()
		{
			var monitors = GetMonitors();
			foreach (var monitor in monitors)
			{
				monitor.Start();
			}
		}

		static void StopPolling()
		{
			lock (Sync)
			{
				var monitors = GetMonitors();

				foreach (var monitor in monitors)
				{
					monitor.Interrupt();
				}

				foreach (var monitor in monitors)
				{
					monitor.WaitForFinish();
				}
			}
		}

		private static void UpdateSettings()
		{
			try
			{
				lock (Sync)
				{
					if (AppSettings.Instance.CheckRemoteSettings())
					{
						Tracker.Log("Event",
							AppSettings.Instance.MonitorSettings.PollingDisabled ? "Polling disabled" : "Central settings updated");

						InitTracker(AppSettings.Instance.MonitorSettings.LogEventUrl,
							AppSettings.Instance.MonitorSettings.ApplicationKey);
						StopPolling();
						StartPolling();
					}
				}
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

			Tracker.Terminate();
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
			Tracker.Log(type, message);
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

		static IList<LatencyMonitor> GetMonitors()
		{
			lock (Sync)
			{
				var res = AppSettings.Instance.MonitorSettings.Monitors.ToList().AsReadOnly();
				return res;
			}
		}

		private static void InitTracker(string url, string appKey)
		{
			try
			{
				lock (TrackerSync)
				{
					if (_tracker != null && (_tracker.Url != url || _tracker.ApplicationKey != appKey))
					{
						_tracker.Dispose();
						_tracker = null;
					}

					if (_tracker == null)
					{
						_tracker = Tracker.Create(url, appKey, "{APPMETRICS_ACCESS_KEY}");

						Tracker.Log("Info_UserId", AppSettings.Instance.UserId);
						Tracker.Log("Info_NodeName", AppSettings.Instance.NodeName);
						var curAssembly = typeof(AppSettings).Assembly;
						Tracker.Log("Info_LatencyCollectorVersion", curAssembly.FullName);
					}
				}
			}
			catch (Exception exc)
			{
				Report(exc);
			}
		}

		private static Tracker _tracker;

		public static Tracker Tracker
		{
			get
			{
				lock (TrackerSync)
				{
					return _tracker;
				}
			}
		}

		private static readonly object Sync = new object();
		private static readonly object TrackerSync = new object();
		private static readonly SettingsUpdateChecker SettingsUpdater = new SettingsUpdateChecker { PeriodSeconds = 15 };

		private static EventWaitHandle _stopEvent;
	}
}
