using System;
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
			_debugMode = true;
		}

		protected override void OnStart(string[] args)
		{
			lock (_sync)
			{
				_thread = new Thread(ThreadProc);
				_thread.Start();
			}
		}

		protected override void OnStop()
		{
			lock (_sync)
			{
				_thread.Interrupt();
				_thread = null;
			}
		}

		void ThreadProc()
		{
			try
			{
				while (true)
				{
					try
					{
						ApplyUpdates();
						if (_appDomain == null)
							StartWorkerDomain(Const.WorkerAssemblyPath);
					}
					catch (ThreadInterruptedException)
					{
						break;
					}
					catch (Exception exc)
					{
						WriteEventLog(exc.ToString());
					}

					Thread.Sleep(AutoUpdateCheckPeriod);
				}
			}
			catch (ThreadInterruptedException)
			{
			}
		}

		void ApplyUpdates()
		{
			using (var client = new WebClient())
			{
				if (!Directory.Exists(Const.WorkingAreaPath))
					Directory.CreateDirectory(Const.WorkingAreaPath);

				// compare versions
				var localVersionFile = Const.WorkingAreaPath + "version.txt";
				if (File.Exists(localVersionFile))
				{
					var localVersion = File.ReadAllText(localVersionFile);
					var newVersion = client.DownloadString(Const.AutoUpdateBaseUrl + "version.txt");
					if (newVersion == localVersion)
						return;
				}

				StopWorkerDomain();

				DeleteAllFiles(Const.WorkingAreaPath);

				var zipFilePath = Const.WorkingAreaPath + "LatencyCollectorCore.zip";
				client.DownloadFile(Const.AutoUpdateBaseUrl + "LatencyCollectorCore.zip", zipFilePath);
				var zipFile = new ZipFile(zipFilePath);
				zipFile.ExtractAll(Const.WorkingAreaPath);
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
			_appDomain = CreateAppDomain(Const.WorkingAreaPath);
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

		public void WriteEventLog(string message)
		{
			if (_debugMode)
			{
				Trace.WriteLine(message);
				return;
			}

			try
			{
				var appId = this.ServiceName;
				EventLog.WriteEntry(appId, message, EventLogEntryType.Information);
			}
			catch (Exception exc)
			{
				Trace.WriteLine(message);
				Trace.WriteLine(exc);
			}
		}

		private readonly object _sync = new object();
		private Thread _thread;
		private AppDomain _appDomain;
		private dynamic _proxyClass;
		private static readonly TimeSpan AutoUpdateCheckPeriod = TimeSpan.FromMinutes(1);
		private bool _debugMode;
	}
}
