using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LatencyCollectorCore
{
	class AllServiceMonitor : Monitor
	{
		public override void Execute()
		{
			var data = new Data();
			try
			{
				data.Login();

				data.GetMarketsList(MarketType.CFD, 100, "", "");
			}
			catch (Exception exc)
			{
				Program.Report(exc);
			}
			finally
			{
				try
				{
					data.Logout();
				}
				catch (Exception exc)
				{
					Program.Report(exc);
				}
				finally
				{
					data.Dispose();
				}
			}
		}
	}
}
