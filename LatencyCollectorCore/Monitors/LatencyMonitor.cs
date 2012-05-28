using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

namespace LatencyCollectorCore.Monitors
{
	[XmlInclude(typeof(DefaultPageMonitor))]
	[XmlInclude(typeof(AuthenticatedMonitor))]
	[XmlInclude(typeof(AllServiceMonitor))]
	public abstract class LatencyMonitor
	{
		protected LatencyMonitor()
		{
			PeriodSeconds = 10;
		}

		public int PeriodSeconds { get; set; }

		public void Run()
		{
			IsExecuting = true;
			LastExecution = DateTime.UtcNow;

			lock (_sync)
			{
				_thread = new Thread(ThreadEntry);
				_thread.Start();
			}
		}

		private void ThreadEntry()
		{
			try
			{
				Execute();
			}
			catch (ThreadInterruptedException)
			{
			}
			catch (Exception exc)
			{
				Program.Report(exc);
			}

			lock (_sync)
			{
				IsExecuting = false;
				_thread = null;
			}
		}

		public void Interrupt()
		{
			lock (_sync)
			{
				if (_thread == null)
					return;
				_thread.Interrupt();
			}
		}

		public void WaitForFinish()
		{
			lock (_sync)
			{
				if (_thread == null)
					return;
				_thread.Join(TimeSpan.FromSeconds(10));
			}
		}

		protected abstract void Execute();

		private volatile bool _isExecuting;

		[XmlIgnore]
		public bool IsExecuting
		{
			get { return _isExecuting; }
			private set { _isExecuting = value; }
		}

		private Thread _thread;
		private readonly object _sync = new object();

		[XmlIgnore]
		public DateTime LastExecution { get; set; }
	}
}
