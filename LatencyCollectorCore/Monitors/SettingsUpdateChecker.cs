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

		public event Action OnExecute;

	    public override void Execute()
		{
			if (OnExecute != null)
				OnExecute();
		}
	}
}
