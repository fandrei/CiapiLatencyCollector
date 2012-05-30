﻿using System;
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

		[XmlIgnore]
		public TimeSpan Period { get { return TimeSpan.FromSeconds(PeriodSeconds); } }

		public void Start()
		{
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
				while (!_terminated)
				{
					LastExecution = DateTime.UtcNow;
					Execute();

					var executionTime = DateTime.UtcNow - LastExecution;
					var period = Period - executionTime;
					if (period.TotalSeconds > 0)
						Thread.Sleep(period);
				}
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
				_thread = null;
			}
		}

		public void Interrupt()
		{
			lock (_sync)
			{
				_terminated = true;
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

		private Thread _thread;
		private readonly object _sync = new object();

		private volatile bool _terminated;

		[XmlIgnore]
		public DateTime LastExecution { get; set; }
	}
}