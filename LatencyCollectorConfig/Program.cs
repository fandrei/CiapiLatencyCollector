using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using CiapiLatencyCollector;
using LatencyCollectorCore;

namespace LatencyCollectorConfig
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			try
			{
				if (args.Length != 0)
				{
					var argsDic = new Dictionary<string, string>();
					foreach (var arg in args)
					{
						if (arg.StartsWith("-") || arg.StartsWith("/"))
						{
							var tmp = arg.Substring(1);
							if (!tmp.Contains(":"))
							{
								argsDic.Add(tmp, "");
							}
							else
							{
								var parts = tmp.Split(':');
								var value = parts[1];
								if (parts.Count() > 2)
								{
									value = string.Join(":", parts.Skip(1));
								}

								argsDic.Add(parts[0], value);
							}
						}
					}

					string userName;
					argsDic.TryGetValue("username", out userName);

					string password;
					argsDic.TryGetValue("password", out password);

					if (!string.IsNullOrEmpty(userName))
					{
						AppSettings.Instance.UserName = userName;
						AppSettings.Instance.Password = password;
						
						AppSettings.Instance.Save();
					}
				}
				else
				{
					Application.EnableVisualStyles();
					Application.SetCompatibleTextRenderingDefault(false);
					Application.Run(new ConfigForm());
				}
			}
			catch (Exception exc)
			{
				MessageBox.Show(exc.ToString(), Const.AppName);
			}
		}
	}
}
