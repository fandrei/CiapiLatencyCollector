using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LatencyCollectorCore.Monitors
{
	class SettingsUpdateChecker : LatencyMonitor
	{
		public SettingsUpdateChecker()
		{
			PeriodSeconds = 60;
		}

		protected override void Execute()
		{
			AppSettings.Instance.CheckUpdates();
		}
	}
}
