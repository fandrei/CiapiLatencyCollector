using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

using AppMetrics.Client;

namespace LatencyCollectorCore.Monitors
{
	[XmlInclude(typeof(DefaultPageMonitor))]
	[XmlInclude(typeof(AuthenticatedMonitor))]
	[XmlInclude(typeof(AllServiceMonitor))]
	public abstract class LatencyMonitor : IDisposable
	{
		protected LatencyMonitor()
		{
			PeriodSeconds = 10;

			lock (Sync)
			{
				if (!string.IsNullOrEmpty(LogEventUrl))
				{
					_tracker = Tracker.Create(LogEventUrl, ApplicationKey);
				}
			}
		}

		public abstract void Execute();

		protected virtual void Cleanup()
		{
		}

		public int PeriodSeconds { get; set; }

		[XmlIgnore]
		public TimeSpan Period { get { return TimeSpan.FromSeconds(PeriodSeconds); } }

		public string LogEventUrl { get; set; }
		public string ApplicationKey { get; set; }

		public void Start()
		{
			lock (_sync)
			{
				_terminated = false;
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
					try
					{
						Execute();
					}
					catch (Exception exc)
					{
						Tracker.Log(exc);
					}

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
				Tracker.Log(exc);
			}

			try
			{
				Cleanup();
			}
			catch (Exception exc)
			{
				Tracker.Log(exc);
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

		public void Dispose()
		{
			Interrupt();
		}

		private Thread _thread;
		private readonly object _sync = new object();

		private volatile bool _terminated;

		[XmlIgnore]
		public DateTime LastExecution { get; set; }

		static readonly object Sync = new object();

		private readonly Tracker _tracker;

		public Tracker Tracker
		{
			get
			{
				lock (Sync)
				{
					if (_tracker != null)
						return _tracker;
					else
						return Program.Tracker;
				}
			}
		}
	}
}
