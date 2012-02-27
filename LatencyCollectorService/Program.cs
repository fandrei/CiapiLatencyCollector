using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace CiapiLatencyCollector
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
			if (args.Length > 0 && args[0] == "-debug")
			{
				var service = new LatencyCollectorService();
				service.Start();
				Thread.Sleep(Timeout.Infinite);
			}
			else
			{
				var servicesToRun = new ServiceBase[]
					{
						new LatencyCollectorService()
					};
				ServiceBase.Run(servicesToRun);
			}
		}
	}
}
