﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;

using Ionic.Zip;

namespace CiapiLatencyCollector
{
	public partial class LatencyCollectorService : ServiceBase
	{
		public LatencyCollectorService()
		{
			InitializeComponent();
		}

		public void Start()
		{
			OnStart(new string[0]);
		}

		protected override void OnStart(string[] args)
		{
			lock (_sync)
			{
				_terminated = false;
				_thread = new Thread(ThreadProc);
				_thread.Start();
			}
		}

		protected override void OnStop()
		{
			lock (_sync)
			{
				try
				{
					_terminated = true;
					_thread.Abort();

					StopWorkerDomain();

					_thread.Join();
					_thread = null;
				}
				catch (Exception exc)
				{
					ReportEvent(exc.ToString(), EventLogEntryType.Warning);
				}
			}
		}

		void ThreadProc()
		{
			try
			{
				while (!_terminated)
				{
					try
					{
						ApplyUpdates();
						if (_appDomain == null)
							StartWorkerDomain(Const.WorkerAssemblyPath);
					}
					catch (ThreadAbortException)
					{
						break;
					}
					catch (Exception exc)
					{
						ReportEvent(exc.ToString(), EventLogEntryType.Warning);
					}

					Thread.Sleep(AutoUpdateCheckPeriod);
				}
			}
			catch (ThreadAbortException)
			{
			}
		}

		void ApplyUpdates()
		{
			using (var client = new WebClient())
			{
				if (!Directory.Exists(Const.WorkingAreaBinPath))
					Directory.CreateDirectory(Const.WorkingAreaBinPath);

				// compare versions
				var localVersionFile = Const.WorkingAreaBinPath + "version.txt";
				if (File.Exists(localVersionFile))
				{
					var localVersion = File.ReadAllText(localVersionFile);
					var newVersion = client.DownloadString(Const.AutoUpdateBaseUrl + "version.txt");
					if (newVersion == localVersion)
						return;
					ReportEvent(string.Format("Trying to update to version {0}", newVersion));
				}

				StopWorkerDomain();

				DeleteAllFiles(Const.WorkingAreaBinPath);

				var zipFilePath = Const.WorkingAreaBinPath + "LatencyCollectorCore.zip";
				client.DownloadFile(Const.AutoUpdateBaseUrl + "LatencyCollectorCore.zip", zipFilePath);
				var zipFile = new ZipFile(zipFilePath);
				zipFile.ExtractAll(Const.WorkingAreaBinPath);

				ReportEvent(string.Format("Update is successful"));
			}
		}

		private static void DeleteAllFiles(string path)
		{
			foreach (var file in Directory.GetFiles(path))
			{
				File.Delete(file);
			}
		}

		private void StartWorkerDomain(string assemblyPath)
		{
			_appDomain = CreateAppDomain(Const.WorkingAreaBinPath);
			_proxyClass = _appDomain.CreateInstanceFromAndUnwrap(assemblyPath, "LatencyCollectorCore.Proxy");
			_proxyClass.Start();
		}

		void StopWorkerDomain()
		{
			if (_appDomain == null)
				return;

			_proxyClass.Stop();
			_proxyClass = null;
			AppDomain.Unload(_appDomain);
			_appDomain = null;
		}

		static AppDomain CreateAppDomain(string basePath)
		{
			var setup = new AppDomainSetup
				{
					ApplicationBase = basePath
				};
			var appDomain = AppDomain.CreateDomain("LatencyCollectorCore", null, setup);
			return appDomain;
		}

		public void ReportEvent(string message, EventLogEntryType type = EventLogEntryType.Information)
		{
			Trace.WriteLine(message);

			try
			{
				var appId = this.ServiceName;
				EventLog.WriteEntry(appId, message, type);
			}
			catch (Exception exc)
			{
				Trace.WriteLine(exc);
			}
		}

		private readonly object _sync = new object();
		private Thread _thread;
		private volatile bool _terminated;
		private AppDomain _appDomain;
		private dynamic _proxyClass;
		private static readonly TimeSpan AutoUpdateCheckPeriod = TimeSpan.FromMinutes(1);
	}
}
