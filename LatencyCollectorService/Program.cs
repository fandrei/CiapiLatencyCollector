using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
			if (args.Length > 0)
			{
				var arg = args[0];

				try
				{
					if (arg == "-debug")
					{
						var service = new LatencyCollectorService();
						service.Start();
						Thread.Sleep(Timeout.Infinite);
					}
					else if (arg == "-install")
					{
						ManagedInstallerClass.InstallHelper(new[] { ExePath });
					}
					else if (arg == "-uninstall")
					{
						ManagedInstallerClass.InstallHelper(new[] { "/u", ExePath });
					}
				}
				catch (Exception exc)
				{
					var message = exc.ToString();
					ShowMessage(message);
				}
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

		static void ShowMessage(string message)
		{
			MessageBox(IntPtr.Zero, message, Const.AppName, 0);
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int MessageBox(IntPtr hWnd, string text, string caption, int type);


		private static readonly string ExePath = Assembly.GetExecutingAssembly().Location;
	}
}
