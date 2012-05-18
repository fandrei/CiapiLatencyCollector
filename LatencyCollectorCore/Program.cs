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
			try
			{
				AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

				var curAssembly = typeof(Data).Assembly;
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
				if ((now - monitor.LastExecution).TotalSeconds > monitor.Info.PeriodSeconds)
				{
					monitor.LastExecution = now;
					try
					{
						monitor.Execute();
					}
					catch (Exception exc)
					{
						Report(exc);
					}
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

		static Monitor[] GetMonitors()
		{
			lock (Sync)
			{
				if (_monitors == null)
				{
					var infoList = AppSettings.Instance.Monitors;
					var res = new List<Monitor>();

					foreach (var info in infoList)
					{
						var typeName = typeof(Monitor).Namespace + "." + info.Name;
						var type = Assembly.GetExecutingAssembly().GetType(typeName);
						var monitor = (Monitor)Activator.CreateInstance(type);
						monitor.Info = info;
						res.Add(monitor);
					}

					_monitors = res;
				}
				return _monitors.ToArray();
			}
		}

		private static volatile bool _terminated;
		private static Thread _thread;
		private static readonly object Sync = new object();

		private static List<Monitor> _monitors;
	}
}
