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
					StopPolling();

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
			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

			var curAssembly = typeof(AppSettings).Assembly;
			AppMetrics.Tracker.Log("Info_LatencyCollectorVersion", curAssembly.FullName);

			//SntpClient.Init();

			lock (Sync)
			{
				AppSettings.Instance.CheckUpdates();
				SettingsUpdater.Start();
			}

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

				SettingsUpdater.Interrupt();
				foreach (var monitor in monitors)
				{
					monitor.Interrupt();
				}

				SettingsUpdater.WaitForFinish();
				foreach (var monitor in monitors)
				{
					monitor.WaitForFinish();
				}
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

		static IList<LatencyMonitor> GetMonitors()
		{
			lock (Sync)
			{
				var res = AppSettings.Instance.Monitors.ToList().AsReadOnly();
				return res;
			}
		}

		private static readonly object Sync = new object();
		private static readonly SettingsUpdateChecker SettingsUpdater = new SettingsUpdateChecker();
	}
}
