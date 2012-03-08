using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
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
						InitWorkingArea();
						ManagedInstallerClass.InstallHelper(new[] { ExePath, "/LogFile=" });
					}
					else if (arg == "-uninstall")
					{
						ManagedInstallerClass.InstallHelper(new[] { "/u", ExePath, "/LogFile=" });
					}
					else if (arg == "-start")
					{
						StartService();
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

		private static void StartService()
		{
			using (var controller = new ServiceController(Const.AppName))
			{
				if (controller.Status != ServiceControllerStatus.Running)
				{
					controller.Start();
				}
			}
		}

		static void InitWorkingArea()
		{
			EnsureFolderExists(Const.WorkingAreaPath);
			EnsureFolderExists(Const.WorkingAreaBinPath);

			var accessRights = Directory.GetAccessControl(Const.WorkingAreaBinPath);
			var accessRule = new FileSystemAccessRule("NETWORK SERVICE", FileSystemRights.FullControl,
				InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None,
				AccessControlType.Allow);
			accessRights.AddAccessRule(accessRule);
			Directory.SetAccessControl(Const.WorkingAreaBinPath, accessRights);
		}

		private static void EnsureFolderExists(string path)
		{
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
		}

		static void ShowMessage(string message)
		{
			MessageBox(IntPtr.Zero, message, Const.AppName, MessageBoxOptions.OkOnly | MessageBoxOptions.Topmost);
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int MessageBox(IntPtr hWnd, string text, string caption, MessageBoxOptions type);

		[Flags]
		public enum MessageBoxOptions : uint
		{
			OkOnly = 0x000000,
			Topmost = 0x040000
		}

		private static readonly string ExePath = Assembly.GetExecutingAssembly().Location;
	}
}
