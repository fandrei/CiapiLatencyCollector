﻿using System;
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

					var period = TimeSpan.FromMinutes(1.0 / AppSettings.Instance.DataPollingRate);
					Thread.Sleep(period);
				}
			}
			catch (ThreadInterruptedException)
			{ }

			StopStreaming();

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
			EnsureStreamingStarted();

			var data = new Data();
			try
			{
				data.Login();

				data.GetMarketsList(MarketType.CFD, 100, "", "");
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

		static void EnsureStreamingStarted()
		{
			try
			{
				lock (StreamingSync)
				{
					if (_streamingData != null)
						return;

					var streamingData = new Data();
					streamingData.Login();
					streamingData.SubscribePrice(400481142); // GBP/USD

					_streamingData = streamingData;
				}
			}
			catch (Exception exc)
			{
				Report(exc);
			}
		}

		static void StopStreaming()
		{
			try
			{
				lock (StreamingSync)
				{
					if (_streamingData == null)
						return;

					try
					{
						_streamingData.Logout();
					}
					catch (Exception exc)
					{
						Report(exc);
					}

					_streamingData.Dispose();
					_streamingData = null;
				}
			}
			catch (Exception exc)
			{
				Report(exc);
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

		private static Data _streamingData;
		private static readonly object StreamingSync = new object();

		private static volatile bool _terminated;
		private static Thread _thread;
		private static readonly object Sync = new object();
	}
}
