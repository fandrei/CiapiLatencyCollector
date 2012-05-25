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

			ThreadPool.QueueUserWorkItem(
				s =>
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
					IsExecuting = false;
				});
		}

		public void Interrupt()
		{
			_thread.Interrupt();
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

		[XmlIgnore]
		public DateTime LastExecution { get; set; }
	}
}
