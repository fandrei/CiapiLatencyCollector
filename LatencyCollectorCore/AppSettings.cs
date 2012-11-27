using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

using AppMetrics.Shared;

using LatencyCollectorCore.Monitors;

namespace LatencyCollectorCore
{
	public class AppSettings : AppMetrics.AgentService.PluginBase.AppSettings
	{
		private string _lastConfigText;

		public bool CheckRemoteSettings()
		{
			try
			{
				if (string.IsNullOrEmpty(UserName))
					return false;

				var text = DownloadConfigText();
				if (text == _lastConfigText)
					return false;

				if (text == null)
				{
					_monitorSettings.PollingDisabled = true;
				}
				else
				{
					_monitorSettings.Dispose();
					ApplyRemoteSettings(text);
				}

				_lastConfigText = text;

				return true;
			}
			catch (Exception exc)
			{
				Program.Tracker.Log(exc);
			}
			return false;
		}

		private string DownloadConfigText()
		{
			var configAddress = ConfigBaseUrl + string.Format("/GetConfig.ashx?NodeName={0}", NodeName);

			using (var client = new WebClient())
			{
				client.Credentials = new NetworkCredential(UserName, Password);

				var res = client.DownloadString(configAddress);
				if (res == "disabled")
					return null;
				return res;
			}
		}

		private static XmlSerializer _serializer;

		void ApplyRemoteSettings(string text)
		{
			if (_serializer == null)
			{
				_serializer = new XmlSerializer(typeof(MonitorSettings));
			}

			using (var rd = new StringReader(text))
			{
				_monitorSettings = (MonitorSettings)_serializer.Deserialize(rd);
			}

			_monitorSettings.PollingDisabled = false;
		}

		private MonitorSettings _monitorSettings = new MonitorSettings();

		public MonitorSettings MonitorSettings
		{
			get { return _monitorSettings; }
		}

		private static AppSettings _instance;

		public static AppSettings Instance
		{
			get { return _instance ?? (_instance = Load<AppSettings>(FileName)); }
		}

		public static string Version
		{
			get
			{
				var res = string.Format("v{0}", Assembly.GetCallingAssembly().GetName().Version);
				return res;
			}
		}

		protected override void OnAfterLoad()
		{
			if (UserId.IsNullOrEmpty())
			{
				UserId = Guid.NewGuid().ToString();
			}
		}
	}
}
