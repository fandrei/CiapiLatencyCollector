using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;

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
					CheckUpdates();
					Thread.Sleep(AutoUpdateCheckPeriod);
				}
			}
			catch (ThreadInterruptedException)
			{}
			catch (Exception exc)
			{
				Trace.WriteLine(exc);
			}
		}

		void CheckUpdates()
		{
			using (var client = new WebClient())
			{
				if (!Directory.Exists(Const.WorkingAreaPath))
					Directory.CreateDirectory(Const.WorkingAreaPath);

				var assemblyPath = Const.WorkerAssemblyPath;
				var tmpAssemblyPath = assemblyPath + ".tmp";
				client.DownloadFile(Const.AutoUpdateUrl, tmpAssemblyPath);

				if (!File.Exists(assemblyPath) || !AssembliesAreEqual(tmpAssemblyPath, assemblyPath))
				{
					StopWorkerDomain();
					File.Delete(assemblyPath);
					File.Move(tmpAssemblyPath, assemblyPath);
				}

				if (_appDomain == null)
					StartWorkerDomain(assemblyPath);
			}
		}

		private static bool AssembliesAreEqual(string file1, string file2)
		{
			var version1 = FileVersionInfo.GetVersionInfo(file1);
			var version2 = FileVersionInfo.GetVersionInfo(file2);
			var res = version1.ToString().Equals(version2.ToString());
			return res;
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

		private readonly object _sync = new object();
		private Thread _thread;
		private AppDomain _appDomain;
		private dynamic _proxyClass;
		private static readonly TimeSpan AutoUpdateCheckPeriod = TimeSpan.FromMinutes(10);
	}
}
