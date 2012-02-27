using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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

		protected override void OnStart(string[] args)
		{
			lock (Sync)
			{
				_thread = new Thread(ThreadProc);
				_thread.Start();
			}
		}

		protected override void OnStop()
		{
			lock (Sync)
			{
				_thread.Interrupt();
				_thread = null;
			}
		}

		static void ThreadProc()
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

		static void CheckUpdates()
		{
			using (var client = new WebClient())
			{
				var tmpAssemblyPath = Const.WorkerAssemblyPath + ".tmp";
				client.DownloadFile(Const.AutoUpdateUrl, tmpAssemblyPath);
			}
		}

		private static Thread _thread;
		private static readonly object Sync = new object();
		private static readonly TimeSpan AutoUpdateCheckPeriod = TimeSpan.FromMinutes(1);
	}
}
